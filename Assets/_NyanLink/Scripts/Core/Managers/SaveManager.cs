using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NineMood.Boss;
using NineMood.Core.Persistence;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using NineMood.Runner;
using UnityEngine;

namespace NineMood.Core.Managers
{
    [DefaultExecutionOrder(1000)]
    public class SaveManager : SceneSingleton<SaveManager>
    {
        public StageData stageData;
        public RunnerProgressSystem progressSystem;
        public ObstacleSpawner obstacleSpawner;
#if UNITY_EDITOR
        public static string TestDirectoryOverride;
#endif
        public static string StorageDirectory
        {
            get
            {
#if UNITY_EDITOR
                if(!string.IsNullOrEmpty(TestDirectoryOverride)) return TestDirectoryOverride;
#endif
                return Path.Combine(Application.persistentDataPath,"saves");
            }
        }
        public SaveStore Store => new SaveStore(StorageDirectory);
        public string CurrentRunId { get; private set; }
        public string LastMessage { get; private set; }
        public bool LastWriteSucceeded { get; private set; }
        public bool HasWriteError { get; private set; }
        public event Action<RunCheckpoint> CheckpointSaved;
        public event Action<RunCheckpoint> CheckpointRestored;
        private CheckpointBoundary? _pending;
        private bool Configured => stageData!=null && !string.IsNullOrEmpty(stageData.stageId);
        private PuzzleBoardManager Board => FindFirstObjectByType<PuzzleBoardManager>();
        private RunnerController Runner => FindFirstObjectByType<RunnerController>();
        private BossController Boss => BossController.Instance;
        private void OnEnable() => GameEvents.OnGameStateChanged+=OnState;
        private void OnDisable() { GameEvents.OnGameStateChanged-=OnState;_pending=null; }
        private void OnState(GameState state) { if(state==GameState.Briefing || state==GameState.Result) _pending=null; }
        public void RequestBoundary(CheckpointBoundary boundary)
        {
            if(Configured) _pending=boundary;
        }
        private void LateUpdate()
        {
            if(!_pending.HasValue || !Configured) return;
            var game=GameManager.Instance;
            if(game==null) return;
            GameState phase=game.CurrentState==GameState.Paused ? game.StateBeforePause : game.CurrentState;
            if(phase!=GameState.RunnerPhase && phase!=GameState.BossPhase) return;
            if(FindFirstObjectByType<TileMatcher>()?.IsResolving ?? false) return;
            var boundary=_pending.Value;_pending=null;
            var cp=Capture(boundary);
            if(!ValidateRuntime(cp,out var reason)) { Failure(reason);return; }
            if(Commit(doc=>{doc.checkpoint=cp;return true;})) CheckpointSaved?.Invoke(SaveStore.Clone(cp));
        }
        public void BeginNewRun()
        {
            _pending=null;CurrentRunId=Guid.NewGuid().ToString("N");
            if(Configured) Commit(doc=>{doc.checkpoint=null;return true;});
        }
        public void ClearCurrentCheckpoint()
        {
            _pending=null;
            if(Configured) Commit(doc=>{doc.checkpoint=null;return true;});
        }
        public void CompleteCurrentRun()
        {
            _pending=null;
            if(!Configured || string.IsNullOrEmpty(CurrentRunId)) return;
            Commit(doc=>
            {
                if(doc.pendingClears.Any(p=>p.runId==CurrentRunId) || doc.permanent.rewardReceipts.Contains(CurrentRunId)) return false;
                doc.pendingClears.Add(new PendingClear {runId=CurrentRunId,stageId=stageData.stageId,score=ScoreManager.Instance.CurrentScore});
                doc.checkpoint=null;return true;
            });
        }
        public bool TryUpdatePermanent(Action<PermanentData> update) => Commit(doc=>{update(doc.permanent);return true;});
        public bool TryApplyReward(string runId,Action<PermanentData> grant)
        {
            return Commit(doc=>
            {
                if(doc.permanent.rewardReceipts.Contains(runId)) return false;
                var pending=doc.pendingClears.Find(p=>p.runId==runId);
                if(pending==null) return false;
                grant(doc.permanent);
                var clear=doc.permanent.clears.Find(c=>c.stageId==pending.stageId);
                if(clear==null) { clear=new ClearRecord {stageId=pending.stageId};doc.permanent.clears.Add(clear); }
                clear.cleared=true;clear.bestScore=Math.Max(clear.bestScore,pending.score);
                doc.permanent.rewardReceipts.Add(runId);doc.pendingClears.Remove(pending);
                return true;
            });
        }
        private bool Commit(Func<SaveDocument,bool> update)
        {
            if(!Configured) return false;
            var read=Store.Load();
            if(!read.Usable) { Failure(read.message);return false; }
            var doc=SaveStore.Clone(read.data);
            try { if(!update(doc)) return false; }
            catch(Exception e) { Failure("Save transaction rejected: "+e.GetType().Name);return false; }
            if(!Store.Write(doc,out var error)) { Failure(error);return false; }
            HasWriteError=false;LastWriteSucceeded=true;LastMessage="Checkpoint saved";return true;
        }
        private void Failure(string message)
        {
            HasWriteError=true;LastWriteSucceeded=false;LastMessage=message;
            Debug.LogWarning("[SaveManager] "+message);
        }
        public bool HasCurrentCheckpoint()
        {
            if(!Configured) return false;
            var read=Store.Load();LastMessage=read.message;
            if(!read.Usable) return false;
            if(read.data.checkpoint==null)
            {
                try { if(Directory.Exists(StorageDirectory) && Directory.GetFiles(StorageDirectory,"checkpoint_*.json").Length>0) LastMessage="Older checkpoint preserved. Start a new run."; }
                catch(IOException) { LastMessage="Cannot inspect older checkpoints."; }
                return false;
            }
            bool valid=ValidateRuntime(read.data.checkpoint,out var error);
            if(!valid) LastMessage=error;
            return valid;
        }
        public bool TryRestoreCheckpoint()
        {
            if(!HasCurrentCheckpoint()) return false;
            var read=Store.Load();
            if(!read.Usable || !ValidateRuntime(read.data.checkpoint,out var error)) return false;
            var cp=read.data.checkpoint;
            _pending=null;CurrentRunId=cp.runId;
            GameManager.Instance.RestorePhase(cp.phase);
            progressSystem.RestoreDistance(cp.distance);
            obstacleSpawner.RestoreBoundary(cp.obstacleIndex,cp.retryRemaining);
            StaminaManager.Instance.RestoreStamina(cp.hp,cp.elapsed);
            StaminaManager.Instance.RestoreEffects(cp.freezeRemaining);
            ScoreManager.Instance.RestoreScore(cp.score);
            Runner.RestoreBoundary(cp.runnerState,cp.actionRemaining,cp.boostRemaining);
            Boss.RestoreBoundary(cp.boss);
            Board.RestoreTiles(cp.board,cp.requiredColor);
            UnityEngine.Random.state=JsonUtility.FromJson<UnityEngine.Random.State>(cp.randomState);
            LastMessage=read.status==SaveReadStatus.Recovered ? read.message : "Continued from last checkpoint";
            CheckpointRestored?.Invoke(SaveStore.Clone(cp));
            return true;
        }
        [Serializable] private sealed class Rules
        {
            public string stageId,balance,boss;
            public float speed,length,decay,decayGrowth,obstacleSpeed,obstacleDamage;
            public List<ObstacleEntry> obstacles;
        }
        private string BaseRulesSignature => SaveStore.Hash(JsonUtility.ToJson(new Rules {
            stageId=stageData.stageId,speed=stageData.runnerSpeed,length=stageData.stageLength,decay=stageData.stageDecayMultiplier,
            decayGrowth=stageData.decayIncreasePerSecond,obstacleSpeed=stageData.obstacleMoveSpeed,obstacleDamage=stageData.ActualObstacleDamage,
            obstacles=stageData.obstacles,balance=JsonUtility.ToJson(DataManager.Instance.BalanceData),boss=JsonUtility.ToJson(Boss.config) }));
        public string RulesSignature
        {
            get
            {
                var stats=ProgressionService.Stats();
                // Base loadouts retain Session06 signatures; changed stats must not resume old runs.
                return stats.maxHP==100f && stats.attack==1f ? BaseRulesSignature
                    : SaveStore.Hash(BaseRulesSignature+":"+stats.maxHP.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+stats.attack.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        public RunCheckpoint Capture(CheckpointBoundary boundary)
        {
            var stamina=StaminaManager.Instance;
            return new RunCheckpoint {runId=CurrentRunId,stageId=stageData.stageId,rulesSignature=RulesSignature,boundary=boundary,
                phase=GameManager.Instance.CurrentState==GameState.Paused ? GameManager.Instance.StateBeforePause : GameManager.Instance.CurrentState,distance=progressSystem.CurrentDistance,obstacleIndex=obstacleSpawner.NextObstacleIndex,retryRemaining=obstacleSpawner.RetryRemaining,
                hp=stamina.CurrentStamina,maxHP=stamina.MaxStamina,elapsed=stamina.ElapsedRunnerTime,score=ScoreManager.Instance.CurrentScore,
                freezeRemaining=stamina.DecayFreezeRemaining,boostRemaining=Runner.BoostTimeRemaining,runnerState=Runner.CurrentState,actionRemaining=Runner.ActionTimeRemaining,
                requiredColor=Board.RequiredMatchColor.HasValue ? (int)Board.RequiredMatchColor.Value : -1,boss=Boss.CaptureBoundary(),randomState=JsonUtility.ToJson(UnityEngine.Random.state),
                board=Board.GetAllTileOffsets().OrderBy(c=>c.y).ThenBy(c=>c.x).Select(c=> {var t=Board.GetTileAtOffset(c);return new TileRecord {x=c.x,y=c.y,color=t.Color,state=t.State,itemType=t.ItemType};}).ToList()};
        }
        [Serializable] private sealed class RandomCheck { public int s0,s1,s2,s3; }
        public bool ValidateRuntime(RunCheckpoint cp,out string error)
        {
            error="Checkpoint is incompatible; original preserved";
            if(cp==null || !Configured || cp.stageId!=stageData.stageId || cp.rulesSignature!=RulesSignature) return false;
            var temporary=new SaveDocument {checkpoint=cp};
            if(!SaveStore.Validate(temporary,out error)) return false;
            error="Checkpoint boundary is incompatible; original preserved";
            if(cp.distance>stageData.stageLength || cp.maxHP!=ProgressionService.Stats().maxHP || cp.boss.phase>Boss.config.phaseHPThresholds.Count) return false;
            var obstacles=stageData.obstacles.OrderBy(o=>o.triggerDistance).ToArray();
            if(cp.phase==GameState.RunnerPhase)
            {
                float next=cp.obstacleIndex<obstacles.Length ? obstacles[cp.obstacleIndex].triggerDistance*stageData.stageLength : stageData.stageLength;
                if(cp.distance>next+.001f || (cp.retryRemaining>0 && (cp.obstacleIndex>=obstacles.Length || Mathf.Abs(cp.distance-next)>.001f))) return false;
                if(cp.requiredColor!=(cp.obstacleIndex<obstacles.Length ? (int)obstacles[cp.obstacleIndex].requiredColor : -1)) return false;
            }
            else
            {
                if(cp.obstacleIndex!=obstacles.Length || cp.distance!=stageData.stageLength || cp.retryRemaining!=0 || cp.requiredColor!=-1 || cp.boss.maxHP!=Boss.config.maxHP) return false;
                if(cp.boss.nextStep==BossNextStep.Interval && cp.boss.nextSequence.Count!=Boss.config.timeAttackSequenceLength+cp.boss.phase*Boss.config.sequenceGrowthPerPhase) return false;
                if(cp.boss.nextStep==BossNextStep.Groggy && cp.boss.groggyRemaining<=0) return false;
            }
            if(cp.requiredColor>=0 && !cp.board.Any(a=>(int)a.color==cp.requiredColor && cp.board.Any(b=>(int)b.color==cp.requiredColor && HexOffsetUtils.IsAdjacent(new Vector3Int(a.x,a.y,0),new Vector3Int(b.x,b.y,0))))) return false;
            try {var rng=JsonUtility.FromJson<RandomCheck>(cp.randomState);if(rng==null || (rng.s0|rng.s1|rng.s2|rng.s3)==0) return false;} catch {return false;}
            error=null;return true;
        }
    }
    // 이전 형식은 변환 추정을 하지 않고 별도 파일로 보존한다.
    [Serializable] public class CheckpointData
    {
        public string stageId; public int nextObstacleIndex,savedScore;
        public float savedDistance,savedStamina,savedElapsedRunTime;
    }
}
