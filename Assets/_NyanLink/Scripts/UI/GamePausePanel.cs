using NineMood.Core;
using NineMood.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>씬 소유 UGUI 일시정지. 버튼은 배율 0에서도 입력된다.</summary>
    public class GamePausePanel : MonoBehaviour
    {
        public Button PauseButton { get; private set; }
        public Button ResumeButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button HomeButton { get; private set; }
        public bool IsOpen => _overlay != null && _overlay.activeSelf;
        private GameObject _overlay;

        private void Awake()
        {
            var root = new GameObject("GamePauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080,1920);
            PauseButton = MakeButton(root.transform,"PAUSE",new Vector2(0.88f,0.96f),new Vector2(180,64),
                () => GameManager.Instance?.PauseGame());
            _overlay = new GameObject("PauseOverlay", typeof(RectTransform),typeof(Image));
            _overlay.transform.SetParent(root.transform,false);
            var rect = _overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _overlay.GetComponent<Image>().color = new Color(0.03f,0.03f,0.09f,0.95f);
            Label(_overlay.transform,"PAUSED",new Vector2(0.5f,0.69f),new Vector2(600,100),64);
            ResumeButton = MakeButton(_overlay.transform,"RESUME",new Vector2(0.5f,0.55f),new Vector2(540,120),
                () => GameManager.Instance?.ResumeGame());
            RestartButton = MakeButton(_overlay.transform,"RESTART",new Vector2(0.5f,0.44f),new Vector2(540,120),
                () => GameManager.Instance?.RetryGame());
            HomeButton = MakeButton(_overlay.transform,"HOME",new Vector2(0.5f,0.33f),new Vector2(540,120),SceneLoader.LoadMainMenu);
            _overlay.SetActive(false);
            PauseButton.gameObject.SetActive(false);
        }
        private void OnEnable() => GameEvents.OnGameStateChanged += OnState;
        private void OnDisable() => GameEvents.OnGameStateChanged -= OnState;
        private void OnState(GameState state)
        {
            _overlay.SetActive(state == GameState.Paused);
            PauseButton.gameObject.SetActive(state == GameState.RunnerPhase || state == GameState.BossPhase);
        }
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (GameManager.Instance?.CurrentState == GameState.Paused) GameManager.Instance.ResumeGame();
            else GameManager.Instance?.PauseGame();
        }
        private static Button MakeButton(Transform parent,string text,Vector2 anchor,Vector2 size,UnityAction action)
        {
            var go = new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));
            go.transform.SetParent(parent,false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.12f,0.4f,0.8f,1f);
            var button = go.GetComponent<Button>(); button.onClick.AddListener(action);
            Label(go.transform,text,new Vector2(0.5f,0.5f),size,32);
            return button;
        }
        private static void Label(Transform parent,string text,Vector2 anchor,Vector2 size,int fontSize)
        {
            var go = new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
            go.transform.SetParent(parent,false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
            var label = go.GetComponent<TextMeshProUGUI>(); label.text = text; label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        }
    }
}
