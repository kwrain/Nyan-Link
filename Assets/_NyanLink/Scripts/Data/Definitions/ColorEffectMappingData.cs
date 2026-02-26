using System;
using System.Collections.Generic;
using UnityEngine;
using NyanLink.Data.Enums;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 타일 색상별 아이템 효과 매핑 데이터.
    /// 추후 기획 변경 시 에디터에서 지정만 바꾸면 되도록 데이터로 관리.
    /// </summary>
    [Serializable]
    public class ColorEffectEntry
    {
        public TileColor color;
        public ItemEffectType effect;
    }

    /// <summary>
    /// 색상 → 아이템 효과 매핑 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorEffectMappingData", menuName = "NyanLink/Data/Color Effect Mapping", order = 1)]
    public class ColorEffectMappingData : ScriptableObject
    {
        [Tooltip("타일 색상별로 발동할 아이템 효과. 추후 변경 시 여기서만 수정하면 됨.")]
        [SerializeField]
        private List<ColorEffectEntry> mapping = new List<ColorEffectEntry>();

        private Dictionary<TileColor, ItemEffectType> _cache;

        /// <summary>
        /// 지정된 색상에 대응하는 아이템 효과 반환. 매핑 없으면 기본값 사용.
        /// </summary>
        public ItemEffectType GetEffectForColor(TileColor color)
        {
            BuildCacheIfNeeded();
            return _cache != null && _cache.TryGetValue(color, out var effect)
                ? effect
                : GetDefaultEffectForColor(color);
        }

        /// <summary>
        /// 매핑에 해당 색상이 포함되어 있는지
        /// </summary>
        public bool HasMappingForColor(TileColor color)
        {
            BuildCacheIfNeeded();
            return _cache != null && _cache.ContainsKey(color);
        }

        private void BuildCacheIfNeeded()
        {
            if (_cache != null)
                return;
            _cache = new Dictionary<TileColor, ItemEffectType>();
            if (mapping == null)
                return;
            foreach (var entry in mapping)
            {
                if (!_cache.ContainsKey(entry.color))
                    _cache[entry.color] = entry.effect;
            }
        }

        /// <summary>
        /// 에디터/리소스 변경 시 캐시 무효화 (에디터 전용)
        /// </summary>
        public void InvalidateCache()
        {
            _cache = null;
        }

        /// <summary>
        /// 기획서 기본 매핑 (매핑 데이터 없을 때 폴백)
        /// </summary>
        private static ItemEffectType GetDefaultEffectForColor(TileColor color)
        {
            return color switch
            {
                TileColor.Red => ItemEffectType.Recovery,
                TileColor.Blue => ItemEffectType.TimeFreeze,
                TileColor.Yellow => ItemEffectType.Blast,
                TileColor.Purple => ItemEffectType.LineClear,
                TileColor.Orange => ItemEffectType.PowerUp,
                TileColor.Cyan => ItemEffectType.Rainbow,
                _ => ItemEffectType.Recovery
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InvalidateCache();
        }
#endif
    }
}
