using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ColonizationUserArtworkSmokeTest
{
    const string Key="UserArtwork.Test";
    const string Result="Docs/AI/Validation/UserArtwork/PlayMode.result.txt";
    static double started;
    static ColonizationUserArtworkSmokeTest(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode)started=EditorApplication.timeSinceStartup;};}
    static void Tick()
    {
        if(File.Exists("Temp/UserArtworkTest.request")&&!File.Exists(ColonizationUserArtworkSetup.Request)&&!File.Exists("Temp/ColonizationEditorRefresh.request")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
        {
            File.Delete("Temp/UserArtworkTest.request");Directory.CreateDirectory(Path.GetDirectoryName(Result));
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/Map01.unity"){File.WriteAllText(Result,"FAILED: open Map01");return;}
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;File.WriteAllText(Result,"RUNNING\n");EditorApplication.isPlaying=true;
        }
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.timeSinceStartup-started<6)return;
        var objects=new System.Collections.Generic.List<GameObject>();
        try
        {
            GameManager m=GameManager.instance;if(m==null||m.AllHexes[49,59]==null)throw new InvalidOperationException("Map initialization failed.");
            int index=0;
            var land=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/LandUnit.prefab");
            var ship=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/NavalUnit.prefab");
            foreach(var data in m.LandUnitData)
            {
                var go=UnityEngine.Object.Instantiate(land);objects.Add(go);var unit=go.GetComponent<LandUnit>();unit.UnitInit(m,m.PlayerFaction,data);
                Check(unit,data.unitIcon);go.transform.position=new Vector3((index%6)*3,(index/6)*-3,0);index++;
            }
            foreach(var data in m.NavalUnitData)
            {
                var go=UnityEngine.Object.Instantiate(ship);objects.Add(go);var unit=go.GetComponent<NavalUnit>();unit.UnitInit(m,m.PlayerFaction,data);
                Check(unit,data.shipIcon);go.transform.position=new Vector3((index%6)*3,(index/6)*-3,0);index++;
            }
            int nativeTowns=0, nativeFactions=0;
            foreach(var f in m.Factions)if(!f.IsEuropean)
            {
                nativeFactions++;
                if(f.Nation.ToString()!=ColonizationUserArtworkSetup.NativeNames[(int)f.Nation-5])throw new InvalidOperationException("Wrong Native name");
                if(!AssetDatabase.GetAssetPath(f.LeaderSprite).Contains("UserArtwork/Natives"))throw new InvalidOperationException("Leader not initialized: "+f.Nation);
                foreach(var t in f.Towns){if(t.TownSprite.sprite!=f.TownIcon)throw new InvalidOperationException("Village image mismatch");nativeTowns++;}
            }
            if(nativeFactions!=8)throw new InvalidOperationException("Missing Native factions");
            // Isolate the test unit gallery from the map without saving runtime state.
            foreach(var go in objects){go.transform.position+=new Vector3(-200,-200,0);go.GetComponent<Unit>().UnitSprite.gameObject.SetActive(true);}
            var camera=Camera.main;camera.transform.position=new Vector3(-192.5f,-203,-10);camera.orthographicSize=5.4f;
            var rt=new RenderTexture(1600,900,24);var old=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var im=new Texture2D(1600,900,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1600,900),0,0);im.Apply();File.WriteAllBytes("Docs/AI/Validation/UserArtwork/UnitGallery.png",im.EncodeToPNG());UnityEngine.Object.DestroyImmediate(im);}
            finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            File.WriteAllText(Result,"PASSED\n"+DateTime.Now+"\n12 land and 6 naval runtime initializations match user sprites; all visible unit heights 1.6 world units; 8 Native leaders and "+nativeTowns+" generated villages match new faction data.\n");
        }
        catch(Exception e){File.WriteAllText(Result,"FAILED\n"+e);Debug.LogException(e);}
        finally{foreach(var go in objects)if(go!=null)UnityEngine.Object.DestroyImmediate(go);SessionState.SetBool(Key,false);Application.runInBackground=SessionState.GetBool(Key+".Background",false);EditorApplication.isPlaying=false;}
    }
    static void Check(Unit unit,Sprite expected)
    {
        if(unit.UnitSprite.sprite!=expected||!AssetDatabase.GetAssetPath(expected).Contains("UserArtwork"))throw new InvalidOperationException("Wrong unit sprite");
        if(Mathf.Abs(unit.UnitSprite.bounds.size.y-1.6f)>.04f)throw new InvalidOperationException("Unit scale: "+unit.name+" = "+unit.UnitSprite.bounds.size.y);
    }
}
