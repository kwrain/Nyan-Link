using System.Collections.Generic;
using UnityEngine;
using NyanLink.Data.Enums;
using NyanLink.Data.Definitions;
using NyanLink.Core.Managers;

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

        [Tooltip("밸런스 데이터 (비어있으면 DataManager에서 로드). 체인 티어/아이템 타일 수 제한에 사용")]
        public BalanceData balanceData;

        [Tooltip("아이템 효과 수치 데이터 (StaminaBoost 회복량 등)")]
        public TileEffectData tileEffectData;

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

            // 체인 정보
            TileInstance firstTileInstance = boardManager.GetTileAtOffset(selectedChain[0]);
            TileColor chainColor = firstTileInstance != null ? firstTileInstance.Color : TileColor.Red;
            int chainLength = selectedChain.Count;

            if (debugLog)
            {
                Debug.Log($"[매칭] 성공 - 체인 길이: {chainLength}, 색상: {chainColor}");
            }

            // 체인 티어 및 아이템 생성 여부 계산
            BalanceData activeBalance =
                balanceData != null ? balanceData :
                (DataManager.Instance != null ? DataManager.Instance.BalanceData : null);

            int effectLevel = ChainEvaluator.GetEffectLevel(activeBalance, chainLength);
            bool canCreateItemTile = false;
            TileState itemState = TileState.Normal;
            Vector3Int itemTilePosition = selectedChain[chainLength - 1];

            if (activeBalance != null && effectLevel > 0)
            {
                int currentItemCount = boardManager.GetItemTileCount();
                if (currentItemCount < activeBalance.maxItemTilesOnBoard)
                {
                    canCreateItemTile = true;
                    itemState = ChainEvaluator.EffectLevelToTileState(effectLevel);

                    if (debugLog)
                    {
                        Debug.Log($"[아이템 생성] 체인 길이 {chainLength}, 효과 레벨 {effectLevel}, 현재 아이템 {currentItemCount}/{activeBalance.maxItemTilesOnBoard}, 위치 {itemTilePosition}, 상태 {itemState}");
                    }
                }
                else if (debugLog)
                {
                    Debug.Log($"[아이템 생성 스킵] 최대 아이템 수 도달: {currentItemCount}/{activeBalance.maxItemTilesOnBoard}");
                }
            }

            // 아이템 효과 대상 타일 집합
            // - 체인/아이템 효과로 제거될 모든 타일을 이 집합에 모은다.
            // - 체인에 포함된 타일도 ApplyItemEffects 안에서 AddAffectedTile로 추가한다.
            HashSet<Vector3Int> tilesToRemove = new HashSet<Vector3Int>();

            // 체인 안에 포함된 아이템 타일들의 효과를 적용하여 제거 타일 계산
            ApplyItemEffects(selectedChain, tilesToRemove);

            // 모든 대상 타일 제거
            RemoveMatchedTiles(tilesToRemove);

            // 제거된 모든 위치에 새 타일 스폰 (마지막 타일 위치에 아이템 타일 생성 가능)
            SpawnNewTiles(tilesToRemove, canCreateItemTile, itemTilePosition, chainColor, itemState);
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
        private void RemoveMatchedTiles(IEnumerable<Vector3Int> tilesToRemove)
        {
            foreach (var tileOffset in tilesToRemove)
            {
                boardManager.RemoveTile(tileOffset);
            }
        }

        /// <summary>
        /// 새 타일 스폰
        /// - 기본적으로 랜덤 색상 일반 타일 생성
        /// - 조건 만족 시 마지막 타일 위치에 체인 색상의 아이템 타일 생성
        /// </summary>
        private void SpawnNewTiles(
            IEnumerable<Vector3Int> emptyPositions,
            bool createItemTile,
            Vector3Int itemTilePosition,
            TileColor itemColor,
            TileState itemState)
        {
            foreach (var position in emptyPositions)
            {
                if (createItemTile && position == itemTilePosition)
                {
                    // 마지막 타일 위치에 동일 색상의 아이템 타일 생성
                    boardManager.SpawnTile(position, itemColor, itemState);
                }
                else
                {
                    // 랜덤 색상 일반 타일 생성
                    TileColor randomColor = boardManager.GetRandomTileColor();
                    boardManager.SpawnTile(position, randomColor, TileState.Normal);
                }
            }
        }

        /// <summary>
        /// 체인 안에 포함된 아이템 타일들의 효과 적용
        /// - 각 아이템 타일의 타입/Lv에 따라 추가로 제거할 타일들을 tilesToRemove에 추가
        /// - 아이템 효과로 제거된 타일 중 또 다른 아이템 타일이 있으면, 그 타일의 효과도 연쇄적으로 발동
        /// - StaminaBoost는 타일 제거 없이 이벤트만 발행
        /// </summary>
        private void ApplyItemEffects(List<Vector3Int> chain, HashSet<Vector3Int> tilesToRemove)
        {
            var dataManager = DataManager.Instance;
            ColorEffectMappingData mapping = dataManager != null ? dataManager.ColorEffectMapping : null;

            // 연쇄 처리를 위한 큐와 처리済 아이템 집합
            Queue<Vector3Int> itemQueue = new Queue<Vector3Int>();
            HashSet<Vector3Int> processedItemSources = new HashSet<Vector3Int>();

            // 1차: 체인에 포함된 타일들을 제거 대상에 추가하고, 아이템 타일은 큐에 넣는다.
            foreach (var offset in chain)
            {
                AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);
            }

            // 큐에 쌓인 아이템 타일들을 하나씩 처리하면서, 효과로 새로 제거되는 타일 중
            // 또 다른 아이템 타일이 있으면 큐에 추가하는 방식으로 연쇄 처리
            while (itemQueue.Count > 0)
            {
                var offset = itemQueue.Dequeue();
                if (!processedItemSources.Add(offset))
                {
                    continue;
                }

                TileInstance tile = boardManager.GetTileAtOffset(offset);
                if (tile == null || tile.State == TileState.Normal)
                {
                    continue;
                }

                int level = tile.State == TileState.ItemLv2 ? 2 : 1;
                ItemEffectType effectType = ResolveEffectType(mapping, tile.Color);

                Debug.Log($"[아이템 효과] {effectType} Lv.{level} 발동 (위치: {offset}, 색상: {tile.Color})");

                switch (effectType)
                {
                    case ItemEffectType.AreaBlast:
                        ApplyAreaBlast(offset, level, tilesToRemove, itemQueue, processedItemSources);
                        break;
                    case ItemEffectType.HorizontalLine:
                        ApplyHorizontalLine(offset, level, tilesToRemove, itemQueue, processedItemSources);
                        break;
                    case ItemEffectType.DiagonalLeft:
                        ApplyDiagonalLine(offset, level, tilesToRemove, itemQueue, processedItemSources, isLeft: true);
                        break;
                    case ItemEffectType.DiagonalRight:
                        ApplyDiagonalLine(offset, level, tilesToRemove, itemQueue, processedItemSources, isLeft: false);
                        break;
                    case ItemEffectType.Rainbow:
                        ApplyRainbow(offset, level, tilesToRemove, itemQueue, processedItemSources);
                        break;
                    case ItemEffectType.StaminaBoost:
                        ApplyStaminaBoost(level);
                        break;
                }
            }
        }

        #region 아이템 효과 구현

        /// <summary>
        /// 색상 → 아이템 효과 타입 결정.
        /// - ColorEffectMappingData가 있으면 그 값을 사용
        /// - 없으면 PROJECT_ROADMAP/PHASE3_START_GUIDE에 정의된 기본 매핑 사용
        /// </summary>
        private static ItemEffectType ResolveEffectType(ColorEffectMappingData mapping, TileColor color)
        {
            if (mapping != null)
            {
                return mapping.GetEffectForColor(color);
            }

            // 기본 매핑 (ColorEffectMappingData.GetDefaultEffectForColor와 동일한 규칙)
            return color switch
            {
                TileColor.Red    => ItemEffectType.StaminaBoost,
                TileColor.Yellow => ItemEffectType.AreaBlast,
                TileColor.Blue   => ItemEffectType.HorizontalLine,
                TileColor.Purple => ItemEffectType.DiagonalLeft,
                TileColor.Orange => ItemEffectType.DiagonalRight,
                TileColor.Cyan   => ItemEffectType.Rainbow,
                _ => ItemEffectType.StaminaBoost
            };
        }

        /// <summary>
        /// 제거 대상 타일로 추가하고, 아이템 타일이면 큐에 넣어 연쇄 효과를 가능하게 한다.
        /// </summary>
        private void AddAffectedTile(
            Vector3Int offset,
            HashSet<Vector3Int> tilesToRemove,
            Queue<Vector3Int> itemQueue,
            HashSet<Vector3Int> processedItemSources)
        {
            TileInstance tile = boardManager.GetTileAtOffset(offset);
            if (tile == null || !tile.IsActive)
            {
                return;
            }

            // 새로 추가된 타일만 처리 (이미 제거 대상인 경우 중복 처리 방지)
            if (tilesToRemove.Add(offset))
            {
                if (tile.State != TileState.Normal && !processedItemSources.Contains(offset))
                {
                    itemQueue.Enqueue(offset);
                }
            }
        }

        private struct CubeCoord
        {
            public int x;
            public int y;
            public int z;

            public CubeCoord(int x, int y, int z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        /// <summary>
        /// Offset(odd-r, pointy-top) → Cube 좌표 변환
        /// </summary>
        private static CubeCoord OffsetToCube(Vector3Int offset)
        {
            int col = offset.x;
            int row = offset.y;
            int x = col - (row - (row & 1)) / 2;
            int z = row;
            int y = -x - z;
            return new CubeCoord(x, y, z);
        }

        /// <summary>
        /// Cube → Offset(odd-r, pointy-top) 변환
        /// </summary>
        private static Vector3Int CubeToOffset(CubeCoord cube)
        {
            int col = cube.x + (cube.z - (cube.z & 1)) / 2;
            int row = cube.z;
            return new Vector3Int(col, row, 0);
        }

        /// <summary>
        /// 주변 타일 파괴 (AreaBlast)
        /// - 중심에서 GetNeighbors로만 N걸음 이내 확장 (인접 정의와 완전 동일)
        /// - Lv.1: 반경 1 (중심 + 인접 6칸), Lv.2: 반경 2
        /// </summary>
        private void ApplyAreaBlast(
            Vector3Int centerOffset,
            int level,
            HashSet<Vector3Int> tilesToRemove,
            Queue<Vector3Int> itemQueue,
            HashSet<Vector3Int> processedItemSources)
        {
            int radius = level >= 2 ? 2 : 1;

            // BFS: 중심에서 거리 0, 1, ..., radius 이내인 오프셋 수집 (GetNeighbors만 사용)
            HashSet<Vector3Int> inRange = new HashSet<Vector3Int> { centerOffset };
            List<Vector3Int> frontier = new List<Vector3Int> { centerOffset };

            for (int step = 0; step < radius; step++)
            {
                List<Vector3Int> nextFrontier = new List<Vector3Int>();
                foreach (Vector3Int pos in frontier)
                {
                    foreach (Vector3Int neighbor in HexOffsetUtils.GetNeighbors(pos))
                    {
                        if (inRange.Add(neighbor))
                        {
                            nextFrontier.Add(neighbor);
                        }
                    }
                }
                frontier = nextFrontier;
            }

            foreach (Vector3Int offset in inRange)
            {
                AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);
            }
        }

        /// <summary>
        /// 가로 라인 파괴 (HorizontalLine)
        /// </summary>
        private void ApplyHorizontalLine(
            Vector3Int centerOffset,
            int level,
            HashSet<Vector3Int> tilesToRemove,
            Queue<Vector3Int> itemQueue,
            HashSet<Vector3Int> processedItemSources)
        {
            int y = centerOffset.y;

            // Lv.1: 기준 Y줄, Lv.2: 기준 줄 + 위/아래 줄
            int minY = level >= 2 ? y - 1 : y;
            int maxY = level >= 2 ? y + 1 : y;

            foreach (var offset in boardManager.GetAllTileOffsets())
            {
                if (offset.y >= minY && offset.y <= maxY)
                {
                    AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);
                }
            }
        }

        /// <summary>
        /// 대각선 라인 파괴 (DiagonalLeft/Right)
        /// - DiagonalLeft: Cube.x 기준 라인
        /// - DiagonalRight: Cube.y 기준 라인
        /// </summary>
        private void ApplyDiagonalLine(
            Vector3Int centerOffset,
            int level,
            HashSet<Vector3Int> tilesToRemove,
            Queue<Vector3Int> itemQueue,
            HashSet<Vector3Int> processedItemSources,
            bool isLeft)
        {
            CubeCoord centerCube = OffsetToCube(centerOffset);

            foreach (var offset in boardManager.GetAllTileOffsets())
            {
                CubeCoord cube = OffsetToCube(offset);

                if (isLeft)
                {
                    // 좌상–우하 방향: Cube.x 기준
                    int dx = cube.x - centerCube.x;
                    if (level >= 2)
                    {
                        if (Mathf.Abs(dx) <= 1)
                        {
                            AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);
                        }
                    }
                    else
                    {
                        if (dx == 0)
                        {
                            AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);
                        }
                    }
                }
                else
                {
                    // 우상–좌하 방향: Cube.y 기준
                    int dy = cube.y - centerCube.y;
                    if (level >= 2)
                    {
                        if (Mathf.Abs(dy) <= 1)
                        {
                            AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);
                        }
                    }
                    else
                    {
                        if (dy == 0)
                        {
                            AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 레인보우 효과
        /// - Lv.1: 와일드 타일 생성 (보드는 그대로, 효과는 이후 체인에서 발휘되므로 여기선 제거 없음)
        /// - Lv.2: 보드 전체 타일 제거
        /// </summary>
        private void ApplyRainbow(
            Vector3Int centerOffset,
            int level,
            HashSet<Vector3Int> tilesToRemove,
            Queue<Vector3Int> itemQueue,
            HashSet<Vector3Int> processedItemSources)
        {
            if (level >= 2)
            {
                // 보드 전체 타일 제거
                foreach (var offset in boardManager.GetAllTileOffsets())
                {
                    AddAffectedTile(offset, tilesToRemove, itemQueue, processedItemSources);
                }
            }
            else
            {
                // Lv.1: 와일드 타일 생성은 별도 아이템으로 설계될 수 있으므로,
                // 현재 매칭 시점에서는 추가 제거 없음.
            }
        }

        /// <summary>
        /// 스태미너 추가 회복 (StaminaBoost)
        /// - Phase 3: 타일 제거 없이 이벤트만 발행, 실제 스태미너 갱신은 Phase 4에서 처리
        /// </summary>
        private void ApplyStaminaBoost(int level)
        {
            if (tileEffectData == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("[아이템 효과] TileEffectData가 설정되지 않아 StaminaBoost 효과를 적용할 수 없습니다.");
                }
                return;
            }

            float amount = tileEffectData.GetEffectValue(ItemEffectType.StaminaBoost, level);
            ItemEffectEvents.TriggerStaminaBoost(amount);
        }

        #endregion

    }

    /// <summary>
    /// 아이템 효과 이벤트 집합.
    /// Phase 3에서는 StaminaBoost만 사용하며,
    /// 실제 스태미너 갱신은 러너/게임 시스템(Phase 4)에서 이 이벤트를 구독하여 처리한다.
    /// </summary>
    public static class ItemEffectEvents
    {
        /// <summary>
        /// 스태미너 추가 회복 요청 (양수: 회복량)
        /// </summary>
        public static event System.Action<float> OnStaminaBoost;

        public static void TriggerStaminaBoost(float amount)
        {
            OnStaminaBoost?.Invoke(amount);
        }
    }
}
