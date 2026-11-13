using System.Collections.Generic;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    /// <summary>보드 내 종류/레벨 표식. 재색칠 후에도 실제 아이템 정체성을 표시한다.</summary>
    public class ItemTileLabels : MonoBehaviour
    {
        private PuzzleBoardManager _board;
        private Camera _camera;
        private Transform _root;
        private readonly List<TextMeshProUGUI> _labels=new List<TextMeshProUGUI>();
        private TextMeshProUGUI _freeze;
        private void Awake()
        {
            _board=GetComponent<PuzzleBoardManager>();
            var root=new GameObject("ItemLabels",typeof(RectTransform),typeof(Canvas));root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;_root=root.transform;
            _freeze=MakeLabel();
        }
        private TextMeshProUGUI MakeLabel()
        {
            var badge=new GameObject("Item badge",typeof(RectTransform),typeof(Image));badge.transform.SetParent(_root,false);
            var image=badge.GetComponent<Image>();image.color=new Color(0f,0f,0f,.8f);image.raycastTarget=false;
            var go=new GameObject("Item type/level",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(badge.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var text=go.GetComponent<TextMeshProUGUI>();text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.fontStyle=FontStyles.Bold;
            text.outlineWidth=.2f;text.outlineColor=Color.black;return text;
        }
        private void LateUpdate()
        {
            if (_board==null) return;
            if (GameManager.Instance?.CurrentState==NineMood.Core.GameState.Result || GameManager.Instance?.CurrentState==NineMood.Core.GameState.Briefing)
            {
                foreach(var label in _labels) label.transform.parent.gameObject.SetActive(false);
                _freeze.transform.parent.gameObject.SetActive(false);_freeze.text=""; return;
            }
            if (_camera==null) foreach(var camera in Camera.allCameras) if(camera.name=="Camera_Puzzle") _camera=camera;
            if (_camera==null) return;
            float scale=Screen.height/1920f;
            int count=0;
            foreach(var cell in _board.GetAllTileOffsets())
            {
                var tile=_board.GetTileAtOffset(cell);if(tile==null || tile.State==TileState.Normal) continue;
                if(count==_labels.Count) _labels.Add(MakeLabel());
                var text=_labels[count++];text.transform.parent.gameObject.SetActive(true);
                text.transform.parent.GetComponent<RectTransform>().position=_camera.WorldToScreenPoint(_board.GetCellCenterWorld(cell));text.transform.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(150,60)*scale;text.fontSize=26*scale;
                string kind=tile.ItemType switch { ItemEffectType.Recovery=>"REC",ItemEffectType.TimeFreeze=>"FRZ",ItemEffectType.Blast=>"BLAST",ItemEffectType.LineClear=>"LINE",ItemEffectType.PowerUp=>"PWR",ItemEffectType.ColorShift=>"COLOR",_=>"?" };
                text.text=kind+" "+(tile.State==TileState.ItemLv2 ? "2" : "1");
            }
            for(int i=count;i<_labels.Count;i++) _labels[i].transform.parent.gameObject.SetActive(false);
            float remaining=StaminaManager.Instance?.DecayFreezeRemaining ?? 0f;
            _freeze.transform.parent.GetComponent<RectTransform>().position=new Vector2(PrototypeSafeArea.Pixels.center.x,PrototypeSafeArea.Pixels.y+PrototypeSafeArea.Pixels.height*.615f);_freeze.transform.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(Screen.width,60*scale);_freeze.fontSize=28*scale;
            _freeze.transform.parent.gameObject.SetActive(remaining>0);
            _freeze.text=remaining>0 ? $"TIME FREEZE  {remaining:F1}s" : "";
        }
    }
}
