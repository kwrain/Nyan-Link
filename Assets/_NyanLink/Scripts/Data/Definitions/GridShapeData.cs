using System.Collections.Generic;
using UnityEngine;

namespace NineMood.Data.Definitions
{
    /// <summary>Pointy-top odd-r 보드 마스크. 짝수 행 width, 홀수 행은 설정에 따라 한 칸 좁거나 넓다.</summary>
    [CreateAssetMenu(fileName = "GridShapeData", menuName = "NyanLink/Data/Grid Shape", order = 1)]
    public class GridShapeData : ScriptableObject
    {
        [Header("그리드 크기")]
        [Min(2), Tooltip("짝수 행의 열 수")]
        public int width = 6;
        [Min(1), Tooltip("행 수")]
        public int height = 7;

        [Tooltip("홀수 행을 한 칸 넓히고 양옆으로 반 칸씩 확장")]
        public bool widerOddRows;

        public int GetRowStart(int row) => row >= 0 && row < height && widerOddRows && (row & 1) == 1 ? -1 : 0;

        public int GetRowWidth(int row) => row >= 0 && row < height
            ? Mathf.Max(0, width + (widerOddRows ? 1 : -1) * (row & 1)) : 0;

        // 보드/마스크/렌더링이 같은 odd-r 좌표를 사용한다. 넓은 홀수 행은 x=-1부터 시작한다.
        public bool IsTileActive(Vector3Int offset) => offset.z == 0 &&
            offset.x >= GetRowStart(offset.y) && offset.x < GetRowStart(offset.y) + GetRowWidth(offset.y);

        public IEnumerable<Vector3Int> GetActiveOffsets()
        {
            for (int row = 0; row < height; row++)
                for (int col = GetRowStart(row); col < GetRowStart(row) + GetRowWidth(row); col++)
                    yield return new Vector3Int(col, row, 0);
        }
    }
}
