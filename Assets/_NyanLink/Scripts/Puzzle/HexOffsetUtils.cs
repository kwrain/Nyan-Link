using UnityEngine;

namespace NyanLink.Puzzle
{
    /// <summary>
    /// Unity Tilemap Offset 좌표 기반 헥사곤 유틸리티
    /// Pointy-top 헥사곤용
    /// </summary>
    public static class HexOffsetUtils
    {
        /// <summary>
        /// Offset 좌표의 인접 타일들 반환 (6방향)
        /// Pointy-top 헥사곤 기준 (Unity Tilemap odd-r 패턴)
        /// </summary>
        public static Vector3Int[] GetNeighbors(Vector3Int offset)
        {
            bool isEvenRow = offset.y % 2 == 0;
            
            if (isEvenRow)
            {
                // 짝수 행 (even-r) - Unity Tilemap 변환 공식에 맞춘 패턴
                return new Vector3Int[]
                {
                    new Vector3Int(offset.x + 1, offset.y, 0),      // 오른쪽
                    new Vector3Int(offset.x + 1, offset.y - 1, 0), // 오른쪽 위
                    new Vector3Int(offset.x, offset.y - 1, 0),      // 왼쪽 위
                    new Vector3Int(offset.x - 1, offset.y - 1, 0), // 왼쪽 (수정됨: (x-1, y-1))
                    new Vector3Int(offset.x - 1, offset.y, 0),     // 왼쪽 아래 (수정됨: (x-1, y))
                    new Vector3Int(offset.x, offset.y + 1, 0)      // 오른쪽 아래 (수정됨: (x, y+1))
                };
            }
            else
            {
                // 홀수 행 - 실제 Unity Tilemap 동작에 맞춘 패턴
                // 사용자가 확인한 실제 인접 타일 패턴 사용
                return new Vector3Int[]
                {
                    new Vector3Int(offset.x + 1, offset.y, 0),      // 오른쪽 (수정됨: (x+1, y+1) -> (x+1, y))
                    new Vector3Int(offset.x + 1, offset.y - 1, 0), // 오른쪽 위 (수정됨: (x+1, y) -> (x+1, y-1))
                    new Vector3Int(offset.x, offset.y - 1, 0),      // 왼쪽 위 (수정됨: (x-1, y-1) -> (x, y-1))
                    new Vector3Int(offset.x - 1, offset.y, 0),     // 왼쪽
                    new Vector3Int(offset.x, offset.y + 1, 0),      // 왼쪽 아래 (수정됨: (x-1, y+1) -> (x, y+1))
                    new Vector3Int(offset.x + 1, offset.y + 1, 0)  // 오른쪽 아래 (수정됨: (x, y+1) -> (x+1, y+1))
                };
            }
        }

        /// <summary>
        /// 두 Offset 좌표가 인접한지 확인
        /// 대칭성을 보장하기 위해 양방향으로 확인
        /// </summary>
        public static bool IsAdjacent(Vector3Int offset1, Vector3Int offset2)
        {
            // offset1의 인접 타일 목록에서 offset2 확인
            Vector3Int[] neighbors1 = GetNeighbors(offset1);
            foreach (var neighbor in neighbors1)
            {
                if (neighbor == offset2)
                {
                    return true;
                }
            }
            
            // 대칭성 보장: offset2의 인접 타일 목록에서도 offset1 확인
            // (일부 패턴에서 대칭성이 깨질 수 있으므로 양방향 확인)
            Vector3Int[] neighbors2 = GetNeighbors(offset2);
            foreach (var neighbor in neighbors2)
            {
                if (neighbor == offset1)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 두 Offset 좌표 간의 거리 계산
        /// GetNeighbors 함수를 사용하여 BFS로 거리 계산 (일치성 보장)
        /// </summary>
        public static int GetDistance(Vector3Int offset1, Vector3Int offset2)
        {
            // 같은 위치면 거리 0
            if (offset1 == offset2)
            {
                return 0;
            }
            
            // 먼저 인접 여부를 확인 (거리 1인 경우 빠른 반환)
            if (IsAdjacent(offset1, offset2))
            {
                return 1;
            }
            
            // 인접하지 않으면 BFS로 거리 계산 (GetNeighbors 사용)
            System.Collections.Generic.Queue<Vector3Int> queue = new System.Collections.Generic.Queue<Vector3Int>();
            System.Collections.Generic.HashSet<Vector3Int> visited = new System.Collections.Generic.HashSet<Vector3Int>();
            System.Collections.Generic.Dictionary<Vector3Int, int> distances = new System.Collections.Generic.Dictionary<Vector3Int, int>();
            
            queue.Enqueue(offset1);
            visited.Add(offset1);
            distances[offset1] = 0;
            
            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                int currentDistance = distances[current];
                
                // 인접 타일 확인
                Vector3Int[] neighbors = GetNeighbors(current);
                foreach (var neighbor in neighbors)
                {
                    if (neighbor == offset2)
                    {
                        // 목표 타일 발견
                        return currentDistance + 1;
                    }
                    
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        distances[neighbor] = currentDistance + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            // 경로를 찾을 수 없는 경우 (이론적으로는 발생하지 않아야 함)
            return int.MaxValue;
        }
    }
}
