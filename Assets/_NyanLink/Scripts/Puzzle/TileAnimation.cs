using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NineMood.Puzzle
{
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

        // 오프셋별 실행 중인 Tilemap 코루틴 추적 — 같은 위치에서 중복 실행 방지
        private readonly Dictionary<Vector3Int, Coroutine> _activeCoroutines
            = new Dictionary<Vector3Int, Coroutine>();

        // ── Tilemap 전용 스폰/제거 ────────────────────────────────────

        /// <summary>
        /// Tilemap 개별 타일 스폰 애니메이션 (SetTransformMatrix 방식).
        /// 같은 offset에 실행 중인 애니메이션이 있으면 취소 후 새로 시작한다.
        /// </summary>
        public void PlaySpawnOnTilemap(Tilemap tilemap, Vector3Int offset, System.Action onComplete = null)
        {
            CancelAt(offset);
            var c = StartCoroutine(SpawnTilemapCoroutine(tilemap, offset, onComplete));
            _activeCoroutines[offset] = c;
        }

        /// <summary>
        /// Tilemap 개별 타일 제거 애니메이션 (SetTransformMatrix 방식).
        /// onComplete에서 tilemap.SetTile(null) 호출을 권장.
        /// 같은 offset에서 PlaySpawnOnTilemap이 호출되면 이 코루틴은 자동 취소된다.
        /// </summary>
        public void PlayRemoveOnTilemap(Tilemap tilemap, Vector3Int offset, System.Action onComplete = null)
        {
            CancelAt(offset);
            var c = StartCoroutine(RemoveTilemapCoroutine(tilemap, offset, onComplete));
            _activeCoroutines[offset] = c;
        }

        private void CancelAt(Vector3Int offset)
        {
            if (_activeCoroutines.TryGetValue(offset, out Coroutine c) && c != null)
                StopCoroutine(c);
            _activeCoroutines.Remove(offset);
        }

        private IEnumerator SpawnTilemapCoroutine(Tilemap tilemap, Vector3Int offset, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < spawnDuration)
            {
                elapsed += Time.deltaTime;
                float s = spawnScaleCurve.Evaluate(elapsed / spawnDuration);
                tilemap.SetTransformMatrix(offset, Matrix4x4.Scale(new Vector3(s, s, 1f)));
                yield return null;
            }
            tilemap.SetTransformMatrix(offset, Matrix4x4.identity);
            _activeCoroutines.Remove(offset);
            onComplete?.Invoke();
        }

        private IEnumerator RemoveTilemapCoroutine(Tilemap tilemap, Vector3Int offset, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < removeDuration)
            {
                elapsed += Time.deltaTime;
                float s = removeScaleCurve.Evaluate(elapsed / removeDuration);
                tilemap.SetTransformMatrix(offset, Matrix4x4.Scale(new Vector3(s, s, 1f)));
                yield return null;
            }
            tilemap.SetTransformMatrix(offset, Matrix4x4.identity);
            _activeCoroutines.Remove(offset);
            onComplete?.Invoke();
        }

        // ── 기존 Transform 기반 메서드 (선택 애니메이션 등) ──────────

        public void PlaySpawnAnimation(Transform tileTransform, System.Action onComplete = null)
        {
            if (tileTransform == null) { onComplete?.Invoke(); return; }
            StartCoroutine(SpawnAnimationCoroutine(tileTransform, onComplete));
        }

        public void PlayRemoveAnimation(Transform tileTransform, System.Action onComplete = null)
        {
            if (tileTransform == null) { onComplete?.Invoke(); return; }
            StartCoroutine(RemoveAnimationCoroutine(tileTransform, onComplete));
        }

        public void PlaySelectAnimation(Transform tileTransform, bool isSelected, System.Action onComplete = null)
        {
            if (tileTransform == null) { onComplete?.Invoke(); return; }
            StartCoroutine(SelectAnimationCoroutine(tileTransform, isSelected, onComplete));
        }

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

        private void Reset()
        {
            spawnScaleCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1.2f),
                new Keyframe(1f, 1f)
            );

            removeScaleCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 1.1f),
                new Keyframe(1f, 0f)
            );

            selectScaleCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, selectScaleMultiplier),
                new Keyframe(1f, selectScaleMultiplier)
            );
        }
    }
}
