using UnityEngine;
using NyanLink.Data.Enums;
using System.Collections.Generic;

namespace NyanLink.Data.Definitions
{
    /// <summary>
    /// 보스 전투 설정 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "BossBattleConfig", menuName = "NyanLink/Data/Boss Battle Config", order = 6)]
    public class BossBattleConfig : ScriptableObject
    {
        [Header("보스 기본 정보")]
        [Tooltip("보스 ID")]
        public string bossId;

        [Tooltip("보스 이름")]
        public string bossName;

        [Tooltip("보스 최대 HP")]
        public float maxHP = 1000f;

        [Header("전투 타입")]
        [Tooltip("보스 전투 타입")]
        public BossBattleType battleType = BossBattleType.TimeAttack;

        [Header("패턴 설정")]
        [Tooltip("패턴 제시 시간 (초)")]
        public float patternShowDuration = 5f;

        [Tooltip("입력 제한 시간 (초)")]
        public float inputTimeLimit = 10f;

        [Tooltip("패턴 실패 시 스태미나 감소량")]
        public float failureStaminaDamage = 20f;

        [Tooltip("패턴 성공 시 보스 대미지")]
        public float successDamage = 100f;

        [Header("페이즈 설정")]
        [Tooltip("페이즈 전환 HP 비율 (0~1)")]
        public List<float> phaseHPThresholds = new List<float> { 0.5f, 0.2f };

        [Header("타입별 특수 설정")]
        [Tooltip("TimeAttack: 색상 시퀀스 길이")]
        public int timeAttackSequenceLength = 5;

        [Tooltip("RhythmNote: 노트 속도")]
        public float rhythmNoteSpeed = 1f;

        [Tooltip("ColorQuota: 색상별 목표 개수")]
        public SerializableDictionary<TileColor, int> colorQuotaTargets = new SerializableDictionary<TileColor, int>();

        [Tooltip("MemorySequence: 시퀀스 길이")]
        public int memorySequenceLength = 4;
    }

    /// <summary>
    /// Unity Inspector에서 Dictionary를 표시하기 위한 래퍼 클래스
    /// </summary>
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();
            if (keys.Count != values.Count)
            {
                Debug.LogError($"키와 값의 개수가 일치하지 않습니다. 키: {keys.Count}, 값: {values.Count}");
            }

            for (int i = 0; i < keys.Count; i++)
            {
                this[keys[i]] = values[i];
            }
        }
    }
}
