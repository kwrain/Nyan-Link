using UnityEngine;
using NyanLink.Data.Enums;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 타일별 효과 수치 데이터 (아이템 효과 6종 공통 레벨 데이터)
    /// - 현재는 스태미너 회복량만 수치로 관리하고,
    ///   나머지 범위/라인 형태는 코드 레벨 정의로 처리한다.
    /// </summary>
    [CreateAssetMenu(fileName = "TileEffectData", menuName = "NyanLink/Data/Tile Effect", order = 2)]
    public class TileEffectData : ScriptableObject
    {
        [System.Serializable]
        public class EffectLevel
        {
            [Tooltip("효과 레벨 (Lv.1 또는 Lv.2)")]
            public int level = 1;

            [Tooltip("스태미너 추가 회복량 (StaminaBoost 타일)")]
            public float staminaAmount = 10f;
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
        /// 아이템 타입에 따른 수치형 효과 값 가져오기.
        /// 현재는 스태미너 추가 회복만 수치로 관리하며,
        /// 나머지(범위/라인/레인보우)는 타일 위치 기반 로직에서 처리한다.
        /// </summary>
        public float GetEffectValue(ItemEffectType effectType, int level)
        {
            EffectLevel effect = GetEffectLevel(level);

            return effectType switch
            {
                ItemEffectType.StaminaBoost => effect.staminaAmount,
                _ => 0f
            };
        }
    }
}
