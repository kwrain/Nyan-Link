using UnityEngine;
using NineMood.Data.Enums;
using System.Collections.Generic;

namespace NineMood.Data.Definitions
{
    /// <summary>
    /// 보스 전투 설정 데이터 (2026-06-24 확정)
    /// Groggy, Phase Change 타이밍, 패턴별 실패 판정 포함
    /// </summary>
    [CreateAssetMenu(fileName = "BossBattleConfig", menuName = "NyanLink/Data/Boss Battle Config", order = 6)]
    public class BossBattleConfig : ScriptableObject
    {
        // ── 기본 정보 ───────────────────────────────────────────────────
        [Header("보스 기본 정보")]
        public string bossId;
        public string bossName;
        [Min(1f)] public float maxHP = 1000f;

        // ── 전투 타입 ───────────────────────────────────────────────────
        [Header("전투 타입")]
        public BossBattleType battleType = BossBattleType.TimeAttack;

        // ── 패턴 공통 설정 ──────────────────────────────────────────────
        [Header("패턴 공통")]
        [Tooltip("패턴 시연 시간 (초) — 타임어택/메모리시퀀스 색상 노출 시간")]
        [Min(0f)] public float patternShowDuration = 5f;

        [Tooltip("입력 제한 시간 (초)")]
        [Min(0f)] public float inputTimeLimit = 10f;

        [Tooltip("패턴 파훼 시 보스에게 즉시 입히는 데미지 (Groggy 전 선공격)")]
        [Min(0f)] public float successDamage = 100f;

        [Tooltip("패턴 실패 시 스태미나 차감량 (BossPatternDamageValue)")]
        [Min(0f)] public float failureStaminaDamage = 20f;

        // ── Groggy ──────────────────────────────────────────────────────
        [Header("Groggy")]
        [Tooltip("Groggy 지속 시간 (초). 0이면 BalanceData.groggyDuration 사용")]
        [Min(0f)] public float groggyDuration = 0f;

        [Tooltip("Groggy 데미지 배율. 0이면 BalanceData.groggyDamageMultiplier 사용")]
        [Min(0f)] public float groggyDamageMultiplier = 0f;

        // ── Phase Change ─────────────────────────────────────────────────
        [Header("Phase Change")]
        [Tooltip("Phase 전환 HP 임계값 (0~1). 패턴 종료 후 HP가 해당 비율 이하 시 전환. 오름차순 정렬 불필요")]
        public List<float> phaseHPThresholds = new List<float> { 0.5f, 0.2f };

        // ── 패턴별 설정 ─────────────────────────────────────────────────
        [Header("TimeAttack — 색상 시퀀스")]
        [Tooltip("색상 시퀀스 길이 (각 색상당 2개 이상 제거 필요). 오순서 입력 시 즉시 실패")]
        [Min(1)] public int timeAttackSequenceLength = 5;

        [Header("RhythmNote — 노트")]
        [Tooltip("노트 낙하 속도")]
        [Min(0.1f)] public float rhythmNoteSpeed = 1f;

        [Tooltip("누적 Miss 허용 횟수. 초과 시 실패(피격). 0이면 BalanceData.defaultMaxMissCount 사용")]
        [Min(0)] public int maxMissCount = 0;

        [Header("ColorQuota — 색상별 목표")]
        [Tooltip("색상별 제거 목표 개수. 시간 내 미달 시 이진 실패 (부분 성공 없음)")]
        public SerializableDictionary<TileColor, int> colorQuotaTargets = new SerializableDictionary<TileColor, int>();

        [Header("MemorySequence — 기억 시퀀스")]
        [Tooltip("기억해야 할 색상 순서 길이. 오입력 시 즉시 실패")]
        [Min(1)] public int memorySequenceLength = 4;

        // ── 편의 메서드 ─────────────────────────────────────────────────

        /// <summary>Groggy 지속 시간 — 0이면 BalanceData 폴백</summary>
        public float GetGroggyDuration(BalanceData balance) =>
            groggyDuration > 0f ? groggyDuration : balance.groggyDuration;

        /// <summary>Groggy 데미지 배율 — 0이면 BalanceData 폴백</summary>
        public float GetGroggyDamageMultiplier(BalanceData balance) =>
            groggyDamageMultiplier > 0f ? groggyDamageMultiplier : balance.groggyDamageMultiplier;

        /// <summary>RhythmNote Miss 허용 횟수 — 0이면 BalanceData 폴백</summary>
        public int GetMaxMissCount(BalanceData balance) =>
            maxMissCount > 0 ? maxMissCount : balance.defaultMaxMissCount;
    }

    /// <summary>Inspector에서 Dictionary 표시용 래퍼</summary>
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue>
        : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey>   keys   = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            if (keys.Count != values.Count)
            {
                Debug.LogError($"[SerializableDictionary] 키/값 개수 불일치: {keys.Count} / {values.Count}");
                return;
            }
            for (int i = 0; i < keys.Count; i++)
                this[keys[i]] = values[i];
        }
    }
}
