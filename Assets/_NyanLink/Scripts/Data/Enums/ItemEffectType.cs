namespace NyanLink.Data.Enums
{
    /// <summary>
    /// 아이템 효과 타입
    /// </summary>
    public enum ItemEffectType
    {
        Recovery,    // 회복 - 스태미나 즉시 회복
        TimeFreeze,  // 시간 정지 - 스태미나 감소/타이머 정지
        Blast,       // 폭발 - 주변 범위 타일 파괴
        LineClear,   // 라인 클리어 - 가로/대각선 라인 제거
        PowerUp,     // 파워업 - 다음 보스 공격 대미지 증가
        Rainbow      // 레인보우 - Rainbow Tile 생성 또는 Rainbow Bomb 발동
    }
}
