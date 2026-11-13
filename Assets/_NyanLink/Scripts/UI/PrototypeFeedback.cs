using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Runner;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    public class PrototypeFeedback : MonoBehaviour
    {
        TextMeshProUGUI action;
        Image flash;
        float remaining;
        RunnerController runner;
        void Awake()
        {
            runner=FindFirstObjectByType<RunnerController>();
            var canvas=new GameObject("FeedbackCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(transform,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);
            var safe=new GameObject("SafeArea",typeof(RectTransform),typeof(PrototypeSafeArea));safe.transform.SetParent(canvas.transform,false);
            var hit=new GameObject("Hit flash",typeof(RectTransform),typeof(Image));hit.transform.SetParent(safe.transform,false);flash=hit.GetComponent<Image>();flash.raycastTarget=false;flash.color=Color.clear;
            PrototypePresentation.Place(hit.GetComponent<RectTransform>(),0,.6f,1,1);
            var label=new GameObject("Action readout",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(safe.transform,false);action=label.GetComponent<TextMeshProUGUI>();
            PrototypePresentation.Place(action.rectTransform,.1f,.635f,.9f,.67f);action.fontSize=30;action.alignment=TextAlignmentOptions.Center;action.raycastTarget=false;
        }
        void OnEnable()=>GameEvents.OnPlayerHit+=Hit;
        void OnDisable()=>GameEvents.OnPlayerHit-=Hit;
        void Hit(float amount) {remaining=.28f;}
        void Update()
        {
            var game=GameManager.Instance;
            bool playing=game!=null && (game.CurrentState==GameState.RunnerPhase || game.CurrentState==GameState.BossPhase);
            if(playing)
            {
                remaining=Mathf.Max(0,remaining-Time.deltaTime);
                action.text=runner==null || runner.CurrentState==NineMood.Data.Enums.RunnerState.Run ? "" : runner.CurrentState.ToString().ToUpperInvariant();
            }
            else if(game==null || game.CurrentState!=GameState.Paused) {action.text="";remaining=0;}
            flash.color=new Color(1,.1f,.15f,remaining/.28f*.22f);
        }
    }
}
