using System;
using System.Collections.Generic;
using NineMood.Services.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NineMood.Core.Fx
{
    /// <summary>
    /// Fx의 생성/재생/회수(풀링)를 담당하는 런타임 매니저.
    ///
    /// 1차(로드맵 1번) 목표:
    /// - <see cref="FxCore"/>가 의존하는 onEffectUpdate/ReleaseEffect 연결을 먼저 살린다.
    /// - fxId 기반(문서의 "타입 키")으로 최소한의 풀링/Alive 제한을 제공한다.
    ///
    /// 이후 단계에서 아래 항목을 확장한다:
    /// - Triggered 자동 완료(플레이타임 계산), Pause/Resume/Stop, FxGroup 연동, 레이어링/Sorting 정책
    /// - Addressables/커스텀 로더 도입(현재는 Resources 1차)
    /// </summary>
    public class FxManager : Singleton<FxManager>
    {
        /// <summary>
        /// 중앙 업데이트 브로드캐스트.
        /// 개별 Fx가 매니저에 리스트로 등록되는 구조 대신,
        /// Fx가 활성화될 때 구독하고 비활성/완료 시 해제하는 방식으로 비용/결합도를 낮춘다.
        /// </summary>
        public event Action onEffectUpdate;

        [Header("Fx Registry (Type -> attribute)")]
        // 1차 구현에서는 프로젝트 규모가 크지 않다는 전제 하에 인스펙터 등록형 레지스트리를 사용한다.
        // 추후 데이터 기반(테이블/어드레서블 라벨 검색)으로 전환할 때는 이 리스트를 provider로 교체한다.
        [SerializeField] private List<FxAttribute> attributes = new();

        // Type -> Attribute 매핑(식별자는 Type만 사용)
        private readonly Dictionary<Type, FxAttribute> attributeByType = new();
        // Type -> Pool (재사용 가능한 Fx 인스턴스)
        private readonly Dictionary<Type, Stack<FxCore>> poolByType = new();
        private readonly Dictionary<Type, List<FxCore>> fxByType = new();

        private GameObject effectPool;

        protected override void Awake()
        {
            base.Awake();

            InitFx();

            RebuildRegistry();
        }

        private void OnValidate()
        {
            RebuildRegistry();
        }

        private void Update()
        {
            // FxCore가 OnEnable에서 구독하고, OnComplete/OnDisable에서 해제한다.
            onEffectUpdate?.Invoke();
        }

        public bool TryGetAttribute(Type fxType, out FxAttribute attribute)
        {
            if (fxType == null)
            {
                attribute = null;
                return false;
            }
            return attributeByType.TryGetValue(fxType, out attribute) && attribute != null;
        }

        public bool TryGetAttribute<T>(out FxAttribute attribute) where T : FxCore
        {
            return TryGetAttribute(typeof(T), out attribute);
        }

        private void InitFx()
        {
            if (effectPool == null)
            {
                effectPool = new GameObject("EffectPool");
                effectPool.transform.SetParent(transform);
                effectPool.transform.localPosition = Vector3.zero;
                effectPool.transform.localRotation = Quaternion.identity;
                effectPool.transform.localScale = Vector3.one;
                effectPool.SetActive(false);
            }
        }

        private void RebuildRegistry()
        {
            attributeByType.Clear();
            if (attributes == null) return;

            // 인스펙터에서 등록된 FxAttribute 리스트를 FxType으로 인덱싱한다.
            // 중복 타입은 경고 후 최초 항목을 우선한다.
            foreach (var attr in attributes)
            {
                if (attr == null) continue;
                if (string.IsNullOrEmpty(attr.fxId)) continue;

                var fxType = attr.FxType;
                if (fxType == null)
                {
                    Debug.LogWarning($"[FxManager] Failed to resolve FxType from fxId=\"{attr.fxId}\". This attribute will be ignored.");
                    continue;
                }

                if (attributeByType.ContainsKey(fxType))
                {
                    Debug.LogWarning($"[FxManager] Duplicate fxType in registry: {fxType}");
                    continue;
                }

                attributeByType.Add(fxType, attr);
            }
        }

        /// <summary>
        /// Fx 인스턴스를 "생성/재사용"만 수행하고, 재생/등록은 호출자에게 위임한다.
        /// 호출자는 반환된 인스턴스에 필요한 값을 세팅한 뒤 <see cref="FxCore.Play"/>를 직접 호출한다.
        /// </summary>
        public T GetFx<T>() where T : FxCore
        {
            if (!TryGetAttribute<T>(out var attribute))
            {
                Debug.LogWarning($"[FxManager] FxAttribute not found. fxType={typeof(T)}");
                return null;
            }

            var fx = CreateFx<T>(attribute);
            if (fx == null)
            {
                Debug.LogWarning($"[FxManager] Failed to create Fx. fxType={typeof(T)}");
                return null;
            }

            fx.gameObject.SetActive(true);
            return fx;
        }

         private T CreateFx<T>(FxAttribute attribute) where T : FxCore
        {
            var fxType = typeof(T);
            if (string.IsNullOrEmpty(attribute.prefabPath))
            {
                Debug.LogWarning($"[FxManager] prefabPath is empty. fxType={fxType}");
                return null;
            }

            // Addressables 기반 로딩.
            // 현재는 즉시 FxCore 반환이 필요하므로 WaitForCompletion을 사용한다.
            // (추후 필요하면 InstantiateAsync 기반 비동기 API로 분리)
            AsyncOperationHandle<GameObject> handle = default;
            GameObject prefab = null;
            try
            {
                handle = Addressables.LoadAssetAsync<GameObject>(attribute.prefabPath);
                prefab = handle.WaitForCompletion();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FxManager] Addressables load threw exception. key={attribute.prefabPath}, fxType={fxType}, ex={e.Message}");
            }

            if (prefab == null || (handle.IsValid() && handle.Status != AsyncOperationStatus.Succeeded))
            {
                if (handle.IsValid()) Addressables.Release(handle);
                Debug.LogWarning($"[FxManager] Prefab not found in Addressables. key={attribute.prefabPath}, fxType={fxType}");
                return null;
            }

            var go = Instantiate(prefab);
            go.gameObject.SetActive(false);

            if (go == null)
            {
                Debug.LogWarning($"[FxManager] Failed to instantiate prefab. path={attribute.prefabPath}, fxType={fxType}");
                return null;
            }

            var fxCore = go.GetComponent<FxCore>();
            if (fxCore == null)
            {
                Debug.LogWarning($"[FxManager] Spawned prefab has no FxCore. path={attribute.prefabPath}, fxType={fxType}");
                Destroy(go);
                Addressables.Release(handle);
                return null;
            }

            if (fxType != null && !fxType.IsAssignableFrom(fxCore.GetType()))
            {
                Debug.LogWarning($"[FxManager] Spawned Fx type mismatch. expected={fxType}, actual={fxCore.GetType()}, path={attribute.prefabPath}");
            }

            // 생성 시 일단 풀로 집어넣는다.
            if (!poolByType.TryGetValue(fxType, out var pool))
            {
                pool = new Stack<FxCore>();
                poolByType[fxType] = pool;
            }
            pool.Push(fxCore);

            // 프리팹 핸들은 유지(Release하지 않음)하여 반복 재생 시 언로드로 인한 문제를 피한다.
            // 필요 시 이후 단계에서 "프리팹 캐시 + 명시적 해제"로 개선.
            return (T)fxCore;
        }

        public void ReleaseEffect(FxCore fx)
        {
            if (fx == null) 
            {
                Debug.LogWarning($"[FxManager] Fx is null.");
                return;
            }
            
            var fxType = fx.GetType();
            if(fxByType.TryGetValue(fxType, out var fxList))
            {
                fxList.Remove(fx);
            }
            
            if (!poolByType.TryGetValue(fxType, out var pool))
            {
                pool = new Stack<FxCore>();
                poolByType[fxType] = pool;
            }

            // 풀 반환 시 비활성화. (OnDisable에서 구독 해제가 수행되어야 함)
            fx.gameObject.SetActive(false);
            pool.Push(fx);
        }

        /// <summary>
        /// FxCore.Play()에서 호출되는 등록 함수.
        /// </summary>
        internal bool RegistFx(FxCore fx)
        {
            if (fx == null) 
                return false;

            var fxType = fx.GetType();
            if (!TryGetAttribute(fxType, out var attribute))
            {
                Debug.LogWarning($"[FxManager] FxAttribute not found. fxType={fxType}");
                return false;
            }

            // 이미 등록된 인스턴스라면 중복 등록을 막는다.
            if(fxByType.TryGetValue(fxType, out var fxList))
            {
                if(!fxList.Contains(fx))
                {
                    fxList.Add(fx);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
