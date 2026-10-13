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

// Runs against the saved scene, including real Input System keyboard events and
// the EventSystem raycast/click pipeline. No runtime changes are saved.
public static class ColonizationWeek4SmokeTest
{
    private const string Request = "Temp/ColonizationWeek4SmokeTest.v2.request";
    private const string Result = "Temp/ColonizationWeek4SmokeTest.result.txt";
    private const string Pending = "Colonization.Week4SmokeTest.Pending";
    private static Keyboard keyboard;
    private static InputSettings.EditorInputBehaviorInPlayMode oldInput;
    private static InputSettings.BackgroundBehavior oldBackground;
    private static bool oldRunBackground;
    private static int frame, lastFrame, steps, initialPoints;
    private static double started;
    private static string error;
    private static NavalUnit ship;
    private static Hex origin, destination;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (SessionState.GetBool(Pending, false) && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) error = message + "\n" + stack;
        };
    }

    [MenuItem("Tools/Colonization/Run Week 4 Play Mode Smoke Test")]
    public static void Run()
    {
        GameManager current = Object.FindAnyObjectByType<GameManager>();
        if (current != null && current.Factions.Length == 13)
        {
            ColonizationWeek5SmokeTest.Run();
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Open saved Map01 alone in Edit Mode before the Week 4 smoke test.");
        ColonizationWeek4Setup.ValidateConfiguration(Object.FindAnyObjectByType<GameManager>());
        SessionState.SetBool(Pending, true);
        File.WriteAllText(Result, "RUNNING\n");
        EditorApplication.isPlaying = true;
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            frame = steps = 0;
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
        if (File.Exists(Request) && !File.Exists(ColonizationWeek4Setup.RequestPath) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            File.Delete(Request);
            try { Run(); } catch (Exception e) { File.WriteAllText(Result, "FAILED\n" + e); }
        }
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (EditorApplication.timeSinceStartup - started < 2) return;
        try
        {
            if (error != null) throw new InvalidOperationException(error);
            if (EditorApplication.timeSinceStartup - started > 90) throw new TimeoutException("Week 4 test timed out.");
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            frame++;
            GameManager manager = GameManager.instance;
            if (frame == 1)
            {
                ValidateStart(manager);
                ValidateNeighbors(manager);
                ValidateAllUnitInitializers(manager);
                Capture("Temp/ColonizationWeek4Start.png");
                keyboard = InputSystem.AddDevice<Keyboard>();
                KeyDown(Key.Tab);
            }
            if (frame == 5) { CheckSelected(manager.PlayerFaction.Units[1]); KeyUp(); }
            if (frame == 9) KeyDown(Key.Tab);
            if (frame == 13) { CheckSelected(manager.PlayerFaction.Units[2]); Require(!manager.PlayerFaction.Units[1].gameObject.activeSelf, "Previous passenger remained shown."); KeyUp(); }
            if (frame == 17) KeyDown(Key.Tab);
            if (frame == 21)
            {
                CheckSelected(ship);
                Require(ship.Passengers.All(p => !p.gameObject.activeSelf), "Tab did not hide passengers.");
                KeyUp();
                // Enemy clicks cannot take control. A player ship is selectable via physics raycast.
                Dispatch(manager.Factions[1].Units[0].gameObject, PointerEventData.InputButton.Left);
                Require(manager.CurUnit == ship, "Enemy unit selected.");
                ClickAt(ship.transform.position, PointerEventData.InputButton.Left, typeof(Unit));
                Hex land = manager.AllHexes.Cast<Hex>().First(h => h.HexType != HexType.Ocean);
                ship.PrepareMoveToHex(land);
                Require(!ship.IsMoving && ship.TargetHex == null && ship.MovePoint == initialPoints, "Land rejection mutated movement state.");
                ship.PrepareMoveToHex(manager.AllHexes[49, origin.Y < 30 ? 59 : 0]);
                Require(!ship.IsMoving, "Non-adjacent move accepted.");
                ship.PrepareMoveToHex(null);
                Require(!ship.IsMoving && ship.TargetHex == null, "Null target accepted.");
                ship.Passengers[0].PrepareMoveToHex(HexCalculator.FindHexByDir(ship.CurHex, HexDirection.Left, manager.AllHexes));
                Require(!ship.Passengers[0].IsMoving, "Passenger moved while OnBoard.");
                BeginNextMove(manager);
            }
            if (frame > 21 && !ship.IsMoving && steps > 0)
            {
                Require(ship.CurHex == destination && ship.TargetHex == null && ship.CurPos == destination.Pos && (Vector2)ship.transform.position == destination.Pos, "Arrival position/target reset failed.");
                Require(ship.MovePoint == initialPoints - steps, "Move cost deducted incorrectly.");
                foreach (LandUnit passenger in ship.Passengers)
                    Require(passenger.CurHex == destination && passenger.CurPos == destination.Pos && (Vector2)passenger.transform.position == destination.Pos, "Passenger position did not follow ship.");
                Require(destination.Visible && HexCalculator.GetHexAround(manager.AllHexes, destination).All(h => h.Visible), "Arrival did not reveal neighbors.");
                if (steps < 3) BeginNextMove(manager);
                else
                {
                    Require(!origin.Visible && !Overlay(origin, "darkSprite") && Overlay(origin, "fogSprite"), "Explored tile did not retain seen fog.");
                    ship.MovePoint = 0;
                    ship.PrepareMoveToHex(HexCalculator.FindHexByDir(ship.CurHex, HexDirection.Right, manager.AllHexes));
                    Require(!ship.IsMoving && ship.TargetHex == null, "Zero-point move accepted.");
                    Capture("Temp/ColonizationWeek4Moved.png");
                    KeyDown(Key.I);
                    steps = -1;
                    frame = 1000;
                }
            }
            if (frame == 1004)
            {
                Require(manager.AllHexes.Cast<Hex>().All(h => h.GetComponentInChildren<TMP_Text>(true).gameObject.activeSelf), "I-key show failed after Week 4.");
                KeyUp();
            }
            if (frame == 1008) KeyDown(Key.I);
            if (frame == 1012)
            {
                Require(manager.AllHexes.Cast<Hex>().All(h => !h.GetComponentInChildren<TMP_Text>(true).gameObject.activeSelf), "I-key hide failed after Week 4.");
                Finish("PASSED\n3000 Hexes; even/odd neighbor reciprocity and boundaries; 12 Land/6 Naval initializers; 5 ships and 10 passengers.\nTab cycle and camera focus; player/enemy clicks; Physics2D raycast selection/right-click movement; three animated moves; costs; terrain/distance/OnBoard/zero-point guards; passenger synchronization; unexplored/visible/seen fog; I toggle.\nScreenshots: Temp/ColonizationWeek4Start.png and Temp/ColonizationWeek4Moved.png\n");
            }
        }
        catch (Exception e) { Finish("FAILED\n" + e); }
    }

    private static void ValidateStart(GameManager manager)
    {
        ColonizationWeek4Setup.ValidateConfiguration(manager);
        Require(manager.PlayerFaction == manager.Factions[0], "Player should be England.");
        foreach (Faction faction in manager.Factions)
        {
            Require(faction.Units.Count == 3, "Each faction requires one ship and two passengers.");
            NavalUnit vessel = faction.Units[0] as NavalUnit;
            Require(vessel != null && vessel.NavalUnitType == NavalUnitType.Caravel && vessel.CurHex.X == 49 && vessel.CurHex.HexType == HexType.Ocean, "Caravel spawn incorrect.");
            Require(vessel.MovePoint == 4 && vessel.Strength == 2 && vessel.CargoHoldNum == 2 && vessel.Passengers.Count == 2, "Caravel stats/passengers incorrect.");
            SpriteRenderer flag = vessel.transform.Find("FlagSprite").GetComponent<SpriteRenderer>();
            SpriteRenderer body = vessel.transform.Find("UnitSprite").GetComponent<SpriteRenderer>();
            Require(flag.sprite == faction.ShieldIcon && body.sprite == manager.NavalUnitData[0].shipIcon && flag.sprite.texture != body.sprite.texture, "Ship and shield sprite references must be distinct.");
            Require(flag.sharedMaterial.shader.name == "Universal Render Pipeline/2D/Sprite-Unlit-Default", "Shield must use the installed URP sprite shader.");
            Require(vessel.Passengers[0].LandUnitType == LandUnitType.VeteranSoldiers && vessel.Passengers[0].MusketsNum == 50 && vessel.Passengers[1].ToolsNum == 100, "Explorer equipment incorrect.");
            foreach (LandUnit passenger in vessel.Passengers)
                Require(passenger.UnitStatus == UnitStatus.OnBoard && !passenger.gameObject.activeSelf && passenger.CurHex == vessel.CurHex && passenger.transform.parent == vessel.PassengerParent.transform, "Passenger setup incorrect.");
        }
        ship = (NavalUnit)manager.PlayerFaction.Units[0];
        origin = ship.CurHex;
        initialPoints = ship.MovePoint;
        Require(manager.CurUnit == ship, "Player ship not initially selected.");
        List<Hex> visible = HexCalculator.GetHexAround(manager.AllHexes, origin);
        visible.Add(origin);
        foreach (Hex hex in manager.AllHexes)
        {
            Require(hex.Visible == visible.Contains(hex), "Unexpected initial visibility.");
            Require(Overlay(hex, "fogSprite") == !hex.Visible && Overlay(hex, "darkSprite") == !hex.Visible, "Initial fog/dark incorrect.");
        }
    }

    private static void ValidateNeighbors(GameManager manager)
    {
        foreach (Hex center in manager.AllHexes)
        foreach (Hex other in HexCalculator.GetHexAround(manager.AllHexes, center))
        {
            Require(HexCalculator.GetHexAround(manager.AllHexes, other).Contains(center), "Neighbor relationship not reciprocal.");
            Require(Vector2.Distance(center.Pos, other.Pos) > 0, "Neighbor equals center.");
        }
        Hex even = manager.AllHexes[10, 10];
        Hex odd = manager.AllHexes[10, 11];
        Require(HexCalculator.FindHexByDir(even, HexDirection.TopLeft, manager.AllHexes) == manager.AllHexes[9, 11], "Even TopLeft incorrect.");
        Require(HexCalculator.FindHexByDir(odd, HexDirection.TopRight, manager.AllHexes) == manager.AllHexes[11, 12], "Odd TopRight incorrect.");
        Require(HexCalculator.FindHexByDir(manager.AllHexes[0, 0], HexDirection.Left, manager.AllHexes) == null && HexCalculator.FindHexByDir(manager.AllHexes[49, 59], HexDirection.Right, manager.AllHexes) == null, "Boundary not clipped.");
    }

    internal static void ValidateAllUnitInitializers(GameManager manager)
    {
        GameObject land = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/LandUnit.prefab"));
        GameObject naval = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/NavalUnit.prefab"));
        try
        {
            LandUnit person = land.GetComponent<LandUnit>();
            foreach (LandUnitData data in manager.LandUnitData)
            {
                person.UnitInit(manager, manager.PlayerFaction, data);
                Require(person.LandUnitType == data.landUnitType && person.MovePoint == data.movePointMax && person.Strength == data.strength && person.VisualRange == data.visualRange && person.HasHorse == data.hasHorse && person.MusketsNum == (data.hasMusket ? 50 : 0) && person.ToolsNum == (data.landUnitType == LandUnitType.HardyPioneers ? 100 : 0), "Land initialization/equipment reset failed.");
            }
            NavalUnit vessel = naval.GetComponent<NavalUnit>();
            foreach (NavalUnitData data in manager.NavalUnitData)
            {
                vessel.UnitInit(manager, manager.PlayerFaction, data);
                Require(vessel.NavalUnitType == data.navalUnitType && vessel.Strength == data.strength && vessel.MovePoint == data.movePointMax && vessel.CargoHoldNum == data.cargoHoldNum && vessel.Armed == data.armed, "Naval initialization failed.");
            }
        }
        finally { Object.DestroyImmediate(land); Object.DestroyImmediate(naval); }
    }

    private static void CheckSelected(Unit unit)
    {
        Require(GameManager.instance.CurUnit == unit && unit.gameObject.activeSelf, "Tab cycle incorrect.");
        Require((Vector2)Camera.main.transform.position == (Vector2)unit.transform.position && Camera.main.transform.position.z == -10f, "Camera focus incorrect.");
        Require(unit.transform.Find("BorderSprite").gameObject.activeSelf && unit.transform.Find("UnitSprite").GetComponent<SpriteRenderer>().sortingOrder == 5, "Selected highlight/layer incorrect.");
    }
    private static void BeginNextMove(GameManager manager)
    {
        destination = HexCalculator.FindHexByDir(ship.CurHex, HexDirection.Left, manager.AllHexes);
        Require(destination != null && destination.HexType == HexType.Ocean, "Expected guaranteed eastern ocean.");
        CameraController.instance.MoveCamera(ship.transform.position);
        ClickAt(destination.transform.position, PointerEventData.InputButton.Right, typeof(Hex));
        Require(ship.IsMoving && ship.TargetHex == destination, "Right-click did not start movement.");
        // Repeated clicks during movement must not redirect/deduct twice.
        ship.PrepareMoveToHex(origin);
        Require(ship.TargetHex == destination, "Move redirected in transit.");
        steps++;
    }
    private static bool Overlay(Hex hex, string field)
    {
        return ((SpriteRenderer)new SerializedObject(hex).FindProperty(field).objectReferenceValue).gameObject.activeSelf;
    }
    private static void ClickAt(Vector3 position, PointerEventData.InputButton button, Type expected)
    {
        Physics2D.SyncTransforms();
        PointerEventData data = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(position), button = button };
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Require(hits.Count > 0, "No EventSystem raycast hit.");
        GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Require(handler != null && handler.GetComponent(expected) != null, "Top raycast did not resolve " + expected.Name + ": " + hits[0].gameObject.name);
        ExecuteEvents.Execute(handler, data, ExecuteEvents.pointerClickHandler);
    }
    private static void Dispatch(GameObject obj, PointerEventData.InputButton button)
    {
        ExecuteEvents.Execute(obj, new PointerEventData(EventSystem.current) { button = button }, ExecuteEvents.pointerClickHandler);
    }
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
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(picture);
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
