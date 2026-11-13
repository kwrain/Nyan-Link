using NineMood.Core;
using NineMood.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>
    /// 검증3 전용 디버그 패널.
    /// 현재 GameState를 표시하고, 버튼으로 각 상태를 강제 전환한다.
    /// 프로덕션 빌드에서는 씬에서 제거할 것.
    /// </summary>
    public class GameStateDebugPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textCurrentState;
        [SerializeField] private Button btnForceStart;      // Briefing → RunnerPhase (GameReadyPanel도 닫힘)
        [SerializeField] private Button btnForceBoss;
        [SerializeField] private Button btnForceClear;
        [SerializeField] private Button btnForceGameOver;

        private void Awake()
        {
            if (btnForceStart   != null) btnForceStart.onClick.AddListener(OnForceStart);
            if (btnForceBoss    != null) btnForceBoss.onClick.AddListener(OnForceBoss);
            if (btnForceClear   != null) btnForceClear.onClick.AddListener(OnForceClear);
            if (btnForceGameOver!= null) btnForceGameOver.onClick.AddListener(OnForceGameOver);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            GameEvents.OnResultSet        += OnResultSet;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            GameEvents.OnResultSet        -= OnResultSet;
        }

        private void Start()
        {
            RefreshStateText();
        }

        private void OnGameStateChanged(GameState state) => RefreshStateText();
        private void OnResultSet(ResultType result)       => RefreshStateText();

        private void RefreshStateText()
        {
            if (textCurrentState == null) return;
            if (!GameManager.IsCreated) { textCurrentState.text = "State: (no GameManager)"; return; }

            GameState state = GameManager.Instance.CurrentState;
            string display  = state == GameState.Result
                ? $"Result / {GameManager.Instance.CurrentResult}"
                : state.ToString();
            textCurrentState.text = $"State: {display}";
        }

        // IsCreated 체크 없이 직접 호출 — 어떤 상태에서도 RunnerPhase로 강제 전환.
        // GameManager가 없으면 Instance getter가 자동으로 생성한다.
        // GameReadyPanel도 OnGameStateChanged(RunnerPhase)를 받아 자동으로 닫힌다.
        private void OnForceStart()
        {
            GameManager.Instance.StartGame();
            RefreshStateText();
        }

        private void OnForceBoss()
        {
            if (!GameManager.IsCreated) return;
            if (GameManager.Instance.CurrentState == GameState.RunnerPhase)
                GameManager.Instance.TriggerBossFight();
            else
                Debug.LogWarning("[DebugPanel] TriggerBossFight은 RunnerPhase 상태에서만 가능합니다.");
        }

        private void OnForceClear()
        {
            if (!GameManager.IsCreated) return;
            if (GameManager.Instance.CurrentState == GameState.BossPhase)
                GameManager.Instance.TriggerStageClear();
            else
                Debug.LogWarning("[DebugPanel] TriggerStageClear은 BossPhase 상태에서만 가능합니다.");
        }

        private void OnForceGameOver()
        {
            if (!GameManager.IsCreated) return;
            GameManager.Instance.TriggerGameOver();
        }

        private void OnDestroy()
        {
            if (btnForceStart   != null) btnForceStart.onClick.RemoveListener(OnForceStart);
            if (btnForceBoss    != null) btnForceBoss.onClick.RemoveListener(OnForceBoss);
            if (btnForceClear   != null) btnForceClear.onClick.RemoveListener(OnForceClear);
            if (btnForceGameOver!= null) btnForceGameOver.onClick.RemoveListener(OnForceGameOver);
        }
    }
}
