using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace NineMood.Data.Definitions
{
    [Serializable]
    public sealed class BossHealthPhase
    {
        [Range(0,1)] public float enterAtHP = 1;
        [Range(2,9)] public int minimumLinks = 2;
        [Min(.1f)] public float inputSeconds = 1.8f;
        [Min(.05f)] public float stepGap = .2f;
        [Min(.05f)] public float patternGap = .35f;
        public BossPatternDefinition[] patterns;
    }

    [CreateAssetMenu(menuName="NyanLink/Boss/Encounter", fileName="BossEncounter")]
    public sealed class BossDefinition : ScriptableObject
    {
        public string bossId;
        public string displayName;
        [Min(1)] public float maxHP = 120;
        [Min(0)] public float failureDamage = 20;
        [Min(.1f)] public float feverSeconds = 5;
        [Min(.1f)] public float feverRefillSeconds = .4f;
        [Min(0)] public float feverDamageMultiplier = .5f;
        public BossHealthPhase[] phases;

        public int PhaseFor(float hpRatio)
        {
            int phase = 0;
            for (int i=1; i<phases.Length; i++) if (hpRatio <= phases[i].enterAtHP) phase=i;
            return phase;
        }
        public bool Validate(out string reason)
        {
            reason="보스 체력 구간/패턴 설정을 확인하세요";
            if (string.IsNullOrWhiteSpace(bossId) || maxHP<=0 || feverSeconds<=0 || feverRefillSeconds<=0 ||
                phases==null || phases.Length==0 || phases[0]==null || phases[0].enterAtHP!=1) return false;
            for (int i=0;i<phases.Length;i++)
            {
                var p=phases[i];
                if (p==null) return false;
                if (p.minimumLinks<2 || p.minimumLinks>9 || p.inputSeconds<.1f || p.stepGap<.05f || p.patternGap<.05f ||
                    (i>0 && p.enterAtHP>=phases[i-1].enterAtHP) || p.patterns==null || p.patterns.Length==0) return false;
                foreach (var pattern in p.patterns)
                {
                    if (pattern==null || string.IsNullOrWhiteSpace(pattern.patternId) || pattern.steps==null || pattern.steps.Length==0 ||
                        !Enum.IsDefined(typeof(BossPatternExecution),pattern.execution)) return false;
                    foreach (var step in pattern.steps)
                        if (step==null || !Enum.IsDefined(typeof(NineMood.Data.Enums.TileColor),step.color) ||
                            !Enum.IsDefined(typeof(BossResponseAction),step.action) || step.minimumLinks<0 || step.minimumLinks==1 ||
                            step.minimumLinks>9 || step.inputSeconds<0 || step.damagePerLink<0) return false;
                }
            }
            reason=null; return true;
        }
        // Persist stable content, never Unity instance IDs (which vary between processes/builds).
        public string RulesKey()
        {
            var key=new StringBuilder("response-fever-v1|");
            key.Append(bossId).Append('|');
            foreach(var v in new[]{maxHP,failureDamage,feverSeconds,feverRefillSeconds,feverDamageMultiplier})
                key.Append(v.ToString("R",CultureInfo.InvariantCulture)).Append('|');
            foreach(var phase in phases)
            {
                foreach(var v in new[]{phase.enterAtHP,phase.minimumLinks,phase.inputSeconds,phase.stepGap,phase.patternGap})
                    key.Append(v.ToString("R",CultureInfo.InvariantCulture)).Append('|');
                foreach(var pattern in phase.patterns) key.Append(pattern.RulesKey).Append('|');
            }
            return key.ToString();
        }
    }
}
