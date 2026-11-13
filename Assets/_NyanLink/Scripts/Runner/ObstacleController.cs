using System;
using System.Collections;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using UnityEngine;

namespace NineMood.Runner
{
    /// <summary>성공 예약은 이 장애물 인스턴스에만 속하고 충돌 때 한 번 판정한다.</summary>
    public class ObstacleController : MonoBehaviour
    {
        public static event Action OnObstacleFailed;
        public static event Action OnObstacleCleared;
        public event Action<ObstacleController, bool> Resolved;
        public event Action<ObstacleController> InputWindowOpened;
        public float moveSpeed = 4f;
        public float judgmentOffsetX;
        public float despawnX = -15f;
        public TileColor RequiredColor { get; private set; }
        public RunnerState RequiredAction { get; private set; }
        public bool HasSuccessReservation { get; private set; }
        public bool IsResolved { get; private set; }
        public bool IsInitialized { get; private set; }
        public float DistanceToRunner => _runner != null ? transform.position.x - _judgmentX : float.PositiveInfinity;
        public float TimeUntilCollision => Mathf.Max(0f, DistanceToRunner / Mathf.Max(0.01f, moveSpeed));
        public bool IsInputWindowOpen => IsInitialized && !IsResolved && DistanceToRunner >= 0f && DistanceToRunner <= _warningDistance;
        private RunnerController _runner;
        private float _warningDistance, _damage, _judgmentX;
        private bool _warningShown;
        private SpriteRenderer _sprite;

        private void OnEnable()
        {
            GameEvents.OnMatchCompleted += OnMatch;
            GameEvents.OnGameStateChanged += OnState;
        }
        private void OnDisable()
        {
            GameEvents.OnMatchCompleted -= OnMatch;
            GameEvents.OnGameStateChanged -= OnState;
        }
        private void OnState(GameState state)
        {
            if (state == GameState.Briefing) { gameObject.SetActive(false); Destroy(gameObject); }
        }

        public void Initialize(RunnerState required, RunnerController runner, float warningDistance,
            float obstacleDamage, TileColor? requiredColor = null)
        {
            RequiredAction = required;
            RequiredColor = requiredColor ?? (required == RunnerState.Jump ? TileColor.Red :
                required == RunnerState.Slide ? TileColor.Blue : TileColor.Yellow);
            _runner = runner;
            _warningDistance = warningDistance;
            _damage = obstacleDamage;
            _judgmentX = runner != null ? runner.transform.position.x + judgmentOffsetX : judgmentOffsetX;
            _sprite = GetComponentInChildren<SpriteRenderer>();
            HasSuccessReservation = false;
            IsResolved = false;
            _warningShown = false;
            IsInitialized = true;
        }

        private void Update()
        {
            if (!IsInitialized || IsResolved || !GameManager.IsCreated ||
                GameManager.Instance.CurrentState != GameState.RunnerPhase) return;
            transform.position += Vector3.left * moveSpeed * Time.deltaTime;
            if (!_warningShown && DistanceToRunner <= _warningDistance)
            {
                _warningShown = true;
                InputWindowOpened?.Invoke(this);
                StartCoroutine(Pulse());
            }
            if (transform.position.x <= _judgmentX) Judge();
        }

        private void OnMatch(int chainLength, int scoreGained, int tilesRemoved, TileColor color)
        {
            if (!GameManager.IsCreated || GameManager.Instance.CurrentState != GameState.RunnerPhase ||
                !IsInputWindowOpen || chainLength < 2 || tilesRemoved <= 0) return;
            if (color == RequiredColor) HasSuccessReservation = true;
            else FindFirstObjectByType<TileInputHandler>()?.LockInput(DataManager.Instance?.BalanceData?.mistakeLockDuration ?? 0.5f);
        }

        private IEnumerator Pulse()
        {
            float elapsed = 0f;
            while (!IsResolved)
            {
                elapsed += Time.deltaTime;
                Color color = RequiredColor == TileColor.Red ? Color.red : RequiredColor == TileColor.Blue ? Color.blue : Color.yellow;
                color.a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 2.5f));
                if (_sprite != null) _sprite.color = color;
                yield return null;
            }
        }

        private void Judge()
        {
            if (IsResolved) return;
            IsResolved = true;
            bool success = HasSuccessReservation;
            if (_sprite != null) _sprite.color = success ? Color.green : Color.red;
            if (!success)
            {
                _runner?.TriggerStumble();
                StaminaManager.Instance?.TakeDamage(_damage);
            }
            // 사망/리셋으로 이미 소유자가 취소했으면 이벤트로 재도전을 되살리지 않는다.
            if (GameManager.IsCreated && GameManager.Instance.CurrentState == GameState.RunnerPhase)
            {
                Resolved?.Invoke(this, success);
                if (success) OnObstacleCleared?.Invoke(); else OnObstacleFailed?.Invoke();
            }
            Destroy(gameObject, 0.3f);
        }
    }
}
