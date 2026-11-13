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

public static class NyanLinkSession04Validation
{
    const string Key = "NyanLink.Session04";
    static readonly string Output = Environment.GetEnvironmentVariable("NYANLINK_SESSION04_OUTPUT") ?? "output/playtests/2026-11-13-session04";
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
        expires = EditorApplication.timeSinceStartup + 210;
        next = EditorApplication.timeSinceStartup + 3;
        work = new Stack<IEnumerator>(); work.Push(Sequence());
        EditorApplication.update += KeepAlive;
    }
    static void KeepAlive()
    {
        Application.runInBackground = true;
        if (EditorApplication.timeSinceStartup > expires) { Finish(new Exception("Session04 timeout")); return; }
        if (EditorApplication.isPlaying && driver == null)
        {
            driver = new GameObject("Session04 validation (temporary)").AddComponent<NyanLinkSession02PlayDriver>();
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
        Debug.Log("[Session04] " + results.Last());
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
    static int successfulPatterns, failedPatterns, phaseEvents, hpEvents, matchEvents;
    static void Matched(int n,int score,int removed,TileColor color) { matchEvents++; }
    static void PatternResult(bool success) { if (success) successfulPatterns++; else failedPatterns++; }
    static void PhaseChanged(int phase) { phaseEvents++; }
    static void HPChanged(float current,float max) { hpEvents++; }
    static void Near(float actual,float expected,string label,float tolerance=0.05f) => Assert(Mathf.Abs(actual-expected)<tolerance,label+" actual="+actual+" expected="+expected);
    static void BeginIsolatedBattle()
    {
        if (bossClone != null) Object.Destroy(bossClone);
        bossClone = Object.Instantiate(boss.config);
        boss.config = bossClone;
        typeof(BossController).GetMethod("StartBattle",PrivateInstance).Invoke(boss,null);
    }
    static List<Vector3Int> Chain(int length,TileColor color)
    {
        // A known continuous odd-r path across the first two rows.
        var cells = new List<Vector3Int>();
        for (int x=0;x<6 && cells.Count<length;x++) cells.Add(new Vector3Int(x,0,0));
        for (int x=4;x>=0 && cells.Count<length;x--) cells.Add(new Vector3Int(x,1,0));
        Assert(cells.Count==length,"Known path length");
        foreach (var cell in cells) board.SpawnTile(cell,color,TileState.Normal);
        return cells;
    }
    static IEnumerator Drag(List<Vector3Int> cells)
    {
        yield return Wait(()=>!input.IsInputLocked,"Unlocked before drag");
        foreach (var cell in cells) { Send(Point(cell),true); yield return 0.025f; }
        Assert(input.SelectedChain.Count==cells.Count,"Actual mouse selects full chain "+cells.Count);
        Send(Point(cells.Last()),false); yield return 0.06f;
    }
    static IEnumerator CompletePattern()
    {
        yield return Wait(()=>boss.IsPatternActive && boss.timeAttackPattern.IsRunning,"Next pattern ready");
        while (boss.timeAttackPattern.IsRunning)
        {
            var required=boss.timeAttackPattern.RequiredColor.Value;
            Assert(board.TryEnsureAdjacentPair(required),"Required color available before each input");
            yield return Match(required);
        }
        yield return Wait(()=>boss.IsGroggy,"Groggy opens");
    }
    static IEnumerator BossPause(string screenshot)
    {
        yield return Click(game.GetComponent<GamePausePanel>().PauseButton);
        float hp=boss.CurrentHP, timer=boss.timeAttackPattern.RemainingTime, groggy=boss.GroggyRemaining, freeze=boss.TimerFreezeRemaining;
        int phase=boss.PhaseIndex,index=boss.timeAttackPattern.NextIndex;
        string tiles=Fingerprint();
        if (screenshot!=null) Capture(screenshot);
        yield return 0.65f;
        Assert(boss.CurrentHP==hp && boss.GroggyRemaining==groggy && boss.timeAttackPattern.RemainingTime==timer && boss.TimerFreezeRemaining==freeze,"Boss pause freezes all clocks and HP");
        Assert(boss.PhaseIndex==phase && boss.timeAttackPattern.NextIndex==index && Fingerprint()==tiles,"Boss pause preserves sequence, phase and board");
        boss.ApplyDamage(999f); Assert(boss.CurrentHP==hp,"Damage rejected while paused");
        yield return Click(game.GetComponent<GamePausePanel>().ResumeButton);
        Assert(game.CurrentState==GameState.BossPhase && boss.CurrentHP==hp && boss.PhaseIndex==phase,"Resume keeps exact boss state");
    }
    static IEnumerator Sequence()
    {
        View("SetGameViewSize",1080,1920); yield return 0.4f;
        Assert(Screen.width==1080 && Screen.height==1920,"Runtime portrait resolution");
        saveHashes=Saves();
        previousMouse=Mouse.current; mouseWasEnabled=previousMouse!=null && previousMouse.enabled;
        if(mouseWasEnabled) InputSystem.DisableDevice(previousMouse);
        mouse=InputSystem.AddDevice<Mouse>("NyanLinkSession04Mouse"); mouse.MakeCurrent();
        GameEvents.OnMatchCompleted+=Matched;
        GameEvents.OnBossAppeared+=BossAppeared; GameEvents.OnResultSet+=Result;
        GameEvents.OnBossPatternResult+=PatternResult; GameEvents.OnBossPhaseChanged+=PhaseChanged; GameEvents.OnBossHPChanged+=HPChanged;
        // Independent numeric examples, including large secondary removal counts.
        var balance=DataManager.Instance.BalanceData;
        foreach(var pair in new[]{(2,1),(4,1),(5,2),(8,2),(9,4),(39,4)})
            Assert(balance.GetChainMultiplier(pair.Item1)==pair.Item2,"Tier boundary "+pair.Item1);
        Near(balance.GetPrototypeGroggyDamage(5,TileColor.Orange,2f,.5f),30f,"Spec example 5*1*2*2*1.5");
        Near(balance.GetPrototypeGroggyDamage(9,TileColor.Red,2f,0f),18f,"Direct 9, no combo or critical");
        Near(balance.GetPrototypeGroggyDamage(5,TileColor.Cyan,2f,1f),0f,"Cyan no damage");
        Pass("Chapter 07 numeric examples and tier boundaries");
        yield return Click(Button(Object.FindFirstObjectByType<MainMenuController>(),"buttonPlay"));
        yield return Wait(()=>SceneManager.GetActiveScene().name=="Game" && Object.FindFirstObjectByType<PuzzleBoardManager>()?.GetTileCount()==39,"Game ready");
        Configure(); boss=BossController.Instance;
        Assert(boss.enableCombat && boss.config.maxHP==120 && boss.config.timeAttackSequenceLength==3 && boss.config.inputTimeLimit==10,"Production boss baseline data and activation");
        Assert(boss.rhythmNotePattern==null && boss.colorQuotaPattern==null && boss.memorySequencePattern==null,"Deferred patterns detached");
        yield return Click(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart"));
        StaminaManager.Instance.RestoreStamina(50,0); ScoreManager.Instance.ResetScore();
        ScoreManager.Instance.AddScore(5,2,11,TileColor.Red);
        Assert(ScoreManager.Instance.CurrentScore==220,"Five direct, eleven unique removals score220 regardless of old effect argument");
        Near(StaminaManager.Instance.CurrentStamina,53.3f,"Eleven removals recovery3.3");
        ScoreManager.Instance.AddScore(9,0,11,TileColor.Red);
        Assert(ScoreManager.Instance.CurrentScore==660,"Nine direct tier4 scores440 with no continuous combo");
        Near(StaminaManager.Instance.CurrentStamina,56.6f,"Repeated independent recovery");
        StaminaManager.Instance.ResetStamina(); ScoreManager.Instance.ResetScore();
        boss.GrantAttackBuff(.5f); boss.GrantAttackBuff(.2f);
        yield return Drag(Chain(2,TileColor.Cyan));
        Assert(boss.PendingAttackBonus==.5f && boss.HasFailureShield,"Runner grants max buff and cyan failure shield");
        yield return PauseSnapshot(null);
        Assert(boss.PendingAttackBonus==.5f && boss.HasFailureShield,"Runner resume preserves pending boss effects");
        // Natural obstacle ownership/entry path, accelerated only between input windows.
        foreach(var color in new[]{TileColor.Red,TileColor.Blue,TileColor.Yellow})
        {
            yield return Approach(); yield return Wait(()=>spawner.CurrentObstacle.IsInputWindowOpen,"Obstacle input window");
            int count=spawner.ClearedObstacleCount; yield return Match(color);
            yield return Wait(()=>spawner.ClearedObstacleCount==count+1,"Obstacle success");
        }
        Time.timeScale=8f; yield return Wait(()=>game.CurrentState==GameState.BossPhase,"Natural boss entry"); Time.timeScale=1f;
        Near(boss.CurrentHP,120,"No pattern success fixed damage at entry");
        Assert(boss.PendingAttackBonus==.5f && boss.HasFailureShield,"Pending runner effects survive boss entry");
        Assert(bossEvents==1 && boss.IsPatternActive && boss.timeAttackPattern.Sequence.Count==3,"One boss entry and time-attack only");
        float player=StaminaManager.Instance.CurrentStamina, elapsed=StaminaManager.Instance.ElapsedRunnerTime;
        Capture("01-timeattack-sequence.png"); yield return 0.2f;
        Near(StaminaManager.Instance.CurrentStamina,player,"No boss natural decay");
        Assert(StaminaManager.Instance.ElapsedRunnerTime==elapsed,"Runner elapsed stops in boss");
        yield return BossPause("02-paused-pattern.png");
        // Pattern items are protected and cannot produce effects/new items.
        var expected=boss.timeAttackPattern.RequiredColor.Value;
        var protectedChain=Chain(5,expected); board.SpawnTile(protectedChain[0],expected,TileState.ItemLv1);
        int score=ScoreManager.Instance.CurrentScore; float hp=StaminaManager.Instance.CurrentStamina;
        yield return Drag(protectedChain);
        Assert(board.GetTileAtOffset(protectedChain[0]).State==TileState.ItemLv1,"Existing item survives pattern chain");
        Assert(protectedChain.Skip(1).All(c=>board.GetTileAtOffset(c).State==TileState.Normal),"No new pattern item");
        Assert(ScoreManager.Instance.CurrentScore-score==80,"5 direct chain, 4 actual removal *10*2");
        Near(StaminaManager.Instance.CurrentStamina,Mathf.Min(100,hp+1.2f),"Only actual removal heals");
        Near(boss.CurrentHP,120,"Pattern inputs do not attack or grant fixed success damage");
        var itemOnly=Chain(2,boss.timeAttackPattern.RequiredColor.Value);
        foreach(var cell in itemOnly) board.SpawnTile(cell,boss.timeAttackPattern.RequiredColor.Value,TileState.ItemLv1);
        score=ScoreManager.Instance.CurrentScore; hp=StaminaManager.Instance.CurrentStamina;
        int nextIndex=boss.timeAttackPattern.NextIndex;
        yield return Drag(itemOnly);
        Assert(ScoreManager.Instance.CurrentScore==score && StaminaManager.Instance.CurrentStamina==hp,"Preserved items yield zero score/recovery");
        Assert(boss.timeAttackPattern.NextIndex==nextIndex+1 && itemOnly.All(c=>board.GetTileAtOffset(c).State==TileState.ItemLv1),"Two-item chain is pattern input, items preserved");
        yield return CompletePattern();
        Near(boss.CurrentHP,120,"Pattern success has zero fixed damage");
        Assert(boss.PendingAttackBonus==.5f,"Pattern success does not consume pending attack buff");
        Assert(successfulPatterns==1 && failedPatterns==0 && boss.CanUseItems && !boss.PreserveItems,"Groggy opens item/action boundary");
        Capture("03-groggy-attack.png"); yield return 0.08f;
        // Next valid attack consumes maximum bonus once; cyan never consumes it.
        boss.GrantAttackBuff(.5f); boss.GrantAttackBuff(1f); boss.GrantAttackBuff(.5f);
        Near(boss.PendingAttackBonus,1f,"Buff max, no stacking");
        yield return Drag(Chain(2,TileColor.Cyan));
        Assert(boss.HasFailureShield && boss.PendingAttackBonus==1f && boss.CurrentHP==120,"Cyan shield, zero damage, buff retained");
        yield return Drag(Chain(5,TileColor.Orange));
        Near(boss.CurrentHP,80,"5 Orange with max buff does 40"); Near(boss.PendingAttackBonus,0,"Buff consumed once");
        yield return Drag(Chain(2,TileColor.Red)); Near(boss.CurrentHP,76,"Next attack unbuffed direct count");
        var blastChain=Chain(2,TileColor.Yellow); board.SpawnTile(blastChain[0],TileColor.Yellow,TileState.ItemLv1);
        int actualRemoved=-1, direct=-1;
        Action<int,int,int,TileColor> observe=(n,g,r,c)=>{ actualRemoved=r; direct=n; };
        GameEvents.OnMatchCompleted+=observe; hp=StaminaManager.Instance.CurrentStamina; score=ScoreManager.Instance.CurrentScore;
        yield return Drag(blastChain); GameEvents.OnMatchCompleted-=observe;
        Assert(direct==2 && actualRemoved>2,"Blast secondary removal is distinct from direct connection");
        Near(boss.CurrentHP,72,"Blast chain attacks with 2 direct tiles only");
        Assert(ScoreManager.Instance.CurrentScore-score==actualRemoved*10,"Secondary removals count toward score once");
        Near(StaminaManager.Instance.CurrentStamina,Mathf.Min(100,hp+actualRemoved*.3f),"Secondary removals count toward recovery once");
        yield return BossPause("04-paused-groggy.png");
        boss.FreezeTimersFor(.7f); boss.FreezeTimersFor(.2f);
        float remaining=boss.GroggyRemaining; yield return 0.3f;
        Near(boss.GroggyRemaining,remaining,"Time freeze leaves groggy timer unchanged");
        Assert(boss.TimerFreezeRemaining>0 && boss.TimerFreezeRemaining<.7f,"Freeze duration max not sum");
        yield return Wait(()=>!boss.TimersFrozen,"Freeze expires");
        boss.ApplyDamage(20f); Near(boss.CurrentHP,52,"Unified external damage applies immediately");
        Assert(boss.PhaseIndex==0,"Crossed threshold waits for groggy end");
        Time.timeScale=8f; yield return Wait(()=>boss.PhaseIndex==1,"Phase change after groggy end"); Time.timeScale=1f;
        Assert(!boss.IsGroggy,"Phase change after groggy only");
        yield return Wait(()=>boss.timeAttackPattern.IsRunning,"Enhanced pattern");
        Assert(boss.timeAttackPattern.Sequence.Count==4 && boss.timeAttackPattern.RemainingTime<=9f && boss.timeAttackPattern.RemainingTime>8f,"Phase1 length4 and 9sec");
        Capture("05-enhanced-pattern.png"); yield return 0.1f;
        int failed=failedPatterns;
        var wrong=(TileColor)(((int)boss.timeAttackPattern.RequiredColor.Value+1)%6);
        hp=StaminaManager.Instance.CurrentStamina; yield return Drag(Chain(2,wrong));
        Assert(failedPatterns==failed+1 && !boss.HasFailureShield,"Wrong sequence exactly one failure consumes cyan shield");
        Near(StaminaManager.Instance.CurrentStamina,Mathf.Min(100,hp+.6f),"Shield blocks failure damage, normal recovery retained");
        yield return Wait(()=>boss.timeAttackPattern.IsRunning,"Retry pattern");
        failed=failedPatterns; hp=StaminaManager.Instance.CurrentStamina;
        Time.timeScale=8f; yield return Wait(()=>failedPatterns==failed+1,"Timeout failure"); Time.timeScale=1f;
        Near(StaminaManager.Instance.CurrentStamina,hp-20,"Timeout exactly20 damage");
        Assert(failedPatterns==failed+1,"No duplicate failure event");
        Pass("Natural obstacle→boss entry; item protection, exact score/recovery, no success damage, cyan/buff/direct attacks, pause/freeze and deferred phase strengthening");
        // Melee lethal and all public damage sources converge on one immediate result.
        yield return Wait(()=>boss.timeAttackPattern.IsRunning,"Next pattern for groggy lethal");
        yield return CompletePattern();
        boss.ApplyDamage(boss.CurrentHP-4f); int resultCount=resultEvents, hpCount=hpEvents;
        yield return Drag(Chain(2,TileColor.Orange));
        Assert(game.CurrentState==GameState.Result && game.CurrentResult==ResultType.Win && boss.CurrentHP==0 && resultEvents==resultCount+1,"Lethal match immediately wins once");
        boss.ApplyDamage(999); game.TriggerStageClear();
        Assert(resultEvents==resultCount+1 && hpEvents==hpCount+1 && Time.timeScale==0,"No duplicate HP0 or victory");
        Capture("06-victory.png"); yield return 0.1f;
        // Fresh run: direct API lethal during pattern, no missing victory without match handler.
        game.RetryGame(); yield return 0.1f; boss=BossController.Instance;
        spawner.RestoreCheckpoint(3); progress.RestoreDistance(progress.StageLength); game.TriggerBossFight();
        resultCount=resultEvents; boss.ApplyDamage(120); boss.ApplyDamage(120);
        Assert(resultEvents==resultCount+1 && game.CurrentResult==ResultType.Win,"Direct pattern damage HP0 wins exactly once");
        // Fresh run: failure can lose, clocks stop, retry resets combat state.
        game.RetryGame(); yield return 0.1f; spawner.RestoreCheckpoint(3); progress.RestoreDistance(progress.StageLength); game.TriggerBossFight();
        StaminaManager.Instance.RestoreStamina(10,0); failed=failedPatterns; resultCount=resultEvents;
        yield return Drag(Chain(2,(TileColor)(((int)boss.timeAttackPattern.RequiredColor.Value+1)%6)));
        Assert(game.CurrentState==GameState.Result && game.CurrentResult==ResultType.Lose && resultEvents==resultCount+1 && failedPatterns==failed+1,"Wrong sequence HP0 loses once");
        Capture("07-defeat.png"); yield return 0.2f;
        float stopped=boss.timeAttackPattern.RemainingTime; yield return 0.3f;
        Assert(boss.timeAttackPattern.RemainingTime==stopped && !boss.IsGroggy && !boss.timeAttackPattern.IsRunning,"Result cancels all combat progress");
        game.RetryGame(); yield return 0.1f;
        Assert(game.CurrentState==GameState.RunnerPhase && StaminaManager.Instance.CurrentStamina>99 && !boss.HasFailureShield && boss.PendingAttackBonus==0,"Retry fresh player and combat boundary");
        SceneLoader.LoadMainMenu(); yield return Wait(()=>SceneManager.GetActiveScene().name=="Main","Return Main");
        Assert(BossController.Instance==null && GameManager.Instance==null,"No leaked scene services"); CheckSubscribers();
        var after=Saves(); Assert(saveHashes.Count==after.Count && saveHashes.All(p=>after.TryGetValue(p.Key,out var value) && value==p.Value),"User saves preserved");
        Pass("Immediate single victory via match and direct API, failure defeat, result freeze/retry, scene cleanup and unchanged user saves");
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
        GameEvents.OnBossPatternResult-=PatternResult; GameEvents.OnBossPhaseChanged-=PhaseChanged; GameEvents.OnBossHPChanged-=HPChanged;
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
        if (error == null && errors.Count == 0) Pass("Session04 completed without runtime errors");
        File.WriteAllLines(Path.Combine(Output,"play-results.txt"),results);
        SessionState.SetBool(Key,false);
        EditorApplication.Exit(error == null && errors.Count == 0 ? 0 : 1);
    }
}
