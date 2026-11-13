using System.Collections;
using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using UnityEngine;

namespace NineMood.Runner
{
    /// <summary>
    /// 거리 기반 장애물 스폰 + 재도전 관리 — 기획서02 §3.2.
    ///
    /// 사전 경고 흐름:
    ///   스폰 preWarningTime 초 전 → OnPreWarningBegan (HUD 표시)
    ///   실제 스폰 시              → OnPreWarningEnded  (HUD 닫힘)
    ///
    /// 재도전 흐름:
    ///   장애물 실패 → 러너 진행 정지 → retryDelay 초 대기(HUD 재표시) → 같은 장애물 재스폰
    ///   장애물 클리어 → 러너 진행 재개 → 다음 장애물로
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        // ── 사전 경고 이벤트 (RunnerWarningHUD가 구독) ───────────────
        public static event System.Action<RunnerState, float> OnPreWarningBegan;
        public static event System.Action                     OnPreWarningEnded;

        [Header("참조")]
        public RunnerController     runner;
        public RunnerProgressSystem progressSystem;

        [Header("스테이지")]
        [Tooltip("null이면 테스트용 기본 장애물 3종 사용")]
        public StageData stageData;

        [Header("사전 경고")]
        [Tooltip("장애물 등장 몇 초 전에 HUD 경고를 표시할지")]
        [Min(0.1f)]
        public float preWarningTime = 2f;

        [Header("재도전")]
        [Tooltip("장애물 실패 후 재스폰까지 대기 시간 (초). 이 동안 HUD 경고 재표시")]
        [Min(0.5f)]
        public float retryDelay = 2f;

        [Header("프리팹 (비어있으면 임시 박스 사용)")]
        public GameObject jumpObstaclePrefab;
        public GameObject slideObstaclePrefab;
        public GameObject blockObstaclePrefab;

        [Header("스폰 위치")]
        public float spawnX = 12f;
        [Tooltip("러너 캐릭터 Y 기준 오프셋")]
        public float spawnY = 0f;

        [Header("레이어")]
        public string obstacleLayer = "TopView";

        /// <summary>다음에 스폰할 장애물 인덱스. SaveManager가 체크포인트 저장에 사용.</summary>
        public int NextObstacleIndex => _nextObstacleIndex;

        private List<ObstacleEntry> _sortedObstacles;
        private int       _nextObstacleIndex;
        private bool      _active;
        private bool      _preWarningFired;
        private bool      _waitingForClear;     // 현재 장애물 클리어 대기 중
        private Coroutine _retryCoroutine;

        private BalanceData Balance => DataManager.Instance?.BalanceData;

        // ── 라이프사이클 ──────────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged     += OnGameStateChanged;
            ObstacleController.OnObstacleFailed  += HandleObstacleFailed;
            ObstacleController.OnObstacleCleared += HandleObstacleCleared;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged     -= OnGameStateChanged;
            ObstacleController.OnObstacleFailed  -= HandleObstacleFailed;
            ObstacleController.OnObstacleCleared -= HandleObstacleCleared;
        }

        private void Update()
        {
            if (!_active || progressSystem == null) return;
            if (_sortedObstacles == null || _nextObstacleIndex >= _sortedObstacles.Count) return;
            if (_waitingForClear) return;   // 재도전 처리 중 — 신규 스폰·경고 모두 중단

            float dist     = progressSystem.CurrentDistance;
            float speed    = progressSystem.RunnerSpeed;
            float stageLen = progressSystem.StageLength;

            // ── 사전 경고 ─────────────────────────────────────────────
            if (!_preWarningFired)
            {
                var   next        = _sortedObstacles[_nextObstacleIndex];
                float triggerDist = next.triggerDistance * stageLen;
                float timeLeft    = speed > 0f ? (triggerDist - dist) / speed : float.MaxValue;

                if (timeLeft <= preWarningTime && timeLeft > 0f)
                {
                    _preWarningFired = true;
                    OnPreWarningBegan?.Invoke(
                        ColorActionMapData.GetDefaultAction(next.requiredColor), timeLeft);
                }
            }

            // ── 스폰 ──────────────────────────────────────────────────
            while (_nextObstacleIndex < _sortedObstacles.Count)
            {
                float triggerDist = _sortedObstacles[_nextObstacleIndex].triggerDistance * stageLen;
                if (dist >= triggerDist)
                {
                    OnPreWarningEnded?.Invoke();
                    DoSpawn(_sortedObstacles[_nextObstacleIndex]);
                    progressSystem?.SetPaused(true);    // 판정 완료(성공/실패)까지 진행률 동결
                    _nextObstacleIndex++;
                    _preWarningFired = false;
                    _waitingForClear = true;
                    break;  // 한 프레임에 하나씩만 스폰 (dist는 루프 진입 시점에 고정)
                }
                else
                {
                    break;
                }
            }
        }

        // ── 재도전 이벤트 핸들러 ─────────────────────────────────────

        private void HandleObstacleFailed()
        {
            // 잔존 장애물(씬 리셋 직후) 이벤트 방어 — 인덱스 0이면 무시
            if (_nextObstacleIndex <= 0) return;

            // progressSystem은 스폰 시 이미 SetPaused(true) 상태
            if (_retryCoroutine != null)
                StopCoroutine(_retryCoroutine);
            _retryCoroutine = StartCoroutine(RetryRoutine(_nextObstacleIndex - 1));
        }

        private void HandleObstacleCleared()
        {
            _waitingForClear = false;
            progressSystem?.SetPaused(false);
        }

        private IEnumerator RetryRoutine(int entryIndex)
        {
            // retryDelay 동안 HUD 경고 재표시
            var   entry  = _sortedObstacles[entryIndex];
            RunnerState action = ColorActionMapData.GetDefaultAction(entry.requiredColor);
            OnPreWarningBegan?.Invoke(action, retryDelay);

            yield return new WaitForSeconds(retryDelay);

            OnPreWarningEnded?.Invoke();
            DoSpawn(entry);     // _nextObstacleIndex는 그대로 (재도전이므로 증가 없음)

            _retryCoroutine = null;
        }

        /// <summary>체크포인트 복원 — SaveManager가 StartGame 직후 호출한다.</summary>
        public void RestoreCheckpoint(int nextObstacleIndex)
        {
            _nextObstacleIndex = Mathf.Clamp(nextObstacleIndex, 0,
                _sortedObstacles != null ? _sortedObstacles.Count : 0);
            _preWarningFired = false;
            _waitingForClear = false;
        }

        // ── 스폰 내부 ─────────────────────────────────────────────────

        private void DoSpawn(ObstacleEntry entry)
        {
            if (runner == null) return;

            RunnerState required    = ColorActionMapData.GetDefaultAction(entry.requiredColor);
            float       warningDist = Balance?.obstacleWarningDistance ?? 5f;
            float       damage      = stageData?.ActualObstacleDamage  ?? 20f;

            float worldY     = runner.transform.position.y + spawnY;
            Vector3 spawnPos = new Vector3(spawnX, worldY, 0f);

            GameObject prefab = GetPrefab(required);
            GameObject go     = prefab != null
                ? Instantiate(prefab, spawnPos, Quaternion.identity)
                : CreatePlaceholder(entry.requiredColor, worldY);

            var obstacle = go.GetComponent<ObstacleController>()
                           ?? go.AddComponent<ObstacleController>();
            obstacle.Initialize(required, runner, warningDist, damage);

            int layer = LayerMask.NameToLayer(obstacleLayer);
            if (layer >= 0) SetLayerRecursive(go, layer);
        }

        private GameObject GetPrefab(RunnerState action) => action switch
        {
            RunnerState.Jump  => jumpObstaclePrefab,
            RunnerState.Slide => slideObstaclePrefab,
            RunnerState.Block => blockObstaclePrefab,
            _                 => null
        };

        private GameObject CreatePlaceholder(TileColor color, float worldY)
        {
            Color c = color switch
            {
                TileColor.Red    => Color.red,
                TileColor.Blue   => new Color(0.3f, 0.5f, 1f),
                TileColor.Yellow => Color.yellow,
                _                => Color.white
            };
            var go = new GameObject($"Obstacle_{color}");
            go.transform.position   = new Vector3(spawnX, worldY, 0f);
            go.transform.localScale = Vector3.one * 0.8f;
            var sr    = go.AddComponent<SpriteRenderer>();
            sr.color  = c;
            sr.sprite = GetDefaultSprite();
            return go;
        }

        private static Sprite _defaultSprite;
        private static Sprite GetDefaultSprite()
        {
            if (_defaultSprite != null) return _defaultSprite;
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _defaultSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _defaultSprite;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        // ── 게임 상태 변경 ───────────────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            _active = state == GameState.RunnerPhase;
            if (state == GameState.RunnerPhase)
            {
                // Briefing 없이 RetryGame() 직행 시 잔류 상태 방지
                _waitingForClear = false;
                if (_retryCoroutine != null)
                {
                    StopCoroutine(_retryCoroutine);
                    _retryCoroutine = null;
                }
            }

            if (state == GameState.Briefing)
            {
                if (_retryCoroutine != null)
                {
                    StopCoroutine(_retryCoroutine);
                    _retryCoroutine = null;
                }
                _nextObstacleIndex = 0;
                _preWarningFired   = false;
                _waitingForClear   = false;

                if (stageData?.HasObstacles == true)
                {
                    _sortedObstacles = new List<ObstacleEntry>(stageData.obstacles);
                    _sortedObstacles.Sort((a, b) =>
                        a.triggerDistance.CompareTo(b.triggerDistance));
                }
                else
                {
                    _sortedObstacles = new List<ObstacleEntry>
                    {
                        new ObstacleEntry { triggerDistance = 0.25f, requiredColor = TileColor.Red    },
                        new ObstacleEntry { triggerDistance = 0.50f, requiredColor = TileColor.Blue   },
                        new ObstacleEntry { triggerDistance = 0.75f, requiredColor = TileColor.Yellow },
                    };
                }
            }
        }
    }
}
