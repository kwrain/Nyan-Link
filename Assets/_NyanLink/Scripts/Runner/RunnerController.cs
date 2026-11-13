using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using UnityEngine;

namespace NineMood.Runner
{
    /// <summary>
    /// 러너 캐릭터 상태 머신.
    /// 퍼즐 매칭(GameEvents.OnMatchCompleted)을 받아 색상에 맞는 액션을 실행한다.
    /// 상태별 지속 시간이 지나면 자동으로 Run 상태로 복귀.
    /// </summary>
    public class RunnerController : MonoBehaviour
    {
        [Header("데이터")]
        [Tooltip("색상-액션 매핑. 없으면 기본 매핑 사용")]
        public ColorActionMapData actionMap;

        [Header("참조")]
        [Tooltip("애니메이터 (없으면 애니메이션 없이 동작)")]
        public Animator animator;

        // ── 상태 ───────────────────────────────────────────────
        public RunnerState CurrentState { get; private set; } = RunnerState.Run;

        private float _stateTimer;
        private float _boostTimer;
        public float ActionTimeRemaining => _stateTimer;
        public float BoostTimeRemaining => _boostTimer;
        public float SpeedMultiplier => _boostTimer > 0f ? (Balance?.runSpeedMultiplier ?? 1.5f) : 1f;
        private BalanceData Balance => DataManager.Instance?.BalanceData;

        // ── 라이프사이클 ───────────────────────────────────────

        private void Awake()
        {
            var visual = transform.Find("RunnerVisual");
            if (visual != null)
            {
                var player = visual.GetComponent<PlayerCharacter>() ?? visual.gameObject.AddComponent<PlayerCharacter>();
                player.controller = this;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnMatchCompleted  += OnMatchCompleted;
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchCompleted  -= OnMatchCompleted;
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!GameManager.IsCreated || GameManager.Instance.CurrentState != GameState.RunnerPhase) return;
            _boostTimer = Mathf.Max(0f, _boostTimer - Time.deltaTime);
            if (CurrentState == RunnerState.Run) return;

            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
                TransitionTo(RunnerState.Run);
        }

        // ── 공개 메서드 ────────────────────────────────────────

        /// <summary>ObstacleController가 장애물 판정 실패 시 호출</summary>
        public void TriggerStumble()
        {
            TransitionTo(RunnerState.Stumble);
        }

        // ── 이벤트 핸들러 ──────────────────────────────────────

        private void OnMatchCompleted(int chainLength, int scoreGained, int tilesRemoved, TileColor color)
        {
            if (GameManager.Instance?.CurrentState != GameState.RunnerPhase) return;

            if (color == TileColor.Purple) _boostTimer = Mathf.Max(_boostTimer, Balance?.runSpeedDuration ?? 3f);
            RunnerState action = actionMap != null
                ? actionMap.GetAction(color)
                : ColorActionMapData.GetDefaultAction(color);

            if (action != RunnerState.Run)
                TransitionTo(action);
        }

        private void OnGameStateChanged(GameState state)
        {
            // Briefing 복귀 또는 Result(Lose) 시 러너 초기 자세로 리셋
            if (state == GameState.Briefing || state == GameState.Result)
            {
                _boostTimer = 0f;
                ForceRun();
            }
            else if (state == GameState.BossPhase && !(GameManager.Instance?.IsResuming ?? false)) ForceRun();
        }

        // ── 내부 ───────────────────────────────────────────────

        private void TransitionTo(RunnerState newState)
        {
            CurrentState = newState;
            _stateTimer  = GetDuration(newState);

            // 애니메이터 파라미터 동기화
            if (animator != null)
            {
                animator.SetInteger("State", (int)newState);
                animator.SetTrigger(newState.ToString());
            }

            // 스태미나 데미지는 ObstacleController.Judge()에서 StageData 기반으로 적용
        }

        private void ForceRun()
        {
            CurrentState = RunnerState.Run;
            _stateTimer  = 0f;
            if (animator != null)
                animator.SetInteger("State", (int)RunnerState.Run);
        }

        private float GetDuration(RunnerState state)
        {
            BalanceData b = Balance;
            return state switch
            {
                RunnerState.Jump       => b?.jumpDuration       ?? 0.8f,
                RunnerState.Slide      => b?.slideDuration      ?? 0.6f,
                RunnerState.Block      => b?.blockDuration      ?? 0.5f,
                RunnerState.Attack     => b?.attackDuration     ?? 0.4f,
                RunnerState.Invincible => b?.invincibleDuration ?? 2.0f,
                RunnerState.Stumble    => b?.stumbleDuration    ?? 1.0f,
                _                     => 0f
            };
        }
    }
}
