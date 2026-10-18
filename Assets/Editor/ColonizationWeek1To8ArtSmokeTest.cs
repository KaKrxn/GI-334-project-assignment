using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ColonizationWeek1To8ArtSmokeTest
{
    const string Key="Week1To8Art.Test";
    const string Dir="Docs/AI/Validation/Week1To8Art/";
    static double since;
    static int phase;
    static ColonizationWeek1To8ArtSmokeTest(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode){phase=0;since=EditorApplication.timeSinceStartup;}};}
    static void Tick()
    {
        if(File.Exists("Temp/Week1To8ArtTest.request")&&!File.Exists(ColonizationWeek1To8ArtSetup.Request)&&!File.Exists("Temp/ColonizationEditorRefresh.request")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
        {
            File.Delete("Temp/Week1To8ArtTest.request");SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;File.WriteAllText(Dir+"PlayMode.result.txt","RUNNING\n");EditorApplication.isPlaying=true;
        }
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.timeSinceStartup-since<5)return;
        try
        {
            if(phase==0){SceneManager.LoadScene("SelectNation");phase=1;since=EditorApplication.timeSinceStartup;return;}
            if(phase==1)
            {
                var screen=UnityEngine.Object.FindAnyObjectByType<SelectNation>();if(screen==null)throw new InvalidOperationException("Missing selection screen");
                string[] names={"England","France","Spain","Netherland","Portugal"};
                foreach(var card in UnityEngine.Object.FindObjectsByType<NationCard>(FindObjectsSortMode.None))if(card.transform.Find("LeaderImage").GetComponent<Image>().sprite!=ColonizationWeek1To8ArtSetup.Sprite("Leaders/"+names[card.NationIndex]))throw new InvalidOperationException("Card portrait not saved");
                for(int i=0;i<5;i++){screen.SelectEuropeNation(i);if(screen.LeaderImage.sprite!=ColonizationWeek1To8ArtSetup.Sprite("Leaders/"+names[i]))throw new InvalidOperationException("Portrait selection failed");}
                Capture("SelectNation.png");SceneManager.LoadScene("Map01");phase=2;since=EditorApplication.timeSinceStartup;return;
            }
            if(phase==2)
            {
                if(ColonizationLegacyDialogFixture.AcknowledgeNotice())return;
                var m=GameManager.instance;var ui=UIManager.instance;if(m==null||ui==null)throw new InvalidOperationException("Map did not initialize");
                foreach(var d in m.ProductData)foreach(var icon in d.icons)if(!AssetDatabase.GetAssetPath(icon).Contains("GeneratedWeek1To8/Products"))throw new InvalidOperationException("Old resource icon");
                foreach(var f in m.Factions.Where(f=>f.IsEuropean))if(!AssetDatabase.GetAssetPath(f.LeaderSprite).Contains("GeneratedWeek1To8/Leaders"))throw new InvalidOperationException("Old European leader");
                Hex hex=m.AllHexes.Cast<Hex>().First(h=>h!=null&&h.X>3&&h.X<35&&h.Y>3&&h.Y<55&&!h.HasTown&&h.HexType!=HexType.Ocean&&h.HexType!=HexType.Mountains);
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Towns/Town.prefab"));var town=go.GetComponent<Town>();town.TownInit(m,m.PlayerFaction);town.CurHex=hex;hex.Town=town;hex.HasTown=true;m.PlayerFaction.Towns.Add(town);m.CurTown=town;
                ui.SetupCurrentTown(hex,HexCalculator.GetHexAroundToArray(m.AllHexes,hex));ui.ToggleTownPanel(true);
                for(int i=0;i<16;i++)if(ui.StockSlots[i].Image.sprite!=m.ProductData[i].icons[0])throw new InvalidOperationException("Warehouse icon not updated");
                if(ui.TownPanel.GetComponent<Image>().sprite!=ColonizationWeek1To8ArtSetup.Sprite("UI/TownPanel"))throw new InvalidOperationException("Town background missing");
                Capture("TownPanel.png");
                var open=ui.TownPanel.transform.Find("UnitPanel/SpecialPanel/ProductionGuideButton").GetComponent<Button>();open.onClick.Invoke();var guide=ui.TownPanel.transform.Find("ProductionGuide");if(!guide.gameObject.activeSelf)throw new InvalidOperationException("Guide did not open");Capture("ProductionGuide.png");guide.Find("CloseButton").GetComponent<Button>().onClick.Invoke();if(guide.gameObject.activeSelf)throw new InvalidOperationException("Guide did not close");
                File.WriteAllText(Dir+"PlayMode.result.txt","PASSED\n"+DateTime.Now+"\nAll 5 selection portraits switch correctly; 5 runtime European leaders, 17 product icons and all 16 warehouse icons reference generated art; TownPanel background and production-guide open/close work.\n");Finish();
            }
        }
        catch(Exception e){File.WriteAllText(Dir+"PlayMode.result.txt","FAILED\n"+e);Debug.LogException(e);Finish();}
    }
    static void Finish(){SessionState.SetBool(Key,false);Application.runInBackground=SessionState.GetBool(Key+".Background",false);EditorApplication.isPlaying=false;}
    static void Capture(string name)
    {
        Camera camera=Camera.main;
        if(camera==null){var obj=new GameObject("Art validation camera");camera=obj.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,0,-10);}
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
        var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
        var rt=new RenderTexture(1920,1080,24);var old=RenderTexture.active;
        var renderers=SceneManager.GetActiveScene().name=="Map01"?UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None):Array.Empty<Renderer>();
        var enabled=renderers.Select(r=>r.enabled).ToArray();
        try
        {
            foreach(var r in renderers)r.enabled=false;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=1;}
            camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var im=new Texture2D(1920,1080,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1920,1080),0,0);im.Apply();File.WriteAllBytes(Dir+name,im.EncodeToPNG());UnityEngine.Object.DestroyImmediate(im);
        }
        finally{for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
