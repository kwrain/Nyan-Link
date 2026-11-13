using System;
using NineMood.Data.Enums;

namespace NineMood.Core
{
    // ── Phase 4 Boss 이벤트용 ──────────────────────────────────────────
    // (BossController가 Trigger, BossHUD/구독자가 수신)

    /// <summary>
    /// 기획서 05 확정 상태 머신: Briefing → RunnerPhase → BossPhase → Result
    /// </summary>
    public enum GameState
    {
        Briefing,    // 스테이지 브리핑 / 시작 대기
        RunnerPhase, // 러너 + 퍼즐 진행 중
        BossPhase,   // 보스 전투 중
        Paused,      // 일시정지
        Result,      // 결과 화면 (승/패 공통) — ResultType으로 구분
        ColorSelection // 청록 선택. 별도의 사용자 Pause가 이 상태 위에 중첩될 수 있다.
    }

    /// <summary>
    /// Result 상태의 승패 구분. OnResultSet 이벤트와 함께 전달된다.
    /// </summary>
    public enum ResultType
    {
        Win,  // 보스 처치 완료
        Lose  // 스태미나 소진
    }

    /// <summary>
    /// 게임 루프 이벤트 허브. 구독자는 반드시 OnDisable에서 구독 해제해야 한다.
    /// </summary>
    public static class GameEvents
    {
        // ── 게임 상태 ──────────────────────────────────────────
        public static event Action<GameState> OnGameStateChanged;

        /// <summary>Result 상태 진입 시 승/패 타입을 함께 전달. OnGameStateChanged 직후 발행.</summary>
        public static event Action<ResultType> OnResultSet;

        // ── 점수 ───────────────────────────────────────────────
        public static event Action<int, int> OnScoreChanged;  // (current, delta)

        /// <summary>
        /// 타일 체인 매칭 완료. chainColor는 RunnerController가 액션 결정에 사용한다.
        /// </summary>
        public static event Action<int, int, int, TileColor> OnMatchCompleted;
        // (chainLength, scoreGained, tilesRemoved, chainColor)

        // ── 러너 ───────────────────────────────────────────────
        /// <summary>스태미나 변경 (current, max)</summary>
        public static event Action<float, float> OnStaminaChanged;
        public static event Action<float> OnPlayerHit;
        public static void TriggerPlayerHit(float amount)=>OnPlayerHit?.Invoke(amount);

        /// <summary>스테이지 진행도 변경 (0~1)</summary>
        public static event Action<float> OnProgressChanged;

        /// <summary>보스 등장 (진행도 100% 도달)</summary>
        public static event Action OnBossAppeared;
        public static event Action<int,int> OnItemUseCompleted; // score, 실제 제거 수. 색상 액션/패턴 입력 아님
        public static void TriggerItemUseCompleted(int score,int removed) => OnItemUseCompleted?.Invoke(score,removed);

        // ── 보스전 ─────────────────────────────────────────────
        /// <summary>보스 HP 변경 (current, max)</summary>
        public static event Action<float, float> OnBossHPChanged;

        /// <summary>Groggy 돌입 — duration(초) 전달</summary>
        public static event Action<float> OnBossGroggyStarted;

        /// <summary>Groggy 종료</summary>
        public static event Action OnBossGroggyEnded;

        /// <summary>Phase 전환 — 새 phaseIndex 전달 (0-based)</summary>
        public static event Action<int> OnBossPhaseChanged;

        /// <summary>패턴 시작 — 전투 타입 전달</summary>
        public static event Action<BossBattleType> OnBossPatternStarted;

        /// <summary>패턴 결과 — success 여부 전달</summary>
        public static event Action<bool> OnBossPatternResult;

        /// <summary>패턴 진행 중 안내 텍스트 갱신</summary>
        public static event Action<string> OnBossPatternInstructionChanged;

        // ── 트리거 메서드 ───────────────────────────────────────
        public static void TriggerGameStateChanged(GameState state) =>
            OnGameStateChanged?.Invoke(state);

        public static void TriggerResultSet(ResultType result) =>
            OnResultSet?.Invoke(result);

        public static void TriggerScoreChanged(int current, int delta) =>
            OnScoreChanged?.Invoke(current, delta);

        public static void TriggerMatchCompleted(int chainLength, int scoreGained, int tilesRemoved, TileColor chainColor) =>
            OnMatchCompleted?.Invoke(chainLength, scoreGained, tilesRemoved, chainColor);

        public static void TriggerStaminaChanged(float current, float max) =>
            OnStaminaChanged?.Invoke(current, max);

        public static void TriggerProgressChanged(float progress) =>
            OnProgressChanged?.Invoke(progress);

        public static void TriggerBossAppeared() =>
            OnBossAppeared?.Invoke();

        public static void TriggerBossHPChanged(float current, float max) =>
            OnBossHPChanged?.Invoke(current, max);

        public static void TriggerBossGroggyStarted(float duration) =>
            OnBossGroggyStarted?.Invoke(duration);

        public static void TriggerBossGroggyEnded() =>
            OnBossGroggyEnded?.Invoke();

        public static void TriggerBossPhaseChanged(int phaseIndex) =>
            OnBossPhaseChanged?.Invoke(phaseIndex);

        public static void TriggerBossPatternStarted(BossBattleType type) =>
            OnBossPatternStarted?.Invoke(type);

        public static void TriggerBossPatternResult(bool success) =>
            OnBossPatternResult?.Invoke(success);

        public static void TriggerBossPatternInstructionChanged(string instruction) =>
            OnBossPatternInstructionChanged?.Invoke(instruction);
    }
}
