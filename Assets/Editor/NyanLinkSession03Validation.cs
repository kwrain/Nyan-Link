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

public static class NyanLinkSession03Validation
{
    const string Key = "NyanLink.Session03";
    const string Output = "output/playtests/2026-11-13-session03";
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
        Debug.Log("[Session03] " + results.Last());
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

    static IEnumerator Sequence()
    {
        View("SetGameViewSize",1080,1920); yield return 0.4f;
        Assert(Screen.width == 1080 && Screen.height == 1920,"Runtime 1080x1920 context");
        saveHashes = Saves();
        previousMouse = Mouse.current; mouseWasEnabled = previousMouse != null && previousMouse.enabled;
        if (mouseWasEnabled) InputSystem.DisableDevice(previousMouse);
        mouse = InputSystem.AddDevice<Mouse>("NyanLinkSession03Mouse"); mouse.MakeCurrent();
        GameEvents.OnBossAppeared += BossAppeared;
        GameEvents.OnResultSet += Result;
        ObstacleController.OnObstacleFailed += Failed;
        ObstacleController.OnObstacleCleared += Cleared;
        int dataId = DataManager.Instance.GetInstanceID();
        for (int cycle = 0; cycle < 3; cycle++)
        {
            yield return Click(Button(Object.FindFirstObjectByType<MainMenuController>(),"buttonPlay"));
            yield return Wait(() => SceneManager.GetActiveScene().name == "Game" && Object.FindFirstObjectByType<PuzzleBoardManager>()?.GetTileCount() == 39,"Game initialized");
            Configure();
            Assert(DataManager.Instance.GetInstanceID() == dataId,"Global DataManager preserved across scenes");
            yield return Click(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart"));
            Assert(game.CurrentState == GameState.RunnerPhase,"UI start enters RunnerPhase");
            if (cycle == 0)
            {
                var root = game.gameObject;
                foreach (var type in new[] { typeof(GameManager),typeof(ScoreManager),typeof(StaminaManager),typeof(SaveManager) }) root.AddComponent(type);
                var bossRoot = BossController.Instance.gameObject; bossRoot.AddComponent<BossController>();
                yield return 0.15f;
                Assert(root != null && root.GetComponents<GameManager>().Length == 1 && root.GetComponents<ScoreManager>().Length == 1 && root.GetComponents<StaminaManager>().Length == 1 && root.GetComponents<SaveManager>().Length == 1,"Duplicate components do not destroy shared manager root");
                Assert(bossRoot != null && bossRoot.GetComponents<BossController>().Length == 1,"Boss duplicate does not destroy pattern root");
                CheckSubscribers();
                int beforeResult = resultEvents;
                StaminaManager.Instance.TakeDamage(1000f);
                Assert(game.CurrentState == GameState.Result && Time.timeScale == 0f && resultEvents == beforeResult + 1,"Lose result freezes once");
                float distance = progress.CurrentDistance, time = StaminaManager.Instance.ElapsedRunnerTime;
                int score = ScoreManager.Instance.CurrentScore;
                Object.FindFirstObjectByType<TileMatcher>().ProcessMatch(board.GetAllTileOffsets().Take(2).ToList());
                yield return 0.3f;
                Assert(progress.CurrentDistance == distance && StaminaManager.Instance.ElapsedRunnerTime == time && ScoreManager.Instance.CurrentScore == score,"Result stops progress/decay/puzzle processing");
                yield return Click(Button(Object.FindFirstObjectByType<GameOverPanel>(),"buttonRetry"));
                Assert(game.CurrentState == GameState.RunnerPhase && Time.timeScale == 1f && board.GetTileCount() == 39 && spawner.NextObstacleIndex == 0 && StaminaManager.Instance.CurrentStamina > 99f,"Result retry resets run without scene duplication");
            }
            if (cycle == 2) { Capture("01-reentry-board.png"); yield return 0.2f; break; }
            yield return PauseSnapshot(cycle == 1 ? "02-paused-runner.png" : null);
            yield return Click(game.GetComponent<GamePausePanel>().PauseButton);
            yield return Click(game.GetComponent<GamePausePanel>().HomeButton);
            yield return Wait(() => SceneManager.GetActiveScene().name == "Main","Pause Home returns Main");
            Assert(GameManager.Instance == null && SaveManager.Instance == null && StaminaManager.Instance == null && ScoreManager.Instance == null && BossController.Instance == null && Time.timeScale == 1f,"Scene services absent in Main; no phantom singleton creation");
            CheckSubscribers();
        }
        Pass("Main→Game→Main cycles, fresh 39-cell board, live scene references, shared-root duplicates, result retry, no stale subscriptions");
        Assert(bossEvents == 0,"No early boss event");
        game.TriggerBossFight(); Assert(game.CurrentState == GameState.RunnerPhase && bossEvents == 0,"Cannot skip the three obstacles");
        yield return Match(TileColor.Purple);
        Assert(runner.SpeedMultiplier == 1.5f && Mathf.Abs(progress.RunnerSpeed - 7.5f) < 0.01f,"Purple speed connected: runnerMult=" + runner.SpeedMultiplier + " boost=" + runner.BoostTimeRemaining + " base=" + progress.BaseRunnerSpeed + " speed=" + progress.RunnerSpeed + " boundRunner=" + typeof(RunnerProgressSystem).GetField("_runner",PrivateInstance).GetValue(progress));
        yield return PauseSnapshot(null);
        Time.timeScale = 8f; yield return Wait(() => runner.BoostTimeRemaining <= 0f,"Boost expires"); Time.timeScale = 1f;
        Assert(progress.RunnerSpeed == 5f,"Base speed restored after boost");
        game.RetryGame();
        Time.timeScale = 8f; yield return Wait(() => spawner.IsPreWarning,"Two-second prewarning"); Time.timeScale = 1f;
        Assert(spawner.WarningRemaining > 1.6f && spawner.WarningRemaining <= 2.05f,"Prewarning begins approximately two seconds before spawn");
        Assert(board.RequiredMatchColor == TileColor.Red,"First obstacle registers required color");
        Capture("03-jump-prewarning.png"); yield return 0.15f;
        yield return Approach();
        var first = spawner.CurrentObstacle;
        Assert(!first.IsInputWindowOpen && !first.HasSuccessReservation,"No reservation at spawn");
        yield return Match(TileColor.Red);
        Assert(!first.HasSuccessReservation,"Early match cannot reserve success");
        yield return ExpectFailure();
        float retryStarted = Time.time;
        float retryHp = StaminaManager.Instance.CurrentStamina, retryElapsed = StaminaManager.Instance.ElapsedRunnerTime, retryDistance = progress.CurrentDistance;
        yield return 0.3f;
        Assert(progress.CurrentDistance == retryDistance && StaminaManager.Instance.CurrentStamina < retryHp && StaminaManager.Instance.ElapsedRunnerTime > retryElapsed,"Retry holds progress while natural decay/elapsed time continue");
        Capture("04-retry-warning.png");
        yield return PauseSnapshot(null);
        yield return Wait(() => spawner.CurrentObstacle != null,"Same obstacle retried");
        Assert(Time.time - retryStarted >= 1.9f && spawner.NextObstacleIndex == 0 && spawner.CurrentObstacle != first && !spawner.CurrentObstacle.HasSuccessReservation,"Two-second retry has a new instance and no carried reservation");
        yield return ExpectFailure();
        yield return Wait(() => spawner.CurrentObstacle != null,"Repeated retry");
        var reservedBeforeRestart = spawner.CurrentObstacle;
        yield return Wait(() => reservedBeforeRestart.IsInputWindowOpen,"Input window after repeated failure");
        yield return Match(TileColor.Red);
        Assert(reservedBeforeRestart.HasSuccessReservation,"Matching in input window reserves success");
        yield return PauseSnapshot(null);
        Assert(spawner.CurrentObstacle == reservedBeforeRestart && reservedBeforeRestart.HasSuccessReservation,"Reservation survives pause/resume");
        yield return Click(game.GetComponent<GamePausePanel>().PauseButton);
        yield return Click(game.GetComponent<GamePausePanel>().RestartButton);
        Assert(spawner.NextObstacleIndex == 0 && spawner.CurrentObstacle == null && spawner.RetryRemaining == 0f && !input.IsInputLocked && board.GetTileCount() == 39,"Restart cancels old obstacle, retry and input lock");
        Pass("Purple boost, prewarning, early-input rejection, 20 damage, repeated 2-second retry, decay while held, reservation pause and restart reset");
        int clearsBefore = clears;
        foreach (TileColor color in new[] { TileColor.Red,TileColor.Blue,TileColor.Yellow })
        {
            yield return Approach();
            var obstacle = spawner.CurrentObstacle;
            Assert(obstacle.RequiredColor == color && !obstacle.HasSuccessReservation,"Ordered obstacle and no reservation leak: " + color);
            Assert(board.RequiredMatchColor == color,"Required color transition connected");
            yield return Wait(() => obstacle.IsInputWindowOpen,"Obstacle input window");
            if (color == TileColor.Blue)
            {
                int score = ScoreManager.Instance.CurrentScore;
                yield return Match(TileColor.Red);
                Assert(input.IsInputLocked && !obstacle.HasSuccessReservation && ScoreManager.Instance.CurrentScore == score + 20,"Wrong match scores and locks without granting success");
                float locked = Time.time;
                yield return Wait(() => !input.IsInputLocked,"Half-second wrong-match lock");
                Assert(Time.time - locked >= 0.35f,"Wrong match lock lasts roughly 0.5 seconds including completed input step");
            }
            yield return Match(color);
            Assert(obstacle.HasSuccessReservation,"Correct match reserves success: " + color);
            if (color == TileColor.Red)
            {
                yield return Wait(() => runner.CurrentState == RunnerState.Run,"Jump animation/state finishes");
                Assert(spawner.CurrentObstacle == obstacle && !obstacle.IsResolved && obstacle.HasSuccessReservation,"Expired jump state does not lose reservation before contact");
            }
            if (color == TileColor.Yellow) { Capture("05-block-reserved.png"); yield return 0.05f; }
            float hp = StaminaManager.Instance.CurrentStamina, elapsed = StaminaManager.Instance.ElapsedRunnerTime;
            int index = spawner.NextObstacleIndex;
            yield return Wait(() => spawner.NextObstacleIndex == index + 1,"Successful pass");
            float collisionDamage = hp - StaminaManager.Instance.CurrentStamina - NaturalLoss(elapsed,StaminaManager.Instance.ElapsedRunnerTime);
            Assert(Mathf.Abs(collisionDamage) < 0.2f && !progress.IsPaused && spawner.RetryRemaining == 0f,"Successful pass has no collision damage: " + color);
        }
        Assert(clears == clearsBefore + 3 && spawner.AllObstaclesCleared,"Exactly three ordered successes");
        Time.timeScale = 8f; yield return Wait(() => game.CurrentState == GameState.BossPhase,"Natural boss entry"); Time.timeScale = 1f;
        Assert(bossEvents == 1 && progress.CurrentProgress == 1f && !BossController.Instance.enableCombat,"Single natural boss event; unfinished combat gated");
        Capture("06-boss-entry.png"); yield return 0.2f;
        float hpBoss = StaminaManager.Instance.CurrentStamina, elapsedBoss = StaminaManager.Instance.ElapsedRunnerTime;
        yield return PauseSnapshot(null);
        yield return 0.3f;
        Assert(StaminaManager.Instance.CurrentStamina == hpBoss && StaminaManager.Instance.ElapsedRunnerTime == elapsedBoss,"Boss phase stops natural HP decay");
        Pass("Jump→Slide→Block through default StageData to one boss entry; reserved success independent of animation; wrong-match lock; yellow no-damage pass");
        // Isolate the preserved legacy pattern solely to verify pause lifetime; do not claim session 04 combat correctness.
        var boss = BossController.Instance;
        bossClone = Object.Instantiate(boss.config); bossClone.inputTimeLimit = 1.5f; boss.config = bossClone;
        boss.enableCombat = true;
        typeof(BossController).GetMethod("StartBattle",PrivateInstance).Invoke(boss,null);
        typeof(BossController).GetMethod("ApplyDamage",PrivateInstance).Invoke(boss,new object[] { 10f });
        yield return 0.3f;
        var timer = typeof(NineMood.Boss.Patterns.TimeAttackPattern).GetField("_timerRoutine",PrivateInstance).GetValue(boss.timeAttackPattern);
        yield return Click(game.GetComponent<GamePausePanel>().PauseButton);
        float health = boss.CurrentHP, playerHp = StaminaManager.Instance.CurrentStamina;
        yield return 1.7f;
        Assert(game.StateBeforePause == GameState.BossPhase && boss.timeAttackPattern.IsRunning && boss.CurrentHP == health && playerHp == StaminaManager.Instance.CurrentStamina,"Boss pattern wait/HP survive a pause longer than its limit");
        yield return Click(game.GetComponent<GamePausePanel>().ResumeButton);
        Assert(game.CurrentState == GameState.BossPhase && boss.CurrentHP == health && ReferenceEquals(timer,typeof(NineMood.Boss.Patterns.TimeAttackPattern).GetField("_timerRoutine",PrivateInstance).GetValue(boss.timeAttackPattern)),"Boss resume does not restart battle, timer or HP");
        yield return Wait(() => StaminaManager.Instance.CurrentStamina <= playerHp - 19f,"Preserved pattern resumes and times out",3f);
        Pass("Isolated legacy boss timer pauses and resumes without battle/HP reset (combat rules deferred)");
        yield return Click(game.GetComponent<GamePausePanel>().PauseButton);
        yield return Click(game.GetComponent<GamePausePanel>().HomeButton);
        yield return Wait(() => SceneManager.GetActiveScene().name == "Main","Boss pause Home");
        CheckSubscribers();
        var after = Saves();
        Assert(saveHashes.Count == after.Count && saveHashes.All(p => after.TryGetValue(p.Key,out var hash) && hash == p.Value),"User checkpoint files unchanged");
        foreach (var file in new[] { "01-reentry-board.png","02-paused-runner.png","03-jump-prewarning.png","04-retry-warning.png","05-block-reserved.png","06-boss-entry.png" })
            Assert(File.Exists(Path.Combine(Output,file)) && new FileInfo(Path.Combine(Output,file)).Length > 0,"Actual screenshot saved: " + file);
        Pass($"{assertions} Play assertions; user saves preserved; no destroyed/duplicate event subscribers");
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
        Time.timeScale = 1f;
        if (error == null && errors.Count == 0) Pass("Session03 completed without runtime errors");
        File.WriteAllLines(Path.Combine(Output,"play-results.txt"),results);
        SessionState.SetBool(Key,false);
        EditorApplication.Exit(error == null && errors.Count == 0 ? 0 : 1);
    }
}
