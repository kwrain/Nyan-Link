using NineMood.Core;
using NineMood.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    public class StageClearPanel : MonoBehaviour
    {
        [Header("텍스트 참조")]
        [SerializeField] private TextMeshProUGUI textScore;
        [SerializeField] private TextMeshProUGUI textBestScore;

        [Header("버튼 참조")]
        [SerializeField] private Button buttonContinue;
        [SerializeField] private Button buttonMainMenu;

        private void Awake()
        {
            // 버튼 리스너는 Start()에서 등록 — NyanLinkUISetup이 Awake() 이후에 SerializedField를 할당하므로
            GameEvents.OnResultSet        += OnResultSet;
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (buttonContinue != null)
            {
                buttonContinue.onClick.AddListener(OnClickContinue);
                buttonContinue.GetComponentInChildren<TextMeshProUGUI>().text="RETRY";
            }
            if (buttonMainMenu != null) buttonMainMenu.onClick.AddListener(OnClickMainMenu);
        }

        // Result(Win) 진입 시 패널 활성화
        private void OnResultSet(ResultType result)
        {
            if (result == ResultType.Win)
            {
                if(SaveManager.Instance?.stageData!=null) NineMood.Core.Persistence.ProgressionService.RecoverPending();
                RefreshUI();
                gameObject.SetActive(true);
            }
        }

        // 게임 재시작 시 패널 닫기 — RetryGame()은 Briefing을 거치지 않고 RunnerPhase로 직행하므로 둘 다 처리
        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Briefing || state == GameState.RunnerPhase)
                gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            if (ScoreManager.Instance == null) return;
            if (textScore != null) textScore.text = ScoreManager.Instance.CurrentScore.ToString("N0");
            if (textBestScore != null) textBestScore.text = ScoreManager.Instance.BestScore.ToString("N0");
        }

        private void OnClickContinue() => GameManager.Instance.RetryGame();
        private void OnClickMainMenu() => SceneLoader.LoadMainMenu();

        private void OnDestroy()
        {
            GameEvents.OnResultSet        -= OnResultSet;
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
            if (buttonContinue != null) buttonContinue.onClick.RemoveListener(OnClickContinue);
            if (buttonMainMenu != null) buttonMainMenu.onClick.RemoveListener(OnClickMainMenu);
        }
    }
}
