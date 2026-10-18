using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ColonizationGeneratedTerrainSmokeTest
{
    const string Key = "GeneratedTerrain.Test";
    const string Result = "Docs/AI/Validation/GeneratedTerrain/PlayMode.result.txt";
    static double start;
    static ColonizationGeneratedTerrainSmokeTest()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredPlayMode) start = EditorApplication.timeSinceStartup; };
    }
    static void Tick()
    {
        if (File.Exists("Temp/GeneratedTerrainTest.request") && !File.Exists("Temp/GeneratedTerrain.request") && !File.Exists("Temp/ColonizationEditorRefresh.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            File.Delete("Temp/GeneratedTerrainTest.request");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Map01.unity") { File.WriteAllText(Result,"FAILED: Map01 must be open"); return; }
            SessionState.SetBool(Key, true); SessionState.SetBool(Key+".Background", Application.runInBackground); Application.runInBackground = true;
            File.WriteAllText(Result,"RUNNING\n"); EditorApplication.isPlaying = true;
        }
        if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.timeSinceStartup-start < 6) return;
        try
        {
            var m = GameManager.instance;
            if (m == null || m.AllHexes[49,59] == null) throw new InvalidOperationException("Map did not initialize.");
            int count = 0, forests = 0;
            Hex forest = null;
            foreach (Hex hex in m.AllHexes)
            {
                if (hex == null || !AssetDatabase.GetAssetPath(hex.TerrainSprite.sprite).Contains("GeneratedV2")) throw new InvalidOperationException("Old/missing map sprite.");
                if (hex.HasForest) { forests++; forest = hex; if (!AssetDatabase.GetAssetPath(hex.ForestSprite.sprite).Contains("GeneratedV2")) throw new InvalidOperationException("Old forest sprite."); }
                count++;
            }
            if (forest == null) throw new InvalidOperationException("No forest generated.");
            Sprite before = forest.TerrainSprite.sprite; int cost = forest.MoveCost;
            forest.ClearForest();
            if (forest.HasForest || forest.ForestSprite.gameObject.activeSelf || forest.TerrainSprite.sprite != before || forest.MoveCost != cost-1) throw new InvalidOperationException("Forest clearing failed.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/TerrainSlot.prefab");
            var slot = UnityEngine.Object.Instantiate(prefab).GetComponent<TerrainSlot>();
            slot.HexSlotInit(forest);
            var image = slot.GetComponentsInChildren<UnityEngine.UI.Image>(true).Single(x=>x.name=="TerrainImage");
            if (Vector2.Distance(image.rectTransform.sizeDelta,new Vector2(288,416))>.1f) throw new InvalidOperationException("Town terrain scale incorrect.");
            UnityEngine.Object.DestroyImmediate(slot.gameObject);
            // Capture actual generated map through its authored camera.
            foreach (Hex hex in m.AllHexes) hex.DiscoverHex();
            Camera camera = Camera.main; camera.transform.position = new Vector3(forest.Pos.x,forest.Pos.y,-10); camera.orthographicSize=12;
            var rt = new RenderTexture(1600,1000,24); var old=RenderTexture.active;
            try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; var im=new Texture2D(1600,1000,TextureFormat.RGB24,false); im.ReadPixels(new Rect(0,0,1600,1000),0,0); im.Apply(); File.WriteAllBytes("Docs/AI/Validation/GeneratedTerrain/Map.png",im.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(im); }
            finally { camera.targetTexture=null; RenderTexture.active=old; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            File.WriteAllText(Result,"PASSED\n"+DateTime.Now+"\n"+count+" generated Hexes use new sprites; "+forests+" forest overlays; clearing preserves base and removes movement surcharge; town image 288x416.\n");
        }
        catch(Exception e) { File.WriteAllText(Result,"FAILED\n"+e); Debug.LogException(e); }
        finally { SessionState.SetBool(Key,false); Application.runInBackground=SessionState.GetBool(Key+".Background",false); EditorApplication.isPlaying=false; }
    }
}
