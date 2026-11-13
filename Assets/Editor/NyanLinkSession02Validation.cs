using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NineMood.Core;
using NineMood.Core.HexCoordinates;
using NineMood.Core.Managers;
using NineMood.Data.Definitions;
using NineMood.Data.Enums;
using NineMood.Puzzle;
using NineMood.Runner;
using NineMood.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>CLI geometry tests and actual rendered Play mode with queued Input System mouse/touch events.</summary>
public static class NyanLinkSession02Validation
{
    const string Key = "NyanLink.Session02";
    const string Output = "output/playtests/2026-11-13-session02";
    const string BestKey = "NyanLink_BestScore";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly List<string> results = new List<string>();
    static int assertions, matches;
    static readonly Dictionary<EditorWindow,int> originalViews = new Dictionary<EditorWindow,int>();
    static IEnumerator sequence;
    static NyanLinkSession02PlayDriver driver;
    static double next, expires;
    static int frame;
    static Mouse mouse;
    static Mouse previousMouse;
    static bool previousMouseEnabled;
    static Touchscreen touch;
    static PuzzleBoardManager board;
    static TileInputHandler input;
    static Camera camera;
    static bool hadBest;
    static int oldBest;
    static InputSettings.BackgroundBehavior oldBackground;
    static InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;

    static void Assert(bool condition, string description)
    {
        assertions++;
        if (!condition) throw new Exception("FAIL " + description);
    }
    static void Pass(string description)
    {
        results.Add("PASS " + description);
        Debug.Log("[Session02] " + results.Last());
    }
    static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);

    public static void ValidateGeometry()
    {
        Directory.CreateDirectory(Output);
        assertions = 0; results.Clear();
        for (int y = -10; y <= 10; y++)
            for (int x = -10; x <= 10; x++)
            {
                var cell = Cell(x, y);
                var cube = HexCoordinates.OffsetToCube(cell);
                Assert(cube.q + cube.r + cube.s == 0 && cube.ToOffset() == cell, "Cube invariant/round trip " + cell);
                var neighbors = HexOffsetUtils.GetNeighbors(cell);
                Assert(neighbors.Length == 6 && neighbors.Distinct().Count() == 6, "Exactly six neighbors");
                foreach (var neighbor in neighbors)
                {
                    Assert(HexOffsetUtils.GetDistance(cell, neighbor) == 1, "Neighbor distance");
                    Assert(HexOffsetUtils.GetNeighbors(neighbor).Contains(cell), "Reciprocal adjacency");
                }
            }
        Assert(!HexOffsetUtils.IsAdjacent(Cell(0, 0), Cell(1, -1)), "Known seventh-neighbor regression");
        var even = new[] { Cell(1,0), Cell(0,-1), Cell(-1,-1), Cell(-1,0), Cell(-1,1), Cell(0,1) };
        Assert(new HashSet<Vector3Int>(even).SetEquals(HexOffsetUtils.GetNeighbors(Cell(0,0))), "Even-row expected neighbors");
        var odd = new[] { Cell(1,1), Cell(1,0), Cell(0,0), Cell(-1,1), Cell(0,2), Cell(1,2) };
        Assert(new HashSet<Vector3Int>(odd).SetEquals(HexOffsetUtils.GetNeighbors(Cell(0,1))), "Odd-row expected neighbors");
        foreach (var center in new[] { Cell(0,0), Cell(7,-9), Cell(-8,11) })
            for (int radius = 0; radius <= 3; radius++)
            {
                var cells = HexOffsetUtils.GetRange(center, radius).ToArray();
                Assert(cells.Length == 1 + 3 * radius * (radius + 1) && cells.Distinct().Count() == cells.Length, "Translated blast disk size");
                Assert(cells.All(c => HexOffsetUtils.GetDistance(center, c) <= radius), "Blast disk distance");
            }
        for (int ay = -2; ay <= 2; ay++) for (int ax = -2; ax <= 2; ax++)
            for (int by = -2; by <= 2; by++) for (int bx = -2; bx <= 2; bx++)
            {
                var a = HexCoordinates.OffsetToCube(Cell(ax, ay));
                var b = HexCoordinates.OffsetToCube(Cell(bx, by));
                var line = HexUtils.GetLine(a, b);
                Assert(line.Count == a.DistanceTo(b) + 1 && line[0] == a && line.Last() == b, "Fractional Cube line endpoints/length");
                Assert(line.Distinct().Count() == line.Count, "Line has no duplicate steps");
                for (int i = 1; i < line.Count; i++) Assert(line[i-1].IsAdjacentTo(line[i]), "Line steps are neighbors");
                Assert(a.DistanceTo(b) == b.DistanceTo(a), "Distance symmetry");
            }
        var shape = Resources.Load<GridShapeData>("Data/GridShapes/DefaultGridShape");
        Assert(shape != null && shape.width == 6 && shape.height == 7, "Prototype shape dimensions");
        Assert(shape.GetActiveOffsets().Count() == 39, "39 mask cells");
        int counted = 0;
        for (int y = -2; y < 10; y++) for (int x = -2; x < 9; x++)
        {
            bool expected = y >= 0 && y < 7 && x >= 0 && x < (y % 2 == 0 ? 6 : 5);
            Assert(shape.IsTileActive(Cell(x,y)) == expected, "Mask edge " + Cell(x,y));
            if (expected) counted++;
        }
        Assert(counted == 39 && !shape.IsTileActive(new Vector3Int(0,0,1)), "No cells outside 2D mask");
        for (int d = 0; d < 6; d++)
        {
            var origin = new HexCoordinates(2,-3);
            var delta = new HexCoordinates(0,0).GetNeighbor(d);
            var far = origin + delta + delta + delta;
            Assert(HexOffsetUtils.IsOnLine(origin.ToOffset(), far.ToOffset(), true), "Three shared line axes");
            Assert(HexOffsetUtils.IsOnLine(origin.ToOffset(), far.ToOffset(), false) == (d == 0 || d == 3), "Horizontal line only");
        }
        Pass($"Geometry: {assertions} assertions (negative rows, 6 neighbors, distances, radius/line, 39-cell mask)");
        File.WriteAllLines(Path.Combine(Output, "geometry-results.txt"), results);
    }

    public static void Run()
    {
        ValidateGeometry();
        EditorSceneManager.OpenScene("Assets/_NyanLink/Scene/Main.unity");
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".HadBest", PlayerPrefs.HasKey(BestKey));
        SessionState.SetInt(Key + ".Best", PlayerPrefs.GetInt(BestKey));
        var window = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        window.Show(); window.Focus();
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (!SessionState.GetBool(Key, false)) return;
        results.Clear(); assertions = 0;
        hadBest = SessionState.GetBool(Key + ".HadBest", false);
        oldBest = SessionState.GetInt(Key + ".Best", 0);
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        oldBackground = InputSystem.settings.backgroundBehavior;
        oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += KeepAlive;
        expires = EditorApplication.timeSinceStartup + 150;
        next = EditorApplication.timeSinceStartup + 4;
        sequence = PlaySequence();
    }

    static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            results.Add("RUNTIME_ERROR " + message + "\n" + trace);
    }

    static void KeepAlive()
    {
        Application.runInBackground = true;
        if (EditorApplication.timeSinceStartup > expires) { Finish(new Exception("Play test timed out")); return; }
        if (EditorApplication.isPlaying && driver == null)
        {
            driver = new GameObject("Session02 validation (temporary)").AddComponent<NyanLinkSession02PlayDriver>();
            driver.gameObject.hideFlags = HideFlags.DontSave;
            driver.Tick = Tick;
        }
    }

    // Called only by the runtime driver's Update. EditorApplication.update has a different Screen/Camera context.
    static void Tick()
    {
        Application.runInBackground = true;
        if (EditorApplication.timeSinceStartup > expires) { Finish(new Exception("Play test timed out")); return; }
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next || Time.frameCount < frame) return;
        try
        {
            if (!sequence.MoveNext()) { Finish(null); return; }
            Debug.Log($"[Session02] Input step advanced; frame={Time.frameCount}, focus={Application.isFocused}");
            next = EditorApplication.timeSinceStartup + (sequence.Current is float delay ? delay : 0.15f);
            frame = Time.frameCount + 3;
        }
        catch (Exception e) { Finish(e); }
    }

    static Button Button(Component controller, string field) =>
        new SerializedObject(controller).FindProperty(field).objectReferenceValue as Button;
    static Vector2 ButtonPosition(Button button) => RectTransformUtility.WorldToScreenPoint(null,
        button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));
    static Vector2 Position(Vector3Int cell) => camera.WorldToScreenPoint(board.GetCellCenterWorld(cell));
    static void MouseStateAt(Vector2 position, bool down)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, down));
    }
    static void At(Vector3Int cell, bool down) => MouseStateAt(Position(cell), down);
    static void TouchAt(Vector3Int cell, UnityEngine.InputSystem.TouchPhase phase) =>
        InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = phase, position = Position(cell), pressure = 1f });
    static void Capture(string file) => ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Output, file)));
    static void Match(int chain, int score, int removed, TileColor color) { matches++; }
    static void Fill(TileColor color)
    {
        board.SetRequiredMatchColor(null);
        foreach (var cell in board.gridShapeData.GetActiveOffsets()) board.SpawnTile(cell, color, TileState.Normal);
    }
    static bool HasPair(TileColor color) => board.GetAllTileOffsets().Any(c =>
        board.GetTileAtOffset(c).Color == color && HexOffsetUtils.GetNeighbors(c).Any(n => board.GetTileAtOffset(n)?.Color == color));

    static void CheckLayout()
    {
        Assert(board.GetTileCount() == 39, "Rendered board has 39 cells");
        var rows = board.GetAllTileOffsets().GroupBy(c => c.y).OrderBy(g => g.Key).Select(g => g.Count()).ToArray();
        Assert(rows.SequenceEqual(new[] { 6,5,6,5,6,5,6 }), "Rendered row lengths");
        Bounds bounds = board.GetBoardWorldBounds();
        foreach (var point in new[] { bounds.min, bounds.max })
        {
            var view = camera.WorldToViewportPoint(point);
            Assert(view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f, "Full sprite bounds fit puzzle viewport");
        }
        foreach (var cell in board.GetAllTileOffsets())
        {
            Assert(input.TryGetTileAtScreenPosition(Position(cell), out var selected) && selected == cell, "Mouse center picking " + cell);
            Assert(input.TryGetTileAtScreenPosition(Position(cell), out selected, true) && selected == cell, "Touch 1.3 center picking " + cell);
        }
        var centerForHit = Cell(2,2);
        foreach (var neighbor in HexOffsetUtils.GetNeighbors(centerForHit))
            foreach (float fraction in new[] { 0.49f, 0.51f })
            {
                var world = Vector3.Lerp(board.GetCellCenterWorld(centerForHit), board.GetCellCenterWorld(neighbor), fraction);
                var screen = camera.WorldToScreenPoint(world);
                var expected = fraction < 0.5f ? centerForHit : neighbor;
                Assert(input.TryGetTileAtScreenPosition(screen,out var picked,true) && picked == expected,
                    "Overlapping 1.3 touch areas choose nearest center on both sides of edge");
            }
        Assert(!input.TryGetTileAtScreenPosition(new Vector2(Screen.width / 2f, Screen.height * 0.8f), out _), "Runner viewport rejects puzzle picking");
        Assert(!input.TryGetTileAtScreenPosition(new Vector2(-1, 0), out _), "Outside screen rejected");
        // World geometry is checked against Unity's actual rendered Grid, not an independent offset approximation.
        var origin = board.GetCellCenterWorld(Cell(2,2));
        var horizontal = board.GetCellCenterWorld(Cell(3,2)) - origin;
        var upper = board.GetCellCenterWorld(Cell(2,3)) - origin;
        Assert(Mathf.Abs(upper.x - horizontal.x * 0.5f) < 0.001f, "Odd row rendered half-column shift");
        Assert(upper.y > 0f, "Row direction matches odd-r conversion");
    }

    static void CheckRepair()
    {
        Fill(TileColor.Blue);
        board.SpawnTile(Cell(0,0), TileColor.Red, TileState.ItemLv1);
        board.SpawnTile(Cell(5,0), TileColor.Yellow, TileState.ItemLv2);
        var original = board.GetAllTileOffsets().ToDictionary(c => c, c => board.GetTileAtOffset(c).Color);
        var protectedCells = new HashSet<Vector3Int> { Cell(1,0), Cell(1,1) };
        board.GetTileAtOffset(Cell(2,1)).IsSelected = true;
        Assert(board.TryEnsureAdjacentPair(TileColor.Red, protectedCells) && HasPair(TileColor.Red), "Repair creates required adjacent pair");
        Assert(board.GetAllTileOffsets().Count(c => original[c] != board.GetTileAtOffset(c).Color) == 1, "Repair uses minimum one recolor next to existing color");
        Assert(board.GetTileAtOffset(Cell(5,0)).State == TileState.ItemLv2 && board.GetTileAtOffset(Cell(5,0)).Color == TileColor.Yellow, "Repair preserves other item");
        Assert(board.GetItemTileCount() == 2 && protectedCells.All(c => board.GetTileAtOffset(c).Color == original[c]), "Repair preserves item count/protected cells");
        Assert(board.GetTileAtOffset(Cell(2,1)).Color == TileColor.Blue, "Repair does not recolor selected tile");
        board.GetTileAtOffset(Cell(2,1)).IsSelected = false;
        var fingerprint = string.Join(";", board.GetAllTileOffsets().Select(c => c + ":" + board.GetTileAtOffset(c).Color));
        Assert(!board.TryEnsureAdjacentPair(TileColor.Purple, new HashSet<Vector3Int>(board.GetAllTileOffsets())), "Fully protected board reports inability");
        Assert(fingerprint == string.Join(";", board.GetAllTileOffsets().Select(c => c + ":" + board.GetTileAtOffset(c).Color)), "Failed repair is atomic");
        Fill(TileColor.Blue);
        foreach (TileColor color in Enum.GetValues(typeof(TileColor)))
            Assert(board.SetRequiredMatchColor(color) && HasPair(color), "Required color transition " + color);
        Fill(TileColor.Blue);
        foreach (var cell in board.GetAllTileOffsets().ToArray()) board.SetTileState(cell, TileState.ItemLv1);
        Assert(!board.TryEnsureAdjacentPair(TileColor.Red), "All incompatible items cannot be recolored");
        Assert(board.GetItemTileCount() == 39 && board.GetAllTileOffsets().All(c => board.GetTileAtOffset(c).Color == TileColor.Blue), "Impossible item repair preserves all states");
        Fill(TileColor.Blue);
        Assert(board.SpawnTile(Cell(5,1), TileColor.Red) == null && board.GetTileCount() == 39, "Refill cannot create cells outside mask");
        Pass("Required-color API: minimum repair, transitions, items/selection/protection preserved, atomic failure");
    }

    static IEnumerator PlaySequence()
    {
        SetGameViewSize(1080, 1920); yield return 1f;
        Debug.Log($"[Session02] Actual rendering size {Screen.width}x{Screen.height}");
        previousMouse = Mouse.current;
        previousMouseEnabled = previousMouse != null && previousMouse.enabled;
        if (previousMouseEnabled) InputSystem.DisableDevice(previousMouse);
        mouse = InputSystem.AddDevice<Mouse>("NyanLinkSession02Mouse");
        mouse.MakeCurrent();
        touch = InputSystem.AddDevice<Touchscreen>("NyanLinkSession02Touch");
        var play = Button(Object.FindFirstObjectByType<MainMenuController>(), "buttonPlay");
        Vector2 playPosition = ButtonPosition(play);
        MouseStateAt(playPosition, true); yield return 0.2f;
        MouseStateAt(playPosition, false); yield return 2f;
        Assert(SceneManager.GetActiveScene().name == "Game", "Queued mouse clicks actual Main Play button");
        board = Object.FindFirstObjectByType<PuzzleBoardManager>();
        input = Object.FindFirstObjectByType<TileInputHandler>();
        camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c => c.name == "Camera_Puzzle");
        SaveManager.Instance.stageData = null;
        Object.FindFirstObjectByType<ObstacleSpawner>().enabled = false;
        Object.FindFirstObjectByType<RunnerProgressSystem>().SetPaused(true);
        var start = Button(Object.FindFirstObjectByType<GameReadyPanel>(), "buttonStart");
        Vector2 startPosition = ButtonPosition(start);
        MouseStateAt(startPosition, true); yield return 0.2f;
        MouseStateAt(startPosition, false); yield return 1f;
        Object.FindFirstObjectByType<RunnerProgressSystem>().SetPaused(true);
        Assert(GameManager.Instance.CurrentState == GameState.RunnerPhase, "Queued mouse clicks actual Game Start button");
        Assert(Screen.width == 1080 && Screen.height == 1920, "Actual 1080x1920 rendering: " + Screen.width + "x" + Screen.height);
        CheckLayout();
        Capture("01-board39-1080x1920.png"); yield return 0.5f;
        Pass("Actual Main→Game→Start via queued mouse, 39 visible cells, screen mapping and viewport boundaries");
        CheckRepair();
        GameEvents.OnMatchCompleted += Match;
        var a = Cell(1,2); var b = Cell(2,2); var c = Cell(2,3);
        Fill(TileColor.Blue);
        foreach (var cell in new[] { a,b,c }) board.SpawnTile(cell, TileColor.Red);
        At(a, true); yield return 0.15f;
        At(b, true); yield return 0.15f;
        At(c, true); yield return 0.15f;
        Assert(input.SelectedChain.SequenceEqual(new[] { a,b,c }) && matches == 0, "Three-cell diagonal chain before release");
        Capture("02-diagonal-chain-three.png"); yield return 0.5f;
        At(b, true); yield return 0.15f;
        Assert(input.SelectedChain.SequenceEqual(new[] { a,b }) && !board.GetTileAtOffset(c).IsSelected, "Backtrack retains returned cell");
        At(a, true); yield return 0.15f;
        Assert(input.SelectedChain.SequenceEqual(new[] { a }) && !board.GetTileAtOffset(b).IsSelected, "Backtrack removes all later cells");
        At(a, false); yield return 0.15f;
        Assert(matches == 0 && input.SelectedChain.Count == 0, "Single-cell release does not match");
        At(a, true); yield return 0.15f;
        At(Cell(1,3), true); yield return 0.15f;
        Assert(!input.IsSelecting && input.SelectedChain.Count == 0 && !input.IsInputLocked, "Other color cancels without locking");
        At(Cell(1,3), false); yield return 0.05f;
        At(a, true); yield return 0.05f;
        Assert(input.SelectedChain.Count == 1 && !input.IsInputLocked, "New gesture starts immediately after color cancel");
        At(a, false); yield return 0.1f;
        Pass("Queued mouse: diagonal selection, backtracking, single release, other-color cancel without lock");
        // All six visual neighbor directions must accept exactly one match on release.
        var center = Cell(2,2);
        foreach (var neighbor in HexOffsetUtils.GetNeighbors(center))
        {
            Fill(TileColor.Blue);
            board.SpawnTile(center, TileColor.Red); board.SpawnTile(neighbor, TileColor.Red);
            int before = matches;
            At(center,true); yield return 0.1f;
            At(neighbor,true); yield return 0.1f;
            Assert(input.SelectedChain.Count == 2 && matches == before, "Adjacent drag waits for release " + neighbor);
            var unchanged = board.GetAllTileOffsets().Where(x => x != center && x != neighbor).ToDictionary(x => x, x => board.GetTileAtOffset(x));
            At(neighbor,false); yield return 0.2f;
            Assert(matches == before + 1 && board.GetTileCount() == 39, "Exactly one release match/refill");
            Assert(unchanged.All(pair => ReferenceEquals(pair.Value, board.GetTileAtOffset(pair.Key))), "Refill is in place, unaffected cells stay unchanged");
        }
        Fill(TileColor.Red);
        int total = matches;
        At(Cell(2,2),true); yield return 0.1f;
        At(Cell(3,1),true); yield return 0.1f;
        Assert(input.SelectedChain.Count == 1, "Known nonneighbor drag is rejected");
        At(Cell(3,1),false); yield return 0.1f;
        Assert(matches == total, "Rejected nonneighbor does not match");
        Fill(TileColor.Blue);
        board.SetRequiredMatchColor(TileColor.Orange);
        // Destroy the guaranteed pair through a controlled fixture, then let real refill repair it.
        foreach (var cell in board.GetAllTileOffsets().ToArray()) board.SpawnTile(cell, TileColor.Blue);
        board.SpawnTile(a,TileColor.Red); board.SpawnTile(b,TileColor.Red);
        At(a,true); yield return 0.1f; At(b,true); yield return 0.1f; At(b,false); yield return 0.2f;
        Assert(HasPair(TileColor.Orange) && board.GetTileCount() == 39, "Actual match refill rechecks registered required color");
        Pass("Six visual neighbor directions, no seventh neighbor, one event per release, in-place refill and post-refill repair");
        Fill(TileColor.Blue); board.SpawnTile(a,TileColor.Red); board.SpawnTile(b,TileColor.Red);
        total = matches;
        TouchAt(a,UnityEngine.InputSystem.TouchPhase.Began); yield return 0.15f;
        TouchAt(b,UnityEngine.InputSystem.TouchPhase.Moved); yield return 0.15f;
        TouchAt(b,UnityEngine.InputSystem.TouchPhase.Canceled); yield return 0.15f;
        Assert(matches == total && !input.IsSelecting, "Canceled touch never submits a match");
        TouchAt(a,UnityEngine.InputSystem.TouchPhase.Began); yield return 0.15f;
        TouchAt(b,UnityEngine.InputSystem.TouchPhase.Moved); yield return 0.15f;
        Assert(input.SelectedChain.Count == 2, "Enhanced Touch reaches shared diagonal/match input path");
        TouchAt(b,UnityEngine.InputSystem.TouchPhase.Ended); yield return 0.2f;
        Assert(matches == total + 1 && !input.IsSelecting, "Enhanced Touch releases exactly once with mouse connected");
        At(a,true); yield return 0.15f;
        input.LockInput(0.5f);
        Assert(input.IsInputLocked && !input.IsSelecting, "External lock cancels active gesture");
        At(a,false); yield return 0.6f;
        At(a,true); yield return 0.15f;
        Assert(input.IsSelecting && !input.IsInputLocked, "External lock expires without stale gesture");
        At(a,false); yield return 0.15f;
        Pass("Queued Enhanced Touch with mouse connected; external lock cancellation and expiry");
        var originalAnchor = board.tilemap.tileAnchor;
        var originalPosition = board.transform.localPosition;
        var originalRotation = board.transform.localRotation;
        var originalScale = board.transform.localScale;
        foreach (var anchor in new[] { Vector3.zero, new Vector3(0.25f,0.75f,0f), originalAnchor })
        {
            board.tilemap.tileAnchor = anchor;
            board.transform.localPosition = new Vector3(2f,-1f,0f);
            board.transform.localRotation = Quaternion.Euler(0,0,12);
            board.transform.localScale = new Vector3(1.2f,0.8f,1f);
            board.FitCameraToBoard();
            foreach (var cell in board.GetAllTileOffsets())
                Assert(input.TryGetTileAtScreenPosition(Position(cell), out var picked) && picked == cell, "Anchor/transform picking " + cell);
        }
        board.tilemap.tileAnchor = originalAnchor; board.transform.localPosition = originalPosition;
        board.transform.localRotation = originalRotation; board.transform.localScale = originalScale;
        Fill(TileColor.Blue);
        foreach (var cell in board.GetAllTileOffsets().ToArray()) board.SpawnTile(cell);
        SetGameViewSize(1284,2778); yield return 1f;
        board.FitCameraToBoard(); CheckLayout();
        Assert(Screen.width == 1284 && Screen.height == 2778, "Actual tall rendering resolution");
        Capture("03-board39-1284x2778.png"); yield return 0.5f;
        foreach (var file in new[] { "01-board39-1080x1920.png", "02-diagonal-chain-three.png", "03-board39-1284x2778.png" })
            Assert(File.Exists(Path.Combine(Output,file)) && new FileInfo(Path.Combine(Output,file)).Length > 0, "Actual screenshot saved " + file);
        Pass($"Anchor/transform picking; full board bounds at 1080x1920 and 1284x2778; {assertions} Play assertions");
    }

    static void SetGameViewSize(int width, int height)
    {
        var assembly = typeof(Editor).Assembly;
        var type = assembly.GetType("UnityEditor.GameViewSizes");
        var instance = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        var groupMethod = type.GetMethod("GetGroup");
        var activeTarget = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString().Replace("iPhone", "iOS");
        var groupType = type.GetProperty("currentGroupType", Flags | BindingFlags.Static).GetValue(instance);
        Debug.Log($"[Session02] GameView group={groupType}; build target={activeTarget}");
        var group = groupMethod.Invoke(instance, new[] { groupType });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var sizeEnum = assembly.GetType("UnityEditor.GameViewSizeType");
        var constructor = sizeType.GetConstructor(Flags, null, new[] { sizeEnum, typeof(int), typeof(int), typeof(string) }, null);
        var size = constructor.Invoke(new object[] { Enum.Parse(sizeEnum,"FixedResolution"), width, height, "NyanLink Session02" });
        var window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        var selection = window.GetType().GetProperty("selectedSizeIndex", Flags);
        if (!SessionState.GetBool(Key + ".ViewSaved", false))
        {
            SessionState.SetInt(Key + ".ViewIndex", (int)selection.GetValue(window));
            SessionState.SetInt(Key + ".CustomCount", (int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null));
            SessionState.SetBool(Key + ".ViewSaved", true);
        }
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null) - 1;
        var views = Resources.FindObjectsOfTypeAll(assembly.GetType("UnityEditor.GameView")).Cast<EditorWindow>().ToArray();
        foreach (var view in views)
        {
            if (!originalViews.ContainsKey(view)) originalViews[view] = (int)selection.GetValue(view);
            var callback = view.GetType().GetMethod("SizeSelectionCallback", Flags);
            if (callback != null) callback.Invoke(view, new object[] { index, null });
            else selection.SetValue(view, index);
            view.Repaint();
            var sizeNow = view.GetType().GetProperty("currentGameViewSize", Flags).GetValue(view);
            Debug.Log($"[Session02] View={view.GetInstanceID()} selected={selection.GetValue(view)} requested={sizeNow.GetType().GetProperty("width").GetValue(sizeNow)}x{sizeNow.GetType().GetProperty("height").GetValue(sizeNow)} target={view.GetType().GetProperty("targetRenderSize", Flags)?.GetValue(view)}");
        }
        Debug.Log($"[Session02] GameView count={views.Length}");
        window.Show(); window.Focus(); window.Repaint();
    }

    static void RestoreView()
    {
        if (!SessionState.GetBool(Key + ".ViewSaved",false)) return;
        var assembly = typeof(Editor).Assembly;
        var type = assembly.GetType("UnityEditor.GameViewSizes");
        var instance = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        var getGroup = type.GetMethod("GetGroup");
        var groupType = type.GetProperty("currentGroupType", Flags | BindingFlags.Static).GetValue(instance);
        var group = getGroup.Invoke(instance,new[] { groupType });
        var window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        foreach (var pair in originalViews)
        {
            if (pair.Key == null) continue;
            var callback = pair.Key.GetType().GetMethod("SizeSelectionCallback", Flags);
            if (callback != null) callback.Invoke(pair.Key,new object[] { pair.Value, null });
            else pair.Key.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(pair.Key,pair.Value);
        }
        originalViews.Clear();
        var getCount = group.GetType().GetMethod("GetCustomCount");
        int removeCount = (int)getCount.Invoke(group,null) - SessionState.GetInt(Key + ".CustomCount", 0);
        for (int i = 0; i < removeCount; i++)
            group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[] {
                (int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null) - 1 });
        SessionState.SetBool(Key + ".ViewSaved",false);
    }

    static void Finish(Exception error)
    {
        if (error != null) results.Add(error.ToString());
        bool runtimeError = results.Any(r => r.StartsWith("RUNTIME_ERROR"));
        if (error == null && !runtimeError) Pass("Play suite completed without runtime errors");
        File.WriteAllLines(Path.Combine(Output,"play-results.txt"),results);
        GameEvents.OnMatchCompleted -= Match;
        Application.logMessageReceived -= OnLog;
        EditorApplication.update -= KeepAlive;
        if (driver != null) driver.Tick = null;
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        if (touch != null) InputSystem.RemoveDevice(touch);
        if (previousMouse != null && previousMouse.added)
        {
            if (previousMouseEnabled) InputSystem.EnableDevice(previousMouse);
            previousMouse.MakeCurrent();
        }
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        if (hadBest) PlayerPrefs.SetInt(BestKey,oldBest); else PlayerPrefs.DeleteKey(BestKey);
        PlayerPrefs.Save();
        try { RestoreView(); } catch (Exception e) { results.Add("View restore warning: " + e.Message); }
        SessionState.SetBool(Key,false);
        File.WriteAllLines(Path.Combine(Output,"play-results.txt"),results);
        EditorApplication.Exit(error == null && !runtimeError ? 0 : 1);
    }
}
