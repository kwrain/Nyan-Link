using UnityEngine;

namespace NineMood.Core
{
    /// <summary>
    /// 기획서04 §2 기준 화면 분할 카메라 관리.
    /// 하단 퍼즐 뷰 60% / 상단 러너 뷰 40% 고정 분할.
    ///
    /// 씬 계층:
    ///   CameraRig (DualCameraRig)
    ///     ├── Camera_Puzzle  — 하단 60%, Default 레이어, 퍼즐 월드(Y=0)
    ///     └── Camera_Runner  — 상단 40%, TopView 레이어, 러너 월드(Y=50)
    /// </summary>
    [ExecuteAlways]
    public class DualCameraRig : MonoBehaviour
    {
        [Header("카메라 참조")]
        public Camera cameraRunner;
        public Camera cameraPuzzle;

        [Header("화면 분할 — 기획서04: 퍼즐 60% / 러너 40%")]
        [Range(0.1f, 0.9f)]
        public float puzzleRatio = 0.60f;

        [Header("러너 뷰")]
        public float runnerWorldY    = 50f;
        public float runnerOrthoSize = 4f;

        [Header("퍼즐 뷰")]
        public float puzzleOrthoSize = 5f;

        private void Awake() => ApplySplit();

        private void OnValidate() => ApplySplit();

        [ContextMenu("Apply Split")]
        public void ApplySplit()
        {
            // ── 하단: 퍼즐 뷰 (Viewport y=0 ~ puzzleRatio) ──────
            if (cameraPuzzle != null)
            {
                cameraPuzzle.rect             = new Rect(0f, 0f, 1f, puzzleRatio);
                cameraPuzzle.orthographicSize = puzzleOrthoSize;
            }

            // ── 상단: 러너 뷰 (Viewport y=puzzleRatio ~ 1) ──────
            if (cameraRunner != null)
            {
                cameraRunner.rect             = new Rect(0f, puzzleRatio, 1f, 1f - puzzleRatio);
                cameraRunner.orthographicSize = runnerOrthoSize;

                Vector3 pos = cameraRunner.transform.position;
                pos.y = runnerWorldY;
                cameraRunner.transform.position = pos;
            }
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
