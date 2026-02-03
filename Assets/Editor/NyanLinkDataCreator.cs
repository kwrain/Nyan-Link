using UnityEngine;
using UnityEditor;
using NyanLink.Data.Definitions;

namespace NyanLink.Editor
{
    /// <summary>
    /// NyanLink 데이터 생성 에디터 메뉴
    /// </summary>
    public static class NyanLinkDataCreator
    {
        private const string MENU_BASE = "Assets/Create/NyanLink/";

        [MenuItem(MENU_BASE + "Data/Grid Shape")]
        public static void CreateGridShapeData()
        {
            CreateScriptableObject<GridShapeData>("GridShapeData");
        }

        [MenuItem(MENU_BASE + "Data/Tile Effect")]
        public static void CreateTileEffectData()
        {
            CreateScriptableObject<TileEffectData>("TileEffectData");
        }

        [MenuItem(MENU_BASE + "Data/Character")]
        public static void CreateCharacterData()
        {
            CreateScriptableObject<CharacterData>("CharacterData");
        }

        [MenuItem(MENU_BASE + "Data/Equipment")]
        public static void CreateEquipmentData()
        {
            CreateScriptableObject<EquipmentData>("EquipmentData");
        }

        [MenuItem(MENU_BASE + "Data/Loot Table")]
        public static void CreateLootTableData()
        {
            CreateScriptableObject<LootTableData>("LootTableData");
        }

        [MenuItem(MENU_BASE + "Data/Boss Battle Config")]
        public static void CreateBossBattleConfig()
        {
            CreateScriptableObject<BossBattleConfig>("BossBattleConfig");
        }

        [MenuItem(MENU_BASE + "Data/Stage")]
        public static void CreateStageData()
        {
            CreateScriptableObject<StageData>("StageData");
        }

        // BalanceData는 Phase 3 이후에서 사용 예정
        /*
        [MenuItem(MENU_BASE + "Data/Balance")]
        public static void CreateBalanceData()
        {
            CreateScriptableObject<BalanceData>("BalanceData");
        }
        */

        /// <summary>
        /// ScriptableObject 생성 헬퍼 메서드
        /// </summary>
        private static void CreateScriptableObject<T>(string defaultName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (!System.IO.Directory.Exists(path))
            {
                path = System.IO.Path.GetDirectoryName(path);
            }

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(
                System.IO.Path.Combine(path, $"{defaultName}.asset")
            );

            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }
}
