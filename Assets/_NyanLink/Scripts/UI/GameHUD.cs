using NineMood.Core;
using NineMood.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>
    /// 인게임 HUD.
    /// 상단(러너 뷰): 스태미나 바, 진행도 바
    /// 공통: 현재 점수
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("점수")]
        [SerializeField] private TextMeshProUGUI textScore;

        [Header("스태미나 (러너 HP)")]
        [SerializeField] private Slider sliderStamina;
        [SerializeField] private TextMeshProUGUI textStamina;

        [Header("진행도")]
        [SerializeField] private Slider sliderProgress;

        private void OnEnable()
        {
            GameEvents.OnScoreChanged    += UpdateScore;
            GameEvents.OnStaminaChanged  += UpdateStamina;
            GameEvents.OnProgressChanged += UpdateProgress;
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnScoreChanged    -= UpdateScore;
            GameEvents.OnStaminaChanged  -= UpdateStamina;
            GameEvents.OnProgressChanged -= UpdateProgress;
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Start()
        {
            if (textScore != null) textScore.text = "0";

            // 스태미나 초기값
            float max = DataManager.Instance?.BalanceData?.maxStamina ?? 100f;
            UpdateStamina(max, max);
            UpdateProgress(0f);
        }

        private void UpdateScore(int current, int delta)
        {
            if (textScore != null)
                textScore.text = current.ToString("N0");
        }

        private void UpdateStamina(float current, float max)
        {
            if (sliderStamina != null)
            {
                sliderStamina.maxValue = max;
                sliderStamina.value    = current;
            }
            if (textStamina != null)
                textStamina.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        private void UpdateProgress(float progress)
        {
            if (sliderProgress != null)
                sliderProgress.value = progress;
        }

        private void OnGameStateChanged(GameState state) { }
    }
}
