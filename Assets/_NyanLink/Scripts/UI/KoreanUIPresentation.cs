using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NineMood.UI
{
    [DefaultExecutionOrder(1000)]
    public class KoreanUIPresentation : MonoBehaviour
    {
        public static TMP_FontAsset Font => Resources.Load<TMP_FontAsset>("Fonts/Galmuri/NyanLinkKoreanPixel");
        float scan;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; }
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main" && scene.name != "Game") return;
            new GameObject("Korean UI presentation").AddComponent<KoreanUIPresentation>();
            ApplyScene();
        }
        public static void Apply(TMP_Text text)
        {
            var font = Font;
            if (font != null && text.font != font) text.font = font;
            string localized = KoreanText.UI(text.text);
            if (!string.IsNullOrEmpty(localized)) localized = localized.Normalize(NormalizationForm.FormC);
            if (text.text != localized) text.text = localized;
        }
        public static void ApplySceneForValidation() => ApplyScene();
        static void ApplyScene()
        {
            foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Apply(text);
        }
        void LateUpdate()
        {
            scan -= Time.unscaledDeltaTime;
            if (scan <= 0) { ApplyScene(); scan = .1f; }
        }
    }
}
