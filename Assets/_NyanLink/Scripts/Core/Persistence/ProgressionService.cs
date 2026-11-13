using System;
using System.Globalization;
using System.Linq;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using UnityEngine;

namespace NineMood.Core.Persistence
{
    public readonly struct EffectiveStats
    {
        public readonly float maxHP, attack;
        public EffectiveStats(float hp,float attack) { maxHP=hp;this.attack=attack; }
    }

    // No cached profile or scene references: each transaction reads the latest valid revision.
    public static class ProgressionService
    {
        public static SaveStore Store => new SaveStore(SaveManager.StorageDirectory);
        public static SaveReadResult Read() => Store.Load();
        public static string LastMessage { get; private set; }
        public static event Action Changed;
        public static int UpgradeCost(int level) => level>=1 && level<5 ? 100+(level-1)*50 : 0;
        public static EffectiveStats Stats(PermanentData permanent=null)
        {
            var character=DataManager.Instance.GetCharacterData("prototype-cat-01");
            float hp=character!=null ? character.maxHP : 100f, attack=character!=null ? character.attack : 1f;
            if(permanent==null) { var read=Read();if(read.Usable) permanent=read.data.permanent; }
            if(permanent!=null) foreach(var record in permanent.equipment.Where(e=>e.equippedSlot>=0))
            {
                var data=DataManager.Instance.GetEquipmentData(record.dataId);
                // Unknown legacy records remain on disk and never supply invented bonuses.
                if(data==null || (int)data.equipmentType!=record.equippedSlot) continue;
                var stat=data.GetStatAtLevel(record.level);hp+=stat.hpBonus;attack+=stat.attackBonus;
            }
            return new EffectiveStats(hp,attack);
        }
        public static bool CanChangeLoadout(SaveDocument doc) => !doc.hasCheckpoint &&
            (!GameManager.IsCreated || GameManager.Instance.CurrentState==GameState.Briefing || GameManager.Instance.CurrentState==GameState.Result);
        public static bool ChangeEquipment(string instanceId,bool equip) => Transaction(doc=>
        {
            Require(CanChangeLoadout(doc),"Finish the saved run, or choose NEW RUN SETUP first.");
            var record=doc.permanent.equipment.Find(e=>e.instanceId==instanceId);
            Require(record!=null,"Equipment was not found.");
            var data=DataManager.Instance.GetEquipmentData(record.dataId);
            Require(data!=null,"Unknown equipment preserved; cannot equip.");
            int slot=(int)data.equipmentType;
            Require(equip ? record.equippedSlot!=slot : record.equippedSlot>=0,"Equipment is already in that state.");
            if(equip) foreach(var other in doc.permanent.equipment.Where(e=>e.equippedSlot==slot)) other.equippedSlot=-1;
            record.equippedSlot=equip ? slot : -1;
        });
        public static bool Upgrade(string instanceId) => Transaction(doc=>
        {
            Require(CanChangeLoadout(doc),"Finish the saved run, or choose NEW RUN SETUP first.");
            var record=doc.permanent.equipment.Find(e=>e.instanceId==instanceId);
            Require(record!=null && DataManager.Instance.GetEquipmentData(record.dataId)!=null,"Unknown equipment preserved; cannot upgrade.");
            int cost=UpgradeCost(record.level);
            Require(cost>0,"Maximum level is 5.");Require(doc.permanent.coins>=cost,"Not enough coins.");
            doc.permanent.coins-=cost;record.level++;
        });
        // Explicit UI action; never silently discard a checkpoint when equipping.
        public static bool NewRunSetup() => Transaction(doc=>
        {
            Require(!GameManager.IsCreated || GameManager.Instance.CurrentState==GameState.Briefing || GameManager.Instance.CurrentState==GameState.Result,"Cannot change setup during play.");
            Require(doc.hasCheckpoint,"No saved run to discard.");doc.checkpoint=null;doc.hasCheckpoint=false;
        });
        static void Require(bool valid,string reason) { if(!valid) throw new InvalidOperationException(reason); }
        static bool Transaction(Action<SaveDocument> update)
        {
            var read=Read();if(!read.Usable) {LastMessage=read.message;return false;}
            var doc=SaveStore.Clone(read.data);
            try { update(doc); }
            catch(InvalidOperationException e) {LastMessage=e.Message;return false;}
            catch(OverflowException) {LastMessage="Coin capacity exceeded. Reward remains pending.";return false;}
            if(!Store.Write(doc,out var error)) {LastMessage=error;return false;}
            LastMessage="Saved";Changed?.Invoke();return true;
        }
        // The run ID fixes the roll across retries/process restarts without consuming gameplay RNG.
        public static double Roll(string runId,string stageId) => uint.Parse(SaveStore.Hash(runId+":"+stageId).Substring(0,8),NumberStyles.HexNumber,CultureInfo.InvariantCulture)/4294967296d;
        public static bool ApplyReward(string runId) => Transaction(doc=>
        {
            Require(!doc.permanent.rewardReceipts.Contains(runId),"Reward already received.");
            var pending=doc.pendingClears.Find(p=>p.runId==runId);Require(pending!=null,"No pending clear.");
            var stage=DataManager.Instance.GetStageData(pending.stageId);Require(stage!=null && stage.lootTable!=null,"Reward data unavailable; clear preserved.");
            var clear=doc.permanent.clears.Find(c=>c.stageId==pending.stageId);
            if(clear==null) {clear=new ClearRecord {stageId=pending.stageId};doc.permanent.clears.Add(clear);}
            bool first=!clear.firstRewardGranted;
            var table=stage.lootTable;
            Require(table.lootEntries!=null,"Loot table unavailable; clear preserved.");
            double total=0;
            foreach(var entry in table.lootEntries)
            {
                Require(entry!=null,"Invalid loot entry; clear preserved.");
                if(entry.itemType!=LootTableData.LootItemType.Equipment) continue;
                Require(!float.IsNaN(entry.dropChance) && !float.IsInfinity(entry.dropChance) && entry.dropChance>=0 && entry.dropChance<=1,"Invalid drop chance; clear preserved.");
                Require(DataManager.Instance.GetEquipmentData(entry.itemId)!=null,"Unknown drop data; clear preserved.");total+=entry.dropChance;
            }
            Require(total<=1.000001,"Equipment chances exceed100%; clear preserved.");
            string item=first ? table.firstGuaranteedEquipmentId : null;
            if(!first)
            {
                double roll=Roll(runId,pending.stageId), cumulative=0;
                foreach(var entry in table.lootEntries.Where(e=>e.itemType==LootTableData.LootItemType.Equipment))
                { cumulative+=entry.dropChance;if(roll<cumulative) {item=entry.itemId;break;} }
            }
            if(!string.IsNullOrEmpty(item))
            {
                Require(DataManager.Instance.GetEquipmentData(item)!=null,"Reward equipment data unavailable; clear preserved.");
                doc.permanent.equipment.Add(new EquipmentRecord {instanceId="reward-"+runId,dataId=item});
            }
            Require(!first || !string.IsNullOrEmpty(item),"First reward data unavailable; clear preserved.");
            checked {doc.permanent.coins+=100;}
            clear.cleared=true;clear.firstRewardGranted=true;clear.bestScore=Math.Max(clear.bestScore,pending.score);
            doc.permanent.rewardReceipts.Add(runId);doc.pendingClears.Remove(pending);
        });
        public static void RecoverPending()
        {
            var read=Read();if(!read.Usable) {LastMessage=read.message;return;}
            foreach(var pending in read.data.pendingClears.ToArray()) if(!ApplyReward(pending.runId)) break;
        }
        public static string RewardSummary(string runId)
        {
            var read=Read();if(!read.Usable) return "SAVE UNAVAILABLE\n"+read.message;
            if(!read.data.permanent.rewardReceipts.Contains(runId)) return "Reward pending. Return home to retry.\n"+LastMessage;
            var gear=read.data.permanent.equipment.Find(e=>e.instanceId=="reward-"+runId);
            var data=gear==null ? null : DataManager.Instance.GetEquipmentData(gear.dataId);
            return "CLEAR REWARD: +100 COINS\n"+(gear==null ? "No equipment this time" : "Equipment: "+(data!=null ? data.equipmentName : gear.dataId));
        }
    }
}
