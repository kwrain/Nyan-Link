using System;
using System.Collections.Generic;
using UnityEngine;

namespace NineMood.Presentation
{
    [Serializable] public sealed class PixelUIEntry
    {
        public string id, texture;
        public Rect rect;
        public Vector4 border;
    }
    [Serializable] public sealed class PixelUICatalog { public PixelUIEntry[] entries; }

    // Own only generated Sprite instances; imported textures remain Resources-owned.
    public static class PixelUIResources
    {
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static bool loaded;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Release();
            Application.quitting -= Release;
            Application.quitting += Release;
        }
        static void Release()
        {
            foreach (var sprite in sprites.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            sprites.Clear(); loaded = false;
        }
        public static Sprite Get(string id)
        {
            if (!loaded)
            {
                loaded = true;
                var json = Resources.Load<TextAsset>("Art/PixelUI/catalog");
                if (json == null) return null;
                foreach (var entry in JsonUtility.FromJson<PixelUICatalog>(json.text).entries)
                {
                    var texture = Resources.Load<Texture2D>("Art/PixelUI/" + entry.texture);
                    if (texture == null) continue;
                    var sprite = Sprite.Create(texture, entry.rect, Vector2.one * .5f, 100, 0,
                        SpriteMeshType.FullRect, entry.border);
                    sprite.name = "PixelUI_" + entry.id;
                    sprites.Add(entry.id, sprite);
                }
            }
            return sprites.TryGetValue(id, out var result) ? result : null;
        }
    }
}
