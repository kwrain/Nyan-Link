using UnityEngine;

namespace NyanLink.Core.HexCoordinates
{
    /// <summary>
    /// 헥사곤 좌표계 (Cube 좌표)
    /// Unity Tilemap의 Offset 좌표와 변환 가능
    /// </summary>
    [System.Serializable]
    public struct HexCoordinates
    {
        public int q; // Cube 좌표의 q (x와 유사)
        public int r; // Cube 좌표의 r (y와 유사)
        public int s; // Cube 좌표의 s (z와 유사, q + r + s = 0)

        public HexCoordinates(int q, int r, int s)
        {
            // Cube 좌표는 항상 q + r + s = 0이어야 함
            int sum = q + r + s;
            if (sum != 0)
            {
                // 자동 보정: 가장 가까운 유효한 Cube 좌표로 보정
                // 정수 나눗셈으로는 정확한 보정이 어려우므로, s를 재계산하는 방식 사용
                this.q = q;
                this.r = r;
                this.s = -q - r; // s를 재계산하여 q + r + s = 0 보장
            }
            else
            {
                this.q = q;
                this.r = r;
                this.s = s;
            }
        }

        /// <summary>
        /// Axial 좌표로 생성 (s는 자동 계산)
        /// </summary>
        public HexCoordinates(int q, int r)
        {
            this.q = q;
            this.r = r;
            this.s = -q - r;
        }

        /// <summary>
        /// Unity Tilemap의 Offset 좌표를 Cube 좌표로 변환 (Pointy-top 헥사곤)
        /// </summary>
        public static HexCoordinates OffsetToCube(Vector3Int offset)
        {
            int q = offset.x;
            int r = offset.y - (offset.x - (offset.x & 1)) / 2;
            return new HexCoordinates(q, r);
        }

        /// <summary>
        /// Cube 좌표를 Unity Tilemap의 Offset 좌표로 변환 (Pointy-top 헥사곤)
        /// </summary>
        public Vector3Int ToOffset()
        {
            int col = q;
            int row = r + (q - (q & 1)) / 2;
            return new Vector3Int(col, row, 0);
        }

        /// <summary>
        /// 두 좌표 간의 거리 계산
        /// </summary>
        public static int Distance(HexCoordinates a, HexCoordinates b)
        {
            return (Mathf.Abs(a.q - b.q) + Mathf.Abs(a.r - b.r) + Mathf.Abs(a.s - b.s)) / 2;
        }

        /// <summary>
        /// 다른 좌표와의 거리 계산
        /// </summary>
        public int DistanceTo(HexCoordinates other)
        {
            return Distance(this, other);
        }

        /// <summary>
        /// 인접 타일인지 확인 (거리가 1인지)
        /// </summary>
        public bool IsAdjacentTo(HexCoordinates other)
        {
            return DistanceTo(other) == 1;
        }

        /// <summary>
        /// 6방향 인접 타일 좌표 반환
        /// </summary>
        public HexCoordinates[] GetNeighbors()
        {
            return new HexCoordinates[]
            {
                new HexCoordinates(q + 1, r, s - 1),     // 오른쪽
                new HexCoordinates(q + 1, r - 1, s),     // 오른쪽 위
                new HexCoordinates(q, r - 1, s + 1),     // 왼쪽 위
                new HexCoordinates(q - 1, r, s + 1),     // 왼쪽
                new HexCoordinates(q - 1, r + 1, s),     // 왼쪽 아래
                new HexCoordinates(q, r + 1, s - 1)      // 오른쪽 아래
            };
        }

        /// <summary>
        /// 특정 방향의 인접 타일 좌표 반환
        /// </summary>
        /// <param name="direction">0~5 (시계방향, 0이 오른쪽부터)</param>
        public HexCoordinates GetNeighbor(int direction)
        {
            direction = direction % 6;
            if (direction < 0) direction += 6;

            HexCoordinates[] neighbors = GetNeighbors();
            return neighbors[direction];
        }

        public override bool Equals(object obj)
        {
            if (obj is HexCoordinates other)
            {
                return q == other.q && r == other.r && s == other.s;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return q.GetHashCode() ^ (r.GetHashCode() << 2) ^ (s.GetHashCode() >> 2);
        }

        public static bool operator ==(HexCoordinates a, HexCoordinates b)
        {
            return a.q == b.q && a.r == b.r && a.s == b.s;
        }

        public static bool operator !=(HexCoordinates a, HexCoordinates b)
        {
            return !(a == b);
        }

        public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.q + b.q, a.r + b.r, a.s + b.s);
        }

        public static HexCoordinates operator -(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.q - b.q, a.r - b.r, a.s - b.s);
        }

        public override string ToString()
        {
            return $"Hex({q}, {r}, {s})";
        }
    }
}
