using System.Collections.Generic;
using System.Linq;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Core.Persistence;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using NineMood.Runner;
using UnityEngine;

namespace NineMood.Boss
{
    /// <summary>Asset-driven response bundles and a temporary tap-only fever board.</summary>
    public sealed class BossEncounter : MonoBehaviour
    {
        private enum State { Idle, Input, Response, Interval, PendingFever, Fever }
        private State _state;
        private BossController _owner;
        private PuzzleBoardManager _board;
        private RunnerController _runner;
        private TileInputHandler _input;
        private BossDefinition Definition => _owner.config.definition;
        private BossHealthPhase Phase => Definition.phases[_owner.PhaseIndex];
        private float _remaining, _feverDuration, _poseRemaining;
        private bool _bundleSucceeded;
        private List<TileRecord> _normalBoard;
        private readonly HashSet<Vector3Int> _consumedItems = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int,float> _refills = new Dictionary<Vector3Int,float>();
        public BossPatternDefinition Pattern { get; private set; }
        public int StepIndex { get; private set; }
        public BossResponseStep Step => Pattern!=null && StepIndex<Pattern.steps.Length ? Pattern.steps[StepIndex] : null;
        public bool AcceptingInput => _state==State.Input;
        public bool IsFever => _state==State.Fever;
        public bool IsResponding => _state==State.Input || _state==State.Response;
        public bool IsResponseAnimation => _state==State.Response;
        public bool LastResponseSucceeded { get; private set; }
        public float Remaining => _remaining;
        public float FeverDuration => _feverDuration;
        public float PoseRemaining => _poseRemaining;
        public int MinimumLinks => Step==null ? 0 : Step.minimumLinks>0 ? Step.minimumLinks : Phase.minimumLinks;
        public float InputDuration => Step!=null && Step.inputSeconds>0 ? Step.inputSeconds : Phase.inputSeconds;
        public string Feedback { get; private set; }
        public int FeverTapCount { get; private set; }

        public void Bind(BossController owner) { _owner=owner; }
        private void OnEnable() { GameEvents.OnMatchCompleted+=OnMatch; }
        private void OnDisable() { GameEvents.OnMatchCompleted-=OnMatch; Abort(); }
        private void References()
        {
            _board=FindFirstObjectByType<PuzzleBoardManager>(); _runner=FindFirstObjectByType<RunnerController>();
            _input=FindFirstObjectByType<TileInputHandler>();
        }
        public void Begin()
        {
            References(); FeverTapCount=0;
            _owner.SetEncounterPhase(Definition.PhaseFor(_owner.CurrentHP/_owner.MaxHP));
            StartBundle();
        }
        private void StartBundle()
        {
            var candidates=Phase.patterns;
            Pattern=candidates[Random.Range(0,candidates.Length)]; StepIndex=0; _bundleSucceeded=true;
            GameEvents.TriggerBossPatternStarted(BossBattleType.TimeAttack);
            StartStep();
        }
        private void StartStep()
        {
            _input?.CancelCurrentSelection();
            if (!_board.SetRequiredMatchPath(Step.color,MinimumLinks))
            { Debug.LogError("[BossEncounter] 요구 연결 경로를 만들 수 없습니다"); return; }
            Feedback=Step.cue; _remaining=InputDuration; _state=State.Input;
            GameEvents.TriggerBossPatternInstructionChanged(Step.cue);
        }
        public void Tick(float delta)
        {
            _poseRemaining=Mathf.Max(0,_poseRemaining-delta);
            if (_state==State.PendingFever) { BeginFever(_feverDuration); return; }
            if (_state==State.Fever)
            {
                // Fever is a fixed burst window; normal TimeFreeze items do not extend it.
                _remaining=Mathf.Max(0,_remaining-delta); _owner.SetEncounterFever(true,_remaining);
                foreach(var cell in _refills.Keys.ToArray())
                {
                    _refills[cell]-=delta;
                    if (_refills[cell]>0) continue;
                    _refills.Remove(cell); _board.SpawnTile(cell,TileColor.Orange,TileState.Normal);
                }
                if (_remaining<=0)
                {
                    RestoreNormalBoard(); _owner.SetEncounterFever(false,0); GameEvents.TriggerBossGroggyEnded();
                    BeginInterval(); SaveManager.Instance?.RequestBoundary(CheckpointBoundary.GroggyEnd);
                }
                return;
            }
            if (_owner.TimersFrozen && _state==State.Input) return;
            _remaining=Mathf.Max(0,_remaining-delta);
            if (_remaining>0) return;
            if (_state==State.Input) ResolveStep(false,0);
            else if (_state==State.Response)
            {
                StepIndex++;
                if (StepIndex<Pattern.steps.Length) StartStep(); else FinishBundle();
            }
            else if (_state==State.Interval) StartBundle();
        }
        private void OnMatch(int connected,int score,int removed,TileColor color)
        {
            if (_owner==null || !_owner.UsesEncounter || !AcceptingInput ||
                GameManager.Instance?.CurrentState!=GameState.BossPhase || color!=Step.color || connected<MinimumLinks || removed<=0) return;
            ResolveStep(true,connected);
        }
        private void ResolveStep(bool success,int connected)
        {
            _state=State.Response; _remaining=Phase.stepGap; _poseRemaining=.3f; LastResponseSucceeded=success;
            _board.SetRequiredMatchColor(null); _input?.CancelCurrentSelection();
            if (success)
            {
                Feedback=Step.action==BossResponseAction.Attack ? "공격 성공" : Step.action==BossResponseAction.Slide ? "회피 성공" : "방어 성공";
                _runner?.PlayBossResponse(Step.action==BossResponseAction.Attack ? RunnerState.Attack : Step.action==BossResponseAction.Slide ? RunnerState.Slide : RunnerState.Block);
                if (Step.action==BossResponseAction.Attack) _owner.ApplyEncounterAttack(connected*Step.damagePerLink);
            }
            else
            {
                _bundleSucceeded=false; Feedback="대응 실패"; _runner?.TriggerStumble();
                _owner.ApplyEncounterFailure(Definition.failureDamage);
            }
        }
        private void FinishBundle()
        {
            GameEvents.TriggerBossPatternResult(_bundleSucceeded);
            if (_bundleSucceeded && Pattern.grantsFeverOnSuccess)
            {
                _feverDuration=Definition.feverSeconds;
                _state=State.PendingFever; _remaining=0;
                // Save the normal board before its temporary fever replacement on the next frame.
                SaveManager.Instance?.RequestBoundary(CheckpointBoundary.BossSuccess);
            }
            else
            {
                BeginInterval(); SaveManager.Instance?.RequestBoundary(_bundleSucceeded ? CheckpointBoundary.BossSuccess : CheckpointBoundary.BossFailure);
            }
        }
        private void BeginInterval()
        {
            _owner.SetEncounterPhase(Definition.PhaseFor(_owner.CurrentHP/_owner.MaxHP));
            _state=State.Interval; _remaining=Phase.patternGap; Feedback="다음 공격 준비";
            _board.SetRequiredMatchColor(null);
        }
        private void BeginFever(float duration)
        {
            _input?.CancelCurrentSelection(); _board.SetRequiredMatchColor(null);
            _normalBoard=_board.GetAllTileOffsets().Select(c=> {var t=_board.GetTileAtOffset(c);return new TileRecord {
                x=c.x,y=c.y,color=t.Color,state=t.State,itemType=t.ItemType};}).ToList();
            _consumedItems.Clear(); _refills.Clear();
            foreach(var tile in _normalBoard)
            {
                var attack=_board.SpawnTile(new Vector3Int(tile.x,tile.y,0),TileColor.Orange,tile.state);
                attack.SetItemType(tile.itemType);
            }
            _board.CancelTileAnimations();
            _state=State.Fever; _remaining=duration; _feverDuration=Definition.feverSeconds; Feedback="공격 타일을 연타하세요";
            _owner.SetEncounterFever(true,_remaining); GameEvents.TriggerBossGroggyStarted(duration);
        }
        public bool TryFeverTap(Vector3Int cell)
        {
            if (!IsFever || GameManager.Instance?.CurrentState!=GameState.BossPhase || _refills.ContainsKey(cell)) return false;
            var tile=_board.GetTileAtOffset(cell);
            if (tile==null || !tile.IsActive) return false;
            float special=1;
            if (tile.State!=TileState.Normal)
            {
                _consumedItems.Add(cell);
                int level=tile.State==TileState.ItemLv2 ? 2 : 1;
                special=level==2 ? 5 : 3;
                Feedback="특수 공격 ×"+special;
                // Original item identity is consumed, never queued as a modal or TimeFreeze during fever.
            }
            else Feedback="연속 공격";
            FeverTapCount++; _poseRemaining=.18f;
            _board.RemoveTile(cell); _refills[cell]=Definition.feverRefillSeconds;
            ScoreManager.Instance?.AddScore(1,0,1,TileColor.Orange,true);
            _runner?.PlayBossResponse(RunnerState.Attack);
            _owner.ApplyEncounterAttack(Definition.feverDamageMultiplier*special);
            return true;
        }
        private void RestoreNormalBoard()
        {
            if (_normalBoard==null || _board==null) return;
            foreach(var tile in _normalBoard)
                if (_consumedItems.Contains(new Vector3Int(tile.x,tile.y,0))) { tile.state=TileState.Normal; tile.itemType=ItemEffectType.None; }
            _board.RestoreTiles(_normalBoard,-1); _normalBoard=null; _refills.Clear(); _consumedItems.Clear();
        }
        public void Abort()
        {
            RestoreNormalBoard(); _state=State.Idle; Pattern=null; _poseRemaining=0; _remaining=0;
        }
        public BossRecord Capture()
        {
            return new BossRecord {active=true,hp=_owner.CurrentHP,maxHP=_owner.MaxHP,phase=_owner.PhaseIndex,
                attackBonus=_owner.PendingAttackBonus,failureShield=_owner.HasFailureShield,
                nextStep=(_state==State.PendingFever || IsFever) ? BossNextStep.Groggy : BossNextStep.Interval,
                groggyRemaining=IsFever ? _remaining : _state==State.PendingFever ? _feverDuration : 0,
                intervalRemaining=_state==State.Interval ? _remaining : Phase.patternGap};
        }
        public List<TileRecord> CheckpointTiles() => _normalBoard.Select(t=> new TileRecord {
            x=t.x,y=t.y,color=t.color,state=_consumedItems.Contains(new Vector3Int(t.x,t.y,0)) ? TileState.Normal : t.state,
            itemType=_consumedItems.Contains(new Vector3Int(t.x,t.y,0)) ? ItemEffectType.None : t.itemType}).ToList();
        public void Restore(BossRecord record)
        {
            References();
            _state=record.nextStep==BossNextStep.Groggy ? State.PendingFever : State.Interval;
            _feverDuration=record.groggyRemaining; _remaining=record.intervalRemaining;
        }
    }
}
