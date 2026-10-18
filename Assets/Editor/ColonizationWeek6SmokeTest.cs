using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// First reruns the Week 5 behavioral suite, then tests Week 6 in a fresh play
// session. Coast/town fixtures only relocate units onto existing generated Hexes.
public static class ColonizationWeek6SmokeTest
{
    private const string Request="Temp/ColonizationWeek6SmokeTest.v9.request";
    private const string Result="Temp/ColonizationWeek6SmokeTest.result.txt";
    private const string Pending="Colonization.Week6Smoke.Pending";
    private const string Regression="Colonization.Week6Smoke.Regression";
    private static Keyboard keyboard;
    private static bool runtimeReady;
    private static int phase,phaseFrame,lastFrame;
    private static double started;
    private static string error;
    private static double editorStableSince;
    private static NavalUnit ship;
    private static LandUnit pioneer,soldier;
    private static Hex land,sea,destination;
    private static UIManager ui;
    private static TerrainSlot first,second;
    private static Vector3 cameraPosition;
    private static float zoom;

    [InitializeOnLoadMethod] private static void Initialize()
    {
        editorStableSince=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=PlayModeChanged;
        Application.logMessageReceived+=(message,stack,type)=>
        { if(SessionState.GetBool(Pending,false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) error=message+"\n"+stack; };
    }
    [MenuItem("Tools/Colonization/Run Week 6 Play Mode Smoke Test")]
    public static void Run()
    {
        UIManager ui=Object.FindAnyObjectByType<UIManager>();
        if(ui!=null && ui.HasYieldConfiguration) { ColonizationWeek7SmokeTest.Run(); return; }
        RunBase();
    }
    internal static void RunBase()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!="Assets/Scenes/Map01.unity" || SceneManager.sceneCount!=1)
            throw new InvalidOperationException("Open saved Map01 alone in Edit Mode before the Week 6 smoke test.");
        ColonizationWeek6Setup.ValidateConfiguration(Object.FindAnyObjectByType<GameManager>(),Object.FindAnyObjectByType<UIManager>());
        File.WriteAllText(Result,"RUNNING\nWeek 5 regression suite first.\n");
        SessionState.SetFloat(Regression+".Started",(float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(Regression,true);
        SessionState.SetBool(Regression+".EditorReady",false);
        ColonizationWeek5SmokeTest.RunBase();
    }
    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Regression,false))
            SessionState.SetBool(Regression+".EditorReady",true);
        if(!SessionState.GetBool(Pending,false)) return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetBool(Pending+".Entered",true);
            BeginRuntime();
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        { SessionState.SetBool(Pending,false); File.WriteAllText(Result,"FAILED\nPlay Mode exited before Week 6 completion.\n"); }
    }
    private static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            editorStableSince=EditorApplication.timeSinceStartup;
        // A script reload can occur after EnteredPlayMode. Static harness state
        // then resets even though the Editor and generated game remain running.
        if(!runtimeReady && SessionState.GetBool(Pending,false) && SessionState.GetBool(Pending+".Entered",false) && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            if(SessionState.GetInt(Pending+".Phase",0)==0 && GameManager.instance!=null) BeginRuntime();
            else
            {
                RestoreInput();
                SessionState.SetBool(Pending,false);
                File.WriteAllText(Result,"FAILED\nScript reload interrupted Week 6; restarting in a fresh play session.\n");
                File.WriteAllText(Request,"extend");
                EditorApplication.isPlaying=false;
                return;
            }
        }
        if(File.Exists(Request) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !File.Exists(ColonizationWeek6Setup.RequestPath) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string mode=File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try
            {
                if(mode=="extend")
                {
                    Scene scene=SceneManager.GetActiveScene();
                    if(scene.path!="Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount!=1 || !File.ReadAllText("Temp/ColonizationWeek5SmokeTest.result.txt").StartsWith("PASSED"))
                        throw new InvalidOperationException("Extended rerun requires saved Map01 and a passed Week 5 baseline.");
                    SessionState.SetBool(Pending+".StartRequested",true);
                    return;
                }
                Run();
            }
            catch(Exception e) { SessionState.SetBool(Regression,false); File.WriteAllText(Result,"FAILED\n"+e); }
        }
        if(SessionState.GetBool(Regression,false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string regression=File.ReadAllText("Temp/ColonizationWeek5SmokeTest.result.txt");
            if(regression.StartsWith("FAILED") || (!regression.StartsWith("PASSED") && EditorApplication.timeSinceStartup-SessionState.GetFloat(Regression+".Started",(float)EditorApplication.timeSinceStartup)>240))
            { SessionState.SetBool(Regression,false); File.WriteAllText(Result,"FAILED\nWeek 5 regression:\n"+regression); return; }
            if(regression.StartsWith("PASSED") && SessionState.GetBool(Regression+".EditorReady",false))
            {
                SessionState.SetBool(Regression,false);
                SessionState.SetBool(Pending+".StartRequested",true);
                return;
            }
        }
        if(SessionState.GetBool(Pending+".StartRequested",false) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && EditorApplication.timeSinceStartup-editorStableSince>=5)
        { SessionState.SetBool(Pending+".StartRequested",false); StartExtended(); }
        if(SessionState.GetBool(Pending,false) && EditorApplication.isPlaying) EditorApplication.QueuePlayerLoopUpdate();
        if(!runtimeReady || !SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.timeSinceStartup-started<2) return;
        try
        {
            if(error!=null) throw new InvalidOperationException(error);
            if(EditorApplication.timeSinceStartup-started>180) throw new TimeoutException("Week 6 timeout at phase "+phase);
            if(lastFrame==Time.frameCount) return;
            lastFrame=Time.frameCount; phaseFrame++;
            if(ColonizationLegacyDialogFixture.AcknowledgeNotice()) return;
            GameManager manager=GameManager.instance;
            switch(phase)
            {
                case 0:
                    ui=UIManager.instance;
                    ColonizationWeek6Setup.ValidateConfiguration(manager,ui);
                    Require(!ui.IsTownPanelOpen,"Town panel must start hidden.");
                    foreach(Faction faction in manager.Factions.Skip(5))
                        foreach(Unit unit in faction.Units)
                            Require(((LandUnit)unit).LandUnitType==(faction.IsTropicalNatives?LandUnitType.TropicalIndians:LandUnitType.PlainIndians),"Native appearance configuration failed.");
                    Require(manager.Factions.Skip(5).Any(f=>f.IsTropicalNatives) && manager.Factions.Skip(5).Any(f=>!f.IsTropicalNatives),"Both Native variants required.");
                    ValidateOccupancy(manager);
                    keyboard=InputSystem.AddDevice<Keyboard>();
                    ship=(NavalUnit)manager.PlayerFaction.Units[0]; soldier=(LandUnit)manager.PlayerFaction.Units[1]; pioneer=(LandUnit)manager.PlayerFaction.Units[2];
                    land=manager.AllHexes.Cast<Hex>().First(h=>!h.HasTown && h.HexType!=HexType.Ocean && h.HexType!=HexType.Mountains && h.HexType!=HexType.Hills &&
                        HexCalculator.GetHexAround(manager.AllHexes,h).Count(n=>!n.HasTown)>=3 && HexCalculator.GetHexAround(manager.AllHexes,h).Any(n=>n.HexType==HexType.Ocean && n.UnitsInHex.Count==0 && HexCalculator.GetHexAround(manager.AllHexes,n).Any(s=>s.HexType==HexType.Ocean)));
                    land.ClearForest(); sea=HexCalculator.GetHexAround(manager.AllHexes,land).First(h=>h.HexType==HexType.Ocean && h.UnitsInHex.Count==0 && HexCalculator.GetHexAround(manager.AllHexes,h).Any(s=>s.HexType==HexType.Ocean));
                    PositionShip(ship,sea); manager.SelectPlayerUnit(pioneer); CameraController.instance.MoveCamera(land.Pos);
                    pioneer.PrepareMoveToHex(land); Require(pioneer.IsMoving,"Initial landfall fixture did not start."); Next(); break;
                case 1:
                    if(pioneer.IsMoving) break;
                    Require(land.UnitsInHex.Contains(pioneer) && !sea.UnitsInHex.Contains(pioneer),"Landfall did not update Hex membership.");
                    pioneer.MovePoint=3;
                    SetCargo(1); pioneer.PrepareMoveToHex(sea);
                    Require(!pioneer.IsMoving && pioneer.MovePoint==3 && pioneer.TransportShip==null,"Full ship accepted boarding."); SetCargo(2);
                    Hex emptyOcean=manager.AllHexes.Cast<Hex>().First(h=>h.HexType==HexType.Ocean && h.UnitsInHex.Count==0);
                    Require(manager.CheckIfHexHasOurShipToBoard(emptyOcean)==null,"Empty sea can be boarded.");
                    NavalUnit enemy=(NavalUnit)manager.Factions[1].Units[0];
                    Require(manager.CheckIfHexHasOurShipToBoard(enemy.CurHex)==null,"Enemy ship can be boarded.");
                    ClickWorld(ship.transform.position,PointerEventData.InputButton.Right,typeof(NavalUnit));
                    Require(pioneer.IsMoving && ship.HasBoardingReservation(pioneer) && !ship.HasBoardingSpace,"Boarding did not reserve last berth.");
                    destination=HexCalculator.GetHexAround(manager.AllHexes,sea).First(h=>h.HexType==HexType.Ocean);
                    ship.PrepareMoveToHex(destination); Require(!ship.IsMoving,"Ship departed during reserved boarding.");
                    LandUnit extra=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/LandUnit.prefab")).GetComponent<LandUnit>();
                    extra.UnitInit(manager,manager.PlayerFaction,manager.LandUnitData[0]); extra.SetupPosition(land);
                    extra.PrepareMoveToHex(sea); Require(!extra.IsMoving,"Concurrent boarders overbooked ship."); Object.DestroyImmediate(extra.gameObject);
                    Next(); break;
                case 2:
                    if(pioneer.IsMoving) break;
                    Require(pioneer.UnitStatus==UnitStatus.OnBoard && pioneer.TransportShip==ship && ship.Passengers.Count==2 && pioneer.transform.parent==ship.PassengerParent.transform && !ship.HasBoardingReservation(pioneer),"Boarding arrival/link/parent/capacity failed.");
                    Require(pioneer.MovePoint==2 && !land.UnitsInHex.Contains(pioneer),"Boarding cost/membership failed.");
                    manager.SelectPlayerUnit(ship); ship.MovePoint=4; ship.PrepareMoveToHex(destination); Require(ship.IsMoving,"Ship failed after boarding."); Next(); break;
                case 3:
                    if(ship.IsMoving) break;
                    foreach(LandUnit rider in ship.Passengers)
                        Require(rider.CurHex==destination && destination.UnitsInHex.Contains(rider) && !sea.UnitsInHex.Contains(rider) && rider.CurPos==destination.Pos,"Reboarded passenger did not follow ship.");
                    ValidateOccupancy(manager); PositionShip(ship,sea); pioneer.MovePoint=3;
                    manager.SelectPlayerUnit(pioneer); pioneer.PrepareMoveToHex(land); Require(pioneer.IsMoving,"Second landfall did not start."); Next(); break;
                case 4:
                    if(pioneer.IsMoving) break;
                    Require(ship.Passengers.Count==1 && pioneer.TransportShip==null,"Second landfall failed.");
                    manager.GenerateTown(manager.PlayerFaction,land);
                    Require(land.Town!=null && land.Town.CurHex==land && land.HasTown,"Town reference missing.");
                    manager.SelectPlayerUnit(soldier); soldier.PrepareMoveToHex(land); Require(soldier.IsMoving,"Second idle worker fixture failed."); Next(); break;
                case 5:
                    if(soldier.IsMoving) break;
                    manager.SelectPlayerUnit(pioneer); SetKeys(Key.Enter); Next(); break;
                case 6:
                    if(phaseFrame<4) break;
                    Require(ui.IsTownPanelOpen && manager.CurTown==land.Town && ui.CenterSlot.Hex==land && ui.AllUnitDrags.Count==2,"Enter did not open own town/idle workers.");
                    Hex[] neighbors=HexCalculator.GetHexAroundToArray(manager.AllHexes,land);
                    for(int i=0;i<6;i++) Require(ui.AreaSlots[i].Hex==neighbors[i],"Town Hex order incorrect.");
                    first=ui.AreaSlots.First(s=>s.Hex!=null && !s.Hex.HasTown && s.Hex.Labor==null);
                    second=ui.AreaSlots.First(s=>s!=first && s.Hex!=null && !s.Hex.HasTown && s.Hex.Labor==null);
                    // Week 9 deliberately allows Backspace/Endturn from town;
                    // that new path has its own turn/warehouse coverage.
                    if(!ui.HasStockConfiguration) manager.Endturn();
                    pioneer.PrepareMoveToHex(first.Hex);
                    Require(manager.PlayerTurn && manager.GameTurn==1 && !pioneer.IsMoving,"Map action accepted behind town panel.");
                    cameraPosition=Camera.main.transform.position; zoom=Camera.main.orthographicSize;
                    Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek6BeforeDrag.png");
                    InputSystem.QueueStateEvent(keyboard,ui.HasStockConfiguration ? new KeyboardState(Key.W,Key.Z,Key.Tab,Key.P,Key.B) : new KeyboardState(Key.W,Key.Z,Key.Tab,Key.Backspace,Key.P,Key.B)); Next(); break;
                case 7:
                    if(phaseFrame<4) break;
                    Require(Camera.main.transform.position==cameraPosition && Camera.main.orthographicSize==zoom && manager.CurUnit==pioneer && pioneer.ToolsNum==100 && manager.GameTurn==1,"Town UI did not block map keyboard/camera input.");
                    SetKeys();
                    Drag(FindDrag(pioneer),first.gameObject);
                    Require(first.Hex.Labor==pioneer && pioneer.UnitStatus==UnitStatus.WorkInField && FindDrag(pioneer).transform.parent==first.LaborParent,"Outside-to-field drag failed.");
                    Drag(FindDrag(soldier),first.gameObject);
                    Require(first.Hex.Labor==pioneer && soldier.UnitStatus==UnitStatus.None && FindDrag(soldier).transform.parent==ui.OutsideTownParent.transform,"Occupied Hex accepted second worker.");
                    Drag(FindDrag(pioneer),ui.CenterSlot.gameObject);
                    Require(first.Hex.Labor==pioneer && FindDrag(pioneer).TerrainSlot==first,"Center Hex accepted worker/old job lost.");
                    Drag(FindDrag(pioneer),second.gameObject);
                    Require(first.Hex.Labor==null && second.Hex.Labor==pioneer && FindDrag(pioneer).transform.parent==second.LaborParent,"Field-to-field reassignment failed.");
                    // Dropping over empty background cancels without losing assignment.
                    UnitDrag cancel=FindDrag(pioneer); PointerEventData data=Data(cancel.gameObject);
                    ExecuteEvents.Execute(cancel.gameObject,data,ExecuteEvents.beginDragHandler);
                    ExecuteEvents.Execute(cancel.gameObject,data,ExecuteEvents.endDragHandler);
                    Require(second.Hex.Labor==pioneer && cancel.transform.parent==second.LaborParent,"Cancelled drag lost job.");
                    Next(); break;
                case 8:
                    if(phaseFrame==4) { Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek6TownPanel.png"); }
                    if(phaseFrame<8) break;
                    ui.ToggleTownPanel(false);
                    Require(!pioneer.gameObject.activeSelf && ui.AllUnitDrags.Count==0 && !ui.IsTownPanelOpen,"Closing did not hide worker/clean icons.");
                    manager.SelectPlayerUnit(soldier); SetKeys(Key.Enter); Next(); break;
                case 9:
                    if(phaseFrame<4) break;
                    Require(ui.IsTownPanelOpen && ui.AllUnitDrags.Count==2 && FindDrag(pioneer).transform.parent==second.LaborParent && second.Hex.Labor==pioneer,"Reopening lost worker/recreated duplicate icons."); SetKeys();
                    Drag(FindDrag(pioneer),ui.OutsideTownParent);
                    Require(second.Hex.Labor==null && pioneer.UnitStatus==UnitStatus.None && FindDrag(pioneer).TerrainSlot==null,"Field-to-outside drag failed.");
                    SetKeys(Key.Escape); Next(); break;
                case 10:
                    if(phaseFrame<4) break;
                    Require(!ui.IsTownPanelOpen && pioneer.gameObject.activeSelf && ui.AllUnitDrags.Count==0,"Escape/idle worker visibility failed."); SetKeys();
                    manager.SetupCurrentTown(manager.Factions[5].Towns[0]); Require(!ui.IsTownPanelOpen,"Foreign town opened.");
                    // A corner town exercises null neighbors and image reset after a
                    // different city with forest/ships was shown.
                    Hex corner=manager.AllHexes[0,0]; if(corner.HasTown) corner=manager.AllHexes[0,59];
                    Require(!corner.HasTown,"Corner fixture unexpectedly occupied twice.");
                    corner.ClearForest(); manager.GenerateTown(manager.PlayerFaction,corner);
                    manager.SetupCurrentTown(corner.Town); Require(ui.IsTownPanelOpen,"Corner town failed to open.");
                    Hex[] around=HexCalculator.GetHexAroundToArray(manager.AllHexes,corner);
                    Require(around.Any(h=>h==null),"Corner lacks null neighbor fixture.");
                    for(int i=0;i<6;i++) Require(ui.AreaSlots[i].Hex==around[i] && ui.AreaSlots[i].gameObject.activeSelf==(around[i]!=null),"Null/stale neighbor presentation failed.");
                    Require(ui.AllUnitDrags.Count==0,"Previous city's units leaked into corner town.");
                    ui.ToggleTownPanel(false); manager.SelectPlayerUnit(pioneer); manager.SetupCurrentTown(land.Town); Next(); break;
                case 11:
                    if(phaseFrame<4) break;
                    UnityEngine.UI.Button exit=ui.TownPanel.transform.Find(ui.HasStockConfiguration?"NamePanel/ExitButton":"ProductPanel/ExitButton").GetComponent<UnityEngine.UI.Button>();
                    PointerEventData click=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                    ExecuteEvents.Execute(exit.gameObject,click,ExecuteEvents.pointerClickHandler);
                    Require(!ui.IsTownPanelOpen && ui.AllUnitDrags.Count==0,"EXIT button not wired.");
                    ValidateOccupancy(manager);
                    Require(File.Exists("Temp/ColonizationWeek6TownPanel.png"),"Town UI screenshot missing.");
                    Finish("PASSED\nWeek 5 regression suite PASSED in the first play session.\nPlain/Tropical Native units, exactly-once Hex unit membership, reserved/capacity-checked animated reboarding and disembarkation, passenger synchronization after ship movement, full/enemy/empty-water guards.\nEnter own-town UI, seven ordered Hex slots, actual GraphicRaycaster drag/drop into fields, occupied/center/cancelled drop guards, field reassignment, worker persistence across close/reopen, return outside, Escape/EXIT, map input blocking and corner/null-neighbor resets.\nScreenshot: Temp/ColonizationWeek6TownPanel.png\n"); break;
            }
        }
        catch(Exception e)
        {
            if(ui!=null && ui.IsTownPanelOpen) ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek6FailedUI.png");
            Finish("FAILED\nPhase "+phase+": "+e);
        }
    }
    private static void StartExtended()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup-editorStableSince<5)
        { SessionState.SetBool(Pending+".StartRequested",true); return; }
        SessionState.SetBool(Pending,true); runtimeReady=false;
        SessionState.SetBool(Pending+".Entered",false); SessionState.SetInt(Pending+".Phase",0);
        File.WriteAllText(Result,"RUNNING\nWeek 5 passed; entering Week 6 Play Mode.\n");
        EditorApplication.isPlaying=true;
    }
    private static void BeginRuntime()
    {
        runtimeReady=true;
        File.WriteAllText(Result,"RUNNING\nWeek 5 passed; Week 6 entered Play Mode.\n");
        phase=phaseFrame=0; lastFrame=-1; error=null; started=EditorApplication.timeSinceStartup;
        if(!SessionState.GetBool(Pending+".InputSaved",false))
        {
            SessionState.SetInt(Pending+".OldInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);
            SessionState.SetInt(Pending+".OldBackground",(int)InputSystem.settings.backgroundBehavior);
            SessionState.SetBool(Pending+".OldRunBackground",Application.runInBackground);
            SessionState.SetBool(Pending+".InputSaved",true);
        }
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; Application.runInBackground=true;
    }
    private static void RestoreInput()
    {
        if(!SessionState.GetBool(Pending+".InputSaved",false)) return;
        InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Pending+".OldInput",0);
        InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Pending+".OldBackground",0);
        Application.runInBackground=SessionState.GetBool(Pending+".OldRunBackground",false);
        SessionState.SetBool(Pending+".InputSaved",false);
    }
    private static void Next(){phase++;phaseFrame=0; SessionState.SetInt(Pending+".Phase",phase); File.WriteAllText(Result,"RUNNING\nWeek 5 passed; Week 6 phase "+phase+".\n");}
    private static void SetKeys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
    private static void PositionShip(NavalUnit vessel,Hex target)
    { vessel.SetupPosition(target); foreach(LandUnit rider in vessel.Passengers) rider.SetupPosition(target); }
    private static void SetCargo(int value)
    { SerializedObject so=new SerializedObject(ship); so.FindProperty("cargoHoldNum").intValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void ValidateOccupancy(GameManager manager)
    {
        List<Unit> units=manager.Factions.SelectMany(f=>f.Units).ToList();
        foreach(Hex hex in manager.AllHexes)
            Require(hex.UnitsInHex.Distinct().Count()==hex.UnitsInHex.Count && hex.UnitsInHex.All(u=>u!=null && u.CurHex==hex && units.Contains(u)),"Hex membership duplicate/stale.");
        foreach(Unit unit in units) Require(unit.CurHex.UnitsInHex.Count(u=>u==unit)==1,"Unit missing from its current Hex.");
    }
    private static UnitDrag FindDrag(LandUnit unit){return ui.AllUnitDrags.Select(o=>o.GetComponent<UnitDrag>()).Single(d=>d.LandUnit==unit);}
    private static PointerEventData Data(GameObject drag){return new PointerEventData(EventSystem.current){pointerDrag=drag,button=PointerEventData.InputButton.Left,position=drag.transform.position};}
    private static void Drag(UnitDrag drag,GameObject target)
    {
        Canvas.ForceUpdateCanvases(); PointerEventData data=Data(drag.gameObject);
        ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.beginDragHandler);
        data.position=target.transform.position;
        ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.dragHandler);
        List<RaycastResult> hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0,"UI drop raycast missed.");
        GameObject handler=ExecuteEvents.GetEventHandler<IDropHandler>(hits[0].gameObject);
        Require(handler==target,"Unexpected UI drop target: "+hits[0].gameObject.name+"; target="+target.name+"; position="+data.position+"; canvas="+ui.GetComponent<Canvas>().renderMode+" scale="+ui.GetComponent<Canvas>().scaleFactor+"; hits="+string.Join(" | ",hits.Select(h=>h.gameObject.name+":"+h.module.GetType().Name+"/layer="+h.sortingLayer+"/order="+h.sortingOrder+"/priority="+h.module.sortOrderPriority)));
        ExecuteEvents.Execute(handler,data,ExecuteEvents.dropHandler);
        // The Week 7 contract asks for a profession after a valid drop. This
        // regression chooses Food; Week 7 separately raycasts every real button.
        if(ui.IsProfessionPanelOpen) ui.SelectYield(0);
        ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.endDragHandler);
    }
    private static void ClickWorld(Vector3 position,PointerEventData.InputButton button,Type expected)
    {
        Physics2D.SyncTransforms(); PointerEventData data=new PointerEventData(EventSystem.current){position=Camera.main.WorldToScreenPoint(position),button=button};
        List<RaycastResult> hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0,"Ship raycast missed."); GameObject handler=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Require(handler!=null && handler.GetComponent(expected)!=null,"Unexpected ship click target.");
        ExecuteEvents.Execute(handler,data,ExecuteEvents.pointerClickHandler);
    }
    private static void Finish(string result)
    {
        if(ui!=null && ui.IsTownPanelOpen) ui.ToggleTownPanel(false);
        if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
        RestoreInput();
        SessionState.SetBool(Pending,false); File.WriteAllText(Result,result); EditorApplication.isPlaying=false;
        runtimeReady=false;
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
