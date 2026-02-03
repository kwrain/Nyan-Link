using System.Collections;
using UnityEngine;

namespace NyanLink.Puzzle
{
    /// <summary>
    /// 타일 애니메이션 처리
    /// </summary>
    public class TileAnimation : MonoBehaviour
    {
        [Header("스폰 애니메이션")]
        [Tooltip("스폰 애니메이션 지속시간")]
        public float spawnDuration = 0.3f;

        [Tooltip("스폰 애니메이션 커브")]
        public AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("스폰 시작 스케일")]
        public Vector3 spawnStartScale = Vector3.zero;

        [Tooltip("스폰 최종 스케일")]
        public Vector3 spawnEndScale = Vector3.one;

        [Header("제거 애니메이션")]
        [Tooltip("제거 애니메이션 지속시간")]
        public float removeDuration = 0.2f;

        [Tooltip("제거 애니메이션 커브")]
        public AnimationCurve removeScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Tooltip("제거 시작 스케일")]
        public Vector3 removeStartScale = Vector3.one;

        [Tooltip("제거 최종 스케일")]
        public Vector3 removeEndScale = Vector3.zero;

        [Header("선택 애니메이션")]
        [Tooltip("선택 애니메이션 지속시간")]
        public float selectDuration = 0.15f;

        [Tooltip("선택 시 스케일 배율")]
        public float selectScaleMultiplier = 1.1f;

        [Tooltip("선택 애니메이션 커브")]
        public AnimationCurve selectScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.1f);

        /// <summary>
        /// 타일 스폰 애니메이션 실행
        /// </summary>
        public void PlaySpawnAnimation(Transform tileTransform, System.Action onComplete = null)
        {
            if (tileTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(SpawnAnimationCoroutine(tileTransform, onComplete));
        }

        /// <summary>
        /// 타일 제거 애니메이션 실행
        /// </summary>
        public void PlayRemoveAnimation(Transform tileTransform, System.Action onComplete = null)
        {
            if (tileTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(RemoveAnimationCoroutine(tileTransform, onComplete));
        }

        /// <summary>
        /// 타일 선택 애니메이션 실행
        /// </summary>
        public void PlaySelectAnimation(Transform tileTransform, bool isSelected, System.Action onComplete = null)
        {
            if (tileTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(SelectAnimationCoroutine(tileTransform, isSelected, onComplete));
        }

        /// <summary>
        /// 스폰 애니메이션 코루틴
        /// </summary>
        private IEnumerator SpawnAnimationCoroutine(Transform tileTransform, System.Action onComplete)
        {
            float elapsed = 0f;
            Vector3 originalScale = tileTransform.localScale;

            while (elapsed < spawnDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / spawnDuration;
                float curveValue = spawnScaleCurve.Evaluate(t);

                Vector3 currentScale = Vector3.Lerp(spawnStartScale, spawnEndScale, curveValue);
                tileTransform.localScale = Vector3.Scale(originalScale, currentScale);

                yield return null;
            }

            tileTransform.localScale = Vector3.Scale(originalScale, spawnEndScale);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 제거 애니메이션 코루틴
        /// </summary>
        private IEnumerator RemoveAnimationCoroutine(Transform tileTransform, System.Action onComplete)
        {
            float elapsed = 0f;
            Vector3 originalScale = tileTransform.localScale;

            while (elapsed < removeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / removeDuration;
                float curveValue = removeScaleCurve.Evaluate(t);

                Vector3 currentScale = Vector3.Lerp(removeStartScale, removeEndScale, curveValue);
                tileTransform.localScale = Vector3.Scale(originalScale, currentScale);

                yield return null;
            }

            tileTransform.localScale = Vector3.Scale(originalScale, removeEndScale);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 선택 애니메이션 코루틴
        /// </summary>
        private IEnumerator SelectAnimationCoroutine(Transform tileTransform, bool isSelected, System.Action onComplete)
        {
            float elapsed = 0f;
            Vector3 originalScale = tileTransform.localScale;
            float targetScale = isSelected ? selectScaleMultiplier : 1f;
            float startScale = tileTransform.localScale.x / originalScale.x;

            while (elapsed < selectDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / selectDuration;
                float curveValue = selectScaleCurve.Evaluate(t);

                float currentScale = Mathf.Lerp(startScale, targetScale, curveValue);
                tileTransform.localScale = originalScale * currentScale;

                yield return null;
            }

            tileTransform.localScale = originalScale * targetScale;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 기본 애니메이션 커브 초기화
        /// </summary>
        private void Reset()
        {
            // 스폰 애니메이션 커브 초기화
            spawnScaleCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1.2f),
                new Keyframe(1f, 1f)
            );

            // 제거 애니메이션 커브 초기화
            removeScaleCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 1.1f),
                new Keyframe(1f, 0f)
            );

            // 선택 애니메이션 커브 초기화
            selectScaleCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, selectScaleMultiplier),
                new Keyframe(1f, selectScaleMultiplier)
            );
        }
    }
}
