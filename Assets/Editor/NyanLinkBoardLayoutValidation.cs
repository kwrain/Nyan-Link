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

public static class NyanLinkBoardLayoutValidation
{
    const string Key = "NyanLink.BoardLayout";
    static readonly string Output = Environment.GetEnvironmentVariable("NYANLINK_BOARD_LAYOUT_OUTPUT") ?? "output/playtests/2026-11-16-board-layout";
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
    static int nextFrame, assertions;
    static GameManager game;



    static PuzzleBoardManager board;



    static Camera camera;

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
        expires = EditorApplication.timeSinceStartup + 180;
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

    static void View(string method, params object[] args) => typeof(NyanLinkSession02Validation)
        .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    static float PixelsPerUnit(Camera c) => c.pixelRect.height / (2f * c.orthographicSize);
    static GridShapeData shape;
    static DualCameraRig rig;
    static float baselineScale, baselineHeight, baselineRunnerWidth, baselineScoreY;
    static RectTransform Score => Object.FindFirstObjectByType<GameHUD>().transform.Find("Text_Score").GetComponent<RectTransform>();
    static void SetShape(int width, int height)
    {
        shape.width = width; shape.height = height; board.ResetBoard();
    }
    static void CheckLayout(float scale, string label)
    {
        Assert(Mathf.Abs(PixelsPerUnit(camera) - scale) < .01f, label + " tile pixel size preserved");
        var origin = camera.WorldToScreenPoint(Vector3.zero);
        Assert(Mathf.Abs((camera.WorldToScreenPoint(Vector3.right)-origin).magnitude-scale)<.5f &&
            Mathf.Abs((camera.WorldToScreenPoint(Vector3.up)-origin).magnitude-scale)<.5f, label + " projected tile scale within subpixel rounding");
        var bounds = board.GetBoardWorldBounds();
        var min = camera.WorldToScreenPoint(bounds.min); var max = camera.WorldToScreenPoint(bounds.max);
        Assert(min.x >= camera.pixelRect.xMin - 1 && min.y >= camera.pixelRect.yMin - 1 &&
            max.x <= camera.pixelRect.xMax + 1 && max.y <= camera.pixelRect.yMax + 1, label + " board fully visible");
        var safe = PrototypeSafeArea.Pixels;
        Assert(Mathf.Abs(camera.pixelRect.yMin-safe.yMin)<1 && Mathf.Abs(rig.cameraRunner.pixelRect.yMax-safe.yMax)<1,
            label + " cameras respect safe area");
        float gap = rig.cameraRunner.pixelRect.yMin-camera.pixelRect.yMax;
        Assert(Mathf.Abs(gap-safe.height*DualCameraRig.HudGapRatio)<1, label + " HUD gap preserved");
        Assert(Mathf.Abs(rig.cameraRunner.orthographicSize*2*rig.cameraRunner.aspect-baselineRunnerWidth)<.05f,
            label + " runner horizontal framing preserved");
        Assert(Mathf.Abs(Score.anchorMin.y-baselineScoreY-(rig.CurrentPuzzleRatio-.60f))<.001f,
            label + " score follows split");
        foreach (var cell in board.GetAllTileOffsets())
        {
            var screen = camera.WorldToScreenPoint(board.GetCellCenterWorld(cell));
            Assert(board.TryGetTileAtWorldPosition(camera.ScreenToWorldPoint(screen),1f,out var hit) && hit==cell,
                label + " cell picking " + cell);
        }
        Pass(label + $" tiles={shape.width}x{shape.height} pixels/unit={PixelsPerUnit(camera):F3} lower={rig.CurrentPuzzleRatio:P2}");
    }
    static IEnumerator Capture(string file)
    {
        board.CancelTileAnimations();
        yield return .1f;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Output,file)));
        yield return .15f;
    }
    static IEnumerator Sequence()
    {
        View("SetGameViewSize",1080,1920); yield return .5f;
        SceneManager.LoadScene("Game"); yield return 1f;
        board=Object.FindFirstObjectByType<PuzzleBoardManager>();
        rig=Object.FindFirstObjectByType<DualCameraRig>(); camera=rig.cameraPuzzle;
        shape=Object.Instantiate(board.gridShapeData); board.gridShapeData=shape;
        game=GameManager.Instance; game.StartGame(); yield return .4f;
        Time.timeScale=0;
        SetShape(6,7); yield return .4f;
        baselineScale=PixelsPerUnit(camera); baselineHeight=camera.pixelHeight;
        baselineRunnerWidth=rig.cameraRunner.orthographicSize*2*rig.cameraRunner.aspect;
        baselineScoreY=Score.anchorMin.y;
        Assert(Mathf.Abs(rig.CurrentPuzzleRatio-.6f)<.001f,"Default 6x7 split unchanged");
        CheckLayout(baselineScale,"Default 6x7"); yield return Capture("01-default-6x7.png");
        SetShape(4,5); yield return .4f;
        CheckLayout(baselineScale,"Reduced 4x5");
        float reducedHeight=camera.pixelHeight;
        Assert(reducedHeight<baselineHeight,"Fewer rows shrink lower viewport");
        Assert(Mathf.Abs((baselineHeight-reducedHeight)-(rig.cameraRunner.pixelHeight-PrototypeSafeArea.Pixels.height*.4f))<1,
            "Runner receives all released height");
        yield return Capture("02-reduced-4x5.png");
        // Pick a real matching adjacent pair and send it through the actual Mouse input path.
        previousMouse=Mouse.current; mouseWasEnabled=previousMouse!=null && previousMouse.enabled;
        if(mouseWasEnabled) InputSystem.DisableDevice(previousMouse);
        mouse=InputSystem.AddDevice<Mouse>("BoardLayoutMouse"); mouse.MakeCurrent();
        var pair=board.GetAllTileOffsets().SelectMany(a=>HexOffsetUtils.GetNeighbors(a)
            .Where(b=>board.GetTileAtOffset(b)!=null && board.GetTileAtOffset(b).Color==board.GetTileAtOffset(a).Color)
            .Select(b=>new[]{a,b})).First();
        int before=ScoreManager.Instance.CurrentScore;
        Time.timeScale=1;
        foreach(var cell in pair)
        {
            InputSystem.QueueStateEvent(mouse,new MouseState {position=camera.WorldToScreenPoint(board.GetCellCenterWorld(cell))}.WithButton(MouseButton.Left,true));
            yield return .08f;
        }
        InputSystem.QueueStateEvent(mouse,new MouseState {position=camera.WorldToScreenPoint(board.GetCellCenterWorld(pair[1]))});
        yield return .5f; Time.timeScale=0;
        Assert(ScoreManager.Instance.CurrentScore>before,"Reduced board actual mouse drag scores and refills");
        Assert(board.GetTileCount()==18,"Reduced board retains 18 cells after matching");
        yield return Capture("03-reduced-after-drag.png");
        SetShape(4,7); yield return .3f; CheckLayout(baselineScale,"Columns only 4x7");
        Assert(Mathf.Abs(camera.pixelHeight-baselineHeight)<1,"Columns alone retain board height");
        SetShape(6,3); yield return .3f; CheckLayout(baselineScale,"Rows only 6x3");
        yield return Capture("04-rows-only-6x3.png");
        SetShape(2,1); yield return .3f; CheckLayout(baselineScale,"Minimum 2x1");
        yield return Capture("05-minimum-2x1.png");
        // Fresh scene with reduced data before board initialization.
        SceneManager.sceneLoaded+=ReducedScene;
        SceneManager.LoadScene("Game"); yield return 1f;
        board=Object.FindFirstObjectByType<PuzzleBoardManager>(); rig=Object.FindFirstObjectByType<DualCameraRig>(); camera=rig.cameraPuzzle;
        game=GameManager.Instance; game.StartGame(); yield return .3f; Time.timeScale=0;
        CheckLayout(baselineScale,"Fresh scene 4x5");
        Assert(Mathf.Abs(camera.pixelHeight-reducedHeight)<1,"Fresh reduced board uses same baseline scale");
        // Taller display plus a simulated notch/home safe area.
        View("SetGameViewSize",1284,2778); yield return .5f;
        PrototypeSafeArea.TestPixels=new Rect(0,100,1284,2550); yield return .4f;
        SetShape(6,7); yield return .3f;
        baselineScale=PixelsPerUnit(camera); baselineScoreY=Score.anchorMin.y;
        baselineRunnerWidth=rig.cameraRunner.orthographicSize*2*rig.cameraRunner.aspect;
        CheckLayout(baselineScale,"Tall safe baseline");
        SetShape(4,5); yield return .4f; CheckLayout(baselineScale,"Tall safe reduced");
        yield return Capture("06-tall-safe-reduced-4x5.png");
        // Oversized data must fit rather than clip or eliminate the runner.
        SetShape(10,12); yield return .4f;
        Assert(rig.CurrentPuzzleRatio<=.9001f && rig.cameraRunner.pixelHeight>0,"Large board keeps runner viewport");
        var bounds=board.GetBoardWorldBounds(); var a=camera.WorldToScreenPoint(bounds.min); var b=camera.WorldToScreenPoint(bounds.max);
        Assert(camera.pixelRect.Contains(a) && camera.pixelRect.Contains(b),"Large board fallback fits all tiles");
        Pass("Actual mouse drag, cold reduced scene, safe area, and oversized fallback verified");
    }
    static void ReducedScene(Scene scene,LoadSceneMode mode)
    {
        SceneManager.sceneLoaded-=ReducedScene;
        var target=Object.FindFirstObjectByType<PuzzleBoardManager>();
        shape=Object.Instantiate(target.gridShapeData);shape.width=4;shape.height=5;target.gridShapeData=shape;
    }
    static void Finish(Exception error)
    {
        EditorApplication.update-=KeepAlive;
        if(driver!=null) {driver.Tick=null;driver.LateTick=null;}
        SceneManager.sceneLoaded-=ReducedScene;
        if(error!=null) results.Add(error.ToString());
        foreach(var message in errors) results.Add("RUNTIME_ERROR "+message);
        Application.logMessageReceived-=Log;
        if(mouse!=null) InputSystem.RemoveDevice(mouse);
        if(previousMouse!=null && previousMouse.added) {if(mouseWasEnabled) InputSystem.EnableDevice(previousMouse);previousMouse.MakeCurrent();}
        InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
        if(hadBest) PlayerPrefs.SetInt(BestKey,oldBest);else PlayerPrefs.DeleteKey(BestKey);PlayerPrefs.Save();
        try {View("RestoreView");}catch(Exception warning) {results.Add(warning.Message);}
        Time.timeScale=1;PrototypeSafeArea.TestPixels=null;
        results.Add($"Assertions={assertions}, runtime errors={errors.Count}");
        File.WriteAllLines(Path.Combine(Output,"play-results.txt"),results);
        SessionState.SetBool(Key,false);EditorApplication.UnlockReloadAssemblies();
        EditorApplication.Exit(error==null && errors.Count==0 ? 0:1);
    }
}
