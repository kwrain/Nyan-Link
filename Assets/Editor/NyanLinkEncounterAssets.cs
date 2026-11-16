using System.IO;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using UnityEditor;
using UnityEngine;

public static class NyanLinkEncounterAssets
{
    public static void Create()
    {
        const string folder="Assets/Resources/Data/BossConfigs";
        var pattern=AssetDatabase.LoadAssetAtPath<BossPatternDefinition>(folder+"/BasicResponse.asset");
        if(pattern==null)
        {
            pattern=ScriptableObject.CreateInstance<BossPatternDefinition>();
            pattern.patternId="basic-response";pattern.displayName="연속 대응";
            pattern.steps=new[]{
                new BossResponseStep {color=TileColor.Orange,action=BossResponseAction.Attack,cue="빈틈! 공격하세요"},
                new BossResponseStep {color=TileColor.Blue,action=BossResponseAction.Slide,cue="높은 공격! 슬라이딩"},
                new BossResponseStep {color=TileColor.Yellow,action=BossResponseAction.Block,cue="정면 공격! 방어하세요"}};
            AssetDatabase.CreateAsset(pattern,folder+"/BasicResponse.asset");
        }
        var boss=AssetDatabase.LoadAssetAtPath<BossDefinition>(folder+"/RatKingEncounter.asset");
        if(boss==null)
        {
            boss=ScriptableObject.CreateInstance<BossDefinition>();boss.bossId="rat-king-response-v1";boss.displayName="도시의 쥐왕";
            boss.phases=new[]{
                new BossHealthPhase {enterAtHP=1,minimumLinks=2,inputSeconds=1.8f,stepGap=.2f,patternGap=.35f,patterns=new[]{pattern}},
                new BossHealthPhase {enterAtHP=.5f,minimumLinks=3,inputSeconds=1.6f,stepGap=.15f,patternGap=.25f,patterns=new[]{pattern}},
                new BossHealthPhase {enterAtHP=.2f,minimumLinks=5,inputSeconds=1.6f,stepGap=.1f,patternGap=.2f,patterns=new[]{pattern}}};
            AssetDatabase.CreateAsset(boss,folder+"/RatKingEncounter.asset");
        }
        if(!boss.Validate(out var reason)) throw new System.Exception(reason);
        var config=AssetDatabase.LoadAssetAtPath<BossBattleConfig>(folder+"/BossBattleConfig.asset");
        config.definition=boss;EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("output/playtests/2026-11-16-boss-encounter");
        File.WriteAllText("output/playtests/2026-11-16-boss-encounter/assets.txt",boss.RulesKey());
    }
}
