using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using NyanLink.Data.Definitions;
using NyanLink.Data.Enums;
using System;
using System.Reflection;

namespace NyanLink.Editor
{
    /// <summary>
    /// NyanLink 테스트 에셋 및 타일 에셋 자동 생성
    /// Phase 2 테스트 에셋 및 AnimatedTile 생성 기능 포함
    /// </summary>
    public static class NyanLinkTestAssetsCreator
    {
        private const string RESOURCES_DATA_PATH = "Assets/Resources/Data";
        private const string GRID_SHAPES_PATH = RESOURCES_DATA_PATH + "/GridShapes";
        private const string TILES_PATH = "Assets/_NyanLink/Art/Tiles";

        #region Phase 2 Test Assets

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

            // 3. 기본 헥사곤 스프라이트 생성 (AnimatedTile 생성에 필요)
            if (CreateHexagonSprite() != null)
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
                    "- _NyanLink/Art/Tiles/HexagonTexture.asset (HexagonSprite 포함)\n\n" +
                    "타일 에셋은 'NyanLink > Setup > Create Animated Tiles'를 사용하여 생성하세요.",
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

        #endregion

        #region AnimatedTile Creation

        [MenuItem("NyanLink/Setup/Create Animated Tiles")]
        public static void CreateAnimatedTiles()
        {
            bool created = false;
            int createdCount = 0;

            // 폴더 생성
            EnsureDirectoryExists(TILES_PATH);

            // 기본 헥사곤 스프라이트 가져오기 또는 생성
            Sprite baseHexagonSprite = GetOrCreateHexagonSprite();
            if (baseHexagonSprite == null)
            {
                EditorUtility.DisplayDialog(
                    "에러",
                    "헥사곤 스프라이트를 찾을 수 없습니다.\n" +
                    "먼저 'NyanLink > Setup > Create Phase 2 Test Assets'를 실행하여 기본 스프라이트를 생성해주세요.",
                    "확인"
                );
                return;
            }

            // 6가지 색상
            TileColor[] colors = { 
                TileColor.Red, 
                TileColor.Blue, 
                TileColor.Yellow, 
                TileColor.Purple, 
                TileColor.Orange, 
                TileColor.Cyan 
            };

            // 3가지 상태
            TileState[] states = { 
                TileState.Normal, 
                TileState.ItemLv1, 
                TileState.ItemLv2 
            };

            // 각 색상과 상태 조합으로 AnimatedTile 생성
            foreach (TileColor color in colors)
            {
                foreach (TileState state in states)
                {
                    if (CreateAnimatedTile(color, state, baseHexagonSprite))
                    {
                        created = true;
                        createdCount++;
                    }
                }
            }

            if (created)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "완료",
                    $"AnimatedTile 생성이 완료되었습니다!\n\n" +
                    $"생성된 타일: {createdCount}개\n" +
                    $"(6가지 색상 × 3가지 상태 = 18개)\n\n" +
                    $"위치: {TILES_PATH}\n\n" +
                    $"네이밍 규칙:\n" +
                    $"HexagonTile_{{Color}}_{{State}}\n" +
                    $"예: HexagonTile_Red_Normal\n" +
                    $"    HexagonTile_Red_ItemLv1\n" +
                    $"    HexagonTile_Red_ItemLv2",
                    "확인"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "알림",
                    "모든 AnimatedTile이 이미 존재합니다.\n기존 타일을 사용하세요.",
                    "확인"
                );
            }
        }

        /// <summary>
        /// 특정 색상과 상태의 AnimatedTile 생성
        /// </summary>
        private static bool CreateAnimatedTile(TileColor color, TileState state, Sprite baseSprite)
        {
            string colorName = color.ToString();
            string stateName = state.ToString();
            string tileName = $"HexagonTile_{colorName}_{stateName}";
            string assetPath = $"{TILES_PATH}/{tileName}.asset";

            // 이미 존재하는지 확인
            TileBase existing = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
            if (existing != null)
            {
                Debug.Log($"AnimatedTile이 이미 존재합니다: {assetPath}");
                return false;
            }

            // AnimatedTile 생성 (리플렉션 사용)
            Type animatedTileType = GetAnimatedTileType();
            if (animatedTileType == null)
            {
                Debug.LogError("AnimatedTile 타입을 찾을 수 없습니다. 2D Tilemap Extras 패키지가 설치되어 있는지 확인해주세요.");
                return false;
            }

            ScriptableObject animatedTile = ScriptableObject.CreateInstance(animatedTileType);
            animatedTile.name = tileName;

            // 상태에 따라 스프라이트 배열 설정 (색상별 스프라이트 생성)
            Sprite[] sprites = GetSpritesForState(state, baseSprite, color);
            
            // 리플렉션을 사용하여 속성 설정
            SetAnimatedTileProperties(animatedTile, sprites, color, state);

            AssetDatabase.CreateAsset(animatedTile, assetPath);
            Debug.Log($"AnimatedTile 생성됨: {assetPath} (색상: {color}, 상태: {state})");
            return true;
        }

        /// <summary>
        /// AnimatedTile 타입 가져오기 (리플렉션 사용)
        /// </summary>
        private static Type GetAnimatedTileType()
        {
            Type animatedTileType = Type.GetType("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras");
            if (animatedTileType == null)
            {
                // 다른 어셈블리 이름 시도
                animatedTileType = Type.GetType("UnityEngine.Tilemaps.AnimatedTile, Unity.2D.Tilemap.Extras.Editor");
            }
            return animatedTileType;
        }

        /// <summary>
        /// 상태에 따른 스프라이트 배열 반환
        /// 색상별로 다른 스프라이트를 생성하여 사용
        /// </summary>
        private static Sprite[] GetSpritesForState(TileState state, Sprite baseSprite, TileColor color)
        {
            // 색상별 스프라이트 생성 (기본 스프라이트에 색상 틴트 적용)
            Sprite coloredSprite = CreateColoredSprite(baseSprite, color);
            return new Sprite[] { coloredSprite };
        }

        /// <summary>
        /// 색상별 스프라이트 생성 (기본 스프라이트에 색상 틴트 적용)
        /// 스프라이트를 에셋으로 저장하여 재사용 가능하게 함
        /// </summary>
        private static Sprite CreateColoredSprite(Sprite baseSprite, TileColor color)
        {
            if (baseSprite == null)
            {
                return null;
            }

            string colorName = color.ToString();
            string spriteName = $"HexagonSprite_{colorName}";
            string spritePath = $"{TILES_PATH}/{spriteName}.asset";

            // 이미 생성된 색상별 스프라이트가 있는지 확인
            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            // 기본 스프라이트의 텍스처 가져오기
            Texture2D originalTexture = baseSprite.texture;
            
            // 텍스처가 읽기 가능한지 확인
            if (!originalTexture.isReadable)
            {
                Debug.LogWarning($"텍스처가 읽기 불가능합니다: {originalTexture.name}. " +
                               $"텍스처 Import Settings에서 'Read/Write Enabled'를 활성화해주세요. " +
                               $"기본 스프라이트를 사용합니다.");
                return baseSprite; // 원본 스프라이트 반환
            }

            int width = originalTexture.width;
            int height = originalTexture.height;

            // 새 텍스처 생성
            Texture2D coloredTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            // 원본 텍스처의 픽셀 읽기
            Color[] pixels = originalTexture.GetPixels();
            
            // 색상 틴트 적용
            Color tintColor = TileColorToUnityColor(color);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0f) // 알파가 있는 픽셀만 색상 적용
                {
                    pixels[i] = pixels[i] * tintColor;
                }
            }
            
            coloredTexture.SetPixels(pixels);
            coloredTexture.Apply();

            // 새 스프라이트 생성
            // 새 텍스처는 전체 텍스처 크기이므로 rect는 (0, 0, width, height)로 설정
            // pivot은 원본 스프라이트의 pivot을 사용하되, 정규화된 값으로 변환 필요
            Rect spriteRect = new Rect(0, 0, width, height); // 전체 텍스처 사용
            
            // baseSprite.pivot은 픽셀 단위이므로 정규화된 값으로 변환
            // pivot = (픽셀 위치) / (스프라이트 크기)
            Vector2 normalizedPivot = new Vector2(
                baseSprite.pivot.x / baseSprite.rect.width,
                baseSprite.pivot.y / baseSprite.rect.height
            );
            
            Sprite coloredSprite = Sprite.Create(
                coloredTexture,
                spriteRect, // 전체 텍스처 사용
                normalizedPivot, // 정규화된 pivot 사용
                baseSprite.pixelsPerUnit, // 동일한 Pixels Per Unit 사용
                0,
                SpriteMeshType.Tight
            );
            coloredSprite.name = spriteName;
            
            // 디버그 로그
            Debug.Log($"[색상별 스프라이트 생성] " +
                     $"Color: {color}, " +
                     $"Base Sprite Rect: {baseSprite.rect}, " +
                     $"Base Sprite Pivot (픽셀): {baseSprite.pivot}, " +
                     $"New Sprite Rect: {spriteRect}, " +
                     $"Normalized Pivot: {normalizedPivot}, " +
                     $"Pixels Per Unit: {baseSprite.pixelsPerUnit}");

            // 텍스처와 스프라이트를 에셋으로 저장
            AssetDatabase.CreateAsset(coloredTexture, spritePath);
            AssetDatabase.AddObjectToAsset(coloredSprite, spritePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"색상별 스프라이트 생성됨: {spritePath} (색상: {color})");
            return coloredSprite;
        }

        /// <summary>
        /// AnimatedTile 속성 설정 (리플렉션 사용)
        /// </summary>
        private static void SetAnimatedTileProperties(ScriptableObject animatedTile, Sprite[] sprites, TileColor color, TileState state)
        {
            Type tileType = animatedTile.GetType();
            
            // m_AnimatedSprites 설정
            FieldInfo spritesField = tileType.GetField("m_AnimatedSprites", BindingFlags.NonPublic | BindingFlags.Instance);
            if (spritesField != null)
            {
                spritesField.SetValue(animatedTile, sprites);
            }

            // m_MinSpeed, m_MaxSpeed 설정
            FieldInfo minSpeedField = tileType.GetField("m_MinSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo maxSpeedField = tileType.GetField("m_MaxSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            
            float speed = state switch
            {
                TileState.Normal => 1f,
                TileState.ItemLv1 => 0.5f,
                TileState.ItemLv2 => 1.5f,
                _ => 1f
            };

            if (minSpeedField != null) minSpeedField.SetValue(animatedTile, speed);
            if (maxSpeedField != null) maxSpeedField.SetValue(animatedTile, speed);

            // m_AnimationStartTime, m_AnimationStartFrame 설정
            FieldInfo startTimeField = tileType.GetField("m_AnimationStartTime", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo startFrameField = tileType.GetField("m_AnimationStartFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (startTimeField != null) startTimeField.SetValue(animatedTile, 0f);
            if (startFrameField != null) startFrameField.SetValue(animatedTile, 0);

            // m_TileColliderType 설정
            FieldInfo colliderTypeField = tileType.GetField("m_TileColliderType", BindingFlags.NonPublic | BindingFlags.Instance);
            if (colliderTypeField != null)
            {
                colliderTypeField.SetValue(animatedTile, Tile.ColliderType.Sprite);
            }

            // 참고: AnimatedTile은 TileBase를 상속받지만 color 속성이 공식적으로 지원되지 않음
            // 대신 색상별로 다른 스프라이트를 생성하여 사용 (GetSpritesForState에서 처리)
            // TileBase의 color 속성은 Tilemap 레벨에서 적용되므로 여기서는 설정하지 않음
        }

        /// <summary>
        /// 헥사곤 스프라이트 가져오기 또는 생성
        /// </summary>
        private static Sprite GetOrCreateHexagonSprite()
        {
            // 기존 스프라이트 찾기 (HexagonTexture.asset에 포함된 스프라이트)
            string texturePath = TILES_PATH + "/HexagonTexture.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            
            if (texture != null)
            {
                // 텍스처에서 스프라이트 찾기
                string[] guids = AssetDatabase.FindAssets("HexagonSprite t:Sprite", new[] { TILES_PATH });
                if (guids.Length > 0)
                {
                    string spritePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (existingSprite != null)
                    {
                        return existingSprite;
                    }
                }
            }

            // 스프라이트가 없으면 생성
            return CreateHexagonSprite();
        }

        #endregion

        #region Common Utilities

        /// <summary>
        /// 육각형 스프라이트 생성
        /// AnimatedTile 생성에도 사용되므로 public으로 공개
        /// </summary>
        public static Sprite CreateHexagonSprite()
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

        #endregion
    }
}
