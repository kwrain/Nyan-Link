using UnityEngine;

namespace NineMood.Core
{
    /// <summary>씬 소유 서비스. 전역 데이터를 보존하는 외부 Singleton과 달리 씬을 따라 소멸한다.</summary>
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<T>();
                return _instance; // 로비 등 서비스가 없는 씬에 빈 매니저를 자동 생성하지 않는다.
            }
        }
        public static bool IsCreated => _instance != null;
        protected bool IsOwner => _instance == this;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                enabled = false;
                Destroy(this); // 다른 매니저/씬 참조가 붙은 루트를 파괴하지 않는다.
                return;
            }
            _instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (IsOwner) _instance = null;
        }
    }
}
