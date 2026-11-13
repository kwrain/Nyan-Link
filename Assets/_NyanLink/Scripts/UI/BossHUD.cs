using NineMood.Core;
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

        private void Start()
        {
            gameObject.SetActive(false);
        }

        private void OnGameStateChanged(GameState state)
        {
            gameObject.SetActive(state == GameState.BossPhase);
            if (state == GameState.BossPhase)
                ResetDisplay();
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
            if (groggyIndicator != null) groggyIndicator.SetActive(true);
            if (textInstruction != null) textInstruction.text = "GROGGY! 타일을 매칭하세요!";
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
