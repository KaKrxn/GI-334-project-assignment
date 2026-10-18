using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ColonizationWeek5Setup
{
    public const string RequestPath = "Temp/ColonizationWeek5Setup.v1.request";
    public const string ResultPath = "Temp/ColonizationWeek5Setup.result.txt";
    private const string MapPath = "Assets/Scenes/Map01.unity";
    private const string TownPath = "Assets/Prefabs/Towns/Town.prefab";
    private static readonly string[] NativeNames = { "Powhatan", "Taino", "Onondaga", "Mapuche", "Mexica", "Wendat", "Wampanoag", "Inca" };
    private static readonly string[] TownIcons = { "villageWood00 1", "villageWood01 1", "villageWood02 1", "villageWood03 1", "SteppedPyramid00", "villageWood00 1", "SteppedPyramid00", "SteppedPyramid00" };

    [InitializeOnLoadMethod]
    private static void Initialize() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        try { Apply(); } catch (Exception e) { File.WriteAllText(ResultPath, "FAILED\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Tools/Colonization/Apply Week 5 From Existing Assets")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before Week 5 setup.");
        Scene map = SceneManager.GetSceneByPath(MapPath);
        bool opened = !map.IsValid() || !map.isLoaded;
        if (!opened && map.isDirty) throw new InvalidOperationException("Save Map01 before applying Week 5.");
        GameObject factionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Faction.prefab");
        GameObject landPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/LandUnit.prefab");
        if (factionPrefab == null || landPrefab == null) throw new InvalidOperationException("Apply Week 4 before Week 5.");
        // Every source is already in this project. Five nations temporarily use
        // the neutral Age 1 emblem; the matching three use existing native emblems.
        Sprite[] flags = new Sprite[8], shields = new Sprite[8], towns = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            string emblem = i == 4 ? "Aztec_emb" : i == 6 ? "Maya_emb" : i == 7 ? "Incas_emb" : "Age_1_Dark";
            string flagFolder = emblem == "Age_1_Dark" ? "Shields" : "Flags";
            flags[i] = SpriteAt("Assets/Textures/Emblems_of_civ/" + flagFolder + "/" + emblem + ".png");
            shields[i] = SpriteAt("Assets/Textures/Emblems_of_civ/Shields/" + emblem + ".png");
            towns[i] = SpriteAt("Assets/Textures/Terrain/Tiles/" + TownIcons[i] + ".png");
        }
        EnsureFolder("Assets/Prefabs/Towns");
        EnsureTownSortingLayer();
        GameObject townPrefab = CreateTownPrefab(landPrefab, towns[0], flags[0]);
        FactionData[] nativeData = new FactionData[8];
        for (int i = 0; i < 8; i++)
        {
            string path = "Assets/Scripts/FactionsSO/" + NativeNames[i] + ".asset";
            FactionData data = AssetDatabase.LoadAssetAtPath<FactionData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<FactionData>(); AssetDatabase.CreateAsset(data, path); }
            data.nation = (Nation)(i + 5);
            data.isEuropean = false;
            data.flagIcon = flags[i];
            data.shieldIcon = shields[i];
            data.townIcon = towns[i];
            EditorUtility.SetDirty(data);
            nativeData[i] = data;
        }
        // Week 5 section 27 explicitly hides these while previewing Native towns.
        // Runtime DiscoverHex/SeenHex continue to work during player movement.
        GameObject hex = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Hex.prefab");
        try
        {
            hex.transform.Find("HexFog").gameObject.SetActive(false);
            hex.transform.Find("HexDark").gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(hex, "Assets/Prefabs/Hex.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(hex); }
        if (opened) map = EditorSceneManager.OpenScene(MapPath, OpenSceneMode.Additive);
        try
        {
            GameManager manager = map.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).Single();
            ColonizationWeek4Setup.ValidateConfiguration(manager);
            Faction[] factions = new Faction[13];
            FactionData[] data = new FactionData[13];
            Array.Copy(manager.Factions, factions, 5);
            Array.Copy(manager.FactionData, data, 5);
            GameObject root = map.GetRootGameObjects().Single(r => r.name == "Factions");
            for (int i = 0; i < 8; i++)
            {
                Transform existing = root.transform.Find(NativeNames[i]);
                GameObject obj = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(factionPrefab, map);
                obj.name = NativeNames[i];
                obj.transform.SetParent(root.transform, false);
                obj.transform.SetSiblingIndex(i + 5);
                Faction faction = obj.GetComponent<Faction>();
                faction.FactionInit(nativeData[i]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(faction);
                factions[i + 5] = faction;
                data[i + 5] = nativeData[i];
            }
            SerializedObject so = new SerializedObject(manager);
            SetArray(so.FindProperty("factions"), factions);
            SetArray(so.FindProperty("factionData"), data);
            so.FindProperty("townPrefab").objectReferenceValue = townPrefab;
            so.FindProperty("playerTurn").boolValue = true;
            so.FindProperty("gameTurn").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (manager.GetComponent<AIManager>() == null) Undo.AddComponent<AIManager>(manager.gameObject);
            EditorSceneManager.MarkSceneDirty(map);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(map)) throw new IOException("Could not save Map01.");
            ValidateConfiguration(manager);
            File.WriteAllText(ResultPath, "PASSED\nWeek 5 Town prefab, Town sorting layer, eight Native faction SOs/instances, 13 ordered faction references and AIManager wired in Map01.\nExisting project artwork only; Week 5 Native preview hides prefab Fog/Dark as specified.\n");
        }
        finally { if (opened) EditorSceneManager.CloseScene(map, true); }
    }

    private static GameObject CreateTownPrefab(GameObject landPrefab, Sprite townIcon, Sprite flagIcon)
    {
        bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(TownPath) != null;
        GameObject root = existing ? PrefabUtility.LoadPrefabContents(TownPath) : Object.Instantiate(landPrefab);
        try
        {
            if (!existing && PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "Town";
            LandUnit land = root.GetComponent<LandUnit>();
            if (land != null) Object.DestroyImmediate(land);
            Town town = root.GetComponent<Town>();
            if (town == null) town = root.AddComponent<Town>();
            Transform body = root.transform.Find("TownSprite") ?? root.transform.Find("UnitSprite");
            body.name = "TownSprite";
            body.localScale = new Vector3(0.7f, 0.7f, 1);
            SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();
            bodyRenderer.sprite = townIcon;
            bodyRenderer.sortingLayerName = "Town";
            bodyRenderer.sortingOrder = 1;
            SpriteRenderer flag = root.transform.Find("FlagSprite").GetComponent<SpriteRenderer>();
            flag.sprite = flagIcon;
            float flagScale = 0.5f / Mathf.Max(flagIcon.bounds.size.x, flagIcon.bounds.size.y);
            flag.transform.localScale = new Vector3(flagScale, flagScale, 1);
            SpriteRenderer border = root.transform.Find("BorderSprite").GetComponent<SpriteRenderer>();
            border.gameObject.SetActive(false);
            SpriteRenderer hitOrder = root.GetComponent<SpriteRenderer>();
            hitOrder.sortingLayerName = "Town";
            hitOrder.sortingOrder = 1;
            SerializedObject so = new SerializedObject(town);
            so.FindProperty("townSprite").objectReferenceValue = bodyRenderer;
            so.FindProperty("flagSprite").objectReferenceValue = flag;
            so.FindProperty("borderSprite").objectReferenceValue = border;
            so.FindProperty("visualRange").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, TownPath);
        }
        finally { if (existing) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root); }
    }

    private static void EnsureTownSortingLayer()
    {
        SerializedObject so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = so.FindProperty("m_SortingLayers");
        for (int i = 0; i < layers.arraySize; i++) if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Town") return;
        layers.InsertArrayElementAtIndex(2); // Terrain, Town, Unit, FogOfWar, UI.
        SerializedProperty entry = layers.GetArrayElementAtIndex(2);
        entry.FindPropertyRelative("name").stringValue = "Town";
        entry.FindPropertyRelative("uniqueID").intValue = 33450001;
        entry.FindPropertyRelative("locked").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static Sprite SpriteAt(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Missing existing sprite: " + path);
        return sprite;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
    private static void SetArray(SerializedProperty property, Object[] objects)
    {
        property.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }

    public static void ValidateConfiguration(GameManager manager)
    {
        ColonizationWeek4Setup.ValidateConfiguration(manager);
        if (manager.Factions.Length != 13 || manager.FactionData.Length != 13 || manager.GetComponent<AIManager>() == null) throw new InvalidOperationException("Week 5 faction/AI setup incomplete.");
        for (int i = 5; i < 13; i++)
        {
            Faction fact = manager.Factions[i];
            FactionData data = manager.FactionData[i];
            if (fact == null || data == null || (int)data.nation != i || data.isEuropean || fact.UnitParent == null || fact.TownParent == null || data.flagIcon == null || data.shieldIcon == null || data.townIcon == null)
                throw new InvalidOperationException("Native faction configuration invalid: " + i);
        }
        GameObject prefab = (GameObject)new SerializedObject(manager).FindProperty("townPrefab").objectReferenceValue;
        if (prefab == null || prefab.GetComponent<Town>() == null || prefab.GetComponent<Unit>() != null || prefab.GetComponent<BoxCollider2D>() == null)
            throw new InvalidOperationException("Town prefab invalid.");
        SerializedObject town = new SerializedObject(prefab.GetComponent<Town>());
        foreach (string field in new[] { "townSprite", "flagSprite", "borderSprite" })
            if (town.FindProperty(field).objectReferenceValue == null) throw new InvalidOperationException("Town renderer reference missing.");
        int terrainOrder = SortingLayer.GetLayerValueFromName("Terrain");
        int townOrder = SortingLayer.GetLayerValueFromName("Town");
        int unitOrder = SortingLayer.GetLayerValueFromName("Unit");
        if (!(terrainOrder < townOrder && townOrder < unitOrder)) throw new InvalidOperationException("Town sorting must be between Terrain and Unit.");
    }
}
