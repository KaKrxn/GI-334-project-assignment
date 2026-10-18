using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ColonizationWeek8SmokeTest
{
    public const string Request="Temp/ColonizationWeek8SmokeTest.v3.request";
    public const string Result="Temp/ColonizationWeek8SmokeTest.result.txt";
    private const string Key="Colonization.Week8Smoke";
    private static readonly int[] SelectionOrder={4,1,3,2,0};
    private static int phase,frames,scenario;
    private static double started,stableSince,phaseStarted,initialized;
    private static string error;
    private static GameObject host;

    [InitializeOnLoadMethod] private static void Initialize()
    {
        initialized=stableSince=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=PlayChanged;
        Application.logMessageReceived+=(message,stack,type)=> { if(SessionState.GetBool(Key+".Pending",false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) error=message+"\n"+stack; };
    }
    [MenuItem("Tools/Colonization/Run Week 8 Play Mode Smoke Test")]
    public static void Run()
    {
        Preflight();
        if(SceneManager.sceneCount!=1) throw new InvalidOperationException("Open one saved scene before the regression suite.");
        SessionState.SetString(Key+".OriginalScene",SceneManager.GetActiveScene().path);
        if(SceneManager.GetActiveScene().path!=ColonizationWeek8Setup.MapPath) EditorSceneManager.OpenScene(ColonizationWeek8Setup.MapPath);
        Settings.ResetForNewGame();
        SessionState.SetBool(Key+".Regression",true); SessionState.SetFloat(Key+".RegressionStarted",(float)EditorApplication.timeSinceStartup);
        File.WriteAllText(Result,"RUNNING\nWeek 5/6/7 behavioral regressions first.\n");
        ColonizationWeek7SmokeTest.Run();
    }
    private static void Preflight()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("Save scenes and use Edit Mode before Week 8 testing.");
        ColonizationWeek8Setup.ValidateAssets();
    }
    private static void Tick()
    {
        // Coroutines stop on a script reload. Keep an Editor watchdog so a
        // reimport or stalled Game View cannot leave an unattended test in Play.
        if(SessionState.GetBool(Key+".Pending",false) && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPaused)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(host==null && EditorApplication.timeSinceStartup-initialized>5) { Fail(new InvalidOperationException("Script reload interrupted the Week 8 test. Rerun in stable Edit Mode.")); return; }
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup)>45) { Fail(new TimeoutException("Week 8 player loop stopped at phase "+SessionState.GetInt(Key+".Phase",0))); return; }
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) stableSince=EditorApplication.timeSinceStartup;
        if(File.Exists(Request) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !File.Exists(ColonizationWeek8Setup.Request) && !SessionState.GetBool(Key+".Restore",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string mode=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try
            {
                Preflight();
                if(mode=="flow") { SessionState.SetString(Key+".OriginalScene",SceneManager.GetActiveScene().path); SessionState.SetBool(Key+".Start",true); }
                else Run();
            }
            catch(Exception e) { Fail(e); }
        }
        if(SessionState.GetBool(Key+".Regression",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string result=File.ReadAllText("Temp/ColonizationWeek7SmokeTest.result.txt");
            if(result.StartsWith("FAILED") || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".RegressionStarted",0)>900) Fail(new InvalidOperationException("Week 7 regression:\n"+result));
            else if(result.StartsWith("PASSED")) { SessionState.SetBool(Key+".Regression",false); SessionState.SetBool(Key+".Start",true); }
        }
        if(SessionState.GetBool(Key+".Start",false) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !SessionState.GetBool(Key+".Restore",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && EditorApplication.timeSinceStartup-stableSince>10)
        {
            SessionState.SetBool(Key+".Start",false);
            try
            {
                Preflight();
                if(string.IsNullOrEmpty(SessionState.GetString(Key+".OriginalScene",""))) SessionState.SetString(Key+".OriginalScene",SceneManager.GetActiveScene().path);
                EditorSceneManager.OpenScene(ColonizationWeek8Setup.MainPath);
                if(!Application.isBatchMode) { Type gameView=typeof(Editor).Assembly.GetType("UnityEditor.GameView"); if(gameView!=null) { EditorWindow window=EditorWindow.GetWindow(gameView); window.Show(); window.Focus(); } }
                SessionState.SetBool(Key+".Pending",true); SessionState.SetBool(Key+".ExitExpected",false);
                SessionState.SetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(Key+".Phase",0);
                File.WriteAllText(Result,"RUNNING\nMainMenu, Back, exclusive nation selection and five full new-game paths.\n");
                EditorApplication.isPlaying=true;
            }
            catch(Exception e) { Fail(e); }
        }
        if(SessionState.GetBool(Key+".Restore",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && EditorApplication.timeSinceStartup-stableSince>3)
        {
            SessionState.SetBool(Key+".Restore",false);
            string original=SessionState.GetString(Key+".OriginalScene",ColonizationWeek8Setup.MapPath);
            if(!string.IsNullOrEmpty(original) && File.Exists(original) && !SceneManager.GetActiveScene().isDirty) EditorSceneManager.OpenScene(original);
        }
    }
    private static void PlayChanged(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode) RestoreBackground();
        if(!SessionState.GetBool(Key+".Pending",false)) return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            phase=frames=scenario=0; error=null; phaseStarted=started=EditorApplication.timeSinceStartup;
            SessionState.SetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup);
            if(!SessionState.GetBool(Key+".BackgroundSaved",false))
            { SessionState.SetBool(Key+".OldRunBackground",Application.runInBackground); SessionState.SetBool(Key+".BackgroundSaved",true); }
            Application.runInBackground=true;
            host=new GameObject("Week8SmokeHost",typeof(RectTransform),typeof(UnityEngine.UI.LayoutElement)); Object.DontDestroyOnLoad(host);
            host.GetComponent<UnityEngine.UI.LayoutElement>().StartCoroutine(RuntimeLoop());
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            if(SessionState.GetBool(Key+".ExitExpected",false))
                File.WriteAllText(Result,"PASSED\nWeek 5/6/7 behavioral regression suites passed first.\nNative MainMenu/SelectNation scene references and build order; existing Art Deco sprite crops/borders; 1920x1080 reference layout; disabled Load Game/Option placeholders; GraphicRaycaster/EventSystem New Game, Back, five exclusive nation cards, Start Game and EXIT.\nNew Game resets to England; false toggle notifications and invalid IDs cannot overwrite the choice; leader/flag/name mapping matches all five nations; all five complete menu-to-Map01 paths select the correct player ship, passengers, shields and camera, with fresh map/faction state on every game. Invalid map selection falls back to England.\nEXIT returned to Edit Mode. Screenshots: Temp/ColonizationWeek8MainMenu.png, Temp/ColonizationWeek8SelectNation.png, Temp/ColonizationWeek8PortugalMap.png. Existing faction shields temporarily replace missing leader portraits.\n");
            else if(!File.Exists(Result) || !File.ReadAllText(Result).StartsWith("FAILED")) File.WriteAllText(Result,"FAILED\nPlay Mode exited before completion.\n");
            SessionState.SetBool(Key+".Pending",false); SessionState.SetBool(Key+".Restore",true); stableSince=EditorApplication.timeSinceStartup;
        }
    }
    private static IEnumerator RuntimeLoop()
    {
        while(SessionState.GetBool(Key+".Pending",false) && Application.isPlaying)
        {
            yield return null;
            if(EditorApplication.isPaused) continue;
            try { if(error!=null) throw new InvalidOperationException(error); if(EditorApplication.timeSinceStartup-started>240) throw new TimeoutException("Week 8 phase "+phase); Step(); }
            catch(Exception e) { Fail(e); yield break; }
        }
    }
    private static void Step()
    {
        if(ColonizationLegacyDialogFixture.AcknowledgeNotice()) return;
        // Toggle's default 0.1-second fade needs elapsed time, not just eight
        // frames on a fast machine, before checking/capturing its visible state.
        frames++; if(frames<8 || EditorApplication.timeSinceStartup-phaseStarted<.2) return;
        switch(phase)
        {
            case 0:
                Require(SceneManager.GetActiveScene().name=="MainMenu" && Object.FindAnyObjectByType<MainMenu>()!=null,"Main menu missing.");
                Require(!Find("LoadGameButton").GetComponent<UnityEngine.UI.Button>().interactable && !Find("OptionButton").GetComponent<UnityEngine.UI.Button>().interactable,"Unimplemented menus should be disabled.");
                CheckSize(Find("FramePanel"),710,900); CheckCanvas();
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek8MainMenu.png"); Next(1); break;
            case 1:
                Settings.playerNationId=99; Click(Find("NewGameButton")); Next(2); break;
            case 2:
                CheckSelection(0); CheckSize(Find("FramePanel"),1416,1080); CheckCanvas();
                SelectNation select=Object.FindAnyObjectByType<SelectNation>(); select.SelectEuropeNation(-1); select.SelectEuropeNation(5); CheckSelection(0);
                Click(select.NationToggles[4].gameObject); Next(3); break;
            case 3: CheckSelection(4); Click(Find("BackButton")); Next(4); break;
            case 4:
                Require(SceneManager.GetActiveScene().name=="MainMenu" && Object.FindAnyObjectByType<GameManager>()==null,"Back did not return to a clean menu.");
                Click(Find("NewGameButton")); Next(5); break;
            case 5:
                CheckSelection(0); Click(Object.FindAnyObjectByType<SelectNation>().NationToggles[SelectionOrder[scenario]].gameObject); Next(6); break;
            case 6:
                CheckSelection(SelectionOrder[scenario]);
                foreach(UnityEngine.UI.Toggle toggle in Object.FindAnyObjectByType<SelectNation>().NationToggles)
                    Require(Mathf.Abs(toggle.graphic.canvasRenderer.GetAlpha()-(toggle.isOn?1f:0f))<.01f,"Selection highlight did not finish fading.");
                // A false event from the old card must never change selection.
                Object.FindAnyObjectByType<SelectNation>().NationToggles[(SelectionOrder[scenario]+1)%5].onValueChanged.Invoke(false); CheckSelection(SelectionOrder[scenario]);
                if(scenario==0) ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek8SelectNation.png");
                Next(7); break;
            case 7: Click(Find("StartGameButton")); Next(Object.FindAnyObjectByType<SelectNation>().HasIntroConfiguration?70:8); break;
            case 70:
                SelectNation intro=Object.FindAnyObjectByType<SelectNation>(); Require(intro.IntroStep==0 && intro.IntroPanels[0].activeSelf,"King command introduction missing.");
                intro.SelectEuropeNation((SelectionOrder[scenario]+1)%5); CheckSelection(SelectionOrder[scenario]);
                if(scenario==0) ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13KingsCommand.png");
                Next(700); break;
            case 700:
                SelectNation kingStep=Object.FindAnyObjectByType<SelectNation>();
                Click(kingStep.IntroPanels[0].transform.Find("Button").gameObject); Next(71); break;
            case 71:
                SelectNation prepare=Object.FindAnyObjectByType<SelectNation>(); Require(prepare.IntroStep==1 && !prepare.IntroPanels[0].activeSelf && prepare.IntroPanels[1].activeSelf,"Prepare-to-leave introduction missing.");
                if(scenario==0) ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13PrepareToLeave.png");
                Next(710); break;
            case 710:
                Click(Object.FindAnyObjectByType<SelectNation>().IntroPanels[1].transform.Find("Button").gameObject); Next(72); break;
            case 72:
                SelectNation departure=Object.FindAnyObjectByType<SelectNation>(); Require(departure.IntroStep==2 && !departure.IntroPanels[1].activeSelf && departure.IntroPanels[2].activeSelf,"Leaving-port introduction missing.");
                if(scenario==0) ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13LeftThePort.png");
                Next(720); break;
            case 720:
                Click(Object.FindAnyObjectByType<SelectNation>().IntroPanels[2].transform.Find("Button").gameObject); Next(8); break;
            case 8:
                Require(SceneManager.GetActiveScene().name=="Map01","Start Game did not load Map01.");
                GameManager manager=GameManager.instance; Require(manager!=null && manager.AllHexes[0,0]!=null,"Map initialization missing.");
                int id=SelectionOrder[scenario]; Require(manager.PlayerFaction==manager.Factions[id] && (int)manager.PlayerFaction.Nation==id && Settings.playerNationId==id,"Selected player faction mismatch.");
                Require(manager.Factions.Length==13 && manager.GameTurn==1 && manager.PlayerTurn && manager.PlayerFaction.Units.Count==3,"New game contains stale faction/turn state.");
                NavalUnit ship=manager.PlayerFaction.Units.OfType<NavalUnit>().Single();
                Require(ship.Passengers.Count==2 && manager.CurUnit==ship && ship.Passengers.All(p=>p.Faction==manager.PlayerFaction && p.TransportShip==ship && p.UnitStatus==UnitStatus.OnBoard),"Selected nation's ship/passengers mismatch.");
                SpriteRenderer flag=(SpriteRenderer)new SerializedObject(ship).FindProperty("flagSprite").objectReferenceValue;
                Require(flag.sprite==manager.PlayerFaction.ShieldIcon && Vector2.Distance(CameraController.instance.transform.position,ship.transform.position)<.01f,"Player shield/camera mismatch.");
                Require(Object.FindObjectsByType<GameManager>().Length==1 && Object.FindObjectsByType<EventSystem>().Length==1,"Scene transition leaked managers/EventSystems.");
                if(scenario==0) ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek8PortugalMap.png");
                Next(9); break;
            case 9:
                scenario++;
                if(scenario<SelectionOrder.Length) { SceneManager.LoadScene("MainMenu"); Next(4); }
                else
                {
                    GameManager current=GameManager.instance; Settings.playerNationId=-1; current.SelectPlayerFaction(); Require(current.PlayerFaction==current.Factions[0],"Negative faction selection guard.");
                    Settings.playerNationId=5; current.SelectPlayerFaction(); Require(current.PlayerFaction==current.Factions[0] && Settings.playerNationId==0,"Native/out-of-range selection guard.");
                    SceneManager.LoadScene("MainMenu"); Next(10);
                }
                break;
            case 10:
                Require(new[]{"MainMenu","SelectNation","PortugalMap"}.All(n=>File.Exists("Temp/ColonizationWeek8"+n+".png")),"Screenshots not written.");
                SessionState.SetBool(Key+".ExitExpected",true); Click(Find("ExitButton")); Next(11); break;
            case 11: if(frames>120) throw new InvalidOperationException("EXIT did not stop Editor Play Mode."); break;
        }
    }
    private static void CheckSelection(int expected)
    {
        SelectNation screen=Object.FindAnyObjectByType<SelectNation>(); Require(screen!=null,"SelectNation missing.");
        Require(screen.Index==expected && Settings.playerNationId==expected && screen.NationToggles.Length==5 && screen.NationToggles.Count(t=>t.isOn)==1 && screen.NationToggles[expected].isOn,"Exclusive nation selection failed for "+expected);
        string[] names={"England","France","Spain","Netherland","Portugal"};
        for(int i=0;i<5;i++)
        {
            Transform card=screen.NationToggles[i].transform; FactionData faction=AssetDatabase.LoadAssetAtPath<FactionData>("Assets/Scripts/FactionsSO/"+names[i]+".asset");
            Require(card.GetComponent<NationCard>().NationIndex==i && card.Find("NationText(TMP)").GetComponent<TMP_Text>().text==names[i] && card.Find("FlagImage").GetComponent<UnityEngine.UI.Image>().sprite==faction.flagIcon && card.Find("LeaderImage").GetComponent<UnityEngine.UI.Image>().sprite==(faction.leaderSprite!=null?faction.leaderSprite:faction.shieldIcon),"Card artwork/name binding mismatch for "+names[i]);
            if(i==expected) Require(screen.LeaderImage.sprite==(faction.leaderSprite!=null?faction.leaderSprite:faction.shieldIcon),"Large leader portrait not updated.");
        }
        UnityEngine.UI.ScrollRect scroll=Object.FindAnyObjectByType<UnityEngine.UI.ScrollRect>(); Require(scroll!=null && scroll.viewport!=null && scroll.content.childCount==5 && !scroll.horizontal && scroll.GetComponent<UnityEngine.UI.ToggleGroup>()!=null,"Nation ScrollRect incomplete.");
    }
    private static void CheckCanvas() { Canvas canvas=Object.FindAnyObjectByType<Canvas>(); Require(canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution==new Vector2(1920,1080),"Canvas reference resolution changed."); }
    private static void CheckSize(GameObject obj,float x,float y) { Require(((RectTransform)obj.transform).sizeDelta==new Vector2(x,y),"Manual dimensions mismatch: "+obj.name); }
    private static GameObject Find(string name) { return Object.FindObjectsByType<Transform>().FirstOrDefault(t=>t.name==name)?.gameObject??throw new InvalidOperationException("Missing object "+name); }
    private static void Click(GameObject target)
    {
        Canvas.ForceUpdateCanvases(); Vector2 point=target.transform.position;
        PointerEventData data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left}; List<RaycastResult> hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        GameObject top=hits.FirstOrDefault(h=>h.module is UnityEngine.UI.GraphicRaycaster).gameObject;
        GameObject handler=top!=null?ExecuteEvents.GetEventHandler<IPointerClickHandler>(top):null;
        Require(handler==target,"UI raycast missed "+target.name+"; top="+(top!=null?top.name:"none")+"; screen="+Screen.width+"x"+Screen.height+"; position="+point);
        ExecuteEvents.Execute(handler,data,ExecuteEvents.pointerClickHandler);
    }
    private static void Next(int next) { phase=next; frames=0; phaseStarted=EditorApplication.timeSinceStartup; SessionState.SetFloat(Key+".LastProgress",(float)phaseStarted); SessionState.SetInt(Key+".Phase",phase); File.WriteAllText(Result,"RUNNING\nWeek 8 phase "+phase+"; nation scenario "+scenario+"/5.\n"); }
    private static void Fail(Exception exception)
    {
        File.WriteAllText(Result,"FAILED\nPhase "+phase+", scenario "+scenario+"\n"+exception);
        SessionState.SetBool(Key+".Regression",false); SessionState.SetBool(Key+".Start",false); SessionState.SetBool(Key+".ExitExpected",false);
        if(EditorApplication.isPlaying) { ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek8Failure.png"); EditorApplication.isPlaying=false; }
        else { RestoreBackground(); SessionState.SetBool(Key+".Pending",false); SessionState.SetBool(Key+".Restore",true); }
    }
    private static void RestoreBackground()
    {
        if(!SessionState.GetBool(Key+".BackgroundSaved",false)) return;
        Application.runInBackground=SessionState.GetBool(Key+".OldRunBackground",false); SessionState.SetBool(Key+".BackgroundSaved",false);
    }
    private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
}
