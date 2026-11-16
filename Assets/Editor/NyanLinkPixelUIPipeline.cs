using System;
using System.Collections.Generic;
using System.IO;
using NineMood.Presentation;
using UnityEditor;
using UnityEngine;

public static class NyanLinkPixelUIPipeline
{
    const string Root = "Assets/Resources/Art/PixelUI/";
    public static void Prepare()
    {
        var entries = new List<PixelUIEntry>();
        Add(entries, "buttons", 2, 2, new[] { "button-blue", "button-mint", "button-gold", "button-red" });
        Add(entries, "panels", 2, 2, new[] { "panel-modal", "panel-card", "panel-slot", "panel-reward" });
        Add(entries, "meters", 1, 4, new[] { "meter-frame", "meter-hp", "meter-progress", "meter-boss" });
        Add(entries, "icons", 3, 2, new[] { "icon-heart", "icon-coin", "icon-hat", "icon-outfit", "icon-weapon", "icon-unknown" });
        File.WriteAllText(Root + "catalog.json", JsonUtility.ToJson(new PixelUICatalog { entries = entries.ToArray() }, true));
        AssetDatabase.ImportAsset(Root + "catalog.json", ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Pixel UI catalog: " + entries.Count + " sprites. Readable CPU copies disabled.");
    }
    static void Add(List<PixelUIEntry> entries, string name, int cols, int rows, string[] ids)
    {
        string path = Root + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new Exception("Missing UI texture: " + path);
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
        importer.isReadable = true; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var pixels = texture.GetPixels32();
        int cw = texture.width / cols, ch = texture.height / rows;
        for (int i = 0; i < ids.Length; i++)
        {
            int x = i % cols * cw, y = (rows - 1 - i / cols) * ch;
            // Ignore detached generation speckles, retaining the largest opaque sprite island.
            var rect = LargestIsland(pixels, texture.width, x, y, cw, ch);
            bool framed = name == "buttons" || name == "panels" || ids[i] == "meter-frame";
            float edge = framed ? Mathf.Min(rect.width, rect.height) * (name == "panels" ? .15f : .23f) : 0;
            entries.Add(new PixelUIEntry { id = ids[i], texture = name, rect = rect, border = Vector4.one * Mathf.Ceil(edge) });
        }
        importer.isReadable = false; importer.SaveAndReimport();
    }
    static Rect LargestIsland(Color32[] pixels, int width, int x0, int y0, int w, int h)
    {
        var visited = new bool[w * h]; var queue = new int[w * h];
        int best = 0, bx0 = 0, by0 = 0, bx1 = 0, by1 = 0;
        for (int start = 0; start < visited.Length; start++)
        {
            if (visited[start]) continue;
            visited[start] = true;
            if (pixels[(y0 + start / w) * width + x0 + start % w].a < 221) continue;
            int head = 0, tail = 1, xmin = w, ymin = h, xmax = 0, ymax = 0;
            queue[0] = start;
            while (head < tail)
            {
                int at = queue[head++], x = at % w, y = at / w;
                xmin = Mathf.Min(xmin, x); ymin = Mathf.Min(ymin, y);
                xmax = Mathf.Max(xmax, x); ymax = Mathf.Max(ymax, y);
                Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
                void Visit(int nx, int ny)
                {
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) return;
                    int index = ny * w + nx;
                    if (visited[index]) return;
                    visited[index] = true;
                    if (pixels[(y0 + ny) * width + x0 + nx].a > 220) queue[tail++] = index;
                }
            }
            if (tail > best) { best = tail; bx0 = xmin; by0 = ymin; bx1 = xmax; by1 = ymax; }
        }
        if (best == 0) throw new Exception("Empty UI sprite cell");
        bx0 = Mathf.Max(0, bx0 - 1); by0 = Mathf.Max(0, by0 - 1);
        bx1 = Mathf.Min(w - 1, bx1 + 1); by1 = Mathf.Min(h - 1, by1 + 1);
        return new Rect(x0 + bx0, y0 + by0, bx1 - bx0 + 1, by1 - by0 + 1);
    }
}
