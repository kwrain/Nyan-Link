using UnityEngine;
using NyanLink.Data.Enums;

namespace NyanLink.Puzzle
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
        public TileInstance(Vector3Int offsetPosition, TileColor color)
        {
            OffsetPosition = offsetPosition;
            Color = color;
            IsSelected = false;
            IsActive = true;
        }

        /// <summary>
        /// 타일 리셋 (풀링용)
        /// </summary>
        public void Reset(Vector3Int offsetPosition, TileColor color)
        {
            OffsetPosition = offsetPosition;
            Color = color;
            IsSelected = false;
            IsActive = true;
        }
    }
}
