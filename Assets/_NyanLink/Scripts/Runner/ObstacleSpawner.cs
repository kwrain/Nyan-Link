using System.Collections;
using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Puzzle;
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

        public int NextObstacleIndex => _nextObstacleIndex;
        public int ClearedObstacleCount => _nextObstacleIndex;
        public int TotalObstacleCount => _sortedObstacles != null ? _sortedObstacles.Count : 0;
        public bool AllObstaclesCleared => _sortedObstacles != null && _nextObstacleIndex >= _sortedObstacles.Count && _current == null && _retryRemaining <= 0f;
        public ObstacleController CurrentObstacle => _current;
        public float RetryRemaining => _retryRemaining;
        public bool IsPreWarning => _preWarningFired;
        public float NextStopDistance => _sortedObstacles != null && _nextObstacleIndex < _sortedObstacles.Count
            ? _sortedObstacles[_nextObstacleIndex].triggerDistance * progressSystem.StageLength : progressSystem.StageLength;
        public float WarningRemaining => _retryRemaining > 0f ? _retryRemaining :
            _current != null ? _current.TimeUntilCollision :
            Mathf.Max(0f, NextStopDistance - progressSystem.CurrentDistance) / Mathf.Max(0.01f, progressSystem.RunnerSpeed);
        private List<ObstacleEntry> _sortedObstacles;
        private int _nextObstacleIndex;
        private bool _preWarningFired;
        private float _retryRemaining;
        private ObstacleController _current;
        private PuzzleBoardManager _board;
        private BalanceData Balance => DataManager.Instance?.BalanceData;

        private void Awake() => _board = FindFirstObjectByType<PuzzleBoardManager>();
        private void OnEnable() => GameEvents.OnGameStateChanged += OnGameStateChanged;
        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            CancelActive();
        }

        private void Update()
        {
            if (!GameManager.IsCreated || GameManager.Instance.CurrentState != GameState.RunnerPhase ||
                progressSystem == null || _sortedObstacles == null || AllObstaclesCleared) return;
            if (_retryRemaining > 0f)
            {
                _retryRemaining = Mathf.Max(0f, _retryRemaining - Time.deltaTime);
                if (_retryRemaining == 0f) SpawnCurrent();
                return;
            }
            if (_current != null) return;
            float timeLeft = WarningRemaining;
            if (!_preWarningFired && timeLeft <= preWarningTime)
            {
                _preWarningFired = true;
                PrepareRequiredColor();
                OnPreWarningBegan?.Invoke(RequiredAction(), timeLeft);
            }
            if (progressSystem.CurrentDistance >= NextStopDistance)
            {
                progressSystem.SetPaused(true);
                SpawnCurrent();
            }
        }

        private RunnerState RequiredAction() => ColorActionMapData.GetDefaultAction(_sortedObstacles[_nextObstacleIndex].requiredColor);
        private void PrepareRequiredColor()
        {
            if (_board == null) return;
            TileColor? color = _nextObstacleIndex < TotalObstacleCount ? _sortedObstacles[_nextObstacleIndex].requiredColor : (TileColor?)null;
            bool repaired = _board.SetRequiredMatchColor(color);
            if (!repaired && _board.IsInitialized)
                Debug.LogWarning("[ObstacleSpawner] 보호 조건으로 요구 색 보정 불가. 다음 리필에서 재검사합니다.");
        }

        private void SpawnCurrent()
        {
            OnPreWarningEnded?.Invoke();
            _preWarningFired = false;
            if (runner == null || _nextObstacleIndex >= TotalObstacleCount) return;
            PrepareRequiredColor();
            var entry = _sortedObstacles[_nextObstacleIndex];
            float y = runner.transform.position.y + spawnY;
            var prefab = GetPrefab(RequiredAction());
            var go = prefab != null ? Instantiate(prefab, new Vector3(spawnX, y, 0f), Quaternion.identity)
                : CreatePlaceholder(entry.requiredColor, y);
            _current = go.GetComponent<ObstacleController>() ?? go.AddComponent<ObstacleController>();
            _current.Initialize(RequiredAction(), runner, Balance?.obstacleWarningDistance ?? 5f,
                stageData?.ActualObstacleDamage ?? 20f, entry.requiredColor);
            _current.moveSpeed = stageData != null ? stageData.obstacleMoveSpeed : 4f;
            _current.Resolved += HandleResolved;
            _current.InputWindowOpened += HandleInputWindow;
            int layer = LayerMask.NameToLayer(obstacleLayer);
            if (layer >= 0) SetLayerRecursive(go, layer);
        }

        private void HandleInputWindow(ObstacleController obstacle)
        {
            if (obstacle == _current) OnPreWarningBegan?.Invoke(obstacle.RequiredAction, obstacle.TimeUntilCollision);
        }

        private void HandleResolved(ObstacleController obstacle, bool success)
        {
            if (obstacle != _current || !GameManager.IsCreated || GameManager.Instance.CurrentState != GameState.RunnerPhase) return;
            obstacle.Resolved -= HandleResolved;
            obstacle.InputWindowOpened -= HandleInputWindow;
            _current = null;
            OnPreWarningEnded?.Invoke();
            if (success)
            {
                _nextObstacleIndex++;
                progressSystem.SetPaused(false);
                PrepareRequiredColor();
            }
            else
            {
                _retryRemaining = retryDelay;
                OnPreWarningBegan?.Invoke(RequiredAction(), retryDelay);
            }
            SaveManager.Instance?.RequestBoundary(success ? NineMood.Core.Persistence.CheckpointBoundary.ObstacleSuccess : NineMood.Core.Persistence.CheckpointBoundary.ObstacleRetry);
        }

        private void CancelActive()
        {
            _retryRemaining = 0f;
            _preWarningFired = false;
            if (_current != null)
            {
                _current.Resolved -= HandleResolved;
                _current.InputWindowOpened -= HandleInputWindow;
                _current.gameObject.SetActive(false);
                Destroy(_current.gameObject);
                _current = null;
            }
            OnPreWarningEnded?.Invoke();
        }

        public void RestoreCheckpoint(int nextObstacleIndex)
        {
            CancelActive();
            _nextObstacleIndex = Mathf.Clamp(nextObstacleIndex, 0, TotalObstacleCount);
            progressSystem?.SetPaused(false);
            PrepareRequiredColor();
        }

        public void RestoreBoundary(int index,float retryRemaining)
        {
            RestoreCheckpoint(index);
            _retryRemaining=retryRemaining;
            progressSystem.SetPaused(retryRemaining>0f);
            if(retryRemaining>0f) OnPreWarningBegan?.Invoke(RequiredAction(),retryRemaining);
        }
        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Paused || state == GameState.ColorSelection || state == GameState.RunnerPhase) return;
            CancelActive();
            if (state == GameState.Briefing)
            {
                _nextObstacleIndex = 0;
                _sortedObstacles = stageData != null ? new List<ObstacleEntry>(stageData.obstacles) : new List<ObstacleEntry>
                {
                    new ObstacleEntry { triggerDistance = 0.25f, requiredColor = TileColor.Red },
                    new ObstacleEntry { triggerDistance = 0.5f, requiredColor = TileColor.Blue },
                    new ObstacleEntry { triggerDistance = 0.75f, requiredColor = TileColor.Yellow }
                };
                _sortedObstacles.Sort((a,b) => a.triggerDistance.CompareTo(b.triggerDistance));
                progressSystem?.SetPaused(false);
                PrepareRequiredColor();
            }
            else _board?.SetRequiredMatchColor(null);
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

    }
}
