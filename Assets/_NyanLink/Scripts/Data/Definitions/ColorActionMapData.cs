using System;
using System.Collections.Generic;
using UnityEngine;
using NineMood.Data.Enums;

namespace NineMood.Data.Definitions
{
    [Serializable]
    public class ColorActionEntry
    {
        public TileColor color;
        public RunnerState action;
    }

    /// <summary>
    /// 타일 색상 → 러너 액션 매핑 ScriptableObject.
    /// Inspector에서 변경 가능하며, 없으면 기본 매핑을 사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorActionMap", menuName = "NyanLink/Data/Color Action Map", order = 9)]
    public class ColorActionMapData : ScriptableObject
    {
        [Tooltip("타일 색상별 러너 액션. 미설정 색상은 기본값을 사용한다.")]
        [SerializeField]
        private List<ColorActionEntry> entries = new List<ColorActionEntry>();

        private Dictionary<TileColor, RunnerState> _cache;

        public RunnerState GetAction(TileColor color)
        {
            BuildCacheIfNeeded();
            return _cache != null && _cache.TryGetValue(color, out var action)
                ? action
                : GetDefaultAction(color);
        }

        private void BuildCacheIfNeeded()
        {
            if (_cache != null) return;
            _cache = new Dictionary<TileColor, RunnerState>();
            foreach (var entry in entries)
            {
                if (!_cache.ContainsKey(entry.color))
                    _cache[entry.color] = entry.action;
            }
        }

        /// <summary>
        /// 기획서 기본 매핑 (데이터 에셋 없을 때 폴백)
        /// Red→Jump, Blue→Slide, Yellow→Block, Orange→Attack, Cyan→Invincible, Purple→Run(무동작)
        /// </summary>
        public static RunnerState GetDefaultAction(TileColor color) => color switch
        {
            TileColor.Red    => RunnerState.Jump,
            TileColor.Blue   => RunnerState.Slide,
            TileColor.Yellow => RunnerState.Block,
            TileColor.Orange => RunnerState.Attack,
            TileColor.Cyan   => RunnerState.Invincible,
            _                => RunnerState.Run
        };

#if UNITY_EDITOR
        private void OnValidate() => _cache = null;
#endif
    }
}
