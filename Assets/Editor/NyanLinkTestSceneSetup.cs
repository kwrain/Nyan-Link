using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using NyanLink.Puzzle;
using NyanLink.Data.Definitions;
using NyanLink.Data.Enums;
using UnityEngine.EventSystems;

namespace NyanLink.Editor
{
    /// <summary>
    /// Phase 2 테스트 씬 자동 구성 에디터 스크립트
    /// </summary>
    public class NyanLinkTestSceneSetup : EditorWindow
    {
        [MenuItem("NyanLink/Setup/Phase 2 Test Scene")]
        public static void SetupTestScene()
        {
            // 새 씬 생성 또는 현재 씬 사용
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorUtility.DisplayDialog("씬 저장", "현재 씬에 변경사항이 있습니다. 계속하시겠습니까?", "계속", "취소"))
                {
                    return;
                }
            }

            // 씬 정리
            ClearScene();

            // 카메라 설정
            SetupCamera();

            // Grid 및 Tilemap 설정
            GameObject gridObject = SetupGridAndTilemap();

            // GameManager 설정
            SetupGameManager();

            // 기본 GridShapeData 확인
            CheckGridShapeData();

            Debug.Log("Phase 2 테스트 씬 구성 완료!");
            
            // 테스트 에셋 생성 안내
            if (EditorUtility.DisplayDialog(
                "씬 구성 완료",
                "Phase 2 테스트 씬 구성이 완료되었습니다!\n\n" +
                "다음 단계:\n" +
                "1. 테스트 에셋 생성 (NyanLink > Setup > Create Phase 2 Test Assets)\n" +
                "2. PuzzleBoardManager에 에셋 할당\n" +
                "3. Play 버튼을 눌러 테스트\n\n" +
                "지금 테스트 에셋을 생성하시겠습니까?",
                "에셋 생성",
                "나중에"))
            {
                NyanLinkTestAssetsCreator.CreatePhase2TestAssets();
            }
        }

        private static void ClearScene()
        {
            // 기존 오브젝트 제거
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name != "Main Camera" && obj.name != "Directional Light")
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        private static void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 10f;
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 1f);

            Debug.Log("카메라 설정 완료 (그리드 생성 후 자동으로 중앙에 배치됩니다)");
        }

        private static GameObject SetupGridAndTilemap()
        {
            // Grid 생성
            GameObject gridObject = new GameObject("Grid");
            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Hexagon;
            // Pointy-top 헥사곤의 경우 Unity가 자동으로 배치를 처리
            // Cell Size는 헥사곤의 바운딩 박스 크기를 의미
            // 스프라이트가 128픽셀, Pixels Per Unit이 128이면 정확히 1x1 유닛
            grid.cellSize = new Vector3(1f, 1f, 0f);
            grid.cellGap = Vector3.zero;
            
            // 헥사곤 타일맵의 경우 Unity Grid가 자동으로 배치를 처리함
            // Pointy-top 헥사곤: 홀수 열에서 자동 오프셋 적용
            
            // 헥사곤 타일맵의 경우 Grid 컴포넌트가 자동으로 배치를 처리함
            // Pointy-top 헥사곤: 홀수 열에서 오프셋 적용
            // Unity는 자동으로 처리하므로 추가 설정 불필요

            // Tilemap 생성
            GameObject tilemapObject = new GameObject("Tilemap");
            tilemapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
            
            // 타일맵 색상 모드 설정 (중요: 색상 변경이 작동하려면 필요)
            // TilemapRenderer의 기본 설정으로도 작동하지만, 명시적으로 설정

            // PuzzleBoardManager 추가
            PuzzleBoardManager boardManager = tilemapObject.AddComponent<PuzzleBoardManager>();
            boardManager.tilemap = tilemap;
            
            // 색상별 타일 자동 할당
            LoadColoredTiles(boardManager);

            // 타일 로드 후 최적 Cell Size 계산 (자동 적용은 하지 않음)
            // Inspector에서 PuzzleBoardManager 우클릭 > "Apply Optimal Cell Size"로 수동 적용 가능
            TileBase sampleTile = boardManager.redTile ?? boardManager.blueTile ?? boardManager.yellowTile ?? boardManager.hexagonTile;
            if (sampleTile != null)
            {
                Vector3 optimalSize = HexGridCalculator.CalculateCellSizeFromTile(sampleTile);
                Debug.Log($"[씬 설정] 계산된 최적 Cell Size: {optimalSize}");
                Debug.Log("Inspector에서 PuzzleBoardManager 컴포넌트를 우클릭하여 'Apply Optimal Cell Size'를 실행하세요.");
            }
            else
            {
                Debug.LogWarning("타일 리소스를 찾을 수 없어 기본 Cell Size를 사용합니다.");
            }

            // TileInputHandler 추가
            TileInputHandler inputHandler = tilemapObject.AddComponent<TileInputHandler>();
            inputHandler.boardManager = boardManager;

            // TileMatcher 추가
            TileMatcher tileMatcher = tilemapObject.AddComponent<TileMatcher>();
            tileMatcher.boardManager = boardManager;

            // TileSelectionVisualizer 추가
            TileSelectionVisualizer selectionVisualizer = tilemapObject.AddComponent<TileSelectionVisualizer>();
            selectionVisualizer.boardManager = boardManager;
            
            // 참조 연결
            inputHandler.tileMatcher = tileMatcher;
            inputHandler.selectionVisualizer = selectionVisualizer;

            // EventSystem 추가 (UI 입력 처리용)
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("Grid 및 Tilemap 설정 완료");
            return gridObject;
        }

        private static void SetupGameManager()
        {
            GameObject gameManagerObject = new GameObject("GameManager");

            // PuzzleBoardManager 초기화를 위한 컴포넌트 추가
            PuzzleBoardInitializer initializer = gameManagerObject.AddComponent<PuzzleBoardInitializer>();

            Debug.Log("GameManager 설정 완료");
        }

        private static void CheckGridShapeData()
        {
            // Resources 폴더에서 GridShapeData 찾기
            GridShapeData[] gridShapes = Resources.LoadAll<GridShapeData>("Data/GridShapes");
            
            if (gridShapes == null || gridShapes.Length == 0)
            {
                Debug.LogWarning("GridShapeData를 찾을 수 없습니다. Resources/Data/GridShapes 폴더에 생성해주세요.");
                Debug.LogWarning("생성 방법: Project 창에서 우클릭 > Create > NyanLink > Grid Shape");
            }
            else
            {
                Debug.Log($"GridShapeData {gridShapes.Length}개를 찾았습니다.");
            }
        }

        /// <summary>
        /// 색상별 타일 자동 할당
        /// </summary>
        private static void LoadColoredTiles(PuzzleBoardManager boardManager)
        {
            string tilesPath = "Assets/_NyanLink/Art/Tiles";
            
            // 색상별 타일 로드
            boardManager.redTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{tilesPath}/HexagonTile_Red.asset");
            boardManager.blueTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{tilesPath}/HexagonTile_Blue.asset");
            boardManager.yellowTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{tilesPath}/HexagonTile_Yellow.asset");
            boardManager.purpleTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{tilesPath}/HexagonTile_Purple.asset");
            boardManager.orangeTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{tilesPath}/HexagonTile_Orange.asset");
            boardManager.cyanTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{tilesPath}/HexagonTile_Cyan.asset");
            
            // 기본 타일도 로드
            boardManager.hexagonTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{tilesPath}/DefaultHexagonTile.asset");
            
            int loadedCount = 0;
            if (boardManager.redTile != null) loadedCount++;
            if (boardManager.blueTile != null) loadedCount++;
            if (boardManager.yellowTile != null) loadedCount++;
            if (boardManager.purpleTile != null) loadedCount++;
            if (boardManager.orangeTile != null) loadedCount++;
            if (boardManager.cyanTile != null) loadedCount++;
            
            if (loadedCount < 6)
            {
                Debug.LogWarning($"색상별 타일이 일부만 로드되었습니다. ({loadedCount}/6) 테스트 에셋을 생성해주세요.");
            }
            else
            {
                Debug.Log("색상별 타일이 모두 로드되었습니다.");
            }
        }
    }
}
