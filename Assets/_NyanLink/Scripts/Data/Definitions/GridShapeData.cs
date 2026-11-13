using System.Collections.Generic;
using UnityEngine;

namespace NineMood.Data.Definitions
{
    /// <summary>Pointy-top odd-r 보드 마스크. 짝수 행 width, 홀수 행 width-1칸.</summary>
    [CreateAssetMenu(fileName = "GridShapeData", menuName = "NyanLink/Data/Grid Shape", order = 1)]
    public class GridShapeData : ScriptableObject
    {
        [Header("그리드 크기")]
        [Min(2), Tooltip("짝수 행의 열 수. 프로토타입은 6")]
        public int width = 6;
        [Min(1), Tooltip("행 수. 프로토타입은 7")]
        public int height = 7;

        public int GetRowWidth(int row) => row >= 0 && row < height
            ? Mathf.Max(0, width - (row & 1)) : 0;

        // 보드/마스크/렌더링이 같은 0부터 시작하는 odd-r 셀을 사용한다.
        public bool IsTileActive(Vector3Int offset) => offset.z == 0 &&
            offset.x >= 0 && offset.x < GetRowWidth(offset.y);

        public IEnumerable<Vector3Int> GetActiveOffsets()
        {
            for (int row = 0; row < height; row++)
                for (int col = 0; col < GetRowWidth(row); col++)
                    yield return new Vector3Int(col, row, 0);
        }
    }
}
