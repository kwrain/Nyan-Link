using UnityEngine;

namespace NineMood.Core
{
    /// <summary>
    /// 기획서04 §2 기준 화면 분할 카메라 관리.
    /// 기준 6×7 보드의 타일 표시 크기를 유지하며 보드 높이에 따라 화면 분할.
    ///
    /// 씬 계층:
    ///   CameraRig (DualCameraRig)
    ///     ├── Camera_Puzzle  — 보드 높이에 맞춘 하단 영역, 퍼즐 월드(Y=0)
    ///     └── Camera_Runner  — 남는 상단 영역, TopView 레이어, 러너 월드(Y=50)
    /// </summary>
    [ExecuteAlways]
    public class DualCameraRig : MonoBehaviour
    {
        [Header("카메라 참조")]
        public Camera cameraRunner;
        public Camera cameraPuzzle;

        [Header("6×7 기준 화면 분할 — 퍼즐 60% / 러너 40%")]
        [Range(0.1f, 0.9f)]
        public float puzzleRatio = 0.60f;

        public const float HudGapRatio = .065f;
        public float CurrentPuzzleRatio { get; private set; } = .60f;
        private Bounds boardBounds, referenceBounds;
        private float boardPadding;
        private bool hasBoard;

        public void FitPuzzleBoard(Bounds bounds, Bounds reference, float padding)
        {
            boardBounds = bounds; referenceBounds = reference; boardPadding = padding; hasBoard = true;
            ApplySplit();
        }

        [Header("러너 뷰")]
        public float runnerWorldY    = 50f;
        public float runnerOrthoSize = 4f;

        [Header("퍼즐 뷰")]
        public float puzzleOrthoSize = 5f;

        private bool masksApplied;
        private Rect lastSafe;
        private void Awake() => ApplySplit();
        private void Update() {if(Application.isPlaying && lastSafe!=NineMood.UI.PrototypeSafeArea.Pixels) ApplySplit();}

        private void OnValidate() => ApplySplit();

        [ContextMenu("Apply Split")]
        public void ApplySplit()
        {
            Rect safe=Application.isPlaying ? NineMood.UI.PrototypeSafeArea.Pixels : new Rect(0,0,Screen.width,Screen.height);
            lastSafe=safe;
            float width=Mathf.Max(1,Screen.width),height=Mathf.Max(1,Screen.height);
            Rect viewport=new Rect(safe.x/width,safe.y/height,safe.width/width,safe.height/height);
            float referenceRatio = Mathf.Clamp(puzzleRatio, HudGapRatio + .01f, .9f);
            float puzzleHeight = referenceRatio - HudGapRatio;
            float orthoSize = puzzleOrthoSize;
            if (hasBoard && safe.width > 0 && safe.height > 0)
            {
                // Always measure against the original 6×7 footprint, never the reduced board width.
                // This keeps pixels per world unit identical when columns or rows are removed.
                float referenceAspect = safe.width / (safe.height * puzzleHeight);
                float referenceOrtho = Mathf.Max(referenceBounds.extents.y + boardPadding,
                    (referenceBounds.extents.x + boardPadding) / referenceAspect);
                float pixelsPerUnit = safe.height * puzzleHeight / (2f * Mathf.Max(.001f, referenceOrtho));
                float requestedHeight = safe.height * puzzleHeight +
                    (boardBounds.size.y - referenceBounds.size.y) * pixelsPerUnit;
                requestedHeight = Mathf.Max(requestedHeight, (boardBounds.size.y + 2f * boardPadding) * pixelsPerUnit);
                // Oversized boards still fit, while retaining space for the runner and HUD.
                float scale = Mathf.Min(1f, safe.width / Mathf.Max(1f, (boardBounds.size.x + 2f * boardPadding) * pixelsPerUnit),
                    safe.height * (.9f - HudGapRatio) / Mathf.Max(1f, requestedHeight));
                puzzleHeight = requestedHeight * scale / safe.height;
                orthoSize = requestedHeight / (2f * pixelsPerUnit);
            }
            CurrentPuzzleRatio = puzzleHeight + HudGapRatio;
            if (cameraPuzzle != null)
            {
                cameraPuzzle.rect = new Rect(viewport.x, viewport.y, viewport.width, viewport.height * puzzleHeight);
                cameraPuzzle.orthographicSize = orthoSize;
                if (hasBoard)
                {
                    var position = cameraPuzzle.transform.position;
                    position.x = boardBounds.center.x; position.y = boardBounds.center.y;
                    cameraPuzzle.transform.position = position;
                }
            }
            if (cameraRunner != null)
            {
                cameraRunner.rect = new Rect(viewport.x, viewport.y + viewport.height * CurrentPuzzleRatio,
                    viewport.width, viewport.height * (1f - CurrentPuzzleRatio));
                // Keep the runner's horizontal framing and ground height as the upper view grows.
                float runnerSize = runnerOrthoSize * (1f - CurrentPuzzleRatio) / (1f - referenceRatio);
                cameraRunner.orthographicSize = runnerSize;
                Vector3 pos = cameraRunner.transform.position;
                pos.y = runnerWorldY + runnerSize - runnerOrthoSize;
                cameraRunner.transform.position = pos;
            }
            if(Application.isPlaying && !masksApplied) { ApplyCullingMasks(); masksApplied = true; }
        }

        /// <summary>
        /// 레이어 기반 Culling Mask 적용.
        /// "TopView" 레이어가 Project Settings에 정의되어 있어야 한다.
        /// </summary>
        [ContextMenu("Apply Culling Masks")]
        public void ApplyCullingMasks()
        {
            int topViewLayer = LayerMask.NameToLayer("TopView");
            if (topViewLayer < 0)
            {
                Debug.LogWarning("[DualCameraRig] 'TopView' 레이어가 없습니다. " +
                                 "Project Settings > Tags and Layers에서 추가하세요.");
                return;
            }

            int topViewMask = 1 << topViewLayer;

            if (cameraRunner != null)
                cameraRunner.cullingMask = topViewMask;

            if (cameraPuzzle != null)
                cameraPuzzle.cullingMask = ~topViewMask;

            Debug.Log($"[DualCameraRig] Culling Mask 적용 완료. TopView layer = {topViewLayer}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 화면 분할선 시각화 (Scene 뷰)
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            float splitY = runnerWorldY - (runnerOrthoSize * 2f * puzzleRatio);
            Gizmos.DrawLine(new Vector3(-20f, splitY, 0f), new Vector3(20f, splitY, 0f));
        }
#endif
    }
}
