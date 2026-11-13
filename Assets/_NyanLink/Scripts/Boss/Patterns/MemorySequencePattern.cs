using System.Collections;
using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using UnityEngine;

namespace NineMood.Boss.Patterns
{
    /// <summary>
    /// 색상 시퀀스를 보여준 뒤 숨기고, 순서대로 재현하는 패턴.
    /// 오입력 즉시 실패.
    /// </summary>
    public class MemorySequencePattern : BossPatternBase
    {
        private List<TileColor> _sequence = new List<TileColor>();
        private int             _inputIndex;
        private bool            _inputPhase;
        private Coroutine       _showRoutine;

        private static readonly TileColor[] Palette =
            { TileColor.Red, TileColor.Blue, TileColor.Yellow, TileColor.Purple };

        protected override void OnRun()
        {
            _sequence.Clear();
            _inputIndex = 0;
            _inputPhase = false;

            int length = Config.memorySequenceLength + PhaseIndex;
            for (int i = 0; i < length; i++)
                _sequence.Add(Palette[Random.Range(0, Palette.Length)]);

            _showRoutine = StartCoroutine(ShowThenHideRoutine());
        }

        protected override void OnCancel()
        {
            GameEvents.OnMatchCompleted -= OnMatchCompleted;
            if (_showRoutine != null) { StopCoroutine(_showRoutine); _showRoutine = null; }
        }

        private IEnumerator ShowThenHideRoutine()
        {
            // 시퀀스 공개
            GameEvents.TriggerBossPatternInstructionChanged(BuildSequenceString());
            yield return new WaitForSeconds(Config.patternShowDuration);

            if (!IsRunning) yield break;

            // 시퀀스 숨김, 입력 대기
            _inputPhase = true;
            GameEvents.TriggerBossPatternInstructionChanged("기억한 순서를 입력하세요!");
            GameEvents.OnMatchCompleted += OnMatchCompleted;

            yield return new WaitForSeconds(Config.inputTimeLimit);
            if (IsRunning) ReportFailed();
        }

        private void OnMatchCompleted(int chainLength, int scoreGained, int tilesRemoved, TileColor color)
        {
            if (!IsRunning || !_inputPhase) return;
            if (GameManager.Instance?.CurrentState != GameState.BossPhase) return;

            TileColor expected = _sequence[_inputIndex];
            if (color != expected)
            {
                ReportFailed();
                return;
            }

            _inputIndex++;
            if (_inputIndex >= _sequence.Count)
                ReportSuccess();
        }

        private string BuildSequenceString()
        {
            var sb = new System.Text.StringBuilder("기억하세요: ");
            foreach (var c in _sequence)
                sb.Append(c).Append(' ');
            return sb.ToString().TrimEnd();
        }
    }
}
