using System;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>게임 Pause와 구분된 선택 상태. Pause를 중첩해도 같은 선택으로 돌아온다.</summary>
    public class ColorSelectionPanel : MonoBehaviour
    {
        public Button[] ColorButtons { get; private set; }
        public bool IsOpen => _overlay != null && _overlay.activeSelf;
        private GameObject _overlay;
        private Action<TileColor> _choose;
        private void Awake()
        {
            var root=new GameObject("ColorSelectionCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=400;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1080,1920);
            _overlay=new GameObject("Color selection",typeof(RectTransform),typeof(Image)); _overlay.transform.SetParent(root.transform,false);
            var full=_overlay.GetComponent<RectTransform>(); full.anchorMin=Vector2.zero; full.anchorMax=Vector2.one; full.offsetMin=full.offsetMax=Vector2.zero;
            _overlay.GetComponent<Image>().color=new Color(.03f,.05f,.1f,.96f);
            Text(_overlay.transform,"CHOOSE A COLOR",new Vector2(.5f,.75f),new Vector2(900,100),54);
            Text(_overlay.transform,"Game and board are paused",new Vector2(.5f,.69f),new Vector2(900,60),28);
            ColorButtons=new Button[6];
            var colors=new[]{Color.red,Color.blue,Color.yellow,new Color(.65f,.15f,.85f),new Color(1f,.45f,0f),Color.cyan};
            for(int i=0;i<6;i++)
            {
                var color=(TileColor)i;
                var go=new GameObject(color.ToString(),typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(_overlay.transform,false);
                var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(.3f+.4f*(i%2),.58f-.13f*(i/2)); rect.sizeDelta=new Vector2(350,170);
                go.GetComponent<Image>().color=colors[i]; var button=go.GetComponent<Button>(); ColorButtons[i]=button;
                button.onClick.AddListener(()=>Choose(color));
                var label=Text(go.transform,color.ToString().ToUpperInvariant(),new Vector2(.5f,.5f),new Vector2(350,170),32);
                label.color=(i==1 || i==3) ? Color.white : Color.black;
            }
            _overlay.SetActive(false);
        }
        private static TextMeshProUGUI Text(Transform parent,string value,Vector2 anchor,Vector2 size,int font)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;
            var text=go.GetComponent<TextMeshProUGUI>();text.text=value;text.fontSize=font;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
        }
        public bool Show(Action<TileColor> choose)
        {
            _choose=choose;
            if (!GameManager.Instance.BeginColorChoice()) { _choose=null; return false; }
            _overlay.SetActive(true); return true;
        }
        private void Choose(TileColor color)
        {
            if (GameManager.Instance?.CurrentState!=GameState.ColorSelection || _choose==null) return;
            var callback=_choose;_choose=null;_overlay.SetActive(false);callback(color);
        }
        private void OnEnable() => GameEvents.OnGameStateChanged+=OnState;
        private void OnDisable() { GameEvents.OnGameStateChanged-=OnState;_choose=null; }
        private void OnState(GameState state)
        {
            if(state==GameState.Briefing || state==GameState.Result) _choose=null;
            _overlay.SetActive(state==GameState.ColorSelection && _choose!=null);
        }
    }
}
