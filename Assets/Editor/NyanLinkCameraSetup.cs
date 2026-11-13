using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 분할 화면 카메라 자동 설정.
///
/// 레이어 분리:
///   TopView  → 러너 영역 오브젝트 (세계 Y = +50)
///   BottomView → 퍼즐 영역 오브젝트 (세계 Y = 0)
///
/// 뷰포트 분리:
///   Camera_Runner  — 화면 상단 45% : Rect(0, 0.55, 1, 0.45)
///   Camera_Puzzle  — 화면 하단 55% : Rect(0, 0.00, 1, 0.55)
/// </summary>
public static class NyanLinkCameraSetup
{
    /// <summary>씬 뷰에서 러너 월드가 위치할 Y. 퍼즐(Y=0)과 분리되어 겹치지 않는다.</summary>
    public const float RunnerWorldY = 50f;
    public const float PuzzleWorldY = 0f;

    // ── 카메라 ortho 사이즈 (게임 콘텐츠 크기에 맞게 Inspector에서 조정 가능) ──
    private const float RunnerOrthoSize = 3.0f;  // 러너 영역: 세로 6유닛 노출
    private const float PuzzleOrthoSize = 6.0f;  // 퍼즐 영역: 세로 12유닛 노출

    [MenuItem("NyanLink/Setup/Setup Split-Screen Cameras")]
    public static void SetupSplitCameras()
    {
        int topLayer = LayerMask.NameToLayer("TopView");
        int botLayer = LayerMask.NameToLayer("BottomView");

        if (topLayer < 0 || botLayer < 0)
        {
            EditorUtility.DisplayDialog(
                "레이어 없음",
                "'TopView' 또는 'BottomView' 레이어가 없습니다.\n\n" +
                "Edit > Project Settings > Tags and Layers 에서 추가하세요.",
                "확인");
            return;
        }

        DisableDefaultMainCamera();

        // Camera_Runner — 화면 상단 40%, TopView 레이어 (기획서 40:60 비율)
        SetupCamera(
            camName:       "Camera_Runner",
            layer:         topLayer,
            worldPos:      new Vector3(0, RunnerWorldY, -10),
            bgColor:       new Color(0.52f, 0.80f, 0.98f, 1f),   // 하늘색
            viewportRect:  new Rect(0f, 0.60f, 1f, 0.40f),
            orthoSize:     RunnerOrthoSize,
            depth:         1,
            audioListener: false
        );

        // Camera_Puzzle — 화면 하단 60%, BottomView 레이어 (기획서 40:60 비율)
        SetupCamera(
            camName:       "Camera_Puzzle",
            layer:         botLayer,
            worldPos:      new Vector3(0, PuzzleWorldY, -10),
            bgColor:       new Color(0.08f, 0.08f, 0.12f, 1f),   // 어두운 배경
            viewportRect:  new Rect(0f, 0.00f, 1f, 0.60f),
            orthoSize:     PuzzleOrthoSize,
            depth:         0,
            audioListener: true
        );

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            "[NyanLink] 분할 카메라 설정 완료!\n" +
            $"Camera_Runner → TopView 레이어, 월드 Y={RunnerWorldY}, 뷰포트 상단 40%\n" +
            $"Camera_Puzzle → BottomView 레이어, 월드 Y={PuzzleWorldY}, 뷰포트 하단 60%\n\n" +
            "■ 다음 단계:\n" +
            "  1. 러너 오브젝트(캐릭터, 장애물, 배경) → Layer: TopView, 위치 Y=50 부근\n" +
            "  2. 퍼즐 오브젝트(타일, 그리드)        → Layer: BottomView, 위치 Y=0 부근"
        );
    }

    // ── 내부 ───────────────────────────────────────────────────────────────────

    private static void SetupCamera(string camName, int layer,
        Vector3 worldPos, Color bgColor, Rect viewportRect,
        float orthoSize, int depth, bool audioListener)
    {
        Camera cam = FindCamera(camName);

        if (cam == null)
        {
            var go = new GameObject(camName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {camName}");
            cam = go.AddComponent<Camera>();
        }

        Undo.RecordObject(cam,            $"Configure {camName}");
        Undo.RecordObject(cam.transform,  $"Move {camName}");

        cam.clearFlags        = CameraClearFlags.SolidColor;
        cam.backgroundColor   = bgColor;
        cam.cullingMask       = 1 << layer;
        cam.orthographic      = true;
        cam.orthographicSize  = orthoSize;
        cam.rect              = viewportRect;
        cam.depth             = depth;
        cam.nearClipPlane     = 0.1f;
        cam.farClipPlane      = 100f;
        cam.transform.position = worldPos;
        cam.transform.rotation = Quaternion.identity;

        // AudioListener는 씬에 하나만 있어야 함
        var al = cam.GetComponent<AudioListener>();
        if (audioListener && al == null)
            cam.gameObject.AddComponent<AudioListener>();
        else if (!audioListener && al != null)
            Undo.DestroyObjectImmediate(al);
    }

    private static void DisableDefaultMainCamera()
    {
        foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (c.name == "Camera_Runner" || c.name == "Camera_Puzzle") continue;
            if (!c.CompareTag("MainCamera")) continue;

            Undo.RecordObject(c.gameObject, "Disable Main Camera");
            c.gameObject.SetActive(false);
            Debug.Log($"[NyanLink] 기본 Main Camera '{c.name}' 비활성화.");
        }
    }

    private static Camera FindCamera(string camName)
    {
        foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c.name == camName) return c;
        return null;
    }

    // ───────────────────────────────────────────────────────────────────────────
    //  레이어 일괄 설정 도우미
    // ───────────────────────────────────────────────────────────────────────────

    public static void AssignRunnerLayers()
    {
        int layer = LayerMask.NameToLayer("TopView");
        if (layer < 0) { Debug.LogError("[NyanLink] TopView 레이어가 없습니다."); return; }

        int count = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go, "Set TopView Layer");
            SetLayerRecursive(go, layer);
            count++;
        }
        Debug.Log($"[NyanLink] {count}개 오브젝트 → TopView 레이어 설정 완료.");
    }

    public static void AssignPuzzleLayers()
    {
        int layer = LayerMask.NameToLayer("BottomView");
        if (layer < 0) { Debug.LogError("[NyanLink] BottomView 레이어가 없습니다."); return; }

        int count = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go, "Set BottomView Layer");
            SetLayerRecursive(go, layer);
            count++;
        }
        Debug.Log($"[NyanLink] {count}개 오브젝트 → BottomView 레이어 설정 완료.");
    }

    private static bool ValidateSelection() => Selection.gameObjects.Length > 0;

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
