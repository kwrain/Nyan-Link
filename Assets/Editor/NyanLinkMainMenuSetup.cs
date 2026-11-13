using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NineMood.UI;

/// <summary>
/// MainMenu 씬의 UI를 자동 생성한다.
/// NyanLink/Setup/Create Main Menu UI 메뉴에서 실행.
/// MainMenu 씬을 열고 실행할 것.
/// </summary>
public static class NyanLinkMainMenuSetup
{
    public static void CreateMainMenuUI()
    {
        Canvas canvas = GetOrCreateCanvas();
        EnsureEventSystem();

        GameObject root = CreateMenuRoot(canvas.transform);
        MainMenuController controller = root.AddComponent<MainMenuController>();

        // 타이틀
        MakeText(root.transform, "Text_Title", "NYAN LINK", 140,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.72f),
            offset: new Vector4(-420, -80, 420, 80));

        // Best Score
        var bestText = MakeText(root.transform, "Text_BestScore", "BEST: 0", 48,
            TextAlignmentOptions.Center,
            anchor: new Vector2(0.5f, 0.58f),
            offset: new Vector4(-300, -32, 300, 32));

        // PLAY 버튼
        Button playBtn = MakeButton(root.transform, "Button_Play", "PLAY", 64,
            anchor: new Vector2(0.5f, 0.44f),
            size: new Vector2(500, 130),
            color: new Color(0.18f, 0.58f, 1f));

        // EXIT 버튼
        Button exitBtn = MakeButton(root.transform, "Button_Exit", "EXIT", 48,
            anchor: new Vector2(0.5f, 0.30f),
            size: new Vector2(360, 100),
            color: new Color(0.35f, 0.35f, 0.38f));

        // 버전 텍스트 (우하단)
        MakeText(root.transform, "Text_Version", $"v{Application.version}", 28,
            TextAlignmentOptions.Right,
            anchor: new Vector2(1f, 0f),
            offset: new Vector4(-200, 20, -20, 56));

        SetFields(controller,
            ("textBestScore", bestText),
            ("buttonPlay", playBtn),
            ("buttonExit", exitBtn));

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[NyanLink] Main Menu UI 생성 완료! 씬을 저장하세요.");
    }

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
        scaler.referenceResolution = new Vector2(1080, 1920);
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
        es.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static GameObject CreateMenuRoot(Transform canvas)
    {
        Transform existing = canvas.Find("MainMenu");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject go = new GameObject("MainMenu");
        go.transform.SetParent(canvas, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.04f, 0.10f, 1f);
        bg.raycastTarget = true;
        return go;
    }

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
        tmp.raycastTarget = false;

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
        cb.pressedColor = color * 0.7f;
        btn.colors = cb;

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
        tmp.raycastTarget = false;

        TMP_FontAsset font = GetDefaultFont();
        if (font != null) tmp.font = font;
        return btn;
    }

    private static TMP_FontAsset GetDefaultFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null) return font;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
    }

    private static void SetFields(MonoBehaviour target, params (string field, Object value)[] assignments)
    {
        SerializedObject so = new SerializedObject(target);
        foreach (var (field, value) in assignments)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop != null) prop.objectReferenceValue = value;
        }
        so.ApplyModifiedProperties();
    }
}
