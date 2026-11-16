using System;
using UnityEngine;
using NineMood.Data.Enums;

namespace NineMood.Data.Definitions
{
    public enum BossPatternExecution { SequentialResponse, TimeAttack, RhythmNote, ColorQuota, MemorySequence, ChargeBreak, ForbiddenColor, WeaknessChoice, ExactLinks, Pursuit }
    public enum BossResponseAction { Attack, Slide, Block }

    [Serializable]
    public sealed class BossResponseStep
    {
        public TileColor color;
        public BossResponseAction action;
        public string cue;
        [Tooltip("0이면 보스 체력 구간의 요구 수량 사용")]
        [Range(0, 9)] public int minimumLinks;
        [Tooltip("0이면 보스 체력 구간의 입력 시간 사용")]
        [Min(0)] public float inputSeconds;
        [Min(0)] public float damagePerLink = 1;
    }

    [CreateAssetMenu(menuName="NyanLink/Boss/Pattern", fileName="BossPattern")]
    public sealed class BossPatternDefinition : ScriptableObject
    {
        public string patternId;
        public string displayName;
        public BossPatternExecution execution;
        public BossResponseStep[] steps;
        public bool grantsFeverOnSuccess = true;
        public string RulesKey => patternId + ":" + (int)execution + ":" + grantsFeverOnSuccess + ":" +
            string.Join(";", Array.ConvertAll(steps, s => JsonUtility.ToJson(s)));
    }
}
