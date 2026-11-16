using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NineMood.Boss;
using NineMood.Core.Persistence;
using TMPro;
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

public static class NyanLinkGroggyLinkValidation
{
    const string Key = "NyanLink.GroggyLink";
    static readonly string Output = Environment.GetEnvironmentVariable("NYANLINK_ENCOUNTER_OUTPUT") ?? "output/playtests/2026-11-16-groggy-link/editor";
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
        EditorApplication.LockReloadAssemblies();
        SaveManager.TestDirectoryOverride = Path.GetFullPath(Path.Combine(Output, "isolated-saves"));
        oldBest = SessionState.GetInt(Key + ".Best",0);
        hadBest = SessionState.GetBool(Key + ".HasBest",false);
        oldBackground = InputSystem.settings.backgroundBehavior;
        oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Application.logMessageReceived += Log;
        expires = EditorApplication.timeSinceStartup + 210;
        next = EditorApplication.timeSinceStartup + 3;
        work = new Stack<IEnumerator>(); work.Push(Sequence());
        EditorApplication.update += KeepAlive;
    }
    static void KeepAlive()
    {
        Application.runInBackground = true;
        if (EditorApplication.timeSinceStartup > expires) { Finish(new Exception("Session03 timeout")); return; }
        if (EditorApplication.isPlaying && driver == null)
        {
            driver = new GameObject("Session03 validation (temporary)").AddComponent<NyanLinkSession02PlayDriver>();
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
        Debug.Log("[Encounter] " + results.Last());
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
    static void CheckInputZone(ObstacleController obstacle, bool open)
    {
        var hud = Object.FindFirstObjectByType<RunnerWarningHUD>();
        Assert(hud.InputZone.gameObject.activeSelf && hud.InputZone.color.a > 0 && hud.InputZone.color.a < .5f, "Translucent obstacle input zone");
        Assert(hud.warningPanel.activeSelf == !open && obstacle.IsInputWindowOpen == open, "Approach and input cues are distinct");
        Assert(hud.ZoneLabel.text == (open ? KoreanText.ColorName(obstacle.RequiredColor) + " 2개 이상 연결" : "영역에 들어오면 연결"), "Zone explains input timing");
        var corners = new Vector3[4]; hud.InputZone.rectTransform.GetWorldCorners(corners);
        var cameraRunner = Object.FindFirstObjectByType<DualCameraRig>().cameraRunner;
        var start = cameraRunner.WorldToScreenPoint(new Vector3(obstacle.InputWindowStartX, 0, 0));
        var end = cameraRunner.WorldToScreenPoint(new Vector3(obstacle.InputWindowEndX, 0, 0));
        Assert(Mathf.Abs(corners[0].x - Mathf.Clamp(start.x,cameraRunner.pixelRect.xMin,cameraRunner.pixelRect.xMax)) < 2 &&
            Mathf.Abs(corners[2].x - Mathf.Clamp(end.x,cameraRunner.pixelRect.xMin,cameraRunner.pixelRect.xMax)) < 2, "Zone matches actual judgment X bounds");
        Assert(Mathf.Abs(obstacle.InputWindowEndX - obstacle.InputWindowStartX - DataManager.Instance.BalanceData.obstacleWarningDistance) < .001f,
            "Moving the window preserves its full input width");
        var markerCorners = new Vector3[4]; hud.JudgmentMarker.rectTransform.GetWorldCorners(markerCorners);
        float judgmentX = cameraRunner.WorldToScreenPoint(new Vector3(obstacle.JudgmentX, 0, 0)).x;
        Assert(Mathf.Abs((markerCorners[0].x + markerCorners[2].x) * .5f - judgmentX) < 2 &&
            obstacle.InputWindowStartX > obstacle.JudgmentX, "Input zone is ahead of the separate action point");
        Assert(hud.InputZone.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g => !g.raycastTarget), "Cues do not intercept input");
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
        Assert(board.GetTileCount() > 0,"Reentered board initialized");
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
    static BossEncounter fight;
    static SaveManager save;
    static List<Vector3Int> PathFor(TileColor color,int count)
    {
        Assert(board.SetRequiredMatchPath(color,count),"Required path can be guaranteed "+count);
        Assert(board.TryGetMatchPath(color,count,out var path),"Required connected path exists "+count);
        Assert(path.Distinct().Count()==count && path.Zip(path.Skip(1),(a,b)=>HexOffsetUtils.IsAdjacent(a,b)).All(x=>x),"Path is distinct and adjacent");
        return path;
    }
    static IEnumerator DragNew(List<Vector3Int> path,string liveCapture=null)
    {
        foreach(var cell in path) { Send(Point(cell),true);yield return .025f; }
        Assert(input.SelectedChain.Count==path.Count,"Actual drag selects "+path.Count);
        if(liveCapture!=null)
        {
            yield return .05f;
            var hud=Object.FindFirstObjectByType<BossHUD>();
            var label=hud.GetComponentsInChildren<TextMeshProUGUI>().Single(t=>t.name=="Required link count");
            Assert(label.text.Contains($"{path.Count} / {fight.MinimumLinks}"),"Live count UI follows drag");
            Capture(liveCapture);yield return .05f;
        }
        Send(Point(path.Last()),false);yield return .05f;
    }
    static IEnumerator TapNew(Vector3Int cell)
    { Send(Point(cell),true);yield return .025f;Send(Point(cell),false);yield return .025f; }
    static IEnumerator NextInput()
    {
        Time.timeScale=1;
        yield return Wait(()=>fight.AcceptingInput || boss.IsGroggy || boss.IsDefeated,"Next response window");
        Time.timeScale=0;
    }
    static IEnumerator Respond(int count=0,string capture=null)
    {
        yield return NextInput(); Assert(fight.AcceptingInput,"Response input open");
        var step=fight.Step; int required=fight.MinimumLinks;
        float before=boss.CurrentHP;
        var path=PathFor(step.color,count==0 ? required:count);
        yield return DragNew(path,capture);
        Assert(!fight.AcceptingInput && fight.LastResponseSucceeded,"Valid release immediately resolves response");
        float expected=step.action==BossResponseAction.Attack ? path.Count*step.damagePerLink*game.RunStats.attack : 0;
        Assert(Mathf.Abs(before-boss.CurrentHP-expected)<.001f,"Only attack action deals linked-count damage");
        Assert(runner.CurrentState==(step.action==BossResponseAction.Attack ? RunnerState.Attack : step.action==BossResponseAction.Slide ? RunnerState.Slide : RunnerState.Block),"Player response follows asset action");
    }
    static IEnumerator BundleToFever()
    {
        while(!boss.IsGroggy && !boss.IsDefeated)
        {
            yield return NextInput();
            if(fight.AcceptingInput) yield return Respond();
        }
        Time.timeScale=0;
    }
    static IEnumerator EndFever()
    {
        Time.timeScale=4;yield return Wait(()=>!boss.IsGroggy,"Fever expires");Time.timeScale=0;
    }
    static bool SameTiles(IEnumerable<TileRecord> records) => records.All(r=> {
        var t=board.GetTileAtOffset(new Vector3Int(r.x,r.y,0));
        return t!=null && t.Color==r.color && t.State==r.state && t.ItemType==r.itemType; });
    static IEnumerator Sequence()
    {
        View("SetGameViewSize",1080,1920);yield return .4f;
        foreach(var source in new[]{"Builds/AndroidDeviceBackups/2026-11-16-test-mode/profile-v1.json","output/playtests/2026-11-13-session06/sandbox-ed94e6b8b65448a5aa103edcf518bcb7/play/profile-v1.json"}) {
            var folder=Path.Combine(Output,"compatibility",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            File.Copy(source,Path.Combine(folder,"profile-v1.json"));var store=new SaveStore(folder);var loaded=store.Load();
            Assert(loaded.status==SaveReadStatus.Ready,"Legacy save checksum readable: "+source);
            if(loaded.data.checkpoint!=null)loaded.data.checkpoint.boss.groggyGauge=65;
            Assert(store.Write(loaded.data,out _),"New gauge save writes");Assert(store.Load().status==SaveReadStatus.Ready,"New gauge checksum reloads");
            if(loaded.data.checkpoint!=null)Assert(store.Load().data.checkpoint.boss.groggyGauge==65,"Gauge persists on disk");
        }

        previousMouse=Mouse.current;mouseWasEnabled=previousMouse!=null && previousMouse.enabled;
        if(mouseWasEnabled)InputSystem.DisableDevice(previousMouse);
        mouse=InputSystem.AddDevice<Mouse>("GroggyLinksMouse");mouse.MakeCurrent();
        yield return Click(Button(Object.FindFirstObjectByType<MainMenuController>(),"buttonPlay"));
        yield return Wait(()=>SceneManager.GetActiveScene().name=="Game" && Object.FindFirstObjectByType<PuzzleBoardManager>()?.GetTileCount()>0,"Game ready");
        Configure();boss=BossController.Instance;fight=boss.Encounter;
        var original=boss.config; var config=Object.Instantiate(original);config.definition=Object.Instantiate(original.definition);
        var response=config.definition.phases[0].patterns[0];var choice=config.definition.phases[0].patterns[1];
        Assert(config.definition.phases.All(p=>p.patterns.Length==2),"Only response and choice active");
        foreach(var phase in config.definition.phases) phase.patterns=new[]{response};boss.config=config;
        game.RetryGame();spawner.RestoreCheckpoint(3);progress.RestoreDistance(progress.StageLength);game.TriggerBossFight();Time.timeScale=0;
        for(int bundle=0;bundle<3;bundle++) {
            for(int step=0;step<3;step++) { yield return DragNew(PathFor(fight.Step.color,fight.MinimumLinks));fight.Tick(.3f); }
            if(bundle==0) {Assert(fight.GroggyGauge==35 && !fight.IsFever,"First response fills35 without groggy");Capture("01-gauge-35.png");yield return .05f;
                var record=fight.Capture();fight.Restore(record);Assert(fight.GroggyGauge==35,"Gauge checkpoint roundtrip");}
            fight.Tick(1);
        }
        Assert(fight.IsFever && fight.GroggyGauge==0,"Three successes trigger groggy and reset gauge");
        Assert(board.GetAllTileOffsets().All(c=>board.GetTileAtOffset(c).State==TileState.Normal && (board.GetTileAtOffset(c).Color==TileColor.Orange || board.GetTileAtOffset(c).Color==TileColor.Purple)),"Two-color normal attack board");
        float hp=boss.CurrentHP;Assert(!fight.TryFeverTap(board.GetAllTileOffsets().First()) && boss.CurrentHP==hp,"Single taps cannot attack");
        yield return DragNew(PathFor(TileColor.Orange,2));Assert(boss.CurrentHP==hp-2,"Two link damage2");
        hp=boss.CurrentHP;yield return DragNew(PathFor(TileColor.Purple,5));Assert(boss.CurrentHP==hp-10,"Five link damage10");
        Capture("02-two-color-link-attack.png");yield return .05f;
        var tiles=fight.CheckpointTiles();fight.Tick(8);Assert(!fight.IsFever && SameTiles(tiles),"Normal board restored after groggy");
        foreach(var phase in config.definition.phases)phase.patterns=new[]{choice};
        game.RetryGame();spawner.RestoreCheckpoint(3);progress.RestoreDistance(progress.StageLength);game.TriggerBossFight();Time.timeScale=0;
        yield return DragNew(PathFor(TileColor.Blue,3));fight.Tick(.3f);Assert(fight.GroggyGauge==35,"Choice evade35");fight.Tick(1);
        float gauge=fight.GroggyGauge;fight.Tick(20);fight.Tick(.3f);Assert(fight.GroggyGauge==gauge,"Failure preserves accumulated gauge");fight.Tick(1);
        hp=boss.CurrentHP;yield return DragNew(PathFor(TileColor.Orange,5));fight.Tick(.3f);Assert(fight.GroggyGauge==85 && boss.CurrentHP==hp-5,"Choice attack50 and damage5");
        Capture("03-choice-gauge-85.png");yield return .05f;
        Pass(assertions+" assertions: gauge, restore, failure retention, two-color link damage and board restoration");
    }
    static void Finish(Exception error)
    {
        EditorApplication.update -= KeepAlive;
        if (driver != null) { driver.Tick = null; driver.LateTick = null; }
        if (error != null) results.Add(error.ToString());
        foreach (var message in errors) results.Add("RUNTIME_ERROR " + message);
        GameEvents.OnBossAppeared -= BossAppeared; GameEvents.OnResultSet -= Result;
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
        Time.timeScale = 1f; PrototypeSafeArea.TestPixels = null;
        if (error == null && errors.Count == 0) Pass("Encounter completed without runtime errors");
        File.WriteAllLines(Path.Combine(Output,"play-results.txt"),results);
        SessionState.SetBool(Key,false);
        EditorApplication.UnlockReloadAssemblies();
        EditorApplication.Exit(error == null && errors.Count == 0 ? 0 : 1);
    }
}
