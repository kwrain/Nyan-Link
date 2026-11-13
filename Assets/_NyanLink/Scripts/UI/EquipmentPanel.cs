using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Core.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NineMood.UI
{
    // Scene-owned overlay, no persistent references to Main/Game UI.
    public class EquipmentPanel : MonoBehaviour
    {
        public Button OpenButton { get; private set; }
        public Button CloseButton { get; private set; }
        public Button NewSetupButton { get; private set; }
        public readonly Dictionary<string,Button> EquipButtons=new Dictionary<string,Button>();
        public readonly Dictionary<string,Button> UpgradeButtons=new Dictionary<string,Button>();
        public bool IsOpen => overlay!=null && overlay.activeSelf;
        GameObject overlay,root;
        Transform content;
        TextMeshProUGUI stats,status,slots,reward;
        string notice;
        void Awake()
        {
            root=new GameObject("EquipmentCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);
            if(transform is RectTransform) Stretch(root.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.overrideSorting=true;canvas.sortingOrder=700;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);
            OpenButton=Button(root.transform,"EQUIPMENT",new Vector2(.5f,.22f),new Vector2(500,90),Open);
            reward=Label(root.transform,"",new Vector2(.5f,.60f),new Vector2(950,110),30);
            reward.gameObject.SetActive(false);
            overlay=new GameObject("EquipmentOverlay",typeof(RectTransform),typeof(Image));overlay.transform.SetParent(root.transform,false);
            Stretch(overlay.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
            overlay.GetComponent<Image>().color=new Color(.035f,.06f,.13f,1f);
            Label(overlay.transform,"EQUIPMENT  /  MAX LV.5",new Vector2(.5f,.94f),new Vector2(960,100),48);
            stats=Label(overlay.transform,"",new Vector2(.5f,.87f),new Vector2(960,130),36);
            slots=Label(overlay.transform,"",new Vector2(.5f,.77f),new Vector2(960,160),30);
            var viewport=new GameObject("InventoryScroll",typeof(RectTransform),typeof(Image),typeof(Mask),typeof(ScrollRect));viewport.transform.SetParent(overlay.transform,false);
            Stretch(viewport.GetComponent<RectTransform>(),new Vector2(.04f,.27f),new Vector2(.96f,.69f));
            viewport.GetComponent<Image>().color=new Color(.07f,.10f,.20f);viewport.GetComponent<Mask>().showMaskGraphic=true;
            var body=new GameObject("Items",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));body.transform.SetParent(viewport.transform,false);content=body.transform;
            var r=body.GetComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);r.sizeDelta=Vector2.zero;
            var layout=body.GetComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.spacing=12;layout.padding=new RectOffset(12,12,12,12);
            body.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.GetComponent<ScrollRect>();scroll.content=r;scroll.viewport=viewport.GetComponent<RectTransform>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            status=Label(overlay.transform,"",new Vector2(.5f,.21f),new Vector2(980,140),27);
            NewSetupButton=Button(overlay.transform,"NEW RUN SETUP (discard checkpoint)",new Vector2(.5f,.12f),new Vector2(940,85),()=>{ProgressionService.NewRunSetup();notice=ProgressionService.LastMessage;Refresh();});
            CloseButton=Button(overlay.transform,"CLOSE",new Vector2(.5f,.05f),new Vector2(600,90),()=>overlay.SetActive(false));
            overlay.SetActive(false);RefreshVisibility();
        }
        void OnEnable() {GameEvents.OnGameStateChanged+=OnState;GameEvents.OnResultSet+=OnResult;}
        void OnDisable() {GameEvents.OnGameStateChanged-=OnState;GameEvents.OnResultSet-=OnResult;}
        void OnState(GameState state) {overlay.SetActive(false);RefreshVisibility();}
        void OnResult(ResultType result) {RefreshVisibility();if(result==ResultType.Win) Refresh();}
        void RefreshVisibility()
        {
            bool menu=!GameManager.IsCreated;
            bool ready=menu || GameManager.Instance.CurrentState==GameState.Briefing;
            bool win=!menu && GameManager.Instance.CurrentState==GameState.Result && GameManager.Instance.CurrentResult==ResultType.Win;
            OpenButton.gameObject.SetActive(ready || win);
            reward.gameObject.SetActive(win);
        }
        public void Open() {notice=null;Refresh();overlay.SetActive(true);}
        public void Refresh()
        {
            var read=ProgressionService.Read();
            reward.text=SaveManager.IsCreated ? ProgressionService.RewardSummary(SaveManager.Instance.CurrentRunId) : "";
            EquipButtons.Clear();UpgradeButtons.Clear();
            foreach(Transform child in content) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
            if(!read.Usable) {stats.text="SAVE UNAVAILABLE";slots.text="";status.text=read.message;NewSetupButton.gameObject.SetActive(false);return;}
            var doc=read.data;var p=doc.permanent;var effective=ProgressionService.Stats(p);
            stats.text=$"COINS  {p.coins:N0}\nMAX HP  {effective.maxHP:0.#}     ATTACK  {effective.attack:0.##}";
            string[] names={"HAT","OUTFIT","WEAPON"};slots.text="";
            for(int slot=0;slot<3;slot++)
            {
                var e=p.equipment.Find(x=>x.equippedSlot==slot);
                var d=e==null ? null : DataManager.Instance.GetEquipmentData(e.dataId);
                slots.text+=names[slot]+": "+(e==null ? "Empty" : (d!=null ? d.equipmentName : e.dataId)+" Lv."+e.level)+"\n";
            }
            bool allowed=ProgressionService.CanChangeLoadout(doc);
            NewSetupButton.gameObject.SetActive(doc.hasCheckpoint);
            status.text=notice ?? (doc.pendingClears.Count>0 ? "CLEAR REWARD PENDING\n"+ProgressionService.LastMessage : allowed ? "Select gear, then retry to use these stats.\nUpgrades: 100 / 150 / 200 / 250 coins" : "Saved run kept. Gear changes are locked.\nNEW RUN SETUP discards only the saved run.");
            if(p.equipment.Count==0)
            {
                var empty=new GameObject("Empty",typeof(RectTransform),typeof(LayoutElement));empty.transform.SetParent(content,false);empty.GetComponent<LayoutElement>().preferredHeight=190;
                Label(empty.transform,"Clear the stage for 100 coins.\nFirst clear guarantees a Trail Hat.",new Vector2(.5f,.5f),new Vector2(900,170),32);
            }
            foreach(var record in p.equipment)
            {
                var data=DataManager.Instance.GetEquipmentData(record.dataId);string id=record.instanceId;
                var row=new GameObject("Item-"+id,typeof(RectTransform),typeof(Image),typeof(LayoutElement));row.transform.SetParent(content,false);row.GetComponent<LayoutElement>().preferredHeight=200;row.GetComponent<Image>().color=new Color(.12f,.17f,.27f);
                var bonus=data!=null ? data.GetStatAtLevel(record.level) : null;
                Label(row.transform,(data!=null ? data.equipmentName : "Unknown: "+record.dataId)+$"  Lv.{record.level}"+(record.equippedSlot>=0 ? "  [EQUIPPED]" : "")+"\n"+(bonus==null ? "Preserved legacy equipment" : $"HP +{bonus.hpBonus:0.#}   ATTACK +{bonus.attackBonus:0.##}"),new Vector2(.5f,.72f),new Vector2(920,95),30);
                bool equipped=record.equippedSlot>=0;
                var equip=Button(row.transform,equipped ? "UNEQUIP" : "EQUIP",new Vector2(.25f,.23f),new Vector2(390,70),()=>{ProgressionService.ChangeEquipment(id,!equipped);notice=ProgressionService.LastMessage;Refresh();});
                int cost=ProgressionService.UpgradeCost(record.level);
                var upgrade=Button(row.transform,cost==0 ? "MAX LV.5" : "UPGRADE  "+cost,new Vector2(.75f,.23f),new Vector2(390,70),()=>{ProgressionService.Upgrade(id);notice=ProgressionService.LastMessage;Refresh();});
                equip.interactable=allowed && data!=null;upgrade.interactable=allowed && data!=null && cost>0 && p.coins>=cost;
                EquipButtons[id]=equip;UpgradeButtons[id]=upgrade;
            }
        }
        static void Stretch(RectTransform r,Vector2 min,Vector2 max) {r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
        static Button Button(Transform parent,string text,Vector2 anchor,Vector2 size,UnityAction action)
        {
            var go=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;
            go.GetComponent<Image>().color=new Color(.13f,.36f,.55f);
            var button=go.GetComponent<Button>();button.onClick.AddListener(action);Label(go.transform,text,new Vector2(.5f,.5f),size,28);return button;
        }
        static TextMeshProUGUI Label(Transform parent,string text,Vector2 anchor,Vector2 size,int font)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;
            var label=go.GetComponent<TextMeshProUGUI>();label.text=text;label.fontSize=font;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
        }
    }
}
