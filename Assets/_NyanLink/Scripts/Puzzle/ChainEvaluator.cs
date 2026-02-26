using NyanLink.Data.Definitions;
using NyanLink.Data.Enums;

namespace NyanLink.Puzzle
{
    /// <summary>
    /// 체인 길이에 따른 티어 판정.
    /// 밸런스 수치는 BalanceData에서 조절 가능 (Middle/Long 기준값).
    /// </summary>
    public static class ChainEvaluator
    {
        /// <summary>
        /// 체인 길이에 따른 효과 레벨 반환.
        /// 0 = 아이템 타일 미생성, 1 = Lv.1 (Middle), 2 = Lv.2 (Long).
        /// </summary>
        /// <param name="balanceData">밸런스 데이터 (null이면 0 반환)</param>
        /// <param name="chainLength">체인 길이</param>
        public static int GetEffectLevel(BalanceData balanceData, int chainLength)
        {
            if (balanceData == null || chainLength <= 0)
                return 0;
            return balanceData.GetEffectLevelForChainLength(chainLength);
        }

        /// <summary>
        /// 해당 체인 길이로 아이템 타일을 생성할 수 있는지
        /// </summary>
        public static bool CanCreateItemTile(BalanceData balanceData, int chainLength)
        {
            return GetEffectLevel(balanceData, chainLength) > 0;
        }

        /// <summary>
        /// 효과 레벨을 TileState로 변환 (1 → ItemLv1, 2 → ItemLv2)
        /// </summary>
        public static TileState EffectLevelToTileState(int effectLevel)
        {
            return effectLevel >= 2 ? TileState.ItemLv2 : effectLevel >= 1 ? TileState.ItemLv1 : TileState.Normal;
        }
    }
}
