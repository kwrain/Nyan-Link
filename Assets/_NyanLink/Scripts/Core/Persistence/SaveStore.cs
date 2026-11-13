using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NineMood.Core.Persistence
{
    public enum SaveReadStatus { Missing, Ready, Recovered, Corrupt, Unsupported, IoError }
    public sealed class SaveReadResult
    {
        public SaveReadStatus status;
        public SaveDocument data;
        public string message;
        public bool Usable => status==SaveReadStatus.Ready || status==SaveReadStatus.Recovered || status==SaveReadStatus.Missing;
    }
    /// <summary>진행과 영구 기록의 단일 원자 문서. 손상 원본 보존, 미래 버전 덮어쓰기 거부.</summary>
    public sealed class SaveStore
    {
        public string DirectoryPath { get; }
        public string FilePath => Path.Combine(DirectoryPath,"profile-v1.json");
        public string BackupPath => FilePath+".bak";
        public SaveStore(string directory) { DirectoryPath=Path.GetFullPath(directory); }
        public static string Hash(string text)
        {
            using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();
        }
        public static T Clone<T>(T value)
        {
            var copy=JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
            if(copy is SaveDocument doc && !doc.hasCheckpoint) doc.checkpoint=null;
            return copy;
        }
        private static string Checksum(SaveDocument doc)
        {
            string old=doc.checksum;doc.checksum="";string result=Hash(JsonUtility.ToJson(doc));doc.checksum=old;return result;
        }
        public static bool Validate(SaveDocument doc,out string error)
        {
            error="Invalid save data";
            if(doc==null || doc.schemaVersion!=1 || doc.revision<0 || doc.permanent==null || doc.pendingClears==null) return false;
            var p=doc.permanent;
            if(p.coins<0 || p.equipment==null || p.clears==null || p.rewardReceipts==null) return false;
            if(p.equipment.Any(e=>e==null || string.IsNullOrWhiteSpace(e.instanceId) || string.IsNullOrWhiteSpace(e.dataId) || e.level<1 || e.level>5 || e.equippedSlot< -1 || e.equippedSlot>2)) return false;
            if(p.equipment.Select(e=>e.instanceId).Distinct().Count()!=p.equipment.Count || p.equipment.Where(e=>e.equippedSlot>=0).GroupBy(e=>e.equippedSlot).Any(g=>g.Count()>1)) return false;
            if(p.clears.Any(e=>e==null || string.IsNullOrWhiteSpace(e.stageId) || e.bestScore<0) || p.clears.Select(e=>e.stageId).Distinct().Count()!=p.clears.Count) return false;
            if(p.rewardReceipts.Any(string.IsNullOrWhiteSpace) || p.rewardReceipts.Distinct().Count()!=p.rewardReceipts.Count) return false;
            if(doc.pendingClears.Any(e=>e==null || string.IsNullOrWhiteSpace(e.runId) || string.IsNullOrWhiteSpace(e.stageId) || e.score<0) || doc.pendingClears.Select(e=>e.runId).Distinct().Count()!=doc.pendingClears.Count) return false;
            if(doc.checkpoint!=null)
            {
                var c=doc.checkpoint;
                if(string.IsNullOrWhiteSpace(c.runId) || string.IsNullOrWhiteSpace(c.stageId) || string.IsNullOrWhiteSpace(c.rulesSignature) || string.IsNullOrWhiteSpace(c.randomState) || c.board==null || c.board.Count!=39 || c.boss==null) return false;
                if(!Enum.IsDefined(typeof(CheckpointBoundary),c.boundary) || (c.phase!=GameState.RunnerPhase && c.phase!=GameState.BossPhase) || !Enum.IsDefined(typeof(NineMood.Data.Enums.RunnerState),c.runnerState)) return false;
                if(!Finite(c.distance,c.hp,c.maxHP,c.elapsed,c.freezeRemaining,c.boostRemaining,c.actionRemaining,c.retryRemaining,c.boss.hp,c.boss.maxHP,c.boss.attackBonus,c.boss.groggyRemaining,c.boss.intervalRemaining)) return false;
                if(c.hp<=0 || c.maxHP<=0 || c.hp>c.maxHP || c.distance<0 || c.elapsed<0 || c.freezeRemaining<0 || c.boostRemaining<0 || c.actionRemaining<0 || c.retryRemaining<0 || c.score<0 || c.obstacleIndex<0 || c.obstacleIndex>3 || c.requiredColor< -1 || c.requiredColor>5) return false;
                if(c.board.Any(t=>t==null || t.y<0 || t.y>=7 || t.x<0 || t.x>=(t.y%2==0?6:5) || !Enum.IsDefined(typeof(NineMood.Data.Enums.TileColor),t.color) || !Enum.IsDefined(typeof(NineMood.Data.Enums.TileState),t.state) || !Enum.IsDefined(typeof(NineMood.Data.Enums.ItemEffectType),t.itemType))) return false;
                if(c.board.Select(t=>(t.x,t.y)).Distinct().Count()!=39 || c.board.Any(t=>(t.state==NineMood.Data.Enums.TileState.Normal)!=(t.itemType==NineMood.Data.Enums.ItemEffectType.None))) return false;
                if(c.board.Where(t=>t.state!=NineMood.Data.Enums.TileState.Normal).GroupBy(t=>t.itemType).Any(g=>g.Count()>2)) return false;
                var boss=c.boss;
                if(!Enum.IsDefined(typeof(BossNextStep),boss.nextStep) || boss.phase<0 || boss.attackBonus<0 || boss.groggyRemaining<0 || boss.intervalRemaining<0 || boss.nextSequence==null || boss.nextSequence.Any(t=>!Enum.IsDefined(typeof(NineMood.Data.Enums.TileColor),t))) return false;
                if(boss.active!=(c.phase==GameState.BossPhase) || (boss.active && (boss.hp<=0 || boss.maxHP<=0 || boss.hp>boss.maxHP || boss.nextStep==BossNextStep.None))) return false;
            }
            error=null;return true;
        }
        private static bool Finite(params float[] values) => values.All(v=>!float.IsNaN(v) && !float.IsInfinity(v));
        private SaveReadResult ReadFile(string path)
        {
            try
            {
                string json=File.ReadAllText(path);
                if(json.Length>4*1024*1024) return new SaveReadResult { status=SaveReadStatus.Corrupt,message="Save is too large" };
                var doc=JsonUtility.FromJson<SaveDocument>(json);
                if(doc!=null && !doc.hasCheckpoint) doc.checkpoint=null;
                if(doc!=null && doc.schemaVersion!=1) return new SaveReadResult { status=SaveReadStatus.Unsupported,message="Save version is unsupported; original preserved" };
                if(!Validate(doc,out var error) || string.IsNullOrEmpty(doc.checksum) || Checksum(doc)!=doc.checksum) return new SaveReadResult { status=SaveReadStatus.Corrupt,message=error ?? "Save checksum mismatch" };
                return new SaveReadResult { status=SaveReadStatus.Ready,data=doc };
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { return new SaveReadResult { status=e is UnauthorizedAccessException ? SaveReadStatus.IoError : SaveReadStatus.Corrupt,message="Cannot read save: "+e.GetType().Name }; }
        }
        public SaveReadResult Load()
        {
            if(File.Exists(FilePath))
            {
                var main=ReadFile(FilePath);
                if(main.status==SaveReadStatus.Ready || main.status==SaveReadStatus.Unsupported) return main;
                if(File.Exists(BackupPath))
                {
                    var backup=ReadFile(BackupPath);
                    if(backup.status==SaveReadStatus.Ready) { backup.status=SaveReadStatus.Recovered;backup.message="Recovered from backup; damaged original preserved";return backup; }
                }
                return main;
            }
            if(File.Exists(BackupPath))
            {
                var backup=ReadFile(BackupPath);if(backup.status==SaveReadStatus.Ready) { backup.status=SaveReadStatus.Recovered;backup.message="Recovered from backup"; }return backup;
            }
            return new SaveReadResult { status=SaveReadStatus.Missing,data=new SaveDocument() };
        }
        private static void AtomicBytes(string path,byte[] bytes)
        {
            string temp=path+".tmp-"+Guid.NewGuid().ToString("N");
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { stream.Write(bytes,0,bytes.Length);stream.Flush(true); }
            if(File.Exists(path)) File.Replace(temp,path,null);else File.Move(temp,path);
        }
        /// <summary>onPrepared는 디스크 실패 검사 지점. 예외가 나도 기존 문서는 바뀌지 않는다.</summary>
        public bool Write(SaveDocument doc,out string error,Action onPrepared=null)
        {
            error=null;
            if(doc!=null) doc.hasCheckpoint=doc.checkpoint!=null;
            if(!Validate(doc,out error)) return false;
            var current=Load();
            if(current.status==SaveReadStatus.Unsupported) { error=current.message;return false; }
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                if(File.Exists(FilePath))
                {
                    var existing=ReadFile(FilePath);
                    if(existing.status==SaveReadStatus.Ready) AtomicBytes(BackupPath,File.ReadAllBytes(FilePath));
                    else
                    {
                        string preserved=Path.Combine(DirectoryPath,"preserved");Directory.CreateDirectory(preserved);
                        string archive=Path.Combine(preserved,DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+Guid.NewGuid().ToString("N")+".json");
                        AtomicBytes(archive,File.ReadAllBytes(FilePath));
                    }
                }
                doc.schemaVersion=1;doc.revision=(current.data?.revision ?? 0)+1;doc.checksum="";doc.checksum=Checksum(doc);
                string temp=FilePath+".tmp-"+Guid.NewGuid().ToString("N");
                byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(doc,true));
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { stream.Write(bytes,0,bytes.Length);stream.Flush(true); }
                onPrepared?.Invoke();
                if(File.Exists(FilePath)) File.Replace(temp,FilePath,null);else File.Move(temp,FilePath);
                return true;
            }
            catch(Exception e) { error="Save write failed: "+e.GetType().Name;return false; }
        }
    }
}
