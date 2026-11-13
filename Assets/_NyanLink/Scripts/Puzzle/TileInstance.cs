using UnityEngine;
using NineMood.Data.Enums;

namespace NineMood.Puzzle
{
    /// <summary>
    /// 타일 인스턴스 데이터 구조
    /// Unity Tilemap Offset 좌표계 사용
    /// </summary>
    public class TileInstance
    {
        /// <summary>
        /// Unity Tilemap Offset 좌표
        /// </summary>
        public Vector3Int OffsetPosition { get; private set; }

        /// <summary>
        /// 타일 색상
        /// </summary>
        public TileColor Color { get; set; }

        /// <summary>
        /// 타일 상태 (일반, 아이템 Lv1, 아이템 Lv2)
        /// </summary>
        private TileState _state;
        public ItemEffectType ItemType { get; private set; }
        public TileState State
        {
            get => _state;
            set { _state = value; if (value == TileState.Normal) ItemType = ItemEffectType.None;
                else if (ItemType == ItemEffectType.None) ItemType = DefaultItemType(Color); }
        }
        public static ItemEffectType DefaultItemType(TileColor color) => color switch
        {
            TileColor.Red => ItemEffectType.Recovery, TileColor.Blue => ItemEffectType.TimeFreeze,
            TileColor.Yellow => ItemEffectType.Blast, TileColor.Purple => ItemEffectType.LineClear,
            TileColor.Orange => ItemEffectType.PowerUp, TileColor.Cyan => ItemEffectType.ColorShift,
            _ => ItemEffectType.None
        };
        public void SetItemType(ItemEffectType type) { ItemType = State == TileState.Normal ? ItemEffectType.None : type; }

        /// <summary>
        /// 선택 상태
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 활성화 상태
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public TileInstance(Vector3Int offsetPosition, TileColor color, TileState state = TileState.Normal)
        {
            OffsetPosition = offsetPosition;
            Color = color;
            ItemType = ItemEffectType.None;
            State = state;
            IsSelected = false;
            IsActive = true;
        }

        /// <summary>
        /// 타일 리셋 (풀링용)
        /// </summary>
        public void Reset(Vector3Int offsetPosition, TileColor color, TileState state = TileState.Normal)
        {
            OffsetPosition = offsetPosition;
            Color = color;
            ItemType = ItemEffectType.None;
            State = state;
            IsSelected = false;
            IsActive = true;
        }
    }
}
