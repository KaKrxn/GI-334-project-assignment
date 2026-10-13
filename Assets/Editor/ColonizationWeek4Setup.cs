using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ColonizationWeek4Setup
{
    public const string RequestPath = "Temp/ColonizationWeek4Setup.v2.request";
    public const string ResultPath = "Temp/ColonizationWeek4Setup.result.txt";
    private const string MapPath = "Assets/Scenes/Map01.unity";
    private const string HexPath = "Assets/Prefabs/Hex.prefab";
    private const string UnitsPath = "Assets/Prefabs/Units/";
    private static readonly string[] LandIcons = { "FreeColonists", "VeteranSoldiers", "HardyPioneers", "PlainIndian", "Tropical_Indians", "FurTrapper", "Farmers", "FreeColonists", "LumberJacks", "MasterCarpenters", "OreMiners", "Blacksmith" };
    private static readonly string[] ShipIcons = { "ship_caravel", "Merchantman", "ship_carrack", "ship_sloop", "Frigate", "ship_man-o-war" };

    [InitializeOnLoadMethod]
    private static void Initialize() { EditorApplication.update += Tick; }

    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        try { Apply(); }
        catch (Exception e) { File.WriteAllText(ResultPath, "FAILED\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Tools/Colonization/Apply Week 4 From Existing Assets")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before Week 4 setup.");
        Scene map = SceneManager.GetSceneByPath(MapPath);
        bool opened = !map.IsValid() || !map.isLoaded;
        if (!opened && map.isDirty) throw new InvalidOperationException("Save Map01 before applying Week 4.");
        string[] iconPaths = LandIcons.Select(n => "Assets/Textures/LandUnits/" + n + ".png")
            .Concat(ShipIcons.Select(n => "Assets/Textures/NavalUnits/" + n + ".png")).Distinct().ToArray();
        foreach (string path in iconPaths)
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) throw new InvalidOperationException("Missing existing artwork: " + path);
        foreach (string path in iconPaths)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int requiredSize = Mathf.NextPowerOfTwo(Mathf.Max(width, height));
            if (importer.spriteImportMode == SpriteImportMode.Single && importer.textureType == TextureImporterType.Sprite && importer.maxTextureSize >= requiredSize) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, requiredSize);
            importer.SaveAndReimport();
        }
        EnsureUnitSortingLayer();
        EnsureFolder("Assets/Scripts/LandUnitsSO");
        EnsureFolder("Assets/Scripts/NavalUnitsSO");
        EnsureFolder("Assets/Prefabs/Units");
        LandUnitData[] landData = new LandUnitData[12];
        for (int i = 0; i < 12; i++)
        {
            string name = ((LandUnitType)i).ToString();
            LandUnitData data = LoadOrCreate<LandUnitData>("Assets/Scripts/LandUnitsSO/" + i.ToString("000") + "_" + name + ".asset");
            data.unitName = ObjectNames.NicifyVariableName(name);
            data.landUnitType = (LandUnitType)i;
            data.strength = i == 1 ? 2 : 1;
            data.movePointMax = i == 5 ? 6 : 3;
            data.visualRange = i == 5 ? 2 : 1;
            data.unitIcon = SpriteAt("Assets/Textures/LandUnits/" + LandIcons[i] + ".png");
            data.armed = i == 1 || i == 3 || i == 4;
            data.hasMusket = i == 1;
            data.hasHorse = i == 5;
            EditorUtility.SetDirty(data);
            landData[i] = data;
        }
        // DOCX block 826: Moves / Strength / Cargo for all six ships.
        int[] moves = { 4, 5, 6, 8, 6, 6 };
        int[] strength = { 2, 6, 10, 8, 16, 24 };
        int[] cargo = { 2, 4, 6, 2, 4, 6 };
        NavalUnitData[] navalData = new NavalUnitData[6];
        for (int i = 0; i < 6; i++)
        {
            string name = ((NavalUnitType)i).ToString();
            NavalUnitData data = LoadOrCreate<NavalUnitData>("Assets/Scripts/NavalUnitsSO/" + i.ToString("000") + "_" + name + ".asset");
            data.unitName = i == 5 ? "Man-O-War" : name;
            data.navalUnitType = (NavalUnitType)i;
            data.strength = strength[i];
            data.movePointMax = moves[i];
            data.visualRange = 1;
            data.shipIcon = SpriteAt("Assets/Textures/NavalUnits/" + ShipIcons[i] + ".png");
            data.armed = i >= 3;
            data.cargoHoldNum = cargo[i];
            EditorUtility.SetDirty(data);
            navalData[i] = data;
        }
        Sprite shield = SpriteAt("Assets/Textures/Emblems_of_civ/Shields/England_emb.png");
        GameObject landPrefab = CreateUnitPrefab(false, landData[0].unitIcon, shield);
        GameObject navalPrefab = CreateUnitPrefab(true, navalData[0].shipIcon, shield);
        GameObject hex = PrefabUtility.LoadPrefabContents(HexPath);
        try
        {
            hex.transform.Find("HexFog").gameObject.SetActive(true);
            hex.transform.Find("HexDark").gameObject.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(hex, HexPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(hex); }
        if (opened) map = EditorSceneManager.OpenScene(MapPath, OpenSceneMode.Additive);
        try
        {
            GameManager manager = map.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).Single();
            ColonizationWeek3Setup.ValidateConfiguration(manager);
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("landUnitPrefab").objectReferenceValue = landPrefab;
            so.FindProperty("navalUnitPrefab").objectReferenceValue = navalPrefab;
            SetArray(so.FindProperty("landUnitData"), landData);
            SetArray(so.FindProperty("navalUnitData"), navalData);
            so.ApplyModifiedPropertiesWithoutUndo();
            Camera camera = map.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera"));
            if (camera.GetComponent<Physics2DRaycaster>() == null) camera.gameObject.AddComponent<Physics2DRaycaster>();
            foreach (Light2D light in map.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light2D>(true)))
            {
                SerializedObject lightSO = new SerializedObject(light);
                SerializedProperty layers = lightSO.FindProperty("m_ApplyToSortingLayers");
                if (layers == null) continue;
                layers.arraySize = SortingLayer.layers.Length;
                for (int i = 0; i < layers.arraySize; i++) layers.GetArrayElementAtIndex(i).intValue = SortingLayer.layers[i].id;
                lightSO.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(map);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(map)) throw new IOException("Could not save Map01.");
            ValidateConfiguration(manager);
            File.WriteAllText(ResultPath, "PASSED\nWeek 4: 12 Land SOs, 6 Naval SOs, two Unit prefabs, Hex fog, Unit sorting and camera raycaster wired in Map01.\n");
        }
        finally { if (opened) EditorSceneManager.CloseScene(map, true); }
    }

    private static GameObject CreateUnitPrefab(bool naval, Sprite icon, Sprite shield)
    {
        string name = naval ? "NavalUnit" : "LandUnit";
        string path = UnitsPath + name + ".prefab";
        bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        try
        {
            Unit unit = root.GetComponent<Unit>();
            if (unit == null) unit = naval ? (Unit)root.AddComponent<NavalUnit>() : root.AddComponent<LandUnit>();
            BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
            if (collider == null) collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.25f, 1.25f);
            // Physics2DRaycaster reads the renderer on the collider object for sorting.
            SpriteRenderer hitOrder = root.GetComponent<SpriteRenderer>();
            if (hitOrder == null) hitOrder = root.AddComponent<SpriteRenderer>();
            hitOrder.sortingLayerName = "Unit";
            hitOrder.sortingOrder = 2;
            SpriteRenderer body = ChildSprite(root, "UnitSprite", icon, 2);
            float scale = 1.6f / icon.bounds.size.y;
            body.transform.localScale = new Vector3(scale, scale, 1);
            SpriteRenderer flag = ChildSprite(root, "FlagSprite", shield, 3);
            scale = 0.5f / Mathf.Max(shield.bounds.size.x, shield.bounds.size.y);
            flag.transform.localScale = new Vector3(scale, scale, 1);
            flag.transform.localPosition = new Vector3(0.75f, -0.35f, 0);
            SpriteRenderer border = ChildSprite(root, "BorderSprite", null, 4);
            LineRenderer line = border.GetComponent<LineRenderer>();
            if (line == null) line = border.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = 0.055f;
            line.numCornerVertices = 2;
            line.sortingLayerName = "Unit";
            line.sortingOrder = 4;
            line.sharedMaterial = SpriteMaterial();
            line.positionCount = 6;
            // Match the supplied 256x384 tiles: the hex occupies the lower 256px,
            // with the shared (0.5, 0.34) pivot and 100 pixels per world unit.
            border.transform.localPosition = new Vector3(0, -0.0256f, 0);
            line.SetPositions(new[] { new Vector3(0, 1.28f), new Vector3(1.28f, 0.64f), new Vector3(1.28f, -0.64f), new Vector3(0, -1.28f), new Vector3(-1.28f, -0.64f), new Vector3(-1.28f, 0.64f) });
            line.startColor = line.endColor = Color.green;
            border.gameObject.SetActive(false);
            SerializedObject so = new SerializedObject(unit);
            so.FindProperty("unitType").enumValueIndex = naval ? 1 : 0;
            so.FindProperty("unitSprite").objectReferenceValue = body;
            so.FindProperty("flagSprite").objectReferenceValue = flag;
            so.FindProperty("borderSprite").objectReferenceValue = border;
            if (naval)
            {
                Transform passengers = root.transform.Find("PassengerParent");
                if (passengers == null) { passengers = new GameObject("PassengerParent").transform; passengers.SetParent(root.transform, false); }
                so.FindProperty("passengerParent").objectReferenceValue = passengers.gameObject;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { if (existing) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root); }
    }

    private static SpriteRenderer ChildSprite(GameObject root, string name, Sprite icon, int order)
    {
        Transform child = root.transform.Find(name);
        if (child == null) { child = new GameObject(name).transform; child.SetParent(root.transform, false); }
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = icon;
        renderer.sortingLayerName = "Unit";
        renderer.sortingOrder = order;
        renderer.sharedMaterial = SpriteMaterial();
        return renderer;
    }

    private static Material SpriteMaterial()
    {
        // Use the installed pipeline's per-renderer sprite texture bindings.
        // The legacy Sprites-Default material repeats the ship texture on its
        // shield when rendered through this project's URP 2D renderer.
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (material == null) throw new InvalidOperationException("Existing URP sprite material unavailable.");
        return material;
    }

    private static void EnsureUnitSortingLayer()
    {
        SerializedObject so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = so.FindProperty("m_SortingLayers");
        for (int i = 0; i < layers.arraySize; i++) if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Unit") return;
        int index = 2; // Between Terrain and FogOfWar, as shown in the manual.
        layers.InsertArrayElementAtIndex(index);
        SerializedProperty entry = layers.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("name").stringValue = "Unit";
        entry.FindPropertyRelative("uniqueID").intValue = 33440001;
        entry.FindPropertyRelative("locked").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T data = AssetDatabase.LoadAssetAtPath<T>(path);
        if (data != null) return data;
        data = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(data, path);
        return data;
    }
    private static Sprite SpriteAt(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Sprite unavailable: " + path);
        return sprite;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
    private static void SetArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    public static void ValidateConfiguration(GameManager manager)
    {
        ColonizationWeek3Setup.ValidateConfiguration(manager);
        if (manager.LandUnitData == null || manager.LandUnitData.Length != 12 || manager.NavalUnitData == null || manager.NavalUnitData.Length != 6) throw new InvalidOperationException("Unit arrays incomplete.");
        for (int i = 0; i < 12; i++)
            if (manager.LandUnitData[i] == null || (int)manager.LandUnitData[i].landUnitType != i || manager.LandUnitData[i].unitIcon == null) throw new InvalidOperationException("Land data missing/out of order: " + i);
        for (int i = 0; i < 6; i++)
            if (manager.NavalUnitData[i] == null || (int)manager.NavalUnitData[i].navalUnitType != i || manager.NavalUnitData[i].shipIcon == null) throw new InvalidOperationException("Naval data missing/out of order: " + i);
        SerializedObject so = new SerializedObject(manager);
        foreach (string field in new[] { "landUnitPrefab", "navalUnitPrefab" })
        {
            GameObject prefab = (GameObject)so.FindProperty(field).objectReferenceValue;
            if (prefab == null || prefab.GetComponent<BoxCollider2D>() == null) throw new InvalidOperationException("Unit prefab/collider missing: " + field);
            SerializedObject unit = new SerializedObject(prefab.GetComponent<Unit>());
            foreach (string spriteField in new[] { "unitSprite", "flagSprite", "borderSprite" })
                if (unit.FindProperty(spriteField).objectReferenceValue == null) throw new InvalidOperationException("Unit renderer reference missing.");
        }
        if (!SortingLayer.layers.Any(layer => layer.name == "Unit")) throw new InvalidOperationException("Unit sorting layer missing.");
    }
}
