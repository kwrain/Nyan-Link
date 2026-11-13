using UnityEngine;
using NineMood.Data.Enums;

namespace NineMood.Data.Definitions
{
    /// <summary>
    /// 전 시스템 공통 수치 데이터 — 기획 확정값 기준 (2026-06-24)
    /// 스테이지별 오버라이드는 StageData / BossBattleConfig에서 처리
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceData", menuName = "NyanLink/Data/Balance", order = 0)]
    public class BalanceData : ScriptableObject
    {
        // ── 체인 티어 ──────────────────────────────────────────────────
        [Header("체인 티어 판정")]
        [Tooltip("Middle Chain 최솟값 — 이 이상이면 Lv.1 아이템 타일 생성")]
        [Min(2)] public int middleChainMin = 5;

        [Tooltip("Middle Chain 최댓값 — 이 이하이면 Lv.1")]
        [Min(2)] public int middleChainMax = 8;

        [Tooltip("Long Chain 최솟값 — 이 이상이면 Lv.2 아이템 타일 생성")]
        [Min(2)] public int longChainMin = 9;

        // ── 아이템 타일 제한 ────────────────────────────────────────────
        [Header("아이템 타일 제한")]
        [Tooltip("동일 ItemEffectType의 보드 최대 개수. 0 = 무제한. 기획서 기본값: 2")]
        [Min(0)] public int maxItemsPerType = 2;

        // ── 점수 ────────────────────────────────────────────────────────
        [Header("점수 계산")]
        [Tooltip("타일 1개 제거 기본 점수")]
        [Min(1)] public int baseScorePerTile = 10;
        // 체인 배율은 Pow(2, effectLevel): Lv0=1x, Lv1=2x, Lv2=4x

        // ── 스태미나 ────────────────────────────────────────────────────
        [Header("스태미나")]
        [Min(1f)] public float maxStamina = 100f;

        [Tooltip("초당 자연 감소량 (HP/sec). 실제 = baseDecayRate × StageData.stageDecayMultiplier")]
        [Min(0f)] public float baseDecayRate = 0.15f;

        [Tooltip("타일 1개 제거 시 회복량. 실제 = value × CharacterData.recovery")]
        [Min(0f)] public float staminaRecoveryPerTile = 0.3f;

        [Tooltip("아케이드 모드 전용 — 매 초마다 decayRate에 선형 가산")]
        [Min(0f)] public float arcadeDecayIncreasePerSec = 0.001f;

        // ── 러너 이동 ───────────────────────────────────────────────────
        [Header("러너 이동")]
        [Tooltip("기본 이동 속도 (거리 단위/초). StageData.runnerSpeed가 0이면 이 값 사용")]
        [Min(0.1f)] public float runnerBaseSpeed = 5f;

        [Tooltip("Purple(Run) 발동 시 속도 배율")]
        [Min(1f)] public float runSpeedMultiplier = 1.5f;

        [Tooltip("Purple(Run) 지속 시간 (초)")]
        [Min(0f)] public float runSpeedDuration = 3f;

        // ── 장애물 ──────────────────────────────────────────────────────
        [Header("장애물")]
        [Tooltip("예고 시작 거리 (거리 단위). 러너가 이 거리 이내 진입 시 입력 창 시작")]
        [Min(0f)] public float obstacleWarningDistance = 5f;

        [Tooltip("오입력 후 입력 잠금 시간 (초). 매 오입력마다 리셋")]
        [Min(0f)] public float mistakeLockDuration = 0.5f;

        // ── 러너 액션 지속 시간 ─────────────────────────────────────────
        [Header("러너 액션 지속 시간 (초)")]
        public float jumpDuration       = 0.8f;
        public float slideDuration      = 0.6f;
        public float blockDuration      = 0.5f;
        public float attackDuration     = 0.4f;
        public float invincibleDuration = 2f;
        public float stumbleDuration    = 1f;

        // ── 보스전 — Groggy ─────────────────────────────────────────────
        [Header("보스전 — Groggy")]
        [Tooltip("Groggy 지속 시간 (초). BossBattleConfig.groggyDuration > 0 이면 오버라이드")]
        [Min(0f)] public float groggyDuration = 5f;

        [Tooltip("Groggy 중 보스 데미지 배율")]
        [Min(1f)] public float groggyDamageMultiplier = 2f;

        [Tooltip("Groggy 데미지 계수. GroggyBossDamage = (count × ScoreBase) × Pow(2, combo) × factor")]
        [Min(0f)] public float groggyDamageFactor = 0.1f;

        // ── 보스전 — 공격 ───────────────────────────────────────────────
        [Header("보스전 — 공격")]
        [Tooltip("치명타 데미지 배율. 발동 확률은 CharacterData.criticalChance")]
        [Min(1f)] public float criticalMultiplier = 1.5f;

        [Tooltip("Orange PowerUp Lv1 버프. ActualDmg = base × (1 + bonus)")]
        [Range(0f, 5f)] public float powerUpBonusLv1 = 0.5f;

        [Tooltip("Orange PowerUp Lv2 버프")]
        [Range(0f, 5f)] public float powerUpBonusLv2 = 1.0f;

        // ── 보스전 — 리듬 노트 ──────────────────────────────────────────
        [Header("보스전 — 리듬 노트 기본값")]
        [Tooltip("누적 Miss 허용 횟수. BossBattleConfig.maxMissCount가 0이면 이 값 사용")]
        [Min(1)] public int defaultMaxMissCount = 3;

        // ── 아이템 수치 — Recovery (Red) ────────────────────────────────
        [Header("아이템 — Recovery (Red)")]
        [Range(0f, 1f)] public float healPercentLv1 = 0.10f;
        [Range(0f, 1f)] public float healPercentLv2 = 0.30f;

        // ── 아이템 수치 — TimeFreeze (Blue) ─────────────────────────────
        [Header("아이템 — TimeFreeze (Blue)")]
        [Min(0f)] public float freezeDurationLv1 = 3f;
        [Min(0f)] public float freezeDurationLv2 = 7f;

        // ── 아이템 수치 — Blast (Yellow) ────────────────────────────────
        [Header("아이템 — Blast (Yellow)")]
        [Tooltip("파괴 반경 (헥사 거리)")]
        [Min(0)] public int blastRangeLv1 = 1;
        [Min(0)] public int blastRangeLv2 = 2;

        // ── 아이템 수치 — LineClear (Purple) ────────────────────────────
        [Header("아이템 — LineClear (Purple)")]
        [Tooltip("Lv1: 가로 or 대각선 1줄")]
        public int lineClearCountLv1 = 1;
        [Tooltip("Lv2: 가로 + 양대각선 3방향")]
        public int lineClearCountLv2 = 3;

        [Header("프로토타입 공격력 (장비 연결 지점)")]
        [Min(0f)] public float attackCoefficient = 1f;

        public int GetChainMultiplier(int connected) => connected >= 9 ? 4 : connected >= 5 ? 2 : 1;
        public float GetPrototypeGroggyDamage(int connected, TileColor color, float groggyMultiplier, float bonus) =>
            color == TileColor.Cyan ? 0f : Mathf.Max(0,connected) * attackCoefficient * groggyMultiplier
                * (color == TileColor.Orange ? 2f : 1f) * (1f + Mathf.Max(0f,bonus));

        // ── 공개 메서드 ─────────────────────────────────────────────────

        /// <summary>체인 길이 → 아이템 효과 레벨 (0 = 미생성, 1 = Lv.1, 2 = Lv.2)</summary>
        public int GetEffectLevel(int chainLength)
        {
            if (chainLength >= longChainMin) return 2;
            if (chainLength >= middleChainMin && chainLength <= middleChainMax) return 1;
            return 0;
        }

        public bool CanCreateItemTile(int chainLength) => GetEffectLevel(chainLength) > 0;

        /// <summary>Orange PowerUp 버프 수치 반환</summary>
        public float GetPowerUpBonus(int itemLevel) => itemLevel switch
        {
            1 => powerUpBonusLv1,
            2 => powerUpBonusLv2,
            _ => 0f
        };

        /// <summary>Groggy 보스 데미지 계산</summary>
        public float GetGroggyDamage(int matchedCount, float comboMultiplier) =>
            matchedCount * baseScorePerTile * Mathf.Pow(2f, comboMultiplier) * groggyDamageFactor;

        /// <summary>공격 데미지 계산 (치명타 + PowerUp + Groggy 중첩)</summary>
        public float GetAttackDamage(float baseAttack, float powerUpBonus, bool isGroggy, bool isCritical)
        {
            float dmg = baseAttack * (1f + powerUpBonus);
            if (isGroggy)    dmg *= groggyDamageMultiplier;
            if (isCritical)  dmg *= criticalMultiplier;
            return dmg;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (middleChainMax < middleChainMin) middleChainMax = middleChainMin;
            if (longChainMin <= middleChainMax)  longChainMin  = middleChainMax + 1;
            if (powerUpBonusLv2 < powerUpBonusLv1) powerUpBonusLv2 = powerUpBonusLv1;
        }
#endif
    }
}
