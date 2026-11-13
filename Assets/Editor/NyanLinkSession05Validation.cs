using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NineMood.Boss;
using NineMood.Core;
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

public static class NyanLinkSession05Validation
{
    const string Key = "NyanLink.Session05";
    const string Output = "output/playtests/2026-11-13-session05";
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
        EditorSceneManager.OpenScene("Assets/_NyanLink/Scene/Main.unity");
        SessionState.SetBool(Key,true);
        SessionState.SetBool(Key + ".HasBest",PlayerPrefs.HasKey(BestKey));
        SessionState.SetInt(Key + ".Best",PlayerPrefs.GetInt(BestKey));
        var view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Show(); view.Focus();
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (!SessionState.GetBool(Key,false)) return;
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
        if (EditorApplication.timeSinceStartup > expires) { Finish(new Exception("Session05 timeout")); return; }
        if (EditorApplication.isPlaying && driver == null)
        {
            driver = new GameObject("Session05 validation (temporary)").AddComponent<NyanLinkSession02PlayDriver>();
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
        Debug.Log("[Session05] " + results.Last());
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
            if (tile.Color != color) continue;
            foreach (var neighbor in HexOffsetUtils.GetNeighbors(cell))
            {
                var other = board.GetTileAtOffset(neighbor);
                if (other == null || other.Color != color) continue;
                a = cell; b = neighbor; found = true; break;
            }
            if (found) break;
        }
        Assert(found,"Adjacent color pair exists");
        Send(Point(a),true); yield return 0.04f;
        Send(Point(b),true); yield return 0.04f;
        Assert(input.SelectedChain.Count == 2,"Actual mouse drag selects pair");
        int scoreBefore = matchEvents;
        Send(Point(b),false);
        yield return Wait(() => matchEvents > scoreBefore,
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
    static void Configure()
    {
        game = GameManager.Instance;
        progress = Object.FindFirstObjectByType<RunnerProgressSystem>();
        spawner = Object.FindFirstObjectByType<ObstacleSpawner>();
        runner = Object.FindFirstObjectByType<RunnerController>();
        board = Object.FindFirstObjectByType<PuzzleBoardManager>();
        input = Object.FindFirstObjectByType<TileInputHandler>();
        camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c => c.name == "Camera_Puzzle");
        Assert(SaveManager.Instance.progressSystem == progress && SaveManager.Instance.obstacleSpawner == spawner,"Save scene references belong to current Game");
        SaveManager.Instance.stageData = null; // No reads/writes/deletion of user checkpoint files.
        if (stageClone != null) Object.Destroy(stageClone);
        stageClone = Object.Instantiate(progress.stageData);
        progress.stageData = spawner.stageData = StaminaManager.Instance.stageData = stageClone;
        Assert(stageClone.obstacles.Count == 3 && Mathf.Abs(stageClone.stageLength / stageClone.runnerSpeed - 50f) < 0.01f,"Three obstacles and 50-second baseline travel data");
        Assert(board.GetTileCount() == 39,"Reentered board initialized");
        Assert(Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1 &&
            Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length == 1 &&
            Object.FindObjectsByType<ScoreManager>(FindObjectsSortMode.None).Length == 1 &&
            Object.FindObjectsByType<StaminaManager>(FindObjectsSortMode.None).Length == 1 &&
            Object.FindObjectsByType<BossController>(FindObjectsSortMode.None).Length == 1,"One scene-owned singleton of each type");
        Assert(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1,"One EventSystem");
        CheckSubscribers();
    }
    static IEnumerator PauseSnapshot(string file)
    {
        yield return Click(game.GetComponent<GamePausePanel>().PauseButton);
        Assert(game.CurrentState == GameState.Paused && Time.timeScale == 0f,"Pause enters frozen state");
        float distance = progress.CurrentDistance, stamina = StaminaManager.Instance.CurrentStamina;
        float elapsed = StaminaManager.Instance.ElapsedRunnerTime, retry = spawner.RetryRemaining;
        float action = runner.ActionTimeRemaining, boost = runner.BoostTimeRemaining;
        var obstacle = spawner.CurrentObstacle;
        Vector3 position = obstacle != null ? obstacle.transform.position : Vector3.zero;
        string tiles = Fingerprint();
        if (file != null) Capture(file);
        yield return 0.45f;
        Assert(progress.CurrentDistance == distance && StaminaManager.Instance.CurrentStamina == stamina && StaminaManager.Instance.ElapsedRunnerTime == elapsed,"Paused progress/HP/elapsed time preserved");
        Assert(spawner.RetryRemaining == retry && runner.ActionTimeRemaining == action && runner.BoostTimeRemaining == boost,"Paused retry/action/boost timers preserved");
        Assert(Fingerprint() == tiles && (obstacle == null || (obstacle == spawner.CurrentObstacle && obstacle.transform.position == position)),"Paused board/obstacle instance and position preserved");
        yield return Click(game.GetComponent<GamePausePanel>().ResumeButton);
        Assert(game.CurrentState == game.StateBeforePause && Time.timeScale == 1f,"Resume restores exact previous phase");
    }
    static IEnumerator Approach()
    {
        Time.timeScale = 8f;
        yield return Wait(() => spawner.CurrentObstacle != null,"Obstacle spawn");
        Time.timeScale = 1f;
        Assert(progress.IsPaused && Mathf.Abs(progress.CurrentDistance - stageClone.obstacles[spawner.NextObstacleIndex].triggerDistance * progress.StageLength) < 0.001f,"Obstacle clamps progress at StageData trigger");
    }
    static float NaturalLoss(float from,float to)
    {
        float duration = to - from;
        var balance = DataManager.Instance.BalanceData;
        float growth = stageClone.decayIncreasePerSecond > 0f ? stageClone.decayIncreasePerSecond : balance.arcadeDecayIncreasePerSec;
        return balance.baseDecayRate * stageClone.stageDecayMultiplier * duration + growth * (from * duration + duration * duration * 0.5f);
    }
    static IEnumerator ExpectFailure()
    {
        float hp = StaminaManager.Instance.CurrentStamina, elapsed = StaminaManager.Instance.ElapsedRunnerTime;
        int index = spawner.NextObstacleIndex, count = failures;
        yield return Wait(() => spawner.RetryRemaining > 0f,"Retry after failed judgment");
        Assert(failures == count + 1 && spawner.NextObstacleIndex == index,"Failure fires once and does not advance obstacle index");
        float damage = hp - StaminaManager.Instance.CurrentStamina - NaturalLoss(elapsed,StaminaManager.Instance.ElapsedRunnerTime);
        Assert(Mathf.Abs(damage - 20f) < 0.2f,"Exactly 20 collision damage, natural decay accounted for: " + damage);
    }
    static void Failed() { failures++; }
    static void Cleared() { clears++; }
    static void BossAppeared() { bossEvents++; }
    static void Result(ResultType value) { resultEvents++; }

    static BossController boss;
    static TileMatcher matcher;
    static int matchEvents, itemEvents, activationEvents, shiftEvents;
    static TileColor lastShift;
    static readonly Dictionary<Vector3Int,int> activated=new Dictionary<Vector3Int,int>();
    static void Matched(int n,int s,int r,TileColor c) { matchEvents++; }
    static void ItemUsed(int score,int removed) { itemEvents++; }
    static void Activated(Vector3Int cell,ItemEffectType type,int level)
    {
        activationEvents++; activated[cell]=activated.TryGetValue(cell,out int n) ? n+1 : 1;
        Assert(board.GetTileCount()==39 && board.GetTileAtOffset(cell)!=null,"All effects read pre-removal board; no early refill");
    }
    static void Shifted(TileColor color) { lastShift=color; shiftEvents++; Assert(board.GetTileCount()==39,"Color shift occurs after complete refill"); }
    static void Near(float actual,float expected,string message,float tolerance=.12f) => Assert(Mathf.Abs(actual-expected)<tolerance,message+" actual="+actual+" expected="+expected);
    static readonly Vector3Int Center=new Vector3Int(2,3,0);
    static TileState Level(int level) => level==2 ? TileState.ItemLv2 : TileState.ItemLv1;
    static void Fill(TileColor color=TileColor.Red)
    {
        board.SetRequiredMatchColor(null);
        foreach(var cell in board.GetAllTileOffsets().ToArray()) board.SpawnTile(cell,color,TileState.Normal);
        activated.Clear(); activationEvents=0;
    }
    static void Fresh()
    {
        game.RetryGame(); Fill();
        StaminaManager.Instance.RestoreStamina(50,0);
    }
    static List<Vector3Int> Chain(int length,TileColor color)
    {
        var cells=new List<Vector3Int>();
        for(int x=0;x<6 && cells.Count<length;x++) cells.Add(new Vector3Int(x,0,0));
        for(int x=4;x>=0 && cells.Count<length;x--) cells.Add(new Vector3Int(x,1,0));
        foreach(var cell in cells) board.SpawnTile(cell,color,TileState.Normal);
        return cells;
    }
    static IEnumerator Drag(List<Vector3Int> cells)
    {
        yield return Wait(()=>!input.IsInputLocked,"Input unlock");
        foreach(var cell in cells) { Send(Point(cell),true); yield return .025f; }
        Assert(input.SelectedChain.Count==cells.Count,"Actual mouse selects chain "+cells.Count);
        Send(Point(cells.Last()),false); yield return .07f;
    }
    static IEnumerator Tap(Vector3Int cell)
    {
        Send(Point(cell),true); yield return .04f;
        Send(Point(cell),false); yield return .08f;
    }
    static IEnumerator Choose(TileColor color,bool nestedPause,string file=null)
    {
        var panel=game.GetComponent<ColorSelectionPanel>();
        Assert(panel.IsOpen && game.CurrentState==GameState.ColorSelection && Time.timeScale==0 && matcher.IsResolving,"Actual six-color modal pauses game and pending resolution");
        float hp=StaminaManager.Instance.CurrentStamina,elapsed=StaminaManager.Instance.ElapsedRunnerTime,distance=progress.CurrentDistance;
        float timer=boss.timeAttackPattern.RemainingTime,groggy=boss.GroggyRemaining,freeze=StaminaManager.Instance.DecayFreezeRemaining;
        var obstacle=spawner.CurrentObstacle; Vector3 pos=obstacle!=null ? obstacle.transform.position : Vector3.zero;
        string fingerprint=Fingerprint();
        if(file!=null) Capture(file);
        int before=matchEvents+itemEvents;
        Send(Point(new Vector3Int(0,0,0)),true);yield return .05f;Send(Point(new Vector3Int(0,0,0)),false);yield return .3f;
        Assert(before==matchEvents+itemEvents && input.SelectedChain.Count==0 && fingerprint==Fingerprint(),"Color selection blocks puzzle input");
        if(nestedPause)
        {
            yield return Click(game.GetComponent<GamePausePanel>().PauseButton);
            Assert(game.CurrentState==GameState.Paused && game.StateBeforePause==GameState.ColorSelection && !panel.IsOpen,"Manual pause nests above color selection");
            yield return .25f;
            yield return Click(game.GetComponent<GamePausePanel>().ResumeButton);
            Assert(panel.IsOpen && game.CurrentState==GameState.ColorSelection && Time.timeScale==0,"Resume returns same modal without advancing game");
        }
        Assert(hp==StaminaManager.Instance.CurrentStamina && elapsed==StaminaManager.Instance.ElapsedRunnerTime && distance==progress.CurrentDistance,"Modal preserves HP/time/progress");
        Assert(timer==boss.timeAttackPattern.RemainingTime && groggy==boss.GroggyRemaining && freeze==StaminaManager.Instance.DecayFreezeRemaining,"Modal and nested pause preserve boss/freeze clocks");
        Assert(obstacle==spawner.CurrentObstacle && (obstacle==null || obstacle.transform.position==pos),"Modal preserves obstacle instance/position");
        yield return Click(panel.ColorButtons[(int)color]);
        Assert(!panel.IsOpen && !matcher.IsResolving && Time.timeScale==1 && game.CurrentState==gameStateBeforeChoice,"Choice completes once and restores original phase");
    }
    static GameState gameStateBeforeChoice;
    static IEnumerator CompletePattern()
    {
        while(boss.timeAttackPattern.IsRunning) yield return Drag(Chain(2,boss.timeAttackPattern.RequiredColor.Value));
        yield return Wait(()=>boss.IsGroggy,"Groggy");
    }
    static IEnumerator Sequence()
    {
        View("SetGameViewSize",1080,1920);yield return .4f;
        Assert(Screen.width==1080 && Screen.height==1920,"Actual portrait Game View");
        saveHashes=Saves(); previousMouse=Mouse.current;mouseWasEnabled=previousMouse!=null && previousMouse.enabled;
        if(mouseWasEnabled) InputSystem.DisableDevice(previousMouse);
        mouse=InputSystem.AddDevice<Mouse>("NyanLinkSession05Mouse");mouse.MakeCurrent();
        GameEvents.OnMatchCompleted+=Matched;GameEvents.OnItemUseCompleted+=ItemUsed;
        ItemEffectEvents.OnItemActivated+=Activated;ItemEffectEvents.OnColorShift+=Shifted;
        GameEvents.OnResultSet+=Result;
        yield return Click(Button(Object.FindFirstObjectByType<MainMenuController>(),"buttonPlay"));
        yield return Wait(()=>SceneManager.GetActiveScene().name=="Game" && Object.FindFirstObjectByType<PuzzleBoardManager>()?.GetTileCount()==39,"Game initialized");
        Configure();boss=BossController.Instance;matcher=Object.FindFirstObjectByType<TileMatcher>();
        yield return Click(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart"));
        // Long chains only create items; generated items cannot trigger during the generating transaction.
        for(int color=0;color<6;color++)
        {
            foreach(int length in new[]{5,9})
            {
                Fresh(); var cells=Chain(length,(TileColor)color);int initialScore=ScoreManager.Instance.CurrentScore;
                yield return Drag(cells);
                var tile=board.GetTileAtOffset(cells.Last());
                Assert(tile.State==Level(length==5?1:2) && tile.ItemType==TileInstance.DefaultItemType((TileColor)color),"6x2 correct item generated at last cell");
                Assert(matcher.LastRemovedCount==length && matcher.LastActivatedCount==0,"Generated item not removed/activated in same transaction");
                Assert(ScoreManager.Instance.CurrentScore-initialScore==length*10*(length==5?2:4),"Creation chain tier score");
                Assert(board.GetTileCount()==39,"In-place full refill");
            }
            Fresh();
            board.SpawnTile(new Vector3Int(0,6,0),(TileColor)color,TileState.ItemLv1);
            board.SpawnTile(new Vector3Int(5,6,0),(TileColor)color,TileState.ItemLv2);
            yield return Drag(Chain(5,(TileColor)color));
            Assert(board.GetTileAtOffset(new Vector3Int(4,0,0)).State==TileState.Normal && board.CountItemsOfType(TileInstance.DefaultItemType((TileColor)color))==2,"Per-type cap2 skips creation only");
            Assert(matcher.LastRemovedCount==5 && ScoreManager.Instance.CurrentScore==100,"Capped chain still removes/scores");
        }
        // No global slot limit: ten other kinds do not prevent the eleventh item.
        Fresh();int at=0;var extra=board.GetAllTileOffsets().Where(c=>c.y>=3).ToArray();
        for(int color=0;color<6;color++) if(color!=(int)TileColor.Orange)
            for(int level=1;level<=2;level++) board.SpawnTile(extra[at++],(TileColor)color,Level(level));
        yield return Drag(Chain(5,TileColor.Orange));
        Assert(board.GetAllTileOffsets().Count(c=>board.GetTileAtOffset(c).State!=TileState.Normal)==11,"No global item slot cap");
        Capture("01-item-types-and-levels.png");yield return .12f;
        Pass("All six types generate Lv1/Lv2, last cell, no new-item recursion, per-type2 cap without global slots");
        // All twelve effects through actual single-click input, independent from color actions/pattern input.
        for(int color=0;color<6;color++) for(int level=1;level<=2;level++)
        {
            Fresh();board.SpawnTile(Center,(TileColor)color,Level(level));
            int shifts=shiftEvents,events=matchEvents,uses=itemEvents;
            var kept=new Vector3Int(5,0,0);
            if(color==(int)TileColor.Cyan) board.SpawnTile(kept,TileColor.Orange,TileState.ItemLv2);
            yield return Tap(Center);
            int expected=color==(int)TileColor.Yellow ? (level==1?7:19) : color==(int)TileColor.Purple ? (level==1?5:17) : 1;
            Assert(matcher.LastRemovedCount==expected && matcher.LastDirectCount==1 && matcher.LastActivatedCount==1,"6x2 exact footprint "+(TileColor)color+level+" removed="+matcher.LastRemovedCount);
            Assert(activated.Count==1 && activated.Values.Single()==1 && itemEvents==uses+1 && matchEvents==events,"Solo effect fires once, separate from match/color action");
            Assert(ScoreManager.Instance.CurrentScore==expected*10,"Solo has no chain tier bonus");
            if(color==(int)TileColor.Red) Near(StaminaManager.Instance.CurrentStamina,50+expected*.3f+(level==1?10:30),"Recovery10/30 percent plus ordinary recovery");
            if(color==(int)TileColor.Blue) Near(StaminaManager.Instance.DecayFreezeRemaining,level==1?3:7,"Freeze3/7",.25f);
            if(color==(int)TileColor.Orange) Near(boss.PendingAttackBonus,level==1?.5f:1f,"PowerUp50/100 percent");
            if(color==(int)TileColor.Cyan)
            {
                if(level==2) { gameStateBeforeChoice=GameState.RunnerPhase;yield return Choose(TileColor.Blue,true,"02-color-selection-runner.png"); }
                Assert(shiftEvents==shifts+1,"Color shift once");
                Assert(board.GetAllTileOffsets().All(c=>board.GetTileAtOffset(c).Color==lastShift),"Full board takes random/chosen color");
                Assert(board.GetTileAtOffset(kept).ItemType==ItemEffectType.PowerUp && board.GetTileAtOffset(kept).State==TileState.ItemLv2 && board.CountItemsOfType(ItemEffectType.PowerUp)==1,"Recolor preserves existing item kind/level and counts");
                yield return Tap(kept);Near(boss.PendingAttackBonus,1f,"Recolored item still activates original PowerUp effect");
                Capture("03-recolored-items.png");yield return .1f;
            }
            Assert(board.GetTileCount()==39,"Solo refill complete");
        }
        Pass("All 6x2 effects via single click: exact area, recovery/freeze/buff, color shift and retained identity, nested modal pause");
        // Overlapping blast/line recursively reaches all six kinds, each original source once.
        Fresh();board.SpawnTile(Center,TileColor.Yellow,TileState.ItemLv2);
        var placements=new[]{(new Vector3Int(2,2,0),TileColor.Purple,2),(new Vector3Int(1,3,0),TileColor.Red,1),(new Vector3Int(3,3,0),TileColor.Blue,2),(new Vector3Int(2,4,0),TileColor.Orange,1),(new Vector3Int(1,4,0),TileColor.Cyan,1)};
        foreach(var entry in placements) board.SpawnTile(entry.Item1,entry.Item2,Level(entry.Item3));
        yield return Tap(Center);
        Assert(matcher.LastActivatedCount==6 && activated.Count==6 && activated.Values.All(n=>n==1),"Overlapping recursive blast/line fires each original item once");
        Assert(ScoreManager.Instance.CurrentScore==matcher.LastRemovedCount*10,"Recursive removals scored uniquely");
        Near(StaminaManager.Instance.CurrentStamina,60+matcher.LastRemovedCount*.3f,"Recursive unique recovery plus one red item");
        Near(boss.PendingAttackBonus,.5f,"Recursive orange buff");Assert(StaminaManager.Instance.DecayFreezeRemaining>6,"Recursive blue freeze");
        Capture("04-chain-effects.png");yield return .1f;
        Fresh();var longChain=Chain(5,TileColor.Yellow);board.SpawnTile(longChain[0],TileColor.Yellow,TileState.ItemLv2);board.SpawnTile(new Vector3Int(1,1,0),TileColor.Yellow,TileState.ItemLv1);board.SpawnTile(new Vector3Int(0,1,0),TileColor.Purple,TileState.ItemLv2);
        yield return Drag(longChain);
        Assert(matcher.LastActivatedCount==3 && activated.Values.All(n=>n==1) && board.GetTileAtOffset(longChain.Last()).ItemType==ItemEffectType.Blast && board.GetTileAtOffset(longChain.Last()).State==TileState.ItemLv1,"Consumed sources free cap; new item survives complete recursive chain");
        Pass("Recursive overlap, unique removed set, all effects before refill, generated item survives chain");
        Fresh();board.SpawnTile(Center,TileColor.Yellow,TileState.ItemLv2);
        board.SpawnTile(new Vector3Int(1,3,0),TileColor.Cyan,TileState.ItemLv2);board.SpawnTile(new Vector3Int(3,3,0),TileColor.Cyan,TileState.ItemLv2);
        int shiftCount=shiftEvents;yield return Tap(Center);
        var chooser=game.GetComponent<ColorSelectionPanel>();
        yield return Click(chooser.ColorButtons[(int)TileColor.Red]);
        Assert(chooser.IsOpen && matcher.IsResolving && game.CurrentState==GameState.ColorSelection && Time.timeScale==0,"Two Lv2 color items keep one paused transaction across choices");
        yield return Click(chooser.ColorButtons[(int)TileColor.Blue]);
        Assert(!chooser.IsOpen && !matcher.IsResolving && shiftEvents==shiftCount+2 && game.CurrentState==GameState.RunnerPhase,"Each queued color item chooses once; resume only after final choice");
        // Freeze only stops natural decay and boss clocks, never input/progress/obstacles/animations.
        Fresh();board.SpawnTile(Center,TileColor.Blue,TileState.ItemLv1);yield return Tap(Center);
        float hp=StaminaManager.Instance.CurrentStamina,elapsed=StaminaManager.Instance.ElapsedRunnerTime,distance=progress.CurrentDistance;
        yield return .45f;
        Assert(StaminaManager.Instance.CurrentStamina==hp && StaminaManager.Instance.ElapsedRunnerTime>elapsed && progress.CurrentDistance>distance && Time.timeScale==1,"Freeze stops decay, keeps elapsed/progress");
        var animation=Object.FindFirstObjectByType<TileAnimation>();var matrixCell=new Vector3Int(0,0,0);board.SpawnTile(matrixCell,TileColor.Red,TileState.Normal);
        var matrix=board.tilemap.GetTransformMatrix(matrixCell);yield return .1f;
        Assert(board.tilemap.GetTransformMatrix(matrixCell)!=matrix,"Tile animation advances during freeze");
        yield return Drag(Chain(2,TileColor.Red));
        Assert(matcher.LastDirectCount==2,"Puzzle input works during freeze");
        board.SpawnTile(Center,TileColor.Blue,TileState.ItemLv1);yield return Tap(Center);Near(StaminaManager.Instance.DecayFreezeRemaining,3,"Reapply max not sum",.25f);
        board.SpawnTile(Center,TileColor.Blue,TileState.ItemLv2);yield return Tap(Center);float longer=StaminaManager.Instance.DecayFreezeRemaining;
        board.SpawnTile(Center,TileColor.Blue,TileState.ItemLv1);yield return Tap(Center);
        Assert(StaminaManager.Instance.DecayFreezeRemaining<longer && StaminaManager.Instance.DecayFreezeRemaining>6,"Short effect does not add to or shorten long remaining");
        progress.RestoreDistance(stageClone.obstacles[0].triggerDistance*progress.StageLength);yield return Wait(()=>spawner.CurrentObstacle!=null,"Obstacle during freeze");
        var obstacle=spawner.CurrentObstacle;Vector3 position=obstacle.transform.position;hp=StaminaManager.Instance.CurrentStamina;yield return .25f;
        Assert(obstacle.transform.position.x<position.x && StaminaManager.Instance.CurrentStamina==hp,"Obstacle moves while decay frozen");
        yield return PauseSnapshot("05-frozen-obstacle.png");
        // Color selection while an obstacle exists must not cancel/recreate it.
        board.SpawnTile(Center,TileColor.Cyan,TileState.ItemLv2);yield return Tap(Center);gameStateBeforeChoice=GameState.RunnerPhase;
        yield return Choose(TileColor.Red,true);
        Assert(spawner.CurrentObstacle==obstacle,"Selection resumes same obstacle");
        // Carry shared remaining freeze into boss, pattern input still works.
        spawner.RestoreCheckpoint(3);progress.RestoreDistance(progress.StageLength);game.TriggerBossFight();
        float remain=StaminaManager.Instance.DecayFreezeRemaining,timer=boss.timeAttackPattern.RemainingTime;yield return .25f;
        Assert(boss.TimerFreezeRemaining==StaminaManager.Instance.DecayFreezeRemaining && boss.TimerFreezeRemaining<remain && boss.timeAttackPattern.RemainingTime==timer,"Single remaining clock carries into boss; pattern timer frozen");
        board.SpawnTile(Center,TileColor.Cyan,TileState.ItemLv2);int usesBefore=itemEvents,matchBefore=matchEvents;int nextIndex=boss.timeAttackPattern.NextIndex;
        yield return Tap(Center);
        Assert(!game.GetComponent<ColorSelectionPanel>().IsOpen && itemEvents==usesBefore && matchEvents==matchBefore && boss.timeAttackPattern.NextIndex==nextIndex && board.GetTileAtOffset(Center).State==TileState.ItemLv2,"Solo item cannot trigger effects or pass boss pattern");
        Fill();var patternChain=Chain(9,boss.timeAttackPattern.RequiredColor.Value);
        board.SpawnTile(patternChain[0],boss.timeAttackPattern.RequiredColor.Value,TileState.ItemLv2);board.GetTileAtOffset(patternChain[0]).SetItemType(ItemEffectType.ColorShift);
        board.SpawnTile(patternChain[1],boss.timeAttackPattern.RequiredColor.Value,TileState.ItemLv1);board.GetTileAtOffset(patternChain[1]).SetItemType(ItemEffectType.Recovery);
        int score=ScoreManager.Instance.CurrentScore;hp=StaminaManager.Instance.CurrentStamina;yield return Drag(patternChain);
        Assert(matcher.LastRemovedCount==7 && matcher.LastActivatedCount==0 && board.GetTileAtOffset(patternChain[0]).State==TileState.ItemLv2 && board.GetTileAtOffset(patternChain[1]).State==TileState.ItemLv1,"Pattern protects original item identity and suppresses all effects");
        Assert(board.GetTileAtOffset(patternChain.Last()).State==TileState.Normal && ScoreManager.Instance.CurrentScore-score==280,"No new pattern item; only7 removed *10*4");
        Near(StaminaManager.Instance.CurrentStamina,Mathf.Min(100,hp+2.1f),"Pattern actual removal recovery only");
        var onlyItems=Chain(2,boss.timeAttackPattern.RequiredColor.Value);foreach(var cell in onlyItems) board.SpawnTile(cell,boss.timeAttackPattern.RequiredColor.Value,TileState.ItemLv1);
        score=ScoreManager.Instance.CurrentScore;hp=StaminaManager.Instance.CurrentStamina;yield return Drag(onlyItems);
        Assert(score==ScoreManager.Instance.CurrentScore && hp==StaminaManager.Instance.CurrentStamina && matcher.LastRemovedCount==0 && matcher.LastActivatedCount==0,"Preserved item chain cannot farm score/recovery/effects");
        yield return CompletePattern();
        timer=boss.GroggyRemaining;yield return .2f;Assert(boss.GroggyRemaining==timer,"Freeze includes groggy clock");
        board.SetRequiredMatchColor(null);board.SpawnTile(Center,TileColor.Cyan,TileState.ItemLv2);yield return Tap(Center);gameStateBeforeChoice=GameState.BossPhase;
        yield return Choose(TileColor.Yellow,true,"06-color-selection-groggy.png");
        Assert(boss.IsGroggy && boss.GroggyRemaining<=timer && boss.GroggyRemaining>timer-.3f,"Groggy choice resumes exact battle state");
        board.SpawnTile(Center,TileColor.Orange,TileState.ItemLv1);float bossHP=boss.CurrentHP;yield return Tap(Center);
        Assert(boss.CurrentHP==bossHP && boss.PendingAttackBonus==.5f,"Solo PowerUp grants next attack, no color attack");
        yield return Drag(Chain(2,TileColor.Orange));Near(boss.CurrentHP,bossHP-12,"Next Orange attack consumes50percent once");
        Assert(boss.PendingAttackBonus==0,"Attack bonus consumed");
        board.SpawnTile(Center,TileColor.Orange,TileState.ItemLv2);yield return Tap(Center);board.SpawnTile(Center,TileColor.Orange,TileState.ItemLv1);yield return Tap(Center);
        Assert(boss.PendingAttackBonus==1f,"Actual item overlap keeps higher buff");
        bossHP=boss.CurrentHP;yield return Drag(Chain(2,TileColor.Red));Near(boss.CurrentHP,bossHP-8,"100percent buff once");
        bossHP=boss.CurrentHP;yield return Drag(Chain(2,TileColor.Red));Near(boss.CurrentHP,bossHP-4,"Following attack unbuffed");
        Capture("07-items-groggy.png");yield return .1f;
        Pass("Shared freeze: decay only, elapsed/input/animation/obstacle movement, max duration, boss carry, pattern/groggy clocks; pattern item protection; actual buff consumption");
        boss.ApplyDamage(boss.CurrentHP-4f);
        var lethal=Chain(2,TileColor.Yellow);board.SpawnTile(lethal[0],TileColor.Yellow,TileState.ItemLv2);board.SpawnTile(new Vector3Int(0,1,0),TileColor.Cyan,TileState.ItemLv2);
        int resultsBefore=resultEvents;yield return Drag(lethal);
        Assert(game.CurrentState==GameState.Result && game.CurrentResult==ResultType.Win && resultEvents==resultsBefore+1 && !matcher.IsResolving && !game.GetComponent<ColorSelectionPanel>().IsOpen,"Lethal item chain cancels queued selection and wins exactly once");
        Capture("08-item-chain-victory.png");yield return .1f;
        game.RetryGame();yield return .1f;
        // Cancel a pending choice via restart and Home without callbacks or stale timeScale.
        board.SpawnTile(Center,TileColor.Cyan,TileState.ItemLv2);yield return Tap(Center);
        yield return Click(game.GetComponent<GamePausePanel>().PauseButton);yield return Click(game.GetComponent<GamePausePanel>().RestartButton);
        Assert(game.CurrentState==GameState.RunnerPhase && !matcher.IsResolving && StaminaManager.Instance.DecayFreezeRemaining==0 && boss.PendingAttackBonus==0 && Time.timeScale==1,"Restart cancels choice, pending effects and freeze");
        board.SpawnTile(Center,TileColor.Cyan,TileState.ItemLv2);yield return Tap(Center);
        yield return Click(game.GetComponent<GamePausePanel>().PauseButton);yield return Click(game.GetComponent<GamePausePanel>().HomeButton);
        yield return Wait(()=>SceneManager.GetActiveScene().name=="Main","Choice Home");
        Assert(GameManager.Instance==null && BossController.Instance==null && Time.timeScale==1,"Home clears choice services and restores timeScale");CheckSubscribers();
        var after=Saves();Assert(saveHashes.Count==after.Count && saveHashes.All(p=>after.TryGetValue(p.Key,out var value) && value==p.Value),"User save hashes unchanged");
        Pass("Choice restart/Home cleanup, no destroyed/duplicate subscribers, user saves preserved");
        Pass(assertions+" Play assertions");
    }

    static void Finish(Exception error)
    {
        EditorApplication.update -= KeepAlive;
        if (driver != null) { driver.Tick = null; driver.LateTick = null; }
        if (error != null) results.Add(error.ToString());
        foreach (var message in errors) results.Add("RUNTIME_ERROR " + message);
        GameEvents.OnBossAppeared -= BossAppeared; GameEvents.OnResultSet -= Result;
        GameEvents.OnMatchCompleted-=Matched;
        GameEvents.OnItemUseCompleted-=ItemUsed;
        ItemEffectEvents.OnItemActivated-=Activated; ItemEffectEvents.OnColorShift-=Shifted;
        ObstacleController.OnObstacleFailed -= Failed; ObstacleController.OnObstacleCleared -= Cleared;
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
        if (error == null && errors.Count == 0) Pass("Session05 completed without runtime errors");
        File.WriteAllLines(Path.Combine(Output,"play-results.txt"),results);
        SessionState.SetBool(Key,false);
        EditorApplication.Exit(error == null && errors.Count == 0 ? 0 : 1);
    }
}
