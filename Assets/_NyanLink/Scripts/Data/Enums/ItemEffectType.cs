namespace NyanLink.Data.Enums
{
    /// <summary>
    /// 아이템 효과 타입 (Phase 3 기준 6종)
    /// </summary>
    public enum ItemEffectType
    {
        /// <summary>주변 타일 파괴 (범위 폭발)</summary>
        AreaBlast,

        /// <summary>가로 타일 파괴 (Y축 동일 라인)</summary>
        HorizontalLine,

        /// <summary>왼쪽 대각선 타일 파괴 (좌상-우하 방향)</summary>
        DiagonalLeft,

        /// <summary>오른쪽 대각선 타일 파괴 (우상-좌하 방향)</summary>
        DiagonalRight,

        /// <summary>레인보우 타일 생성/전체 제거</summary>
        Rainbow,

        /// <summary>스태미너 추가 회복</summary>
        StaminaBoost
    }
}
