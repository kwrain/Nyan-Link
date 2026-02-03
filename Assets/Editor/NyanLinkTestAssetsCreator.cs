using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using NyanLink.Data.Definitions;
using NyanLink.Data.Enums;

namespace NyanLink.Editor
{
    /// <summary>
    /// Phase 2 테스트에 필요한 기본 에셋 자동 생성
    /// </summary>
    public static class NyanLinkTestAssetsCreator
    {
        private const string RESOURCES_DATA_PATH = "Assets/Resources/Data";
        private const string GRID_SHAPES_PATH = RESOURCES_DATA_PATH + "/GridShapes";
        private const string TILES_PATH = "Assets/_NyanLink/Art/Tiles";

        [MenuItem("NyanLink/Setup/Create Phase 2 Test Assets")]
        public static void CreatePhase2TestAssets()
        {
            bool created = false;

            // 1. 폴더 생성
            EnsureDirectoryExists(RESOURCES_DATA_PATH);
            EnsureDirectoryExists(GRID_SHAPES_PATH);
            EnsureDirectoryExists(TILES_PATH);

            // 2. 기본 GridShapeData 생성
            if (CreateDefaultGridShapeData())
            {
                created = true;
            }

            // 3. 기본 헥사곤 타일 생성
            if (CreateDefaultHexagonTile())
            {
                created = true;
            }

            // 4. 헥사곤 테두리 Sprite 생성 (선택 시각화용)
            if (CreateHexagonOutlineSprite())
            {
                created = true;
            }

            // 5. 선택 시각화용 머티리얼 생성
            if (CreateSelectionMaterials())
            {
                created = true;
            }

            if (created)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(
                    "완료",
                    "Phase 2 테스트 에셋이 생성되었습니다!\n\n생성된 에셋:\n" +
                    "- Resources/Data/GridShapes/DefaultGridShape.asset\n" +
                    "- _NyanLink/Art/Tiles/HexagonTile_Red.asset\n" +
                    "- _NyanLink/Art/Tiles/HexagonTile_Blue.asset\n" +
                    "- _NyanLink/Art/Tiles/HexagonTile_Yellow.asset\n" +
                    "- _NyanLink/Art/Tiles/HexagonTile_Purple.asset\n" +
                    "- _NyanLink/Art/Tiles/HexagonTile_Orange.asset\n" +
                    "- _NyanLink/Art/Tiles/HexagonTile_Cyan.asset\n" +
                    "- _NyanLink/Art/Tiles/DefaultHexagonTile.asset\n\n" +
                    "PuzzleBoardManager에 할당해주세요.",
                    "확인"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "알림",
                    "모든 테스트 에셋이 이미 존재합니다.\n기존 에셋을 사용하세요.",
                    "확인"
                );
            }
        }

        /// <summary>
        /// 기본 GridShapeData 생성
        /// </summary>
        private static bool CreateDefaultGridShapeData()
        {
            string assetPath = GRID_SHAPES_PATH + "/DefaultGridShape.asset";
            
            // 이미 존재하는지 확인
            GridShapeData existing = AssetDatabase.LoadAssetAtPath<GridShapeData>(assetPath);
            if (existing != null)
            {
                Debug.Log($"GridShapeData가 이미 존재합니다: {assetPath}");
                return false;
            }

            // 새로 생성
            GridShapeData gridShape = ScriptableObject.CreateInstance<GridShapeData>();
            gridShape.width = 7;
            gridShape.height = 7;

            // 기본 벌집형 모양으로 초기화 (모든 타일 활성화)
            // 벌집형 그리드는 항상 완전한 모양이므로 마스크 초기화 불필요

            AssetDatabase.CreateAsset(gridShape, assetPath);
            Debug.Log($"기본 GridShapeData 생성됨: {assetPath}");
            return true;
        }

        /// <summary>
        /// 기본 헥사곤 타일 생성 (색상별로 생성)
        /// </summary>
        private static bool CreateDefaultHexagonTile()
        {
            bool created = false;
            
            // 육각형 스프라이트 먼저 생성
            Sprite hexagonSprite = CreateHexagonSprite();
            if (hexagonSprite == null)
            {
                Debug.LogWarning("육각형 스프라이트 생성 실패. 빈 타일을 생성합니다.");
            }

            // 6가지 색상별 타일 생성
            TileColor[] colors = { TileColor.Red, TileColor.Blue, TileColor.Yellow, TileColor.Purple, TileColor.Orange, TileColor.Cyan };
            
            foreach (TileColor color in colors)
            {
                string colorName = color.ToString();
                string assetPath = TILES_PATH + $"/HexagonTile_{colorName}.asset";

                // 이미 존재하는지 확인
                TileBase existing = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
                if (existing != null)
                {
                    Debug.Log($"헥사곤 타일이 이미 존재합니다: {assetPath}");
                    continue;
                }

                // ColoredHexagonTile 생성
                ColoredHexagonTile tile = ScriptableObject.CreateInstance<ColoredHexagonTile>();
                tile.name = $"HexagonTile_{colorName}";
                tile.tileColor = TileColorToUnityColor(color);
                
                if (hexagonSprite != null)
                {
                    tile.sprite = hexagonSprite;
                }

                AssetDatabase.CreateAsset(tile, assetPath);
                Debug.Log($"헥사곤 타일 생성됨: {assetPath} (색상: {color})");
                created = true;
            }

            // 기본 타일도 생성 (호환성을 위해)
            string defaultPath = TILES_PATH + "/DefaultHexagonTile.asset";
            TileBase defaultExisting = AssetDatabase.LoadAssetAtPath<TileBase>(defaultPath);
            if (defaultExisting == null)
            {
                Tile defaultTile = ScriptableObject.CreateInstance<Tile>();
                defaultTile.name = "DefaultHexagonTile";
                if (hexagonSprite != null)
                {
                    defaultTile.sprite = hexagonSprite;
                }
                AssetDatabase.CreateAsset(defaultTile, defaultPath);
                Debug.Log($"기본 헥사곤 타일 생성됨: {defaultPath}");
                created = true;
            }

            return created;
        }

        /// <summary>
        /// TileColor를 Unity Color로 변환
        /// </summary>
        private static Color TileColorToUnityColor(TileColor tileColor)
        {
            return tileColor switch
            {
                TileColor.Red => Color.red,
                TileColor.Blue => Color.blue,
                TileColor.Yellow => Color.yellow,
                TileColor.Purple => new Color(0.5f, 0f, 0.5f, 1f), // 보라색
                TileColor.Orange => new Color(1f, 0.5f, 0f, 1f), // 주황색
                TileColor.Cyan => Color.cyan,
                _ => Color.white
            };
        }

        /// <summary>
        /// 육각형 스프라이트 생성
        /// </summary>
        private static Sprite CreateHexagonSprite()
        {
            string texturePath = TILES_PATH + "/HexagonTexture.asset";
            
            // 이미 존재하는지 확인
            Texture2D existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (existingTexture != null)
            {
                // 기존 텍스처에서 스프라이트 찾기
                string[] guids = AssetDatabase.FindAssets("HexagonSprite t:Sprite", new[] { TILES_PATH });
                if (guids.Length > 0)
                {
                    string spritePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (existingSprite != null)
                    {
                        Debug.Log($"육각형 스프라이트가 이미 존재합니다: {spritePath}");
                        return existingSprite;
                    }
                }
            }

            // 육각형 텍스처 생성
            // Grid Cell Size (1, 1)에 맞추기 위해:
            // Pointy-top 헥사곤: 너비 = 2 * 반지름, 높이 = sqrt(3) * 반지름
            // Cell Size가 (1, 1)이면 반지름 = 0.5, 너비 = 1.0, 높이 = sqrt(3) * 0.5 ≈ 0.866
            // 하지만 Unity Hexagonal Grid는 Cell Size를 다르게 해석하므로, 
            // 스프라이트를 정사각형으로 만들고 Pixels Per Unit을 조정
            
            int size = 128; // 스프라이트 크기 (픽셀)
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            // 반지름을 Grid Cell Size에 맞춤: Cell Size (1, 1) 기준으로 반지름 0.5 유닛
            // Pixels Per Unit을 128로 설정하면: 128픽셀 = 1 유닛, 반지름 0.5 유닛 = 64픽셀
            float radius = size / 2f - 2f; // 약간의 여백 (반지름 약 62픽셀)

            // 육각형의 6개 꼭짓점 계산 (Pointy-top)
            Vector2[] vertices = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f - 30f) * Mathf.Deg2Rad; // -30도부터 시작 (Pointy-top)
                vertices[i] = center + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
            }

            // 육각형 내부 채우기
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    
                    // 점이 육각형 내부에 있는지 확인 (Ray casting 알고리즘)
                    bool inside = IsPointInHexagon(point, center, vertices);
                    
                    if (inside)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // 스프라이트 생성
            // Pixels Per Unit을 128로 설정: 128픽셀 = 1 유닛 (Grid Cell Size와 일치)
            // 이렇게 하면 스프라이트 크기가 정확히 1x1 유닛이 됨
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), // 피벗 중앙
                128f, // 픽셀당 유닛 (128 pixels = 1 unit, Grid Cell Size와 일치)
                0,
                SpriteMeshType.Tight
            );
            sprite.name = "HexagonSprite";

            // 텍스처와 스프라이트 저장
            AssetDatabase.CreateAsset(texture, texturePath);
            AssetDatabase.AddObjectToAsset(sprite, texturePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"육각형 스프라이트 생성됨: {texturePath}");
            return sprite;
        }

        /// <summary>
        /// 헥사곤 테두리 Sprite 생성 (선택 시각화용)
        /// </summary>
        private static bool CreateHexagonOutlineSprite()
        {
            string texturePath = TILES_PATH + "/HexagonOutlineTexture.asset";
            
            // 이미 존재하는지 확인
            Texture2D existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (existingTexture != null)
            {
                string[] guids = AssetDatabase.FindAssets("HexagonOutlineSprite t:Sprite", new[] { TILES_PATH });
                if (guids.Length > 0)
                {
                    Debug.Log($"헥사곤 테두리 스프라이트가 이미 존재합니다.");
                    return false;
                }
            }

            // 육각형 테두리 텍스처 생성
            // HexagonSprite와 동일한 크기 및 Pixels Per Unit 사용
            int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            // HexagonSprite와 동일한 반지름 사용
            float outerRadius = size / 2f - 2f;
            float innerRadius = size / 2f - 6f; // 테두리 두께

            // 육각형의 6개 꼭짓점 계산 (Pointy-top)
            Vector2[] outerVertices = new Vector2[6];
            Vector2[] innerVertices = new Vector2[6];
            
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f - 30f) * Mathf.Deg2Rad;
                outerVertices[i] = center + new Vector2(
                    Mathf.Cos(angle) * outerRadius,
                    Mathf.Sin(angle) * outerRadius
                );
                innerVertices[i] = center + new Vector2(
                    Mathf.Cos(angle) * innerRadius,
                    Mathf.Sin(angle) * innerRadius
                );
            }

            // 테두리 영역만 채우기 (외부 육각형 내부이지만 내부 육각형 외부)
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    bool inOuter = IsPointInHexagon(point, center, outerVertices);
                    bool inInner = IsPointInHexagon(point, center, innerVertices);
                    
                    // 외부 육각형 내부이지만 내부 육각형 외부인 영역만 테두리
                    if (inOuter && !inInner)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // 스프라이트 생성
            // HexagonSprite와 동일한 Pixels Per Unit 사용 (128 pixels = 1 unit)
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), // 피벗 중앙
                128f, // 픽셀당 유닛 (128 pixels = 1 unit, Grid Cell Size와 일치)
                0,
                SpriteMeshType.Tight
            );
            sprite.name = "HexagonOutlineSprite";

            // 텍스처와 스프라이트 저장
            AssetDatabase.CreateAsset(texture, texturePath);
            AssetDatabase.AddObjectToAsset(sprite, texturePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"헥사곤 테두리 스프라이트 생성됨: {texturePath}");
            return true;
        }

        /// <summary>
        /// 점이 육각형 내부에 있는지 확인 (Ray casting 알고리즘)
        /// </summary>
        private static bool IsPointInHexagon(Vector2 point, Vector2 center, Vector2[] vertices)
        {
            int intersections = 0;
            int vertexCount = vertices.Length;

            for (int i = 0; i < vertexCount; i++)
            {
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[(i + 1) % vertexCount];

                // 수평선과 교차하는지 확인
                if (((v1.y > point.y) != (v2.y > point.y)) &&
                    (point.x < (v2.x - v1.x) * (point.y - v1.y) / (v2.y - v1.y) + v1.x))
                {
                    intersections++;
                }
            }

            return (intersections % 2) == 1;
        }

        /// <summary>
        /// 선택 시각화용 머티리얼 생성
        /// </summary>
        private static bool CreateSelectionMaterials()
        {
            const string MATERIALS_PATH = "Assets/_NyanLink/Art/Materials";
            EnsureDirectoryExists(MATERIALS_PATH);

            bool created = false;

            // 1. 테두리용 머티리얼
            string outlineMatPath = MATERIALS_PATH + "/TileOutlineMaterial.mat";
            Material outlineMat = AssetDatabase.LoadAssetAtPath<Material>(outlineMatPath);
            if (outlineMat == null)
            {
                // Shader 찾기
                Shader outlineShader = Shader.Find("NyanLink/TileOutline");
                if (outlineShader != null)
                {
                    outlineMat = new Material(outlineShader);
                    outlineMat.name = "TileOutlineMaterial";
                    outlineMat.color = new Color(1f, 1f, 1f, 0.8f); // 흰색, 반투명
                    AssetDatabase.CreateAsset(outlineMat, outlineMatPath);
                    Debug.Log($"테두리 머티리얼 생성됨: {outlineMatPath}");
                    created = true;
                }
                else
                {
                    Debug.LogWarning("TileOutline Shader를 찾을 수 없습니다. Shader가 컴파일되었는지 확인하세요.");
                }
            }

            // 2. 연결선용 머티리얼
            string lineMatPath = MATERIALS_PATH + "/LineFlowMaterial.mat";
            Material lineMat = AssetDatabase.LoadAssetAtPath<Material>(lineMatPath);
            if (lineMat == null)
            {
                // Shader 찾기
                Shader lineShader = Shader.Find("NyanLink/LineFlow");
                if (lineShader != null)
                {
                    lineMat = new Material(lineShader);
                    lineMat.name = "LineFlowMaterial";
                    lineMat.color = new Color(0.7f, 0.9f, 1f, 0.8f); // 연한 하늘색
                    AssetDatabase.CreateAsset(lineMat, lineMatPath);
                    Debug.Log($"연결선 머티리얼 생성됨: {lineMatPath}");
                    created = true;
                }
                else
                {
                    Debug.LogWarning("LineFlow Shader를 찾을 수 없습니다. Shader가 컴파일되었는지 확인하세요.");
                }
            }

            return created;
        }

        /// <summary>
        /// 디렉토리 존재 확인 및 생성
        /// </summary>
        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentPath = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(path);

                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    EnsureDirectoryExists(parentPath);
                }

                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

    }

    /// <summary>
    /// 간단한 헥사곤 타일 (프로그래밍 방식으로 색상 변경 가능)
    /// </summary>
    [CreateAssetMenu(fileName = "ColoredHexagonTile", menuName = "NyanLink/Tiles/Colored Hexagon Tile", order = 1)]
    public class ColoredHexagonTile : Tile
    {
        [Header("타일 색상")]
        [Tooltip("타일의 색상 (렌더링용)")]
        public Color tileColor = Color.white;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);
            tileData.color = tileColor;
        }

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            return false;
        }
    }
}
