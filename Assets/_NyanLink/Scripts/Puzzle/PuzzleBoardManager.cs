using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NineMood.Puzzle
{
    /// <summary>
    /// 벌집형 그리드 생성 및 관리 매니저
    /// Unity Tilemap Offset 좌표계 직접 사용
    /// </summary>
    public class PuzzleBoardManager : MonoBehaviour
    {
        [Header("그리드 설정")]
        [Tooltip("Unity Tilemap 컴포넌트")]
        public Tilemap tilemap;

        [Tooltip("헥사곤 타일 에셋 (TileBase) - 기본 타일 (호환성용, 상태별 타일이 없을 때 사용)")]
        public TileBase hexagonTile;

        [System.Serializable]
        private struct TileEntry
        {
            public TileColor color;
            public TileState state;
            public TileBase tile;
        }

        [Header("색상별 × 상태별 AnimatedTile 에셋")]
        [Tooltip("에디터에서 'Reload Animated Tiles'로 자동 채워짐. 이 값이 런타임에서도 사용됨.")]
        [SerializeField]
        private TileEntry[] _tileEntries = new TileEntry[0];

        private Dictionary<TileColor, Dictionary<TileState, TileBase>> _tilesByColorAndState =
            new Dictionary<TileColor, Dictionary<TileState, TileBase>>();

        [Header("타일 자동 로드 설정")]
        [Tooltip("게임 시작 시 AnimatedTile 자동 로드")]
        public bool autoLoadTiles = true;

        [Tooltip("타일 에셋 경로")]
        private const string TILES_PATH = "Assets/_NyanLink/Art/Tiles";

        [Tooltip("그리드 쉐이프 데이터")]
        public GridShapeData gridShapeData;

        [Header("카메라 설정")]
        [Tooltip("그리드 중앙에 카메라 자동 배치")]
        public bool autoPositionCamera = true;
        [Min(0f)] public float cameraPadding = 0.25f;
        private Camera _fittedCamera;
        private Rect _fittedPixelRect;

        /// <summary>요구 색 전환은 장애물/보스 시스템이 이 API로 통지한다.</summary>
        public TileColor? RequiredMatchColor { get; private set; }

        [Header("레이어")]
        [Tooltip("퍼즐 오브젝트에 적용할 레이어 (Camera_Puzzle culling mask와 일치해야 함)")]
        public string puzzleBoardLayer = "BottomView";

        [Header("애니메이션")]
        [Tooltip("같은 GameObject에 붙은 TileAnimation. 없으면 애니메이션 없이 즉시 처리")]
        [SerializeField] private TileAnimation tileAnimation;

        [Header("디버그")]
        [Tooltip("그리드 생성 시 로그 출력")]
        public bool debugLog = false;

        /// <summary>
        /// 모든 타일 인스턴스 딕셔너리 (Offset 좌표 기준)
        /// </summary>
        private Dictionary<Vector3Int, TileInstance> _tiles = new Dictionary<Vector3Int, TileInstance>();

        /// <summary>
        /// 그리드가 생성되었는지 여부
        /// </summary>
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            if (tilemap == null)
            {
                tilemap = GetComponent<Tilemap>();
                if (tilemap == null)
                    Debug.LogError("PuzzleBoardManager: Tilemap 컴포넌트를 찾을 수 없습니다!");
            }

            if (tileAnimation == null)
                tileAnimation = GetComponent<TileAnimation>();

            if (autoLoadTiles)
                LoadAnimatedTiles();
        }

        /// <summary>
        /// 그리드 초기화 및 생성
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (gridShapeData == null)
            {
                Debug.LogError("PuzzleBoardManager: GridShapeData가 할당되지 않았습니다!");
                return;
            }

            if (tilemap == null)
            {
                Debug.LogError("PuzzleBoardManager: Tilemap이 할당되지 않았습니다!");
                return;
            }

            CreateGrid();
            _isInitialized = true;

            ApplyPuzzleLayer();

            if (autoPositionCamera)
            {
                PositionCameraAtCenter();
            }
        }

        public void ResetBoard()
        {
            if (!_isInitialized) { Initialize(); return; }
            tileAnimation?.CancelAll(tilemap);
            CreateGrid();
            if (autoPositionCamera) FitCameraToBoard();
        }

        /// <summary>
        /// 그리드 생성 (GridShapeData 기반)
        /// </summary>
        private void CreateGrid()
        {
            if (gridShapeData == null || tilemap == null)
            {
                return;
            }

            _tiles.Clear();
            tilemap.ClearAllTiles();
            foreach (var offset in gridShapeData.GetActiveOffsets())
            {
                var color = GetRandomTileColor();
                _tiles[offset] = new TileInstance(offset, color, TileState.Normal);
                tilemap.SetTile(offset, GetTileByColorAndState(color, TileState.Normal) ?? hexagonTile);
            }
            EnsureRequiredColorPair();
            if (debugLog) Debug.Log($"[보드 초기화 완료] 총 {_tiles.Count}개 타일 생성");
        }

        /// <summary>
        /// Offset 좌표로 타일 조회
        /// </summary>
        public TileInstance GetTileAtOffset(Vector3Int offset)
        {
            _tiles.TryGetValue(offset, out TileInstance tile);
            return tile;
        }

        /// <summary>
        /// 현재 보드에 존재하는 모든 타일 Offset 좌표 열거
        /// </summary>
        public IEnumerable<Vector3Int> GetAllTileOffsets()
        {
            return _tiles.Keys;
        }

        /// <summary>현재 보드에 등록된 타일 수 (초기화 여부 진단용)</summary>
        public int GetTileCount() => _tiles.Count;

        public int GetItemTileCount()
        {
            int count = 0;
            foreach (var kv in _tiles)
            {
                TileInstance tile = kv.Value;
                if (tile != null && (tile.State == TileState.ItemLv1 || tile.State == TileState.ItemLv2))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 특정 ItemEffectType의 아이템 타일 수를 반환. TileMatcher.CanPlaceItemTile에서 사용.
        /// </summary>
        public int CountItemsOfType(ItemEffectType effectType)
        {
            ColorEffectMappingData mapping = DataManager.Instance?.ColorEffectMapping;
            int count = 0;
            foreach (var kv in _tiles)
            {
                TileInstance tile = kv.Value;
                if (tile == null || !tile.IsActive || tile.State == TileState.Normal) continue;
                ItemEffectType tileEffect = mapping != null
                    ? mapping.GetEffectForColor(tile.Color)
                    : DefaultEffectForColor(tile.Color);
                if (tileEffect == effectType) count++;
            }
            return count;
        }

        private static ItemEffectType DefaultEffectForColor(TileColor color) => color switch
        {
            TileColor.Red    => ItemEffectType.Recovery,
            TileColor.Blue   => ItemEffectType.TimeFreeze,
            TileColor.Yellow => ItemEffectType.Blast,
            TileColor.Purple => ItemEffectType.LineClear,
            TileColor.Orange => ItemEffectType.PowerUp,
            TileColor.Cyan   => ItemEffectType.ColorShift,
            _                => ItemEffectType.None
        };

        /// <summary>
        /// 셀 중심 월드 위치 계산
        /// </summary>
        public Vector3 GetCellCenterWorld(Vector3Int offset)
        {
            if (tilemap != null)
            {
                return tilemap.GetCellCenterWorld(offset);
            }
            return Vector3.zero;
        }

        /// <summary>
        /// 색상과 상태에 따른 타일 에셋 반환
        /// </summary>
        public TileBase GetTileByColorAndState(TileColor color, TileState state)
        {
            // 딕셔너리에서 타일 찾기
            if (_tilesByColorAndState.TryGetValue(color, out var stateDict))
            {
                if (stateDict.TryGetValue(state, out var tile))
                {
                    return tile;
                }
            }

            // 타일을 찾을 수 없으면 기본 타일 반환
            return hexagonTile;
        }

        /// <summary>
        /// 색상에 따른 타일 에셋 반환 (호환성용, 일반 상태 사용)
        /// </summary>
        [System.Obsolete("GetTileByColor는 더 이상 사용되지 않습니다. GetTileByColorAndState를 사용하세요.")]
        public TileBase GetTileByColor(TileColor color)
        {
            return GetTileByColorAndState(color, TileState.Normal);
        }

        /// <summary>
        /// 랜덤 타일 색상 생성 (모든 색상 사용)
        /// </summary>
        public TileColor GetRandomTileColor()
        {
            TileColor[] allColors = { TileColor.Red, TileColor.Blue, TileColor.Yellow, TileColor.Purple, TileColor.Orange, TileColor.Cyan };
            return allColors[Random.Range(0, allColors.Length)];
        }

        /// <summary>
        /// 새 타일 스폰. 같은 위치에 진행 중인 제거 애니메이션이 있으면 자동으로 취소된다.
        /// </summary>
        public TileInstance SpawnTile(Vector3Int offset, TileColor? color = null, TileState? state = null)
        {
            if (gridShapeData == null || !gridShapeData.IsTileActive(offset)) return null;
            TileColor tileColor = color ?? GetRandomTileColor();
            TileState tileState = state ?? TileState.Normal;

            TileInstance tileInstance = new TileInstance(offset, tileColor, tileState);
            _tiles[offset] = tileInstance;

            TileBase tileToPlace = GetTileByColorAndState(tileColor, tileState) ?? hexagonTile;
            if (tileToPlace != null)
                tilemap.SetTile(offset, tileToPlace);

            // CancelAt이 내부에 있으므로 진행 중인 제거 애니메이션도 자동 취소됨
            tileAnimation?.PlaySpawnOnTilemap(tilemap, offset);

            if (debugLog)
                Debug.Log($"[Tile 스폰] Offset: {offset}, Color: {tileColor}, State: {tileState}");

            return tileInstance;
        }

        /// <summary>
        /// 타일 상태 변경 및 타일맵 업데이트
        /// </summary>
        public void SetTileState(Vector3Int offset, TileState newState)
        {
            if (_tiles.TryGetValue(offset, out TileInstance tileInstance))
            {
                tileInstance.State = newState;
                
                // 타일맵에 새로운 타일 배치
                TileBase tileToPlace = GetTileByColorAndState(tileInstance.Color, newState);
                if (tileToPlace != null)
                {
                    tilemap.SetTile(offset, tileToPlace);
                }

                if (debugLog)
                {
                    Debug.Log($"[Tile 상태 변경] Offset: {offset}, Color: {tileInstance.Color}, State: {newState}");
                }
            }
        }

        /// <summary>
        /// 타일 제거. 데이터는 즉시 제거하여 입력에서 배제하고, tilemap 클리어는 애니메이션 완료 후 처리.
        /// </summary>
        public void RemoveTile(Vector3Int offset)
        {
            if (!_tiles.ContainsKey(offset)) return;

            _tiles.Remove(offset);

            if (tileAnimation != null)
            {
                // 애니메이션 완료 후 tilemap 클리어 (onComplete)
                tileAnimation.PlayRemoveOnTilemap(tilemap, offset,
                    onComplete: () => tilemap.SetTile(offset, null));
            }
            else
            {
                tilemap.SetTile(offset, null);
            }

            if (debugLog)
                Debug.Log($"[Tile 제거] Offset: {offset}");
        }

        /// <summary>
        /// AnimatedTile 로드. 직렬화된 _tileEntries 배열로부터 딕셔너리 재구성.
        /// 에디터에서는 배열이 비어있을 때만 AssetDatabase로 로드 후 배열에 저장한다.
        /// </summary>
        private void LoadAnimatedTiles()
        {
            _tilesByColorAndState.Clear();

            // 직렬화된 배열에서 딕셔너리 재구성 (에디터/런타임 공통 경로)
            if (_tileEntries != null && _tileEntries.Length > 0)
            {
                foreach (var entry in _tileEntries)
                {
                    if (entry.tile == null) continue;
                    if (!_tilesByColorAndState.TryGetValue(entry.color, out var stateDict))
                    {
                        stateDict = new Dictionary<TileState, TileBase>();
                        _tilesByColorAndState[entry.color] = stateDict;
                    }
                    stateDict[entry.state] = entry.tile;
                }
                if (debugLog) Debug.Log($"AnimatedTile 로드 완료: {_tileEntries.Length}개");
                return;
            }

#if UNITY_EDITOR
            // _tileEntries가 비어있을 때 AssetDatabase로 로드하고 배열에 저장
            TileColor[] colors = { TileColor.Red, TileColor.Blue, TileColor.Yellow, TileColor.Purple, TileColor.Orange, TileColor.Cyan };
            TileState[] states = { TileState.Normal, TileState.ItemLv1, TileState.ItemLv2 };
            var entries = new System.Collections.Generic.List<TileEntry>();

            foreach (TileColor color in colors)
            {
                var stateDict = new Dictionary<TileState, TileBase>();
                foreach (TileState state in states)
                {
                    string assetPath = $"{TILES_PATH}/HexagonTile_{color}_{state}.asset";
                    TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
                    if (tile != null)
                    {
                        stateDict[state] = tile;
                        entries.Add(new TileEntry { color = color, state = state, tile = tile });
                    }
                    else if (debugLog)
                    {
                        Debug.LogWarning($"타일을 찾을 수 없습니다: {assetPath}");
                    }
                }
                if (stateDict.Count > 0) _tilesByColorAndState[color] = stateDict;
            }

            _tileEntries = entries.ToArray();
            if (debugLog) Debug.Log($"AnimatedTile 로드 완료: {_tileEntries.Length}개");
#else
            Debug.LogError("[PuzzleBoardManager] _tileEntries가 비어있습니다. 에디터에서 'Reload Animated Tiles'를 실행하고 씬을 저장하세요.");
#endif
        }

        /// <summary>
        /// 타일 스프라이트의 Pixels Per Unit 가져오기 (디버그용)
        /// </summary>
        private float GetTileSpritePixelsPerUnit()
        {
            // 딕셔너리에서 샘플 타일 찾기
            TileBase sampleTile = null;
            if (_tilesByColorAndState.TryGetValue(TileColor.Red, out var redDict))
            {
                if (redDict.TryGetValue(TileState.Normal, out sampleTile))
                {
                    // AnimatedTile은 TileBase를 상속하므로 sprite 속성 접근 시도
                    if (sampleTile is Tile unityTile && unityTile.sprite != null)
                    {
                        return unityTile.sprite.pixelsPerUnit;
                    }
                }
            }

            // 기본값 반환
            return 128f;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 타일 강제 재로드. 실행 후 씬을 저장해야 런타임에서도 반영된다.
        /// </summary>
        [ContextMenu("Reload Animated Tiles")]
        private void ReloadAnimatedTiles()
        {
            _tileEntries = new TileEntry[0]; // 강제 재로드
            LoadAnimatedTiles();
            Debug.Log($"AnimatedTile 재로드 완료: {_tileEntries.Length}개. 씬을 저장하세요.");
        }
#endif

        /// <summary>
        /// 퍼즐 관련 오브젝트 전체를 puzzleBoardLayer 레이어로 설정.
        /// Tilemap, TilemapRenderer, 부모 Grid 모두 포함해서 Camera_Puzzle culling mask에 잡히게 한다.
        /// </summary>
        private void ApplyPuzzleLayer()
        {
            int layerIndex = LayerMask.NameToLayer(puzzleBoardLayer);
            if (layerIndex < 0)
            {
                Debug.LogWarning($"[PuzzleBoardManager] '{puzzleBoardLayer}' 레이어가 없습니다. " +
                                 "Project Settings > Tags and Layers 에서 추가하세요.");
                return;
            }

            // Tilemap 자신과 모든 자식
            SetLayerRecursive(gameObject, layerIndex);

            // 부모 Grid 컴포넌트가 있으면 부모도 같은 레이어로
            if (transform.parent != null && transform.parent.GetComponent<Grid>() != null)
                transform.parent.gameObject.layer = layerIndex;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        /// <summary>
        /// 그리드 중앙에 카메라 배치
        /// </summary>
        private void PositionCameraAtCenter() => FitCameraToBoard();

        private void LateUpdate()
        {
            if (_isInitialized && autoPositionCamera &&
                (_fittedCamera == null || _fittedPixelRect != _fittedCamera.pixelRect))
                FitCameraToBoard();
        }

        public void FitCameraToBoard()
        {
            var camera = FindPuzzleCamera();
            if (camera == null || !camera.orthographic || _tiles.Count == 0 || camera.pixelHeight <= 0) return;
            Bounds bounds = GetBoardWorldBounds();
            float aspect = camera.pixelWidth / (float)camera.pixelHeight;
            camera.orthographicSize = Mathf.Max(bounds.extents.y + cameraPadding,
                (bounds.extents.x + cameraPadding) / aspect);
            var position = camera.transform.position;
            position.x = bounds.center.x;
            position.y = bounds.center.y;
            camera.transform.position = position;
            _fittedCamera = camera;
            _fittedPixelRect = camera.pixelRect;
        }

        private Camera FindPuzzleCamera()
        {
            foreach (var camera in Camera.allCameras)
                if (camera.name == "Camera_Puzzle") return camera;
            return Camera.main;
        }

        private Vector2 GetCellExtents(Vector3Int offset)
        {
            var sprite = tilemap.GetSprite(offset);
            if (sprite != null) return sprite.bounds.extents;
            var size = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : Vector3.one;
            return new Vector2(size.x * 0.5f, size.y * 0.5f);
        }

        public Bounds GetBoardWorldBounds()
        {
            var bounds = new Bounds();
            bool first = true;
            foreach (var cell in _tiles.Keys)
            {
                var center = GetCellCenterWorld(cell);
                var half = GetCellExtents(cell);
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                    {
                        var corner = center + tilemap.transform.TransformVector(new Vector3(x * half.x, y * half.y));
                        if (first) { bounds = new Bounds(corner, Vector3.zero); first = false; }
                        else bounds.Encapsulate(corner);
                    }
            }
            return bounds;
        }

        /// <summary>렌더링 중심과 동일한 좌표의 육각 판정. 겹친 보정 영역은 가장 가까운 중심을 선택한다.</summary>
        public bool TryGetTileAtWorldPosition(Vector3 world, float hitAreaScale, out Vector3Int cell)
        {
            cell = default;
            float best = float.PositiveInfinity;
            Vector3 local = tilemap.transform.InverseTransformPoint(world);
            foreach (var pair in _tiles)
            {
                if (!pair.Value.IsActive) continue;
                var center = tilemap.transform.InverseTransformPoint(GetCellCenterWorld(pair.Key));
                var half = GetCellExtents(pair.Key) * Mathf.Max(0.01f, hitAreaScale);
                if (half.x <= 0f || half.y <= 0f) continue;
                float x = Mathf.Abs(local.x - center.x) / half.x;
                float y = Mathf.Abs(local.y - center.y) / half.y;
                if (x > 1f || y > 1f || y + 0.5f * x > 1f) continue;
                float distance = (local - center).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                cell = pair.Key;
            }
            return !float.IsPositiveInfinity(best);
        }

        public bool SetRequiredMatchColor(TileColor? color)
        {
            RequiredMatchColor = color;
            return EnsureRequiredColorPair();
        }

        /// <summary>생성/리필 완료 후 등록된 요구 색을 재검사. 요구 색이 없으면 변경하지 않는다.</summary>
        public bool EnsureRequiredColorPair(ISet<Vector3Int> protectedCells = null) =>
            !RequiredMatchColor.HasValue || TryEnsureAdjacentPair(RequiredMatchColor.Value, protectedCells);

        /// <summary>최소한의 일반 타일만 재색칠. 아이템/선택/보호/비활성 타일을 변경하지 않는다.</summary>
        public bool TryEnsureAdjacentPair(TileColor color, ISet<Vector3Int> protectedCells = null)
        {
            TileInstance bestA = null, bestB = null;
            int bestCost = 3;
            foreach (var a in _tiles.Values)
            {
                if (!Available(a)) continue;
                foreach (var neighbor in HexOffsetUtils.GetNeighbors(a.OffsetPosition))
                {
                    var b = GetTileAtOffset(neighbor);
                    if (!Available(b)) continue;
                    int costA = a.Color == color ? 0 : 1;
                    int costB = b.Color == color ? 0 : 1;
                    if ((costA != 0 && a.State != TileState.Normal) ||
                        (costB != 0 && b.State != TileState.Normal)) continue;
                    if (costA + costB >= bestCost) continue;
                    bestCost = costA + costB;
                    bestA = a; bestB = b;
                }
            }
            if (bestA == null) return false; // 보정 불가일 때 부분 변경하지 않는다.
            Recolor(bestA); Recolor(bestB);
            return true;

            bool Available(TileInstance tile) => tile != null && tile.IsActive && !tile.IsSelected &&
                (protectedCells == null || !protectedCells.Contains(tile.OffsetPosition));
            void Recolor(TileInstance tile)
            {
                if (tile.Color == color) return;
                tile.Color = color;
                tilemap.SetTile(tile.OffsetPosition, GetTileByColorAndState(color, tile.State) ?? hexagonTile);
            }
        }
    }
}
