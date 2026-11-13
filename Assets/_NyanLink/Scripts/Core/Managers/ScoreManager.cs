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
        /// 매칭 결과로 점수 추가. 기획서 공식: (tilesRemoved × ScoreBase) × Pow(2, effectLevel)
        /// effectLevel: 0=Lv0(1x), 1=Middle(2x), 2=Long(4x)
        /// </summary>
        public void AddScore(int chainLength, int effectLevel, int tilesRemoved, TileColor chainColor)
        {
            BalanceData balance  = DataManager.Instance?.BalanceData;
            int   baseScore      = balance != null ? balance.baseScorePerTile : 10;
            float multiplier     = Mathf.Pow(2f, effectLevel); // 기획서: Pow(2, Combo)

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
            GameEvents.TriggerMatchCompleted(chainLength, gained, tilesRemoved, chainColor);
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
