using UnityEngine;

namespace NineMood.UI
{
    public class PrototypeSafeArea : MonoBehaviour
    {
#if UNITY_EDITOR
        public static Rect? TestPixels;
#endif
        public static Rect Pixels
        {
            get
            {
#if UNITY_EDITOR
                if(TestPixels.HasValue) return TestPixels.Value;
#endif
                return Screen.safeArea;
            }
        }
        RectTransform rect;
        void Awake() {rect=GetComponent<RectTransform>();Apply();}
        void Update()=>Apply();
        void Apply()
        {
            if(rect==null || Screen.width<=0 || Screen.height<=0) return;
            Rect safe=Pixels;
            rect.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);
            rect.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
