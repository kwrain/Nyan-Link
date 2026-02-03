using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using NyanLink.Data.Enums;

namespace NyanLink.Puzzle
{
    /// <summary>
    /// 타일 입력 처리 (터치/드래그)
    /// Unity Tilemap Offset 좌표계 사용
    /// </summary>
    public class TileInputHandler : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("PuzzleBoardManager 참조")]
        public PuzzleBoardManager boardManager;

        [Tooltip("TileMatcher 참조")]
        public TileMatcher tileMatcher;

        [Tooltip("TileSelectionVisualizer 참조")]
        public TileSelectionVisualizer selectionVisualizer;

        [Header("입력 설정")]
        [Tooltip("마우스 이동 민감도 (이 값 이상 이동해야 Moved로 판별)")]
        [Range(0.01f, 1f)]
        public float mouseMoveSensitivity = 0.1f;

        [Header("디버그")]
        [Tooltip("입력 디버그 로그 출력")]
        public bool debugLog = false;

        /// <summary>
        /// 현재 선택된 타일 체인
        /// </summary>
        private List<Vector3Int> _selectedChain = new List<Vector3Int>();

        /// <summary>
        /// 마지막 입력 위치 (중복 호출 방지용)
        /// </summary>
        private Vector2 _lastInputPosition;

        /// <summary>
        /// 입력이 시작되었는지 여부
        /// </summary>
        private bool _isInputActive = false;

        private void Awake()
        {
            if (boardManager == null)
            {
                boardManager = FindFirstObjectByType<PuzzleBoardManager>();
            }

            if (tileMatcher == null)
            {
                tileMatcher = FindFirstObjectByType<TileMatcher>();
            }

            if (selectionVisualizer == null)
            {
                selectionVisualizer = FindFirstObjectByType<TileSelectionVisualizer>();
            }
        }

        private void Update()
        {
            // 마우스 입력 처리
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 screenPos = Input.mousePosition;
                OnInputBegan(screenPos);
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 screenPos = Input.mousePosition;
                if (_isInputActive && Vector2.Distance(screenPos, _lastInputPosition) >= mouseMoveSensitivity)
                {
                    OnInputMoved(screenPos);
                    _lastInputPosition = screenPos;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                OnInputReleased();
            }

            // 터치 입력 처리
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 screenPos = touch.position;

                if (touch.phase == TouchPhase.Began)
                {
                    OnInputBegan(screenPos);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (_isInputActive && Vector2.Distance(screenPos, _lastInputPosition) >= mouseMoveSensitivity)
                    {
                        OnInputMoved(screenPos);
                        _lastInputPosition = screenPos;
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    OnInputReleased();
                }
            }
        }

        /// <summary>
        /// 입력 시작 처리
        /// </summary>
        private void OnInputBegan(Vector2 screenPosition)
        {
            // UI 위 터치는 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector3Int? tile = GetTileAtScreenPosition(screenPosition);
            if (tile.HasValue)
            {
                TileInstance tileInstance = boardManager.GetTileAtOffset(tile.Value);
                if (tileInstance != null && tileInstance.IsActive)
                {
                    _selectedChain.Clear();
                    _selectedChain.Add(tile.Value);
                    tileInstance.IsSelected = true;
                    _isInputActive = true;
                    _lastInputPosition = screenPosition;

                    if (debugLog)
                    {
                        Debug.Log($"[입력] 터치 시작 - ScreenPos: {screenPosition}, 타일: Offset{tile.Value}, 색상: {tileInstance.Color}");
                    }

                    UpdateSelectionVisualization();
                }
            }
        }

        /// <summary>
        /// 입력 이동 처리
        /// </summary>
        private void OnInputMoved(Vector2 screenPosition)
        {
            if (!_isInputActive || _selectedChain.Count == 0)
            {
                return;
            }

            Vector3Int? touchedTile = GetTileAtScreenPosition(screenPosition);
            if (!touchedTile.HasValue)
            {
                return;
            }

            // 마지막으로 체인에 추가된 타일 기준으로 인접 타일 검증
            Vector3Int lastSelectedTile = _selectedChain[_selectedChain.Count - 1];

            // 이미 선택된 타일인지 확인 (Back-track 처리)
            int existingIndex = _selectedChain.IndexOf(touchedTile.Value);
            if (existingIndex >= 0)
            {
                // 이전에 선택한 타일로 되돌아가기
                if (existingIndex < _selectedChain.Count - 1)
                {
                    // 선택 해제
                    for (int i = _selectedChain.Count - 1; i > existingIndex; i--)
                    {
                        Vector3Int tileToDeselect = _selectedChain[i];
                        TileInstance tileInstance = boardManager.GetTileAtOffset(tileToDeselect);
                        if (tileInstance != null)
                        {
                            tileInstance.IsSelected = false;
                        }
                        _selectedChain.RemoveAt(i);
                    }

                    if (debugLog)
                    {
                        Debug.Log($"[입력] Back-track - Offset{touchedTile.Value}로 되돌아감");
                    }

                    UpdateSelectionVisualization();
                }
                return;
            }

            // 인접 타일인지 확인
            if (!HexOffsetUtils.IsAdjacent(lastSelectedTile, touchedTile.Value))
            {
                return;
            }

            // 타일 인스턴스 확인
            TileInstance touchedTileInstance = boardManager.GetTileAtOffset(touchedTile.Value);
            if (touchedTileInstance == null || !touchedTileInstance.IsActive)
            {
                return;
            }

            // 같은 색상인지 확인
            TileInstance lastTileInstance = boardManager.GetTileAtOffset(lastSelectedTile);
            if (lastTileInstance == null || touchedTileInstance.Color != lastTileInstance.Color)
            {
                // 다른 색상 선택 시 체인 취소
                ClearSelection();
                return;
            }

            // 체인에 추가
            _selectedChain.Add(touchedTile.Value);
            touchedTileInstance.IsSelected = true;

            if (debugLog)
            {
                Debug.Log($"[입력] 타일 추가 - Offset{touchedTile.Value}, 색상: {touchedTileInstance.Color}, 체인 길이: {_selectedChain.Count}");
            }

            UpdateSelectionVisualization();
        }

        /// <summary>
        /// 입력 종료 처리
        /// </summary>
        private void OnInputReleased()
        {
            if (!_isInputActive)
            {
                return;
            }

            _isInputActive = false;

            // 체인 길이가 2 이상이면 매칭 처리
            if (_selectedChain.Count >= 2 && tileMatcher != null)
            {
                tileMatcher.ProcessMatch(_selectedChain);
            }

            // 선택 해제
            ClearSelection();
        }

        /// <summary>
        /// 스크린 좌표로 타일 조회 (정확한 위치의 타일만 반환)
        /// </summary>
        private Vector3Int? GetTileAtScreenPosition(Vector2 screenPosition)
        {
            if (boardManager == null || boardManager.tilemap == null)
            {
                return null;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }

            if (mainCamera == null)
            {
                return null;
            }

            // 스크린 좌표를 월드 좌표로 변환
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
            worldPos.z = 0f;

            // 월드 좌표를 셀 좌표로 변환
            Vector3Int cellPos = boardManager.tilemap.WorldToCell(worldPos);

            // 해당 셀에 타일이 있는지 확인
            TileInstance tileInstance = boardManager.GetTileAtOffset(cellPos);
            if (tileInstance != null && tileInstance.IsActive)
            {
                return cellPos;
            }

            return null;
        }

        /// <summary>
        /// 선택 시각화 업데이트
        /// </summary>
        private void UpdateSelectionVisualization()
        {
            if (selectionVisualizer != null)
            {
                if (_selectedChain.Count >= 2)
                {
                    // 마우스 포인터 위치의 타일을 미리보기로 표시
                    Vector3Int? previewTile = GetTileAtScreenPosition(Input.mousePosition);
                    selectionVisualizer.UpdateConnectionLine(_selectedChain, previewTile);
                }
                else
                {
                    selectionVisualizer.ClearConnectionLine();
                }
            }
        }

        /// <summary>
        /// 선택 해제
        /// </summary>
        private void ClearSelection()
        {
            foreach (var tileOffset in _selectedChain)
            {
                TileInstance tileInstance = boardManager.GetTileAtOffset(tileOffset);
                if (tileInstance != null)
                {
                    tileInstance.IsSelected = false;
                }
            }

            _selectedChain.Clear();

            if (selectionVisualizer != null)
            {
                selectionVisualizer.ClearVisualization();
            }
        }
    }
}
