using UnityEngine;
using NyanLink.Data.Enums;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 타일별 효과 수치 데이터
    /// 회복량, 지속시간 등의 효과 수치를 정의
    /// </summary>
    [CreateAssetMenu(fileName = "TileEffectData", menuName = "NyanLink/Data/Tile Effect", order = 2)]
    public class TileEffectData : ScriptableObject
    {
        [System.Serializable]
        public class EffectLevel
        {
            [Tooltip("효과 레벨 (Lv.1 또는 Lv.2)")]
            public int level = 1;

            [Tooltip("회복량 (Recovery 타일)")]
            public float recoveryAmount = 10f;

            [Tooltip("시간 정지 지속시간 (초) (TimeFreeze 타일)")]
            public float timeFreezeDuration = 3f;

            [Tooltip("폭발 범위 반경 (Blast 타일)")]
            public int blastRadius = 1;

            [Tooltip("라인 클리어 범위 (LineClear 타일)")]
            public int lineClearRange = 3;

            [Tooltip("파워업 대미지 배율 (PowerUp 타일)")]
            public float powerUpMultiplier = 1.5f;
        }

        [Header("효과 레벨별 수치")]
        [Tooltip("Lv.1 효과 (Middle Chain: 5~8개)")]
        public EffectLevel level1 = new EffectLevel { level = 1 };

        [Tooltip("Lv.2 효과 (Long Chain: 9개 이상)")]
        public EffectLevel level2 = new EffectLevel { level = 2 };

        /// <summary>
        /// 특정 레벨의 효과 수치 가져오기
        /// </summary>
        public EffectLevel GetEffectLevel(int level)
        {
            return level >= 2 ? level2 : level1;
        }

        /// <summary>
        /// 아이템 타입에 따른 효과 수치 가져오기
        /// </summary>
        public float GetEffectValue(ItemEffectType effectType, int level)
        {
            EffectLevel effect = GetEffectLevel(level);

            return effectType switch
            {
                ItemEffectType.Recovery => effect.recoveryAmount,
                ItemEffectType.TimeFreeze => effect.timeFreezeDuration,
                ItemEffectType.Blast => effect.blastRadius,
                ItemEffectType.LineClear => effect.lineClearRange,
                ItemEffectType.PowerUp => effect.powerUpMultiplier,
                _ => 0f
            };
        }
    }
}
