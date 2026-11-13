namespace NineMood.Data.Enums
{
    /// <summary>
    /// 아이템 효과 타입 — 타일 색상별 1:1 대응
    /// 수치는 BalanceData, 비주얼 참조는 TileEffectData에서 관리
    /// </summary>
    public enum ItemEffectType
    {
        None,        // 아이템 없음 (일반 타일)
        Recovery,    // Red   — 스태미나 즉시 회복
        TimeFreeze,  // Blue  — 스태미나 감소 / 타이머 정지
        Blast,       // Yellow — 주변 N칸 범위 파괴
        LineClear,   // Purple — 라인 제거 (Lv1: 1줄, Lv2: 3방향)
        PowerUp,     // Orange — 다음 보스 공격 데미지 버프
        ColorShift   // Cyan  — 보드 전체 색상 통일
    }
}
