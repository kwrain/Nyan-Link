using UnityEngine;
using UnityEngine.Tilemaps;
using System.Reflection;

namespace NyanLink.Puzzle
{
    /// <summary>
    /// 헥사곤 그리드 설정 계산 유틸리티
    /// 스프라이트 리소스 크기를 기준으로 정확한 Grid Cell Size 계산
    /// </summary>
    public static class HexGridCalculator
    {
        /// <summary>
        /// Pointy-top 헥사곤 스프라이트로부터 정확한 Grid Cell Size 계산
        /// </summary>
        /// <param name="sprite">헥사곤 스프라이트</param>
        /// <returns>계산된 Cell Size (X: 너비, Y: 높이)</returns>
        public static Vector3 CalculateCellSizeFromSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                Debug.LogWarning("HexGridCalculator: 스프라이트가 null입니다. 기본값 (1, 1) 반환");
                return new Vector3(1f, 1f, 0f);
            }

            // 스프라이트의 픽셀 크기
            float spriteWidth = sprite.texture.width;
            float spriteHeight = sprite.texture.height;
            float pixelsPerUnit = sprite.pixelsPerUnit;

            // 스프라이트의 월드 크기 (유닛)
            float worldWidth = spriteWidth / pixelsPerUnit;
            float worldHeight = spriteHeight / pixelsPerUnit;

            // Pointy-top 헥사곤의 실제 크기 계산
            // 스프라이트는 정사각형이지만, 실제 헥사곤은 그 안에 그려져 있음
            // 헥사곤의 반지름은 스프라이트 크기의 약 절반 (여백 제외)
            
            // 스프라이트의 바운딩 박스 크기 사용
            Bounds spriteBounds = sprite.bounds;
            float hexWidth = spriteBounds.size.x;   // 헥사곤의 너비 (좌우)
            float hexHeight = spriteBounds.size.y; // 헥사곤의 높이 (상하)

            // Pointy-top 헥사곤의 이론적 비율: 높이/너비 = sqrt(3)/2 ≈ 0.866
            // 실제 스프라이트의 비율과 비교하여 보정
            float theoreticalRatio = Mathf.Sqrt(3f) / 2f; // ≈ 0.866
            float actualRatio = hexHeight / hexWidth;

            // 스프라이트가 정사각형이고 헥사곤이 그 안에 그려져 있다면
            // 실제 헥사곤 크기는 스프라이트 크기보다 작을 수 있음
            // 바운딩 박스를 사용하면 정확한 크기를 얻을 수 있음

            Vector3 cellSize = new Vector3(hexWidth, hexHeight, 0f);

            Debug.Log($"[HexGridCalculator] 스프라이트 크기: {spriteWidth}x{spriteHeight}px, " +
                     $"Pixels Per Unit: {pixelsPerUnit}, " +
                     $"월드 크기: {worldWidth}x{worldHeight}, " +
                     $"헥사곤 바운딩: {hexWidth}x{hexHeight}, " +
                     $"비율: {actualRatio:F3} (이론값: {theoreticalRatio:F3}), " +
                     $"계산된 Cell Size: {cellSize}");

            return cellSize;
        }

        /// <summary>
        /// TileBase에서 스프라이트를 가져와 Cell Size 계산
        /// </summary>
        public static Vector3 CalculateCellSizeFromTile(TileBase tile)
        {
            if (tile == null)
            {
                return new Vector3(1f, 1f, 0f);
            }

            Sprite sprite = null;
            if (tile is Tile unityTile)
            {
                sprite = unityTile.sprite;
            }
            else
            {
                // ColoredHexagonTile 등 다른 Tile 타입도 처리
                // TileBase의 sprite 속성에 접근 시도
                var spriteProperty = tile.GetType().GetProperty("sprite");
                if (spriteProperty != null)
                {
                    sprite = spriteProperty.GetValue(tile) as Sprite;
                }
            }

            if (sprite == null)
            {
                Debug.LogWarning($"HexGridCalculator: 타일에서 스프라이트를 찾을 수 없습니다. 타일 타입: {tile.GetType()}");
                return new Vector3(1f, 1f, 0f);
            }

            return CalculateCellSizeFromSprite(sprite);
        }

        /// <summary>
        /// Grid 컴포넌트의 Cell Size를 자동으로 설정
        /// </summary>
        public static void ApplyOptimalCellSize(Grid grid, TileBase sampleTile)
        {
            if (grid == null)
            {
                Debug.LogError("HexGridCalculator: Grid가 null입니다.");
                return;
            }

            if (grid.cellLayout != GridLayout.CellLayout.Hexagon)
            {
                Debug.LogWarning($"HexGridCalculator: Grid의 Cell Layout이 Hexagon이 아닙니다! (현재: {grid.cellLayout})");
                return;
            }

            Vector3 optimalCellSize = CalculateCellSizeFromTile(sampleTile);
            grid.cellSize = optimalCellSize;

            Debug.Log($"[HexGridCalculator] Grid Cell Size를 {optimalCellSize}로 설정했습니다.");
        }
    }
}
