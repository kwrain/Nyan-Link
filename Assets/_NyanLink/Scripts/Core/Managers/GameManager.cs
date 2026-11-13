using NineMood.Data.Definitions;
using NineMood.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NineMood.Core.Managers
{
    /// <summary>
    /// 게임 상태 머신 — 기획서 05 확정 구조.
    /// Briefing → RunnerPhase → BossPhase → Result(Win)
    ///                       ↘ Result(Lose) ← 스태미나 소진
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.Briefing;

        /// <summary>현재 Result 상태의 승패 구분. Result 상태가 아닐 때는 의미 없음.</summary>
        public ResultType CurrentResult { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneLoader.SceneGame)
                SetState(GameState.Briefing);
        }

        // ── 공개 제어 메서드 ────────────────────────────────────

        public void StartGame()
        {
            ScoreManager.Instance?.ResetScore();
            StaminaManager.Instance?.ResetStamina();
            // Briefing을 항상 경유 — RunnerProgressSystem/ObstacleSpawner의 거리·인덱스·
            // _bossFightTriggered가 Briefing 핸들러에서만 리셋되므로, 직행 시 GameOver/StageClear
            // 이후 재시작이 이전 거리에서 멈추거나(softlock) 잘못된 위치에서 재개되는 문제가 있었음.
            SetState(GameState.Briefing);
            SetState(GameState.RunnerPhase);
            // 각 시스템의 리셋 이후 체크포인트가 있으면 덮어쓰기 복원
            SaveManager.Instance?.TryRestoreCheckpoint();
        }

        public void RetryGame()
        {
            SaveManager.Instance?.ClearCurrentCheckpoint();
            StartGame();
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.RunnerPhase || CurrentState == GameState.BossPhase)
            {
                Time.timeScale = 0f;
                SetState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                Time.timeScale = 1f;
                SetState(GameState.RunnerPhase);
            }
        }

        /// <summary>스태미나 소진 시 StaminaManager가 호출</summary>
        public void TriggerGameOver()
        {
            if (CurrentState != GameState.RunnerPhase && CurrentState != GameState.BossPhase) return;
            Time.timeScale = 1f;
            SetResult(ResultType.Lose);
        }

        /// <summary>진행도 100% 도달 시 RunnerProgressSystem이 호출</summary>
        public void TriggerBossFight()
        {
            if (CurrentState != GameState.RunnerPhase) return;
            SetState(GameState.BossPhase);
            GameEvents.TriggerBossAppeared();
        }

        /// <summary>보스 처치 시 BossController가 호출 (Phase 4)</summary>
        public void TriggerStageClear()
        {
            if (CurrentState != GameState.BossPhase) return;
            SaveManager.Instance?.ClearCurrentCheckpoint();
            SetResult(ResultType.Win);
        }

        // ── 내부 ───────────────────────────────────────────────

        private void SetState(GameState state)
        {
            CurrentState = state;
            Debug.Log($"[GameManager] 상태 전환 → {state}");
            GameEvents.TriggerGameStateChanged(state);
        }

        // Result 상태 진입 시 ResultType도 함께 발행
        private void SetResult(ResultType result)
        {
            CurrentResult = result;
            SetState(GameState.Result);
            GameEvents.TriggerResultSet(result);
        }
    }
}
