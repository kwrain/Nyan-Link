using UnityEngine;
using UnityEditor;
// using NyanLink.Data.Definitions; // Phase 3 이후에서 사용 예정

namespace NyanLink.Editor
{
    /// <summary>
    /// 기본 BalanceData를 생성하는 에디터 스크립트
    /// Phase 3 이후에서 사용 예정 (현재는 비활성화)
    /// </summary>
    public static class NyanLinkBalanceDataInitializer
    {
        private const string BALANCE_DATA_PATH = "Assets/Resources/Data/BalanceData/DefaultBalanceData.asset";

        [MenuItem("NyanLink/Setup/Create Default Balance Data")]
        public static void CreateDefaultBalanceData()
        {
            EditorUtility.DisplayDialog(
                "Phase 3 기능",
                "BalanceData는 Phase 3 (특수 아이템 시스템) 이후에서 사용됩니다.\n\n" +
                "현재 Phase 2에서는 사용하지 않습니다.",
                "확인"
            );
            
            // Phase 3에서 활성화 예정
            /*
            // 이미 존재하는지 확인
            BalanceData existing = AssetDatabase.LoadAssetAtPath<BalanceData>(BALANCE_DATA_PATH);
            if (existing != null)
            {
                if (EditorUtility.DisplayDialog(
                    "BalanceData 이미 존재",
                    $"이미 {BALANCE_DATA_PATH}에 BalanceData가 존재합니다.\n덮어쓰시겠습니까?",
                    "덮어쓰기",
                    "취소"))
                {
                    SetupBalanceData(existing);
                }
                else
                {
                    return;
                }
            }
            else
            {
                // 새로 생성
                BalanceData balanceData = ScriptableObject.CreateInstance<BalanceData>();
                SetupBalanceData(balanceData);
                AssetDatabase.CreateAsset(balanceData, BALANCE_DATA_PATH);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<BalanceData>(BALANCE_DATA_PATH);
            
            Debug.Log($"기본 BalanceData가 생성되었습니다: {BALANCE_DATA_PATH}");
            */
        }

        /*
        private static void SetupBalanceData(BalanceData balanceData)
        {
            // 스태미나 시스템 기본값
            balanceData.baseStaminaDecayRate = 1f;
            balanceData.hitStaminaDamage = 10f;
            balanceData.tileRemoveRecovery = 2f;

            // 점수 시스템 기본값
            balanceData.baseTileScore = 10;
            balanceData.chainLengthMultiplier = 1.2f;
            balanceData.middleChainBonus = 50;
            balanceData.longChainBonus = 100;

            // 체인 티어 기준
            balanceData.middleChainMin = 5;
            balanceData.middleChainMax = 8;
            balanceData.longChainMin = 9;

            // 아이템 효과 수치
            balanceData.recoveryLv1Amount = 20f;
            balanceData.recoveryLv2Amount = 40f;
            balanceData.timeFreezeLv1Duration = 3f;
            balanceData.timeFreezeLv2Duration = 5f;
            balanceData.blastLv1Radius = 1;
            balanceData.blastLv2Radius = 2;
            balanceData.lineClearLv1Range = 3;
            balanceData.lineClearLv2Range = 5;
            balanceData.powerUpLv1Multiplier = 1.5f;
            balanceData.powerUpLv2Multiplier = 2f;

            // 장비 레벨업 비용 (Lv.1~20)
            balanceData.levelUpCosts = new int[20];
            for (int i = 0; i < 20; i++)
            {
                balanceData.levelUpCosts[i] = 100 * (i + 1); // 레벨 * 100
            }

            // 장비 승급
            balanceData.promotionMaterialCount = 2;
            balanceData.promotionCost = 1000;

            // 보스 전투
            balanceData.bossPatternFailureDamage = 20f;
            balanceData.bossPatternSuccessDamage = 100f;
            balanceData.groggyDamageMultiplier = 2f;

            // 진행도 시스템
            balanceData.progressPerTile = 0.1f;
            balanceData.progressPerChain = 1f;

            EditorUtility.SetDirty(balanceData);
        }
        */
    }
}
