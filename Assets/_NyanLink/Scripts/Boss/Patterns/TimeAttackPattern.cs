using System.Collections;
using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using UnityEngine;

namespace NineMood.Boss.Patterns
{
    /// <summary>
    /// 색상 시퀀스를 순서대로 매칭해야 하는 패턴.
    /// 오순서 입력 시 즉시 실패.
    /// </summary>
    public class TimeAttackPattern : BossPatternBase
    {
        private List<TileColor> _sequence = new List<TileColor>();
        private int             _nextIndex;
        private Coroutine       _timerRoutine;

        private static readonly TileColor[] Palette =
            { TileColor.Red, TileColor.Blue, TileColor.Yellow, TileColor.Purple };

        protected override void OnRun()
        {
            _sequence.Clear();
            _nextIndex = 0;

            int length = Config.timeAttackSequenceLength + PhaseIndex;
            for (int i = 0; i < length; i++)
                _sequence.Add(Palette[Random.Range(0, Palette.Length)]);

            string seq = BuildInstruction();
            GameEvents.TriggerBossPatternInstructionChanged(seq);

            GameEvents.OnMatchCompleted += OnMatchCompleted;
            _timerRoutine = StartCoroutine(TimerRoutine());
        }

        protected override void OnCancel()
        {
            GameEvents.OnMatchCompleted -= OnMatchCompleted;
            if (_timerRoutine != null) { StopCoroutine(_timerRoutine); _timerRoutine = null; }
        }

        private void OnMatchCompleted(int chainLength, int scoreGained, int tilesRemoved, TileColor color)
        {
            if (!IsRunning) return;
            if (GameManager.Instance?.CurrentState != GameState.BossPhase) return;

            TileColor expected = _sequence[_nextIndex];
            if (color != expected)
            {
                ReportFailed();
                return;
            }

            _nextIndex++;
            GameEvents.TriggerBossPatternInstructionChanged(BuildInstruction());

            if (_nextIndex >= _sequence.Count)
                ReportSuccess();
        }

        private IEnumerator TimerRoutine()
        {
            yield return new WaitForSeconds(Config.inputTimeLimit);
            if (IsRunning) ReportFailed();
        }

        private string BuildInstruction()
        {
            var sb = new System.Text.StringBuilder("순서대로 매칭: ");
            for (int i = _nextIndex; i < _sequence.Count; i++)
                sb.Append(_sequence[i]).Append(' ');
            return sb.ToString().TrimEnd();
        }
    }
}
