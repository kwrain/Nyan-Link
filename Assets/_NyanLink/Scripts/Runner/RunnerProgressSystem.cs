using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using UnityEngine;

namespace NineMood.Runner
{
    public class RunnerProgressSystem : MonoBehaviour
    {
        public StageData stageData;
        public float CurrentDistance { get; private set; }
        public float CurrentProgress { get; private set; }
        public float StageLength { get; private set; }
        public float BaseRunnerSpeed { get; private set; }
        public float RunnerSpeed => BaseRunnerSpeed * (_runner != null ? _runner.SpeedMultiplier : 1f);
        public bool IsPaused { get; private set; }
        private bool _bossFightTriggered;
        private RunnerController _runner;
        private ObstacleSpawner _spawner;

        private void Awake()
        {
            _runner = FindFirstObjectByType<RunnerController>();
            _spawner = FindFirstObjectByType<ObstacleSpawner>();
        }
        private void OnEnable() => GameEvents.OnGameStateChanged += OnStateChanged;
        private void OnDisable() => GameEvents.OnGameStateChanged -= OnStateChanged;
        public void SetPaused(bool paused) => IsPaused = paused;

        public void RestoreDistance(float distance)
        {
            CurrentDistance = Mathf.Clamp(distance, 0f, StageLength);
            CurrentProgress = StageLength > 0f ? CurrentDistance / StageLength : 0f;
            GameEvents.TriggerProgressChanged(CurrentProgress);
        }

        private void Update()
        {
            if (!GameManager.IsCreated || GameManager.Instance.CurrentState != GameState.RunnerPhase ||
                _bossFightTriggered || IsPaused || StageLength <= 0f) return;
            float barrier = _spawner != null ? _spawner.NextStopDistance : StageLength;
            CurrentDistance = Mathf.Min(CurrentDistance + RunnerSpeed * Time.deltaTime, barrier, StageLength);
            CurrentProgress = CurrentDistance / StageLength;
            GameEvents.TriggerProgressChanged(CurrentProgress);
            if (CurrentDistance >= StageLength && _spawner != null && _spawner.AllObstaclesCleared)
            {
                _bossFightTriggered = true;
                GameManager.Instance.TriggerBossFight();
            }
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.Briefing) return; // 재개는 장애물 진행 정지 상태를 덮어쓰지 않는다.
            StageLength = stageData != null ? stageData.stageLength : 250f;
            BaseRunnerSpeed = stageData != null && stageData.runnerSpeed > 0f
                ? stageData.runnerSpeed : (DataManager.Instance?.BalanceData?.runnerBaseSpeed ?? 5f);
            _bossFightTriggered = false;
            IsPaused = false;
            RestoreDistance(0f);
        }
    }
}
