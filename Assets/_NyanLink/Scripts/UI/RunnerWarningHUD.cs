using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using NineMood.Presentation;
using NineMood.Runner;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>Approach preview and an input zone projected from the actual obstacle judgment bounds.</summary>
    public class RunnerWarningHUD : MonoBehaviour
    {
        public GameObject warningPanel;
        public Image actionIcon;
        public TextMeshProUGUI actionLabel;
        // Retained for old serialized references; countdown graphics are no longer used.
        public Image timerBar;
        [Range(0f, 1f)] public float puzzleRatio = .60f;
        public float Remaining => _spawner != null ? _spawner.WarningRemaining : 0f;
        public Image InputZone { get; private set; }
        public Image JudgmentMarker { get; private set; }
        public TextMeshProUGUI ZoneLabel { get; private set; }
        private ObstacleSpawner _spawner;
        private Camera _camera;
        private RunnerState _action;
        private float _pulseTime, _entrance;
        private bool _announced;
        private readonly Image[] _zoneEdges = new Image[2];

        private void Awake()
        {
            _spawner = FindFirstObjectByType<ObstacleSpawner>();
            _camera = FindFirstObjectByType<DualCameraRig>()?.cameraRunner;
            if (warningPanel == null) BuildUI();
            if (timerBar != null) timerBar.transform.parent.gameObject.SetActive(false);
            BuildZone();
            warningPanel.SetActive(false);
        }
        private void OnEnable()
        {
            ObstacleSpawner.OnPreWarningBegan += ShowWarning;
            ObstacleSpawner.OnPreWarningEnded += HideWarning;
        }
        private void OnDisable()
        {
            ObstacleSpawner.OnPreWarningBegan -= ShowWarning;
            ObstacleSpawner.OnPreWarningEnded -= HideWarning;
            HideWarning();
            if (InputZone != null) InputZone.gameObject.SetActive(false);
            if (JudgmentMarker != null) JudgmentMarker.gameObject.SetActive(false);
        }
        private void ShowWarning(RunnerState action, float duration)
        {
            _action = action; _announced = true; _entrance = 0f; _pulseTime = 0f;
        }
        private void HideWarning() { _announced = false; if (warningPanel != null) warningPanel.SetActive(false); }

        private void LateUpdate()
        {
            var game = GameManager.Instance;
            bool running = game != null && (game.CurrentState == GameState.RunnerPhase ||
                (game.CurrentState == GameState.Paused && game.StateBeforePause == GameState.RunnerPhase) ||
                (game.CurrentState == GameState.ColorSelection && !(NineMood.Boss.BossController.Instance?.MaxHP > 0)));
            var obstacle = _spawner != null ? _spawner.CurrentObstacle : null;
            bool hasObstacle = running && obstacle != null && obstacle.IsInitialized && !obstacle.IsResolved;
            bool open = hasObstacle && obstacle.IsInputWindowOpen;
            bool preview = running && (hasObstacle ? obstacle.transform.position.x > obstacle.InputWindowEndX : _announced);
            warningPanel.SetActive(preview);
            if (preview)
            {
                if (game.CurrentState == GameState.RunnerPhase) { _pulseTime += Time.deltaTime; _entrance += Time.deltaTime; }
                var action = hasObstacle ? obstacle.RequiredAction : _action;
                actionIcon.sprite = ConceptArtLibrary.Get("obstacle-" + ActionIndex(action));
                actionIcon.preserveAspect = true;
                actionIcon.color = new Color(1, 1, 1, .45f + .55f * (.5f + .5f * Mathf.Cos(_pulseTime * Mathf.PI * 4)));
                actionLabel.text = _spawner.RetryRemaining > 0 ? "다시 접근" : "접근 중";
                actionLabel.color = Color.white;
                float slide = Mathf.Lerp(.12f, 0f, Mathf.Clamp01(_entrance / .25f));
                PrototypePresentation.Place(warningPanel.GetComponent<RectTransform>(), .72f + slide, .805f, .97f + slide, .88f);
            }
            InputZone.gameObject.SetActive(hasObstacle && _camera != null);
            JudgmentMarker.gameObject.SetActive(InputZone.gameObject.activeSelf);
            if (!InputZone.gameObject.activeSelf) return;
            // Use the same world X bounds as IsInputWindowOpen, including any judgment offset.
            float y = _spawner.runner.transform.position.y;
            Vector3 min = _camera.WorldToScreenPoint(new Vector3(obstacle.InputWindowStartX, y - .8f, 0));
            Vector3 max = _camera.WorldToScreenPoint(new Vector3(obstacle.InputWindowEndX, y + 1.65f, 0));
            var view = _camera.pixelRect;
            var parent = (RectTransform)InputZone.transform.parent;
            Vector2 a = ParentAnchor(parent, new Vector2(Mathf.Clamp(min.x, view.xMin, view.xMax), Mathf.Clamp(min.y, view.yMin, view.yMax)));
            Vector2 b = ParentAnchor(parent, new Vector2(Mathf.Clamp(max.x, view.xMin, view.xMax), Mathf.Clamp(max.y, view.yMin, view.yMax)));
            PrototypePresentation.Place(InputZone.rectTransform, a.x, a.y, b.x, b.y);
            var judgment = _camera.WorldToScreenPoint(new Vector3(obstacle.JudgmentX, y, 0));
            Vector2 marker = ParentAnchor(parent, new Vector2(Mathf.Clamp(judgment.x, view.xMin, view.xMax), min.y));
            PrototypePresentation.Place(JudgmentMarker.rectTransform, marker.x, a.y, marker.x, b.y);
            JudgmentMarker.rectTransform.sizeDelta = new Vector2(3, 0);
            bool passed = obstacle.HasInputWindowPassed;
            bool ready = obstacle.HasSuccessReservation;
            Color color = ready ? new Color(.35f, 1f, .68f) : ActionColor(obstacle.RequiredAction);
            InputZone.color = new Color(color.r, color.g, color.b, open ? .23f : .10f);
            foreach (var edge in _zoneEdges) edge.color = new Color(color.r, color.g, color.b, open ? .95f : .45f);
            ZoneLabel.text = passed ? (ready ? "입력 완료 · 통과 대기" : "입력 마감 · 통과 대기") : ready ? "입력 완료" : open ? KoreanText.ColorName(obstacle.RequiredColor) + " 2개 이상 연결" : "영역에 들어오면 연결";
            ZoneLabel.color = ready ? color : Color.white;
        }

        private static Vector2 ParentAnchor(RectTransform parent, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local);
            return new Vector2((local.x - parent.rect.xMin) / parent.rect.width, (local.y - parent.rect.yMin) / parent.rect.height);
        }

        private void BuildUI()
        {
            var root = new GameObject("RunnerWarningCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 90; // Pause/choice overlays stay above the gameplay cues.
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 1;
            warningPanel = new GameObject("WarningPanel", typeof(RectTransform), typeof(Image));
            warningPanel.transform.SetParent(root.transform, false);
            var backing = warningPanel.GetComponent<Image>(); backing.color = new Color(.035f, .055f, .12f, .94f); backing.raycastTarget = false;
            actionIcon = new GameObject("ActionIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            actionIcon.transform.SetParent(warningPanel.transform, false); actionIcon.raycastTarget = false;
            PrototypePresentation.Place(actionIcon.rectTransform, .05f, .12f, .43f, .88f);
            actionLabel = Label(warningPanel.transform, "Approach label", 25);
            PrototypePresentation.Place(actionLabel.rectTransform, .44f, .1f, .97f, .9f);
        }
        private void BuildZone()
        {
            InputZone = new GameObject("Obstacle input zone", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            InputZone.transform.SetParent(warningPanel.transform.parent, false);
            InputZone.transform.SetAsFirstSibling(); InputZone.raycastTarget = false;
            foreach (bool right in new[] { false, true })
            {
                var edge = new GameObject(right ? "Entry boundary" : "Judgment boundary", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                edge.transform.SetParent(InputZone.transform, false); edge.raycastTarget = false;
                _zoneEdges[right ? 1 : 0] = edge;
                var rect = edge.rectTransform; rect.anchorMin = new Vector2(right ? 1 : 0, 0); rect.anchorMax = new Vector2(right ? 1 : 0, 1);
                rect.sizeDelta = new Vector2(3, 0); rect.anchoredPosition = Vector2.zero;
            }
            ZoneLabel = Label(InputZone.transform, "Input zone instruction", 24);
            var bg = new GameObject("Instruction backing", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bg.transform.SetParent(InputZone.transform, false); bg.transform.SetSiblingIndex(2);
            bg.color = new Color(.02f, .04f, .09f, .82f); bg.raycastTarget = false;
            PrototypePresentation.Place(bg.rectTransform, 0, 1, 1, 1); bg.rectTransform.sizeDelta = new Vector2(0, 42);
            PrototypePresentation.Place(ZoneLabel.rectTransform, 0, 1, 1, 1); ZoneLabel.rectTransform.sizeDelta = new Vector2(0, 42);
            JudgmentMarker = new GameObject("Obstacle action point", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            JudgmentMarker.transform.SetParent(InputZone.transform.parent, false);
            JudgmentMarker.raycastTarget = false; JudgmentMarker.color = new Color(1, 1, 1, .65f);
            var pointLabel = Label(JudgmentMarker.transform, "Action point label", 22);
            PrototypePresentation.Place(pointLabel.rectTransform, .5f, 1, .5f, 1);
            pointLabel.rectTransform.sizeDelta = new Vector2(110, 42); pointLabel.text = "연출 지점";
            JudgmentMarker.gameObject.SetActive(false);
            InputZone.gameObject.SetActive(false);
        }
        private static TextMeshProUGUI Label(Transform parent, string name, float size)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false); label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false; KoreanUIPresentation.Apply(label); return label;
        }
        private static int ActionIndex(RunnerState action) => action == RunnerState.Jump ? 0 : action == RunnerState.Slide ? 1 : 2;
        private static Color ActionColor(RunnerState action) => action == RunnerState.Jump ? new Color(1, .35f, .4f) :
            action == RunnerState.Slide ? new Color(.3f, .65f, 1) : new Color(1, .85f, .2f);
    }
}
