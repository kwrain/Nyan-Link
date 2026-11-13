using System;
using NineMood.Data.Definitions;
using UnityEngine;

namespace NineMood.Boss
{
    /// <summary>
    /// 보스 패턴 추상 베이스 — callback 방식으로 결과 반환.
    /// 각 패턴은 Run() 시작, Cancel() 강제 종료.
    /// </summary>
    public abstract class BossPatternBase : MonoBehaviour
    {
        protected BossBattleConfig Config  { get; private set; }
        protected BalanceData       Balance { get; private set; }
        protected int               PhaseIndex { get; private set; }

        private Action _onSuccess;
        private Action _onFailed;
        private bool   _running;

        public bool IsRunning => _running;

        /// <summary>패턴 시작. 이전 실행이 끝나지 않은 경우 Cancel 후 재시작.</summary>
        public void Run(BossBattleConfig config, BalanceData balance, int phaseIndex,
                        Action onSuccess, Action onFailed)
        {
            if (_running) Cancel();

            Config     = config;
            Balance    = balance;
            PhaseIndex = phaseIndex;
            _onSuccess = onSuccess;
            _onFailed  = onFailed;
            _running   = true;
            OnRun();
        }

        /// <summary>강제 종료 — 콜백 없이 상태만 초기화.</summary>
        public void Cancel()
        {
            if (!_running) return;
            _running = false;
            OnCancel();
        }

        protected void ReportSuccess()
        {
            if (!_running) return;
            _running = false;
            OnCancel();
            _onSuccess?.Invoke();
        }

        protected void ReportFailed()
        {
            if (!_running) return;
            _running = false;
            OnCancel();
            _onFailed?.Invoke();
        }

        protected abstract void OnRun();
        protected abstract void OnCancel();
    }
}
