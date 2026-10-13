using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Exercises the authored Map01 scene. Play Mode changes are never saved.
public static class ColonizationWeek3SmokeTest
{
    private const string ResultPath = "Temp/ColonizationWeek3SmokeTest.result.txt";
    private const string RequestPath = "Temp/ColonizationWeek3SmokeTest.v2.request";
    private const string PendingKey = "Colonization.Week3SmokeTest.Pending";
    private static int frame;
    private static double started;
    private static string runtimeError;
    private static Keyboard keyboard;
    private static int lastGameFrame;
    private static InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    private static InputSettings.BackgroundBehavior previousBackground;
    private static bool previousRunInBackground;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += ObserveLog;
    }

    [MenuItem("Tools/Colonization/Run Week 3 Play Mode Smoke Test")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before starting the smoke test.");
        Scene active = SceneManager.GetActiveScene();
        if (active.path != "Assets/Scenes/Map01.unity" || SceneManager.sceneCount != 1 || active.isDirty)
            throw new InvalidOperationException("Open the saved Map01 scene by itself before running the smoke test.");
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        ColonizationWeek3Setup.ValidateConfiguration(manager);
        SessionState.SetBool(PendingKey, true);
        File.WriteAllText(ResultPath, "RUNNING\n");
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            frame = 0;
            started = EditorApplication.timeSinceStartup;
            runtimeError = null;
            lastGameFrame = -1;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(PendingKey, false))
        {
            SessionState.SetBool(PendingKey, false);
            File.WriteAllText(ResultPath, "FAILED\nPlay Mode exited before the smoke test completed.\n");
        }
    }

    private static void ObserveLog(string message, string stack, LogType type)
    {
        if (SessionState.GetBool(PendingKey, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            runtimeError = message + "\n" + stack;
    }

    private static void Tick()
    {
        if (File.Exists(RequestPath) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            File.Delete(RequestPath);
            try { Run(); }
            catch (Exception e) { File.WriteAllText(ResultPath, "FAILED\n" + e); }
        }
        if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (EditorApplication.timeSinceStartup - started < 2) return;
        // Editor ticks can outnumber actual gameplay frames, especially when the
        // Game view is unfocused. Give queued input time to reach GameManager.Update.
        if (Time.frameCount == lastGameFrame) return;
        lastGameFrame = Time.frameCount;
        try
        {
            if (runtimeError != null) throw new InvalidOperationException(runtimeError);
            frame++;
            if (frame == 1)
            {
                ValidateMap();
                ValidateRepeatedInitialization();
                keyboard = InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I));
            }
            if (frame == 5)
            {
                AssertCoordinateVisibility(true);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            }
            if (frame == 9) InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I));
            if (frame == 13)
            {
                AssertCoordinateVisibility(false);
                CaptureMap();
                Finish("PASSED\nGenerated 3000 Hexes; checked biome bands, coordinates, sprites, sorting, move costs and five initialized factions.\nRepeated Hex initialization and ClearForest passed. I-key show/hide passed.\nScreenshot: Temp/ColonizationWeek3Map.png\n");
            }
        }
        catch (Exception e) { Finish("FAILED\n" + e); }
    }

    private static void ValidateMap()
    {
        GameManager manager = GameManager.instance;
        Require(manager != null, "GameManager did not initialize.");
        ColonizationWeek3Setup.ValidateConfiguration(manager);
        int oceanCount = 0;
        int landCount = 0;
        int hillsCount = 0;
        int mountainsCount = 0;
        for (int x = 0; x < GameManager.WIDTH; x++)
        for (int y = 0; y < GameManager.HEIGHT; y++)
        {
            Hex hex = manager.AllHexes[x, y];
            Require(hex != null && hex.X == x && hex.Y == y, "Missing/incorrect Hex at " + x + "," + y);
            int type = (int)hex.HexType;
            SerializedObject serialized = new SerializedObject(hex);
            SpriteRenderer terrain = (SpriteRenderer)serialized.FindProperty("terrainSprite").objectReferenceValue;
            SpriteRenderer forest = (SpriteRenderer)serialized.FindProperty("forestSprite").objectReferenceValue;
            SpriteRenderer fog = (SpriteRenderer)serialized.FindProperty("fogSprite").objectReferenceValue;
            SpriteRenderer dark = (SpriteRenderer)serialized.FindProperty("darkSprite").objectReferenceValue;
            TMP_Text text = (TMP_Text)serialized.FindProperty("hexText").objectReferenceValue;
            Require(terrain.sprite != null && text.text == x + "," + y, "Terrain/text not initialized.");
            Require(terrain.sortingOrder == -10 * y + (type >= 8 ? 1 : 0), "Terrain sorting incorrect.");
            Require(forest.sortingOrder == -10 * y + 1 && fog.sortingOrder == -10 * y + 5 && dark.sortingOrder == -10 * y + 6, "Overlay sorting incorrect.");
            Require(forest.gameObject.activeSelf == hex.HasForest, "Forest visibility/state disagree.");
            Require(hex.MoveCost == manager.HexData[type].moveCost + (hex.HasForest ? 1 : 0), "Move cost inconsistent.");
            if (manager.LandUnitData == null || manager.LandUnitData.Length == 0)
                Require(!fog.gameObject.activeSelf && !dark.gameObject.activeSelf, "Fog should be hidden before Week 4.");
            if (type == 0)
            {
                oceanCount++;
                Require(!hex.HasForest && x >= 38, "Unexpected ocean/forest location.");
            }
            else
            {
                landCount++;
                Require(x < 46, "Land reached guaranteed ocean columns.");
                if (type == 9) hillsCount++;
                else if (type == 10) mountainsCount++;
                else Require(AllowedTerrain(y).Contains(type), "Terrain outside its biome at row " + y);
            }
        }
        Require(oceanCount > 0 && landCount > 0 && hillsCount > 0 && mountainsCount > 0, "Expected terrain categories absent.");
        for (int i = 0; i < 5; i++)
        {
            Faction faction = manager.Factions[i];
            Require((int)faction.Nation == i && faction.IsEuropean && faction.FlagIcon == manager.FactionData[i].flagIcon && faction.ShieldIcon == manager.FactionData[i].shieldIcon && faction.TownIcon != null, "Faction initialization incorrect.");
        }
    }

    private static int[] AllowedTerrain(int y)
    {
        if (y < 2 || y >= 58) return new[] { 8, 5 };
        if (y < 6 || y >= 54) return new[] { 5, 1 };
        if (y < 12 || y >= 48) return new[] { 1, 2, 5 };
        if (y < 20 || y >= 40) return new[] { 2, 1, 3, 4 };
        if (y < 24 || y >= 36) return new[] { 3, 1, 2, 4, 6 };
        return new[] { 4, 1, 7 };
    }

    private static void ValidateRepeatedInitialization()
    {
        GameManager manager = GameManager.instance;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hex.prefab");
        GameObject sample = Object.Instantiate(prefab);
        try
        {
            Hex hex = sample.GetComponent<Hex>();
            for (int n = 0; n < 20 && !hex.HasForest; n++) hex.HexInit(0, 10, Vector2.zero, manager, 1);
            Require(hex.HasForest, "Could not initialize forest test sample.");
            hex.ClearForest();
            Require(!hex.HasForest && hex.MoveCost == 1, "ClearForest did not restore base move cost.");
            hex.ClearForest();
            Require(hex.MoveCost == 1, "Repeated ClearForest changed move cost.");
            hex.HexInit(0, 10, Vector2.zero, manager, 0);
            Require(!hex.HasForest && hex.MoveCost == 1, "Ocean retained old forest state.");
            hex.HexInit(0, 10, Vector2.zero, manager, 10);
            Require(!hex.HasForest && hex.MoveCost == 3, "Mountain retained old forest state.");
        }
        finally { Object.DestroyImmediate(sample); }
    }

    private static void AssertCoordinateVisibility(bool expected)
    {
        foreach (Hex hex in GameManager.instance.AllHexes)
            Require(hex.GetComponentInChildren<TMP_Text>(true).gameObject.activeSelf == expected, "I-key coordinate toggle failed.");
    }

    private static void CaptureMap()
    {
        Camera camera = Camera.main;
        Vector3 position = camera.transform.position;
        float size = camera.orthographicSize;
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        RenderTexture target = new RenderTexture(1200, 1200, 24);
        Texture2D image = new Texture2D(1200, 1200, TextureFormat.RGB24, false);
        try
        {
            Vector3 a = GameManager.instance.AllHexes[0, 0].transform.position;
            Vector3 b = GameManager.instance.AllHexes[49, 59].transform.position;
            camera.transform.position = new Vector3((a.x + b.x) / 2, (a.y + b.y) / 2, position.z);
            camera.orthographicSize = Mathf.Max(b.y - a.y, b.x - a.x) * 0.55f;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1200, 1200), 0, 0);
            image.Apply();
            File.WriteAllBytes("Temp/ColonizationWeek3Map.png", image.EncodeToPNG());
        }
        finally
        {
            camera.transform.position = position;
            camera.orthographicSize = size;
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
        }
    }

    private static void Finish(string result)
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        InputSystem.settings.backgroundBehavior = previousBackground;
        Application.runInBackground = previousRunInBackground;
        SessionState.SetBool(PendingKey, false);
        File.WriteAllText(ResultPath, result);
        EditorApplication.isPlaying = false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
