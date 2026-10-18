using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ColonizationWeek7SmokeTest
{
    private const string Request="Temp/ColonizationWeek7SmokeTest.v8.request";
    private const string Result="Temp/ColonizationWeek7SmokeTest.result.txt";
    private const string Pending="Colonization.Week7Smoke.Pending";
    private const string Regression="Colonization.Week7Smoke.Regression";
    private static bool ready;
    private static double started,stableSince;
    private static int phase,frames,lastFrame,choice;
    private static string error,failure;
    private static Keyboard keyboard;
    private static GameManager manager;
    private static UIManager ui;
    private static LandUnit pioneer,soldier;
    private static Town town,cornerTown;
    private static TerrainSlot field,forest,ocean;
    private static readonly int[] FieldYields={5,4,3,3,5,5,4,2};

    [InitializeOnLoadMethod] private static void Initialize()
    {
        stableSince=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=PlayChanged;
        Application.logMessageReceived+=(message,stack,type)=> { if(SessionState.GetBool(Pending,false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) error=message+"\n"+stack; };
    }
    [MenuItem("Tools/Colonization/Run Week 7 Play Mode Smoke Test")]
    public static void Run()
    {
        Preflight();
        SessionState.SetBool(Regression,true); SessionState.SetFloat(Regression+".Started",(float)EditorApplication.timeSinceStartup);
        File.WriteAllText(Result,"RUNNING\nWeek 5/6 regression suites first.\n");
        ColonizationWeek6SmokeTest.RunBase();
    }
    private static void Preflight()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!="Assets/Scenes/Map01.unity" || SceneManager.sceneCount!=1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode. Active="+scene.path+"; dirty="+scene.isDirty+"; count="+SceneManager.sceneCount+"; playingOrChanging="+EditorApplication.isPlayingOrWillChangePlaymode);
        ColonizationWeek7Setup.Validate(Object.FindAnyObjectByType<GameManager>(),Object.FindAnyObjectByType<UIManager>());
    }
    private static void PlayChanged(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Pending,false)) return;
        if(state==PlayModeStateChange.EnteredPlayMode) { SessionState.SetBool(Pending+".Entered",true); BeginRuntime(); }
        if(state==PlayModeStateChange.EnteredEditMode) { RestoreInput(); SessionState.SetBool(Pending,false); File.WriteAllText(Result,"FAILED\nPlay Mode exited before Week 7 completed.\n"); }
    }
    private static void Tick()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) stableSince=EditorApplication.timeSinceStartup;
        if(!ready && SessionState.GetBool(Pending,false) && SessionState.GetBool(Pending+".Entered",false) && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            if(SessionState.GetInt(Pending+".Phase",0)==0 && GameManager.instance!=null) BeginRuntime();
            else { Finish("FAILED\nScript reload interrupted Week 7. Rerun the menu in Edit Mode.\n"); return; }
        }
        if(File.Exists(Request) && !File.Exists(ColonizationWeek7Setup.RequestPath) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string mode=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try
            {
                if(mode=="extend") { Preflight(); SessionState.SetBool(Pending+".StartRequested",true); }
                else Run();
            }
            catch(Exception e) { SessionState.SetBool(Regression,false); File.WriteAllText(Result,"FAILED\n"+e); }
        }
        if(SessionState.GetBool(Regression,false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string result=File.ReadAllText("Temp/ColonizationWeek6SmokeTest.result.txt");
            if(result.StartsWith("FAILED") || EditorApplication.timeSinceStartup-SessionState.GetFloat(Regression+".Started",(float)EditorApplication.timeSinceStartup)>600)
            { SessionState.SetBool(Regression,false); File.WriteAllText(Result,"FAILED\nWeek 6 regression:\n"+result); return; }
            if(result.StartsWith("PASSED")) { SessionState.SetBool(Regression,false); SessionState.SetBool(Pending+".StartRequested",true); }
        }
        if(SessionState.GetBool(Pending+".StartRequested",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && EditorApplication.timeSinceStartup-stableSince>=5)
        { SessionState.SetBool(Pending+".StartRequested",false); StartExtended(); }
    }
    private static IEnumerator RuntimeLoop()
    {
        // Screen and overlay-Canvas coordinates must be sampled in the game's
        // player loop, rather than an Editor update with a different view context.
        while(ready && SessionState.GetBool(Pending,false) && Application.isPlaying)
        {
            yield return null;
            RuntimeStep();
        }
    }
    private static void RuntimeStep()
    {
        if(!ready || !SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.timeSinceStartup-started<2) return;
        if(phase==99)
        {
            if(lastFrame==Time.frameCount) return; lastFrame=Time.frameCount; frames++;
            if(frames>=4) Finish(failure);
            return;
        }
        try
        {
            if(error!=null) throw new InvalidOperationException(error);
            if(EditorApplication.timeSinceStartup-started>240) throw new TimeoutException("Week 7 phase "+phase);
            if(lastFrame==Time.frameCount) return; lastFrame=Time.frameCount; frames++;
            if(ColonizationLegacyDialogFixture.AcknowledgeNotice()) return;
            switch(phase)
            {
                case 0:
                    manager=GameManager.instance; ui=UIManager.instance; ColonizationWeek7Setup.Validate(manager,ui); TestRules();
                    keyboard=InputSystem.AddDevice<Keyboard>(); SessionState.SetInt(Pending+".Keyboard",keyboard.deviceId);
                    pioneer=(LandUnit)manager.PlayerFaction.Units[2]; soldier=(LandUnit)manager.PlayerFaction.Units[1];
                    Hex center=manager.AllHexes.Cast<Hex>().First(h=>!h.HasTown && h.HexType!=HexType.Ocean && h.HexType!=HexType.Hills && h.HexType!=HexType.Mountains &&
                        HexCalculator.GetHexAround(manager.AllHexes,h).Count==6 && HexCalculator.GetHexAround(manager.AllHexes,h).Any(n=>!n.HasTown && n.HexType==HexType.Ocean) && HexCalculator.GetHexAround(manager.AllHexes,h).Count(n=>!n.HasTown && n.HexType!=HexType.Ocean)>=2);
                    center.ClearForest(); center.ResourceYield=new[]{5,4,3,3,3,3,0,0};
                    manager.GenerateTown(manager.PlayerFaction,center); town=center.Town;
                    Require(town.Warehouse.Length==16 && town.Warehouse.All(n=>n==0) && town.TotalYieldThisTurn.Length==16 && town.CrossNum==0 && town.BellNum==0,"Town storage initialization failed.");
                    Relocate(pioneer,center); Relocate(soldier,center); manager.SelectPlayerUnit(pioneer); Keys(Key.Enter); Next(); break;
                case 1:
                    if(frames<4) break;
                    Require(ui.IsTownPanelOpen && ui.AllUnitDrags.Count==2 && town.TotalYieldThisTurn[0]==3 && ui.FoodIconList.Count==3,"Automatic center Food failed."); Keys();
                    field=ui.AreaSlots.First(s=>s.Hex!=null && !s.Hex.HasTown && s.Hex.HexType!=HexType.Ocean);
                    forest=ui.AreaSlots.First(s=>s!=field && s.Hex!=null && !s.Hex.HasTown && s.Hex.HexType!=HexType.Ocean);
                    ocean=ui.AreaSlots.First(s=>s.Hex!=null && !s.Hex.HasTown && s.Hex.HexType==HexType.Ocean);
                    field.Hex.HasForest=false; field.Hex.ResourceYield=(int[])FieldYields.Clone();
                    forest.Hex.HasForest=true; forest.Hex.ResourceYield=(int[])FieldYields.Clone();
                    ocean.Hex.HasForest=false; ocean.Hex.ResourceYield=new[]{4,0,0,0,0,0,0,0};
                    ui.SetupHexSlots(centerHex:town.CurHex,aroundHexes:HexCalculator.GetHexAroundToArray(manager.AllHexes,town.CurHex)); ui.RefreshTownYield();
                    Drag(Icon(pioneer),field.gameObject);
                    Require(ui.IsProfessionPanelOpen && ui.CurrentSlot==field && ui.BlockImage.activeSelf && field.Hex.YieldID==-1 && field.YieldIconList.Count==0 && town.TotalYieldThisTurn[0]==3,"Drop did not open an uncommitted profession."); Next(); break;
                case 2:
                    if(frames==4) { Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek7Profession.png"); }
                    if(frames<8) break;
                    for(int i=0;i<8;i++) Require(Button(i).transform.Find("YieldText").GetComponent<TMP_Text>().text==field.NormalYield[i]+" "+manager.ProductData[i].productName,"Profession yield label incorrect.");
                    Require(Button(0).transform.Find("Image").gameObject.activeSelf && !Button(0).transform.Find("FishImage").gameObject.activeSelf,"Land Food icon incorrect.");
                    Require(Top(ui.OutsideTownParent.transform.position)==ui.BlockImage,"Modal does not block town interactions.");
                    UnitDrag idle=Icon(soldier); ExecuteEvents.Execute(idle.gameObject,Data(idle.gameObject),ExecuteEvents.beginDragHandler); Require(!idle.IsDragging,"Drag accepted behind modal.");
                    ui.SelectYield(-1); ui.SelectYield(8); Require(ui.IsProfessionPanelOpen && field.Hex.YieldID==-1,"Invalid profession accepted.");
                    Choose(0); ExpectTotals(0,5); Require(ui.FoodText.text=="8" && ui.FoodIconList.Count==8,"Total Food is not center plus worker."); VerifyIcons(field,0,5);
                    choice=1; RightClick(Icon(pioneer).gameObject); Next(); break;
                case 3:
                    if(frames<4) break;
                    Choose(choice); ExpectTotals(choice,field.NormalYield[choice]); VerifyIcons(field,choice,field.NormalYield[choice]);
                    if(choice<7) { choice++; RightClick(Icon(pioneer).gameObject); frames=0; }
                    else { Drag(Icon(pioneer),ocean.gameObject); Next(); } break;
                case 4:
                    if(frames<4) break;
                    Require(!Button(0).transform.Find("Image").gameObject.activeSelf && Button(0).transform.Find("FishImage").gameObject.activeSelf,"Ocean Food choice did not switch to fish.");
                    Choose(0); ExpectTotals(0,4); VerifyIcons(ocean,0,4); Require(ui.FoodIconList.Count==7,"Ocean Food total incorrect.");
                    Drag(Icon(soldier),forest.gameObject); Next(); break;
                case 5:
                    if(frames<4) break;
                    Require(forest.NormalYield[0]==3 && forest.NormalYield[4]==5 && forest.NormalYield[5]==5 && forest.NormalYield[6]==2,"Forest yields incorrect.");
                    Choose(5); Require(town.TotalYieldThisTurn[0]==7 && town.TotalYieldThisTurn[5]==5,"Two-worker totals incorrect."); VerifyIcons(forest,5,5);
                    Drag(Icon(pioneer),field.gameObject); Require(town.TotalYieldThisTurn[0]==3 && ocean.YieldIconList.Count==0 && ocean.Hex.Labor==null,"Moving worker left old fish yield."); Next(); break;
                case 6:
                    if(frames<4) break;
                    Choose(0); Require(town.TotalYieldThisTurn[0]==8 && town.TotalYieldThisTurn[5]==5,"Reassignment did not replace yield.");
                    Drag(Icon(pioneer),forest.gameObject); Require(forest.Hex.Labor==soldier && field.Hex.Labor==pioneer && !ui.IsProfessionPanelOpen && town.TotalYieldThisTurn[0]==8,"Occupied drop altered worker/yield.");
                    Drag(Icon(pioneer),ui.CenterSlot.gameObject); Require(field.Hex.Labor==pioneer && !ui.IsProfessionPanelOpen,"Center accepted worker.");
                    UnitDrag cancel=Icon(soldier); PointerEventData cancelled=Data(cancel.gameObject); ExecuteEvents.Execute(cancel.gameObject,cancelled,ExecuteEvents.beginDragHandler); ExecuteEvents.Execute(cancel.gameObject,cancelled,ExecuteEvents.endDragHandler);
                    Require(forest.Hex.Labor==soldier && forest.Hex.YieldID==5 && town.TotalYieldThisTurn[5]==5,"Cancelled drag lost yield.");
                    Drag(Icon(pioneer),ui.OutsideTownParent); Require(field.Hex.Labor==null && field.Hex.YieldID==-1 && town.TotalYieldThisTurn[0]==3 && ui.FoodIconList.Count==3,"Outside drop did not remove Food.");
                    Require(ui.FoodIconList[0].transform.parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing==0,"Food spacing was not reset after shrinking.");
                    Drag(Icon(pioneer),field.gameObject); Next(); break;
                case 7:
                    if(frames<4) break; Keys(Key.Escape); Next(); break;
                case 8:
                    if(frames<4) break;
                    Require(!ui.IsProfessionPanelOpen && !ui.BlockImage.activeSelf && ui.IsTownPanelOpen && field.Hex.Labor==null && pioneer.UnitStatus==UnitStatus.None && Icon(pioneer).transform.parent==ui.OutsideTownParent.transform,"Escape did not cancel pending assignment.");
                    Keys(); Next(); break;
                case 9:
                    if(frames<4) break; RightClick(Icon(soldier).gameObject); Next(); break;
                case 10:
                    if(frames<4) break; Keys(Key.Escape); Next(); break;
                case 11:
                    if(frames<4) break;
                    Require(!ui.IsProfessionPanelOpen && forest.Hex.Labor==soldier && forest.Hex.YieldID==5 && town.TotalYieldThisTurn[5]==5,"Cancelling a profession change lost existing work."); Keys(); Next(); break;
                case 12:
                    if(frames==4) { Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek7TownYield.png"); }
                    if(frames<8) break;
                    ui.ToggleTownPanel(false);
                    Require(!soldier.gameObject.activeSelf && pioneer.gameObject.activeSelf && ui.FoodIconList.Count==0 && ui.CenterSlot.YieldIconList.Count==0 && ui.AreaSlots.All(s=>s.YieldIconList.Count==0) && town.TotalYieldThisTurn[0]==3 && town.TotalYieldThisTurn[5]==5,"Closing altered forecast or leaked icons.");
                    manager.SelectPlayerUnit(pioneer); Keys(Key.Enter); Next(); break;
                case 13:
                    if(frames<4) break; Keys();
                    for(int i=0;i<3;i++)
                    {
                        Require(ui.IsTownPanelOpen && ui.AllUnitDrags.Count==2 && town.TotalYieldThisTurn[0]==3 && town.TotalYieldThisTurn[5]==5 && ui.FoodIconList.Count==3 && forest.YieldIconList.Count==5,"Reopen duplicated yield/icons.");
                        ui.ToggleTownPanel(false); manager.SetupCurrentTown(town);
                    }
                    ui.ToggleTownPanel(false);
                    Hex corner=manager.AllHexes[0,0]; if(corner.HasTown) corner=manager.AllHexes[0,59]; Require(!corner.HasTown,"Both corner fixtures occupied.");
                    manager.GenerateTown(manager.PlayerFaction,corner); cornerTown=corner.Town; manager.SetupCurrentTown(cornerTown); Next(); break;
                case 14:
                    if(frames<4) break;
                    Hex[] neighbors=HexCalculator.GetHexAroundToArray(manager.AllHexes,cornerTown.CurHex);
                    Require(neighbors.Any(h=>h==null) && ui.AllUnitDrags.Count==0 && cornerTown.TotalYieldThisTurn[0]==TerrainYield.Calculate(cornerTown.CurHex,0),"Corner town leaked previous yield.");
                    for(int i=0;i<6;i++) Require(ui.AreaSlots[i].Hex==neighbors[i] && (neighbors[i]!=null || (ui.AreaSlots[i].NormalYield.All(n=>n==0) && ui.AreaSlots[i].YieldIconList.Count==0 && !ui.AreaSlots[i].gameObject.activeSelf)),"Null slot retained old yield.");
                    ui.ToggleTownPanel(false); manager.SetupCurrentTown(town); Next(); break;
                case 15:
                    if(frames<4) break;
                    Require(town.TotalYieldThisTurn[0]==3 && town.TotalYieldThisTurn[5]==5 && Icon(soldier).TerrainSlot.Hex.Labor==soldier,"Different city corrupted worker forecast.");
                    Drag(Icon(soldier),ui.OutsideTownParent); Require(town.TotalYieldThisTurn[5]==0 && town.TotalYieldThisTurn[0]==3 && town.Warehouse.All(n=>n==0),"Removing Lumber failed or preview mutated warehouse.");
                    // Reparenting a Graphic updates its rendered depth on the
                    // next Canvas frame; a later physical click cannot occur in
                    // the same drop callback. Wait before testing the idle icon.
                    Next(); break;
                case 16:
                    if(frames<4) break;
                    RightClick(Icon(soldier).gameObject); Require(!ui.IsProfessionPanelOpen,"Idle unit opened profession picker.");
                    Click(ui.TownPanel.transform.Find("NamePanel/ExitButton").gameObject);
                    Require(!ui.IsTownPanelOpen && !ui.IsProfessionPanelOpen && ui.AllUnitDrags.Count==0 && soldier.gameObject.activeSelf && ui.FoodIconList.Count==0,"Header EXIT cleanup failed.");
                    Require(File.Exists("Temp/ColonizationWeek7Profession.png") && File.Exists("Temp/ColonizationWeek7TownYield.png"),"Week 7 screenshots missing.");
                    Finish("PASSED\nWeek 5/6 behavioral regression passed first.\n16 ProductData entries, all 11 eight-resource terrain configurations, Food corn/fish variants, terrain/forest/town rounding and nonnegative rules.\nAuto center Food, actual GraphicRaycaster drop and all eight persistent profession buttons, label/icon correctness, modal blocking and invalid-ID guards, zero-yield professions, Food/other-product replacement without double counts, two-worker totals, occupied/center/cancelled drop guards, return outside, spacing reset, Escape cancellation, worker/yield persistence across repeated reopen and switching cities, null neighbors, header EXIT and visual cleanup.\nWarehouse remains unchanged by production previews; turn-based harvest is outside Week 7.\nScreenshots: Temp/ColonizationWeek7Profession.png and Temp/ColonizationWeek7TownYield.png\n"); break;
            }
        }
        catch(Exception e)
        {
            failure="FAILED\nPhase "+phase+": "+e;
            if(ui!=null && ui.IsTownPanelOpen)
            { ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek7Failure.png"); phase=99; frames=0; File.WriteAllText(Result,"RUNNING\nCapturing failure diagnostics.\n"); }
            else Finish(failure);
        }
    }
    private static void StartExtended()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup-stableSince<5) { SessionState.SetBool(Pending+".StartRequested",true); return; }
        SessionState.SetBool(Pending,true); SessionState.SetBool(Pending+".Entered",false); SessionState.SetInt(Pending+".Phase",0); ready=false;
        File.WriteAllText(Result,"RUNNING\nEntering Week 7 Play Mode.\n"); EditorApplication.isPlaying=true;
    }
    private static void BeginRuntime()
    {
        if(EventSystem.current==null) return;
        ready=true; phase=frames=0; lastFrame=-1; error=null; started=EditorApplication.timeSinceStartup;
        if(!SessionState.GetBool(Pending+".InputSaved",false))
        {
            SessionState.SetInt(Pending+".OldInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode); SessionState.SetInt(Pending+".OldBackground",(int)InputSystem.settings.backgroundBehavior); SessionState.SetBool(Pending+".OldRunBackground",Application.runInBackground); SessionState.SetBool(Pending+".InputSaved",true);
        }
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; Application.runInBackground=true;
        File.WriteAllText(Result,"RUNNING\nWeek 7 entered Play Mode.\n");
        EventSystem.current.StartCoroutine(RuntimeLoop());
    }
    private static void RestoreInput()
    {
        InputDevice device=InputSystem.GetDeviceById(SessionState.GetInt(Pending+".Keyboard",-1)); if(device!=null) InputSystem.RemoveDevice(device); SessionState.SetInt(Pending+".Keyboard",-1);
        if(!SessionState.GetBool(Pending+".InputSaved",false)) return;
        InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Pending+".OldInput",0); InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Pending+".OldBackground",0); Application.runInBackground=SessionState.GetBool(Pending+".OldRunBackground",false); SessionState.SetBool(Pending+".InputSaved",false);
    }
    private static void Finish(string result) { if(ui!=null && ui.IsTownPanelOpen) ui.ToggleTownPanel(false); RestoreInput(); SessionState.SetBool(Pending,false); File.WriteAllText(Result,result); ready=false; EditorApplication.isPlaying=false; }
    private static void Next() { phase++; frames=0; SessionState.SetInt(Pending+".Phase",phase); File.WriteAllText(Result,"RUNNING\nWeek 7 phase "+phase+".\n"); }
    private static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
    private static void Relocate(LandUnit unit,Hex hex)
    {
        if(unit.TransportShip!=null) unit.TransportShip.Passengers.Remove(unit);
        SerializedObject serialized=new SerializedObject(unit); serialized.FindProperty("transportShip").objectReferenceValue=null; serialized.ApplyModifiedPropertiesWithoutUndo();
        unit.transform.SetParent(unit.Faction.UnitParent,true); unit.UnitStatus=UnitStatus.None; unit.MovePoint=3; unit.SetupPosition(hex); unit.gameObject.SetActive(true);
    }
    private static UnitDrag Icon(LandUnit unit) { return ui.AllUnitDrags.Select(o=>o.GetComponent<UnitDrag>()).Single(d=>d.LandUnit==unit); }
    private static PointerEventData Data(GameObject obj) { return new PointerEventData(EventSystem.current){position=obj.transform.position,pointerDrag=obj,button=PointerEventData.InputButton.Left}; }
    private static GameObject Top(Vector2 position)
    {
        Canvas.ForceUpdateCanvases(); List<RaycastResult> hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
        Canvas canvas=ui.GetComponent<Canvas>();
        Require(hits.Count>0,"UI raycast missed; position="+position+"; screen="+Screen.width+"x"+Screen.height+"; canvasRect="+((RectTransform)canvas.transform).rect+"; canvasPosition="+canvas.transform.position+"; scale="+canvas.scaleFactor+"; targetDisplay="+canvas.targetDisplay+"; graphics="+string.Join(" | ",ui.GetComponentsInChildren<UnityEngine.UI.Graphic>().Where(g=>g.raycastTarget).Take(16).Select(g=>g.name+"/depth="+g.depth+"/cull="+g.canvasRenderer.cull)));
        return hits[0].gameObject;
    }
    private static void Drag(UnitDrag drag,GameObject target)
    {
        Canvas.ForceUpdateCanvases(); PointerEventData data=Data(drag.gameObject); ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.beginDragHandler); Require(drag.IsDragging,"Drag blocked unexpectedly.");
        data.position=target.transform.position; ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.dragHandler); GameObject top=Top(data.position); GameObject handler=ExecuteEvents.GetEventHandler<IDropHandler>(top); Require(handler==target,"Unexpected drop raycast: "+top.name+" for "+target.name);
        ExecuteEvents.Execute(handler,data,ExecuteEvents.dropHandler); ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.endDragHandler);
    }
    private static UnityEngine.UI.Button Button(int id) { return ui.ProfessionPanel.transform.Find("SelectPanel/Button"+id).GetComponent<UnityEngine.UI.Button>(); }
    private static void Choose(int id) { Require(ui.IsProfessionPanelOpen,"Profession modal missing."); Click(Button(id).gameObject); Require(!ui.IsProfessionPanelOpen && !ui.BlockImage.activeSelf,"Profession button did not close modal."); }
    private static void Click(GameObject obj)
    {
        Canvas.ForceUpdateCanvases();
        Vector2 position=obj.transform.position;
        List<RaycastResult> hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
        GameObject top=hits.Count>0?hits[0].gameObject:null; GameObject handler=top!=null?ExecuteEvents.GetEventHandler<IPointerClickHandler>(top):null;
        UnityEngine.UI.Graphic graphic=obj.GetComponent<UnityEngine.UI.Graphic>(); Canvas canvas=ui.GetComponent<Canvas>();
        Require(handler==obj,"Unexpected button raycast: "+(top!=null?top.name:"none")+" for "+obj.name+"; position="+position+"; screen="+Screen.width+"x"+Screen.height+"; canvasRect="+((RectTransform)canvas.transform).rect+"; scale="+canvas.scaleFactor+"; graphicDepth="+graphic.depth+"; cull="+graphic.canvasRenderer.cull+"; active="+graphic.isActiveAndEnabled+"; modalActive="+ui.IsProfessionPanelOpen+"; hits="+string.Join(" | ",hits.Select(h=>h.gameObject.name+":"+h.module.GetType().Name+"/priority="+h.module.sortOrderPriority+"/layer="+h.sortingLayer+"/order="+h.sortingOrder)));
        ExecuteEvents.Execute(handler,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=position},ExecuteEvents.pointerClickHandler);
    }
    private static void RightClick(GameObject obj)
    {
        Canvas.ForceUpdateCanvases();
        GameObject top=Top(obj.transform.position); GameObject handler=ExecuteEvents.GetEventHandler<IPointerClickHandler>(top); Require(handler==obj,"Worker right-click raycast missed: top="+top.name+"; target="+obj.name+"; position="+obj.transform.position); ExecuteEvents.Execute(handler,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Right,position=obj.transform.position},ExecuteEvents.pointerClickHandler);
    }
    private static void ExpectTotals(int id,int amount)
    {
        for(int i=0;i<16;i++) Require(town.TotalYieldThisTurn[i]==(i==0?3:0)+(i==id?amount:0),"Product "+i+" total drift after choosing "+id);
        Require(ui.FoodIconList.Count==town.TotalYieldThisTurn[0] && ui.FoodText.text==town.TotalYieldThisTurn[0].ToString(),"Food display drift.");
    }
    private static void VerifyIcons(TerrainSlot slot,int id,int amount)
    {
        Sprite expected=manager.ProductData[id].icons[id==0 && slot.Hex.HexType==HexType.Ocean?1:0];
        Require(slot.Hex.YieldID==id && slot.ActualYield[id]==amount && slot.YieldIconList.Count==amount && slot.YieldIconList.All(o=>o.GetComponent<UnityEngine.UI.Image>().sprite==expected),"Field amount/resource sprite incorrect.");
    }
    private static void TestRules()
    {
        GameObject obj=new GameObject("Week7RuleFixture"); Hex hex=obj.AddComponent<Hex>();
        try
        {
            hex.ResourceYield=(int[])FieldYields.Clone(); Require(TerrainYield.Calculate(hex,0)==5 && TerrainYield.Calculate(hex,4)==0 && TerrainYield.Calculate(hex,5)==0,"Clear terrain rules failed.");
            hex.HasForest=true; Require(TerrainYield.Calculate(hex,0)==3 && TerrainYield.Calculate(hex,4)==5 && TerrainYield.Calculate(hex,5)==5 && TerrainYield.Calculate(hex,6)==2,"Forest rules failed.");
            hex.HasTown=true; Require(TerrainYield.Calculate(hex,0)==2 && TerrainYield.Calculate(hex,4)==3 && TerrainYield.Calculate(hex,6)==1,"Town rounding rules failed.");
            hex.ResourceYield=new int[8]; Require(Enumerable.Range(0,8).All(i=>TerrainYield.Calculate(hex,i)==0) && TerrainYield.Calculate(hex,-1)==0 && TerrainYield.Calculate(hex,8)==0,"Negative/boundary yields failed.");
        }
        finally { Object.DestroyImmediate(obj); }
    }
    private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
}
