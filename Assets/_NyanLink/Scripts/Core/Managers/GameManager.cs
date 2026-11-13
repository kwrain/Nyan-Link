using NineMood.Puzzle;
using NineMood.Runner;
using NineMood.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NineMood.Core.Managers
{
    public class GameManager : SceneSingleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.Briefing;
        public ResultType CurrentResult { get; private set; }
        public GameState StateBeforePause { get; private set; } = GameState.Briefing;
        public bool IsResuming { get; private set; }
        public NineMood.Core.Persistence.EffectiveStats RunStats { get; private set; }
        private bool _bossAppeared;
        private GameState _stateBeforeColorChoice;

        protected override void Awake()
        {
            base.Awake();
            if (!IsOwner) return;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var debugPanel = FindFirstObjectByType<GameStateDebugPanel>();
            if (debugPanel != null) debugPanel.gameObject.SetActive(false);
            // 기존 씬 GUID와 계층을 유지하면서 최소 일시정지 UI를 연결한다.
            if (GetComponent<GamePausePanel>() == null) gameObject.AddComponent<GamePausePanel>();
            if (GetComponent<ColorSelectionPanel>() == null) gameObject.AddComponent<ColorSelectionPanel>();
            if (GetComponent<EquipmentPanel>() == null) gameObject.AddComponent<EquipmentPanel>();
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (IsOwner) Time.timeScale = 1f;
            base.OnDestroy();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene != gameObject.scene) return;
            Time.timeScale = 1f;
            PublishState(GameState.Briefing);
        }

        public void StartGame() => StartRun(true);
        public void RetryGame() => StartRun(false);
        private void StartRun(bool continueSaved)
        {
            Time.timeScale=1f;IsResuming=false;_bossAppeared=false;
            RunStats=NineMood.Core.Persistence.ProgressionService.Stats();
            PublishState(GameState.Briefing);
            FindFirstObjectByType<PuzzleBoardManager>()?.ResetBoard();
            ScoreManager.Instance?.ResetScore();StaminaManager.Instance?.ResetStamina();
            PublishState(GameState.RunnerPhase);
            if(continueSaved && (SaveManager.Instance?.TryRestoreCheckpoint() ?? false)) return;
            SaveManager.Instance?.BeginNewRun();
        }
        public void RestorePhase(GameState phase)
        {
            _bossAppeared=phase==GameState.BossPhase;
            IsResuming=true;PublishState(phase);IsResuming=false;
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.RunnerPhase && CurrentState != GameState.BossPhase && CurrentState != GameState.ColorSelection) return;
            StateBeforePause = CurrentState;
            Time.timeScale = 0f;
            PublishState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            Time.timeScale = StateBeforePause == GameState.ColorSelection ? 0f : 1f;
            IsResuming = true;
            PublishState(StateBeforePause);
            IsResuming = false;
        }

        public bool BeginColorChoice()
        {
            if (CurrentState == GameState.ColorSelection) return true;
            if (CurrentState != GameState.RunnerPhase && CurrentState != GameState.BossPhase) return false;
            _stateBeforeColorChoice = CurrentState;
            Time.timeScale = 0f;
            PublishState(GameState.ColorSelection);
            return true;
        }
        public void CompleteColorChoice()
        {
            if (CurrentState != GameState.ColorSelection) return;
            Time.timeScale = 1f;
            IsResuming = true;
            PublishState(_stateBeforeColorChoice);
            IsResuming = false;
        }

        public void TriggerGameOver()
        {
            if (CurrentState != GameState.RunnerPhase && CurrentState != GameState.BossPhase) return;
            SetResult(ResultType.Lose);
        }

        public void TriggerBossFight()
        {
            if (CurrentState != GameState.RunnerPhase || _bossAppeared) return;
            var obstacles = FindFirstObjectByType<ObstacleSpawner>();
            if (obstacles == null || !obstacles.AllObstaclesCleared) return;
            _bossAppeared = true;
            PublishState(GameState.BossPhase);
            GameEvents.TriggerBossAppeared();
        }

        public void TriggerStageClear()
        {
            if (CurrentState != GameState.BossPhase) return;
            SaveManager.Instance?.CompleteCurrentRun();
            if(SaveManager.Instance?.stageData!=null) NineMood.Core.Persistence.ProgressionService.RecoverPending();
            SetResult(ResultType.Win);
        }

        private void PublishState(GameState state)
        {
            CurrentState = state;
            GameEvents.TriggerGameStateChanged(state);
        }

        private void SetResult(ResultType result)
        {
            if(result==ResultType.Lose) SaveManager.Instance?.ClearCurrentCheckpoint();
            CurrentResult = result;
            Time.timeScale = 0f;
            PublishState(GameState.Result);
            GameEvents.TriggerResultSet(result);
        }
    }
}
