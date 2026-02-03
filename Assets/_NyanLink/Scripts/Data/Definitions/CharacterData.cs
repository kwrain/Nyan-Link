using UnityEngine;
using NyanLink.Data.Enums;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 캐릭터 기본 스탯 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterData", menuName = "NyanLink/Data/Character", order = 3)]
    public class CharacterData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("캐릭터 ID")]
        public string characterId;

        [Tooltip("캐릭터 이름")]
        public string characterName;

        [Tooltip("캐릭터 설명")]
        [TextArea(3, 5)]
        public string description;

        [Header("기본 스탯")]
        [Tooltip("최대 HP")]
        public float maxHP = 100f;

        [Tooltip("공격력")]
        public float attack = 10f;

        [Tooltip("회복력 (타일 제거 시 회복량 배율)")]
        public float recovery = 1f;

        [Tooltip("치명타 확률 (0~1)")]
        [Range(0f, 1f)]
        public float criticalChance = 0.1f;

        [Tooltip("치명타 배율")]
        public float criticalMultiplier = 2f;

        [Header("승급 정보 (후순위 기능)")]
        [Tooltip("승급에 필요한 조각 개수")]
        public int promotionPieceCount = 100;

        [Tooltip("승급 후 등급")]
        public EquipmentGrade promotionGrade = EquipmentGrade.Green;
    }
}
