using System.Collections;
using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using UnityEngine;

namespace NineMood.Boss.Patterns
{
    /// <summary>
    /// 제한 시간 안에 색상별 목표 개수를 제거하는 패턴.
    /// 시간 만료 시 미달 색상이 있으면 실패.
    /// </summary>
    public class ColorQuotaPattern : BossPatternBase
    {
        private Dictionary<TileColor, int> _remaining = new Dictionary<TileColor, int>();
        private Coroutine                  _timerRoutine;

        protected override void OnRun()
        {
            _remaining.Clear();

            if (Config.colorQuotaTargets == null || Config.colorQuotaTargets.Count == 0)
            {
                // 설정 없으면 기본값
                _remaining[TileColor.Red]    = 5 + PhaseIndex;
                _remaining[TileColor.Blue]   = 5 + PhaseIndex;
                _remaining[TileColor.Yellow] = 5 + PhaseIndex;
            }
            else
            {
                foreach (var kv in Config.colorQuotaTargets)
                    _remaining[kv.Key] = Mathf.Max(1, kv.Value + PhaseIndex);
            }

            GameEvents.OnMatchCompleted += OnMatchCompleted;
            UpdateInstruction();
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
            if (!_remaining.ContainsKey(color)) return;

            _remaining[color] = Mathf.Max(0, _remaining[color] - tilesRemoved);
            if (_remaining[color] == 0)
                _remaining.Remove(color);

            if (_remaining.Count == 0)
                ReportSuccess();
            else
                UpdateInstruction();
        }

        private IEnumerator TimerRoutine()
        {
            yield return new WaitForSeconds(Config.inputTimeLimit);
            if (IsRunning) ReportFailed();
        }

        private void UpdateInstruction()
        {
            var sb = new System.Text.StringBuilder("목표: ");
            foreach (var kv in _remaining)
                sb.Append($"{kv.Key}×{kv.Value} ");
            GameEvents.TriggerBossPatternInstructionChanged(sb.ToString().TrimEnd());
        }
    }
}
