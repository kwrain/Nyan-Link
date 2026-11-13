using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Core;
using UnityEngine;

namespace NineMood.Core.Managers
{
    /// <summary>
    /// 점수 계산 및 최고 점수 관리. PlayerPrefs로 최고 점수를 저장한다.
    /// </summary>
    public class ScoreManager : SceneSingleton<ScoreManager>
    {
        private const string BestScoreKey = "NyanLink_BestScore";

        public int CurrentScore { get; private set; }
        public int BestScore    { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (!IsOwner) return;
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        }

        /// <summary>
        /// 실제 제거 수 × 기본 점수 × 직접 연결 수의 티어 배율. effectLevel은 구형 호출 호환용.
        /// 연속 콤보/치명타를 사용하지 않는다.
        /// </summary>
        public void AddScore(int chainLength, int effectLevel, int tilesRemoved, TileColor chainColor, bool isItemUse = false)
        {
            BalanceData balance  = DataManager.Instance?.BalanceData;
            int   baseScore      = balance != null ? balance.baseScorePerTile : 10;
            float multiplier     = isItemUse ? 1f : (balance != null ? balance.GetChainMultiplier(chainLength) : chainLength >= 9 ? 4 : chainLength >= 5 ? 2 : 1); // 직접 연결 수의 체인 티어

            if (tilesRemoved < 0 || !GameManager.IsCreated || (GameManager.Instance.CurrentState != GameState.RunnerPhase && GameManager.Instance.CurrentState != GameState.BossPhase)) return;
            int gained = Mathf.RoundToInt(tilesRemoved * baseScore * multiplier);
            CurrentScore += gained;

            Debug.Log($"[ScoreManager] 체인 {chainLength}개 / Lv{effectLevel} / 타일 {tilesRemoved}개 " +
                      $"→ {tilesRemoved} × {baseScore} × {multiplier}x = +{gained} (누적: {CurrentScore})");

            if (CurrentScore > BestScore)
            {
                BestScore = CurrentScore;
                PlayerPrefs.SetInt(BestScoreKey, BestScore);
                PlayerPrefs.Save();
            }

            GameEvents.TriggerScoreChanged(CurrentScore, gained);
            if (isItemUse) GameEvents.TriggerItemUseCompleted(gained,tilesRemoved);
            else GameEvents.TriggerMatchCompleted(chainLength, gained, tilesRemoved, chainColor);
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            GameEvents.TriggerScoreChanged(0, 0);
        }

        /// <summary>체크포인트 복원 — SaveManager가 StartGame 직후 호출한다.</summary>
        public void RestoreScore(int score)
        {
            CurrentScore = score;
            GameEvents.TriggerScoreChanged(CurrentScore, 0);
        }
    }
}
