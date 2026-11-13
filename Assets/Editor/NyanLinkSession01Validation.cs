using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Puzzle;
using NineMood.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Session 01 CLI smoke check. Does not save scenes or exercise user checkpoint files.</summary>
public static class NyanLinkSession01Validation
{
    const string Key = "NyanLink.Session01.Smoke";
    const string Output = "output/playtests/2026-11-13-session01";
    static double deadline;
    static int step;
    static readonly List<string> failures = new List<string>();
    static readonly List<string> checks = new List<string>();
    static readonly List<string> errors = new List<string>();

    public static void RunLifecycle()
    {
        SessionState.SetBool(Key + ".Lifecycle", true);
        Run();
    }

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        Audit();
        EditorSceneManager.OpenScene("Assets/_NyanLink/Scene/Main.unity");
        // Render a real Game view. This invocation intentionally has no -batchmode/-nographics.
        var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        EditorWindow.GetWindow(gameView).Show();
        EditorWindow.GetWindow(gameView).Focus();
        EditorApplication.isPaused = false;
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    public static void Audit()
    {
        Directory.CreateDirectory(Output);
        var notes = new List<string>();
        var stage = Resources.Load<StageData>("Data/Stages/StageData");
        if (stage == null || string.IsNullOrWhiteSpace(stage.stageId) || stage.bossConfig == null || stage.gridShape == null)
            throw new Exception("Test StageData graph is incomplete.");
        if (Resources.Load<BalanceData>("Data/DefaultBalanceData") == null)
            throw new Exception("BalanceData missing.");
        var ids = new HashSet<string>();
        foreach (var data in Resources.LoadAll<StageData>("Data/Stages"))
            if (string.IsNullOrWhiteSpace(data.stageId) || !ids.Add(data.stageId))
                throw new Exception("StageData ID empty or duplicate: " + data.name);
        notes.Add($"Stage: {stage.stageId}; boss: {stage.bossConfig.bossId}; obstacles: {stage.obstacles.Count}");
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            var scene = EditorSceneManager.OpenScene(entry.path);
            var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            int missing = objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            var components = objects.SelectMany(g => g.GetComponents<MonoBehaviour>()).Where(c => c != null).ToArray();
            var eventSystems = components.OfType<EventSystem>().Where(e => e.isActiveAndEnabled).ToArray();
            notes.Add($"{scene.name}: missing scripts={missing}, active EventSystems={eventSystems.Length}, input={string.Join(",", components.OfType<BaseInputModule>().Where(m => m.isActiveAndEnabled).Select(m => m.GetType().Name))}");
            if (missing != 0 || eventSystems.Length != 1 || eventSystems[0].GetComponent<InputSystemUIInputModule>() == null)
                throw new Exception("Scene startup audit failed: " + scene.name);
            foreach (var group in components.Where(c => c.GetType().BaseType?.Name.StartsWith("Singleton") == true).GroupBy(c => c.GetType()))
            {
                notes.Add($"{scene.name}: {group.Key.Name}={group.Count()}");
                if (group.Count() != 1) throw new Exception("Duplicate singleton in scene: " + group.Key.Name);
            }
        }
        File.WriteAllLines(Path.Combine(Output, "audit.txt"), notes);
        Debug.Log("[Session01] Static scene/data audit passed.");
    }

    [InitializeOnLoadMethod]
    static void ResumeAfterDomainReload()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        step = 0;
        deadline = EditorApplication.timeSinceStartup + 5;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
    }

    static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(message + "\n" + trace);
    }

    static void Check(bool ok, string message)
    {
        checks.Add((ok ? "PASS " : "FAIL ") + message);
        if (!ok) failures.Add(message);
        Debug.Log("[Session01] " + checks.Last());
    }

    static void Click(Component controller, string field)
    {
        if (controller == null) throw new Exception("Controller missing for " + field);
        var button = new SerializedObject(controller).FindProperty(field).objectReferenceValue as Button;
        if (button == null || !button.isActiveAndEnabled || !button.interactable)
            throw new Exception("Button unavailable: " + field);
        button.onClick.Invoke(); // Real bound UI callback, not a physical pointer test.
    }

    static void Capture(string name)
    {
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Output, name + ".png")));
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < deadline) return;
        Application.runInBackground = true;
        checks.Add($"INFO step={step}, frames={Time.frameCount}, paused={EditorApplication.isPaused}, focused={Application.isFocused}, timeScale={Time.timeScale}");
        try
        {
            switch (step++)
            {
                case 0:
                    Check(SceneManager.GetActiveScene().name == "Main", "Main opened in Play mode");
                    Check(DataManager.Instance.BalanceData != null && DataManager.Instance.ColorEffectMapping != null, "Shared Resources loaded before first scene");
                    Check(DataManager.Instance.GetStageData("prototype-stage-01") != null, "Stage lookup by stable ID");
                    Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Main has one active EventSystem");
                    Capture("01-main");
                    break;
                case 1:
                    Click(UnityEngine.Object.FindFirstObjectByType<MainMenuController>(), "buttonPlay");
                    break;
                case 2:
                    Check(SceneManager.GetActiveScene().name == "Game", "Play button enters Game");
                    Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Game has one active EventSystem");
                    var board = UnityEngine.Object.FindFirstObjectByType<PuzzleBoardManager>();
                    Check(board != null && board.GetTileCount() > 0, "Puzzle board initialized (39-cell compliance belongs to session 02)");
                    checks.Add("INFO board tile count=" + board?.GetTileCount());
                    Check(GameManager.Instance.CurrentState == GameState.Briefing, "Game enters Briefing");
                    // The old save implementation is not under test. Avoid reading/writing user checkpoint files.
                    SaveManager.Instance.stageData = null;
                    Capture("02-game-ready");
                    break;
                case 3:
                    Click(UnityEngine.Object.FindFirstObjectByType<GameReadyPanel>(), "buttonStart");
                    break;
                case 4:
                    Check(GameManager.Instance.CurrentState == GameState.RunnerPhase, "Start button enters RunnerPhase");
                    Capture("03-game-running");
                    break;
                case 10:
                    Check(SceneManager.GetActiveScene().name == "Main", "Return path loads Main");
                    Capture("04-return-main");
                    break;
                case 11:
                    Click(UnityEngine.Object.FindFirstObjectByType<MainMenuController>(), "buttonPlay");
                    break;
                case 12:
                    Check(SceneManager.GetActiveScene().name == "Game", "Second Play loads Game");
                    var secondBoard = UnityEngine.Object.FindFirstObjectByType<PuzzleBoardManager>();
                    Check(secondBoard != null && secondBoard.GetTileCount() > 0, "Reentered Game board initialized");
                    Check(SaveManager.Instance.progressSystem != null && SaveManager.Instance.obstacleSpawner != null,
                        "Persistent SaveManager retains live scene references");
                    Check(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1,
                        "One GameManager after reentry");
                    Capture("05-game-reentry");
                    break;
                case 13:
                    File.WriteAllLines(Path.Combine(Output, "lifecycle-results.txt"), checks.Concat(errors));
                    Finish(failures.Count == 0 ? 0 : 1);
                    return;
                default:
                    foreach (var file in new[] { "01-main.png", "02-game-ready.png", "03-game-running.png" })
                        Check(File.Exists(Path.Combine(Output, file)) && new FileInfo(Path.Combine(Output, file)).Length > 0, "Screenshot saved: " + file);
                    Check(errors.Count == 0, "No runtime errors during Main→Game→Runner smoke");
                    File.WriteAllLines(Path.Combine(Output, "smoke-results.txt"), checks.Concat(errors));
                    if (SessionState.GetBool(Key + ".Lifecycle", false))
                    {
                        step = 10;
                        SceneLoader.LoadMainMenu();
                        break;
                    }
                    Finish(failures.Count == 0 ? 0 : 1);
                    return;
            }
            deadline = EditorApplication.timeSinceStartup + 2;
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(Output, "smoke-failure.txt"), e.ToString());
            Debug.LogException(e);
            Finish(1);
        }
    }

    static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        SessionState.SetBool(Key + ".Lifecycle", false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        EditorApplication.Exit(code);
    }
}
