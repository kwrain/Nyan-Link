using NineMood.Core;
using NineMood.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>
    /// 게임 시작 전 대기 화면.
    ///
    /// 체크포인트 없음 : "게임 시작" 버튼 단독.
    /// 체크포인트 있음 : "이어하기" (StartGame → 자동 복원) +
    ///                   "처음부터" (RetryGame → 체크포인트 삭제 후 시작).
    ///
    /// buttonNewGame은 Inspector에서 할당하지 않으면 비활성화 상태로 유지된다.
    /// </summary>
    public class GameReadyPanel : MonoBehaviour
    {
        [Header("버튼 참조")]
        [SerializeField] private Button buttonStart;
        [SerializeField] private Button buttonNewGame;          // "처음부터" — Inspector 할당 선택

        [Header("버튼 레이블 (TMPro — Inspector 할당 선택)")]
        [SerializeField] private TextMeshProUGUI textButtonStart;

        private const string LabelStart    = "NEW GAME";
        private const string LabelContinue = "CONTINUE";

        // ── 라이프사이클 ──────────────────────────────────────────

        private TextMeshProUGUI _policy;
        private void Awake()
        {
            if(buttonNewGame==null && buttonStart!=null)
            {
                buttonNewGame=Instantiate(buttonStart,buttonStart.transform.parent);
                buttonNewGame.name="New Game";buttonNewGame.onClick.RemoveAllListeners();
                var rect=buttonNewGame.GetComponent<RectTransform>();rect.anchoredPosition+=Vector2.down*150f;
                buttonNewGame.GetComponentInChildren<TextMeshProUGUI>().text="NEW GAME";
            }
            if(buttonNewGame!=null) buttonNewGame.GetComponentInChildren<TextMeshProUGUI>().text="NEW GAME";
            var go=new GameObject("Continue policy",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(transform,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=new Vector2(.1f,.10f);r.anchorMax=new Vector2(.9f,.22f);r.offsetMin=r.offsetMax=Vector2.zero;
            _policy=go.GetComponent<TextMeshProUGUI>();_policy.fontSize=26;_policy.alignment=TextAlignmentOptions.Center;_policy.raycastTarget=false;
            RefreshButtons();
        }
        private void Start()
        {
            if (buttonStart   != null) buttonStart.onClick.AddListener(OnClickStart);
            if (buttonNewGame != null) buttonNewGame.onClick.AddListener(OnClickNewGame);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
            RefreshButtons();
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (buttonStart   != null) buttonStart.onClick.RemoveListener(OnClickStart);
            if (buttonNewGame != null) buttonNewGame.onClick.RemoveListener(OnClickNewGame);
        }

        // ── 버튼 핸들러 ──────────────────────────────────────────

        private void OnClickStart()   => GameManager.Instance?.StartGame();
        private void OnClickNewGame() => GameManager.Instance?.RetryGame();

        // ── 게임 상태 ────────────────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            if(state==GameState.Briefing) RefreshButtons();
            if (state == GameState.RunnerPhase || state==GameState.BossPhase)
                gameObject.SetActive(false);
        }

        // ── 버튼 상태 갱신 ───────────────────────────────────────

        private void RefreshButtons()
        {
            bool hasCheckpoint = SaveManager.Instance?.HasCurrentCheckpoint() ?? false;

            if(_policy!=null) _policy.text="Continue resumes the last saved boundary.\nLater actions may be rolled back.\nNew game keeps coins and equipment."+
                (string.IsNullOrEmpty(SaveManager.Instance?.LastMessage) ? "" : "\n"+SaveManager.Instance.LastMessage);
            // 시작 버튼 레이블
            if (textButtonStart != null)
                textButtonStart.text = hasCheckpoint ? LabelContinue : LabelStart;

            // "처음부터" 버튼은 체크포인트가 있을 때만 활성
            if (buttonNewGame != null)
                buttonNewGame.gameObject.SetActive(hasCheckpoint);
        }
    }
}
