using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class NyanLinkKoreanFontPipeline
{
    const string Root = "Assets/Resources/Fonts/Galmuri/";
    public static void Prepare()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "Galmuri11.ttf");
        if (font == null) throw new System.Exception("Official Galmuri11 source font is missing");
        var asset = TMP_FontAsset.CreateFontAsset(font, 12, 1, GlyphRenderMode.RASTER_HINTED, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        asset.name = "NyanLinkKoreanPixel";
        asset.normalStyle = 0; asset.boldStyle = 0;
        var characters = new List<uint>();
        for (uint c = 0xAC00; c <= 0xD7A3; c++) characters.Add(c); // Every modern Hangul syllable, including legacy/user names.
        for (uint c = 32; c < 127; c++) characters.Add(c);
        for (uint c = 0x3131; c <= 0x3163; c++) characters.Add(c);
        characters.AddRange(new uint[]{0x00D7,0x00B7,0x2192,0x2026,0x25A1,0xFFFD});
        asset.TryAddCharacters(characters.ToArray(), out var missing);
        if (missing != null && missing.Any(c => c >= 0xAC00 && c <= 0xD7A3)) throw new System.Exception("A modern Korean syllable is missing");
        var legacy = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        if (legacy != null) asset.fallbackFontAssetTable.Add(legacy);
        string path = Root + "NyanLinkKoreanPixel.asset";
        if (File.Exists(path)) throw new System.Exception("Font asset already exists; preserve its GUID and use an explicit rebuild instead of overwriting");
        AssetDatabase.CreateAsset(asset, path);
        asset.material.name = "NyanLink Korean Pixel Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var texture in asset.atlasTextures)
        {
            texture.name = "NyanLink Korean Pixel Atlas"; texture.filterMode = FilterMode.Point;
            AssetDatabase.AddObjectToAsset(texture, asset);
        }
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
        var settings = new SerializedObject(TMP_Settings.instance);
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = asset;
        settings.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(TMP_Settings.instance);
        AssetDatabase.SaveAssets();
        File.WriteAllText("output/localization/2026-11-16-ko/font-build.txt",$"font=Galmuri11 v2.40.4\nmode={asset.atlasRenderMode}\npointSize={asset.faceInfo.pointSize}\ncharacters={asset.characterTable.Count}\natlasPages={asset.atlasTextures.Length}\nmissingNonHangul={string.Join(",",missing ?? new uint[0])}\nclearDynamicDataOnBuild=false\n");
        Debug.Log("Korean pixel font prepared; every11172 modern Hangul syllable is present.");
    }
}
