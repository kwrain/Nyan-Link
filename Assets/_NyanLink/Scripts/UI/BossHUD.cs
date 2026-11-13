using NineMood.Core;
using NineMood.Boss;
using NineMood.Data.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>
    /// BossPhase HUD — HP 바, 패턴 안내, Groggy 표시.
    /// BossPhase 진입 시 활성화, 이탈 시 비활성화.
    /// </summary>
    public class BossHUD : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private Slider          sliderHP;
        [SerializeField] private TextMeshProUGUI textHP;

        [Header("패턴 안내")]
        [SerializeField] private TextMeshProUGUI textInstruction;
        [SerializeField] private TextMeshProUGUI textPatternType;

        [Header("Groggy")]
        [SerializeField] private GameObject      groggyIndicator;

        [Header("Phase")]
        [SerializeField] private TextMeshProUGUI textPhase;

        private CanvasGroup _group;
        private TextMeshProUGUI _timer;
        public string TimerText => _timer != null ? _timer.text : "";
        private void Place(RectTransform rect, float x0,float y0,float x1,float y1)
        {
            rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private void SetupText(TextMeshProUGUI text,float y0,float y1)
        {
            if (text == null) return;
            Place(text.rectTransform,0.08f,y0,0.92f,y1);
            text.fontSize = 30; text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
        private void LateUpdate()
        {
            var boss = BossController.Instance;
            if (boss == null || _timer == null) return;
            bool visible = NineMood.Core.Managers.GameManager.Instance?.CurrentState == GameState.BossPhase;
            if (!visible) return;
            _timer.text = boss.IsGroggy ? $"GROGGY  {boss.GroggyRemaining:F1}s" :
                boss.IsPatternActive ? $"TIME  {boss.timeAttackPattern.RemainingTime:F1}s" : "NEXT PATTERN";
            if (boss.TimersFrozen) _timer.text += "  FROZEN";
        }
        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            SetupText(textHP,0.745f,0.775f);
            SetupText(textPhase,0.775f,0.8f);
            SetupText(textPatternType,0.70f,0.735f);
            SetupText(textInstruction,0.65f,0.70f);
            if (sliderHP != null) Place(sliderHP.GetComponent<RectTransform>(),0.12f,0.735f,0.88f,0.745f);
            if (groggyIndicator != null) groggyIndicator.SetActive(false);
            _timer = new GameObject("Boss Timer",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _timer.transform.SetParent(transform,false);
            if (textHP != null) _timer.font = textHP.font;
            SetupText(_timer,0.61f,0.65f);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged            += OnGameStateChanged;
            GameEvents.OnBossHPChanged               += OnBossHPChanged;
            GameEvents.OnBossGroggyStarted           += OnBossGroggyStarted;
            GameEvents.OnBossGroggyEnded             += OnBossGroggyEnded;
            GameEvents.OnBossPhaseChanged            += OnBossPhaseChanged;
            GameEvents.OnBossPatternStarted          += OnBossPatternStarted;
            GameEvents.OnBossPatternInstructionChanged += OnBossPatternInstructionChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged            -= OnGameStateChanged;
            GameEvents.OnBossHPChanged               -= OnBossHPChanged;
            GameEvents.OnBossGroggyStarted           -= OnBossGroggyStarted;
            GameEvents.OnBossGroggyEnded             -= OnBossGroggyEnded;
            GameEvents.OnBossPhaseChanged            -= OnBossPhaseChanged;
            GameEvents.OnBossPatternStarted          -= OnBossPatternStarted;
            GameEvents.OnBossPatternInstructionChanged -= OnBossPatternInstructionChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Paused) return;
            _group.alpha = state == GameState.BossPhase ? 1f : 0f;

        }

        private void ResetDisplay()
        {
            if (groggyIndicator != null) groggyIndicator.SetActive(false);
            if (textInstruction != null) textInstruction.text = "";
            if (textPhase       != null) textPhase.text       = "Phase 1";
            if (textPatternType != null) textPatternType.text = "";
        }

        private void OnBossHPChanged(float current, float max)
        {
            if (sliderHP != null)
            {
                sliderHP.maxValue = max;
                sliderHP.value    = current;
            }
            if (textHP != null)
                textHP.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void OnBossGroggyStarted(float duration)
        {
            // 그로기 표시는 타이머와 안내 텍스트에서 표시. 기존 오버레이는 보존하되 숨긴다.
            if (textInstruction != null) textInstruction.text = "FREE MATCH  /  ORANGE x2  /  CYAN SHIELD";
        }

        private void OnBossGroggyEnded()
        {
            if (groggyIndicator != null) groggyIndicator.SetActive(false);
        }

        private void OnBossPhaseChanged(int phaseIndex)
        {
            if (textPhase != null)
                textPhase.text = $"Phase {phaseIndex + 1}";
        }

        private void OnBossPatternStarted(BossBattleType type)
        {
            if (textPatternType != null)
                textPatternType.text = type switch
                {
                    BossBattleType.TimeAttack     => "TIME ATTACK",
                    BossBattleType.RhythmNote     => "RHYTHM NOTE",
                    BossBattleType.ColorQuota     => "COLOR QUOTA",
                    BossBattleType.MemorySequence => "MEMORY",
                    _                             => type.ToString()
                };
        }

        private void OnBossPatternInstructionChanged(string instruction)
        {
            if (textInstruction != null)
                textInstruction.text = instruction;
        }
    }
}
