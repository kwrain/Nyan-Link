using System.Collections;
using NineMood.Data.Enums;
using UnityEngine;

namespace NineMood.Runner
{
    /// <summary>
    /// 러너 뷰의 플레이어블 캐릭터.
    /// RunnerController.CurrentState를 감시하여 상태별 시각 반응(이동/스케일/색상)을 적용한다.
    /// 스프라이트/애니메이터가 없으면 컬러 박스 플레이스홀더를 자동 생성한다.
    /// </summary>
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("같은 오브젝트 또는 부모의 RunnerController")]
        public RunnerController controller;

        [Header("점프")]
        public float jumpHeight    = 1.2f;
        public float jumpDuration  = 0.4f;

        [Header("슬라이드")]
        public float slideOffsetY  = -0.3f;
        public float slideScaleX   = 1.4f;
        public float slideScaleY   = 0.55f;

        [Header("공격")]
        public float attackOffsetX = 0.5f;

        [Header("무적 플래시")]
        public float flashInterval = 0.08f;

        [Header("비틀거림 흔들림")]
        public float stumbleShake  = 0.15f;

        // ── 내부 ───────────────────────────────────────────────
        private Renderer _sr;
        private Material _ownedMaterial;
        private Vector3 _baseLocalPos;
        private Vector3 _baseLocalScale;
        private Color   _baseColor;
        private Coroutine _activeRoutine;
        private RunnerState _prevState = RunnerState.Run;

        private void Awake()
        {
            _sr = GetComponent<Renderer>();
            if (_sr == null)
            {
                var sprite = gameObject.AddComponent<SpriteRenderer>();
                sprite.sprite = BuildPlaceholderSprite();
                _sr = sprite;
                SetColor(new Color(1f, 0.55f, 0.1f));   // 주황 (고양이 느낌)
                transform.localScale = Vector3.one * 0.8f;
            }
            if (!(_sr is SpriteRenderer)) _ownedMaterial = _sr.material;
            _baseColor = _sr is SpriteRenderer sr ? sr.color : _ownedMaterial.color;

            // TopView 레이어 자동 설정
            int layer = LayerMask.NameToLayer("TopView");
            if (layer >= 0) gameObject.layer = layer;
        }

        private void Start()
        {
            _baseLocalPos   = transform.localPosition;
            _baseLocalScale = transform.localScale;

            if (controller == null)
                controller = GetComponentInParent<RunnerController>();
        }

        private void Update()
        {
            if (controller == null) return;

            RunnerState cur = controller.CurrentState;
            if (cur == _prevState) return;

            _prevState = cur;
            ApplyState(cur);
        }

        // ── 상태 분기 ───────────────────────────────────────────

        private void ApplyState(RunnerState state)
        {
            // 이전 루틴 취소 후 시각 초기화
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
            ResetVisual();

            switch (state)
            {
                case RunnerState.Jump:
                    _activeRoutine = StartCoroutine(JumpRoutine());
                    break;

                case RunnerState.Slide:
                    transform.localPosition = _baseLocalPos + Vector3.up * slideOffsetY;
                    transform.localScale    = new Vector3(
                        _baseLocalScale.x * slideScaleX,
                        _baseLocalScale.y * slideScaleY,
                        _baseLocalScale.z);
                    break;

                case RunnerState.Block:
                    SetColor(new Color(1f, 0.9f, 0.2f));      // 노랑 방어
                    transform.localScale = _baseLocalScale * 1.15f;
                    break;

                case RunnerState.Attack:
                    _activeRoutine = StartCoroutine(AttackRoutine());
                    break;

                case RunnerState.Invincible:
                    SetColor(new Color(0.2f, 1f, 0.85f));     // 시안색
                    _activeRoutine = StartCoroutine(FlashRoutine());
                    break;

                case RunnerState.Stumble:
                    SetColor(Color.red);
                    _activeRoutine = StartCoroutine(StumbleRoutine());
                    break;

                // Run: ResetVisual()로 이미 복귀
            }
        }

        // ── 루틴 ───────────────────────────────────────────────

        private IEnumerator JumpRoutine()
        {
            Vector3 top = _baseLocalPos + Vector3.up * jumpHeight;
            float half = jumpDuration * 0.5f;
            yield return LerpLocalPos(_baseLocalPos, top, half);
            yield return LerpLocalPos(top, _baseLocalPos, half);
        }

        private IEnumerator AttackRoutine()
        {
            Vector3 fwd = _baseLocalPos + Vector3.right * attackOffsetX;
            yield return LerpLocalPos(_baseLocalPos, fwd, 0.08f);
            yield return LerpLocalPos(fwd, _baseLocalPos, 0.12f);
        }

        private IEnumerator FlashRoutine()
        {
            while (true)
            {
                _sr.enabled = !_sr.enabled;
                yield return new WaitForSeconds(flashInterval);
            }
        }

        private IEnumerator StumbleRoutine()
        {
            float elapsed = 0f, duration = 0.45f;
            while (elapsed < duration)
            {
                Vector3 shake = new Vector3(
                    Random.Range(-stumbleShake, stumbleShake),
                    Random.Range(-stumbleShake, stumbleShake), 0f);
                transform.localPosition = _baseLocalPos + shake;
                elapsed += Time.deltaTime;
                yield return null;
            }
            ResetVisual();
        }

        private IEnumerator LerpLocalPos(Vector3 from, Vector3 to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(from, to, t / duration);
                yield return null;
            }
            transform.localPosition = to;
        }

        // ── 내부 유틸 ───────────────────────────────────────────

        private void ResetVisual()
        {
            transform.localPosition = _baseLocalPos;
            transform.localScale    = _baseLocalScale;
            SetColor(_baseColor);
            _sr.enabled             = true;
        }

        private void SetColor(Color color)
        {
            if (_sr is SpriteRenderer sprite) sprite.color = color;
            else if (_ownedMaterial != null) _ownedMaterial.color = color;
        }

        private void OnDestroy()
        {
            if (_ownedMaterial != null) Destroy(_ownedMaterial);
        }

        /// <summary>32×32 흰색 텍스처로 기본 스프라이트 생성</summary>
        private static Sprite BuildPlaceholderSprite()
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius   = size * 0.45f;
            for (int i = 0; i < pixels.Length; i++)
            {
                int px = i % size, py = i / size;
                // 원형 마스크 (각지지 않은 플레이스홀더)
                float dist = Vector2.Distance(new Vector2(px, py), center);
                pixels[i] = dist <= radius ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
