using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using NyanLink.Data.Definitions;
using NyanLink.Data.Enums;

namespace NyanLink.Puzzle
{
    /// <summary>
    /// 선택된 타일들의 시각적 피드백 관리
    /// 연결선 표시. 체인 티어(Middle/Long)·선택 색상에 따라 연출 변경 가능 (LineVisualEffectConfig).
    /// </summary>
    public class TileSelectionVisualizer : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("PuzzleBoardManager 참조")]
        public PuzzleBoardManager boardManager;

        [Header("연결선 연출 (추후 변경 가능)")]
        [Tooltip("티어/색상별 연결선 연출 설정. 비어있으면 아래 기본값 사용")]
        public LineVisualEffectConfig lineEffectConfig;

        [Header("연결선 기본값 (lineEffectConfig 없을 때)")]
        [Tooltip("연결선 색상")]
        public Color lineColor = new Color(0.7f, 0.9f, 1f, 0.8f);

        [Tooltip("연결선 두께")]
        [Range(0.01f, 0.1f)]
        public float lineWidth = 0.015f;

        [Tooltip("흐르는 효과 속도")]
        [Range(0.5f, 5f)]
        public float flowSpeed = 2f;

        [Header("디버그")]
        [Tooltip("시각화 디버그 로그 출력")]
        public bool debugLog = false;

        /// <summary> 현재 선택된 체인 (미리보기용) </summary>
        private List<Vector3Int> _currentChain = new List<Vector3Int>();

        /// <summary> 연결선 LineRenderer </summary>
        private LineRenderer _connectionLine;

        /// <summary> 연결선 GameObject </summary>
        private GameObject _lineObject;

        /// <summary> 현재 미리보기 중인 타일 </summary>
        private Vector3Int? _previewTile = null;

        /// <summary> 흐르는 효과용 시간 </summary>
        private float _flowTime = 0f;

        /// <summary> 현재 적용 중인 체인 티어 (0=Short, 1=Middle, 2=Long) </summary>
        private int _currentChainTier;

        /// <summary> 현재 적용 중인 체인 색상 </summary>
        private TileColor _currentChainColor;


        private void Awake()
        {
            if (boardManager == null)
            {
                boardManager = FindFirstObjectByType<PuzzleBoardManager>();
            }

            // 연결선 GameObject 생성
            CreateConnectionLine();
        }

        private void Update()
        {
            // 흐르는 효과 업데이트
            UpdateFlowAnimation();
        }

        /// <summary>
        /// 연결선 업데이트 (타일 선택 중 사용).
        /// chainTier: 0=Short, 1=Middle, 2=Long (Middle/Long일 때 티어별·색상별 연출 적용)
        /// </summary>
        public void UpdateConnectionLine(List<Vector3Int> selectedChain, Vector3Int? previewTile = null, int chainTier = 0, TileColor? chainColor = null)
        {
            if (selectedChain == null || selectedChain.Count < 2)
            {
                ClearConnectionLine();
                _previewTile = null;
                return;
            }

            _currentChain = new List<Vector3Int>(selectedChain);
            _previewTile = previewTile;
            _currentChainTier = chainTier;
            _currentChainColor = chainColor ?? (boardManager != null && boardManager.GetTileAtOffset(selectedChain[0]) != null
                ? boardManager.GetTileAtOffset(selectedChain[0]).Color
                : TileColor.Red);

            CreateConnectionLine(selectedChain, previewTile);
        }

        /// <summary>
        /// 선택된 타일 체인 시각화 업데이트 (레거시, 호환성 유지)
        /// </summary>
        public void UpdateSelectionVisualization(List<Vector3Int> selectedChain)
        {
            UpdateConnectionLine(selectedChain, null);
        }


        /// <summary>
        /// 연결선 생성 (직선으로 타일 중점들을 연결, 미리보기 타일 포함 가능)
        /// </summary>
        private void CreateConnectionLine(List<Vector3Int> selectedChain, Vector3Int? previewTile = null)
        {
            if (selectedChain == null || selectedChain.Count < 2)
            {
                ClearConnectionLine();
                return;
            }

            if (_connectionLine == null)
            {
                return;
            }

            // 타일 중점 위치 계산 (셀 중심 사용)
            // Z 위치를 타일맵보다 앞에 배치하여 앞에 표시되도록 함
            float lineZOffset = -0.1f; // 타일맵보다 앞에 배치 (카메라에 가까운 위치)
            
            List<Vector3> positions = new List<Vector3>();
            foreach (var tileCoord in selectedChain)
            {
                // Tilemap Anchor를 고려하여 셀의 중심 위치 사용
                Vector3 worldPos = boardManager.tilemap.GetCellCenterWorld(tileCoord);
                // Z 위치를 타일맵보다 앞에 배치
                worldPos.z = lineZOffset;
                positions.Add(worldPos);
            }

            // 미리보기 타일이 있으면 추가
            if (previewTile.HasValue && previewTile.Value != selectedChain[selectedChain.Count - 1])
            {
                Vector3 previewPos = boardManager.tilemap.GetCellCenterWorld(previewTile.Value);
                previewPos.z = lineZOffset;
                positions.Add(previewPos);
            }

            // 직선으로 연결
            _connectionLine.positionCount = positions.Count;
            _connectionLine.SetPositions(positions.ToArray());
            _connectionLine.useWorldSpace = true;

            // 티어·색상별 연출 적용 (config 있으면 사용, 없으면 기본값)
            float width = lineWidth;
            Color color = lineColor;
            float effectIntensity = 0f;
            Material tierMaterial = null;
            if (lineEffectConfig != null)
            {
                width = lineEffectConfig.GetLineWidth(_currentChainTier);
                color = lineEffectConfig.GetLineColor(_currentChainTier, _currentChainColor);
                effectIntensity = lineEffectConfig.GetEffectIntensity(_currentChainTier, _currentChainColor);
                tierMaterial = lineEffectConfig.GetMaterial(_currentChainTier);
            }
            _connectionLine.startWidth = width;
            _connectionLine.endWidth = width;
            _connectionLine.startColor = color;
            _connectionLine.endColor = color;
            if (tierMaterial != null)
                _connectionLine.material = tierMaterial;
            ApplyEffectIntensityToMaterial(effectIntensity);
            
            // 2D 렌더링 모드 설정
            // View 모드: 카메라를 향하도록 설정 (2D 게임에 적합)
            _connectionLine.alignment = LineAlignment.View;
            
            // 2D 렌더링을 위한 추가 설정
            _connectionLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _connectionLine.receiveShadows = false;
            
            // Sorting Layer와 Order를 Tilemap과 동일하게 설정하여 비교 가능하게 함
            if (boardManager != null && boardManager.tilemap != null)
            {
                TilemapRenderer tilemapRenderer = boardManager.tilemap.GetComponent<TilemapRenderer>();
                if (tilemapRenderer != null)
                {
                    // 동일한 Sorting Layer 사용 (중요: 같은 Layer에서만 sortingOrder 비교 가능)
                    _connectionLine.sortingLayerName = tilemapRenderer.sortingLayerName;
                    
                    // TilemapRenderer의 실제 sortingOrder (int) 확인
                    // TilemapRenderer는 Renderer를 상속하므로 sortingOrder 속성 사용 가능
                    int tilemapSortOrder = tilemapRenderer.sortingOrder;
                    _connectionLine.sortingOrder = tilemapSortOrder + 10; // 타일 위에 표시
                }
                else
                {
                    // TilemapRenderer가 없으면 기본값 사용
                    _connectionLine.sortingOrder = 10;
                }
            }
            else
            {
                _connectionLine.sortingOrder = 10;
            }
            
            _connectionLine.enabled = true;
        }

        /// <summary>
        /// 머티리얼에 이펙트 강도 전달 (셰이더에 _EffectIntensity 등이 있으면 추후 연출 변경 가능)
        /// </summary>
        private void ApplyEffectIntensityToMaterial(float intensity)
        {
            if (_connectionLine == null || _connectionLine.material == null) return;
            if (_connectionLine.material.HasProperty("_EffectIntensity"))
                _connectionLine.material.SetFloat("_EffectIntensity", intensity);
        }

        /// <summary>
        /// 연결선 제거 (public - 외부에서 호출 가능)
        /// </summary>
        public void ClearConnectionLine()
        {
            if (_connectionLine != null)
            {
                _connectionLine.enabled = false;
            }
            _previewTile = null;
        }



        /// <summary>
        /// 연결선 GameObject 생성
        /// </summary>
        private void CreateConnectionLine()
        {
            _lineObject = new GameObject("ConnectionLine");
            _lineObject.transform.SetParent(transform);
            
            // Z 위치를 타일맵보다 앞에 배치 (카메라에 가까운 위치)
            // 타일맵이 Z=0에 있다면, LineRenderer를 Z=-0.1로 설정하여 앞에 표시
            if (boardManager != null && boardManager.tilemap != null)
            {
                Vector3 tilemapPos = boardManager.tilemap.transform.position;
                _lineObject.transform.position = new Vector3(tilemapPos.x, tilemapPos.y, tilemapPos.z - 0.1f);
            }
            else
            {
                _lineObject.transform.position = new Vector3(0, 0, -0.1f);
            }
            
            _connectionLine = _lineObject.AddComponent<LineRenderer>();
            _connectionLine.enabled = false;
            
            // 2D 렌더링을 위한 추가 설정
            _connectionLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _connectionLine.receiveShadows = false;
            _connectionLine.useWorldSpace = true;
            
            // 2D용 Material 설정 (기본 Material은 3D용이므로 2D용으로 변경)
            // Sprite-Default Shader를 사용하여 2D 렌더링 보장
            Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
            if (lineMaterial != null)
            {
                _connectionLine.material = lineMaterial;
            }
        }

        /// <summary>
        /// 연결선 업데이트 (체인 + 미리보기 타일 포함)
        /// </summary>
        public void UpdatePreviewLine(Vector3Int lastSelectedTile, Vector3Int? previewTile)
        {
            // 체인 길이가 2 미만이면 연결선 표시 안 함
            if (_currentChain.Count < 2)
            {
                ClearConnectionLine();
                _previewTile = null;
                return;
            }

            // 미리보기 타일 저장
            _previewTile = previewTile;

            // 연결선 업데이트 (체인 + 미리보기 타일)
            CreateConnectionLine(_currentChain, previewTile);
        }

        /// <summary>
        /// 미리보기 연결선 제거
        /// </summary>
        public void ClearPreviewLine()
        {
            _previewTile = null;
            // 체인이 있으면 미리보기 없이 연결선만 다시 그리기
            if (_currentChain.Count >= 2)
            {
                CreateConnectionLine(_currentChain, null);
            }
        }


        /// <summary>
        /// 흐르는 효과 업데이트 (UV 오프셋 + 티어/색상 기반 이펙트 강도 유지)
        /// </summary>
        private void UpdateFlowAnimation()
        {
            if (_connectionLine == null || !_connectionLine.enabled) return;

            _flowTime += Time.deltaTime * flowSpeed;
            if (_connectionLine.material != null)
            {
                if (_connectionLine.material.HasProperty("_FlowOffset"))
                    _connectionLine.material.SetFloat("_FlowOffset", _flowTime);
                if (lineEffectConfig != null && _connectionLine.material.HasProperty("_EffectIntensity"))
                {
                    float intensity = lineEffectConfig.GetEffectIntensity(_currentChainTier, _currentChainColor);
                    _connectionLine.material.SetFloat("_EffectIntensity", intensity);
                }
            }
        }

        /// <summary>
        /// 시각화 제거
        /// </summary>
        public void ClearVisualization()
        {
            // 연결선 비활성화 및 정보 초기화
            ClearConnectionLine();
            
            // 체인 정보 초기화
            _currentChain.Clear();
            
            // 미리보기 타일 초기화
            _previewTile = null;

            if (debugLog)
            {
                Debug.Log("TileSelectionVisualizer: 시각화 제거됨");
            }
        }

        private void OnDestroy()
        {
            ClearVisualization();
        }
    }
}
