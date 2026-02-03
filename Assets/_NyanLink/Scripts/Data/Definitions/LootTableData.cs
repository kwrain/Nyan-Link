using UnityEngine;
using System.Collections.Generic;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 보스 드롭 테이블 데이터
    /// 독립 시행 드롭 로직 사용
    /// </summary>
    [CreateAssetMenu(fileName = "LootTableData", menuName = "NyanLink/Data/Loot Table", order = 5)]
    public class LootTableData : ScriptableObject
    {
        [System.Serializable]
        public class LootEntry
        {
            [Tooltip("아이템 ID (장비 ID 또는 코인 등)")]
            public string itemId;

            [Tooltip("아이템 타입")]
            public LootItemType itemType = LootItemType.Equipment;

            [Tooltip("드롭 확률 (0~1)")]
            [Range(0f, 1f)]
            public float dropChance = 0.1f;

            [Tooltip("수량 (코인의 경우)")]
            public int quantity = 1;

            [Tooltip("최소 수량 (코인의 경우)")]
            public int minQuantity = 1;

            [Tooltip("최대 수량 (코인의 경우)")]
            public int maxQuantity = 1;
        }

        public enum LootItemType
        {
            Equipment,  // 장비
            Coin        // 코인
        }

        [Header("보스 정보")]
        [Tooltip("보스 ID")]
        public string bossId;

        [Tooltip("보스 이름")]
        public string bossName;

        [Header("드롭 테이블")]
        [Tooltip("드롭 항목 리스트 (독립 시행)")]
        public List<LootEntry> lootEntries = new List<LootEntry>();

        /// <summary>
        /// 드롭 테이블에서 전리품 획득 (독립 시행)
        /// </summary>
        public List<LootEntry> RollLoot()
        {
            List<LootEntry> droppedItems = new List<LootEntry>();

            foreach (var entry in lootEntries)
            {
                // 각 항목은 독립적으로 드롭 판정
                if (Random.Range(0f, 1f) <= entry.dropChance)
                {
                    LootEntry dropped = new LootEntry
                    {
                        itemId = entry.itemId,
                        itemType = entry.itemType,
                        quantity = entry.itemType == LootItemType.Coin
                            ? Random.Range(entry.minQuantity, entry.maxQuantity + 1)
                            : entry.quantity
                    };
                    droppedItems.Add(dropped);
                }
            }

            return droppedItems;
        }
    }
}
