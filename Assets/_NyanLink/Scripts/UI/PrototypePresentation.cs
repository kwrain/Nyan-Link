using System.Collections;
using System.Linq;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Runner;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NineMood.UI
{
    // Presentation only: retains scene resources, components, serialized references and gameplay rules.
    public class PrototypePresentation : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() {SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Main" && scene.name!="Game") return;
            new GameObject("Prototype presentation").AddComponent<PrototypePresentation>();
            if(!FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Any(l=>l.isActiveAndEnabled))
                FindFirstObjectByType<Camera>()?.gameObject.AddComponent<AudioListener>();
            _=AudioManager.Instance;
            if(scene.name=="Main") AudioManager.Instance.StopBGM();
        }
        IEnumerator Start()
        {
            yield return null; // Includes UI created by MainMenuController.Start.
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.isRootCanvas && c.renderMode==RenderMode.ScreenSpaceOverlay))
            {
                var children=canvas.transform.Cast<Transform>().ToArray();
                var safe=new GameObject("SafeArea",typeof(RectTransform),typeof(PrototypeSafeArea));safe.transform.SetParent(canvas.transform,false);
                foreach(var child in children) child.SetParent(safe.transform,false);
            }
            if(SceneManager.GetActiveScene().name=="Game")
            {
                var camera=FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault(c=>c.name=="Main Camera");
                if(camera!=null)
                {
                    camera.depth=-100;camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;
                    camera.backgroundColor=new Color(.035f,.05f,.10f);
                }
            }
            Layout();
        }
        public static void Place(RectTransform r,float x0,float y0,float x1,float y1)
        {if(r==null)return;r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
        static void FitText(TextMeshProUGUI text,float x0,float y0,float x1,float y1,float size)
        {Place(text.rectTransform,x0,y0,x1,y1);text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size*.65f;text.fontSizeMax=size;}
        void Layout()
        {
            var hud=FindFirstObjectByType<GameHUD>();if(hud==null)return;
            foreach(var text in hud.GetComponentsInChildren<TextMeshProUGUI>(true)) switch(text.name)
            {
                case "Text_Score": FitText(text,.05f,.566f,.45f,.596f,38);text.alignment=TextAlignmentOptions.Left;break;
                case "Chain Tier": FitText(text,.45f,.566f,.95f,.596f,26);break;
                case "Text_HPLabel": FitText(text,.05f,.93f,.14f,.96f,28);break;
                case "Text_Stamina": FitText(text,.2f,.895f,.65f,.925f,27);break;
                case "Save status": FitText(text,.05f,.60f,.95f,.62f,21);break;
            }
            Place(hud.transform.Find("Slider_Stamina")?.GetComponent<RectTransform>(),.17f,.936f,.72f,.951f);
            Place(hud.transform.Find("Slider_Progress")?.GetComponent<RectTransform>(),.05f,.98f,.95f,.989f);
            var warning=FindFirstObjectByType<RunnerWarningHUD>();if(warning!=null)Place(warning.warningPanel.GetComponent<RectTransform>(),.06f,.69f,.94f,.79f);
            var boss=FindFirstObjectByType<BossHUD>();if(boss!=null)
            {
                foreach(var t in boss.GetComponentsInChildren<TextMeshProUGUI>(true)) switch(t.name)
                {
                    case "Text_BossHP": FitText(t,.1f,.87f,.9f,.895f,27);break;
                    case "Text_Phase": FitText(t,.1f,.83f,.9f,.855f,24);break;
                    case "Text_PatternType": FitText(t,.1f,.80f,.9f,.825f,28);break;
                    case "Text_Instruction": FitText(t,.06f,.69f,.94f,.755f,30);break;
                    case "Boss Timer": FitText(t,.1f,.76f,.9f,.795f,34);break;
                }
                Place(boss.transform.Find("Slider_BossHP")?.GetComponent<RectTransform>(),.1f,.86f,.9f,.869f);
            }
            // Opaque modal backgrounds keep frozen mid-animation tiles from obscuring results.
            foreach(var panel in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p is GameReadyPanel || p is StageClearPanel || p is GameOverPanel))
                if(panel.TryGetComponent<Image>(out var bg)) bg.color=new Color(.035f,.05f,.10f,1);
            gameObject.AddComponent<PrototypeFeedback>();
        }
    }
}
