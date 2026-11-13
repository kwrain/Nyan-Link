using System.Collections;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using UnityEngine;

namespace NineMood.Runner
{
    /// <summary>
    /// 개별 장애물 — 기획서02 §3.2 예고/입력/결과 3단계.
    ///
    ///   Approach  : 오른쪽에서 왼쪽으로 이동. 아직 예고 없음.
    ///   Warning   : runner와의 거리 ≤ warningDistance → 색상 강조 + 펄싱.
    ///               이 구간이 입력 창(플레이어가 퍼즐 타일을 매칭하는 시간).
    ///   Judged    : 러너 X 위치 도달 → 현재 RunnerState 비교.
    ///               성공 = 무피해 통과, 실패 = TriggerStumble + 스태미나 데미지.
    ///
    /// </summary>
    public class ObstacleController : MonoBehaviour
    {
        // ── 정적 이벤트 (ObstacleSpawner가 구독해 재도전 처리) ────────
        public static event System.Action OnObstacleFailed;
        public static event System.Action OnObstacleCleared;

        [Header("이동")]
        public float moveSpeed       = 4f;
        public float judgmentOffsetX = 0f;
        public float despawnX        = -15f;

        private RunnerState      _requiredAction;
        private RunnerController _runner;
        private float            _warningDistance;
        private float            _obstacleDamage;

        private float          _judgmentX;
        private ObstaclePhase  _phase = ObstaclePhase.Approach;
        private SpriteRenderer _sr;
        private Color          _warningColor;

        private enum ObstaclePhase { Approach, Warning, Judged }

        // ── 초기화 ────────────────────────────────────────────────────

        private void OnEnable()  => GameEvents.OnGameStateChanged += OnGameStateChanged;
        private void OnDisable() => GameEvents.OnGameStateChanged -= OnGameStateChanged;

        private void OnGameStateChanged(GameState state)
        {
            // RunnerPhase 이탈 시 즉시 파괴 — 잔존 장애물의 이벤트 오염 방지
            if (state != GameState.RunnerPhase)
                Destroy(gameObject);
        }

        public void Initialize(RunnerState required, RunnerController runner,
                               float warningDistance, float obstacleDamage)
        {
            _requiredAction  = required;
            _runner          = runner;
            _warningDistance = warningDistance;
            _obstacleDamage  = obstacleDamage;
            _judgmentX       = runner != null
                ? runner.transform.position.x + judgmentOffsetX
                : judgmentOffsetX;

            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        }

        // ── 업데이트 ──────────────────────────────────────────────────

        private void Update()
        {
            if (_phase == ObstaclePhase.Judged) return;

            transform.position += Vector3.left * moveSpeed * Time.deltaTime;

            if (_phase == ObstaclePhase.Approach)
            {
                float distToRunner = _runner != null
                    ? transform.position.x - _runner.transform.position.x
                    : float.MaxValue;

                if (distToRunner <= _warningDistance)
                {
                    _phase = ObstaclePhase.Warning;
                    OnWarningEnter();
                }
            }

            if (transform.position.x <= _judgmentX)
                Judge();

            if (transform.position.x < despawnX)
                Destroy(gameObject);
        }

        // ── 단계 전환 ─────────────────────────────────────────────────

        private void OnWarningEnter()
        {
            _warningColor = GetWarningColor(_requiredAction);
            if (_sr != null) _sr.color = _warningColor;

            StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float t = 0f;
            while (_phase == ObstaclePhase.Warning)
            {
                t += Time.deltaTime;
                // 2.5Hz 사인파 펄싱 (alpha 0.35 ~ 1.0)
                float alpha = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2.5f));
                if (_sr != null)
                    _sr.color = new Color(_warningColor.r, _warningColor.g, _warningColor.b, alpha);
                yield return null;
            }
        }

        private void Judge()
        {
            _phase = ObstaclePhase.Judged;

            bool success = _runner != null && _runner.CurrentState == _requiredAction;

            if (success)
            {
                if (_sr != null) _sr.color = Color.green;
                OnObstacleCleared?.Invoke();
            }
            else
            {
                if (_sr != null) _sr.color = Color.red;
                _runner?.TriggerStumble();
                StaminaManager.Instance?.TakeDamage(_obstacleDamage);
                OnObstacleFailed?.Invoke();
            }

            Destroy(gameObject, 0.3f);
        }

        // ── 색상 유틸 ─────────────────────────────────────────────────

        private static Color GetWarningColor(RunnerState action) => action switch
        {
            RunnerState.Jump  => new Color(1f, 0.3f, 0.3f),
            RunnerState.Slide => new Color(0.3f, 0.5f, 1f),
            RunnerState.Block => new Color(1f, 0.9f, 0.2f),
            _                 => Color.white
        };
    }
}
