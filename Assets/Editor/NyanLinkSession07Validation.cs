using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NineMood.Boss;
using NineMood.Core;
using NineMood.Core.Persistence;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using NineMood.Runner;
using NineMood.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class NyanLinkSession07Validation
{
    const string Key = "NyanLink.Session07";
    const string Output = "output/playtests/2026-11-13-session07";
    const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly List<string> results = new List<string>();
    static readonly List<string> errors = new List<string>();
    static Stack<IEnumerator> work;
    static NyanLinkSession02PlayDriver driver;
    static Mouse mouse, previousMouse;
    static bool mouseWasEnabled;
    static InputSettings.BackgroundBehavior oldBackground;
    static InputSettings.EditorInputBehaviorInPlayMode oldEditor;
    static double next, expires;
    static int nextFrame, assertions, failures, clears, bossEvents, resultEvents;
    static GameManager game;
    static ObstacleSpawner spawner;
    static RunnerProgressSystem progress;
    static RunnerController runner;
    static PuzzleBoardManager board;
    static TileInputHandler input;
    static StageData stageClone;
    static BossBattleConfig bossClone;
    static Camera camera;
    static Dictionary<string,string> saveHashes;
    static string BestKey = "NyanLink_BestScore";
    static int oldBest;
    static bool hadBest;

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        if(!SessionState.GetBool(Key+".Cold",false)) File.WriteAllText(Path.Combine(Output,"active-root.txt"),Path.GetFullPath(Path.Combine(Output,"sandbox-"+Guid.NewGuid().ToString("N"))));
        EditorSceneManager.OpenScene("Assets/_NyanLink/Scene/Main.unity");
        SessionState.SetBool(Key,true);
        SessionState.SetBool(Key + ".HasBest",PlayerPrefs.HasKey(BestKey));
        SessionState.SetInt(Key + ".Best",PlayerPrefs.GetInt(BestKey));
        var view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Show(); view.Focus();
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
    }

    public static void RunColdContinue() { SessionState.SetBool(Key+".Cold",true);Run(); }
    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (!SessionState.GetBool(Key,false)) return;
        SaveManager.TestDirectoryOverride=Path.Combine(File.ReadAllText(Path.Combine(Output,"active-root.txt")),"play");
        oldBest = SessionState.GetInt(Key + ".Best",0);
        hadBest = SessionState.GetBool(Key + ".HasBest",false);
        oldBackground = InputSystem.settings.backgroundBehavior;
        oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Application.logMessageReceived += Log;
        expires = EditorApplication.timeSinceStartup + 300;
        next = EditorApplication.timeSinceStartup + 3;
        work = new Stack<IEnumerator>(); work.Push(Sequence());
        EditorApplication.update += KeepAlive;
    }
    static void KeepAlive()
    {
        Application.runInBackground = true;
        if (EditorApplication.timeSinceStartup > expires) { Finish(new Exception("Session07 timeout")); return; }
        if (EditorApplication.isPlaying && driver == null)
        {
            driver = new GameObject("Session07 validation (temporary)").AddComponent<NyanLinkSession02PlayDriver>();
            driver.gameObject.hideFlags = HideFlags.DontSave;
            driver.LateTick = Tick;
        }
    }
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next || Time.frameCount < nextFrame) return;
        try
        {
            while (work.Count > 0)
            {
                if (!work.Peek().MoveNext()) { work.Pop(); continue; }
                var yielded = work.Peek().Current;
                if (yielded is IEnumerator child) { work.Push(child); continue; }
                next = EditorApplication.timeSinceStartup + (yielded is float delay ? delay : 0.02f);
                nextFrame = Time.frameCount + 1;
                return;
            }
            Finish(null);
        }
        catch (Exception error) { Finish(error); }
    }
    static void Assert(bool condition,string message)
    {
        assertions++;
        if (!condition) throw new Exception("FAIL " + message);
    }
    static void Pass(string message)
    {
        results.Add("PASS " + message);
        Debug.Log("[Session07] " + results.Last());
    }
    static void Log(string text,string trace,LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text + "\n" + trace);
    }
    static IEnumerator Wait(Func<bool> predicate,string message,float timeout=8f)
    {
        double end = EditorApplication.timeSinceStartup + timeout;
        while (!predicate())
        {
            if (EditorApplication.timeSinceStartup > end) throw new Exception("Timeout: " + message);
            yield return 0.02f;
        }
    }
    static void View(string method,params object[] args) => typeof(NyanLinkSession02Validation)
        .GetMethod(method,BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,args);
    static Button Button(Component component,string field) => new SerializedObject(component).FindProperty(field).objectReferenceValue as Button;
    static Vector2 ButtonPoint(Button button)
    {
        var rect = button.GetComponent<RectTransform>();
        return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
    }
    static void Send(Vector2 point,bool down)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse,new MouseState { position = point }.WithButton(MouseButton.Left,down));
    }
    static IEnumerator Click(Button button)
    {
        Assert(button != null && button.isActiveAndEnabled && button.interactable,"Actual UI button available");
        Vector2 point = ButtonPoint(button);
        Send(point,true); yield return 0.06f;
        Send(point,false); yield return 0.12f;
    }
    static Vector2 Point(Vector3Int cell) => camera.WorldToScreenPoint(board.GetCellCenterWorld(cell));
    static IEnumerator Match(TileColor color)
    {
        yield return Wait(() => !input.IsInputLocked,"Input unlocked");
        Assert(board.TryEnsureAdjacentPair(color),"Color pair available: " + color);
        Vector3Int a = default, b = default;
        bool found = false;
        foreach (var cell in board.GetAllTileOffsets())
        {
            var tile = board.GetTileAtOffset(cell);
            if (tile.Color != color || tile.State != TileState.Normal) continue;
            foreach (var neighbor in HexOffsetUtils.GetNeighbors(cell))
            {
                var other = board.GetTileAtOffset(neighbor);
                if (other == null || other.Color != color || other.State != TileState.Normal) continue;
                a = cell; b = neighbor; found = true; break;
            }
            if (found) break;
        }
        Assert(found,"Normal adjacent pair exists");
        Send(Point(a),true); yield return 0.04f;
        Send(Point(b),true); yield return 0.04f;
        Assert(input.SelectedChain.Count == 2,"Actual mouse drag selects pair");
        int scoreBefore = ScoreManager.Instance.CurrentScore;
        Send(Point(b),false);
        yield return Wait(() => ScoreManager.Instance.CurrentScore > scoreBefore,
            "Release score " + color + " (selection=" + input.SelectedChain.Count + ")",2f);
        yield return 0.02f;
    }
    static void Capture(string file) => ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Output,file)));
    static string Fingerprint() => string.Join(";",board.GetAllTileOffsets().OrderBy(c=>c.y).ThenBy(c=>c.x)
        .Select(c => c + ":" + board.GetTileAtOffset(c).Color + ":" + board.GetTileAtOffset(c).State));
    static Dictionary<string,string> Saves()
    {
        var path = Path.Combine(Application.persistentDataPath,"saves");
        var hashes = new Dictionary<string,string>();
        if (!Directory.Exists(path)) return hashes;
        foreach (var file in Directory.GetFiles(path,"*",SearchOption.AllDirectories))
            using (var hash = SHA256.Create()) hashes[file] = Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(file)));
        return hashes;
    }
    static void CheckSubscribers()
    {
        foreach (var field in typeof(GameEvents).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if (!(field.GetValue(null) is Delegate value)) continue;
            var callbacks = value.GetInvocationList();
            Assert(!callbacks.Any(c => c.Target is Object obj && obj == null),"No destroyed subscriber: " + field.Name);
            Assert(callbacks.Select(c => (c.Target,c.Method)).Distinct().Count() == callbacks.Length,"No duplicate subscriber: " + field.Name);
        }
    }
    static string root;
    static SaveManager save;
    static BossController boss;
    static bool compatibilityChecked;
    static void Bind()
    {
        game=GameManager.Instance;save=SaveManager.Instance;boss=BossController.Instance;
        progress=Object.FindFirstObjectByType<RunnerProgressSystem>();spawner=Object.FindFirstObjectByType<ObstacleSpawner>();
        runner=Object.FindFirstObjectByType<RunnerController>();board=Object.FindFirstObjectByType<PuzzleBoardManager>();input=Object.FindFirstObjectByType<TileInputHandler>();
        camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c=>c.name=="Camera_Puzzle");
        CheckSubscribers();
        if(!compatibilityChecked)
        {
            compatibilityChecked=true;string original=SaveManager.TestDirectoryOverride;
            SaveManager.TestDirectoryOverride=Path.Combine(root,"compatibility-session06");Directory.CreateDirectory(SaveManager.StorageDirectory);
            File.Copy("output/playtests/2026-11-13-session06/schema-example.json",ProgressionService.Store.FilePath,true);
            string bytes=Bytes();var old=ProgressionService.Read();
            Assert(old.Usable && old.data.schemaVersion==1 && old.data.permanent.coins==421,"Original Session06 checksum/schema/profile readable without migration");
            Assert(save.HasCurrentCheckpoint() && Bytes()==bytes,"Original Session06 checkpoint compatible and remains byte-identical");
            SaveManager.TestDirectoryOverride=original;
        }
    }
    static IEnumerator EnterGame()
    {
        var main=Object.FindFirstObjectByType<EquipmentPanel>();
        yield return Click(main.OpenButton);yield return .06f;
        var rect=main.transform.Find("EquipmentCanvas/EquipmentOverlay").GetComponent<RectTransform>();
        Assert(main.IsOpen && rect.rect.width>=1000 && rect.rect.height>=1900,"Main equipment overlay fills portrait canvas");
        yield return Click(main.CloseButton);Assert(!main.IsOpen,"Main equipment close uses actual UI");
        yield return Click(Button(Object.FindFirstObjectByType<MainMenuController>(),"buttonPlay"));
        yield return Wait(()=>SceneManager.GetActiveScene().name=="Game" && Object.FindFirstObjectByType<PuzzleBoardManager>()?.GetTileCount()==39,"Game ready");Bind();
    }
    static PermanentData Profile()=>ProgressionService.Read().data.permanent;
    static void Near(float actual,float expected,string text)=>Assert(Mathf.Abs(actual-expected)<.03f,text+" actual="+actual+" expected="+expected);
    static string Bytes()=>File.ReadAllText(ProgressionService.Store.FilePath);
    static IEnumerator BossEntry()
    {
        spawner.RestoreCheckpoint(3);progress.RestoreDistance(progress.StageLength);game.TriggerBossFight();
        yield return .04f;Assert(game.CurrentState==GameState.BossPhase,"Boss entered via runtime state API (travel fixture)");
    }
    static void RewardCases()
    {
        string play=SaveManager.TestDirectoryOverride;
        SaveManager.TestDirectoryOverride=Path.Combine(root,"reward-cases");
        var doc=new SaveDocument();doc.pendingClears.Add(new PendingClear {runId="first",stageId="prototype-stage-01",score=20});
        Assert(ProgressionService.Store.Write(doc,out _),"Seed durable pending clear, emulating interruption before payout");
        ProgressionService.RecoverPending();var p=Profile();
        Assert(p.coins==100 && p.equipment.Single().dataId=="prototype-hat-01" && p.clears.Single().firstRewardGranted && p.rewardReceipts.Single()=="first","First reward +100 and one guaranteed hat and receipt atomic");
        string before=Bytes();Assert(!ProgressionService.ApplyReward("first") && Bytes()==before,"Repeated grant no write/no double coins or gear");
        ProgressionService.RecoverPending();Assert(Bytes()==before,"Repeated recovery no write");
        var rng=UnityEngine.Random.state;
        string[] types={"hat","outfit","weapon",null};
        for(int outcome=0;outcome<4;outcome++)
        {
            string id=Enumerable.Range(0,10000).Select(i=>"roll-"+outcome+"-"+i).First(x=>{double n=ProgressionService.Roll(x,"prototype-stage-01");return n>=outcome*.1 && n<(outcome<3 ? (outcome+1)*.1 : 1);});
            doc=ProgressionService.Read().data;doc.pendingClears.Add(new PendingClear{runId=id,stageId="prototype-stage-01",score=100+outcome});Assert(ProgressionService.Store.Write(doc,out _),"Queue deterministic drop branch");
            int coins=Profile().coins,count=Profile().equipment.Count;
            Assert(ProgressionService.ApplyReward(id),"Apply branch "+outcome);p=Profile();
            Assert(p.coins==coins+100 && p.equipment.Count==count+(outcome<3 ? 1 : 0),"Exact fixed coins and at most one equipment");
            if(outcome<3) Assert(p.equipment.Last().dataId=="prototype-"+types[outcome]+"-01","Correct cumulative probability branch "+outcome);
            before=Bytes();Assert(!ProgressionService.ApplyReward(id) && Bytes()==before,"All branch duplicate grants refused");
        }
        Assert(JsonUtility.ToJson(rng)==JsonUtility.ToJson(UnityEngine.Random.state),"Reward rolls do not reroll board/pattern random state");
        var table=DataManager.Instance.GetStageData("prototype-stage-01").lootTable;
        table.lootEntries.Add(new LootTableData.LootEntry{itemType=LootTableData.LootItemType.Coin,dropChance=1,quantity=999999});
        doc=ProgressionService.Read().data;doc.pendingClears.Add(new PendingClear{runId="coin-filter",stageId="prototype-stage-01"});Assert(ProgressionService.Store.Write(doc,out _),"Coin entry fixture");
        int previous=Profile().coins;Assert(ProgressionService.ApplyReward("coin-filter") && Profile().coins==previous+100,"Loot coin entries never pay additional coins");table.lootEntries.RemoveAt(table.lootEntries.Count-1);
        doc=ProgressionService.Read().data;doc.permanent.coins=0;doc.permanent.equipment.Add(new EquipmentRecord{instanceId="legacy-unknown",dataId="legacy-unknown"});Assert(ProgressionService.Store.Write(doc,out _),"Preserve unknown legacy record");
        before=Bytes();Assert(!ProgressionService.Upgrade("reward-first") && Bytes()==before,"Insufficient funds rejected without write");
        Assert(!ProgressionService.Upgrade("missing") && !ProgressionService.ChangeEquipment("missing",true) && !ProgressionService.ChangeEquipment("legacy-unknown",true) && Bytes()==before,"Invalid/unknown requests rejected with legacy data intact");
        // All slots use the same four exact cost steps and reject Lv6. Test coins only.
        doc=ProgressionService.Read().data;doc.permanent.coins=2100;
        foreach(var item in doc.permanent.equipment) item.equippedSlot=-1;
        Assert(ProgressionService.Store.Write(doc,out _),"Isolated growth budget2100");
        foreach(string kind in new[]{"hat","outfit","weapon"})
        {
            string id=Profile().equipment.First(e=>e.dataId=="prototype-"+kind+"-01").instanceId;
            Assert(ProgressionService.ChangeEquipment(id,true),"Equip "+kind);
            for(int level=1;level<5;level++) {int coins=Profile().coins;Assert(ProgressionService.Upgrade(id) && Profile().coins==coins-ProgressionService.UpgradeCost(level),"Exact level cost "+kind+level);}
            before=Bytes();Assert(!ProgressionService.Upgrade(id) && Bytes()==before,"Lv5 cap no charge "+kind);
        }
        Assert(Profile().coins==0,"Three Lv5 upgrades consume2100 total");Near(ProgressionService.Stats().maxHP,175,"All Lv5 HP25+50");Near(ProgressionService.Stats().attack,1.5f,"Lv5 weapon coefficient1.5");
        string replacement=Profile().equipment.Last(e=>e.dataId=="prototype-hat-01").instanceId;
        Assert(ProgressionService.ChangeEquipment(replacement,true) && Profile().equipment.Count(e=>e.equippedSlot==0)==1,"Replacement clears previous same slot atomically");
        // Interrupted atomic replacement leaves pending clear, then recovery pays once.
        SaveManager.TestDirectoryOverride=Path.Combine(root,"interrupted-reward");
        doc=new SaveDocument();doc.pendingClears.Add(new PendingClear{runId="interrupted",stageId="prototype-stage-01"});Assert(ProgressionService.Store.Write(doc,out _),"Pending before interruption");
        Assert(ProgressionService.ApplyReward("interrupted"),"Prepare actual paid document");var paid=ProgressionService.Read().data;
        Assert(ProgressionService.Store.Write(doc,out _),"Reset isolated fixture to pre-payout boundary");
        Assert(!ProgressionService.Store.Write(paid,out _,()=>throw new IOException("Injected interruption after flush")),"Interrupted replacement refuses commit");
        Assert(Profile().coins==0 && ProgressionService.Read().data.pendingClears.Count==1,"Coherent pending document after interruption");
        ProgressionService.RecoverPending();before=Bytes();Assert(Profile().coins==100 && Profile().equipment.Count==1 && !ProgressionService.ApplyReward("interrupted") && Bytes()==before,"Interrupted payout recovery exactly once");
        // A future save remains read-only even when a reward is pending elsewhere.
        SaveManager.TestDirectoryOverride=Path.Combine(root,"future");Directory.CreateDirectory(SaveManager.StorageDirectory);File.WriteAllText(ProgressionService.Store.FilePath,"{\"schemaVersion\":99}");before=Bytes();
        ProgressionService.RecoverPending();Assert(!ProgressionService.Upgrade("anything") && Bytes()==before,"Future schema preserved");
        SaveManager.TestDirectoryOverride=play;Pass("Reward branches, coin filter, stable rolls, pending recovery, duplicate and invalid transaction cases");
    }
    static IEnumerator Sequence()
    {
        View("SetGameViewSize",1080,1920);yield return .4f;Assert(Screen.width==1080 && Screen.height==1920,"Actual 1080x1920 graphics");
        root=File.ReadAllText(Path.Combine(Output,"active-root.txt"));saveHashes=Saves();
        previousMouse=Mouse.current;mouseWasEnabled=previousMouse!=null && previousMouse.enabled;if(mouseWasEnabled) InputSystem.DisableDevice(previousMouse);
        mouse=InputSystem.AddDevice<Mouse>("NyanLinkSession07Mouse");mouse.MakeCurrent();
        if(SessionState.GetBool(Key+".Cold",false))
        {
            var p=Profile();Assert(p.coins==0 && p.equipment.Count>=3 && p.equipment.Single(e=>e.dataId=="prototype-hat-01").level==5,"Cold disk profile coins and Lv5");
            string before=Bytes();ProgressionService.RecoverPending();foreach(var receipt in p.rewardReceipts) Assert(!ProgressionService.ApplyReward(receipt),"Cold repeated receipt rejected");Assert(Bytes()==before,"Process restart does not pay twice or write");
            var panel=Object.FindFirstObjectByType<EquipmentPanel>();yield return Click(panel.OpenButton);Capture("06-cold-equipment.png");yield return .15f;yield return Click(panel.CloseButton);
            yield return EnterGame();Assert(save.HasCurrentCheckpoint(),"Cold geared checkpoint compatible");
            save.CheckpointRestored+=cp=>{Near(StaminaManager.Instance.MaxStamina,135,"Cold effective HP");Near(game.RunStats.attack,1.1f,"Cold weapon coefficient");Time.timeScale=0;};
            yield return Click(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart"));
            Assert(game.CurrentState==GameState.BossPhase && board.GetTileCount()==39,"Cold geared run restores Boss and39 tiles");Capture("07-cold-geared-continue.png");yield return .15f;
            SceneLoader.LoadMainMenu();yield return Wait(()=>SceneManager.GetActiveScene().name=="Main","Cold return home");
            yield return EnterGame();panel=game.GetComponent<EquipmentPanel>();yield return Click(panel.OpenButton);string permanent=JsonUtility.ToJson(Profile());
            yield return Click(panel.NewSetupButton);
            Assert(!ProgressionService.Read().data.hasCheckpoint && JsonUtility.ToJson(Profile())==permanent,"Explicit new setup discards checkpoint only, retains coins/gear/clear/receipts");
            string hat=Profile().equipment.Single(e=>e.dataId=="prototype-hat-01").instanceId;
            yield return Click(panel.EquipButtons[hat]);Near(ProgressionService.Stats().maxHP,110,"Cold UI unequip persisted");
            yield return Click(panel.EquipButtons[hat]);Near(ProgressionService.Stats().maxHP,135,"Cold UI re-equip persisted");
            yield return Click(panel.CloseButton);
            Assert(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart").GetComponentInChildren<TMPro.TextMeshProUGUI>().text=="NEW GAME","Setup action refreshes ready label immediately");
            Assert(Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.text=="135/135"),"Ready HUD previews current equipment HP135");Capture("08-new-setup-preserved-gear.png");yield return .15f;
        }
        else
        {
            RewardCases();
            Assert(DataManager.Instance.GetCharacterData("prototype-cat-01")!=null && Resources.LoadAll<EquipmentData>("Data/Equipment").Length==3,"Stable character ID and exactly3 fixed equipment data");
            foreach(var d in Resources.LoadAll<EquipmentData>("Data/Equipment")) Assert(d.GetMaxLevel()==5 && d.levelStats.Count==5,"Five explicit levels "+d.equipmentId);
            yield return EnterGame();yield return Click(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart"));yield return BossEntry();
            while(boss.timeAttackPattern.IsRunning) yield return Match(boss.timeAttackPattern.RequiredColor.Value);
            yield return Wait(()=>boss.IsGroggy,"Real mouse pattern opens groggy");
            // HP fixture permits a real final attack to trigger the normal single clear path.
            boss.ApplyDamage(boss.CurrentHP-1);yield return Match(TileColor.Red);
            Assert(game.CurrentResult==ResultType.Win && game.CurrentState==GameState.Result,"Real attack -> win Result");
            Assert(Profile().coins==100 && Profile().equipment.Count==1 && Profile().clears.Single().firstRewardGranted,"Actual clear pays100 and first hat");
            string first=save.CurrentRunId;string before=Bytes();game.TriggerStageClear();save.CompleteCurrentRun();GameEvents.TriggerResultSet(ResultType.Win);ProgressionService.RecoverPending();
            Assert(Bytes()==before && !ProgressionService.ApplyReward(first),"Duplicate victory/completion/result/recovery does not pay again");
            Capture("01-first-clear-reward.png");yield return .15f;
            var panel=game.GetComponent<EquipmentPanel>();yield return Click(panel.OpenButton);string hat="reward-"+first;
            yield return Click(panel.EquipButtons[hat]);Near(ProgressionService.Stats().maxHP,105,"Equipped hat actual effective HP");
            yield return Click(panel.UpgradeButtons[hat]);Assert(Profile().coins==0 && Profile().equipment.Single().level==2,"Actual UI upgrade costs100 and persistsLv2");
            Capture("02-equipped-upgrade.png");yield return .15f;yield return Click(panel.CloseButton);
            yield return Click(Button(Object.FindFirstObjectByType<StageClearPanel>(),"buttonContinue"));Near(StaminaManager.Instance.MaxStamina,110,"Retry uses Lv2 effective HP");
            before=Bytes();Assert(!ProgressionService.Upgrade(hat) && !ProgressionService.ChangeEquipment(hat,false) && Bytes()==before,"Live run cannot mutate loadout");
            // Six further runtime clears fund remaining upgrades exactly (150+200+250).
            for(int i=0;i<6;i++)
            {
                yield return BossEntry();
                // Choose deterministic IDs to cover outfit, weapon and no-drop, never give a second hat.
                int branch=i==0 ? 1 : i==1 ? 2 : 3;
                string id=Enumerable.Range(0,10000).Select(n=>"play-"+i+"-"+n).First(x=>{double roll=ProgressionService.Roll(x,save.stageData.stageId);return roll>=branch*.1 && roll<(branch<3 ? (branch+1)*.1 : 1);});
                typeof(SaveManager).GetProperty("CurrentRunId").SetValue(save,id);
                boss.ApplyDamage(999);Assert(Profile().coins==(i+1)*100,"Each distinct clear pays exactly100");
                if(i<5) game.RetryGame();yield return .04f;
            }
            Assert(Profile().equipment.Count==3 && Profile().rewardReceipts.Count==7,"First hat plus subsequent outfit/weapon, seven receipts");
            yield return Click(panel.OpenButton);
            foreach(int cost in new[]{150,200,250}) {int coins=Profile().coins;yield return Click(panel.UpgradeButtons[hat]);Assert(Profile().coins==coins-cost,"Actual UI cost "+cost);}
            Assert(Profile().coins==0 && Profile().equipment.Single(e=>e.instanceId==hat).level==5,"Lv5 exact700 total coins spent");
            before=Bytes();Assert(!ProgressionService.Upgrade(hat) && Bytes()==before,"MaxLv5 rejects extra charge");
            yield return Click(panel.EquipButtons[hat]);Near(ProgressionService.Stats().maxHP,100,"Unequip removes full HP bonus");yield return Click(panel.EquipButtons[hat]);
            string outfit=Profile().equipment.Single(e=>e.dataId=="prototype-outfit-01").instanceId, weapon=Profile().equipment.Single(e=>e.dataId=="prototype-weapon-01").instanceId;
            // API then fresh UI buttons; inventory supports scrolling for all owned items.
            Assert(ProgressionService.ChangeEquipment(outfit,true) && ProgressionService.ChangeEquipment(weapon,true),"All3 slots equipped");panel.Refresh();
            Near(ProgressionService.Stats().maxHP,135,"HatLv5 +25 and outfitLv1 +10");Near(ProgressionService.Stats().attack,1.1f,"Weapon adds .1 attack coefficient");
            Capture("03-three-slots-lv5.png");yield return .15f;yield return Click(panel.CloseButton);game.RetryGame();yield return BossEntry();
            while(boss.timeAttackPattern.IsRunning) yield return Match(boss.timeAttackPattern.RequiredColor.Value);yield return Wait(()=>boss.IsGroggy,"Geared groggy");
            StaminaManager.Instance.PauseDecay(7);float hp=boss.CurrentHP;yield return Match(TileColor.Red);Near(hp-boss.CurrentHP,4.4f,"Real equipped red attack 2*1.1*2");
            hp=boss.CurrentHP;boss.GrantAttackBuff(.5f);yield return Match(TileColor.Orange);Near(hp-boss.CurrentHP,13.2f,"Real orange with gear and50percent one-shot buff");
            Near(StaminaManager.Instance.MaxStamina,135,"Real maxHP135 in gameplay HUD");yield return .45f;Capture("04-geared-boss.png");yield return .15f;
            save.RequestBoundary(CheckpointBoundary.BossSuccess);yield return .12f;Assert(save.HasCurrentCheckpoint(),"Geared checkpoint saved and runtime compatible");
            game.PauseGame();SceneLoader.LoadMainMenu();yield return Wait(()=>SceneManager.GetActiveScene().name=="Main","Main after gear checkpoint");
            panel=Object.FindFirstObjectByType<EquipmentPanel>();yield return Click(panel.OpenButton);Assert(panel.NewSetupButton.gameObject.activeSelf,"Saved run offers explicit setup action");
            before=Bytes();Assert(!ProgressionService.ChangeEquipment(hat,false) && !ProgressionService.Upgrade(hat) && Bytes()==before,"Checkpoint protects loadout and bytes");
            Assert(panel.IsOpen && panel.transform.Find("EquipmentCanvas/EquipmentOverlay").GetComponent<RectTransform>().rect.height>=1900,"Saved-run equipment overlay full height");Capture("05-saved-loadout-locked.png");yield return .15f;yield return Click(panel.CloseButton);
            File.WriteAllText(Path.Combine(Output,"profile-example.json"),Bytes());
            Pass("Actual clear -> reward -> UI equip/upgrade Lv5 -> retry -> effective HP/attack -> checkpoint; cold fixture ready");
        }
        var after=Saves();Assert(saveHashes.Count==after.Count && saveHashes.All(p=>after.TryGetValue(p.Key,out var v)&&v==p.Value),"Original user saves byte-identical");
        CheckSubscribers();Pass(assertions+" Play assertions");
    }

    static void Finish(Exception error)
    {
        EditorApplication.update -= KeepAlive;
        if (driver != null) { driver.Tick = null; driver.LateTick = null; }
        if (error != null) results.Add(error.ToString());
        foreach (var message in errors) results.Add("RUNTIME_ERROR " + message);

        Application.logMessageReceived -= Log;
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        if (previousMouse != null && previousMouse.added)
        {
            if (mouseWasEnabled) InputSystem.EnableDevice(previousMouse);
            previousMouse.MakeCurrent();
        }
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
        if (hadBest) PlayerPrefs.SetInt(BestKey,oldBest); else PlayerPrefs.DeleteKey(BestKey);
        PlayerPrefs.Save();
        try { View("RestoreView"); } catch (Exception warning) { results.Add("View restore warning: " + warning.Message); }
        Time.timeScale = 1f;
        if (error == null && errors.Count == 0) Pass("Session07 completed without runtime errors");
        File.WriteAllLines(Path.Combine(Output,SessionState.GetBool(Key+".Cold",false)?"cold-results.txt":"play-results.txt"),results);
        File.WriteAllText(Path.Combine(Output,"environment.txt"),"Unity="+Application.unityVersion+"\npersistentDataPath="+Application.persistentDataPath+"\nisolatedStorage="+SaveManager.StorageDirectory+"\n");
        SaveManager.TestDirectoryOverride=null;SessionState.SetBool(Key+".Cold",false);
        SessionState.SetBool(Key,false);
        EditorApplication.Exit(error == null && errors.Count == 0 ? 0 : 1);
    }
}
