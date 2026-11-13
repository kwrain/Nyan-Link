using NineMood.Services.Core;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using UnityEngine;

namespace NineMood.Core.Managers
{
    /// <summary>
    /// 러너 스태미나(HP) 관리.
    ///
    /// 감소율 공식 (RunnerPhase 동안):
    ///   currentDecay = (baseDecayRate × stageDecayMultiplier)
    ///                + (decayIncreasePerSecond × elapsedRunnerTime)
    ///
    /// 재도전 중에도 RunnerPhase 상태가 유지되므로 감소·시간 누적 모두 그대로 진행.
    /// BossPhase 중 자연감소 정지 — 패턴 실패 데미지는 TakeDamage로 직접 처리.
    /// </summary>
    public class StaminaManager : Singleton<StaminaManager>
    {
        [Header("스테이지 데이터 (null = BalanceData 기본값)")]
        public StageData stageData;

        public float CurrentStamina    { get; private set; }
        public float MaxStamina        { get; private set; }
        public float ElapsedRunnerTime => _elapsedRunnerTime;

        private float _baseDecayRate;
        private float _decayIncreasePerSec;
        private float _elapsedRunnerTime;
        private float _recoveryPerTile;

        protected override void Awake()
        {
            base.Awake();
            ResetStamina();
        }

        private void OnEnable()
        {
            ItemEffectEvents.OnRecovery += OnRecovery;
            GameEvents.OnMatchCompleted += OnMatchCompleted;
        }

        private void OnDisable()
        {
            ItemEffectEvents.OnRecovery -= OnRecovery;
            GameEvents.OnMatchCompleted -= OnMatchCompleted;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.RunnerPhase) return;

            _elapsedRunnerTime += Time.deltaTime;

            float currentDecay = _baseDecayRate + _decayIncreasePerSec * _elapsedRunnerTime;
            ApplyDelta(-currentDecay * Time.deltaTime);
        }

        // ── 공개 메서드 ────────────────────────────────────────

        public void ResetStamina()
        {
            BalanceData b = DataManager.Instance?.BalanceData;

            MaxStamina         = b?.maxStamina            ?? 100f;
            _recoveryPerTile   = b?.staminaRecoveryPerTile ?? 0.3f;
            _elapsedRunnerTime = 0f;

            float multiplier = stageData?.stageDecayMultiplier > 0f
                ? stageData.stageDecayMultiplier : 1f;
            _baseDecayRate = (b?.baseDecayRate ?? 0.15f) * multiplier;

            _decayIncreasePerSec = stageData != null && stageData.decayIncreasePerSecond > 0f
                ? stageData.decayIncreasePerSecond
                : (b?.arcadeDecayIncreasePerSec ?? 0.001f);

            CurrentStamina = MaxStamina;
            GameEvents.TriggerStaminaChanged(CurrentStamina, MaxStamina);
        }

        /// <summary>체크포인트 복원 — 감소율 연속성 유지를 위해 경과 시간도 함께 복원.</summary>
        public void RestoreStamina(float stamina, float elapsedRunnerTime)
        {
            CurrentStamina     = Mathf.Clamp(stamina, 0f, MaxStamina);
            _elapsedRunnerTime = elapsedRunnerTime;
            GameEvents.TriggerStaminaChanged(CurrentStamina, MaxStamina);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f) return;
            ApplyDelta(-amount);
        }

        public void Recover(float amount)
        {
            if (amount <= 0f) return;
            ApplyDelta(amount);
        }

        // ── 이벤트 핸들러 ──────────────────────────────────────

        private void OnRecovery(float percent) => Recover(percent * MaxStamina);

        private void OnMatchCompleted(int chainLength, int scoreGained, int tilesRemoved, TileColor color)
        {
            Recover(tilesRemoved * _recoveryPerTile);
        }

        // ── 내부 ───────────────────────────────────────────────

        private void ApplyDelta(float delta)
        {
            CurrentStamina = Mathf.Clamp(CurrentStamina + delta, 0f, MaxStamina);
            GameEvents.TriggerStaminaChanged(CurrentStamina, MaxStamina);

            if (CurrentStamina <= 0f)
                GameManager.Instance?.TriggerGameOver();
        }
    }
}
