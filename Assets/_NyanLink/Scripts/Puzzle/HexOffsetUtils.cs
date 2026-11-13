using System.Collections.Generic;
using NineMood.Core.HexCoordinates;
using UnityEngine;

namespace NineMood.Puzzle
{
    /// <summary>Pointy-top odd-r 어댑터. 모든 판정은 공통 Cube 좌표를 사용한다.</summary>
    public static class HexOffsetUtils
    {
        public static Vector3Int[] GetNeighbors(Vector3Int offset)
        {
            var cubeNeighbors = HexCoordinates.OffsetToCube(offset).GetNeighbors();
            var result = new Vector3Int[6];
            for (int i = 0; i < result.Length; i++) result[i] = cubeNeighbors[i].ToOffset();
            return result;
        }

        public static bool IsAdjacent(Vector3Int a, Vector3Int b) => GetDistance(a, b) == 1;

        public static int GetDistance(Vector3Int a, Vector3Int b) =>
            HexCoordinates.Distance(HexCoordinates.OffsetToCube(a), HexCoordinates.OffsetToCube(b));

        public static IEnumerable<Vector3Int> GetRange(Vector3Int center, int radius)
        {
            foreach (var cell in HexUtils.GetRange(HexCoordinates.OffsetToCube(center), radius))
                yield return cell.ToOffset();
        }

        /// <summary>가로 한 줄 또는 Cube 세 축의 라인. 보드 경계 필터링은 호출자가 담당.</summary>
        public static bool IsOnLine(Vector3Int center, Vector3Int cell, bool allAxes)
        {
            var a = HexCoordinates.OffsetToCube(center);
            var b = HexCoordinates.OffsetToCube(cell);
            return a.r == b.r || (allAxes && (a.q == b.q || a.s == b.s));
        }
    }
}
