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

        private TextMeshProUGUI _chainTier;
        private TextMeshProUGUI _saveStatus;
        private void LateUpdate()
        { if(_saveStatus!=null) _saveStatus.text=(SaveManager.Instance?.HasWriteError ?? false) ? "SAVE UNAVAILABLE - last checkpoint retained" : ""; }
        private void Awake()
        {
            _chainTier = new GameObject("Chain Tier",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _chainTier.transform.SetParent(transform,false);
            var rect = _chainTier.rectTransform;
            rect.anchorMin = new Vector2(.2f,.89f); rect.anchorMax = new Vector2(.8f,.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (textScore != null) _chainTier.font = textScore.font;
            _chainTier.fontSize = 26; _chainTier.alignment = TextAlignmentOptions.Center;
            _chainTier.raycastTarget = false;
            _saveStatus=new GameObject("Save status",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();_saveStatus.transform.SetParent(transform,false);
            _saveStatus.rectTransform.anchorMin=new Vector2(.1f,.555f);_saveStatus.rectTransform.anchorMax=new Vector2(.9f,.57f);_saveStatus.rectTransform.offsetMin=_saveStatus.rectTransform.offsetMax=Vector2.zero;
            _saveStatus.fontSize=22;_saveStatus.color=new Color(1f,.4f,.3f);_saveStatus.alignment=TextAlignmentOptions.Center;_saveStatus.raycastTarget=false;
        }
        private void OnMatch(int connected,int gained,int removed,NineMood.Data.Enums.TileColor color)
        {
            int tier = DataManager.Instance.BalanceData.GetChainMultiplier(connected);
            _chainTier.text = $"CHAIN {connected}  /  TIER x{tier}";
        }
        private void OnItemUse(int score,int removed) => _chainTier.text=$"ITEM USE  /  REMOVED {removed}";
        private void OnEnable()
        {
            GameEvents.OnItemUseCompleted += OnItemUse;
            GameEvents.OnMatchCompleted += OnMatch;
            GameEvents.OnScoreChanged    += UpdateScore;
            GameEvents.OnStaminaChanged  += UpdateStamina;
            GameEvents.OnProgressChanged += UpdateProgress;
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            NineMood.Core.Persistence.ProgressionService.Changed += RefreshReadyStats;
        }

        private void OnDisable()
        {
            GameEvents.OnItemUseCompleted -= OnItemUse;
            GameEvents.OnMatchCompleted -= OnMatch;
            GameEvents.OnScoreChanged    -= UpdateScore;
            GameEvents.OnStaminaChanged  -= UpdateStamina;
            GameEvents.OnProgressChanged -= UpdateProgress;
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            NineMood.Core.Persistence.ProgressionService.Changed -= RefreshReadyStats;
        }

        private void Start()
        {
            if (textScore != null) textScore.text = "0";

            // 스태미나 초기값
            var stamina=StaminaManager.Instance;
            if(stamina!=null) UpdateStamina(stamina.CurrentStamina,stamina.MaxStamina);
            RefreshReadyStats();
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

        private void RefreshReadyStats()
        {
            if(GameManager.Instance?.CurrentState!=GameState.Briefing) return;
            float max=NineMood.Core.Persistence.ProgressionService.Stats().maxHP;
            UpdateStamina(max,max);
        }
        private void OnGameStateChanged(GameState state)
        {
            if(state!=GameState.Briefing) return;
            if(_chainTier!=null) _chainTier.text="";
            RefreshReadyStats();
        }
    }
}
