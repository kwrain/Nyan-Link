using System.Collections.Generic;
using System.Text;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using UnityEngine;

namespace NineMood.Boss.Patterns
{
    /// <summary>전체 순서를 공개한다. 다음 요구 색 보정 및 독립 제한 시간.</summary>
    public class TimeAttackPattern : BossPatternBase
    {
        private readonly List<TileColor> _sequence = new List<TileColor>();
        public IReadOnlyList<TileColor> Sequence => _sequence.AsReadOnly();
        public int NextIndex { get; private set; }
        public float RemainingTime { get; private set; }
        public TileColor? RequiredColor => IsRunning && NextIndex < _sequence.Count ? _sequence[NextIndex] : (TileColor?)null;
        private List<TileColor> _prepared;
        public void PrepareSequence(System.Collections.Generic.IEnumerable<TileColor> sequence) => _prepared=sequence==null ? null : new List<TileColor>(sequence);
        public static List<TileColor> CreateSequence(NineMood.Data.Definitions.BossBattleConfig config,int phase)
        {
            var sequence=new List<TileColor>();
            int length=config.timeAttackSequenceLength+phase*config.sequenceGrowthPerPhase;
            for(int i=0;i<length;i++) sequence.Add((TileColor)Random.Range(0,6));
            return sequence;
        }
        protected override void OnRun()
        {
            _sequence.Clear(); NextIndex = 0;
            _sequence.AddRange(_prepared ?? CreateSequence(Config,PhaseIndex));
            _prepared=null;
            RemainingTime = Mathf.Max(Config.minimumTimeLimit,Config.inputTimeLimit - PhaseIndex * Config.timeReductionPerPhase);
            GameEvents.OnMatchCompleted += OnMatch;
            PrepareNext();
        }
        protected override void OnCancel() => GameEvents.OnMatchCompleted -= OnMatch;
        private void Update()
        {
            if (!IsRunning || GameManager.Instance?.CurrentState != GameState.BossPhase ||
                (BossController.Instance?.TimersFrozen ?? false)) return;
            RemainingTime = Mathf.Max(0f,RemainingTime - Time.deltaTime);
            if (RemainingTime <= 0f) ReportFailed();
        }
        private void OnMatch(int connected,int score,int removed,TileColor color)
        {
            if (!IsRunning || connected < 2 || GameManager.Instance?.CurrentState != GameState.BossPhase) return;
            if (color != _sequence[NextIndex]) { ReportFailed(); return; }
            NextIndex++;
            if (NextIndex == _sequence.Count) ReportSuccess(); else PrepareNext();
        }
        private void PrepareNext()
        {
            var board = FindFirstObjectByType<PuzzleBoardManager>();
            if (board != null && !board.SetRequiredMatchColor(RequiredColor) && board.IsInitialized)
                Debug.LogWarning("[TimeAttack] 요구 색 보정 불가: 보호 타일 유지, 다음 리필에서 재검사");
            var text = new StringBuilder("MATCH  ");
            for (int i = 0; i < _sequence.Count; i++)
            {
                if (i < NextIndex) text.Append("OK ");
                else text.Append(i == NextIndex ? "[" + _sequence[i] + "] " : _sequence[i] + " ");
            }
            GameEvents.TriggerBossPatternInstructionChanged(text.ToString());
        }
    }
}
