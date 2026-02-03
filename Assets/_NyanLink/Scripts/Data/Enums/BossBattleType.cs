namespace NyanLink.Data.Enums
{
    /// <summary>
    /// 보스 전투 타입
    /// </summary>
    public enum BossBattleType
    {
        TimeAttack,      // 시간 공격 - 순차 색상 입력
        RhythmNote,      // 리듬 노트 - 리듬 노트 판정
        ColorQuota,      // 색상 할당량 - 색상별 목표 개수
        MemorySequence   // 기억 시퀀스 - 순서 기억 입력
    }
}
