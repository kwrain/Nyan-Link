using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using NineMood.Boss;
using NineMood.Boss.Patterns;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Puzzle;
using NineMood.Runner;
using NineMood.UI;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;

namespace NineMood.Editor
{
    /// <summary>
    /// Phase 2/3 테스트 씬 자동 구성 에디터 스크립트
    /// </summary>
    public class NyanLinkTestSceneSetup : EditorWindow
    {
        // ── Phase 3: 듀얼 카메라 씬 ──────────────────────────────────

        [MenuItem("NyanLink/Setup/Phase 3 Scene (Dual Camera)")]
        public static void SetupPhase3Scene()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorUtility.DisplayDialog("씬 저장",
                    "현재 씬에 변경사항이 있습니다. 계속하시겠습니까?", "계속", "취소"))
                    return;
            }

            ClearScene();

            // 1. 듀얼 카메라 리그 생성
            GameObject cameraRig = SetupDualCameraRig();

            // 2. 퍼즐 월드 (Y=0) — Phase 2와 동일
            SetupGridAndTilemap();

            // 3. 러너 월드 (Y=50)
            SetupRunnerWorld();

            // 4. 매니저들
            SetupManagers();

            // 5. 레이어 안내
            EnsureTopViewLayer();

            // 6. UI
            NyanLinkUISetup.CreateGameUI();

            CheckGridShapeData();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Phase 3 씬] 듀얼 카메라 씬 구성 완료!");

            EditorUtility.DisplayDialog(
                "Phase 3 씬 구성 완료",
                "듀얼 카메라 씬이 구성되었습니다.\n\n" +
                "필수 수동 작업:\n" +
                "1. Project Settings > Tags and Layers에서\n" +
                "   'TopView' 레이어 추가\n" +
                "2. CameraRig > DualCameraRig 컴포넌트에서\n" +
                "   우클릭 > 'Apply Culling Masks' 실행\n" +
                "3. RunnerAnchor 하위 오브젝트를 'TopView' 레이어로 변경\n" +
                "4. PuzzleBoardManager에 GridShapeData 에셋 할당\n" +
                "5. Managers > SaveManager에 StageData 에셋 할당\n" +
                "   (체크포인트 저장 활성화 필수)",
                "확인");
        }

        private static GameObject SetupDualCameraRig()
        {
            // CameraRig 루트
            GameObject rigObj = new GameObject("CameraRig");
            DualCameraRig rig = rigObj.AddComponent<DualCameraRig>();

            // Camera_Puzzle — 하단 60%, 퍼즐 월드(Y=0)
            GameObject puzzleCamObj = new GameObject("Camera_Puzzle");
            puzzleCamObj.transform.SetParent(rigObj.transform);
            Camera puzzleCam = puzzleCamObj.AddComponent<Camera>();
            puzzleCam.orthographic     = true;
            puzzleCam.clearFlags       = CameraClearFlags.SolidColor;
            puzzleCam.backgroundColor  = new Color(0.13f, 0.13f, 0.20f);
            puzzleCam.depth            = 0;
            puzzleCam.tag              = "MainCamera";
            puzzleCamObj.transform.position = new Vector3(0f, 0f, -10f);

            // Camera_Runner — 상단 40%, 러너 월드(Y=50)
            GameObject runnerCamObj = new GameObject("Camera_Runner");
            runnerCamObj.transform.SetParent(rigObj.transform);
            Camera runnerCam = runnerCamObj.AddComponent<Camera>();
            runnerCam.orthographic    = true;
            runnerCam.clearFlags      = CameraClearFlags.SolidColor;
            runnerCam.backgroundColor = new Color(0.10f, 0.10f, 0.18f);
            runnerCam.depth           = 1;
            runnerCamObj.transform.position = new Vector3(0f, 50f, -10f);

            // DualCameraRig에 카메라 할당 후 분할 적용
            rig.cameraPuzzle = puzzleCam;
            rig.cameraRunner = runnerCam;
            rig.ApplySplit();

            Debug.Log("[Phase 3] 듀얼 카메라 리그 생성 완료 (Camera_Puzzle / Camera_Runner)");
            return rigObj;
        }

        private static void SetupRunnerWorld()
        {
            // 러너 월드 앵커 (Y=50)
            GameObject runnerAnchor = new GameObject("RunnerAnchor");
            runnerAnchor.transform.position = new Vector3(0f, 50f, 0f);

            // RunnerProgressSystem
            GameObject progressObj = new GameObject("RunnerProgressSystem");
            progressObj.transform.SetParent(runnerAnchor.transform);
            RunnerProgressSystem progressSystem = progressObj.AddComponent<RunnerProgressSystem>();

            // PlayerCharacter (플레이스홀더)
            GameObject playerObj = new GameObject("PlayerCharacter");
            playerObj.transform.SetParent(runnerAnchor.transform);
            playerObj.transform.localPosition = new Vector3(-3f, 0f, 0f);
            RunnerController runnerCtrl = playerObj.AddComponent<RunnerController>();

            // 플레이스홀더 시각 표시 (Quad)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "RunnerVisual";
            visual.transform.SetParent(playerObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale    = new Vector3(0.8f, 1.2f, 1f);
            visual.GetComponent<Renderer>().material.color = Color.cyan;
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // ObstacleSpawner
            GameObject spawnerObj = new GameObject("ObstacleSpawner");
            spawnerObj.transform.SetParent(runnerAnchor.transform);
            ObstacleSpawner spawner = spawnerObj.AddComponent<ObstacleSpawner>();
            spawner.runner         = runnerCtrl;
            spawner.progressSystem = progressSystem;
            spawner.spawnX         = 7f;

            // RunnerWarningHUD — Screen Space Overlay 캔버스 자동 생성
            var hudObj = new GameObject("RunnerWarningHUD");
            hudObj.transform.SetParent(runnerAnchor.transform);
            hudObj.AddComponent<RunnerWarningHUD>();

            Debug.Log("[Phase 3] 러너 월드 구성 완료 (Y=50 기준)");
        }

        private static void SetupManagers()
        {
            // 통합 매니저 오브젝트
            GameObject managers = new GameObject("Managers");

            managers.AddComponent<PuzzleBoardInitializer>();

            // GameManager, StaminaManager, ScoreManager
            // — Singleton이므로 씬에 하나씩만 배치
            managers.AddComponent<GameManager>();
            managers.AddComponent<StaminaManager>();
            managers.AddComponent<ScoreManager>();

            // SaveManager — 씬 참조 연결 (RunnerWorld가 먼저 생성된 상태)
            SaveManager saveManager = managers.AddComponent<SaveManager>();
            saveManager.progressSystem  = Object.FindAnyObjectByType<RunnerProgressSystem>();
            saveManager.obstacleSpawner = Object.FindAnyObjectByType<ObstacleSpawner>();
            // stageData는 스테이지별 ScriptableObject이므로 Inspector에서 수동 할당 필요

            // ── BossController + 패턴 컴포넌트 ───────────────────────────
            GameObject bossGO = new GameObject("BossController");
            BossController bossCtrl = bossGO.AddComponent<BossController>();
            bossCtrl.timeAttackPattern     = bossGO.AddComponent<TimeAttackPattern>();
            bossCtrl.rhythmNotePattern     = bossGO.AddComponent<RhythmNotePattern>();
            bossCtrl.colorQuotaPattern     = bossGO.AddComponent<ColorQuotaPattern>();
            bossCtrl.memorySequencePattern = bossGO.AddComponent<MemorySequencePattern>();
            // config, balance는 Inspector에서 할당 필요

            // EventSystem (New Input System)
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            Debug.Log("[Phase 4] 매니저 구성 완료\n" +
                      "· SaveManager.stageData — Inspector에서 할당\n" +
                      "· BossController.config, .balance — Inspector에서 할당");
        }

        private static void EnsureTopViewLayer()
        {
            int layer = LayerMask.NameToLayer("TopView");
            if (layer < 0)
            {
                Debug.LogWarning("[Phase 3] 'TopView' 레이어가 없습니다. " +
                                 "Project Settings > Tags and Layers에서 수동으로 추가하세요. " +
                                 "추가 후 DualCameraRig > 'Apply Culling Masks'를 실행하세요.");
            }
            else
            {
                Debug.Log($"[Phase 3] 'TopView' 레이어 확인 (index={layer})");
            }
        }

        // ── Phase 2: 기존 씬 ──────────────────────────────────────────

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

            // UGUI 생성 (GameHUD, GameReadyPanel, GameOverPanel, StageClearPanel, DebugPanel)
            NyanLinkUISetup.CreateGameUI();

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
            
            // 타일 앵커 설정: 스프라이트의 pivot이 (0.5, 0.5)이므로 셀 중심에 맞춤
            // 타일 앵커는 스프라이트의 pivot이 배치될 셀 내 위치를 지정
            // (0.5, 0.5, 0) = 셀 중심, (0, 0, 0) = 셀 좌측 하단 모서리
            tilemap.orientationMatrix = Matrix4x4.TRS(
                new Vector3(0.5f, 0.5f, 0f), // 타일 앵커를 셀 중심으로 설정
                Quaternion.identity,
                Vector3.one
            );
            
            TilemapRenderer tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
            
            // 타일맵 색상 모드 설정 (중요: 색상 변경이 작동하려면 필요)
            // TilemapRenderer의 기본 설정으로도 작동하지만, 명시적으로 설정

            // PuzzleBoardManager 추가
            PuzzleBoardManager boardManager = tilemapObject.AddComponent<PuzzleBoardManager>();
            boardManager.tilemap = tilemap;
            
            // AnimatedTile은 PuzzleBoardManager의 autoLoadTiles가 true이면 자동으로 로드됨
            // 필요시 수동으로 다시 로드: Inspector에서 PuzzleBoardManager 우클릭 > "Reload Animated Tiles"

            // 타일 로드 후 최적 Cell Size 계산 (자동 적용은 하지 않음)
            // Inspector에서 PuzzleBoardManager 우클릭 > "Apply Optimal Cell Size"로 수동 적용 가능
            TileBase sampleTile = boardManager.hexagonTile;
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

            // EventSystem 추가 (UI 입력 처리용 — New Input System 호환)
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
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
        /// AnimatedTile 자동 할당 (더 이상 사용되지 않음, PuzzleBoardManager가 자동으로 로드함)
        /// </summary>
        [System.Obsolete("LoadColoredTiles는 더 이상 사용되지 않습니다. PuzzleBoardManager가 자동으로 AnimatedTile을 로드합니다.")]
        private static void LoadColoredTiles(PuzzleBoardManager boardManager)
        {
            // PuzzleBoardManager의 autoLoadTiles가 true이면 자동으로 로드됨
            Debug.Log("PuzzleBoardManager가 자동으로 AnimatedTile을 로드합니다. (autoLoadTiles = true)");
        }
    }
}
