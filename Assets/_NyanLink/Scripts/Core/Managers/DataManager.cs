using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using NyanLink.Data.Definitions;
using NyanLink.Core;

namespace NyanLink.Core.Managers
{
    /// <summary>
    /// ScriptableObject 로드 및 관리 매니저
    /// </summary>
    public class DataManager : Singleton<DataManager>
    {
        [Header("데이터 리소스 경로")]
        [Tooltip("스테이지 데이터 리소스 경로 (폴더)")]
        public string stageDataFolderPath = "Data/Stages";

        [Tooltip("캐릭터 데이터 리소스 경로 (폴더)")]
        public string characterDataFolderPath = "Data/Characters";

        [Tooltip("장비 데이터 리소스 경로 (폴더)")]
        public string equipmentDataFolderPath = "Data/Equipment";

        [Tooltip("그리드 쉐이프 데이터 리소스 경로 (폴더)")]
        public string gridShapeDataFolderPath = "Data/GridShapes";

        [Tooltip("보스 전투 설정 데이터 리소스 경로 (폴더)")]
        public string bossConfigFolderPath = "Data/BossConfigs";

        [Tooltip("전리품 테이블 데이터 리소스 경로 (폴더)")]
        public string lootTableFolderPath = "Data/LootTables";

        [Header("로드된 데이터 (읽기 전용)")]
        // BalanceData는 Phase 3 이후에서 사용 예정
        // private BalanceData _balanceData;
        // public BalanceData BalanceData => _balanceData;

        private Dictionary<string, StageData> _stageDataDict = new Dictionary<string, StageData>();
        private Dictionary<string, CharacterData> _characterDataDict = new Dictionary<string, CharacterData>();
        private Dictionary<string, EquipmentData> _equipmentDataDict = new Dictionary<string, EquipmentData>();
        private Dictionary<string, GridShapeData> _gridShapeDataDict = new Dictionary<string, GridShapeData>();
        private Dictionary<string, BossBattleConfig> _bossConfigDict = new Dictionary<string, BossBattleConfig>();
        private Dictionary<string, LootTableData> _lootTableDict = new Dictionary<string, LootTableData>();

        private bool _isInitialized = false;

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public override async Task Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            await LoadAllDataAsync();
            _isInitialized = true;
        }

        /// <summary>
        /// 모든 데이터 로드 (비동기)
        /// </summary>
        private async Task LoadAllDataAsync()
        {
            // BalanceData는 Phase 3 이후에서 사용 예정
            // _balanceData = Resources.Load<BalanceData>(balanceDataPath);
            // if (_balanceData == null)
            // {
            //     Debug.LogWarning($"BalanceData를 찾을 수 없습니다: {balanceDataPath}");
            // }

            // 스테이지 데이터 로드
            LoadDataFromFolder<StageData>(stageDataFolderPath, _stageDataDict);

            // 캐릭터 데이터 로드
            LoadDataFromFolder<CharacterData>(characterDataFolderPath, _characterDataDict);

            // 장비 데이터 로드
            LoadDataFromFolder<EquipmentData>(equipmentDataFolderPath, _equipmentDataDict);

            // 그리드 쉐이프 데이터 로드
            LoadDataFromFolder<GridShapeData>(gridShapeDataFolderPath, _gridShapeDataDict);

            // 보스 전투 설정 데이터 로드
            LoadDataFromFolder<BossBattleConfig>(bossConfigFolderPath, _bossConfigDict);

            // 전리품 테이블 데이터 로드
            LoadDataFromFolder<LootTableData>(lootTableFolderPath, _lootTableDict);

            Debug.Log($"DataManager 초기화 완료. 로드된 데이터: " +
                      $"Stages: {_stageDataDict.Count}, " +
                      $"Characters: {_characterDataDict.Count}, " +
                      $"Equipment: {_equipmentDataDict.Count}, " +
                      $"GridShapes: {_gridShapeDataDict.Count}, " +
                      $"BossConfigs: {_bossConfigDict.Count}, " +
                      $"LootTables: {_lootTableDict.Count}");

            await Task.CompletedTask;
        }

        /// <summary>
        /// 폴더에서 데이터 로드
        /// </summary>
        private void LoadDataFromFolder<T>(string folderPath, Dictionary<string, T> dict) where T : ScriptableObject
        {
            T[] dataArray = Resources.LoadAll<T>(folderPath);
            dict.Clear();

            foreach (T data in dataArray)
            {
                if (data != null)
                {
                    // ID 기반으로 딕셔너리에 추가
                    string id = GetDataId(data);
                    if (!string.IsNullOrEmpty(id))
                    {
                        dict[id] = data;
                    }
                    else
                    {
                        Debug.LogWarning($"{typeof(T).Name}의 ID가 없습니다: {data.name}");
                    }
                }
            }
        }

        /// <summary>
        /// 데이터에서 ID 추출
        /// </summary>
        private string GetDataId(ScriptableObject data)
        {
            return data switch
            {
                StageData stage => stage.stageId,
                CharacterData character => character.characterId,
                EquipmentData equipment => equipment.equipmentId,
                GridShapeData gridShape => gridShape.name,
                BossBattleConfig bossConfig => bossConfig.bossId,
                LootTableData lootTable => lootTable.bossId,
                _ => data.name
            };
        }

        /// <summary>
        /// 스테이지 데이터 가져오기
        /// </summary>
        public StageData GetStageData(string stageId)
        {
            return _stageDataDict.TryGetValue(stageId, out var data) ? data : null;
        }

        /// <summary>
        /// 캐릭터 데이터 가져오기
        /// </summary>
        public CharacterData GetCharacterData(string characterId)
        {
            return _characterDataDict.TryGetValue(characterId, out var data) ? data : null;
        }

        /// <summary>
        /// 장비 데이터 가져오기
        /// </summary>
        public EquipmentData GetEquipmentData(string equipmentId)
        {
            return _equipmentDataDict.TryGetValue(equipmentId, out var data) ? data : null;
        }

        /// <summary>
        /// 그리드 쉐이프 데이터 가져오기
        /// </summary>
        public GridShapeData GetGridShapeData(string shapeName)
        {
            return _gridShapeDataDict.TryGetValue(shapeName, out var data) ? data : null;
        }

        /// <summary>
        /// 보스 전투 설정 가져오기
        /// </summary>
        public BossBattleConfig GetBossConfig(string bossId)
        {
            return _bossConfigDict.TryGetValue(bossId, out var data) ? data : null;
        }

        /// <summary>
        /// 전리품 테이블 가져오기
        /// </summary>
        public LootTableData GetLootTable(string bossId)
        {
            return _lootTableDict.TryGetValue(bossId, out var data) ? data : null;
        }
    }
}
