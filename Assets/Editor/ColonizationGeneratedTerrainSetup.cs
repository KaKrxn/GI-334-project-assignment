using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Generated artwork is normalized here, leaving the delivered source PNGs intact.
[InitializeOnLoad]
public static class ColonizationGeneratedTerrainSetup
{
    const string Request = "Temp/GeneratedTerrain.request";
    const string Result = "Temp/GeneratedTerrain.result.txt";
    const string Output = "Assets/Textures/Terrain/GeneratedV2";
    const string Source = "Art/GeneratedHexTiles/2026-10-17-v2";
    static readonly string[] Names = { "Ocean", "Grassland", "Prairie", "Savanna", "Plain", "Tundra", "Desert", "Swamp", "Arctic", "Hills", "Mountains" };
    static readonly string[] Forests = { "", "ConiferForest", "BroadleafForest", "SavannaWoodland", "MixedForest", "BorealForest", "ScrubForest", "TropicalRainforest", "", "", "" };
    static ColonizationGeneratedTerrainSetup() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (!File.Exists(Request) || File.Exists("Temp/ColonizationEditorRefresh.request") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request);
        try { Apply(); } catch (Exception e) { File.WriteAllText(Result, "FAILED\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Tools/Colonization/Apply Generated Terrain V2")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var data = AssetDatabase.FindAssets("t:HexData", new[] { "Assets/Scripts/HexSO" })
            .Select(g => AssetDatabase.LoadAssetAtPath<HexData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(d => (int)d.type).ToArray();
        if (data.Length != 11) throw new InvalidOperationException("Expected eleven terrain definitions.");
        foreach (string n in Names) if (!File.Exists(Source + "/Assignment2/" + n + "_01.png")) throw new FileNotFoundException(n);
        foreach (string n in Forests.Where(n => n.Length > 0)) if (!File.Exists(Source + "/Assignment3/" + n + "_01.png")) throw new FileNotFoundException(n);
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory("Docs/AI/Validation/GeneratedTerrain");
        var report = new StringBuilder();
        Sprite[] bases = new Sprite[11], trees = new Sprite[11];
        for (int i = 0; i < 11; i++)
        {
            bases[i] = Import(Names[i], 2, report);
            if (Forests[i].Length > 0) trees[i] = Import(Forests[i], 3, report);
        }
        // Resolve all sprites before changing any serialized references.
        for (int i = 0; i < 11; i++)
        {
            Undo.RecordObject(data[i], "Use generated terrain artwork");
            data[i].terrainSprites = new[] { bases[i] };
            data[i].forestSprites = trees[i] == null ? Array.Empty<Sprite>() : new[] { trees[i] };
            EditorUtility.SetDirty(data[i]);
        }
        AssetDatabase.SaveAssets();
        Validate(data);
        Capture(data);
        File.WriteAllText("Docs/AI/Validation/GeneratedTerrain/Import.result.txt", "PASSED\n" + DateTime.Now + "\n" + report);
        File.WriteAllText(Result, "PASSED\n18 normalized sprites imported; 11 HexData assets wired; preview rendered.\n" + report);
    }

    static Sprite Import(string name, int assignment, StringBuilder report)
    {
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var dst = new Texture2D(1152, 1664, TextureFormat.RGBA32, false);
        try
        {
            if (!src.LoadImage(File.ReadAllBytes(Source + "/Assignment" + assignment + "/" + name + "_01.png"))) throw new InvalidOperationException(name);
            int w = src.width, h = src.height;
            Color32[] pixels = src.GetPixels32();
            int[] lo = new int[h], hi = new int[h];
            int bottom = h, left = w, right = 0, maxWidth = 0;
            for (int y = 0; y < h; y++)
            {
                lo[y] = w; hi[y] = -1;
                for (int x = 0; x < w; x++) if (pixels[y * w + x].a > 128) { lo[y] = Math.Min(lo[y], x); hi[y] = x; }
                if (hi[y] >= lo[y]) { bottom = Math.Min(bottom, y); left = Math.Min(left, lo[y]); right = Math.Max(right, hi[y]); maxWidth = Math.Max(maxWidth, hi[y] - lo[y] + 1); }
            }
            int corner = bottom;
            while (corner < h && hi[corner] - lo[corner] + 1 < maxWidth * .985f) corner++;
            if (corner >= h || corner - bottom < 50) throw new InvalidOperationException("Cannot resolve ground corners: " + name);
            float scaleY = (corner - bottom) / 256f;
            int upperCorner = corner;
            for (int y = corner; y < h; y++) if (hi[y] - lo[y] + 1 >= maxWidth * .985f) upperCorner = y;
            float centerX = (left + right) * .5f;
            Color32[] output = new Color32[1152 * 1664];
            for (int y = 64; y < 1600; y++)
            {
                int sy = Mathf.RoundToInt(y < 320 ? bottom + (y - 64) * scaleY : y <= 832
                    ? Mathf.Lerp(corner, upperCorner, (y - 320) / 512f)
                    : upperCorner + (y - 832) * scaleY);
                if (sy < 0 || sy >= h) continue;
                float half = y < 320 ? (y - 64) * 2f : y <= 832 ? 512f : (1088 - y) * 2f;
                for (int x = 64; x < 1088; x++)
                {
                    bool ground = y <= 1088 && Mathf.Abs(x + .5f - 576) <= half;
                    if (y <= 832 && !ground) continue;
                    int sx = Mathf.RoundToInt(centerX + (x + .5f - 576) * (right - left + 1) / 1024f);
                    if (ground && hi[sy] >= lo[sy] && half > 0)
                        sx = Mathf.RoundToInt(Mathf.Lerp(lo[sy], hi[sy], Mathf.Clamp01((x + .5f - (576 - half)) / (2 * half))));
                    if (sx < 0 || sx >= w) continue;
                    Color32 c = pixels[sy * w + sx];
                    if (ground) c.a = 255;
                    else if (c.a < 16) c = new Color32();
                    output[y * 1152 + x] = c;
                }
            }
            dst.SetPixels32(output); dst.Apply();
            string path = Output + "/" + name + ".png";
            File.WriteAllBytes(path, dst.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, (1536 * .34f + 64) / 1664f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = 400;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null || Mathf.Abs(sprite.rect.width - 1152) > .1f) throw new InvalidOperationException("Import failed: " + name);
            report.AppendLine(name + ": source bottom=" + bottom + ", lower corner=" + corner + ", width=" + (right-left+1) + "; PPU400, padded 1152x1664");
            return sprite;
        }
        finally { UnityEngine.Object.DestroyImmediate(src); UnityEngine.Object.DestroyImmediate(dst); }
    }

    static void Validate(HexData[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            if ((int)data[i].type != i || data[i].terrainSprites.Length != 1 || data[i].terrainSprites[0] == null || data[i].forestSprites.Length != (Forests[i].Length > 0 ? 1 : 0)) throw new InvalidOperationException("Invalid terrain wiring " + i);
            foreach (Sprite s in data[i].terrainSprites.Concat(data[i].forestSprites)) if (!AssetDatabase.GetAssetPath(s).StartsWith(Output + "/")) throw new InvalidOperationException("Old artwork still referenced.");
        }
    }

    static void Capture(HexData[] data)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var cameraObject = new GameObject("Generated terrain verification camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene;
        camera.orthographic = true; camera.orthographicSize = 6.5f;
        camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.25f,.27f,.3f);
        var materials = new System.Collections.Generic.List<Material>();
        var rt = new RenderTexture(1600, 1000, 24); var old = RenderTexture.active;
        try
        {
            int index = 0;
            foreach (var d in data) foreach (var sprite in d.terrainSprites.Concat(d.forestSprites))
            {
                var go = new GameObject(sprite.name); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.position = new Vector3((index % 6 - 2.5f) * 3.2f, (1-index/6) * 4, 0);
                var material = new Material(Shader.Find("Sprites/Default")); material.mainTexture = sprite.texture; materials.Add(material);
                var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material; index++;
            }
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply();
            File.WriteAllBytes("Docs/AI/Validation/GeneratedTerrain/ImportedSprites.png", image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
        }
        finally { RenderTexture.active = old; camera.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); foreach (var material in materials) UnityEngine.Object.DestroyImmediate(material); UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
    }
}
