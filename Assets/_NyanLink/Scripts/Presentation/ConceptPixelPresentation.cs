using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NineMood.Boss;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using NineMood.Runner;
using NineMood.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NineMood.Presentation
{
    // Art-only scene adapter: never changes collisions, tile identity, stats, timers, rewards or saves.
    public class ConceptPixelPresentation : MonoBehaviour
    {
        const float GroundOffset=-.8f;
        RunnerController runner;RunnerProgressSystem progress;ObstacleSpawner spawner;BossController boss;
        SpriteRenderer[] city;SpriteRenderer rat;float floor,cityWidth;Material ringMaterial;
        readonly Dictionary<Vector3Int,LineRenderer> rings=new Dictionary<Vector3Int,LineRenderer>();
        PuzzleBoardManager board;float uiScan;
        IEnumerator Start()
        {
            // Added after the existing presentation has laid out the dynamic menus.
            if(ConceptArtLibrary.Get("cat-0")==null)yield break;
            StyleMenu();StyleButtons();
            runner=FindFirstObjectByType<RunnerController>();if(runner==null)yield break;
            progress=FindFirstObjectByType<RunnerProgressSystem>();spawner=FindFirstObjectByType<ObstacleSpawner>();boss=BossController.Instance;board=FindFirstObjectByType<PuzzleBoardManager>();
            floor=runner.transform.position.y+GroundOffset;
            var rig=FindFirstObjectByType<DualCameraRig>();rig.runnerWorldY=runner.transform.position.y+1.6f;rig.ApplySplit();board?.FitCameraToBoard();
            foreach(var old in runner.GetComponentsInChildren<PlayerCharacter>()) {old.StopAllCoroutines();old.enabled=false;}
            foreach(var old in runner.GetComponentsInChildren<Renderer>())old.enabled=false;
            var cat=SpriteObject("Pixel cat",runner.transform,ConceptArtLibrary.Get("cat-0"),40);
            cat.gameObject.AddComponent<PixelRunnerAnimator>().Bind(runner,floor);
            city=new SpriteRenderer[2];
            for(int i=0;i<2;i++)city[i]=SpriteObject("Mint city loop "+i,transform,ConceptArtLibrary.Get("city-0"),-100);
            cityWidth=city[0].sprite.bounds.size.x;
            rat=SpriteObject("Rat king",transform,ConceptArtLibrary.Get("boss-0"),30);
            rat.transform.position=new Vector3(2.3f,floor,-.15f);rat.transform.localScale=Vector3.one*(1.8f/2.1f);rat.enabled=false;
            ringMaterial=new Material(Resources.Load<Material>("PrototypeLineMaterial"));
            if(ringMaterial.HasProperty("_Color"))ringMaterial.SetColor("_Color",Color.white);
            StyleHud();
        }
        static SpriteRenderer SpriteObject(string name,Transform parent,Sprite sprite,int order)
        {
            var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.layer=LayerMask.NameToLayer("TopView");
            var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=order;return renderer;
        }
        void LateUpdate()
        {
            uiScan-=Time.unscaledDeltaTime;if(uiScan<=0){StyleButtons();uiScan=.5f;}
            if(runner==null || city==null)return;
            float offset=Mathf.Repeat((progress!=null ? progress.CurrentDistance : 0)*.12f,cityWidth);
            for(int i=0;i<2;i++)city[i].transform.position=new Vector3(i*cityWidth-offset,floor+3.9f,2);
            var game=GameManager.Instance;
            var phase=game.CurrentState==GameState.Paused ? game.StateBeforePause : game.CurrentState;
            bool bossScene=phase==GameState.BossPhase || (phase==GameState.ColorSelection && boss!=null && boss.MaxHP>0);
            rat.enabled=bossScene;
            if(bossScene)rat.sprite=ConceptArtLibrary.Get(boss.IsGroggy ? "boss-1":"boss-0");
            if(spawner!=null && spawner.CurrentObstacle!=null)ApplyObstacle(spawner.CurrentObstacle);
            UpdateRings();
        }
        void ApplyObstacle(ObstacleController obstacle)
        {
            if(obstacle.transform.Find("Concept obstacle")!=null)return;
            if(!obstacle.IsInitialized)return;
            foreach(var old in obstacle.GetComponentsInChildren<Renderer>())old.enabled=false;
            int id=obstacle.RequiredAction==RunnerState.Jump ? 0:obstacle.RequiredAction==RunnerState.Slide ? 1:2;
            var art=SpriteObject("Concept obstacle",obstacle.transform,ConceptArtLibrary.Get("obstacle-"+id),25);
            // Normalize old cube-prefab scale, retaining the authoritative obstacle transform/collider.
            var scale=obstacle.transform.lossyScale;
            art.transform.localScale=new Vector3(1/Mathf.Max(.01f,Mathf.Abs(scale.x)),1/Mathf.Max(.01f,Mathf.Abs(scale.y)),1);
            art.transform.position=new Vector3(obstacle.transform.position.x,floor,-.1f);
        }
        void UpdateRings()
        {
            if(board==null || ringMaterial==null)return;
            bool show=GameManager.Instance.CurrentState!=GameState.Briefing && GameManager.Instance.CurrentState!=GameState.Result;
            foreach(var ring in rings.Values)ring.enabled=false;
            if(!show)return;
            foreach(var cell in board.GetAllTileOffsets())
            {
                var tile=board.GetTileAtOffset(cell);if(tile.State==TileState.Normal)continue;
                if(!rings.TryGetValue(cell,out var line))
                {
                    var go=new GameObject("Item tier ring");go.transform.SetParent(transform,false);go.layer=board.gameObject.layer;
                    line=go.AddComponent<LineRenderer>();line.sharedMaterial=ringMaterial;line.useWorldSpace=true;line.loop=true;line.positionCount=6;
                    var renderer=board.tilemap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();line.sortingLayerID=renderer.sortingLayerID;line.sortingOrder=renderer.sortingOrder+5;
                    rings.Add(cell,line);
                }
                var center=board.GetCellCenterWorld(cell);
                for(int i=0;i<6;i++)
                {float angle=i*Mathf.PI/3;var local=new Vector3(Mathf.Sin(angle)*.47f,Mathf.Cos(angle)*.47f,0);line.SetPosition(i,center+board.tilemap.transform.TransformVector(local)+Vector3.back*.05f);}
                line.startWidth=line.endWidth=tile.State==TileState.ItemLv2 ? .055f:.035f;
                line.startColor=line.endColor=tile.State==TileState.ItemLv2 ? new Color(.7f,1f,1f):new Color(1f,.78f,.25f);line.enabled=true;
            }
        }
        void StyleMenu()
        {
            foreach(var menu in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(m=>m is MainMenuController || m is GameReadyPanel))
            {
                var root=menu.transform;
                var backdrop=new GameObject("Concept city backdrop",typeof(RectTransform),typeof(Image),typeof(AspectRatioFitter));backdrop.transform.SetParent(root,false);backdrop.transform.SetAsFirstSibling();
                var image=backdrop.GetComponent<Image>();image.sprite=ConceptArtLibrary.Get("city-0");image.color=new Color(.3f,.48f,.65f,.30f);image.raycastTarget=false;
                var fit=backdrop.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=image.sprite.rect.width/image.sprite.rect.height;
                if(root.GetComponent<RectMask2D>()==null)root.gameObject.AddComponent<RectMask2D>();
                foreach(var text in menu.GetComponentsInChildren<TextMeshProUGUI>(true).Where(t=>t.text=="NYAN LINK"))
                {
                    text.enabled=false;var go=new GameObject("Concept title logo",typeof(RectTransform),typeof(Image));go.transform.SetParent(root,false);
                    PrototypePresentation.Place(go.GetComponent<RectTransform>(),.05f,.62f,.95f,.85f);
                    var logo=go.GetComponent<Image>();logo.sprite=ConceptArtLibrary.Get("logo-0");logo.preserveAspect=true;logo.raycastTarget=false;
                }
            }
        }
        void StyleButtons()
        {
            foreach(var button in FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(PixelUISkin.Available) {PixelUISkin.Button(button);continue;}
                if(button.GetComponent<PixelButtonAudio>()!=null)continue;
                button.gameObject.AddComponent<PixelButtonAudio>();
                bool colorChoice=false;for(var parent=button.transform.parent;parent!=null;parent=parent.parent)if(parent.name=="Color selection")colorChoice=true;
                if(!colorChoice && button.targetGraphic is Image image)
                {
                    image.color=new Color(.10f,.25f,.43f,1f);
                    var border=button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();border.effectColor=new Color(.25f,.75f,.9f,.8f);border.effectDistance=new Vector2(2,-2);
                }
            }
        }
        static void PlaceText(TextMeshProUGUI text,float y0,float y1,float size)
        {PrototypePresentation.Place(text.rectTransform,.06f,y0,.94f,y1);text.fontSizeMax=size;text.fontSize=size;text.fontSizeMin=size*.7f;}
        void StyleHud()
        {
            var hud=FindFirstObjectByType<GameHUD>();
            var tint=new GameObject("Navy HUD backing",typeof(RectTransform),typeof(Image));tint.transform.SetParent(hud.transform,false);tint.transform.SetAsFirstSibling();
            PrototypePresentation.Place(tint.GetComponent<RectTransform>(),0,.895f,1,1);tint.GetComponent<Image>().color=new Color(.025f,.055f,.13f,.92f);tint.GetComponent<Image>().raycastTarget=false;
            var bossHud=FindFirstObjectByType<BossHUD>();
            var info=new GameObject("Boss info backing",typeof(RectTransform),typeof(Image));info.transform.SetParent(bossHud.transform,false);info.transform.SetAsFirstSibling();
            PrototypePresentation.Place(info.GetComponent<RectTransform>(),0,.778f,1,.895f);info.GetComponent<Image>().color=new Color(.025f,.055f,.13f,.78f);info.GetComponent<Image>().raycastTarget=false;
            foreach(var text in bossHud.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if(text.fontSharedMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth)) {text.outlineColor=new Color(.02f,.04f,.10f);text.outlineWidth=.22f;}
                switch(text.name)
                {
                    case "Text_BossHP":PlaceText(text,.87f,.895f,27);break;
                    case "Text_Phase":PlaceText(text,.833f,.855f,22);PrototypePresentation.Place(text.rectTransform,.06f,.833f,.28f,.855f);break;
                    case "Text_PatternType":text.color=new Color(1f,.83f,.35f);PlaceText(text,.833f,.855f,23);PrototypePresentation.Place(text.rectTransform,.30f,.833f,.94f,.855f);break;
                    case "Boss Timer":PlaceText(text,.805f,.833f,29);break;
                    case "Text_Instruction":PlaceText(text,.778f,.805f,25);break;
                }
            }
            var warning=FindFirstObjectByType<RunnerWarningHUD>();
            PrototypePresentation.Place(warning.warningPanel.GetComponent<RectTransform>(),.08f,.805f,.92f,.88f);
            warning.warningPanel.GetComponent<Image>().color=new Color(.035f,.055f,.12f,.94f);
        }
        void OnDestroy() {if(ringMaterial!=null)Destroy(ringMaterial);}
    }
}
