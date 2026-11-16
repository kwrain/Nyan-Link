using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.Presentation
{
    // Reuses the existing selectable, slider, canvas and inventory bindings.
    // Decorative graphics never receive raycasts or change persisted equipment data.
    public static class PixelUISkin
    {
        public static bool Available => PixelUIResources.Get("button-blue") != null;
        public static void Surface(Image image, string id, float alpha = 1)
        {
            var sprite = PixelUIResources.Get(id);
            if (image == null || sprite == null) return;
            var kind = sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            var tint = new Color(1, 1, 1, alpha);
            if (image.sprite == sprite && image.type == kind && image.color == tint &&
                Mathf.Approximately(image.pixelsPerUnitMultiplier, 3)) return;
            image.sprite = sprite; image.overrideSprite = null;
            image.type = kind;
            image.pixelsPerUnitMultiplier = 3; image.color = tint;
        }
        public static Image Decoration(Transform parent, string name, string id,
            float x0, float y0, float x1, float y1)
        {
            var found = parent.Find(name);
            if (found != null) return found.GetComponent<Image>();
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.raycastTarget = false;
            PrototypePresentation.Place(image.rectTransform, x0, y0, x1, y1);
            Surface(image, id); return image;
        }
        public static void Button(Button button)
        {
            if (!(button.targetGraphic is Image image) || !Available) return;
            if (button.GetComponent<PixelButtonAudio>() == null) button.gameObject.AddComponent<PixelButtonAudio>();
            if (image.sprite != null && image.sprite.name.StartsWith("PixelUI_")) return;
            bool choice = false;
            for (var p = button.transform.parent; p != null; p = p.parent) if (p.name == "Color selection") choice = true;
            string label = button.GetComponentInChildren<TextMeshProUGUI>(true)?.text ?? button.name;
            label = KoreanText.ButtonRole(label);
            string id = label.Contains("UPGRADE") || label.Contains("EQUIPMENT") ? "button-gold" :
                label.Contains("NEW RUN") || label == "RESTART" || label == "EXIT" ? "button-red" :
                label == "PLAY" || label == "RESUME" || label == "CONTINUE" || label == "RETRY" || label == "EQUIP" ? "button-mint" : "button-blue";
            Surface(image, choice ? "button-blue" : id);
            if (image.TryGetComponent<Outline>(out var outline)) outline.enabled = false;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(.85f, 1, 1);
            colors.pressedColor = new Color(.55f, .72f, .8f);
            colors.selectedColor = Color.white; colors.disabledColor = new Color(.4f, .43f, .5f, .75f);
            colors.colorMultiplier = 1; colors.fadeDuration = .08f; button.colors = colors;
            foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = Color.white; text.fontStyle |= FontStyles.Bold;
                text.enableAutoSizing = true; text.fontSizeMax = text.fontSize;
                text.fontSizeMin = text.fontSize * .7f;
            }
            if (choice && System.Enum.TryParse(button.name, out TileColor color))
            {
                var swatch = Decoration(button.transform, "Pixel color icon", "icon-unknown", .04f, .18f, .30f, .82f);
                swatch.sprite = ConceptArtLibrary.Get("tile-" + (int)color); swatch.type = Image.Type.Simple; swatch.preserveAspect = true;
                var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
                PrototypePresentation.Place(text.rectTransform, .32f, .06f, .95f, .94f);
            }
        }
        public static void Gauge(Slider slider)
        {
            if (!Available || slider.fillRect == null) return;
            var fill = slider.fillRect.GetComponent<Image>();
            if (fill == null || fill.sprite != null && fill.sprite.name.StartsWith("PixelUI_")) return;
            var frame = Decoration(slider.transform, "Pixel gauge housing", "meter-frame", 0, 0, 1, 1);
            frame.transform.SetAsFirstSibling(); frame.pixelsPerUnitMultiplier = 5;
            var oldBackground = slider.transform.Find("Background")?.GetComponent<Image>();
            if (oldBackground != null) oldBackground.enabled = false;
            if (slider.handleRect != null) slider.handleRect.gameObject.SetActive(false);
            var area = slider.fillRect.parent as RectTransform;
            if (area != null && area != slider.transform)
            {
                area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
                area.offsetMin = new Vector2(8, 5); area.offsetMax = new Vector2(-8, -5);
            }
            Surface(fill, slider.name.Contains("Progress") ? "meter-progress" : slider.name.Contains("Boss") ? "meter-boss" : "meter-hp");
            fill.raycastTarget = false;
        }
        public static void EquipmentRow(Transform row, EquipmentData data)
        {
            if (!Available) return;
            Surface(row.GetComponent<Image>(), "panel-card");
            string icon = data == null ? "icon-unknown" : data.equipmentType == EquipmentType.Hat ? "icon-hat" :
                data.equipmentType == EquipmentType.Outfit ? "icon-outfit" : "icon-weapon";
            Slot(row, "Pixel gear icon", icon, .035f, .47f, .17f, .95f);
            var label = row.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                PrototypePresentation.Place(label.rectTransform, .20f, .49f, .96f, .95f);
                label.enableAutoSizing = true; label.fontSizeMax = 30; label.fontSizeMin = 20;
            }
            foreach (var button in row.GetComponentsInChildren<Button>(true)) Button(button);
        }
        public static void Slot(Transform parent, string name, string icon, float x0, float y0, float x1, float y1)
        {
            var frame = Decoration(parent, name, "panel-slot", x0, y0, x1, y1);
            var image = Decoration(frame.transform, "Icon", icon, .15f, .15f, .85f, .85f);
            image.preserveAspect = true;
        }
    }
}
