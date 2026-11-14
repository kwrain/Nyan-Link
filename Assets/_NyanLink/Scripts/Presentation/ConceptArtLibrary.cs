using System;
using System.Collections.Generic;
using NineMood.Data.Enums;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NineMood.Presentation
{
    [Serializable] public sealed class ConceptSpriteRecord
    {
        public string id,texture;
        public float x,y,width,height,ppu,pivotX=.5f,pivotY=.5f;
    }
    [Serializable] public sealed class ConceptSpriteCatalog {public ConceptSpriteRecord[] entries;}
    public static class ConceptArtLibrary
    {
        const string Root="Art/ConceptPixel/";
        static Dictionary<string,Sprite> sprites;
        static readonly Dictionary<int,Tile> tiles=new Dictionary<int,Tile>();
        static readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Release();Application.quitting-=Release;Application.quitting+=Release;
        }
        static void Release()
        {
            foreach(var value in owned) if(value!=null) UnityEngine.Object.Destroy(value);
            owned.Clear();tiles.Clear();sprites=null;
        }
        public static Sprite Get(string id)
        {
            if(sprites==null)
            {
                var json=Resources.Load<TextAsset>(Root+"catalog");if(json==null)return null;
                sprites=new Dictionary<string,Sprite>();
                foreach(var entry in JsonUtility.FromJson<ConceptSpriteCatalog>(json.text).entries)
                {
                    var texture=Resources.Load<Texture2D>(Root+entry.texture);if(texture==null)continue;
                    var sprite=Sprite.Create(texture,new Rect(entry.x,entry.y,entry.width,entry.height),new Vector2(entry.pivotX,entry.pivotY),entry.ppu,0,SpriteMeshType.FullRect);
                    sprite.name="Concept_"+entry.id;owned.Add(sprite);sprites.Add(entry.id,sprite);
                }
            }
            return sprites.TryGetValue(id,out var result) ? result : null;
        }
        public static Tile Tile(TileColor color,TileState state)
        {
            int key=(int)color*3+(int)state;
            if(tiles.TryGetValue(key,out var existing))return existing;
            var sprite=Get("tile-"+(int)color);if(sprite==null)return null;
            var tile=ScriptableObject.CreateInstance<Tile>();tile.name=$"Concept_{color}_{state}";
            tile.sprite=sprite;tile.color=Color.white;tile.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;
            tile.flags=TileFlags.None;owned.Add(tile);tiles.Add(key,tile);return tile;
        }
    }
}
