using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using UnityEngine;

namespace NineMood.Runner
{
    /// <summary>
    /// 스테이지 진행도 추적 — 거리 기반 (기획서02 §3.2).
    /// currentDistance += speed × dt, progress = dist / stageLength.
    /// ObstacleSpawner가 CurrentDistance·StageLength를 구독한다.
    /// </summary>
    public class RunnerProgressSystem : MonoBehaviour
    {
        [Header("스테이지 데이터 (null = BalanceData 기본값)")]
        public StageData stageData;

        public float CurrentDistance { get; private set; }
        public float CurrentProgress { get; private set; }
        public float StageLength     => _stageLength;
        public float RunnerSpeed     => _runnerSpeed;
        public bool  IsPaused        { get; private set; }

        private float _stageLength;
        private float _runnerSpeed;
        private bool  _active;
        private bool  _bossFightTriggered;

        private BalanceData Balance => DataManager.Instance?.BalanceData;

        private void OnEnable()  => GameEvents.OnGameStateChanged += OnGameStateChanged;
        private void OnDisable() => GameEvents.OnGameStateChanged -= OnGameStateChanged;

        public void SetPaused(bool paused) => IsPaused = paused;

        /// <summary>체크포인트 복원 — SaveManager가 StartGame 직후 호출한다.</summary>
        public void RestoreDistance(float distance)
        {
            if (_stageLength <= 0f) return;
            CurrentDistance = distance;
            CurrentProgress = Mathf.Clamp01(distance / _stageLength);
            GameEvents.TriggerProgressChanged(CurrentProgress);
        }

        private void Update()
        {
            if (!_active || _bossFightTriggered || IsPaused) return;

            CurrentDistance += _runnerSpeed * Time.deltaTime;
            CurrentProgress  = Mathf.Clamp01(CurrentDistance / _stageLength);
            GameEvents.TriggerProgressChanged(CurrentProgress);

            if (CurrentDistance >= _stageLength)
            {
                _bossFightTriggered = true;
                GameManager.Instance?.TriggerBossFight();
            }
        }

        private void ResetProgress()
        {
            CurrentDistance     = 0f;
            CurrentProgress     = 0f;
            _bossFightTriggered = false;
            IsPaused            = false;
            GameEvents.TriggerProgressChanged(0f);
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Briefing)
            {
                _stageLength = stageData != null && stageData.stageLength > 0f
                    ? stageData.stageLength
                    : 1000f;

                _runnerSpeed = stageData != null && stageData.runnerSpeed > 0f
                    ? stageData.runnerSpeed
                    : (Balance?.runnerBaseSpeed ?? 5f);

                ResetProgress();
            }

            _active = state == GameState.RunnerPhase;
            if (state == GameState.RunnerPhase)
                IsPaused = false;   // Briefing 없이 RetryGame() 직행 시 잔류 방지
        }
    }
}
