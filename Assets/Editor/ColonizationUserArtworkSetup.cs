using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ColonizationUserArtworkSetup
{
    const string Root="Assets/Textures/UserArtwork";
    const string Source="Art/UserAssets/2026-10-18";
    public const string Request="Temp/UserArtwork.request";
    public const string Result="Docs/AI/Validation/UserArtwork/Import.result.txt";
    static readonly string[] Lands={"01_Free_Colonist","02_Soldier","03_Hardy_Pioneers","04_Plain_Indians","05_Tropical_Indians","06_Scouts","07_Farmer","08_Fisherman","09_Lumberjack","10_Carpenter","11_Ore_Miner","12_Blacksmith"};
    static readonly string[] Ships={"01_Caravel","02_Merchantman","03_Galleon","04_Privateer","05_Frigate","06_Man_Of_War"};
    public static readonly string[] NativeNames={"Powhatan","Taino","Onondaga","Mapuche","Mexica","Wendat","Wampanoag","Inca"};
    static ColonizationUserArtworkSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||File.Exists("Temp/ColonizationEditorRefresh.request")||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isUpdating||EditorApplication.isCompiling)return;
        File.Delete(Request);try{Apply();}catch(Exception e){Directory.CreateDirectory(Path.GetDirectoryName(Result));File.WriteAllText(Result,"FAILED\n"+e);Debug.LogException(e);}
    }
    [MenuItem("Tools/Colonization/Apply User Unit and Native Artwork")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory("Docs/AI/Validation/UserArtwork");
        var lands=AssetDatabase.FindAssets("t:LandUnitData",new[]{"Assets/Scripts/LandUnitsSO"}).Select(g=>AssetDatabase.LoadAssetAtPath<LandUnitData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(d=>(int)d.landUnitType).ToArray();
        var ships=AssetDatabase.FindAssets("t:NavalUnitData",new[]{"Assets/Scripts/NavalUnitsSO"}).Select(g=>AssetDatabase.LoadAssetAtPath<NavalUnitData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(d=>(int)d.navalUnitType).ToArray();
        if(lands.Length!=12||ships.Length!=6)throw new InvalidOperationException("Unexpected unit roster.");
        // Import original-resolution PNGs. PPU preserves the authored prefab
        // body sizes (land: 10.24*.15625; ship: 1.28*1.25 = 1.6 world units).
        foreach(string p in Directory.GetFiles(Source+"/Colonial_Units_20_PNG/Originals","*.png")) Import(p,Root+"/Land/"+Path.GetFileName(p),10.24f);
        foreach(string p in Directory.GetFiles(Source+"/Colonial_Naval_Units_6_PNG/Originals","*.png")) Import(p,Root+"/Naval/"+Path.GetFileName(p),1.28f);
        foreach(string p in Directory.GetFiles(Source+"/Week6_Native_American_8_Nations/Week6_Native_American","*.png",SearchOption.AllDirectories))
        {
            string nation=new DirectoryInfo(Path.GetDirectoryName(p)).Name;
            string kind=Path.GetFileNameWithoutExtension(p);
            Import(p,Root+"/Natives/"+nation+"/"+kind+".png",kind=="village"?2.56f:kind=="flag"?5.12f:10.24f);
        }
        var sprites=Lands.Select(n=>Load("Land/"+n+".png")).ToArray();
        var naval=Ships.Select(n=>Load("Naval/"+n+".png")).ToArray();
        for(int i=0;i<12;i++){Undo.RecordObject(lands[i],"Apply user unit artwork");lands[i].unitIcon=sprites[i];EditorUtility.SetDirty(lands[i]);}
        for(int i=0;i<6;i++){Undo.RecordObject(ships[i],"Apply user ship artwork");ships[i].shipIcon=naval[i];EditorUtility.SetDirty(ships[i]);}
        MigrateNatives();
        // Prefab preview sprites use the same icons as runtime initialization.
        Preview("Assets/Prefabs/Units/LandUnit.prefab",sprites[0]);
        Preview("Assets/Prefabs/Units/NavalUnit.prefab",naval[0]);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Result,"PASSED\n"+DateTime.Now+"\nImported 50 images; 12 land + 6 naval definitions and all 8 Native nations wired. Native enum numeric slots and asset GUIDs preserved; roster names updated. Only 8 extra land images remain reserves.\n");
    }
    static void MigrateNatives()
    {
        var data=AssetDatabase.FindAssets("t:FactionData",new[]{"Assets/Scripts/FactionsSO"}).Select(g=>AssetDatabase.LoadAssetAtPath<FactionData>(AssetDatabase.GUIDToAssetPath(g))).Where(d=>!d.isEuropean).OrderBy(d=>(int)d.nation).ToArray();
        if(data.Length!=8)throw new InvalidOperationException("Expected eight Native factions.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Map01.unity");
        if(scene.IsValid()&&scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save Map01 before roster migration.");
        for(int i=0;i<8;i++)
        {
            if((int)data[i].nation!=i+5)throw new InvalidOperationException("Unexpected Native numeric slot.");
            string name=NativeNames[i];
            string path=AssetDatabase.GetAssetPath(data[i]);
            string error=Path.GetFileNameWithoutExtension(path)==name?"":AssetDatabase.RenameAsset(path,name);
            if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            data[i].isTropicalNatives=name=="Taino"||name=="Mexica"||name=="Inca";
            ApplyNative(name,name);
        }
        if(scene.IsValid()&&scene.isLoaded)
        {
            foreach(var root in scene.GetRootGameObjects())foreach(var f in root.GetComponentsInChildren<Faction>(true))
            {
                int i=(int)f.Nation-5;if(i<0||i>=8)continue;
                var so=new SerializedObject(f);
                foreach(string field in new[]{"flagIcon","shieldIcon","townIcon","leaderSprite"})
                    so.FindProperty(field).objectReferenceValue=new SerializedObject(data[i]).FindProperty(field).objectReferenceValue;
                so.FindProperty("isTropicalNatives").boolValue=data[i].isTropicalNatives;so.ApplyModifiedPropertiesWithoutUndo();
                f.gameObject.name=NativeNames[i];
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save roster scene.");
        }
    }
    static void ApplyNative(string faction,string folder)
    {
        var d=AssetDatabase.LoadAssetAtPath<FactionData>("Assets/Scripts/FactionsSO/"+faction+".asset");
        if(d==null)throw new InvalidOperationException(faction);
        Undo.RecordObject(d,"Apply Native nation artwork");
        d.townIcon=Load("Natives/"+folder+"/village.png");d.leaderSprite=Load("Natives/"+folder+"/leader.png");
        d.flagIcon=Load("Natives/"+folder+"/flag.png");d.shieldIcon=d.flagIcon;
        EditorUtility.SetDirty(d);
    }
    static Sprite Load(string path)
    {
        var s=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+path);if(s==null)throw new InvalidOperationException(path);return s;
    }
    static void Import(string source,string destination,float worldHeight)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,true);
        AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
        // Read native PNG dimensions, independent of the first-pass importer
        // defaults (which may resize NPOT textures before settings are applied).
        var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(source));
        var imp=(TextureImporter)AssetImporter.GetAtPath(destination);
        imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
        imp.spritePixelsPerUnit=t.height/worldHeight;
        if(Path.GetFileName(destination)=="flag.png")imp.spritePixelsPerUnit=Mathf.Max(t.width,t.height)/worldHeight;
        UnityEngine.Object.DestroyImmediate(t);
        var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(settings);
        imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=2048;imp.filterMode=FilterMode.Bilinear;imp.wrapMode=TextureWrapMode.Clamp;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
    }
    static void Preview(string path,Sprite sprite)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try{root.GetComponent<Unit>().UnitSprite.sprite=sprite;PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
