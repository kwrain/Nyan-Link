using NineMood.Data.Enums;
using NineMood.Runner;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>
    /// 러너 뷰 영역에 장애물 경고를 표시하는 HUD.
    /// ObstacleController.OnWarningBegan / OnWarningResolved 이벤트를 구독한다.
    ///
    /// UI 참조가 모두 비어 있으면 Awake에서 Screen Space Overlay 캔버스를 자동 생성한다.
    /// 패널은 화면 상단 (puzzleRatio ~ 1.0 구간 = 러너 뷰) 에 배치된다.
    /// </summary>
    public class RunnerWarningHUD : MonoBehaviour
    {
        [Header("UI 참조 (비어있으면 자동 생성)")]
        public GameObject      warningPanel;
        public Image           actionIcon;
        public TextMeshProUGUI actionLabel;
        public Image           timerBar;

        [Header("화면 분할 비율 — DualCameraRig.puzzleRatio와 일치")]
        [Range(0f, 1f)]
        public float puzzleRatio = 0.60f;

        private float _totalDuration;
        private float _elapsed;
        private bool  _warningActive;
        private Color _actionColor;
        private RunnerState _action;
        private ObstacleSpawner _spawner;
        public float Remaining => _spawner != null ? _spawner.WarningRemaining : Mathf.Max(0f, _totalDuration - _elapsed);

        // ── 라이프사이클 ──────────────────────────────────────────────

        private void Awake()
        {
            _spawner = FindFirstObjectByType<ObstacleSpawner>();
            if (warningPanel == null)
                BuildUI();
        }

        private void Start()
        {
            if (warningPanel != null)
                warningPanel.SetActive(false);
        }

        private void OnEnable()
        {
            ObstacleSpawner.OnPreWarningBegan  += ShowWarning;
            ObstacleSpawner.OnPreWarningEnded  += HideWarning;
        }

        private void OnDisable()
        {
            ObstacleSpawner.OnPreWarningBegan  -= ShowWarning;
            ObstacleSpawner.OnPreWarningEnded  -= HideWarning;
        }

        private void Update()
        {
            if (!_warningActive || _totalDuration <= 0f) return;

            _elapsed += Time.deltaTime;
            float fill = Mathf.Clamp01(Remaining / _totalDuration);
            if (actionLabel != null) actionLabel.text = GetActionLabel(_action) +
                (_spawner != null && _spawner.CurrentObstacle != null && _spawner.CurrentObstacle.HasSuccessReservation ? " READY " : " ") +
                Mathf.CeilToInt(Remaining);

            if (timerBar != null)
            {
                timerBar.fillAmount = fill;
                // 남은 시간에 따라 액션 색 → 빨강으로 전환
                timerBar.color = Color.Lerp(Color.red, _actionColor, fill);
            }
        }

        // ── 이벤트 핸들러 ────────────────────────────────────────────

        private void ShowWarning(RunnerState action, float duration)
        {
            _action = action;
            _totalDuration = duration;
            _elapsed       = 0f;
            _warningActive = true;
            _actionColor   = GetActionColor(action);

            if (warningPanel != null) warningPanel.SetActive(true);
            if (timerBar     != null) { timerBar.fillAmount = 1f; timerBar.color = _actionColor; }
            if (actionIcon   != null) actionIcon.color = _actionColor;
            if (actionLabel  != null)
            {
                actionLabel.text  = GetActionLabel(action);
                actionLabel.color = _actionColor;
            }
        }

        private void HideWarning()
        {
            _warningActive = false;
            if (warningPanel != null)
                warningPanel.SetActive(false);
        }

        // ── UI 자동 생성 ─────────────────────────────────────────────

        private void BuildUI()
        {
            // ── Canvas ──────────────────────────────────────────────
            var canvasGO = new GameObject("RunnerWarningCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight  = 1f;   // portrait → 세로 기준 스케일
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── 경고 패널 — 러너 뷰 중앙에 가로 띠로 배치 ──────────
            //   anchorY: puzzleRatio(0.6) ~ 1.0 범위 내 중앙부
            float runnerCenterY = puzzleRatio + (1f - puzzleRatio) * 0.5f; // 0.8
            float halfHeight    = 0.085f;

            var panelGO   = new GameObject("WarningPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, runnerCenterY - halfHeight);
            panelRect.anchorMax = new Vector2(0.95f, runnerCenterY + halfHeight);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelBg = panelGO.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.6f);
            warningPanel  = panelGO;

            // ── 액션 아이콘 (좌측) ──────────────────────────────────
            var iconGO   = new GameObject("ActionIcon");
            iconGO.transform.SetParent(panelGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin        = new Vector2(0f, 0.15f);
            iconRect.anchorMax        = new Vector2(0f, 0.85f);
            iconRect.pivot            = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(14f, 0f);
            iconRect.sizeDelta        = new Vector2(54f, 0f);
            actionIcon       = iconGO.AddComponent<Image>();
            actionIcon.color = Color.white;

            // ── 액션 레이블 (중앙) ──────────────────────────────────
            var labelGO   = new GameObject("ActionLabel");
            labelGO.transform.SetParent(panelGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.13f, 0.18f);
            labelRect.anchorMax = new Vector2(0.98f, 0.98f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            actionLabel           = labelGO.AddComponent<TextMeshProUGUI>();
            actionLabel.text      = "";
            actionLabel.fontSize  = 52f;
            actionLabel.fontStyle = FontStyles.Bold;
            actionLabel.alignment = TextAlignmentOptions.MidlineLeft;
            actionLabel.color     = Color.white;

            // ── 타이머 바 배경 (하단 strip) ─────────────────────────
            var timerBgGO = new GameObject("TimerBarBg");
            timerBgGO.transform.SetParent(panelGO.transform, false);
            var timerBgRect  = timerBgGO.AddComponent<RectTransform>();
            timerBgRect.anchorMin = new Vector2(0f, 0f);
            timerBgRect.anchorMax = new Vector2(1f, 0.16f);
            timerBgRect.offsetMin = Vector2.zero;
            timerBgRect.offsetMax = Vector2.zero;
            var timerBgImg  = timerBgGO.AddComponent<Image>();
            timerBgImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            // ── 타이머 바 fill ──────────────────────────────────────
            var timerGO   = new GameObject("TimerBar");
            timerGO.transform.SetParent(timerBgGO.transform, false);
            var timerRect = timerGO.AddComponent<RectTransform>();
            timerRect.anchorMin = Vector2.zero;
            timerRect.anchorMax = Vector2.one;
            timerRect.offsetMin = new Vector2(2f,  2f);
            timerRect.offsetMax = new Vector2(-2f, -2f);

            timerBar            = timerGO.AddComponent<Image>();
            timerBar.type       = Image.Type.Filled;
            timerBar.fillMethod = Image.FillMethod.Horizontal;
            timerBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerBar.color      = Color.white;
        }

        // ── 색상·레이블 유틸 ─────────────────────────────────────────

        private static Color GetActionColor(RunnerState action) => action switch
        {
            RunnerState.Jump  => new Color(1f,   0.3f, 0.3f),
            RunnerState.Slide => new Color(0.3f, 0.6f, 1f),
            RunnerState.Block => new Color(1f,   0.85f, 0.1f),
            _                 => Color.white
        };

        private static string GetActionLabel(RunnerState action) => action switch
        {
            RunnerState.Jump  => "JUMP!",
            RunnerState.Slide => "SLIDE!",
            RunnerState.Block => "BLOCK!",
            _                 => "?"
        };
    }
}
