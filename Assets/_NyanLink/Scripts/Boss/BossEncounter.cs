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
        private enum State { Idle, Preview, Input, Response, Interval, PendingFever, Fever }
        private State _state;
        private BossController _owner;
        private PuzzleBoardManager _board;
        private RunnerController _runner;
        private TileInputHandler _input;
        private BossDefinition Definition => _owner.config.definition;
        private BossHealthPhase Phase => Definition.phases[_owner.PhaseIndex];
        private float _remaining, _feverDuration, _poseRemaining;
        private bool _bundleSucceeded;
        private int _progress, _misses;
        private readonly Dictionary<TileColor,int> _quotas = new Dictionary<TileColor,int>();
        private BossPatternExecution Mode => Pattern.execution;
        public bool HiddenSequence => Pattern != null && Mode == BossPatternExecution.MemorySequence && _state != State.Preview;
        public string PatternGuide
        {
            get
            {
                if (Pattern == null) return "";
                if (_state == State.Preview || Mode == BossPatternExecution.TimeAttack)
                    return string.Join(" → ", Pattern.steps.Select(s => ColorName(s.color)));
                if (Mode == BossPatternExecution.MemorySequence) return "기억한 순서대로 연결하세요";
                if (Mode == BossPatternExecution.ColorQuota) return string.Join(" · ", _quotas.Select(q => ColorName(q.Key)+" "+q.Value));
                if (Mode == BossPatternExecution.ChargeBreak) return $"주황 누적 {_progress} / 8 · 초과 연결 추가 피해";
                if (Mode == BossPatternExecution.ForbiddenColor) return $"{ColorName(Step.color)} 금지 · 다른 색 {_progress} / 10";
                if (Mode == BossPatternExecution.WeaknessChoice) return "주황 5개 공격 / 파랑 3개 회피";
                if (Mode == BossPatternExecution.ExactLinks) return $"{ColorName(Step.color)} 정확히 {MinimumLinks}개";
                if (Mode == BossPatternExecution.RhythmNote) return $"{ColorName(Step.color)} · 1.0초에 연결 · 실수 {_misses}/2";
                return Step?.cue ?? "";
            }
        }
        private static string ColorName(TileColor c) => new[]{"빨강","파랑","노랑","보라","주황","청록"}[(int)c];
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
            _progress=0; _misses=0; _quotas.Clear();
            if (Mode==BossPatternExecution.ColorQuota) foreach(var step in Pattern.steps) _quotas[step.color]=5;
            GameEvents.TriggerBossPatternStarted(BossBattleType.TimeAttack);
            StartStep();
            if(Mode==BossPatternExecution.MemorySequence) { _state=State.Preview; _remaining=3; _board.SetRequiredMatchColor(null); }
        }
        private void StartStep()
        {
            _input?.CancelCurrentSelection();
            if (!_board.SetRequiredMatchPath(Step.color,MinimumLinks))
            { Debug.LogError("[BossEncounter] 요구 연결 경로를 만들 수 없습니다"); return; }
            Feedback=Step.cue; _remaining=InputDuration; _state=State.Input;
            if(Mode==BossPatternExecution.Pursuit) _remaining=Mathf.Max(.8f,InputDuration-StepIndex*.25f);
            if(Mode==BossPatternExecution.RhythmNote) _remaining=2;
            if(Mode==BossPatternExecution.WeaknessChoice) _board.SetRequiredMatchPath(TileColor.Blue,3);
            if(Mode==BossPatternExecution.ForbiddenColor) _board.SetRequiredMatchPath(Step.color==TileColor.Blue?TileColor.Yellow:TileColor.Blue,2);
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
            if (_owner.TimersFrozen && (_state==State.Input || _state==State.Preview)) return;
            _remaining=Mathf.Max(0,_remaining-delta);
            if (_remaining>0) return;
            if (_state==State.Preview) StartStep();
            else if (_state==State.Input) {
                if(Mode==BossPatternExecution.RhythmNote && ++_misses<=2) ResolveStep(true,0);
                else if(Mode==BossPatternExecution.SequentialResponse) ResolveStep(false,0);
                else FailBundle();
            }
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
                GameManager.Instance?.CurrentState!=GameState.BossPhase || connected<2 || removed<=0) return;
            switch(Mode)
            {
                case BossPatternExecution.ColorQuota:
                    if(_quotas.ContainsKey(color)) _quotas[color]=Mathf.Max(0,_quotas[color]-connected);
                    if(_quotas.Values.All(v=>v==0)) CompleteBundle(0);
                    else _board.SetRequiredMatchPath(_quotas.First(q=>q.Value>0).Key,2);
                    return;
                case BossPatternExecution.ChargeBreak:
                    if(color!=TileColor.Orange) return;
                    _progress+=connected;
                    if(_progress>=8) CompleteBundle(_progress);
                    return;
                case BossPatternExecution.ForbiddenColor:
                    if(color==Step.color) { FailBundle(); return; }
                    _progress+=connected; if(_progress>=10) CompleteBundle(0);
                    return;
                case BossPatternExecution.WeaknessChoice:
                    if(color==TileColor.Orange && connected>=5) CompleteBundle(connected);
                    else if(color==TileColor.Blue && connected>=3) CompleteBundle(0);
                    return;
                case BossPatternExecution.ExactLinks:
                    if(color!=Step.color || connected!=MinimumLinks) { FailBundle(); return; }
                    break;
                case BossPatternExecution.TimeAttack:
                case BossPatternExecution.MemorySequence:
                    if(color!=Step.color) { FailBundle(); return; }
                    if(connected<MinimumLinks) return;
                    break;
                case BossPatternExecution.RhythmNote:
                    if(color!=Step.color || Mathf.Abs(_remaining-1)>.4f) {
                        if(++_misses>2) FailBundle(); else ResolveStep(true,0);
                        return;
                    }
                    Feedback=Mathf.Abs(_remaining-1)<=.15f ? "Perfect" : "Good";
                    var grade=Feedback; ResolveStep(true,0); Feedback=grade; return;
                default:
                    if(color!=Step.color || connected<MinimumLinks) return;
                    break;
            }
            ResolveStep(true,connected);
        }
        private void FailBundle()
        {
            ResolveStep(false,0);
            StepIndex=Pattern.steps.Length-1;
        }
        private void CompleteBundle(int damage)
        {
            ResolveStep(true,0);
            StepIndex=Pattern.steps.Length-1;
            if(damage>0) _owner.ApplyEncounterAttack(damage);
        }
        private void ResolveStep(bool success,int connected)
        {
            _state=State.Response; _remaining=Phase.stepGap; _poseRemaining=.3f; LastResponseSucceeded=success;
            _board.SetRequiredMatchColor(null); _input?.CancelCurrentSelection();
            if (success)
            {
                Feedback=Step.action==BossResponseAction.Attack ? "공격 성공" : Step.action==BossResponseAction.Slide ? "회피 성공" : "방어 성공";
                _runner?.PlayBossResponse(Step.action==BossResponseAction.Attack ? RunnerState.Attack : Step.action==BossResponseAction.Slide ? RunnerState.Slide : RunnerState.Block);
                if (connected>0 && Step.action==BossResponseAction.Attack) _owner.ApplyEncounterAttack(connected*Step.damagePerLink);
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
