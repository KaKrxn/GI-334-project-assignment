using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Applies the manual's Week 3 wiring through Unity's serialization/prefab APIs.
public static class ColonizationWeek3Setup
{
    private const string MapPath = "Assets/Scenes/Map01.unity";
    private const string ResultPath = "Temp/ColonizationWeek3Setup.result.txt";
    private const string RequestPath = "Temp/ColonizationWeek3Setup.request";
    private const string HexPrefabPath = "Assets/Prefabs/Hex.prefab";
    private const string FactionPrefabPath = "Assets/Prefabs/Faction.prefab";
    private const string TilesPath = "Assets/Textures/Terrain/Tiles/";

    private static readonly string[] TerrainNames =
        { "Ocean", "Grassland", "Prairie", "Savanna", "Plain", "Tundra", "Desert", "Swamp", "Arctic", "Hills", "Mountains" };
    private static readonly string[] TerrainFiles =
        { "00_Ocean", "01_Grassland", "02_Prairie", "03_Savanna", "04_Plain", "05_Tundra", "06_Desert", "07_Swamp", "08_Arctic", "09_Hills", "010_Mountain" };
    private static readonly string[] NationNames = { "England", "France", "Spain", "Netherland", "Portugal" };
    private static readonly string[] EmblemNames = { "England_emb", "Franks_emb", "Spain_emb", "Dutch", "Portuguese_emb" };

    [InitializeOnLoadMethod]
    private static void ScheduleRequestedSetup()
    {
        EditorApplication.update += CheckRequestedSetup;
    }

    private static double nextRequestCheck;
    private static void CheckRequestedSetup()
    {
        if (EditorApplication.timeSinceStartup < nextRequestCheck) return;
        nextRequestCheck = EditorApplication.timeSinceStartup + 1;
        if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(RequestPath))
            ApplyRequestedSetup();
    }

    private static void ApplyRequestedSetup()
    {
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        try
        {
            Apply();
        }
        catch (Exception e)
        {
            File.WriteAllText(ResultPath, "FAILED\n" + e);
            Debug.LogException(e);
        }
    }

    [MenuItem("Tools/Colonization/Apply Week 3 From Existing Assets")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before applying Week 3.");

        Scene map = SceneManager.GetSceneByPath(MapPath);
        bool openedForSetup = !map.IsValid() || !map.isLoaded;
        if (!openedForSetup && map.isDirty)
            throw new InvalidOperationException("Map01 has unsaved changes. Save it, then run Tools/Colonization/Apply Week 3 From Existing Assets.");

        // Resolve every source before writing assets; nothing is imported/downloaded.
        HexData[] terrain = LoadTerrainData();
        Sprite[][] terrainSprites = BuildTerrainSprites();
        Sprite[][] forestSprites = BuildForestSprites();
        Sprite[] flags = new Sprite[5];
        Sprite[] shields = new Sprite[5];
        for (int i = 0; i < 5; i++)
        {
            string flag = i == 3 ? "DutchFlag" : EmblemNames[i];
            string shield = i == 3 ? "DutchShield" : EmblemNames[i];
            flags[i] = LoadSprite("Assets/Textures/Emblems_of_civ/Flags/" + flag + ".png");
            shields[i] = LoadSprite("Assets/Textures/Emblems_of_civ/Shields/" + shield + ".png");
        }
        Sprite townIcon = LoadSprite(TilesPath + "villageWood00 1.png");

        for (int i = 0; i < terrain.Length; i++)
        {
            terrain[i].hexName = TerrainNames[i];
            terrain[i].type = (HexType)i;
            terrain[i].terrainSprites = terrainSprites[i];
            terrain[i].forestSprites = forestSprites[i];
            terrain[i].moveCost = i == 10 ? 3 : i == 7 || i == 9 ? 2 : 1;
            // Yield data is introduced later in the manual.
            EditorUtility.SetDirty(terrain[i]);
        }

        EnsureFolder("Assets/Scripts/FactionsSO");
        FactionData[] data = new FactionData[5];
        for (int i = 0; i < 5; i++)
        {
            string path = "Assets/Scripts/FactionsSO/" + NationNames[i] + ".asset";
            data[i] = AssetDatabase.LoadAssetAtPath<FactionData>(path);
            if (data[i] == null)
            {
                data[i] = ScriptableObject.CreateInstance<FactionData>();
                AssetDatabase.CreateAsset(data[i], path);
            }
            data[i].nation = (Nation)i;
            data[i].isEuropean = true;
            data[i].flagIcon = flags[i];
            data[i].shieldIcon = shields[i];
            data[i].townIcon = townIcon;
            EditorUtility.SetDirty(data[i]);
        }

        RepairHexPrefab();
        GameObject factionPrefab = CreateFactionPrefab();

        if (openedForSetup)
            map = EditorSceneManager.OpenScene(MapPath, OpenSceneMode.Additive);
        try
        {
            GameManager manager = map.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).Single();
            GameObject factionsRoot = map.GetRootGameObjects().FirstOrDefault(root => root.name == "Factions");
            if (factionsRoot == null)
            {
                factionsRoot = new GameObject("Factions");
                SceneManager.MoveGameObjectToScene(factionsRoot, map);
            }
            factionsRoot.transform.position = Vector3.zero;
            Faction[] factions = new Faction[5];
            for (int i = 0; i < 5; i++)
            {
                Transform existing = factionsRoot.transform.Find(NationNames[i]);
                GameObject instance = existing != null ? existing.gameObject :
                    (GameObject)PrefabUtility.InstantiatePrefab(factionPrefab, map);
                instance.name = NationNames[i];
                instance.transform.SetParent(factionsRoot.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.SetSiblingIndex(i);
                factions[i] = instance.GetComponent<Faction>();
                factions[i].FactionInit(data[i]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(factions[i]);
            }

            SerializedObject serialized = new SerializedObject(manager);
            SetArray(serialized.FindProperty("hexData"), terrain);
            SetArray(serialized.FindProperty("factions"), factions);
            SetArray(serialized.FindProperty("factionData"), data);
            serialized.FindProperty("hexPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(HexPrefabPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(map);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(map))
                throw new IOException("Could not save Map01.");
            ValidateConfiguration(manager);
            File.WriteAllText(ResultPath, "PASSED\nWeek 3 setup complete: 11 Terrain SOs, 5 Faction SOs, Faction prefab and Map01 references.\n");
            Debug.Log("Colonization Week 3 setup PASSED. Run Tools/Colonization/Run Week 3 Play Mode Smoke Test to verify the map.");
        }
        finally
        {
            if (openedForSetup && map.IsValid())
                EditorSceneManager.CloseScene(map, true);
        }
    }

    private static HexData[] LoadTerrainData()
    {
        return TerrainFiles.Select(name =>
        {
            string path = "Assets/Scripts/HexSO/" + name + ".asset";
            HexData data = AssetDatabase.LoadAssetAtPath<HexData>(path);
            if (data == null) throw new InvalidOperationException("Missing HexData: " + path);
            return data;
        }).ToArray();
    }

    private static Sprite[][] BuildTerrainSprites()
    {
        return new[]
        {
            Variants("hexOceanCalm"), Variants("hexPlainsCold", " 1"),
            Variants("hexDirt", " 1"), Variants("hexDesertRedGrass", " 1"),
            Variants("hexTropicalPlains", " 1"), Variants("hexPlainsColdSnowTransition", " 1"),
            Variants("hexDesertYellowDirt", " 1"), Variants("hexWetlands", " 1"),
            Variants("hexSnowField", " 1"), Variants("hexHills"), Variants("hexMountain", " 1")
        };
    }

    private static Sprite[][] BuildForestSprites()
    {
        return new[]
        {
            Array.Empty<Sprite>(), Variants("hexForestPine", " 1"),
            Variants("hexForestBroadleaf"), Variants("hexDesertRedForest", " 1"),
            Variants("hexWoodlands"), Variants("hexForestPineSnowTransition"),
            Variants("hexDesertYellowCactiForest", " 1"), Variants("hexJungle"),
            Array.Empty<Sprite>(), Array.Empty<Sprite>(), Array.Empty<Sprite>()
        };
    }

    private static Sprite[] Variants(string prefix, string suffix = "")
    {
        return Enumerable.Range(0, 4).Select(i => LoadSprite(TilesPath + prefix + i.ToString("00") + suffix + ".png")).ToArray();
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Missing existing sprite: " + path);
        return sprite;
    }

    private static void RepairHexPrefab()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(HexPrefabPath);
        try
        {
            Hex hex = contents.GetComponent<Hex>();
            TMP_Text text = contents.GetComponentInChildren<TMP_Text>(true);
            SerializedObject serialized = new SerializedObject(hex);
            serialized.FindProperty("hexText").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (string name in new[] { "HexForest", "HexFog", "HexDark" })
                contents.transform.Find(name).gameObject.SetActive(false);
            text.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(contents, HexPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    private static GameObject CreateFactionPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FactionPrefabPath);
        if (prefab != null) return prefab;
        GameObject root = new GameObject("Faction");
        try
        {
            Faction faction = root.AddComponent<Faction>();
            GameObject towns = new GameObject("Towns");
            GameObject units = new GameObject("Units");
            towns.transform.SetParent(root.transform, false);
            units.transform.SetParent(root.transform, false);
            SerializedObject serialized = new SerializedObject(faction);
            serialized.FindProperty("townParent").objectReferenceValue = towns.transform;
            serialized.FindProperty("unitParent").objectReferenceValue = units.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, FactionPrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
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
        for (int i = 0; i < objects.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }

    public static void ValidateConfiguration(GameManager manager)
    {
        if (manager.HexData.Length != 11 || manager.Factions.Length < 5 || manager.FactionData.Length != manager.Factions.Length)
            throw new InvalidOperationException("Incorrect Terrain/Faction array sizes.");
        for (int i = 0; i < 11; i++)
        {
            HexData data = manager.HexData[i];
            if (data == null || (int)data.type != i || data.terrainSprites.Length == 0 || data.terrainSprites.Any(sprite => sprite == null))
                throw new InvalidOperationException("Terrain data is missing or out of order: " + i);
            if (data.forestSprites.Any(sprite => sprite == null))
                throw new InvalidOperationException("Forest reference missing: " + i);
            if (data.moveCost != (i == 10 ? 3 : i == 7 || i == 9 ? 2 : 1))
                throw new InvalidOperationException("Incorrect Move Cost: " + i);
        }
        for (int i = 0; i < 5; i++)
        {
            Faction faction = manager.Factions[i];
            FactionData data = manager.FactionData[i];
            if (faction == null || data == null || (int)data.nation != i || !data.isEuropean ||
                faction.TownParent == null || faction.UnitParent == null || data.flagIcon == null || data.shieldIcon == null || data.townIcon == null)
                throw new InvalidOperationException("Faction configuration missing or out of order: " + i);
        }
        SerializedObject serialized = new SerializedObject(manager);
        if (serialized.FindProperty("grid").objectReferenceValue == null || serialized.FindProperty("hexParent").objectReferenceValue == null)
            throw new InvalidOperationException("Grid/Hex Parent reference missing.");
        GameObject prefab = (GameObject)serialized.FindProperty("hexPrefab").objectReferenceValue;
        SerializedObject hex = new SerializedObject(prefab.GetComponent<Hex>());
        foreach (string field in new[] { "hexText", "terrainSprite", "forestSprite", "fogSprite", "darkSprite" })
            if (hex.FindProperty(field).objectReferenceValue == null)
                throw new InvalidOperationException("Hex prefab reference missing: " + field);
    }
}
