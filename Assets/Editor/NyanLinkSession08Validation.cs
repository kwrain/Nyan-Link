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

public static class NyanLinkSession08Validation
{
    const string Key = "NyanLink.Session08";
    static readonly string Output = Environment.GetEnvironmentVariable("NYANLINK_SESSION06_OUTPUT") ?? "output/playtests/2026-11-13-session08";
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
        SessionState.SetBool(Key+".Muted",EditorUtility.audioMasterMute);EditorUtility.audioMasterMute=false;
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
        if (EditorApplication.timeSinceStartup > expires) { Finish(new Exception("Session08 timeout")); return; }
        if (EditorApplication.isPlaying && driver == null)
        {
            driver = new GameObject("Session08 validation (temporary)").AddComponent<NyanLinkSession02PlayDriver>();
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
        Debug.Log("[Session08] " + results.Last());
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
        // Save writes use the isolated fixture directory only.
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

    static SaveManager save;
    static BossController boss;
    static RunCheckpoint last;
    static int saves,restores,patternResults;
    static bool holdSave;
    static string root;
    static string expectedFile => Path.Combine(Output,"expected-cold.json");
    static readonly Vector3Int Kept=new Vector3Int(5,6,0);
    static void Saved(RunCheckpoint cp) { saves++;last=cp;if(holdSave) Time.timeScale=0; }
    static void Restored(RunCheckpoint cp) { restores++;last=cp;Compare(cp);Time.timeScale=0; }
    static void Pattern(bool success) { patternResults++; }
    static void Near(float value,float expected,string text) => Assert(Mathf.Abs(value-expected)<.03f,text+" actual="+value+" expected="+expected);
    static List<Vector3Int> Chain(TileColor color)
    {
        var cells=new List<Vector3Int>{new Vector3Int(0,0,0),new Vector3Int(1,0,0)};
        foreach(var cell in cells) board.SpawnTile(cell,color,TileState.Normal);
        return cells;
    }
    static IEnumerator Drag(TileColor color)
    {
        yield return Wait(()=>!input.IsInputLocked,"Input unlock");
        var cells=Chain(color);
        foreach(var cell in cells) {Send(Point(cell),true);yield return .04f;}
        Assert(input.SelectedChain.Count==2,"Actual mouse chain");Send(Point(cells.Last()),false);yield return .08f;
    }
    static IEnumerator Tap(Vector3Int cell)
    {
        Send(Point(cell),true);yield return .04f;Send(Point(cell),false);yield return .08f;
    }
    static void Bind()
    {
        Configure();save=SaveManager.Instance;boss=BossController.Instance;
        save.stageData=stageClone;
        save.CheckpointSaved+=Saved;save.CheckpointRestored+=Restored;
    }
    static IEnumerator EnterGame()
    {
        yield return Click(Button(Object.FindFirstObjectByType<MainMenuController>(),"buttonPlay"));
        yield return Wait(()=>SceneManager.GetActiveScene().name=="Game" && Object.FindFirstObjectByType<PuzzleBoardManager>()?.GetTileCount()==39,"Game initialized");Bind();
    }
    static string Tiles(IEnumerable<TileRecord> records) => string.Join(";",records.OrderBy(t=>t.y).ThenBy(t=>t.x).Select(t=>$"{t.x},{t.y}:{t.color}:{t.state}:{t.itemType}"));
    static void Compare(RunCheckpoint cp)
    {
        var live=save.Capture(cp.boundary);
        Assert(game.CurrentState==cp.phase && save.CurrentRunId==cp.runId,"Restored phase and run identity");
        Near(live.distance,cp.distance,"Distance");Near(live.hp,cp.hp,"HP");Near(live.elapsed,cp.elapsed,"Runner elapsed");
        Near(live.freezeRemaining,cp.freezeRemaining,"Freeze remaining");Near(live.boostRemaining,cp.boostRemaining,"Boost remaining");Near(live.retryRemaining,cp.retryRemaining,"Retry remaining");Near(live.actionRemaining,cp.actionRemaining,"Action remaining");
        Assert(live.score==cp.score && live.obstacleIndex==cp.obstacleIndex && live.runnerState==cp.runnerState && live.requiredColor==cp.requiredColor,"Score, obstacle, runner state and required color");
        Assert(Tiles(live.board)==Tiles(cp.board),"All39 cells: position/color/level/item identity exact");
        Assert(JsonUtility.ToJson(live.boss)==JsonUtility.ToJson(cp.boss),"Boss HP, phase, next step, clocks, buffs, shield and planned sequence exact");
        Assert(live.randomState==cp.randomState,"Random state exact");
    }
    static IEnumerator Reload(RunCheckpoint cp,string screenshot=null)
    {
        int resultsBefore=patternResults,savedBefore=saves,restoredBefore=restores;
        SceneLoader.LoadMainMenu();yield return Wait(()=>SceneManager.GetActiveScene().name=="Main","Main reload");CheckSubscribers();
        yield return EnterGame();Assert(save.HasCurrentCheckpoint(),"Continue available after scene reload");
        if(screenshot!=null) {Capture(screenshot);yield return .1f;}
        yield return Click(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart"));
        Assert(restores==restoredBefore+1 && saves==savedBefore && patternResults==resultsBefore,"Restore once, no save/failure/success replay");
        Assert(Tiles(save.Capture(cp.boundary).board)==Tiles(cp.board),"Restored board remains exact after first simulation frame");
    }
    static void StorageCases()
    {
        var store=new SaveStore(Path.Combine(root,"cases"));
        Assert(store.Load().status==SaveReadStatus.Missing,"Missing file is empty profile");
        var doc=new SaveDocument();doc.permanent.coins=12;
        Assert(store.Write(doc,out _),"Initial atomic document");doc.permanent.coins=34;
        Assert(store.Write(doc,out _) && store.Load().data.permanent.coins==34 && File.Exists(store.BackupPath),"Second document with valid backup");
        string original=File.ReadAllText(store.FilePath);
        doc.permanent.coins=99;
        Assert(!store.Write(doc,out _,()=>throw new IOException("Injected interruption")) && File.ReadAllText(store.FilePath)==original,"Interruption after temp flush preserves committed primary");
        Assert(Directory.GetFiles(store.DirectoryPath,"*.tmp-*").Length>0 && store.Load().data.permanent.coins==34,"Stray temp is ignored");
        File.WriteAllText(store.FilePath,original.Replace("\"coins\": 34","\"coins\": 35"));
        Assert(store.Load().status==SaveReadStatus.Recovered && store.Load().data.permanent.coins==34,"Valid JSON with wrong checksum falls back to coherent backup");
        File.WriteAllText(store.FilePath,"{broken");
        var recovered=store.Load();Assert(recovered.status==SaveReadStatus.Recovered && recovered.data.permanent.coins==34,"Corrupt primary recovers last valid backup");
        Assert(store.Write(recovered.data,out _) && Directory.GetFiles(Path.Combine(store.DirectoryPath,"preserved")).Any(p=>File.ReadAllText(p)=="{broken"),"Damaged original archived before repair");
        File.WriteAllText(store.FilePath,"{\"schemaVersion\":99}");string future=File.ReadAllText(store.FilePath);
        Assert(store.Load().status==SaveReadStatus.Unsupported && !store.Write(new SaveDocument(),out _) && File.ReadAllText(store.FilePath)==future,"Future version never overwritten or downgraded to backup");
        var bad=new SaveDocument();bad.permanent.coins=-1;Assert(!store.Write(bad,out _),"Invalid permanent mutation rejected");
        var noBackup=new SaveStore(Path.Combine(root,"both-corrupt"));Directory.CreateDirectory(noBackup.DirectoryPath);File.WriteAllText(noBackup.FilePath,"{bad");File.WriteAllText(noBackup.BackupPath,"{bad backup");
        Assert(noBackup.Load().status==SaveReadStatus.Corrupt && File.ReadAllText(noBackup.FilePath)=="{bad","Both corrupt files remain intact");
        Pass("Atomic write/backup/checksum, interrupted temp, corrupt recovery with original preservation, future version refusal, invalid permanent data");
    }
    static IEnumerator LayoutCases()
    {
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(1080,1920),new Vector2Int(1284,2778)})
        {
            View("SetGameViewSize",size.x,size.y);yield return .35f;
            PrototypeSafeArea.TestPixels=size.x==1284 ? new Rect(40,110,size.x-80,size.y-270) : (Rect?)null;
            yield return .25f;Canvas.ForceUpdateCanvases();
            var safe=PrototypeSafeArea.Pixels;var rig=Object.FindFirstObjectByType<DualCameraRig>();
            Assert(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c=>c.name=="Main Camera").cullingMask==0,"Legacy fullscreen camera only clears, never draws duplicate board");
            Assert((rig.cameraRunner.cullingMask & (1<<board.gameObject.layer))==0,"Runner camera excludes puzzle layer");
            Near(rig.cameraRunner.pixelRect.height/safe.height,.4f,"Runner40percent of safe area");
            Near(rig.cameraPuzzle.pixelRect.height/safe.height,.535f,"Board viewport leaves score strip within lower60percent");
            var bounds=board.GetBoardWorldBounds();
            foreach(var corner in new[]{bounds.min,bounds.max})
            {
                var point=camera.WorldToScreenPoint(corner);
                Assert(point.x>=safe.xMin && point.x<=safe.xMax && point.y>=safe.yMin && point.y<=safe.yMin+safe.height*.535f,"All39 board bounds inside safe viewport");
            }
            var pause=game.GetComponent<GamePausePanel>().PauseButton.GetComponent<RectTransform>();
            var corners=new Vector3[4];pause.GetWorldCorners(corners);
            Assert(corners.All(c=>c.x>=safe.xMin && c.x<=safe.xMax && c.y>=safe.yMin && c.y<=safe.yMax),"Pause button inside notch/home safe area");
            int sounds=AudioManager.Instance.PlayedSfxCount;yield return Match(TileColor.Red);
            Assert(AudioManager.Instance.PlayedSfxCount>sounds,"Real match dispatches SFX");yield return .3f;
            Capture("layout-"+size.x+"x"+size.y+(size.x==1284 ? "-simulated-notch" : "")+".png");yield return .1f;
        }
        PrototypeSafeArea.TestPixels=null;View("SetGameViewSize",1080,1920);yield return .3f;
        Assert(AudioManager.Instance.sfxMatchShort!=null && AudioManager.Instance.sfxItemEffect!=null && AudioManager.Instance.sfxHit!=null && AudioManager.Instance.sfxStageClear!=null,"Temporary sound resources loaded");
        Assert(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled)==1,"Exactly one active audio listener");
        AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxStageClear);yield return .06f;
        var samples=new float[256];AudioListener.GetOutputData(samples,0);
        File.WriteAllText(Path.Combine(Output,"audio-probe.txt"),"masterMute="+EditorUtility.audioMasterMute+"\nlistenerPause="+AudioListener.pause+"\nsourcePlaying="+AudioManager.Instance.GetComponents<AudioSource>()[0].isPlaying+"\npeak="+samples.Max(v=>Mathf.Abs(v))+"\nsampleRate="+AudioSettings.outputSampleRate);
        if(!samples.Any(v=>Mathf.Abs(v)>.00001f)) results.Add("UNVERIFIED AudioListener output peak0 despite active source; audible output requires separate listening check");
        var pcm=new float[AudioManager.Instance.sfxStageClear.samples];
        Assert(AudioManager.Instance.sfxStageClear.GetData(pcm,0) && pcm.Any(v=>Mathf.Abs(v)>.00001f),"Imported SFX contains nonzero PCM data");
        Assert(AudioManager.Instance.GetComponents<AudioSource>()[0].isPlaying,"SFX playback source active");
        Pass("Three portrait sizes, simulated notch/home insets,40:60 logical split, safe controls and board");
    }
    static IEnumerator Sequence()
    {
        View("SetGameViewSize",1080,1920);yield return .4f;
        Assert(Screen.width==1080 && Screen.height==1920,"Actual Game View portrait");
        root=File.ReadAllText(Path.Combine(Output,"active-root.txt"));SaveManager.TestDirectoryOverride=Path.Combine(root,"play");
        saveHashes=Saves();
        previousMouse=Mouse.current;mouseWasEnabled=previousMouse!=null && previousMouse.enabled;if(mouseWasEnabled) InputSystem.DisableDevice(previousMouse);
        mouse=InputSystem.AddDevice<Mouse>("NyanLinkSession08Mouse");mouse.MakeCurrent();
        GameEvents.OnBossPatternResult+=Pattern;GameEvents.OnResultSet+=Result;
        if(SessionState.GetBool(Key+".Cold",false))
        {
            yield return EnterGame();Assert(save.HasCurrentCheckpoint(),"Cold process finds checkpoint");
            Capture("08-cold-continue-ready.png");yield return .1f;
            var expected=JsonUtility.FromJson<RunCheckpoint>(File.ReadAllText(expectedFile));
            yield return Click(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart"));
            Assert(Tiles(save.Capture(expected.boundary).board)==Tiles(expected.board),"Cold board exact");Assert(restores==1 && saves==0 && patternResults==0,"Cold continue has no duplicate outcome or checkpoint events");
            Assert(board.GetTileAtOffset(Kept).Color==TileColor.Blue && board.GetTileAtOffset(Kept).ItemType==ItemEffectType.PowerUp,"Cold resume keeps recolored item identity");
            Assert(Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.text=="PWR 2" && t.isActiveAndEnabled),"Restored item identity badge active");
            Capture("09-cold-restored-board.png");yield return .15f;
            var permanent=save.Store.Load().data.permanent;
            Assert(permanent.coins==321 && permanent.equipment.Count==2 && permanent.rewardReceipts.Count==1 && permanent.clears.Single().firstRewardGranted && boss.PhaseIndex==1,"Permanent profile and strengthened phase survive process restart");
            Time.timeScale=1f;
            yield return Click(game.GetComponent<GamePausePanel>().PauseButton);
            yield return Click(game.GetComponent<GamePausePanel>().RestartButton);
            Assert(save.Store.Load().data.checkpoint==null && save.Store.Load().data.permanent.coins==321,"Actual UI restart clears progress only");
            var after=Saves();Assert(saveHashes.Count==after.Count && saveHashes.All(p=>after.TryGetValue(p.Key,out var value) && value==p.Value),"Original user saves unchanged after cold process");
            yield return Approach();
            StaminaManager.Instance.TakeDamage(StaminaManager.Instance.CurrentStamina-5);
            yield return Wait(()=>game.CurrentState==GameState.Result,"Lethal obstacle failure",8);
            Assert(game.CurrentResult==ResultType.Lose && Time.timeScale==0 && save.Store.Load().data.permanent.coins==321 && save.Store.Load().data.checkpoint==null,"Obstacle loss stops play, clears only progress, no reward");
            Capture("11-obstacle-loss.png");yield return .15f;
            yield return Click(Button(Object.FindFirstObjectByType<GameOverPanel>(),"buttonRetry"));
            Near(StaminaManager.Instance.MaxStamina,110,"Actual defeat retry retains upgraded loadout");
            Pass("Cold Unity restart: all checkpoint fields and item identity exact, permanent record intact, actual UI restart progress-only");Pass(assertions+" cold Play assertions");yield break;
        }
        StorageCases();
        // Legacy format lacks board/effects, so it is deliberately retained rather than guessed.
        Directory.CreateDirectory(SaveManager.StorageDirectory);string legacyPath=Path.Combine(SaveManager.StorageDirectory,"checkpoint_prototype-stage-01.json");
        string legacy="{\"stageId\":\"prototype-stage-01\",\"savedStamina\":57,\"savedDistance\":62.5,\"nextObstacleIndex\":1}";File.WriteAllText(legacyPath,legacy);
        yield return EnterGame();Assert(!save.HasCurrentCheckpoint() && File.ReadAllText(legacyPath)==legacy,"Legacy checkpoint preserved, unsafe partial restore not offered");
        yield return Click(Button(Object.FindFirstObjectByType<GameReadyPanel>(),"buttonStart"));
        Assert(save.TryUpdatePermanent(p=>{p.coins=321;p.equipment.Add(new EquipmentRecord{instanceId="test-weapon",dataId="test-weapon-data",level=2,equippedSlot=2});}),"Permanent transaction seed");
        yield return LayoutCases();
        holdSave=true;
        int count=saves;
        yield return Approach();Time.timeScale=1f;
        board.SpawnTile(new Vector3Int(2,3,0),TileColor.Blue,TileState.ItemLv2);yield return Tap(new Vector3Int(2,3,0));
        yield return Wait(()=>spawner.CurrentObstacle.IsInputWindowOpen,"First input window");
        yield return Drag(TileColor.Red);
        runner.RestoreBoundary(RunnerState.Jump,.4f,2.75f); // Storage fixture: current boost already verified through real purple input in Session05.
        board.SpawnTile(Kept,TileColor.Blue,TileState.ItemLv2);board.GetTileAtOffset(Kept).SetItemType(ItemEffectType.PowerUp);
        boss.GrantAttackBuff(.5f);boss.GrantFailureShield();
        yield return Wait(()=>saves>count,"Obstacle success saved");var runnerCp=SaveStore.Clone(last);
        Assert(runnerCp.boundary==CheckpointBoundary.ObstacleSuccess && runnerCp.obstacleIndex==1 && runnerCp.boostRemaining>0 && runnerCp.freezeRemaining>0,"Success boundary includes live boost/freeze");Compare(runnerCp);
        var duplicate=SaveStore.Clone(runnerCp);duplicate.board[0].x=duplicate.board[1].x;duplicate.board[0].y=duplicate.board[1].y;
        string untouched=File.ReadAllText(save.Store.FilePath);
        Assert(!save.Store.Write(new SaveDocument{checkpoint=duplicate},out _) && File.ReadAllText(save.Store.FilePath)==untouched,"Duplicate/missing mask cell rejected without file mutation");
        duplicate=SaveStore.Clone(runnerCp);duplicate.freezeRemaining=float.NaN;
        Assert(!save.Store.Write(new SaveDocument{checkpoint=duplicate},out _),"Nonfinite effect timer rejected");
        var validDocument=save.Store.Load().data;var incompatible=SaveStore.Clone(validDocument);incompatible.checkpoint.rulesSignature="changed-rules";
        Assert(save.Store.Write(incompatible,out _) && !save.HasCurrentCheckpoint(),"Rules mismatch is not offered as continue");
        Assert(save.Store.Write(validDocument,out _),"Known valid document restored in isolated test fixture");
        Capture("01-runner-saved.png");yield return .1f;
        game.PauseGame();count=saves;save.RequestBoundary(CheckpointBoundary.ObstacleSuccess);
        yield return Wait(()=>saves>count,"Pending boundary commits while user-paused");
        Assert(game.CurrentState==GameState.Paused && last.phase==GameState.RunnerPhase,"Pause at boundary persists underlying playable phase, not paused UI");runnerCp=SaveStore.Clone(last);
        yield return Reload(runnerCp,"02-continue-choice.png");Capture("03-runner-restored.png");yield return .1f;
        // Pause is memory resume, not disk restore.
        Time.timeScale=1f;yield return Drag(TileColor.Red);string changed=Tiles(save.Capture(runnerCp.boundary).board);int restoresBefore=restores;
        yield return PauseSnapshot(null);Assert(Tiles(save.Capture(runnerCp.boundary).board)==changed && restores==restoresBefore,"Pause does not reload checkpoint");
        yield return Reload(runnerCp);Time.timeScale=1f;
        yield return Approach();count=saves;yield return Wait(()=>saves>count,"Failure boundary saved");var retryCp=SaveStore.Clone(last);
        Assert(retryCp.boundary==CheckpointBoundary.ObstacleRetry && retryCp.retryRemaining>0 && retryCp.obstacleIndex==1,"Failure checkpoint retains same obstacle and remaining retry");
        Capture("04-retry-saved.png");yield return .1f;
        yield return Reload(retryCp);float retryHP=StaminaManager.Instance.CurrentStamina;
        Time.timeScale=1f;yield return Wait(()=>spawner.CurrentObstacle!=null,"Restored retry spawns new same obstacle");
        Assert(spawner.NextObstacleIndex==1 && !spawner.CurrentObstacle.HasSuccessReservation && StaminaManager.Instance.CurrentStamina>retryHP-1,"Retry restore does not apply collision damage twice or leak reservation");
        yield return Wait(()=>spawner.CurrentObstacle.IsInputWindowOpen,"Blue input");count=saves;yield return Drag(TileColor.Blue);yield return Wait(()=>saves>count,"Blue success checkpoint");
        Time.timeScale=1f;yield return Approach();yield return Wait(()=>spawner.CurrentObstacle.IsInputWindowOpen,"Yellow input");count=saves;yield return Drag(TileColor.Yellow);yield return Wait(()=>saves>count,"Yellow success checkpoint");
        Time.timeScale=8f;yield return Wait(()=>game.CurrentState==GameState.BossPhase,"Boss entry");Time.timeScale=1f;
        count=saves;yield return Drag((TileColor)(((int)boss.timeAttackPattern.RequiredColor.Value+1)%6));yield return Wait(()=>saves>count,"Boss failure checkpoint");
        var failureCp=SaveStore.Clone(last);Assert(failureCp.boundary==CheckpointBoundary.BossFailure && failureCp.boss.nextStep==BossNextStep.Interval && !failureCp.boss.failureShield,"Failure consumes shield and records next boundary");
        Capture("05-boss-failure-saved.png");yield return .1f;
        yield return Reload(failureCp);int outcomes=patternResults;Time.timeScale=8f;yield return Wait(()=>boss.timeAttackPattern.IsRunning,"Planned next pattern");Time.timeScale=1f;
        Assert(boss.timeAttackPattern.Sequence.SequenceEqual(failureCp.boss.nextSequence) && patternResults==outcomes,"Restored planned pattern does not reroll/replay outcomes");
        float oldHP=StaminaManager.Instance.CurrentStamina;count=saves;yield return Drag((TileColor)(((int)boss.timeAttackPattern.RequiredColor.Value+1)%6));yield return Wait(()=>saves>count,"Unshielded failure saved");
        Near(last.hp,Mathf.Min(100,oldHP+.6f)-20,"Failure20 applied once before checkpoint capture");
        var secondFailure=SaveStore.Clone(last);yield return Reload(secondFailure);Time.timeScale=8f;yield return Wait(()=>boss.timeAttackPattern.IsRunning,"Next pattern after failure");Time.timeScale=1f;
        // Simulate a carried effect at a pattern boundary, already verified via real blue items in Session05.
        StaminaManager.Instance.RestoreEffects(4.25f);count=saves;
        while(boss.timeAttackPattern.IsRunning) yield return Drag(boss.timeAttackPattern.RequiredColor.Value);
        yield return Wait(()=>saves>count,"Boss success saved after refill");var groggyCp=SaveStore.Clone(last);
        Assert(groggyCp.boundary==CheckpointBoundary.BossSuccess && groggyCp.boss.nextStep==BossNextStep.Groggy && groggyCp.boss.groggyRemaining==5 && groggyCp.freezeRemaining>0,"Success boundary opens full groggy with preserved effect");
        Capture("06-groggy-saved.png");yield return .1f;yield return Reload(groggyCp);Assert(boss.IsGroggy,"Continue resumes groggy, not a fresh pattern");
        Time.timeScale=1f;float hpBefore=boss.CurrentHP;long revision=save.Store.Load().data.revision;yield return Drag(TileColor.Orange);
        Near(boss.CurrentHP,hpBefore-12,"Restored50percent buff applied once");Assert(save.Store.Load().data.revision==revision,"No per-match checkpoint writes");
        boss.ApplyDamage(60f);Assert(boss.PhaseIndex==0,"HP threshold does not strengthen until groggy ends");
        Time.timeScale=8f;count=saves;yield return Wait(()=>saves>count,"Groggy end saved");var endCp=SaveStore.Clone(last);
        Assert(endCp.boundary==CheckpointBoundary.GroggyEnd && endCp.boss.hp==hpBefore-72 && endCp.boss.phase==1 && endCp.boss.nextSequence.Count==4 && endCp.boss.attackBonus==0 && endCp.boss.nextStep==BossNextStep.Interval,"Groggy exit persists damage, consumed buff, next phase boundary");
        yield return Reload(endCp);Capture("07-boss-restored.png");yield return .1f;
        // Session07 contract: pending completion, reward and clear/receipt in one atomic transaction.
        Time.timeScale=1f;string rewardedRun=save.CurrentRunId;boss.ApplyDamage(999);
        Assert(save.Store.Load().data.checkpoint==null && save.Store.Load().data.permanent.rewardReceipts.Contains(rewardedRun),"Session07 completion automatically pays durable reward and receipt");
        string pendingDocument=File.ReadAllText(save.Store.FilePath);save.CompleteCurrentRun();
        Assert(File.ReadAllText(save.Store.FilePath)==pendingDocument,"Duplicate completion does not write or enqueue twice");
        Assert(save.Store.Load().data.permanent.coins==421 && save.Store.Load().data.permanent.equipment.Count==2,"Actual reward +100/hat added to retained legacy record");
        string committed=File.ReadAllText(save.Store.FilePath);
        Assert(!save.TryApplyReward(rewardedRun,p=>p.coins+=100) && File.ReadAllText(save.Store.FilePath)==committed,"Duplicate reward cannot pay twice");
        Assert(!save.TryUpdatePermanent(p=>p.coins=-99) && File.ReadAllText(save.Store.FilePath)==committed,"Rejected permanent mutation leaves committed data unchanged");
        var gearPanel=game.GetComponent<EquipmentPanel>();yield return Click(gearPanel.OpenButton);
        string hat=save.Store.Load().data.permanent.equipment.First(e=>e.dataId=="prototype-hat-01").instanceId;
        yield return Click(gearPanel.EquipButtons[hat]);yield return Click(gearPanel.UpgradeButtons[hat]);
        Assert(save.Store.Load().data.permanent.coins==321 && save.Store.Load().data.permanent.equipment.First(e=>e.instanceId==hat).level==2,"Integrated clear reward -> actual equip/upgrade UI -> atomic100 spend");
        Capture("10-reward-equipment-upgrade.png");yield return .15f;yield return Click(gearPanel.CloseButton);
        game.RetryGame();Near(StaminaManager.Instance.MaxStamina,110,"Retry uses upgraded hat HP110");Assert(save.Store.Load().data.permanent.coins==321 && save.Store.Load().data.permanent.equipment.Count==2 && save.Store.Load().data.checkpoint==null,"New game resets progress only");
        // Final checkpoint for a completely separate Unity process.
        spawner.RestoreCheckpoint(3);progress.RestoreDistance(progress.StageLength);game.TriggerBossFight();count=saves;
        yield return Drag((TileColor)(((int)boss.timeAttackPattern.RequiredColor.Value+1)%6));yield return Wait(()=>saves>count,"Final boss boundary");
        boss.RestoreBoundary(endCp.boss); // Reuse the actual strengthened boundary as the cold-process fixture.
        board.SpawnTile(Kept,TileColor.Blue,TileState.ItemLv2);board.GetTileAtOffset(Kept).SetItemType(ItemEffectType.PowerUp);
        StaminaManager.Instance.RestoreEffects(2.5f);runner.RestoreBoundary(RunnerState.Run,0,1.75f);
        count=saves;save.RequestBoundary(CheckpointBoundary.GroggyEnd);yield return Wait(()=>saves>count,"Cold fixture checkpoint");
        File.WriteAllText(expectedFile,JsonUtility.ToJson(last,true));
        File.WriteAllText(Path.Combine(Output,"schema-example.json"),File.ReadAllText(save.Store.FilePath));
        Assert(File.ReadAllText(legacyPath)==legacy,"Legacy original still byte-identical");
        var afterFiles=Saves();Assert(saveHashes.Count==afterFiles.Count && saveHashes.All(p=>afterFiles.TryGetValue(p.Key,out var value) && value==p.Value),"Original user saves unchanged");
        Assert(AudioManager.Instance.PlayedSfxCount>0 && Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length==1,"Sound cues through one global owner; count="+AudioManager.Instance.PlayedSfxCount);
        Pass("Actual success/retry/boss failure/success/groggy-exit boundaries; exact board/item/effect restore, no duplicate damage/results, pause vs disk, atomic permanent/reward contract");
        Pass(assertions+" Play assertions; cold checkpoint prepared");
    }

    static void Finish(Exception error)
    {
        EditorApplication.update -= KeepAlive;
        if (driver != null) { driver.Tick = null; driver.LateTick = null; }
        if (error != null) results.Add(error.ToString());
        foreach (var message in errors) results.Add("RUNTIME_ERROR " + message);
        GameEvents.OnBossAppeared -= BossAppeared; GameEvents.OnResultSet -= Result;
        ObstacleController.OnObstacleFailed -= Failed; ObstacleController.OnObstacleCleared -= Cleared;
        GameEvents.OnBossPatternResult-=Pattern;
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
        EditorUtility.audioMasterMute=SessionState.GetBool(Key+".Muted",false);
        Time.timeScale = 1f;
        if (error == null && errors.Count == 0) Pass("Session08 completed without runtime errors");
        File.WriteAllLines(Path.Combine(Output,SessionState.GetBool(Key+".Cold",false)?"cold-results.txt":"play-results.txt"),results);
        File.WriteAllText(Path.Combine(Output,"environment.txt"),"Unity="+Application.unityVersion+"\npersistentDataPath="+Application.persistentDataPath+"\nisolatedStorage="+SaveManager.StorageDirectory+"\n");
        PrototypeSafeArea.TestPixels=null;
        SaveManager.TestDirectoryOverride=null;SessionState.SetBool(Key+".Cold",false);
        SessionState.SetBool(Key,false);
        EditorApplication.Exit(error == null && errors.Count == 0 ? 0 : 1);
    }
}
