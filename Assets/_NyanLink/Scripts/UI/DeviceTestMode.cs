using System;
using System.Collections;
using System.IO;
using System.Linq;
using NineMood.Boss;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Puzzle;
using NineMood.Runner;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.UI
{
    // Runtime UI, also included in device builds. Test data never shares the normal profile.
    public sealed class DeviceTestMode : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static string StoragePath { get; private set; }
        public static bool BossOnly = true;
        public static int BossIndex, PatternIndex, PhaseIndex;
        public static BossDefinition[] Bosses => Resources.LoadAll<BossDefinition>("Data/BossConfigs").OrderBy(b=>b.bossId).ToArray();
        GameObject panel, canvasRoot;
        Text bossLabel, patternLabel, phaseLabel, modeLabel;
        bool resume;
        BossBattleConfig original, clone;
        BossDefinition definition;
        public Button Entry { get; private set; }
        public Button Launch { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { Active=false; StoragePath=null; BossIndex=PatternIndex=PhaseIndex=0; BossOnly=true; }
        public static void Leave() { Active=false; StoragePath=null; }
        public static void Enter()
        {
            if(!Active) StoragePath=Path.Combine(Application.persistentDataPath,"device-tests",Guid.NewGuid().ToString("N"));
            Active=true;
        }
        void Start()
        {
            var root=new GameObject("Device test canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasRoot=root;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=900;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var safe=new GameObject("Safe area",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            var area=Screen.safeArea;var rect=safe.GetComponent<RectTransform>();rect.anchorMin=area.min/new Vector2(Screen.width,Screen.height);rect.anchorMax=area.max/new Vector2(Screen.width,Screen.height);rect.offsetMin=rect.offsetMax=Vector2.zero;
            bool inGame=GameManager.IsCreated;
            Entry=Button(safe.transform,Active?"테스트 설정":"테스트 모드",.5f,inGame?.91f:.16f,340,80,Open);
            Entry.gameObject.SetActive(!inGame || Active);
            panel=new GameObject("Test settings",typeof(RectTransform),typeof(Image));panel.transform.SetParent(safe.transform,false);
            rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            panel.GetComponent<Image>().color=new Color(.025f,.04f,.08f,.98f);
            Label(panel.transform,"실기기 테스트",.88f,48);
            Label(panel.transform,"테스트 전용 저장 · 일반 진행과 분리",.82f,26);
            modeLabel=Button(panel.transform,"",.5f,.73f,850,105,()=>{BossOnly=!BossOnly;Refresh();}).GetComponentInChildren<Text>();
            bossLabel=Button(panel.transform,"",.5f,.64f,850,105,()=>{BossIndex++;PatternIndex=PhaseIndex=0;Refresh();}).GetComponentInChildren<Text>();
            patternLabel=Button(panel.transform,"",.5f,.55f,850,105,()=>{PatternIndex++;Refresh();}).GetComponentInChildren<Text>();
            phaseLabel=Button(panel.transform,"",.5f,.46f,850,105,()=>{PhaseIndex++;Refresh();}).GetComponentInChildren<Text>();
            Launch=Button(panel.transform,"선택한 설정으로 시작 / 재시작",.5f,.35f,850,110,()=>{
                Enter(); if(GameManager.IsCreated){Configure();panel.SetActive(false);GameManager.Instance.RetryGame();}else SceneLoader.LoadGame();
            });
            if(inGame && Active) {
                Button(panel.transform,"체력 회복",.28f,.25f,400,90,()=>StaminaManager.Instance?.Recover(9999));
                Button(panel.transform,"퍼즐 보드 초기화",.72f,.25f,400,90,()=>{ if(!(BossController.Instance?.IsGroggy??false)) FindFirstObjectByType<PuzzleBoardManager>()?.ResetBoard(); });
            }
            Button(panel.transform,"닫기",.28f,.13f,400,90,()=>{panel.SetActive(false);if(resume)GameManager.Instance?.ResumeGame();});
            Button(panel.transform,"일반 로비로",.72f,.13f,400,90,SceneLoader.LoadMainMenu);
            Refresh();panel.SetActive(false);
            if(inGame && Active) StartCoroutine(AutoStart());
        }
        IEnumerator AutoStart() { yield return null; Configure();GameManager.Instance.RetryGame(); }
        public void Open()
        {
            resume=GameManager.IsCreated && (GameManager.Instance.CurrentState==GameState.RunnerPhase || GameManager.Instance.CurrentState==GameState.BossPhase);
            if(GameManager.IsCreated && GameManager.Instance.CurrentState==GameState.ColorSelection) return;
            if(resume) GameManager.Instance.PauseGame();
            Refresh();panel.SetActive(true);
        }
        void Refresh()
        {
            var bosses=Bosses;Launch.interactable=bosses.Length>0;
            if(bosses.Length==0) {bossLabel.text="등록된 보스 없음";return;}
            BossIndex%=bosses.Length;var boss=bosses[BossIndex];PhaseIndex%=boss.phases.Length;
            var patterns=boss.phases[PhaseIndex].patterns;PatternIndex%=patterns.Length+1;
            modeLabel.text="진입: "+(BossOnly?"보스 바로 시작":"일반 주행 · 장애물 · 보스")+"  ↻";
            bossLabel.text="보스: "+boss.displayName+"  ↻";
            patternLabel.text="패턴: "+(PatternIndex==0?"전체 무작위":patterns[PatternIndex-1].displayName)+"  ↻";
            phaseLabel.text="난도: 체력 구간 "+(PhaseIndex+1)+"  ↻";
        }
        void Configure()
        {
            var boss=BossController.Instance;if(boss==null)return;
            if(original==null)original=boss.config;
            if(clone!=null)Destroy(clone);if(definition!=null)Destroy(definition);
            clone=Instantiate(original);definition=Instantiate(Bosses[BossIndex]);
            var phase=definition.phases[PhaseIndex];phase.enterAtHP=1;
            if(PatternIndex>0)phase.patterns=new[]{phase.patterns[PatternIndex-1]};
            definition.phases=new[]{phase};clone.definition=definition;boss.config=clone;
        }
        public static void OnRunStarted()
        {
            if(!Active || !BossOnly)return;
            var spawner=FindFirstObjectByType<ObstacleSpawner>();var progress=FindFirstObjectByType<RunnerProgressSystem>();
            spawner.RestoreCheckpoint(spawner.stageData.obstacles.Count);progress.RestoreDistance(progress.StageLength);GameManager.Instance.TriggerBossFight();
        }
        void OnDestroy(){if(canvasRoot!=null)Destroy(canvasRoot);if(clone!=null)Destroy(clone);if(definition!=null)Destroy(definition);}
        static Text Label(Transform parent,string text,float y,int size)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,y);rect.sizeDelta=new Vector2(900,100);
            var label=go.GetComponent<Text>();label.text=text;label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;label.font=Resources.Load<Font>("Fonts/Galmuri/Galmuri11");label.color=Color.white;return label;
        }
        static Button Button(Transform parent,string text,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);
            go.GetComponent<Image>().color=new Color(.12f,.25f,.4f);var button=go.GetComponent<Button>();button.onClick.AddListener(action);
            var label=Label(go.transform,text,.5f,30);label.rectTransform.sizeDelta=new Vector2(w-24,h-8);return button;
        }
    }
}
