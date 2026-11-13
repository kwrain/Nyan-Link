using System.Collections;
using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using UnityEngine;

namespace NineMood.Boss.Patterns
{
    /// <summary>
    /// 순차 노트를 일정 시간 창 안에 매칭하는 패턴.
    /// Miss가 허용치를 초과하면 실패. 모든 노트 성공 시 성공.
    /// </summary>
    public class RhythmNotePattern : BossPatternBase
    {
        private List<TileColor> _notes = new List<TileColor>();
        private int             _noteIndex;
        private int             _missCount;
        private int             _maxMiss;
        private Coroutine       _timerRoutine;
        private float           _noteWindowSec;

        private static readonly TileColor[] Palette =
            { TileColor.Red, TileColor.Blue, TileColor.Yellow };

        protected override void OnRun()
        {
            _notes.Clear();
            _noteIndex  = 0;
            _missCount  = 0;
            _maxMiss    = Config.GetMaxMissCount(Balance) + PhaseIndex;
            // 속도가 높을수록 창이 좁음
            _noteWindowSec = Mathf.Max(0.5f, 2f / Mathf.Max(0.1f, Config.rhythmNoteSpeed));

            int noteCount = 6 + PhaseIndex * 2;
            for (int i = 0; i < noteCount; i++)
                _notes.Add(Palette[Random.Range(0, Palette.Length)]);

            GameEvents.OnMatchCompleted += OnMatchCompleted;
            _timerRoutine = StartCoroutine(NoteSequenceRoutine());
        }

        protected override void OnCancel()
        {
            GameEvents.OnMatchCompleted -= OnMatchCompleted;
            if (_timerRoutine != null) { StopCoroutine(_timerRoutine); _timerRoutine = null; }
        }

        private IEnumerator NoteSequenceRoutine()
        {
            while (_noteIndex < _notes.Count && IsRunning)
            {
                TileColor current = _notes[_noteIndex];
                GameEvents.TriggerBossPatternInstructionChanged($"노트: {current}");

                bool hit = false;
                float elapsed = 0f;
                while (elapsed < _noteWindowSec && IsRunning)
                {
                    // OnMatchCompleted가 hit을 세팅하면 즉시 다음 노트로
                    if (_pendingHit)
                    {
                        hit = true;
                        _pendingHit = false;
                        break;
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!IsRunning) yield break;

                if (!hit)
                {
                    _missCount++;
                    if (_missCount > _maxMiss)
                    {
                        ReportFailed();
                        yield break;
                    }
                }

                _noteIndex++;
                yield return new WaitForSeconds(0.2f);
            }

            if (IsRunning)
                ReportSuccess();
        }

        // 코루틴과 이벤트 핸들러 간 통신을 위한 flag
        private bool _pendingHit;

        private void OnMatchCompleted(int chainLength, int scoreGained, int tilesRemoved, TileColor color)
        {
            if (!IsRunning) return;
            if (GameManager.Instance?.CurrentState != GameState.BossPhase) return;
            if (_noteIndex >= _notes.Count) return;

            if (color == _notes[_noteIndex])
                _pendingHit = true;
            // 틀린 색 매칭은 무시 (Miss는 타임아웃으로만 처리)
        }
    }
}
