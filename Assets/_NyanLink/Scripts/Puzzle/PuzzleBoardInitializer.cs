using UnityEngine;
using NyanLink.Data.Definitions;

namespace NyanLink.Puzzle
{
    /// <summary>
    /// 게임 시작 시 PuzzleBoardManager 자동 초기화
    /// </summary>
    public class PuzzleBoardInitializer : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("GridShapeData가 없으면 기본 그리드 자동 생성")]
        public bool createDefaultGridIfMissing = true;

        [Tooltip("초기화 지연 시간 (초)")]
        public float initializationDelay = 0.1f;

        private PuzzleBoardManager _boardManager;

        private void Start()
        {
            _boardManager = GetComponent<PuzzleBoardManager>();
            if (_boardManager == null)
            {
                _boardManager = FindFirstObjectByType<PuzzleBoardManager>();
            }

            if (_boardManager != null)
            {
                // GridShapeData 확인 및 생성
                if (_boardManager.gridShapeData == null && createDefaultGridIfMissing)
                {
                    CreateDefaultGridShapeData();
                }

                // 지연 후 초기화
                Invoke(nameof(InitializeBoard), initializationDelay);
            }
            else
            {
                Debug.LogError("PuzzleBoardInitializer: PuzzleBoardManager를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// 그리드 초기화
        /// </summary>
        private void InitializeBoard()
        {
            if (_boardManager != null)
            {
                _boardManager.Initialize();
            }
        }

        /// <summary>
        /// 기본 GridShapeData 생성
        /// </summary>
        private void CreateDefaultGridShapeData()
        {
            // Resources 폴더에서 기본 GridShapeData 로드 시도
            GridShapeData defaultGridShape = Resources.Load<GridShapeData>("Data/GridShapes/DefaultGridShape");
            
            if (defaultGridShape == null)
            {
                Debug.LogWarning("PuzzleBoardInitializer: 기본 GridShapeData를 찾을 수 없습니다. " +
                               "Unity 메뉴에서 'NyanLink/Setup/Create Phase 2 Test Assets'를 실행하여 생성하세요.");
            }
            else
            {
                _boardManager.gridShapeData = defaultGridShape;
                Debug.Log("PuzzleBoardInitializer: 기본 GridShapeData를 로드했습니다.");
            }
        }
    }
}
