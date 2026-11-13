using System.Collections;
using System.Collections.Generic;
using NineMood.Boss.Patterns;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using UnityEngine;

namespace NineMood.Boss
{
    /// <summary>
    /// 보스 전투 상태 머신. GameState.BossPhase 동안 HP·Groggy·패턴을 관리한다.
    /// GameManager.TriggerStageClear() 호출로 씬을 종료한다.
    /// </summary>
    public class BossController : SceneSingleton<BossController>
    {
        // ── Inspector ─────────────────────────────────────────────
        [Header("데이터")]
        public BossBattleConfig config;
        [Tooltip("전투 규칙 정비 후 활성화. 현재는 보스 진입까지만 실행")]
        public bool enableCombat;
        private bool _battleStarted;
        public BalanceData      balance;

        [Header("패턴 컴포넌트 (Inspector에서 할당)")]
        public TimeAttackPattern     timeAttackPattern;
        public RhythmNotePattern     rhythmNotePattern;
        public ColorQuotaPattern     colorQuotaPattern;
        public MemorySequencePattern memorySequencePattern;

        // ── Runtime 상태 ──────────────────────────────────────────
        public float CurrentHP  { get; private set; }
        public float MaxHP      { get; private set; }
        public bool  IsGroggy   { get; private set; }
        public int   PhaseIndex { get; private set; }

        private BossState   _state = BossState.Idle;
        private float       _powerUpBonus;
        private bool        _hasInvincibleShield;
        private List<float> _sortedThresholds = new List<float>();
        private Coroutine   _groggyRoutine;
        private Coroutine   _patternRoutine;
        private bool        _patternSuccess;

        private enum BossState { Idle, Pattern, Groggy, PhaseChange, Dead }

        // ── 생명주기 ──────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            GameEvents.OnMatchCompleted   += OnMatchCompleted;
            ItemEffectEvents.OnPowerUp    += OnPowerUp;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            AbortBattle();
            GameEvents.OnMatchCompleted   -= OnMatchCompleted;
            ItemEffectEvents.OnPowerUp    -= OnPowerUp;
        }

        // ── 상태 머신 진입 ────────────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Paused) return;
            if (state == GameState.BossPhase)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsResuming) return;
                if (enableCombat && !_battleStarted) StartBattle();
                else if (!_battleStarted && config != null)
                {
                    MaxHP = CurrentHP = config.maxHP;
                    GameEvents.TriggerBossHPChanged(CurrentHP, MaxHP);
                }
            }
            else AbortBattle();
        }

        private void StartBattle()
        {
            if (config == null || balance == null)
            {
                Debug.LogError("[BossController] config 또는 balance가 할당되지 않았습니다.");
                return;
            }

            _battleStarted = true;
            MaxHP      = config.maxHP;
            CurrentHP  = MaxHP;
            PhaseIndex = 0;
            _powerUpBonus       = 0f;
            _hasInvincibleShield = false;
            _state = BossState.Idle;

            // 내림차순 정렬 (높은 HP에서 낮은 HP 순으로 확인)
            _sortedThresholds = new List<float>(config.phaseHPThresholds);
            _sortedThresholds.Sort((a, b) => b.CompareTo(a));

            GameEvents.TriggerBossHPChanged(CurrentHP, MaxHP);
            _patternRoutine = StartCoroutine(PatternLoop());
        }

        private void AbortBattle()
        {
            _battleStarted = false;
            StopAllCoroutines();
            GetActivePattern()?.Cancel();
            IsGroggy = false;
            _state = BossState.Idle;
        }

        // ── 패턴 루프 ─────────────────────────────────────────────

        private IEnumerator PatternLoop()
        {
            while (_state != BossState.Dead)
            {
                _state = BossState.Pattern;
                yield return RunPattern();

                if (_state == BossState.Dead) yield break;

                // 패턴 결과 처리
                if (_patternSuccess)
                {
                    ApplyPatternSuccessDamage();
                }
                else
                {
                    // Cyan 방패가 있으면 데미지 1회 무효
                    if (_hasInvincibleShield)
                        _hasInvincibleShield = false;
                    else
                        StaminaManager.Instance?.TakeDamage(config.failureStaminaDamage);
                    GameEvents.TriggerBossPatternResult(false);
                }

                if (_state == BossState.Dead) yield break;

                // 패턴 성공 후 Groggy 진입
                if (_patternSuccess)
                {
                    yield return EnterGroggy();
                }

                if (_state == BossState.Dead) yield break;

                // Groggy 종료 후 Phase Change 체크
                yield return CheckPhaseChange();

                if (_state == BossState.Dead) yield break;

                // 다음 패턴 전 짧은 간격
                yield return new WaitForSeconds(1f);
            }
        }

        // ── 개별 패턴 실행 ────────────────────────────────────────

        private IEnumerator RunPattern()
        {
            BossPatternBase pattern = GetPatternForConfig();
            if (pattern == null)
            {
                Debug.LogError($"[BossController] 패턴 컴포넌트 미할당: {config.battleType}");
                _patternSuccess = false;
                yield break;
            }

            bool done = false;
            _patternSuccess = false;

            GameEvents.TriggerBossPatternStarted(config.battleType);
            pattern.Run(config, balance, PhaseIndex,
                onSuccess: () => { _patternSuccess = true;  done = true; },
                onFailed:  () => { _patternSuccess = false; done = true; });

            while (!done)
                yield return null;

            GameEvents.TriggerBossPatternResult(_patternSuccess);
        }

        private BossPatternBase GetPatternForConfig()
        {
            if (config == null) return null;
            return config.battleType switch
            {
                BossBattleType.TimeAttack      => timeAttackPattern,
                BossBattleType.RhythmNote      => rhythmNotePattern,
                BossBattleType.ColorQuota      => colorQuotaPattern,
                BossBattleType.MemorySequence  => memorySequencePattern,
                _                              => null
            };
        }

        private BossPatternBase GetActivePattern()
        {
            if (timeAttackPattern     != null && timeAttackPattern.IsRunning)     return timeAttackPattern;
            if (rhythmNotePattern     != null && rhythmNotePattern.IsRunning)     return rhythmNotePattern;
            if (colorQuotaPattern     != null && colorQuotaPattern.IsRunning)     return colorQuotaPattern;
            if (memorySequencePattern != null && memorySequencePattern.IsRunning) return memorySequencePattern;
            return null;
        }

        // ── 데미지 ───────────────────────────────────────────────

        private void ApplyPatternSuccessDamage()
        {
            float dmg = balance.GetAttackDamage(
                config.successDamage, _powerUpBonus, IsGroggy, isCritical: false);
            ApplyDamage(dmg);
            _powerUpBonus = 0f; // 한 번 소모
        }

        private void ApplyGroggyDamage(int matchedCount, float comboMultiplier)
        {
            float dmg = balance.GetGroggyDamage(matchedCount, comboMultiplier)
                        * config.GetGroggyDamageMultiplier(balance);
            ApplyDamage(dmg);
        }

        private void ApplyDamage(float amount)
        {
            CurrentHP = Mathf.Max(0f, CurrentHP - amount);
            GameEvents.TriggerBossHPChanged(CurrentHP, MaxHP);

            if (CurrentHP <= 0f)
                _state = BossState.Dead;
        }

        // ── Groggy ───────────────────────────────────────────────

        private IEnumerator EnterGroggy()
        {
            _state = BossState.Groggy;
            IsGroggy = true;
            float duration = config.GetGroggyDuration(balance);
            GameEvents.TriggerBossGroggyStarted(duration);

            yield return new WaitForSeconds(duration);

            IsGroggy = false;
            GameEvents.TriggerBossGroggyEnded();
            _state = BossState.Idle;
        }

        // ── Phase Change ─────────────────────────────────────────

        private IEnumerator CheckPhaseChange()
        {
            if (_sortedThresholds.Count == 0) yield break;

            float hpRatio = CurrentHP / MaxHP;

            // 내림차순 임계값 목록에서 현재 phaseIndex 이후에 해당하는 다음 임계값 확인
            // phaseIndex 0 → thresholds[0], phaseIndex 1 → thresholds[1], ...
            if (PhaseIndex < _sortedThresholds.Count &&
                hpRatio <= _sortedThresholds[PhaseIndex])
            {
                PhaseIndex++;
                _state = BossState.PhaseChange;
                GameEvents.TriggerBossPhaseChanged(PhaseIndex);
                yield return new WaitForSeconds(1.5f); // Phase 연출 대기
                _state = BossState.Idle;
            }
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────────

        private void OnMatchCompleted(int chainLength, int scoreGained, int tilesRemoved, TileColor color)
        {
            if (!IsGroggy) return;
            if (GameManager.Instance?.CurrentState != GameState.BossPhase) return;

            if (color == TileColor.Cyan)
            {
                // Cyan: 무적 방패 획득 (다음 패턴 피격 1회 무효)
                _hasInvincibleShield = true;
                return;
            }

            // Groggy 중 매칭 → 보스 데미지
            float combo = chainLength > 1 ? Mathf.Log(chainLength, 2f) : 0f;
            ApplyGroggyDamage(tilesRemoved, combo);

            if (_state == BossState.Dead)
            {
                StopAllCoroutines();
                StartCoroutine(DefeatRoutine());
            }
        }

        private void OnPowerUp(float bonus)
        {
            _powerUpBonus += bonus;
        }

        // ── 보스 처치 ─────────────────────────────────────────────

        private IEnumerator DefeatRoutine()
        {
            _state = BossState.Dead;
            IsGroggy = false;
            GameEvents.TriggerBossHPChanged(0f, MaxHP);
            yield return new WaitForSeconds(1f);
            GameManager.Instance?.TriggerStageClear();
        }
    }
}
