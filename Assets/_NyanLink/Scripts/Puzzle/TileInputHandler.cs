using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

namespace NineMood.Puzzle
{
    /// <summary>New Input System 마우스/Enhanced Touch를 같은 선택·판정 경로로 처리한다.</summary>
    public class TileInputHandler : MonoBehaviour
    {
        [Header("참조")]
        public PuzzleBoardManager boardManager;
        public TileMatcher tileMatcher;
        public TileSelectionVisualizer selectionVisualizer;
        public BalanceData balanceData;
        [SerializeField] private Camera puzzleCamera;

        [Header("입력 설정")]
        [Range(0.01f, 1f)] public float mouseMoveSensitivity = 0.1f;
        [Range(1f, 1.5f), Tooltip("터치 판정 확대 후보. 겹치면 가장 가까운 중심 선택")]
        public float touchHitAreaScale = 1.3f;
        [Range(0f, 0.5f), Tooltip("터치 위치에서 위로 보정할 화면 픽셀 수 / 화면 높이. 실기기 튜닝 전 기본 0")]
        public float touchVerticalOffset = 0f;
        public bool debugLog;

        private readonly List<Vector3Int> _selectedChain = new List<Vector3Int>();
        private readonly List<RaycastResult> _uiRaycastCache = new List<RaycastResult>();
        private Vector2 _lastInputPosition;
        private bool _isInputActive;
        private bool _inputLocked;
        private float _lockTimer;
        private int _activeFingerIndex = -1;
        private bool _usingTouch;

        public IReadOnlyList<Vector3Int> SelectedChain => _selectedChain;
        public bool IsInputLocked => _inputLocked;
        public bool IsSelecting => _isInputActive;

        private void Awake()
        {
            if (boardManager == null) boardManager = GetComponent<PuzzleBoardManager>();
            if (tileMatcher == null) tileMatcher = GetComponent<TileMatcher>();
            if (selectionVisualizer == null) selectionVisualizer = GetComponent<TileSelectionVisualizer>();
        }

        private void OnEnable()
        {
            ETouch.EnhancedTouchSupport.Enable();
            ETouch.Touch.onFingerDown += OnFingerDown;
            ETouch.Touch.onFingerMove += OnFingerMove;
            ETouch.Touch.onFingerUp += OnFingerUp;
            GameEvents.OnGameStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            ETouch.Touch.onFingerDown -= OnFingerDown;
            ETouch.Touch.onFingerMove -= OnFingerMove;
            ETouch.Touch.onFingerUp -= OnFingerUp;
            ETouch.EnhancedTouchSupport.Disable();
            GameEvents.OnGameStateChanged -= OnStateChanged;
            ClearSelection();
            _activeFingerIndex = -1;
        }

        private bool CanAcceptInput() => !_inputLocked && (!GameManager.IsCreated ||
            GameManager.Instance.CurrentState == GameState.RunnerPhase ||
            GameManager.Instance.CurrentState == GameState.BossPhase);

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Briefing)
            {
                _inputLocked = false;
                _lockTimer = 0f;
                _activeFingerIndex = -1;
                _usingTouch = false;
            }
            if (state != GameState.RunnerPhase && state != GameState.BossPhase) ClearSelection();
        }

        private void Update()
        {
            if (_inputLocked)
            {
                _lockTimer -= Time.deltaTime;
                if (_lockTimer <= 0f) _inputLocked = false;
            }
            if (!CanAcceptInput()) { ClearSelection(); return; }
            // 연결된 마우스가 있는 모바일에서도 같은 터치를 이중 처리하지 않는다.
            if (_activeFingerIndex >= 0 || ETouch.Touch.activeTouches.Count > 0) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _usingTouch = false;
                OnInputBegan(position);
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                OnInputMoved(position);
                OnInputReleased();
            }
            else if (mouse.leftButton.isPressed && _isInputActive &&
                     Vector2.Distance(position, _lastInputPosition) >= mouseMoveSensitivity)
                OnInputMoved(position);
        }

        private void OnFingerDown(ETouch.Finger finger)
        {
            if (_activeFingerIndex >= 0 || _isInputActive || !CanAcceptInput()) return;
            _usingTouch = true;
            OnInputBegan(finger.screenPosition);
            if (_isInputActive) _activeFingerIndex = finger.index;
        }

        private void OnFingerMove(ETouch.Finger finger)
        {
            if (finger.index != _activeFingerIndex || !CanAcceptInput()) return;
            OnInputMoved(finger.screenPosition);
        }

        private void OnFingerUp(ETouch.Finger finger)
        {
            if (finger.index != _activeFingerIndex) return;
            if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Canceled) ClearSelection();
            else
            {
                OnInputMoved(finger.screenPosition);
                OnInputReleased();
            }
            _activeFingerIndex = -1;
            _usingTouch = false;
        }

        private void OnInputBegan(Vector2 position)
        {
            if (!CanAcceptInput() || IsOverUI(position)) return;
            ClearSelection();
            if (!TryGetTileAtScreenPosition(position, out var cell, _usingTouch)) return;
            var tile = boardManager.GetTileAtOffset(cell);
            _selectedChain.Add(cell);
            tile.IsSelected = true;
            _isInputActive = true;
            _lastInputPosition = position;
            UpdateSelectionVisualization();
        }

        private void OnInputMoved(Vector2 position)
        {
            if (!_isInputActive || !CanAcceptInput() || _selectedChain.Count == 0) return;
            _lastInputPosition = position;
            if (!TryGetTileAtScreenPosition(position, out var cell, _usingTouch)) return;
            int previousIndex = _selectedChain.IndexOf(cell);
            if (previousIndex >= 0)
            {
                for (int i = _selectedChain.Count - 1; i > previousIndex; i--)
                {
                    var previous = boardManager.GetTileAtOffset(_selectedChain[i]);
                    if (previous != null) previous.IsSelected = false;
                    _selectedChain.RemoveAt(i);
                }
                UpdateSelectionVisualization();
                return;
            }
            var touched = boardManager.GetTileAtOffset(cell);
            var last = boardManager.GetTileAtOffset(_selectedChain[_selectedChain.Count - 1]);
            if (last == null || touched.Color != last.Color)
            {
                ClearSelection(); // 색이 다른 타일에 진입한 취소는 오답 잠금을 만들지 않는다.
                return;
            }
            if (!HexOffsetUtils.IsAdjacent(last.OffsetPosition, cell)) return;
            _selectedChain.Add(cell);
            touched.IsSelected = true;
            UpdateSelectionVisualization();
        }

        private void OnInputReleased()
        {
            if (!_isInputActive) return;
            var chain = new List<Vector3Int>(_selectedChain);
            bool canMatch = CanAcceptInput() && chain.Count >= 2;
            ClearSelection();
            if (canMatch && tileMatcher != null) tileMatcher.ProcessMatch(chain);
        }

        /// <summary>화면→타일 중심 판정. 카메라 영역 밖은 보정으로도 선택할 수 없다.</summary>
        public bool TryGetTileAtScreenPosition(Vector2 position, out Vector3Int cell, bool isTouch = false)
        {
            cell = default;
            var camera = GetPuzzleCamera();
            if (camera == null || boardManager == null || boardManager.tilemap == null ||
                !camera.pixelRect.Contains(position)) return false;
            if (isTouch) position.y += touchVerticalOffset * Screen.height;
            if (!camera.pixelRect.Contains(position)) return false;
            var transform = boardManager.tilemap.transform;
            var plane = new Plane(transform.forward, boardManager.GetCellCenterWorld(Vector3Int.zero));
            var ray = camera.ScreenPointToRay(position);
            if (!plane.Raycast(ray, out float distance)) return false;
            return boardManager.TryGetTileAtWorldPosition(ray.GetPoint(distance),
                isTouch ? touchHitAreaScale : 1f, out cell);
        }

        private Camera GetPuzzleCamera()
        {
            if (puzzleCamera != null) return puzzleCamera;
            foreach (var camera in Camera.allCameras)
                if (camera.name == "Camera_Puzzle") return puzzleCamera = camera;
            return null;
        }

        private void UpdateSelectionVisualization()
        {
            if (selectionVisualizer == null) return;
            if (_selectedChain.Count < 2) { selectionVisualizer.ClearVisualization(); return; }
            var balance = balanceData != null ? balanceData : DataManager.Instance?.BalanceData;
            var first = boardManager.GetTileAtOffset(_selectedChain[0]);
            selectionVisualizer.UpdateConnectionLine(_selectedChain, null,
                ChainEvaluator.GetEffectLevel(balance, _selectedChain.Count), first.Color);
        }

        /// <summary>장애물 오답 등 외부에서 명시적으로 요청하는 잠금. 선택을 취소하고 잔여 시간을 갱신한다.</summary>
        public void LockInput(float duration)
        {
            if (duration <= 0f) return;
            ClearSelection();
            _lockTimer = Mathf.Max(_lockTimer, duration);
            _inputLocked = true;
        }

        private void ClearSelection()
        {
            foreach (var offset in _selectedChain)
            {
                var tile = boardManager != null ? boardManager.GetTileAtOffset(offset) : null;
                if (tile != null) tile.IsSelected = false;
            }
            _selectedChain.Clear();
            _isInputActive = false;
            selectionVisualizer?.ClearVisualization();
        }

        private bool IsOverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            _uiRaycastCache.Clear();
            EventSystem.current.RaycastAll(pointer, _uiRaycastCache);
            return _uiRaycastCache.Count > 0;
        }
    }
}
