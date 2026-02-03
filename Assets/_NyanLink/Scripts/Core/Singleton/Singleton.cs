using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace NyanLink.Core
{
    /// <summary>
    /// Unity에서 Singleton으로 존재할 GameObject을 생성 및 관리하는 클래스
    /// Global 하게 동일한 이름은 1개만 존재할 수 있다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : Component
    {
        static protected object objLock = new object();
        static protected T instance;
        static protected bool isApplicationExit = false;

        static protected string prefabPath = string.Empty;

        static private AsyncOperationHandle<GameObject> opHandle;

        static public bool IsApplicationExit => isApplicationExit;
        static public bool IsCreated => instance != null;

        static public T Instance
        { 
            get
            {
                // Application이 종료되면 null을 리턴한다.
                // isAppliactionExit은 OnApplicationQuit에서만 true로 설정되므로,
                // 종료시점에 SingletoneObject가 destroy 먼저되는 경우엔 Instance값을,
                // null로 리턴하고 다른쪽에서는 이 값을 체크해서 부작용이 생기지 않도록 한다

                if (Application.isPlaying && isApplicationExit)
                    return null;

                if (instance != null) return instance;

                if (string.IsNullOrEmpty(prefabPath))
                {
                    return CreateInstance();
                }
                else
                {
                    return CreateInstance(prefabPath);
                }
            }
        }

        static public T CreateInstance()
        {
            lock (objLock)
            {
                if (IsCreated == true) return instance;

                instance = FindAnyObjectByType<T>();
                if (instance != null)
                {
                    // 씬에서 찾은 경우, 싱글톤의 생명주기를 따라가도록 DontDestroyOnLoad를 적용합니다.
                    DontDestroyOnLoad(instance.gameObject);
                    return instance;
                }

                isApplicationExit = false;
                var className = typeof(T).Name;
                if (instance == null)
                {
                    GameObject go = new GameObject("# " + className);
                    instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        
        static public T CreateInstance(string prefabPath)
        {
            lock (objLock)
            {
                if (IsCreated == true) return instance;

                instance = FindAnyObjectByType<T>();
                if (instance != null)
                {
                    // 씬에서 찾은 경우, 싱글톤의 생명주기를 따라가도록 DontDestroyOnLoad를 적용합니다.
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(instance.gameObject);
                    }
                    return instance;
                }

                isApplicationExit = false;
                var className = typeof(T).Name;
                var goName = "# " + className;
                if (instance == null)
                {
                    GameObject goPrefab = null;
                    opHandle = Addressables.LoadAssetAsync<GameObject>($"{prefabPath}");
                    opHandle.WaitForCompletion();
                    if (opHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        goPrefab = opHandle.Result;
                    }
                    else if (opHandle.Status == AsyncOperationStatus.Failed)
                    {
                        Addressables.Release(opHandle);
                    }
                    
                    if (goPrefab == null)
                    {
                        goPrefab = Resources.Load<GameObject>(prefabPath);
                    }

                    GameObject go = null;
                    if (goPrefab != null)
                    {
                        go = Instantiate(goPrefab);
                    }
                    else
                    {
                        go = new GameObject(goName);
                    }

                    go.name = goName;
                    instance = go.GetComponent<T>();
                    if (instance == null)
                        instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        protected Dictionary<object, Dictionary<string, System.Action>> onApplicationQuit;

        public Singleton() { }

        /// <summary>
        /// 씬이 변경되기전에 호출된다.
        /// </summary>
        /// <param name="currScene"></param>
        protected virtual void ScenePreloadEvent(Scene currScene) { }
        
        /// <summary>
        /// 씬이 변경된 후 호출된다.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="SceneMode"></param>
        protected virtual void SceneLoadedEvent(Scene scene, LoadSceneMode SceneMode) { }

        protected virtual void Awake()
        {
            SceneManager.sceneLoaded += SceneLoadedEvent;
        }

        protected virtual void Start()
        {

        }
        
        public virtual void OnDestroyObject()
        {
            SceneManager.sceneLoaded -= SceneLoadedEvent;

            Destroy(this);
        }

        public virtual async Task Initialize() { }

        /// <summary>
        /// lds - 24.2.29 <br/>
        /// 유니티의 이벤트 함수 호출 순서는 OnApplicationQuit => OnDisable => OnDestroy <br/>
        /// 싱글톤이 아닌 외부 MonoBehaviour의 OnApplicationQuit, OnDisable, OnDestroy 에서 <br/>
        /// 해당 싱글톤을 접근하는 경우 호출 순서가 싱글톤이 먼저 OnApplicationQuit이 호출되거나 <br/>
        /// OnDestroy가 호출되어 Instance가 null인 경우가 있다. <br/>
        /// 따라서 외부에서 각 이벤트 시점에 싱글톤을 호출하지 않고, 미리 등록해두었다가 <br/>
        /// 해당 싱글톤의 OnApplicationQuit이 호출되는 시점에 등록해두었던 callback들을 호출하는 것으로 <br/>
        /// 에러를 방지하도록 할 수 있다. <br/>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="context"></param>
        /// <param name="callback"></param>
        public void AddOnApplicationQuitCallback(object target, string context, Action callback)
        {
            onApplicationQuit ??= new();
            if(onApplicationQuit.ContainsKey(target) == false)
            {
                onApplicationQuit.Add(target, new());
            }
            if(onApplicationQuit[target].ContainsKey(context) == false)
            {
                onApplicationQuit[target].Add(context, callback);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            // lds - 23.6.23,
            // Instance 속성필드의 주석 내용에 따라 추가
            // 또한, OnDestroy에서 isApplicationExit를 true로 변경하게되면
            // 필요에 의해 싱글턴 객체를 제거한 후 재생성이 불가하기 때문에
            // OnDestroy에서 isApplicationExit=true; 를 제거함.
            lock(objLock)
            {
                if(onApplicationQuit != null)
                {
                    foreach(var kv in onApplicationQuit)
                    {
                        foreach(var kv2 in kv.Value)
                        {
                            kv2.Value?.Invoke();
                        }
                    }
                    onApplicationQuit.Clear();
                }
                instance = null;
                isApplicationExit = true;
            }
        }

        protected virtual void OnDestroy()
        {
            lock(objLock)
            {
                if(opHandle.IsValid())
                {
                    Addressables.Release(opHandle);
                }
                instance = null;
            }
        }
    }
}
