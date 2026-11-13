using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using NineMood.UI;

/// <summary>
/// Game UI 계층 구조를 씬에 자동 생성하고 모든 레퍼런스를 연결한다.
/// NyanLink/Setup/Create Game UI 메뉴에서 실행.
/// </summary>
public static class NyanLinkUISetup
{
    private static readonly Vector2 RefRes = new Vector2(1080, 1920);

    [MenuItem("NyanLink/Setup/Create Game UI")]
    public static void CreateGameUI()
    {
        Canvas canvas = GetOrCreateCanvas();
        EnsureEventSystem();

        CreateGameHUD(canvas.transform);
        CreateBossHUD(canvas.transform);
        CreateGameReadyPanel(canvas.transform);
        CreateGameOverPanel(canvas.transform);
        CreateStageClearPanel(canvas.transform);
        CreateDebugPanel(canvas.transform);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[NyanLink] Game UI 생성 완료! 씬을 저장하세요.");
    }

    // ──────────────────────────────────────────────────
    //  Canvas / EventSystem
    // ──────────────────────────────────────────────────

    private static Canvas GetOrCreateCanvas()
    {
        Canvas existing = Object.FindFirstObjectByType<Canvas>();
        if (existing != null) return existing;

        GameObject go = new GameObject("Canvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = RefRes;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>(); // New Input System 호환
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    // ──────────────────────────────────────────────────
    //  GameHUD
    // ──────────────────────────────────────────────────

    private static void CreateGameHUD(Transform canvas)
    {
        DestroyIfExists(canvas, "GameHUD");
        GameObject root = MakePanel(canvas, "GameHUD", new Color(0, 0, 0, 0), active: true, raycastTarget: false);
        GameHUD hud = root.AddComponent<GameHUD>();

        // 진행도 바 (최상단, 녹색 얇은 바)
        var progressSlider = MakeSlider(root.transform, "Slider_Progress",
            new Color(0.2f, 0.85f, 0.3f),
            anchor: new Vector2(0.5f, 0.975f),
            size: new Vector2(960f, 16f));

        // 점수 (진행도 바 아래, 중앙)
        var scoreText = MakeText(root.transform, "Text_Score", "0", 88,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.935f),
            offset: new Vector4(-280, -50, 280, 50));

        // HP 레이블 (좌측)
        MakeText(root.transform, "Text_HPLabel", "HP", 28,
            TextAlignmentOptions.Left,
            anchor: new Vector2(0.1f, 0.868f),
            offset: new Vector4(-40, -20, 40, 20));

        // 스태미나 바 (빨간색)
        var staminaSlider = MakeSlider(root.transform, "Slider_Stamina",
            new Color(0.9f, 0.22f, 0.22f),
            anchor: new Vector2(0.55f, 0.868f),
            size: new Vector2(680f, 28f));

        // 스태미나 수치 텍스트 (바 오른쪽)
        var staminaText = MakeText(root.transform, "Text_Stamina", "100/100", 26,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.828f),
            offset: new Vector4(-120, -18, 120, 18));

        SetFields(hud,
            ("textScore", scoreText),
            ("sliderStamina", staminaSlider),
            ("textStamina", staminaText),
            ("sliderProgress", progressSlider));
    }

    // ──────────────────────────────────────────────────
    //  BossHUD
    // ──────────────────────────────────────────────────

    private static void CreateBossHUD(Transform canvas)
    {
        DestroyIfExists(canvas, "BossHUD");
        GameObject root = MakePanel(canvas, "BossHUD", new Color(0, 0, 0, 0), active: false, raycastTarget: false);
        BossHUD hud = root.AddComponent<BossHUD>();

        // 보스 이름/Phase 텍스트 (상단 중앙)
        var textPhase = MakeText(root.transform, "Text_Phase", "Phase 1", 36,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.62f),
            offset: new Vector4(-300, -22, 300, 22));

        // HP 슬라이더 (빨간색, 상단)
        var sliderHP = MakeSlider(root.transform, "Slider_BossHP",
            new Color(0.9f, 0.15f, 0.15f),
            anchor: new Vector2(0.5f, 0.59f),
            size: new Vector2(900f, 20f));

        // HP 수치 텍스트
        var textHP = MakeText(root.transform, "Text_BossHP", "1000 / 1000", 28,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.565f),
            offset: new Vector4(-200, -18, 200, 18));

        // 패턴 타입 텍스트
        var textPatternType = MakeText(root.transform, "Text_PatternType", "", 42,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.54f),
            offset: new Vector4(-300, -26, 300, 26));

        // 패턴 안내 텍스트 (하단)
        var textInstruction = MakeText(root.transform, "Text_Instruction", "", 34,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.505f),
            offset: new Vector4(-380, -24, 380, 24));

        // Groggy 인디케이터 (오렌지 배경 텍스트)
        GameObject groggyGO = new GameObject("GroggyIndicator");
        groggyGO.transform.SetParent(root.transform, false);
        RectTransform groggyRt = groggyGO.AddComponent<RectTransform>();
        groggyRt.anchorMin = groggyRt.anchorMax = groggyRt.pivot = new Vector2(0.5f, 0.53f);
        groggyRt.sizeDelta = new Vector2(400f, 60f);
        Image groggBg = groggyGO.AddComponent<Image>();
        groggBg.color = new Color(1f, 0.5f, 0f, 0.85f);
        MakeText(groggyGO.transform, "Text_Groggy", "GROGGY!", 44,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.5f),
            offset: new Vector4(-190, -28, 190, 28));
        groggyGO.SetActive(false);

        SetFields(hud,
            ("sliderHP",        sliderHP),
            ("textHP",          textHP),
            ("textInstruction", textInstruction),
            ("textPatternType", textPatternType),
            ("groggyIndicator", groggyGO),
            ("textPhase",       textPhase));
    }

    // ──────────────────────────────────────────────────
    //  GameReadyPanel
    // ──────────────────────────────────────────────────

    private static void CreateGameReadyPanel(Transform canvas)
    {
        DestroyIfExists(canvas, "GameReadyPanel");
        GameObject root = MakePanel(canvas, "GameReadyPanel", new Color(0, 0, 0, 0.72f), active: true);
        GameReadyPanel panel = root.AddComponent<GameReadyPanel>();

        // 타이틀
        MakeText(root.transform, "Text_Title", "NYAN LINK", 130,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.62f),
            offset: new Vector4(-380, -70, 380, 70));

        MakeText(root.transform, "Text_Subtitle", "Connect tiles to score!", 40,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.52f),
            offset: new Vector4(-380, -30, 380, 30));

        Button startBtn = MakeButton(root.transform, "Button_Start", "START", 56,
            anchor: new Vector2(0.5f, 0.38f),
            size: new Vector2(480, 120),
            color: new Color(0.18f, 0.58f, 1f));
        TextMeshProUGUI startLabel = startBtn.GetComponentInChildren<TextMeshProUGUI>();

        // 체크포인트가 있을 때만 노출 ("처음부터" — RetryGame). GameReadyPanel.RefreshButtons가 토글.
        Button newGameBtn = MakeButton(root.transform, "Button_NewGame", "처음부터", 40,
            anchor: new Vector2(0.5f, 0.27f),
            size: new Vector2(380, 90),
            color: new Color(0.3f, 0.3f, 0.35f));

        SetFields(panel,
            ("buttonStart", startBtn),
            ("buttonNewGame", newGameBtn),
            ("textButtonStart", startLabel));
    }

    // ──────────────────────────────────────────────────
    //  GameOverPanel
    // ──────────────────────────────────────────────────

    private static void CreateGameOverPanel(Transform canvas)
    {
        DestroyIfExists(canvas, "GameOverPanel");
        // active: true — Awake()가 실행돼야 이벤트 구독이 이루어짐. Awake 내부에서 SetActive(false) 처리
        GameObject root = MakePanel(canvas, "GameOverPanel", new Color(0.05f, 0.02f, 0.08f, 0.88f), active: true);
        GameOverPanel panel = root.AddComponent<GameOverPanel>();

        MakeText(root.transform, "Text_Title", "GAME OVER", 100,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.66f),
            offset: new Vector4(-380, -60, 380, 60));

        MakeText(root.transform, "Text_ScoreLabel", "SCORE", 38,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.56f),
            offset: new Vector4(-200, -28, 200, 28));

        var scoreText = MakeText(root.transform, "Text_Score", "0", 80,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.50f),
            offset: new Vector4(-300, -50, 300, 50));

        MakeText(root.transform, "Text_BestLabel", "BEST", 36,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.42f),
            offset: new Vector4(-200, -26, 200, 26));

        var bestText = MakeText(root.transform, "Text_BestScore", "0", 52,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.37f),
            offset: new Vector4(-300, -34, 300, 34));

        Button retryBtn = MakeButton(root.transform, "Button_Retry", "RETRY", 52,
            anchor: new Vector2(0.5f, 0.28f),
            size: new Vector2(460, 110),
            color: new Color(0.95f, 0.35f, 0.25f));

        Button mainMenuBtn = MakeButton(root.transform, "Button_MainMenu", "MAIN MENU", 44,
            anchor: new Vector2(0.5f, 0.16f),
            size: new Vector2(380, 90),
            color: new Color(0.3f, 0.3f, 0.35f));

        SetFields(panel,
            ("textScore", scoreText),
            ("textBestScore", bestText),
            ("buttonRetry", retryBtn),
            ("buttonMainMenu", mainMenuBtn));
    }

    // ──────────────────────────────────────────────────
    //  StageClearPanel
    // ──────────────────────────────────────────────────

    private static void CreateStageClearPanel(Transform canvas)
    {
        DestroyIfExists(canvas, "StageClearPanel");
        // active: true — Awake()가 실행돼야 이벤트 구독이 이루어짐. Awake 내부에서 SetActive(false) 처리
        GameObject root = MakePanel(canvas, "StageClearPanel", new Color(0.02f, 0.08f, 0.04f, 0.88f), active: true);
        StageClearPanel panel = root.AddComponent<StageClearPanel>();

        MakeText(root.transform, "Text_Title", "CLEAR!", 120,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.66f),
            offset: new Vector4(-380, -65, 380, 65));

        MakeText(root.transform, "Text_ScoreLabel", "SCORE", 38,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.56f),
            offset: new Vector4(-200, -28, 200, 28));

        var scoreText = MakeText(root.transform, "Text_Score", "0", 80,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.50f),
            offset: new Vector4(-300, -50, 300, 50));

        MakeText(root.transform, "Text_BestLabel", "BEST", 36,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.42f),
            offset: new Vector4(-200, -26, 200, 26));

        var bestText = MakeText(root.transform, "Text_BestScore", "0", 52,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.37f),
            offset: new Vector4(-300, -34, 300, 34));

        Button continueBtn = MakeButton(root.transform, "Button_Continue", "CONTINUE", 52,
            anchor: new Vector2(0.5f, 0.28f),
            size: new Vector2(460, 110),
            color: new Color(0.18f, 0.75f, 0.28f));

        Button mainMenuBtn = MakeButton(root.transform, "Button_MainMenu", "MAIN MENU", 44,
            anchor: new Vector2(0.5f, 0.16f),
            size: new Vector2(380, 90),
            color: new Color(0.3f, 0.3f, 0.35f));

        SetFields(panel,
            ("textScore", scoreText),
            ("textBestScore", bestText),
            ("buttonContinue", continueBtn),
            ("buttonMainMenu", mainMenuBtn));
    }

    // ──────────────────────────────────────────────────
    //  헬퍼 메서드
    // ──────────────────────────────────────────────────

    private static void DestroyIfExists(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) Object.DestroyImmediate(found.gameObject);
    }

    private static GameObject MakePanel(Transform parent, string name, Color bgColor, bool active, bool raycastTarget = true)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = raycastTarget;
        go.SetActive(active);
        return go;
    }

    /// <param name="offset">Vector4(left, bottom, right, top) — anchor 기준 픽셀 오프셋</param>
    private static TextMeshProUGUI MakeText(Transform parent, string name, string content,
        int fontSize, TextAlignmentOptions align, Vector2 anchor, Vector4 offset)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(offset.x, offset.y);
        rt.offsetMax = new Vector2(offset.z, offset.w);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;  // 텍스트는 터치를 가로채지 않음

        TMP_FontAsset font = GetDefaultFont();
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static Button MakeButton(Transform parent, string name, string label,
        int fontSize, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = color;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = Color.white * 0.9f;
        cb.pressedColor = color * 0.7f;
        btn.colors = cb;

        // 버튼 라벨 텍스트
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);

        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;  // 버튼 라벨 텍스트는 터치를 가로채지 않음

        TMP_FontAsset font = GetDefaultFont();
        if (font != null) tmp.font = font;

        return btn;
    }

    // ── 슬라이더 생성 (display-only, Handle 없음) ──────────────

    private static Slider MakeSlider(Transform parent, string name, Color fillColor,
        Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Slider slider = go.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;

        // Background
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.12f, 0.7f);
        bgImg.raycastTarget = false;

        // Fill Area
        GameObject fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(go.transform, false);
        RectTransform fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(4f, 2f);
        fillAreaRt.offsetMax = new Vector2(-4f, -2f);

        // Fill
        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.raycastTarget = false;

        slider.fillRect = fillRt;
        return slider;
    }

    /// <summary>
    /// TMP 기본 폰트 에셋 반환. TMP Settings → TMP Essential Resources 순으로 탐색.
    /// </summary>
    private static TMP_FontAsset GetDefaultFont()
    {
        // 1순위: TMP Settings에 설정된 기본 폰트
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null) return font;

        // 2순위: TMP Essential Resources 임포트 경로
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font != null) return font;

        Debug.LogWarning("[NyanLinkUISetup] TMP 기본 폰트를 찾을 수 없습니다. " +
                         "Window > TextMeshPro > Import TMP Essential Resources 를 실행하세요.");
        return null;
    }

    // ──────────────────────────────────────────────────
    //  GameStateDebugPanel (검증3 전용)
    // ──────────────────────────────────────────────────

    private static void CreateDebugPanel(Transform canvas)
    {
        DestroyIfExists(canvas, "DebugPanel");

        // 루트 — 캔버스 좌측 상단 고정 320×380
        GameObject root = new GameObject("DebugPanel");
        root.transform.SetParent(canvas, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin        = new Vector2(0f, 1f);
        rootRt.anchorMax        = new Vector2(0f, 1f);
        rootRt.pivot            = new Vector2(0f, 1f);
        rootRt.sizeDelta        = new Vector2(320f, 380f);
        rootRt.anchoredPosition = new Vector2(10f, -10f);
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false; // 배경이 타일 클릭을 가로채지 않도록 (버튼 자체는 여전히 클릭 가능)

        GameStateDebugPanel panel = root.AddComponent<GameStateDebugPanel>();

        // 상태 텍스트 (상단)
        var stateText = MakeDebugText(root.transform, "Text_CurrentState", "State: Briefing",
            anchoredPos: new Vector2(10f, -10f), sizeDelta: new Vector2(300f, 40f));

        // 버튼 네 개 (세로 나열)
        // START: 어느 상태에서든 RunnerPhase 강제 전환 + GameReadyPanel 자동 닫힘
        var btnStart = MakeDebugButton(root.transform, "Button_ForceStart", "▶ START (RunnerPhase)",
            new Vector2(0f, -80f), new Vector2(290f, 58f), new Color(0.1f, 0.45f, 0.9f));

        var btnBoss = MakeDebugButton(root.transform, "Button_ForceBoss", "▶ Boss Phase",
            new Vector2(0f, -155f), new Vector2(290f, 58f), new Color(0.9f, 0.55f, 0.1f));

        var btnClear = MakeDebugButton(root.transform, "Button_ForceClear", "▶ Win (Clear)",
            new Vector2(0f, -230f), new Vector2(290f, 58f), new Color(0.15f, 0.75f, 0.25f));

        var btnLose = MakeDebugButton(root.transform, "Button_ForceGameOver", "▶ Lose (GameOver)",
            new Vector2(0f, -305f), new Vector2(290f, 58f), new Color(0.85f, 0.2f, 0.2f));

        SetFields(panel,
            ("textCurrentState", stateText),
            ("btnForceStart",    btnStart),
            ("btnForceBoss",     btnBoss),
            ("btnForceClear",    btnClear),
            ("btnForceGameOver", btnLose));

        Undo.RegisterCreatedObjectUndo(root, "Create DebugPanel");
    }

    // 디버그 패널 전용 텍스트 생성 (앵커: 루트 좌측 상단, piviot 좌상단)
    private static TextMeshProUGUI MakeDebugText(Transform parent, string name, string content,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text            = content;
        tmp.fontSize        = 26;
        tmp.alignment       = TextAlignmentOptions.Left;
        tmp.color           = Color.white;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget   = false;
        TMP_FontAsset font = GetDefaultFont();
        if (font != null) tmp.font = font;
        return tmp;
    }

    // 디버그 패널 전용 버튼 생성 (앵커: 루트 상단 중앙, pivot 중앙)
    private static Button MakeDebugButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = anchoredPos;

        Image img = go.AddComponent<Image>();
        img.color = color;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = color * 1.15f;
        cb.pressedColor     = color * 0.7f;
        btn.colors = cb;

        // 버튼 라벨
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 24;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        TMP_FontAsset font = GetDefaultFont();
        if (font != null) tmp.font = font;

        return btn;
    }

    /// <summary>
    /// SerializedObject를 통해 MonoBehaviour의 private SerializeField에 레퍼런스를 할당한다.
    /// </summary>
    private static void SetFields(MonoBehaviour target, params (string field, Object value)[] assignments)
    {
        SerializedObject so = new SerializedObject(target);
        foreach (var (field, value) in assignments)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[NyanLinkUISetup] '{target.GetType().Name}'에서 '{field}' 필드를 찾을 수 없습니다.");
        }
        so.ApplyModifiedProperties();
    }
}
