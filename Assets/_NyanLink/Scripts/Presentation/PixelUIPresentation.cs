using System.Collections;
using NineMood.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.Presentation
{
    public class PixelUIPresentation : MonoBehaviour
    {
        float scan;
        IEnumerator Start()
        {
            yield return null; // Existing menus and ConceptPixelPresentation create their graphics first.
            if (!PixelUISkin.Available) yield break;
            foreach (var menu in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (menu is MainMenuController || menu is GameReadyPanel)
                    Behind(menu.transform, "Pixel menu tray", "panel-modal", .12f, .11f, .88f, .57f, false);
                if (menu is StageClearPanel || menu is GameOverPanel)
                    Behind(menu.transform, "Pixel result frame", menu is StageClearPanel ? "panel-reward" : "panel-modal", .035f, .04f, .965f, .95f);
            }
            foreach (var slider in FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (slider.name == "Slider_Stamina") PrototypePresentation.Place(slider.GetComponent<RectTransform>(), .17f, .935f, .72f, .957f);
                if (slider.name == "Slider_Progress") PrototypePresentation.Place(slider.GetComponent<RectTransform>(), .05f, .977f, .95f, .994f);
                if (slider.name == "Slider_BossHP") PrototypePresentation.Place(slider.GetComponent<RectTransform>(), .1f, .858f, .9f, .872f);
                PixelUISkin.Gauge(slider);
            }
            var hud = FindFirstObjectByType<GameHUD>();
            if (hud != null)
            {
                Behind(hud.transform, "Pixel score tray", "panel-card", .025f, .553f, .975f, .598f);
                var heart = PixelUISkin.Decoration(hud.transform, "Pixel health icon", "icon-heart", .06f, .90f, .13f, .93f);
                heart.preserveAspect = true;
            }
            Scan();
        }
        void LateUpdate()
        {
            if (!PixelUISkin.Available) return;
            scan -= Time.unscaledDeltaTime;
            if (scan <= 0) { Scan(); scan = .1f; }
        }
        static void Behind(Transform root, string name, string sprite, float x0, float y0, float x1, float y1, bool first = true)
        {
            var image = PixelUISkin.Decoration(root, name, sprite, x0, y0, x1, y1);
            if (first) { if (image.transform.GetSiblingIndex() != 0) image.transform.SetAsFirstSibling(); }
            else
            {
                var backdrop = root.Find("Concept city backdrop");
                image.transform.SetSiblingIndex(backdrop == null ? 0 : backdrop.GetSiblingIndex() + 1);
            }
        }
        static void Scan()
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)) PixelUISkin.Button(button);
            foreach (var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                switch (image.name)
                {
                    case "Navy HUD backing": case "Boss info backing": case "Item badge":
                        PixelUISkin.Surface(image, "panel-card"); break;
                    case "PauseOverlay":
                        image.color = new Color(.015f, .025f, .065f, .9f);
                        Behind(image.transform, "Pixel pause frame", "panel-modal", .10f, .23f, .90f, .78f); break;
                    case "Color selection":
                        image.color = new Color(.015f, .025f, .065f, .9f);
                        Behind(image.transform, "Pixel choice frame", "panel-modal", .06f, .24f, .94f, .82f); break;
                    case "EquipmentOverlay":
                        Behind(image.transform, "Pixel equipment frame", "panel-modal", .015f, .008f, .985f, .992f);
                        var slots = PixelUISkin.Decoration(image.transform, "Pixel equipped tray", "panel-card", .04f, .715f, .96f, .827f);
                        if (slots.transform.GetSiblingIndex() != 1) slots.transform.SetSiblingIndex(1);
                        PixelUISkin.Slot(image.transform, "Pixel hat slot", "icon-hat", .075f, .781f, .13f, .813f);
                        PixelUISkin.Slot(image.transform, "Pixel outfit slot", "icon-outfit", .075f, .752f, .13f, .784f);
                        PixelUISkin.Slot(image.transform, "Pixel weapon slot", "icon-weapon", .075f, .723f, .13f, .755f);
                        var coin = PixelUISkin.Decoration(image.transform, "Pixel coin icon", "icon-coin", .13f, .87f, .19f, .905f); coin.preserveAspect = true;
                        break;
                    case "InventoryScroll":
                        PixelUISkin.Surface(image, "panel-card");
                        // The inventory is rectangular. Rect clipping also contains sliced card graphics
                        // while scrolling, independently of the decorative sprite alpha.
                        if (image.TryGetComponent<Mask>(out var stencil) && stencil.enabled) stencil.enabled = false;
                        if (image.GetComponent<RectMask2D>() == null)
                        {
                            var clip = image.gameObject.AddComponent<RectMask2D>();
                            clip.padding = new Vector4(12, 12, 12, 12);
                        }
                        break;
                }
            }
            foreach (var warning in Object.FindObjectsByType<RunnerWarningHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                PixelUISkin.Surface(warning.warningPanel.GetComponent<Image>(), "panel-modal");
        }
    }
}
