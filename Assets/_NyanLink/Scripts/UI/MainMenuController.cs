using NineMood.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>
    /// 메인 메뉴 씬 컨트롤러. 최고 점수 표시, Play/Exit 버튼 처리.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textBestScore;
        [SerializeField] private Button buttonPlay;
        [SerializeField] private Button buttonExit;

        private const string BestScoreKey = "NyanLink_BestScore";

        private void Awake()
        {
            if (buttonPlay != null) buttonPlay.onClick.AddListener(OnClickPlay);
            if (buttonExit != null) buttonExit.onClick.AddListener(OnClickExit);
        }

        private void Start()
        {
            int best = PlayerPrefs.GetInt(BestScoreKey, 0);
            if (textBestScore != null)
                textBestScore.text = $"BEST: {best:N0}";
        }

        private void OnClickPlay() => SceneLoader.LoadGame();

        private void OnClickExit() => Application.Quit();

        private void OnDestroy()
        {
            if (buttonPlay != null) buttonPlay.onClick.RemoveListener(OnClickPlay);
            if (buttonExit != null) buttonExit.onClick.RemoveListener(OnClickExit);
        }
    }
}
