using System.Collections.Generic;
using NineMood.Core;
using UnityEngine;

namespace NineMood.UI
{
    /// <summary>Keep the score strip and save notice attached to the moving board boundary.</summary>
    public class BoardBoundaryHUD : MonoBehaviour
    {
        readonly Dictionary<RectTransform, Vector2[]> anchors = new Dictionary<RectTransform, Vector2[]>();
        DualCameraRig rig;
        GameHUD hud;
        float nextScan;

        void LateUpdate()
        {
            if (rig == null) rig = FindFirstObjectByType<DualCameraRig>();
            if (hud == null) hud = FindFirstObjectByType<GameHUD>();
            if (rig == null || hud == null) return;
            float delta = rig.CurrentPuzzleRatio - .60f;
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .25f;
                foreach (var rect in hud.GetComponentsInChildren<RectTransform>(true))
                {
                    if (anchors.ContainsKey(rect)) continue;
                    if (rect.name == "Text_Score" || rect.name == "Chain Tier" ||
                        rect.name == "Save status" || rect.name == "Pixel score tray")
                        anchors.Add(rect, new[] { rect.anchorMin, rect.anchorMax });
                }
            }
            foreach (var entry in anchors)
            {
                if (entry.Key == null) continue;
                entry.Key.anchorMin = entry.Value[0] + Vector2.up * delta;
                entry.Key.anchorMax = entry.Value[1] + Vector2.up * delta;
            }
        }
    }
}
