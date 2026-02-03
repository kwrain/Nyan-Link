using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using NyanLink.Data.Definitions;
using NyanLink.Data.Enums;

namespace NyanLink.Puzzle
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

        [Tooltip("헥사곤 타일 에셋 (TileBase) - 기본 타일 (호환성용)")]
        public TileBase hexagonTile;

        [Header("색상별 타일 에셋")]
        [Tooltip("빨강 타일")]
        public TileBase redTile;
        
        [Tooltip("파랑 타일")]
        public TileBase blueTile;
        
        [Tooltip("노랑 타일")]
        public TileBase yellowTile;
        
        [Tooltip("보라 타일")]
        public TileBase purpleTile;
        
        [Tooltip("주황 타일")]
        public TileBase orangeTile;
        
        [Tooltip("청록 타일")]
        public TileBase cyanTile;

        [Tooltip("그리드 쉐이프 데이터")]
        public GridShapeData gridShapeData;

        [Header("카메라 설정")]
        [Tooltip("그리드 중앙에 카메라 자동 배치")]
        public bool autoPositionCamera = true;

        [Header("디버그")]
        [Tooltip("그리드 생성 시 로그 출력")]
        public bool debugLog = true;

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
                {
                    Debug.LogError("PuzzleBoardManager: Tilemap 컴포넌트를 찾을 수 없습니다!");
                }
            }
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

            // 중앙 타일 (0, 0, 0) 기준으로 오프셋 계산
            int centerOffsetX = -(gridShapeData.width / 2) - 1;
            int centerOffsetY = -(gridShapeData.height / 2);

            // 그리드 생성
            for (int y = 0; y < gridShapeData.height; y++)
            {
                // 짝수 열(y + centerOffsetY가 짝수)에서는 타일을 하나 더 추가
                int actualY = y + centerOffsetY;
                bool isEvenRow = actualY % 2 == 0;
                int tileCount = gridShapeData.width;
                if (isEvenRow)
                {
                    tileCount = gridShapeData.width + 1;
                }
                
                for (int x = 0; x < tileCount; x++)
                {
                    // 벌집형 그리드는 항상 완전한 모양이므로 마스크 확인 불필요
                    // 범위 체크만 수행 (짝수 열 추가 타일은 범위를 벗어나므로 항상 활성화)
                    Vector3Int maskIndex = new Vector3Int(x, y, 0);
                    
                    // 마스크 범위 내에 있으면 범위 체크만 수행
                    // 짝수 열의 추가 타일(x >= width)은 범위를 벗어나므로 항상 활성화
                    if (x < gridShapeData.width && !gridShapeData.IsTileActive(maskIndex))
                    {
                        continue; // 범위 밖이면 건너뛰기
                    }

                    // Unity Tilemap Offset 좌표: 중앙 기준으로 조정
                    // (0, 0, 0)이 그리드 중앙에 위치하도록 함
                    Vector3Int offset = new Vector3Int(x + centerOffsetX, y + centerOffsetY, 0);

                    // 랜덤 색상 생성
                    TileColor randomColor = GetRandomTileColor();

                    // 타일 인스턴스 생성
                    TileInstance tileInstance = new TileInstance(offset, randomColor);

                    // 딕셔너리에 추가
                    _tiles[offset] = tileInstance;

                    // Unity Tilemap에 타일 배치
                    TileBase tileToPlace = GetTileByColor(randomColor);
                    if (tileToPlace != null)
                    {
                        tilemap.SetTile(offset, tileToPlace);
                    }
                    else if (hexagonTile != null)
                    {
                        // 색상별 타일이 없으면 기본 타일 사용
                        tilemap.SetTile(offset, hexagonTile);
                    }

                    // 타일 생성 로그 출력
                    if (debugLog)
                    {
                        Vector3 worldPos = tilemap.CellToWorld(offset);
                        string extraInfo = (x >= gridShapeData.width) ? " [짝수열 추가타일]" : "";
                        
                        // 중앙 타일 (0, 0, 0)의 경우 상세 정보 출력
                        if (offset.x == 0 && offset.y == 0 && offset.z == 0)
                        {
                            Debug.Log($"[중앙 타일] Offset: {offset}, WorldPos: {worldPos}, " +
                                     $"Grid Cell Size: {tilemap.layoutGrid?.cellSize}, " +
                                     $"Tile Sprite Pixels Per Unit: {GetTileSpritePixelsPerUnit()}");
                        }
                        else
                        {
                            Debug.Log($"[Tile 생성] MaskIndex: ({x}, {y}), Offset: {offset}, Color: {randomColor}, WorldPos: {worldPos}{extraInfo}");
                        }
                    }
                }
            }
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
        /// 색상에 따른 타일 에셋 반환
        /// </summary>
        public TileBase GetTileByColor(TileColor color)
        {
            return color switch
            {
                TileColor.Red => redTile,
                TileColor.Blue => blueTile,
                TileColor.Yellow => yellowTile,
                TileColor.Purple => purpleTile,
                TileColor.Orange => orangeTile,
                TileColor.Cyan => cyanTile,
                _ => hexagonTile
            };
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
        /// 새 타일 스폰
        /// </summary>
        public TileInstance SpawnTile(Vector3Int offset, TileColor? color = null)
        {
            TileColor tileColor = color ?? GetRandomTileColor();
            TileInstance tileInstance = new TileInstance(offset, tileColor);
            _tiles[offset] = tileInstance;

            TileBase tileToPlace = GetTileByColor(tileColor);
            if (tileToPlace != null)
            {
                tilemap.SetTile(offset, tileToPlace);
            }
            else if (hexagonTile != null)
            {
                tilemap.SetTile(offset, hexagonTile);
            }

            if (debugLog)
            {
                Vector3 worldPos = tilemap.CellToWorld(offset);
                Debug.Log($"[Tile 스폰] Offset: {offset}, Color: {tileColor}, WorldPos: {worldPos}");
            }

            return tileInstance;
        }

        /// <summary>
        /// 타일 제거
        /// </summary>
        public void RemoveTile(Vector3Int offset)
        {
            if (_tiles.ContainsKey(offset))
            {
                _tiles.Remove(offset);
                tilemap.SetTile(offset, null);

                if (debugLog)
                {
                    Debug.Log($"[Tile 제거] Offset: {offset}");
                }
            }
        }

        /// <summary>
        /// 타일 스프라이트의 Pixels Per Unit 가져오기 (디버그용)
        /// </summary>
        private float GetTileSpritePixelsPerUnit()
        {
            TileBase sampleTile = redTile ?? blueTile ?? yellowTile ?? hexagonTile;
            if (sampleTile is Tile unityTile && unityTile.sprite != null)
            {
                return unityTile.sprite.pixelsPerUnit;
            }
            return 128f; // 기본값
        }

        /// <summary>
        /// 그리드 중앙에 카메라 배치
        /// </summary>
        private void PositionCameraAtCenter()
        {
            if (tilemap == null || gridShapeData == null)
            {
                return;
            }

            // 중앙 타일의 Offset 좌표는 (0, 0, 0)
            Vector3Int centerOffset = Vector3Int.zero;

            // 중앙 타일의 월드 위치 계산 (셀 중심 사용)
            Vector3 centerWorldPos = tilemap.GetCellCenterWorld(centerOffset);

            // 카메라 찾기
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }

            if (mainCamera != null)
            {
                // 카메라를 그리드 중앙에 배치
                // Orthographic 카메라는 Z 위치만 조정
                Vector3 cameraPos = mainCamera.transform.position;
                cameraPos.x = centerWorldPos.x;
                cameraPos.y = centerWorldPos.y;
                mainCamera.transform.position = cameraPos;

                if (debugLog)
                {
                    Debug.Log($"[카메라 배치] 그리드 중앙: Offset {centerOffset}, WorldPos {centerWorldPos}, CameraPos {cameraPos}");
                }
            }
            else
            {
                if (debugLog)
                {
                    Debug.LogWarning("[카메라 배치] 카메라를 찾을 수 없습니다.");
                }
            }
        }
    }
}
