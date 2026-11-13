using System;
using System.Collections.Generic;
using NineMood.Data.Enums;

namespace NineMood.Core.Persistence
{
    [Serializable] public sealed class SaveDocument
    {
        public int schemaVersion=1;
        public long revision;
        public string checksum;
        public bool hasCheckpoint;
        public RunCheckpoint checkpoint;
        public PermanentData permanent=new PermanentData();
        public List<PendingClear> pendingClears=new List<PendingClear>();
    }
    [Serializable] public sealed class PermanentData
    {
        public int coins;
        public List<EquipmentRecord> equipment=new List<EquipmentRecord>();
        public List<ClearRecord> clears=new List<ClearRecord>();
        public List<string> rewardReceipts=new List<string>();
    }
    [Serializable] public sealed class EquipmentRecord
    {
        public string instanceId, dataId;
        public int level=1, equippedSlot=-1;
    }
    [Serializable] public sealed class ClearRecord
    {
        public string stageId;
        public bool cleared, firstRewardGranted;
        public int bestScore;
    }
    [Serializable] public sealed class PendingClear { public string runId,stageId; public int score; }
    public enum CheckpointBoundary { ObstacleSuccess, ObstacleRetry, BossSuccess, BossFailure, GroggyEnd }
    public enum BossNextStep { None, Groggy, Interval }
    [Serializable] public sealed class TileRecord
    {
        public int x,y;
        public TileColor color;
        public TileState state;
        public ItemEffectType itemType;
    }
    [Serializable] public sealed class BossRecord
    {
        public bool active, failureShield;
        public float hp,maxHP,attackBonus,groggyRemaining,intervalRemaining;
        public int phase;
        public BossNextStep nextStep;
        public List<TileColor> nextSequence=new List<TileColor>();
    }
    [Serializable] public sealed class RunCheckpoint
    {
        public string runId,stageId,rulesSignature,randomState;
        public CheckpointBoundary boundary;
        public GameState phase;
        public float distance,hp,maxHP,elapsed,freezeRemaining,boostRemaining,actionRemaining,retryRemaining;
        public RunnerState runnerState;
        public int obstacleIndex,score,requiredColor=-1;
        public List<TileRecord> board=new List<TileRecord>();
        public BossRecord boss=new BossRecord();
    }
}
