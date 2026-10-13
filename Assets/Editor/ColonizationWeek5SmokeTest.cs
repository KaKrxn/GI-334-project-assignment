using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Uses the saved Map01 and actual keyboard updates/physics raycasts. The coast
// fixture relocates the starting ship to existing generated terrain, like the
// manual's accelerated travel test; movement and disembarkation are animated.
public static class ColonizationWeek5SmokeTest
{
    private const string Request = "Temp/ColonizationWeek5SmokeTest.v1.request";
    private const string Result = "Temp/ColonizationWeek5SmokeTest.result.txt";
    private const string Pending = "Colonization.Week5SmokeTest.Pending";
    private static Keyboard keyboard;
    private static InputSettings.EditorInputBehaviorInPlayMode oldInput;
    private static InputSettings.BackgroundBehavior oldBackground;
    private static bool oldRunBackground;
    private static int frame, lastFrame, phase, phaseFrame, oceanMoves;
    private static double started;
    private static string error;
    private static NavalUnit ship;
    private static LandUnit pioneer;
    private static Hex destination, origin, coastSea, forest;
    private static int forestCost, nativeCount;
    private static Dictionary<Unit, Hex> enemyPositions;
    private static CursorLockMode oldCursorLock;
    private static bool oldCursorVisible;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (SessionState.GetBool(Pending, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) error = message + "\n" + stack;
        };
    }

    [MenuItem("Tools/Colonization/Run Week 5 Play Mode Smoke Test")]
    public static void Run()
    {
        if (Object.FindAnyObjectByType<UIManager>() != null) { ColonizationWeek6SmokeTest.Run(); return; }
        RunBase();
    }

    internal static void RunBase()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Open saved Map01 alone in Edit Mode before the Week 5 smoke test.");
        ColonizationWeek5Setup.ValidateConfiguration(Object.FindAnyObjectByType<GameManager>());
        SessionState.SetBool(Pending, true);
        File.WriteAllText(Result, "RUNNING\n");
        EditorApplication.isPlaying = true;
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            frame = phase = phaseFrame = oceanMoves = 0;
            lastFrame = -1;
            error = null;
            started = EditorApplication.timeSinceStartup;
            oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldRunBackground = Application.runInBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
            File.WriteAllText(Result, "FAILED\nPlay Mode exited before completion.\n");
        }
    }

    private static void Tick()
    {
        if (File.Exists(Request) && !File.Exists(ColonizationWeek5Setup.RequestPath) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            File.Delete(Request);
            try { Run(); } catch (Exception e) { File.WriteAllText(Result, "FAILED\n" + e); }
        }
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (EditorApplication.timeSinceStartup - started < 2) return;
        try
        {
            if (error != null) throw new InvalidOperationException(error);
            if (EditorApplication.timeSinceStartup - started > 180) throw new TimeoutException("Week 5 smoke test timed out at phase " + phase);
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            frame++;
            phaseFrame++;
            GameManager manager = GameManager.instance;
            switch (phase)
            {
                case 0:
                    ValidateStart(manager);
                    ColonizationWeek4SmokeTest.ValidateAllUnitInitializers(manager);
                    keyboard = InputSystem.AddDevice<Keyboard>();
                    Town sample = manager.Factions[9].Towns[0];
                    CameraController.instance.MoveCamera(sample.CurPos);
                    Capture("Temp/ColonizationWeek5NativeTowns.png");
                    CameraController.instance.MoveCamera(ship.CurPos);
                    KeyDown(Key.Tab); Next(); break;
                case 1:
                    if (phaseFrame < 4) break;
                    CheckSelected(manager.PlayerFaction.Units[1]); KeyUp(); Next(); break;
                case 2:
                    if (phaseFrame < 4) break;
                    KeyDown(Key.Tab); Next(); break;
                case 3:
                    if (phaseFrame < 4) break;
                    CheckSelected(pioneer);
                    Require(!manager.PlayerFaction.Units[1].gameObject.activeSelf, "Previous passenger remained shown.");
                    KeyUp(); Next(); break;
                case 4:
                    if (phaseFrame < 4) break;
                    KeyDown(Key.Tab); Next(); break;
                case 5:
                    if (phaseFrame < 4) break;
                    CheckSelected(ship);
                    Require(ship.Passengers.All(p => !p.gameObject.activeSelf), "Tab did not hide passengers.");
                    KeyUp();
                    ClickAt(ship.transform.position, PointerEventData.InputButton.Left, typeof(Unit));
                    Dispatch(manager.Factions[1].Units[0].gameObject, PointerEventData.InputButton.Left);
                    Require(manager.CurUnit == ship, "Enemy click took player control.");
                    BeginOceanMove(manager); Next(); break;
                case 6:
                    if (ship.IsMoving) break;
                    Require(ship.CurHex == destination && ship.TargetHex == null && ship.MovePoint == 4 - oceanMoves, "Ocean arrival/cost failed.");
                    CheckPassengerPositions(ship);
                    if (oceanMoves < 3) BeginOceanMove(manager);
                    else
                    {
                        Require(!origin.Visible && Overlay(origin, "fogSprite") && !Overlay(origin, "darkSprite"), "Explored fog regressed.");
                        SetupCoastFixture(manager);
                        ValidateDisembarkGuards(manager);
                        Hex adjacentSea = HexCalculator.GetHexAround(manager.AllHexes, coastSea).First(h => h.HexType == HexType.Ocean);
                        ship.PrepareMoveToHex(adjacentSea);
                        Require(ship.IsMoving, "Coast ship test did not start.");
                        pioneer.PrepareMoveToHex(forest);
                        Require(pioneer.UnitStatus == UnitStatus.OnBoard && pioneer.TransportShip == ship, "Passenger left a moving ship.");
                        Next();
                    }
                    break;
                case 7:
                    if (ship.IsMoving) break;
                    CheckPassengerPositions(ship);
                    PositionShipFixture(coastSea);
                    CameraController.instance.MoveCamera(coastSea.Pos);
                    ClickAt(forest.transform.position, PointerEventData.InputButton.Right, typeof(Hex));
                    Require(pioneer.IsMoving && pioneer.UnitStatus == UnitStatus.None && pioneer.TransportShip == null && !ship.Passengers.Contains(pioneer) && pioneer.transform.parent == manager.PlayerFaction.UnitParent, "Accepted landfall did not detach passenger.");
                    manager.Endturn();
                    Require(manager.PlayerTurn && manager.GameTurn == 1, "Turn ended while player was moving.");
                    Next(); break;
                case 8:
                    if (pioneer.IsMoving) break;
                    Require(pioneer.CurHex == forest && pioneer.CurPos == forest.Pos && pioneer.MovePoint == pioneer.MovePointMax - forestCost, "Landfall position/cost failed.");
                    pioneer.BuildSettlement();
                    Require(pioneer.UnitStatus == UnitStatus.None && pioneer.ToolsNum == 100, "Building on forest consumed tools.");
                    pioneer.ToolsNum = 19; pioneer.ClearingLand();
                    Require(pioneer.UnitStatus == UnitStatus.None && pioneer.ToolsNum == 19, "Insufficient tools accepted.");
                    pioneer.ToolsNum = 100;
                    KeyDown(Key.P); Next(); break;
                case 9:
                    if (phaseFrame < 4) break;
                    Require(pioneer.UnitStatus == UnitStatus.Clearing && pioneer.ToolsNum == 80 && forest.HasForest, "P must queue clearing and charge 20 tools.");
                    KeyUp(); Next(); break;
                case 10:
                    if (phaseFrame < 4) break;
                    KeyDown(Key.P); Next(); break;
                case 11:
                    if (phaseFrame < 4) break;
                    Require(pioneer.ToolsNum == 80, "Repeated P charged tools twice.");
                    pioneer.BuildSettlement();
                    pioneer.PrepareMoveToHex(HexCalculator.GetHexAround(manager.AllHexes, forest).First(h => h.HexType != HexType.Ocean));
                    Require(!pioneer.IsMoving && pioneer.UnitStatus == UnitStatus.Clearing, "Pending work was overwritten.");
                    KeyUp(); Next(); break;
                case 12:
                    if (phaseFrame < 4) break;
                    SnapshotEnemyPositions(manager);
                    oldCursorLock = Cursor.lockState;
                    oldCursorVisible = Cursor.visible;
                    KeyDown(Key.Backspace); Next(); break;
                case 13:
                    if (phaseFrame < 4) break;
                    Require(!manager.PlayerTurn && AIManager.instance.IsRunning, "Backspace did not start AI turn.");
                    Unit selected = manager.CurUnit;
                    manager.SelectPlayerUnit(manager.PlayerFaction.Units[1]);
                    manager.Endturn();
                    pioneer.BuildSettlement();
                    Require(manager.CurUnit == selected && manager.GameTurn == 1 && pioneer.ToolsNum == 80, "Player action/double Endturn accepted during AI turn.");
                    KeyUp(); Next(); break;
                case 14:
                    if (!manager.PlayerTurn) break;
                    ValidateNewTurn(manager, 2);
                    Require(!forest.HasForest && forest.MoveCost == forestCost - 1 && pioneer.UnitStatus == UnitStatus.None && pioneer.ToolsNum == 80 && pioneer.MovePoint == pioneer.MovePointMax, "Clearing did not finish at turn boundary.");
                    pioneer.ClearingLand();
                    Require(pioneer.ToolsNum == 80 && pioneer.UnitStatus == UnitStatus.None, "Clearing empty land accepted.");
                    manager.SelectPlayerUnit(pioneer);
                    CameraController.instance.MoveCamera(forest.Pos);
                    KeyDown(Key.B); Next(); break;
                case 15:
                    if (phaseFrame < 4) break;
                    Require(pioneer.UnitStatus == UnitStatus.Building && pioneer.ToolsNum == 60 && !forest.HasTown, "B must queue settlement and charge 20 tools.");
                    KeyUp(); Next(); break;
                case 16:
                    if (phaseFrame < 4) break;
                    KeyDown(Key.B); Next(); break;
                case 17:
                    if (phaseFrame < 4) break;
                    Require(pioneer.ToolsNum == 60 && pioneer.UnitStatus == UnitStatus.Building, "Repeated B charged tools twice.");
                    KeyUp(); Next(); break;
                case 18:
                    if (phaseFrame < 4) break;
                    SnapshotEnemyPositions(manager);
                    KeyDown(Key.Backspace); Next(); break;
                case 19:
                    if (phaseFrame < 4) break;
                    Require(!manager.PlayerTurn, "Second AI turn not started.");
                    KeyUp(); Next(); break;
                case 20:
                    if (!manager.PlayerTurn) break;
                    ValidateNewTurn(manager, 3);
                    Require(forest.HasTown && pioneer.UnitStatus == UnitStatus.None && pioneer.ToolsNum == 60 && manager.PlayerFaction.Towns.Count == 1, "Settlement did not finish exactly once.");
                    Town town = manager.PlayerFaction.Towns[0];
                    Require(town.CurHex == forest && town.CurPos == forest.Pos && town.Faction == manager.PlayerFaction && town.transform.parent == manager.PlayerFaction.TownParent, "Player town ownership/position wrong.");
                    CheckTownSprites(town);
                    manager.GenerateTown(manager.PlayerFaction, forest);
                    pioneer.BuildSettlement();
                    Require(manager.PlayerFaction.Towns.Count == 1 && pioneer.ToolsNum == 60, "Duplicate town accepted.");
                    CameraController.instance.MoveCamera(forest.Pos);
                    pioneer.ShowHideSprite(false);
                    Capture("Temp/ColonizationWeek5PlayerTown.png");
                    pioneer.ShowHideSprite(true);
                    KeyDown(Key.I); Next(); break;
                case 21:
                    if (phaseFrame < 4) break;
                    Require(manager.AllHexes.Cast<Hex>().All(h => h.GetComponentInChildren<TMP_Text>(true).gameObject.activeSelf), "I show regressed.");
                    KeyUp(); Next(); break;
                case 22:
                    if (phaseFrame < 4) break;
                    KeyDown(Key.I); Next(); break;
                case 23:
                    if (phaseFrame < 4) break;
                    Require(manager.AllHexes.Cast<Hex>().All(h => !h.GetComponentInChildren<TMP_Text>(true).gameObject.activeSelf), "I hide regressed.");
                    Finish("PASSED\n3000 Hexes, 13 ordered factions, 8 Native nations with 5-9 unique nonadjacent towns each (" + nativeCount + " towns), one configured Native unit per town, all 18 unit initializers and Town sprites/sorting.\nTab/I input, ship and rider movement, EventSystem raycasts, rejected landfall guards, animated disembarkation and cost, P/B repeated-input/tool/terrain guards, delayed clearing/building, two Backspace AI turns, one-step enemy movement, point resets, input blocking, no in-flight handoff, camera focus and cursor restoration.\nScreenshots: Temp/ColonizationWeek5NativeTowns.png and Temp/ColonizationWeek5PlayerTown.png\n");
                    break;
            }
        }
        catch (Exception e) { Finish("FAILED\nPhase " + phase + ": " + e); }
    }

    private static void Next() { phase++; phaseFrame = 0; }
    private static void ValidateStart(GameManager manager)
    {
        ColonizationWeek5Setup.ValidateConfiguration(manager);
        Require(manager.GameTurn == 1 && manager.PlayerTurn && AIManager.instance != null && !AIManager.instance.IsRunning, "Initial turn state invalid.");
        Require(manager.AllHexes.Cast<Hex>().Count(h => h != null) == 3000, "Map incomplete.");
        nativeCount = 0;
        HashSet<Hex> occupied = new HashSet<Hex>();
        for (int i = 0; i < 13; i++)
        {
            Faction faction = manager.Factions[i];
            Require((int)faction.Nation == i, "Faction order/initialization invalid.");
            if (i < 5)
            {
                Require(faction.IsEuropean && faction.Units.Count == 3 && faction.Towns.Count == 0, "European start changed.");
                NavalUnit vessel = (NavalUnit)faction.Units[0];
                Require(vessel.CurHex.X == 49 && vessel.CurHex.HexType == HexType.Ocean && vessel.MovePoint == 4 && vessel.Passengers.Count == 2, "Caravel start incorrect.");
                foreach (LandUnit rider in vessel.Passengers)
                    Require(rider.TransportShip == vessel && rider.UnitStatus == UnitStatus.OnBoard && !rider.gameObject.activeSelf, "Passenger ship link/visibility invalid.");
                CheckPassengerPositions(vessel);
            }
            else
            {
                Require(!faction.IsEuropean && faction.Towns.Count >= 5 && faction.Towns.Count <= 9 && faction.Units.Count == faction.Towns.Count, "Native towns/units missing.");
                foreach (Town town in faction.Towns)
                {
                    Require(occupied.Add(town.CurHex) && town.CurHex.HasTown && town.CurHex.HexType != HexType.Ocean && town.CurHex.HexType != HexType.Mountains && !HexCalculator.CheckIfHexAroundHasTown(manager.AllHexes, town.CurHex), "Native town overlap/terrain/spacing invalid.");
                    Require(town.Faction == faction && town.CurPos == town.CurHex.Pos, "Native town initialization invalid.");
                    CheckTownSprites(town);
                }
                foreach (Unit unit in faction.Units)
                    Require(((LandUnit)unit).LandUnitType == (faction.IsTropicalNatives ? LandUnitType.TropicalIndians : LandUnitType.PlainIndians) && faction.Towns.Any(t => t.CurHex == unit.CurHex), "Native unit variant/starting town invalid.");
                nativeCount += faction.Towns.Count;
            }
        }
        ship = (NavalUnit)manager.PlayerFaction.Units[0];
        pioneer = (LandUnit)manager.PlayerFaction.Units[2];
        origin = ship.CurHex;
        Require(manager.CurUnit == ship && pioneer.ToolsNum == 100, "Player explorer start incorrect.");
        Hex untouched = manager.AllHexes[0, 0];
        Require(!Overlay(untouched, "fogSprite") && !Overlay(untouched, "darkSprite"), "Week 5 Native preview should hide default fog.");
        foreach (Hex hex in manager.AllHexes)
            foreach (Hex adjacent in HexCalculator.GetHexAround(manager.AllHexes, hex))
                Require(HexCalculator.GetHexAround(manager.AllHexes, adjacent).Contains(hex), "Hex neighbor reciprocity regressed.");
    }

    private static void CheckTownSprites(Town town)
    {
        SpriteRenderer body = town.transform.Find("TownSprite").GetComponent<SpriteRenderer>();
        SpriteRenderer flag = town.transform.Find("FlagSprite").GetComponent<SpriteRenderer>();
        Require(body.sprite == town.Faction.TownIcon && flag.sprite == town.Faction.FlagIcon && body.sortingLayerName == "Town" && body.sortingOrder == 1, "Town sprite/flag/layer invalid.");
        Require(body.sharedMaterial.shader.name == "Universal Render Pipeline/2D/Sprite-Unlit-Default" && flag.sharedMaterial.shader == body.sharedMaterial.shader, "Town must use current URP sprite material.");
    }

    private static void CheckSelected(Unit unit)
    {
        Require(GameManager.instance.CurUnit == unit && unit.gameObject.activeSelf && unit.transform.Find("BorderSprite").gameObject.activeSelf, "Tab/selection regressed.");
        Require((Vector2)Camera.main.transform.position == (Vector2)unit.transform.position && Camera.main.transform.position.z == -10, "Camera focus regressed.");
    }
    private static void CheckPassengerPositions(NavalUnit vessel)
    {
        foreach (LandUnit rider in vessel.Passengers)
            Require(rider.CurHex == vessel.CurHex && rider.CurPos == vessel.CurHex.Pos && (Vector2)rider.transform.position == vessel.CurHex.Pos, "Passenger synchronization regressed.");
    }
    private static void BeginOceanMove(GameManager manager)
    {
        destination = HexCalculator.FindHexByDir(ship.CurHex, HexDirection.Left, manager.AllHexes);
        CameraController.instance.MoveCamera(ship.CurPos);
        ClickAt(destination.Pos, PointerEventData.InputButton.Right, typeof(Hex));
        Require(ship.IsMoving, "Right-click ship movement regressed.");
        manager.Endturn();
        Require(manager.PlayerTurn && manager.GameTurn == 1, "Endturn accepted in-flight ship.");
        oceanMoves++;
    }

    private static void SetupCoastFixture(GameManager manager)
    {
        forest = manager.AllHexes.Cast<Hex>().First(h => h.HasForest && !h.HasTown && h.HexType != HexType.Hills && h.HexType != HexType.Mountains && HexCalculator.GetHexAround(manager.AllHexes, h).Any(n => n.HexType == HexType.Ocean && HexCalculator.GetHexAround(manager.AllHexes, n).Any(sea => sea.HexType == HexType.Ocean)));
        coastSea = HexCalculator.GetHexAround(manager.AllHexes, forest).First(h => h.HexType == HexType.Ocean && HexCalculator.GetHexAround(manager.AllHexes, h).Any(sea => sea.HexType == HexType.Ocean));
        PositionShipFixture(coastSea);
        forestCost = forest.MoveCost;
        manager.SelectPlayerUnit(pioneer);
        CameraController.instance.MoveCamera(coastSea.Pos);
    }
    private static void PositionShipFixture(Hex sea)
    {
        ship.SetupPosition(sea);
        foreach (LandUnit rider in ship.Passengers)
        {
            rider.CurHex = sea;
            rider.CurPos = sea.Pos;
            rider.transform.localPosition = Vector3.zero;
        }
    }
    private static void ValidateDisembarkGuards(GameManager manager)
    {
        pioneer.PrepareMoveToHex(HexCalculator.GetHexAround(manager.AllHexes, coastSea).First(h => h.HexType == HexType.Ocean));
        Require(pioneer.UnitStatus == UnitStatus.OnBoard, "Passenger landed in water.");
        pioneer.MovePoint = 0; pioneer.PrepareMoveToHex(forest);
        Require(pioneer.TransportShip == ship && ship.Passengers.Contains(pioneer) && pioneer.UnitStatus == UnitStatus.OnBoard && !pioneer.IsMoving, "Rejected zero-point landfall detached passenger.");
        pioneer.MovePoint = pioneer.MovePointMax;
        Hex far = manager.AllHexes.Cast<Hex>().First(h => h.HexType != HexType.Ocean && !manager.CheckIfHexIsAdjacent(coastSea, h) && h != coastSea);
        pioneer.PrepareMoveToHex(far);
        Require(pioneer.TransportShip == ship && pioneer.transform.parent == ship.PassengerParent.transform, "Non-adjacent landfall detached passenger.");
    }

    private static void SnapshotEnemyPositions(GameManager manager)
    {
        enemyPositions = manager.Factions.Where(f => f != manager.PlayerFaction).SelectMany(f => f.Units).ToDictionary(u => u, u => u.CurHex);
    }
    private static void ValidateNewTurn(GameManager manager, int turn)
    {
        Require(manager.GameTurn == turn && !AIManager.instance.IsRunning && manager.CurAiUnit == null && manager.Factions.SelectMany(f => f.Units).All(u => !u.IsMoving), "Turn handoff occurred before AI arrival or incremented incorrectly.");
        Require(manager.CurUnit == ship && (Vector2)Camera.main.transform.position == ship.CurPos, "New turn did not focus first player unit.");
        Require(Cursor.visible == oldCursorVisible && Cursor.lockState == oldCursorLock, "Cursor state not restored.");
        int movedNatives = 0;
        foreach (var pair in enemyPositions)
        {
            Unit unit = pair.Key;
            if (unit.UnitStatus == UnitStatus.OnBoard) continue;
            bool moved = unit.CurHex != pair.Value;
            Require(!moved || manager.CheckIfHexIsAdjacent(pair.Value, unit.CurHex), "AI moved more than one Hex.");
            Require(unit.MovePoint == unit.MovePointMax - (moved ? unit.CurHex.MoveCost : 0), "AI points did not reset/deduct correctly.");
            if (moved && !unit.Faction.IsEuropean) movedNatives++;
            if (unit is NavalUnit vessel) CheckPassengerPositions(vessel);
        }
        Require(movedNatives > 0, "No Native unit moved during AI turn.");
    }

    private static bool Overlay(Hex hex, string field) { return ((SpriteRenderer)new SerializedObject(hex).FindProperty(field).objectReferenceValue).gameObject.activeSelf; }
    private static void ClickAt(Vector3 position, PointerEventData.InputButton button, Type expected)
    {
        Physics2D.SyncTransforms();
        PointerEventData data = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(position), button = button };
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Require(hits.Count > 0, "EventSystem raycast missed.");
        GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Require(handler != null && handler.GetComponent(expected) != null, "Unexpected top raycast: " + hits[0].gameObject.name);
        ExecuteEvents.Execute(handler, data, ExecuteEvents.pointerClickHandler);
    }
    private static void Dispatch(GameObject obj, PointerEventData.InputButton button) { ExecuteEvents.Execute(obj, new PointerEventData(EventSystem.current) { button = button }, ExecuteEvents.pointerClickHandler); }
    private static void KeyDown(Key key) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); }
    private static void KeyUp() { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); }
    private static void Capture(string path)
    {
        Camera camera = Camera.main;
        RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
        RenderTexture target = new RenderTexture(1200, 800, 24);
        Texture2D picture = new Texture2D(1200, 800, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            picture.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0);
            picture.Apply();
            File.WriteAllBytes(path, picture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            target.Release();
            Object.DestroyImmediate(target); Object.DestroyImmediate(picture);
        }
    }
    private static void Finish(string result)
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.editorInputBehaviorInPlayMode = oldInput;
        InputSystem.settings.backgroundBehavior = oldBackground;
        Application.runInBackground = oldRunBackground;
        SessionState.SetBool(Pending, false);
        File.WriteAllText(Result, result);
        EditorApplication.isPlaying = false;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
