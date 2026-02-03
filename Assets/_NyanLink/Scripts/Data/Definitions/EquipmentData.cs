using UnityEngine;
using System.Collections.Generic;
using NyanLink.Data.Enums;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 장비 정보 데이터
    /// 보스 전용 장비 포함
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentData", menuName = "NyanLink/Data/Equipment", order = 4)]
    public class EquipmentData : ScriptableObject
    {
        [System.Serializable]
        public class EquipmentStat
        {
            [Tooltip("레벨")]
            public int level;

            [Tooltip("HP 증가량")]
            public float hpBonus = 0f;

            [Tooltip("공격력 증가량")]
            public float attackBonus = 0f;

            [Tooltip("회복력 증가량")]
            public float recoveryBonus = 0f;

            [Tooltip("치명타 확률 증가량")]
            [Range(0f, 1f)]
            public float criticalChanceBonus = 0f;
        }

        [Header("기본 정보")]
        [Tooltip("장비 ID")]
        public string equipmentId;

        [Tooltip("장비 이름")]
        public string equipmentName;

        [Tooltip("장비 설명")]
        [TextArea(3, 5)]
        public string description;

        [Header("장비 타입")]
        [Tooltip("장비 타입")]
        public EquipmentType equipmentType = EquipmentType.Hat;

        [Tooltip("장비 등급")]
        public EquipmentGrade grade = EquipmentGrade.Grey;

        [Header("보스 전용 장비")]
        [Tooltip("보스 전용 장비 여부")]
        public bool isBossExclusive = false;

        [Tooltip("보스 ID (보스 전용 장비인 경우)")]
        public string bossId;

        [Header("레벨별 스탯 증가량")]
        [Tooltip("레벨별 스탯 증가 테이블 (Lv.1~20)")]
        public List<EquipmentStat> levelStats = new List<EquipmentStat>();

        [Header("승급 정보")]
        [Tooltip("승급에 필요한 재료 장비 ID (2개)")]
        public List<string> promotionMaterialIds = new List<string>();

        [Tooltip("승급 후 등급")]
        public EquipmentGrade promotionGrade = EquipmentGrade.Green;

        /// <summary>
        /// 특정 레벨의 스탯 보너스 가져오기
        /// </summary>
        public EquipmentStat GetStatAtLevel(int level)
        {
            if (levelStats == null || levelStats.Count == 0)
            {
                return new EquipmentStat { level = level };
            }

            // 레벨에 맞는 스탯 찾기 (없으면 가장 가까운 레벨)
            EquipmentStat result = null;
            foreach (var stat in levelStats)
            {
                if (stat.level == level)
                {
                    return stat;
                }
                if (stat.level < level)
                {
                    result = stat;
                }
            }

            return result ?? levelStats[0];
        }

        /// <summary>
        /// 최대 레벨 반환
        /// </summary>
        public int GetMaxLevel()
        {
            if (levelStats == null || levelStats.Count == 0)
            {
                return 20; // 기본 최대 레벨
            }

            int maxLevel = 0;
            foreach (var stat in levelStats)
            {
                if (stat.level > maxLevel)
                {
                    maxLevel = stat.level;
                }
            }

            return maxLevel;
        }
    }
}
