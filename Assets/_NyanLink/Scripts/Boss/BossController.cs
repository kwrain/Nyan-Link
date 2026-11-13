using NineMood.Boss.Patterns;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using NineMood.Core.Persistence;
using UnityEngine;

namespace NineMood.Boss
{
    /// <summary>프로토타입 1.1: 타임어택→그로기. 피해는 ApplyDamage 한 경로에서 처치를 확정한다.</summary>
    public class BossController : SceneSingleton<BossController>
    {
        public BossBattleConfig config;
        public bool enableCombat = true;
        public BalanceData balance;
        public TimeAttackPattern timeAttackPattern;
        // 보류 패턴은 리소스와 함께 보존하되 선택하지 않는다.
        public RhythmNotePattern rhythmNotePattern;
        public ColorQuotaPattern colorQuotaPattern;
        public MemorySequencePattern memorySequencePattern;
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }
        public bool IsGroggy { get; private set; }
        public int PhaseIndex { get; private set; }
        public float GroggyRemaining { get; private set; }
        public float TimerFreezeRemaining => StaminaManager.Instance?.DecayFreezeRemaining ?? 0f;
        public bool TimersFrozen => TimerFreezeRemaining > 0f;
        public float PendingAttackBonus { get; private set; }
        public bool HasFailureShield { get; private set; }
        public bool IsPatternActive => _state == BossState.Pattern;
        public bool CanUseItems => IsActive && IsGroggy;
        public bool CanUseColorActions => CanUseItems;
        public bool PreserveItems => _battleStarted && !IsGroggy;
        public bool IsDefeated { get; private set; }
        private bool _battleStarted;
        private System.Collections.Generic.List<TileColor> _nextSequence;
        private bool _groggyPending;
        private float _transitionRemaining;
        private BossState _state;
        private enum BossState { Idle, Pattern, Groggy, Interval, Dead }
        private bool IsActive => GameManager.IsCreated && GameManager.Instance.CurrentState == GameState.BossPhase && _battleStarted && !IsDefeated;

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnState;
            GameEvents.OnMatchCompleted += OnMatch;
            ItemEffectEvents.OnPowerUp += GrantAttackBuff;
        }
        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnState;
            GameEvents.OnMatchCompleted -= OnMatch;
            ItemEffectEvents.OnPowerUp -= GrantAttackBuff;
            AbortBattle();
        }
        private void OnState(GameState state)
        {
            if (state == GameState.Paused || state == GameState.ColorSelection) return;
            if (state == GameState.BossPhase)
            {
                if (!(GameManager.Instance?.IsResuming ?? false) && enableCombat && !_battleStarted) StartBattle();
            }
            else
            {
                if (GameManager.Instance?.IsResuming ?? false) return;
                AbortBattle();
                if (state == GameState.Briefing || state == GameState.Result)
                { PendingAttackBonus = 0f; HasFailureShield = false; }
                if (state == GameState.Briefing) { IsDefeated = false; CurrentHP = MaxHP = 0f; }
            }
        }
        private void StartBattle()
        {
            AbortBattle();
            if (config == null || balance == null || timeAttackPattern == null)
            { Debug.LogError("[BossController] 보스 데이터/타임어택 참조 누락"); return; }
            _battleStarted = true;
            IsDefeated = false;
            MaxHP = CurrentHP = config.maxHP;
            PhaseIndex = 0;
            // 주행에서 얻은 다음 보스 공격 버프/실패 방어권은 유지한다.
            GameEvents.TriggerBossHPChanged(CurrentHP,MaxHP);
            GameEvents.TriggerBossPhaseChanged(0);
            StartPattern();
        }
        private void AbortBattle()
        {
            _battleStarted = false;
            _groggyPending = false;
            _nextSequence=null;
            timeAttackPattern?.Cancel();
            rhythmNotePattern?.Cancel(); colorQuotaPattern?.Cancel(); memorySequencePattern?.Cancel();
            IsGroggy = false;
            GroggyRemaining = _transitionRemaining = 0f;
            _state = IsDefeated ? BossState.Dead : BossState.Idle;
            FindFirstObjectByType<PuzzleBoardManager>()?.SetRequiredMatchColor(null);
        }
        private void StartPattern()
        {
            if (!IsActive) return;
            _state = BossState.Pattern;
            GameEvents.TriggerBossPatternStarted(BossBattleType.TimeAttack);
            timeAttackPattern.PrepareSequence(_nextSequence);
            _nextSequence=null;
            timeAttackPattern.Run(config,balance,PhaseIndex,() => ResolvePattern(true),() => ResolvePattern(false));
        }
        private void ResolvePattern(bool success)
        {
            if (!IsActive || _state != BossState.Pattern) return;
            FindFirstObjectByType<PuzzleBoardManager>()?.SetRequiredMatchColor(null);
            if (success)
            {
                // 마지막 패턴 입력 자체가 그로기 공격으로 다시 처리되지 않도록 다음 프레임에 연다.
                _groggyPending = true;
                _state = BossState.Interval;
                GameEvents.TriggerBossPatternResult(true);
            }
            else
            {
                _state = BossState.Interval;
                _transitionRemaining = config.patternInterval;
                _nextSequence=TimeAttackPattern.CreateSequence(config,PhaseIndex);
                bool shield = HasFailureShield;
                HasFailureShield = false;
                GameEvents.TriggerBossPatternResult(false); // 실패당 정확히 한 번
                if (!shield) StaminaManager.Instance?.TakeDamage(config.failureStaminaDamage);
            }
            if(IsActive) SaveManager.Instance?.RequestBoundary(success ? CheckpointBoundary.BossSuccess : CheckpointBoundary.BossFailure);
        }
        private void Update()
        {
            if (!IsActive) return;
            if (_groggyPending)
            {
                _groggyPending = false;
                IsGroggy = true;
                _state = BossState.Groggy;
                GroggyRemaining = config.GetGroggyDuration(balance);
                GameEvents.TriggerBossGroggyStarted(GroggyRemaining);
                return;
            }
            if (TimersFrozen) return;
            if (_state == BossState.Groggy)
            {
                GroggyRemaining = Mathf.Max(0f,GroggyRemaining - Time.deltaTime);
                if (GroggyRemaining > 0f) return;
                IsGroggy = false;
                GameEvents.TriggerBossGroggyEnded();
                int phase = 0;
                foreach (float threshold in config.phaseHPThresholds)
                    if (CurrentHP / MaxHP <= threshold) phase++;
                if (phase > PhaseIndex)
                {
                    PhaseIndex = phase;
                    GameEvents.TriggerBossPhaseChanged(PhaseIndex);
                }
                _state = BossState.Interval;
                _transitionRemaining = config.patternInterval;
                _nextSequence=TimeAttackPattern.CreateSequence(config,PhaseIndex);
                SaveManager.Instance?.RequestBoundary(CheckpointBoundary.GroggyEnd);
            }
            else if (_state == BossState.Interval)
            {
                _transitionRemaining -= Time.deltaTime;
                if (_transitionRemaining <= 0f) StartPattern();
            }
        }
        public BossRecord CaptureBoundary()
        {
            return new BossRecord { active=_battleStarted, hp=CurrentHP,maxHP=MaxHP,phase=PhaseIndex,
                attackBonus=PendingAttackBonus,failureShield=HasFailureShield,
                nextStep=!_battleStarted ? BossNextStep.None : (IsGroggy || _groggyPending ? BossNextStep.Groggy : BossNextStep.Interval),
                groggyRemaining=_groggyPending ? config.GetGroggyDuration(balance) : IsGroggy ? Mathf.Max(0f,GroggyRemaining) : 0f,
                intervalRemaining=_state==BossState.Interval && !_groggyPending ? Mathf.Max(0f,_transitionRemaining) : 0f,
                nextSequence=_nextSequence==null ? new System.Collections.Generic.List<TileColor>() : new System.Collections.Generic.List<TileColor>(_nextSequence) };
        }
        public void RestoreBoundary(BossRecord record)
        {
            AbortBattle();IsDefeated=false;
            PendingAttackBonus=record.attackBonus;HasFailureShield=record.failureShield;
            if(!record.active) return;
            _battleStarted=true;CurrentHP=record.hp;MaxHP=record.maxHP;PhaseIndex=record.phase;
            _nextSequence=new System.Collections.Generic.List<TileColor>(record.nextSequence);
            IsGroggy=record.nextStep==BossNextStep.Groggy;
            _state=IsGroggy ? BossState.Groggy : BossState.Interval;
            GroggyRemaining=record.groggyRemaining;_transitionRemaining=record.intervalRemaining;
            GameEvents.TriggerBossHPChanged(CurrentHP,MaxHP);GameEvents.TriggerBossPhaseChanged(PhaseIndex);
            if(IsGroggy) GameEvents.TriggerBossGroggyStarted(GroggyRemaining);
        }
        /// <summary>05 세션용: 입력/연출/장애물과 독립된 보스 제한 시간(그로기 포함) 정지.</summary>
        public void FreezeTimersFor(float seconds)
        {
            if (CanUseItems && !float.IsNaN(seconds) && !float.IsInfinity(seconds))
                StaminaManager.Instance?.PauseDecay(seconds);
        }
        /// <summary>유효한 다음 공격까지 높은 버프만 보관. 청록/패턴 성공으로 소모하지 않는다.</summary>
        public void GrantAttackBuff(float bonus)
        {
            if (CanReceiveEffects && !float.IsNaN(bonus) && !float.IsInfinity(bonus))
                PendingAttackBonus = Mathf.Max(PendingAttackBonus,bonus);
        }
        private bool CanReceiveEffects => GameManager.Instance?.CurrentState == GameState.RunnerPhase || CanUseItems;
        public void GrantFailureShield()
        {
            if (CanReceiveEffects) HasFailureShield = true;
        }
        private void OnMatch(int connected,int score,int removed,TileColor color)
        {
            if (GameManager.Instance?.CurrentState == GameState.RunnerPhase && color == TileColor.Cyan && connected >= 2 && removed > 0)
                GrantFailureShield();
            TryApplyGroggyAttack(connected,removed,color);
        }
        // 05 세션은 매칭 결과를 한 번만 전달해야 한다. 확장 제거 수는 공격 계산에 사용하지 않는다.
        public bool TryApplyGroggyAttack(int connected,int removed,TileColor color)
        {
            if (!CanUseColorActions || connected < 2 || removed <= 0) return false;
            if (color == TileColor.Cyan) { GrantFailureShield(); return true; }
            float damage = balance.GetPrototypeGroggyDamage(connected,color,config.GetGroggyDamageMultiplier(balance),PendingAttackBonus);
            if (damage <= 0f) return false;
            PendingAttackBonus = 0f;
            ApplyDamage(damage);
            return true;
        }
        /// <summary>모든 피해 공급자가 공유. HP0 즉시 단일 승리, Pause/Result/중복 호출 거부.</summary>
        public void ApplyDamage(float amount)
        {
            if (!IsActive || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            CurrentHP = Mathf.Max(0f,CurrentHP - amount);
            bool lethal = CurrentHP <= 0f;
            if (lethal)
            {
                IsDefeated = true;
                _state = BossState.Dead;
                IsGroggy = false;
                timeAttackPattern.Cancel();
            }
            GameEvents.TriggerBossHPChanged(CurrentHP,MaxHP);
            if (lethal) GameManager.Instance?.TriggerStageClear();
        }
    }
}
