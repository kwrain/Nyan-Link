using UnityEngine;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 밸런스 수치 데이터 (체인 티어, 아이템 타일 제한 등)
    /// Phase 3: 체인 티어 판정 수치를 조절 가능하게 관리
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceData", menuName = "NyanLink/Data/Balance", order = 0)]
    public class BalanceData : ScriptableObject
    {
        [Header("체인 티어 판정 (밸런스 조절 가능)")]
        [Tooltip("Middle Chain 최소 개수 (이 개수 이상이어야 Lv.1 아이템 타일 생성)")]
        [Min(2)]
        public int middleChainMin = 5;

        [Tooltip("Middle Chain 최대 개수 (이 개수 이하일 때 Lv.1)")]
        [Min(2)]
        public int middleChainMax = 8;

        [Tooltip("Long Chain 최소 개수 (이 개수 이상이면 Lv.2 아이템 타일 생성)")]
        [Min(2)]
        public int longChainMin = 9;

        [Header("아이템 타일 제한")]
        [Tooltip("타일맵 전체에서 아이템 효과를 가진 타일 최대 개수 (종류 무관 합계)")]
        [Min(0)]
        public int maxItemTilesOnBoard = 2;

        /// <summary>
        /// 체인 길이에 따른 효과 레벨 (0 = 아이템 타일 미생성, 1 = Lv.1, 2 = Lv.2)
        /// </summary>
        public int GetEffectLevelForChainLength(int chainLength)
        {
            if (chainLength >= longChainMin)
                return 2;
            if (chainLength >= middleChainMin && chainLength <= middleChainMax)
                return 1;
            return 0;
        }

        /// <summary>
        /// 해당 체인 길이로 아이템 타일을 생성할 수 있는지
        /// </summary>
        public bool CanCreateItemTileForChainLength(int chainLength)
        {
            return GetEffectLevelForChainLength(chainLength) > 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (middleChainMax < middleChainMin)
                middleChainMax = middleChainMin;
            if (longChainMin <= middleChainMax)
                longChainMin = middleChainMax + 1;
        }
#endif
    }
}
