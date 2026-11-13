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
                Debug.LogWarning("PuzzleBoardManager: 이미 초기화되었습니다.");
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

            int centerOffsetX = -(gridShapeData.width / 2) - 1;
            int centerOffsetY = -(gridShapeData.height / 2);

            for (int y = 0; y < gridShapeData.height; y++)
            {
                int actualY = y + centerOffsetY;
                bool isEvenRow = actualY % 2 == 0;
                int tileCount = gridShapeData.width;
                if (isEvenRow)
                {
                    tileCount = gridShapeData.width + 1;
                }

                for (int x = 0; x < tileCount; x++)
                {
                    Vector3Int maskIndex = new Vector3Int(x, y, 0);
                    if (x < gridShapeData.width && !gridShapeData.IsTileActive(maskIndex))
                    {
                        continue;
                    }

                    Vector3Int offset = new Vector3Int(x + centerOffsetX, y + centerOffsetY, 0);

                    TileColor randomColor = GetRandomTileColor();
                    TileInstance tileInstance = new TileInstance(offset, randomColor, TileState.Normal);
                    _tiles[offset] = tileInstance;

                    TileBase tileToPlace = GetTileByColorAndState(randomColor, tileInstance.State);
                    if (tileToPlace != null)
                    {
                        tilemap.SetTile(offset, tileToPlace);
                    }
                    else if (hexagonTile != null)
                    {
                        tilemap.SetTile(offset, hexagonTile);
                    }

                    Vector3 centerWorld = tilemap.GetCellCenterWorld(offset);
                    Debug.Log($"[타일 생성] Cell{offset}  색={randomColor}" +
                              $"  World({centerWorld.x:F3},{centerWorld.y:F3})");
                }
            }

            Debug.Log($"[보드 초기화 완료] 총 {_tiles.Count}개 타일 생성");
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
        private void PositionCameraAtCenter()
        {
            if (tilemap == null || gridShapeData == null) return;

            Vector3 center = tilemap.GetCellCenterWorld(Vector3Int.zero);
            float cellSizeX = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize.x : 1f;

            Camera targetCam = null;
            foreach (Camera c in Camera.allCameras)
                if (c.name == "Camera_Puzzle") { targetCam = c; break; }
            if (targetCam == null) targetCam = Camera.main;
            if (targetCam == null) targetCam = FindFirstObjectByType<Camera>();

            if (targetCam != null)
            {
                Vector3 pos = targetCam.transform.position;
                pos.x = center.x - cellSizeX * 0.5f;
                pos.y = center.y;
                targetCam.transform.position = pos;

                if (debugLog)
                    Debug.Log($"[카메라 배치] center={center}, cellSizeX={cellSizeX}, camX={pos.x}");
            }
            else if (debugLog)
            {
                Debug.LogWarning("[카메라 배치] 카메라를 찾을 수 없습니다.");
            }
        }
    }
}
