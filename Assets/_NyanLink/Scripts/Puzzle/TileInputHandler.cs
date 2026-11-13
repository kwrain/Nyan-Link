using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Tilemaps;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

namespace NineMood.Puzzle
{
    /// <summary>
    /// 타일 드래그 입력 처리 — New Input System.
    /// - 모바일: Enhanced Touch 콜백 (OnFingerDown/Move/Up)
    /// - 에디터/PC: Update()에서 Mouse.current 직접 처리 (설정 변경 불필요)
    /// 두 경로는 Mouse.current가 모바일에서 null이므로 자동으로 분리된다.
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

        [Tooltip("밸런스 데이터 (비어있으면 DataManager에서 로드)")]
        public BalanceData balanceData;

        [Header("카메라")]
        [Tooltip("퍼즐 영역 카메라. 미할당 시 'Camera_Puzzle' 이름으로 자동 검색")]
        [SerializeField] private Camera puzzleCamera;

        [Header("입력 설정")]
        [Tooltip("이 값 이상 이동해야 Moved로 판별 (픽셀)")]
        [Range(0.01f, 1f)]
        public float mouseMoveSensitivity = 0.1f;

        [Header("디버그")]
        public bool debugLog = false;

        private readonly List<Vector3Int>     _selectedChain  = new List<Vector3Int>();
        private readonly List<RaycastResult>  _uiRaycastCache = new List<RaycastResult>();
        private Vector2   _lastInputPosition;
        private Vector2   _currentScreenPos;
        private bool      _isInputActive;
        private Vector3Int _lastLoggedCell = new Vector3Int(int.MinValue, int.MinValue, 0); // Move 로그 중복 방지

        // 오입력 후 입력 잠금
        private bool  _inputLocked;
        private float _lockTimer;

        // ── 라이프사이클 ───────────────────────────────────────

        private void Awake()
        {
            if (boardManager        == null) boardManager        = FindFirstObjectByType<PuzzleBoardManager>();
            if (tileMatcher         == null) tileMatcher         = FindFirstObjectByType<TileMatcher>();
            if (selectionVisualizer == null) selectionVisualizer = FindFirstObjectByType<TileSelectionVisualizer>();
        }

        private void Start()
        {
            // PuzzleBoardInitializer가 0.1초 후 초기화하므로 0.35초 후 확인
            StartCoroutine(DiagnoseAfterInit());
        }

        private IEnumerator DiagnoseAfterInit()
        {
            yield return new WaitForSeconds(2f);
            if (boardManager == null)
            {
                Debug.LogError("[TileInput] PuzzleBoardManager를 찾을 수 없음.");
                yield break;
            }

            int tileCount = boardManager.GetTileCount();
            if (tileCount == 0)
            {
                Debug.LogWarning("[TileInput] 보드 타일 수: 0 — DefaultGridShape.asset이 없거나 Initialize 실패. " +
                                 "NyanLink > Setup > Create Phase 2 Test Assets 를 실행하세요.");
                yield break;
            }

            Camera cam = GetPuzzleCamera();
            Debug.Log($"[타일 스크린 매핑] 카메라={cam?.name ?? "없음"}  " +
                      $"orthoSize={cam?.orthographicSize}  pos={cam?.transform.position}  " +
                      $"viewport={cam?.rect}");

            // 각 타일의 월드 중심 → 스크린 좌표 역변환해서 출력
            // 터치 비긴 로그의 Screen(x,y) 값과 비교해서 어느 타일이 실제로 어디 있는지 확인
            foreach (var offset in boardManager.GetAllTileOffsets())
            {
                TileInstance tile = boardManager.GetTileAtOffset(offset);
                Vector3 worldCenter = boardManager.tilemap.GetCellCenterWorld(offset);

                string screenStr = "cam없음";
                if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(worldCenter);
                    screenStr = $"Screen({sp.x:F0},{sp.y:F0})";
                }

                Debug.Log($"[타일 위치] Cell{offset}  색={tile?.Color}  " +
                          $"World({worldCenter.x:F3},{worldCenter.y:F3})  →  {screenStr}");
            }
        }

        private void OnEnable()
        {
            ETouch.EnhancedTouchSupport.Enable();
            ETouch.Touch.onFingerDown += OnFingerDown;
            ETouch.Touch.onFingerMove += OnFingerMove;
            ETouch.Touch.onFingerUp   += OnFingerUp;
        }

        private void OnDisable()
        {
            ETouch.Touch.onFingerDown -= OnFingerDown;
            ETouch.Touch.onFingerMove -= OnFingerMove;
            ETouch.Touch.onFingerUp   -= OnFingerUp;
            ETouch.EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            // 오입력 잠금 카운트다운
            if (_inputLocked)
            {
                _lockTimer -= Time.deltaTime;
                if (_lockTimer <= 0f) _inputLocked = false;
                return;
            }

            // 마우스 처리 (에디터/PC 전용 — 모바일에서는 Mouse.current가 null)
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            _currentScreenPos = mousePos;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                bool overUI = IsOverUI(mousePos);
                if (debugLog)
                    Debug.Log($"[TileInput] Click: Pos={mousePos}, IsOverUI={overUI}, " +
                              $"State={( GameManager.IsCreated ? GameManager.Instance.CurrentState.ToString() : "NoGM" )}");
                if (!overUI)
                    OnInputBegan(mousePos);
                else if (debugLog)
                    Debug.Log("[TileInput] UI 위에서 클릭 — 타일 입력 무시 (GameReadyPanel이 열려있을 수 있음)");
            }
            else if (mouse.leftButton.isPressed)
            {
                if (_isInputActive && Vector2.Distance(mousePos, _lastInputPosition) >= mouseMoveSensitivity)
                {
                    OnInputMoved(mousePos);
                    _lastInputPosition = mousePos;
                }
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                OnInputReleased();
            }
        }

        // ── Enhanced Touch 콜백 ──────────────────────────────────

        private void OnFingerDown(ETouch.Finger finger)
        {
            if (finger.index != 0 || _inputLocked) return;
            _currentScreenPos = finger.screenPosition;
            if (IsOverUI(finger.screenPosition)) return;
            OnInputBegan(finger.screenPosition);
        }

        private void OnFingerMove(ETouch.Finger finger)
        {
            if (finger.index != 0 || !_isInputActive || _inputLocked) return;

            Vector2 screenPos = finger.screenPosition;
            _currentScreenPos = screenPos;
            if (Vector2.Distance(screenPos, _lastInputPosition) >= mouseMoveSensitivity)
            {
                OnInputMoved(screenPos);
                _lastInputPosition = screenPos;
            }
        }

        private void OnFingerUp(ETouch.Finger finger)
        {
            if (finger.index != 0) return;
            OnInputReleased();
        }

        // ── 입력 처리 ─────────────────────────────────────────

        private void OnInputBegan(Vector2 screenPosition)
        {
            if (GameManager.IsCreated)
            {
                var s = GameManager.Instance.CurrentState;
                if (s != GameState.RunnerPhase && s != GameState.BossPhase)
                {
                    Debug.LogWarning($"[TileInput] 입력 차단: State={s} (RunnerPhase 또는 BossPhase 필요). " +
                                     "DebugPanel의 '▶ START (RunnerPhase)' 버튼을 클릭하세요.");
                    return;
                }
            }

            // 항상 좌표 변환 전체를 출력
            GetTileAtScreenPositionVerbose(screenPosition, "비긴", out Vector3Int cellPos, out TileInstance tileInst);

            if (tileInst == null || !tileInst.IsActive) return;

            _selectedChain.Clear();
            _selectedChain.Add(cellPos);
            tileInst.IsSelected    = true;
            _isInputActive         = true;
            _lastInputPosition     = screenPosition;
            _lastLoggedCell        = cellPos;

            UpdateSelectionVisualization();
        }

        private void OnInputMoved(Vector2 screenPosition)
        {
            if (!_isInputActive || _selectedChain.Count == 0) return;

            // 셀이 바뀔 때만 좌표 로그 출력 (Verbose 내부에서 _lastLoggedCell과 비교)
            GetTileAtScreenPositionVerbose(screenPosition, "무브", out Vector3Int cellPos, out TileInstance touchedInstance);
            _lastLoggedCell = cellPos;

            if (touchedInstance == null || !touchedInstance.IsActive) return;

            Vector3Int lastSelected = _selectedChain[_selectedChain.Count - 1];

            // Back-track 처리
            int existingIndex = _selectedChain.IndexOf(cellPos);
            if (existingIndex >= 0)
            {
                if (existingIndex < _selectedChain.Count - 1)
                {
                    for (int i = _selectedChain.Count - 1; i > existingIndex; i--)
                    {
                        TileInstance t = boardManager.GetTileAtOffset(_selectedChain[i]);
                        if (t != null) t.IsSelected = false;
                        _selectedChain.RemoveAt(i);
                    }
                    if (debugLog) Debug.Log($"[입력] Back-track → Cell{cellPos}");
                    UpdateSelectionVisualization();
                }
                return;
            }

            if (!HexOffsetUtils.IsAdjacent(lastSelected, cellPos)) return;

            TileInstance lastInstance = boardManager.GetTileAtOffset(lastSelected);
            if (lastInstance == null || touchedInstance.Color != lastInstance.Color)
            {
                ClearSelection();
                StartMistakeLock();
                return;
            }

            BalanceData balance = balanceData != null ? balanceData : DataManager.Instance?.BalanceData;
            int prevTier = balance != null ? ChainEvaluator.GetEffectLevel(balance, _selectedChain.Count) : 0;

            _selectedChain.Add(cellPos);
            touchedInstance.IsSelected = true;

            int newTier = balance != null ? ChainEvaluator.GetEffectLevel(balance, _selectedChain.Count) : 0;
            if (selectionVisualizer != null && _selectedChain.Count >= 2)
            {
                TileInstance first = boardManager.GetTileAtOffset(_selectedChain[0]);
                TileColor chainColor = first != null ? first.Color : TileColor.Red;
                Vector3Int? previewTile = GetTileAtScreenPosition(_currentScreenPos);
                selectionVisualizer.UpdateConnectionLine(_selectedChain, previewTile, newTier, chainColor);
            }

            Debug.Log($"[터치 무브] 타일 추가 Cell{cellPos} 색={touchedInstance.Color} 체인={_selectedChain.Count} Tier={newTier}");
        }

        private void OnInputReleased()
        {
            if (!_isInputActive) return;
            _isInputActive = false;

            Debug.Log($"[터치 엔드] 체인 {_selectedChain.Count}개 — " +
                      (_selectedChain.Count >= 2 ? "매칭 처리" : "선택 해제 (2개 미만)"));

            if (_selectedChain.Count >= 2 && tileMatcher != null)
                tileMatcher.ProcessMatch(_selectedChain);

            ClearSelection();
        }

        // ── 보조 메서드 ──────────────────────────────────────

        /// <summary>
        /// 터치 비긴/무브 전용 — 좌표 변환 전 과정을 Console에 출력한다.
        /// "무브"는 셀이 바뀌었을 때만 출력 (중복 방지), "비긴"은 항상 출력.
        /// </summary>
        private void GetTileAtScreenPositionVerbose(Vector2 screenPos, string label,
            out Vector3Int cellPos, out TileInstance tile)
        {
            cellPos = Vector3Int.zero;
            tile    = null;

            Camera cam = GetPuzzleCamera();
            if (boardManager == null || boardManager.tilemap == null || cam == null)
            {
                Debug.LogWarning($"[터치 {label}] 전제 조건 없음 — " +
                                 $"boardManager={boardManager != null} tilemap={boardManager?.tilemap != null} cam={cam != null}");
                return;
            }

            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
            worldPos.z = 0f;

            // tileAnchor 보정: 타일은 CellToWorld(offset) + tileAnchor 위치에 렌더링된다.
            // WorldToCell은 이를 모르므로, 클릭 좌표에서 앵커 오프셋을 빼고 WorldToCell 호출.
            // orientationMatrix.GetColumn(3)이 아닌 tm.tileAnchor를 직접 사용해야 Inspector 값과 일치한다.
            // TransformVector로 로컬 → 월드 방향 변환 (회전/스케일 반영, 평행이동 제외).
            Tilemap tm              = boardManager.tilemap;
            Vector3 anchorLocal     = tm.tileAnchor;
            Vector3 anchorWorld     = tm.transform.TransformVector(anchorLocal);
            cellPos = tm.WorldToCell(worldPos - anchorWorld);
            tile    = boardManager.GetTileAtOffset(cellPos);

            // 무브는 셀이 바뀔 때만, 비긴은 항상 로그
            bool isMove      = label == "무브";
            bool cellChanged = cellPos != _lastLoggedCell;
            if (!isMove || cellChanged)
            {
                string tileInfo = tile != null
                    ? $"색={tile.Color} Active={tile.IsActive}"
                    : $"타일없음 (보드 등록 타일 수={boardManager.GetTileCount()})";

                Debug.Log($"[터치 {label}] " +
                          $"Screen({screenPos.x:F0},{screenPos.y:F0})" +
                          $" → World({worldPos.x:F3},{worldPos.y:F3})" +
                          $" → anchor보정({anchorLocal.x:F2},{anchorLocal.y:F2}) Cell{cellPos}" +
                          $"  ▶ {tileInfo}");
            }
        }

        /// <summary>내부 연산용 (시각화 미리보기 등) — 로그 없음. tileAnchor 보정 포함.</summary>
        private Vector3Int? GetTileAtScreenPosition(Vector2 screenPosition)
        {
            if (boardManager == null || boardManager.tilemap == null) return null;

            Camera cam = GetPuzzleCamera();
            if (cam == null) return null;

            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, cam.nearClipPlane));
            worldPos.z = 0f;

            Tilemap tm          = boardManager.tilemap;
            Vector3 anchorWorld = tm.transform.TransformVector(tm.tileAnchor);
            Vector3Int cellPos  = tm.WorldToCell(worldPos - anchorWorld);

            TileInstance tile = boardManager.GetTileAtOffset(cellPos);
            return (tile != null && tile.IsActive) ? cellPos : (Vector3Int?)null;
        }

        private Camera GetPuzzleCamera()
        {
            if (puzzleCamera != null) return puzzleCamera;
            foreach (Camera cam in Camera.allCameras)
                if (cam.name == "Camera_Puzzle") return cam;
            return Camera.main;
        }

        private void UpdateSelectionVisualization()
        {
            if (selectionVisualizer == null) return;
            if (_selectedChain.Count >= 2)
            {
                BalanceData balance = balanceData != null ? balanceData : DataManager.Instance?.BalanceData;
                int tier = ChainEvaluator.GetEffectLevel(balance, _selectedChain.Count);
                TileInstance first = boardManager.GetTileAtOffset(_selectedChain[0]);
                TileColor chainColor = first != null ? first.Color : TileColor.Red;
                Vector3Int? previewTile = GetTileAtScreenPosition(_currentScreenPos);
                selectionVisualizer.UpdateConnectionLine(_selectedChain, previewTile, tier, chainColor);
            }
            else
            {
                selectionVisualizer.ClearConnectionLine();
            }
        }

        private void StartMistakeLock()
        {
            BalanceData b = balanceData ?? DataManager.Instance?.BalanceData;
            _lockTimer   = b?.mistakeLockDuration ?? 0.5f;
            _inputLocked = true;
        }

        /// <summary>외부에서 입력 잠금 (러너 Stumble 등에서 호출)</summary>
        public void LockInput(float duration)
        {
            _lockTimer   = Mathf.Max(_lockTimer, duration);
            _inputLocked = true;
        }

        private void ClearSelection()
        {
            foreach (var offset in _selectedChain)
            {
                TileInstance t = boardManager.GetTileAtOffset(offset);
                if (t != null) t.IsSelected = false;
            }
            _selectedChain.Clear();
            selectionVisualizer?.ClearVisualization();
        }

        // New Input System + Legacy 양쪽 호환 — 수동 Raycast로 UI 위 여부 판별
        // IsPointerOverGameObject(-1)은 InputSystemUIInputModule에서 동작하지 않음
        private bool IsOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var pd = new PointerEventData(EventSystem.current) { position = screenPos };
            _uiRaycastCache.Clear();
            EventSystem.current.RaycastAll(pd, _uiRaycastCache);
            return _uiRaycastCache.Count > 0;
        }
    }
}
