using System.Collections.Generic;
using UnityEngine;
using NyanLink.Data.Enums;

namespace NyanLink.Puzzle
{
    /// <summary>
    /// 타일 매칭 로직 처리
    /// Unity Tilemap Offset 좌표계 사용
    /// </summary>
    public class TileMatcher : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("PuzzleBoardManager 참조")]
        public PuzzleBoardManager boardManager;

        [Header("매칭 설정")]
        [Tooltip("최소 매칭 개수")]
        public int minMatchCount = 2;

        [Header("디버그")]
        [Tooltip("매칭 디버그 로그 출력")]
        public bool debugLog = false;

        private void Awake()
        {
            if (boardManager == null)
            {
                boardManager = FindFirstObjectByType<PuzzleBoardManager>();
            }
        }

        /// <summary>
        /// 매칭 처리 및 타일 제거
        /// </summary>
        public void ProcessMatch(List<Vector3Int> selectedChain)
        {
            if (selectedChain == null || selectedChain.Count < minMatchCount)
            {
                if (debugLog)
                {
                    Debug.Log($"[매칭] 체인 길이 부족: {selectedChain?.Count ?? 0} < {minMatchCount}");
                }
                return;
            }

            // 체인 유효성 검증
            if (!ValidateChain(selectedChain))
            {
                if (debugLog)
                {
                    Debug.LogWarning("[매칭] 유효하지 않은 체인입니다.");
                }
                return;
            }

            if (debugLog)
            {
                TileInstance firstTile = boardManager.GetTileAtOffset(selectedChain[0]);
                Debug.Log($"[매칭] 성공 - 체인 길이: {selectedChain.Count}, 색상: {firstTile?.Color}");
            }

            // 타일 제거 및 새 타일 스폰
            RemoveMatchedTiles(selectedChain);
            SpawnNewTiles(selectedChain);
        }

        /// <summary>
        /// 체인 유효성 검증
        /// </summary>
        private bool ValidateChain(List<Vector3Int> chain)
        {
            if (chain == null || chain.Count < minMatchCount)
            {
                return false;
            }

            // 첫 번째 타일의 색상 확인
            TileInstance firstTile = boardManager.GetTileAtOffset(chain[0]);
            if (firstTile == null)
            {
                return false;
            }

            TileColor chainColor = firstTile.Color;

            // 모든 타일이 같은 색상이고 인접한지 확인
            for (int i = 0; i < chain.Count; i++)
            {
                TileInstance tile = boardManager.GetTileAtOffset(chain[i]);
                if (tile == null || tile.Color != chainColor)
                {
                    return false;
                }

                // 인접성 확인 (첫 번째 타일 제외)
                if (i > 0)
                {
                    if (!HexOffsetUtils.IsAdjacent(chain[i - 1], chain[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 매칭된 타일 제거
        /// </summary>
        private void RemoveMatchedTiles(List<Vector3Int> tilesToRemove)
        {
            foreach (var tileOffset in tilesToRemove)
            {
                boardManager.RemoveTile(tileOffset);
            }
        }

        /// <summary>
        /// 새 타일 스폰 (랜덤 색상)
        /// </summary>
        private void SpawnNewTiles(List<Vector3Int> emptyPositions)
        {
            foreach (var position in emptyPositions)
            {
                // 랜덤 색상으로 새 타일 생성
                TileColor randomColor = boardManager.GetRandomTileColor();
                boardManager.SpawnTile(position, randomColor);
            }
        }
    }
}
