using UnityEngine;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 벌집형 그리드 크기 정의
    /// 스테이지별로 다른 크기의 벌집형 그리드를 설정할 수 있음
    /// 항상 완전한 벌집형 모양으로 생성됨 (마스크 없음)
    /// Unity Tilemap Offset 좌표계 사용
    /// </summary>
    [CreateAssetMenu(fileName = "GridShapeData", menuName = "NyanLink/Data/Grid Shape", order = 1)]
    public class GridShapeData : ScriptableObject
    {
        [Header("그리드 크기")]
        [Tooltip("그리드의 가로 크기 (열 개수)")]
        public int width = 7;

        [Tooltip("그리드의 세로 크기 (행 개수)")]
        public int height = 7;

        /// <summary>
        /// 특정 좌표가 그리드 범위 내에 있는지 확인
        /// 벌집형 그리드는 항상 완전한 모양이므로 범위 체크만 수행
        /// Unity Tilemap Offset 좌표계 사용
        /// </summary>
        public bool IsTileActive(Vector3Int offset)
        {
            // 범위 체크만 수행 (항상 완전한 벌집형 모양)
            return offset.x >= 0 && offset.x < width && offset.y >= 0 && offset.y < height;
        }
    }
}
