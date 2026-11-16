using NineMood.Core;
using NineMood.Boss;
using NineMood.Presentation;
using System.Collections.Generic;
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

        private readonly List<Image> _sequenceIcons = new List<Image>();
        private readonly List<Image> _sequenceFrames = new List<Image>();
        private readonly List<TextMeshProUGUI> _sequenceLabels = new List<TextMeshProUGUI>();
        private RectTransform _sequenceRow;
        private Material _completedMaterial;
        private int _sequenceSignature = int.MinValue;
        private float _sequenceWidth;
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
            RefreshSequence();
            _timer.text = boss.IsGroggy ? $"그로기  {boss.GroggyRemaining:F1}초" :
                boss.IsPatternActive ? $"남은 시간  {boss.timeAttackPattern.RemainingTime:F1}초" : "다음 순서 준비";
            if (boss.TimersFrozen) _timer.text += "  · 시간 정지";
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
            _sequenceRow = new GameObject("Boss color sequence", typeof(RectTransform)).GetComponent<RectTransform>();
            _sequenceRow.SetParent(transform, false);
            Place(_sequenceRow, .10f, .748f, .90f, .805f);
            _completedMaterial = new Material(Resources.Load<Shader>("BossCompletedIcon"));
            _sequenceRow.gameObject.SetActive(false);
        }

        private void RefreshSequence()
        {
            var pattern = BossController.Instance?.timeAttackPattern;
            bool visible = pattern != null && pattern.IsRunning;
            _sequenceRow.gameObject.SetActive(visible);
            if (textInstruction != null) textInstruction.enabled = !visible;
            if (!visible) { _sequenceSignature = int.MinValue; return; }
            int count = pattern.Sequence.Count;
            int signature = pattern.NextIndex;
            unchecked { foreach (var color in pattern.Sequence) signature = signature * 31 + (int)color + 1; }
            if (_sequenceSignature == signature && Mathf.Approximately(_sequenceWidth, _sequenceRow.rect.width)) return;
            _sequenceSignature = signature; _sequenceWidth = _sequenceRow.rect.width;
            while (_sequenceIcons.Count < count)
            {
                var frame = new GameObject("Step " + (_sequenceIcons.Count + 1), typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                frame.transform.SetParent(_sequenceRow, false);
                frame.raycastTarget = false;
                var icon = new GameObject("Color icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(frame.transform, false);
                Place(icon.rectTransform, .08f, .08f, .92f, .92f);
                icon.preserveAspect = true; icon.raycastTarget = false;
                var label = new GameObject("Step number", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                label.transform.SetParent(frame.transform, false);
                Place(label.rectTransform, -.3f, -.42f, 1.3f, -.02f);
                label.fontSize = 22; label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false; KoreanUIPresentation.Apply(label);
                _sequenceFrames.Add(frame); _sequenceIcons.Add(icon); _sequenceLabels.Add(label);
            }
            float spacing = Mathf.Min(140f, _sequenceRow.rect.width / Mathf.Max(1, count));
            for (int i = 0; i < _sequenceIcons.Count; i++)
            {
                var frame = _sequenceFrames[i];
                frame.gameObject.SetActive(i < count);
                if (i >= count) continue;
                var rect = frame.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .64f);
                rect.sizeDelta = Vector2.one * Mathf.Min(70f, spacing * .65f);
                rect.anchoredPosition = new Vector2((i - (count - 1) * .5f) * spacing, 0);
                bool done = i < pattern.NextIndex, current = i == pattern.NextIndex;
                frame.color = current ? new Color(1f, .88f, .45f) : new Color(.17f, .22f, .30f);
                var icon = _sequenceIcons[i];
                icon.sprite = ConceptArtLibrary.Get("tile-" + (int)pattern.Sequence[i]);
                icon.material = done ? _completedMaterial : null;
                icon.color = done ? new Color(.55f, .55f, .55f) : Color.white;
                var label = _sequenceLabels[i];
                label.text = $"{i + 1}" + (done ? " 완료" : current ? " 지금" : "");
                label.color = done ? new Color(.55f, .55f, .55f) : current ? new Color(1f, .88f, .45f) : Color.white;
            }
        }

        private void OnDestroy()
        {
            if (_completedMaterial != null) Destroy(_completedMaterial);
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
            if (textPhase       != null) textPhase.text       = "1단계";
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
            if (textInstruction != null) textInstruction.text = "자유 연결  /  주황 공격 ×2  /  청록 방어";
        }

        private void OnBossGroggyEnded()
        {
            if (groggyIndicator != null) groggyIndicator.SetActive(false);
        }

        private void OnBossPhaseChanged(int phaseIndex)
        {
            if (textPhase != null)
                textPhase.text = $"{phaseIndex + 1}단계";
        }

        private void OnBossPatternStarted(BossBattleType type)
        {
            if (textPatternType != null)
                textPatternType.text = type switch
                {
                    BossBattleType.TimeAttack     => "시간 내 순서 연결",
                    BossBattleType.RhythmNote     => "리듬 노트",
                    BossBattleType.ColorQuota     => "색상 모으기",
                    BossBattleType.MemorySequence => "순서 기억",
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
