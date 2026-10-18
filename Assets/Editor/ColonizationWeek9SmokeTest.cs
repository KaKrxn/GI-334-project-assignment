using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
using InputKey=UnityEngine.InputSystem.Key;

public static class ColonizationWeek9SmokeTest
{
    public const string Request="Temp/ColonizationWeek9SmokeTest.v1.request";
    public const string Result="Temp/ColonizationWeek9SmokeTest.result.txt";
    private const string Key="Colonization.Week9Smoke";
    private static double stableSince,initialized,started,phaseStarted;
    private static int phase,frames,operation;
    private static bool ready;
    private static string error;
    private static GameManager manager;
    private static UIManager ui;
    private static NavalUnit ship,galleon;
    private static LandUnit pioneer,soldier,passenger;
    private static Town town,foreignTown,emptyTown;
    private static Hex sea,neutral;
    private static TerrainSlot forest;
    private static Keyboard keyboard;
    private static int moveBefore;
    private static Dictionary<Town,int[]> expected;

    [InitializeOnLoadMethod] private static void Initialize()
    {
        initialized=stableSince=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=PlayChanged;
        Application.logMessageReceived+=(message,stack,type)=> { if(SessionState.GetBool(Key+".Pending",false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) error=message+"\n"+stack; };
    }
    [MenuItem("Tools/Colonization/Run Week 9 Play Mode Smoke Test")]
    public static void Run()
    {
        Preflight(); SessionState.SetBool(Key+".Regression",true); SessionState.SetFloat(Key+".RegressionStarted",(float)EditorApplication.timeSinceStartup);
        File.WriteAllText(Result,"RUNNING\nWeek 5/6/7/8 behavioral regression suites first.\n"); ColonizationWeek8SmokeTest.Run();
    }
    private static void Preflight()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount!=1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode for Week 9 validation.");
        ColonizationWeek9Setup.Validate(Object.FindAnyObjectByType<GameManager>(),Object.FindAnyObjectByType<UIManager>());
    }
    private static void Tick()
    {
        if(SessionState.GetBool(Key+".Pending",false) && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPaused)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if((!ready && EditorApplication.timeSinceStartup-initialized>5) || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup)>60)
            { Finish("FAILED\nWeek 9 player loop/reload interrupted phase "+SessionState.GetInt(Key+".Phase",0)); return; }
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) stableSince=EditorApplication.timeSinceStartup;
        if(File.Exists(Request) && !File.Exists(ColonizationWeek9Setup.Request) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string mode=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try { Preflight(); if(mode=="extend") { Require(File.Exists("Temp/ColonizationWeek8SmokeTest.result.txt") && File.ReadAllText("Temp/ColonizationWeek8SmokeTest.result.txt").StartsWith("PASSED"),"A passed Week 8 regression is required."); SessionState.SetBool(Key+".Start",true); } else Run(); }
            catch(Exception e) { Finish("FAILED\n"+e); }
        }
        if(SessionState.GetBool(Key+".Regression",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string result=File.ReadAllText("Temp/ColonizationWeek8SmokeTest.result.txt");
            if(result.StartsWith("FAILED") || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".RegressionStarted",0)>1000) Finish("FAILED\nWeek 8 regression:\n"+result);
            else if(result.StartsWith("PASSED") && SceneManager.GetActiveScene().path=="Assets/Scenes/Map01.unity") { SessionState.SetBool(Key+".Regression",false); SessionState.SetBool(Key+".Start",true); }
        }
        if(SessionState.GetBool(Key+".Start",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !File.Exists("Temp/ColonizationEditorRefresh.request") && EditorApplication.timeSinceStartup-stableSince>10)
        {
            SessionState.SetBool(Key+".Start",false);
            try
            {
                Preflight(); Settings.ResetForNewGame(); SessionState.SetBool(Key+".Pending",true); SessionState.SetInt(Key+".Phase",0); SessionState.SetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup);
                if(!Application.isBatchMode) { Type type=typeof(Editor).Assembly.GetType("UnityEditor.GameView"); if(type!=null) { EditorWindow window=EditorWindow.GetWindow(type); window.Show(); window.Focus(); } }
                File.WriteAllText(Result,"RUNNING\nEntering Week 9 Play Mode.\n"); EditorApplication.isPlaying=true;
            }
            catch(Exception e) { Finish("FAILED\n"+e); }
        }
    }
    private static void PlayChanged(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode) RestoreInput();
        if(!SessionState.GetBool(Key+".Pending",false)) return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            phase=frames=operation=0; ready=true; error=null; started=phaseStarted=EditorApplication.timeSinceStartup;
            manager=GameManager.instance; ui=UIManager.instance;
            SessionState.SetInt(Key+".OldInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode); SessionState.SetInt(Key+".OldBackground",(int)InputSystem.settings.backgroundBehavior); SessionState.SetBool(Key+".OldRun",Application.runInBackground); SessionState.SetBool(Key+".InputSaved",true);
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>(); SessionState.SetInt(Key+".Keyboard",keyboard.deviceId);
            EventSystem.current.StartCoroutine(RuntimeLoop());
        }
        if(state==PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key+".Pending",false); File.WriteAllText(Result,"FAILED\nPlay Mode exited before Week 9 completed.\n"); }
    }
    private static IEnumerator RuntimeLoop()
    {
        while(ready && Application.isPlaying && SessionState.GetBool(Key+".Pending",false))
        {
            yield return null; if(EditorApplication.isPaused) continue;
            try { if(error!=null) throw new InvalidOperationException(error); if(EditorApplication.timeSinceStartup-started>300) throw new TimeoutException("Week 9 phase "+phase); Step(); }
            catch(Exception e) { Finish("FAILED\nPhase "+phase+", operation "+operation+"\n"+e); }
        }
    }
    private static void Step()
    {
        if(ColonizationLegacyDialogFixture.AcknowledgeNotice()) return;
        frames++; if(frames<8 || EditorApplication.timeSinceStartup-phaseStarted<.2) return;
        switch(phase)
        {
            case 0:
                manager=GameManager.instance; ui=UIManager.instance; if(manager==null || ui==null || manager.AllHexes[0,0]==null) break;
                ColonizationWeek9Setup.Validate(manager,ui); TestCargoRules(); BuildFixture();
                manager.SelectPlayerUnit(pioneer); Require(pioneer.gameObject.activeSelf && !soldier.gameObject.activeSelf,"Selecting a land unit did not hide its idle peer.");
                manager.SelectPlayerUnit(soldier); Require(soldier.gameObject.activeSelf && !pioneer.gameObject.activeSelf,"Selecting the other land unit did not swap visibility.");
                pioneer.gameObject.SetActive(false); soldier.gameObject.SetActive(false); WorldClick(town.gameObject,PointerEventData.InputButton.Left);
                Require(ui.IsTownPanelOpen && manager.CurTown==town && ui.AllShipIcons.Count==0 && ui.CargoSlots.All(s=>!s.gameObject.activeSelf),"Owned-town click or empty-port cleanup failed.");
                forest=ui.AreaSlots.First(s=>s.Hex!=null && s.Hex.HasForest && !s.Hex.HasTown && s.Hex.HexType!=HexType.Ocean);
                forest.Hex.ResourceYield=new[]{5,4,3,3,5,5,4,2}; ui.RefreshTownYield(); Next(1); break;
            case 1:
                if(operation==0) { DragWorker(ui.AllUnitDrags.Select(o=>o.GetComponent<UnitDrag>()).Single(d=>d.LandUnit==pioneer),forest.gameObject); Require(ui.IsProfessionPanelOpen,"Worker drop did not open professions."); operation=1; Next(1); break; }
                Click(ui.ProfessionPanel.transform.Find("SelectPanel/Button5").gameObject);
                Require(town.TotalYieldThisTurn[0]==3 && town.TotalYieldThisTurn[5]==5 && town.Warehouse.All(n=>n==0),"Preview altered warehouse or yield arithmetic.");
                ui.ToggleTownPanel(false); manager.SelectPlayerUnit(ship); moveBefore=ship.MovePoint;
                ship.PrepareMoveToHex(foreignTown.CurHex); Require(!ship.IsMoving && ship.MovePoint==moveBefore,"Ship accepted an enemy port.");
                ship.PrepareMoveToHex(neutral); Require(!ship.IsMoving && ship.MovePoint==moveBefore,"Ship accepted ordinary land.");
                soldier.gameObject.SetActive(false); WorldClick(town.gameObject,PointerEventData.InputButton.Right); Require(ship.IsMoving,"Right-clicking friendly town did not begin ship movement."); Next(2); break;
            case 2:
                if(ship.IsMoving) break;
                Require(ship.CurHex==town.CurHex && ship.MovePoint==moveBefore-town.CurHex.MoveCost && passenger.CurHex==town.CurHex && passenger.TransportShip==ship && town.CurHex.UnitsInHex.Count(u=>u==passenger)==1,"Port arrival/cost/passenger membership failed.");
                Keys(InputKey.Enter); Next(3); break;
            case 3:
                Keys(); Require(ui.IsTownPanelOpen && ui.AllShipIcons.Count==1 && ui.CurrentShipInPort.NavalUnit==ship && ui.CargoSlots.Count(s=>s.gameObject.activeSelf)==2,"Enter/Caravel port capacity failed.");
                town.Warehouse[0]=250; town.Warehouse[1]=80; town.Warehouse[2]=7; RefreshStock(); operation=0; Next(4); break;
            case 4:
                StockOperations(); break;
            case 5:
                Require(!ui.IsTownPanelOpen && town.Warehouse[0]==10 && ui.AllShipIcons.Count==0 && ui.CargoSlots.All(s=>!s.gameObject.activeSelf) && ui.StockSlots.All(s=>!s.StockDrag.IsDragging),"Closing during drag lost stock or left port UI alive.");
                galleon=CreateShip(manager.NavalUnitData[2],town.CurHex); manager.SetupCurrentTown(town); Next(6); break;
            case 6:
                Require(ui.AllShipIcons.Count==2 && ui.StockSlots.All(s=>s.GetComponentsInChildren<StockDrag>(true).Length==1),"Reopening duplicated ship or stock icons.");
                Click(ui.AllShipIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==galleon).gameObject); Next(7); break;
            case 7:
                Require(ui.CargoSlots.All(s=>s.gameObject.activeSelf) && ui.CurrentShipInPort.NavalUnit==galleon,"Selecting Galleon did not enable six holds.");
                town.Warehouse[1]=180; RefreshStock(); DragStock(1,ui.CargoSlots[5].gameObject);
                Require(town.Warehouse[1]==80 && galleon.CargoList.Count==6 && galleon.CargoList.Take(5).All(c=>c==null) && galleon.CargoList[5].ProductID==1 && galleon.CargoList[5].Quantity==100,"Sixth/sparse cargo hold binding failed."); Next(8); break;
            case 8:
                BeginStock(0); Require(town.Warehouse[0]==0,"Stock reservation was not deducted.");
                Click(ui.AllShipIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==ship).gameObject);
                Require(town.Warehouse[0]==10 && ui.CargoSlots.Count(s=>s.gameObject.activeSelf)==2 && !ui.StockSlots[0].StockDrag.IsDragging,"Ship switch did not cancel/refund the pending load.");
                Click(ui.AllShipIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==galleon).gameObject); Next(9); break;
            case 9:
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek9Port.png");
                for(int i=0;i<3;i++) { ui.ToggleTownPanel(false); manager.SetupCurrentTown(town); Require(ui.AllShipIcons.Count==2,"Repeated opening duplicated ships."); }
                Next(19); break;
            case 19:
                BeginStock(0); ui.ToggleTownPanel(false); manager.SetupCurrentTown(emptyTown); Next(10); break;
            case 10:
                Require(town.Warehouse[0]==10 && emptyTown.Warehouse.All(n=>n==0) && ui.AllShipIcons.Count==0 && ui.CargoSlots.All(s=>!s.gameObject.activeSelf) && ui.StockSlots.All(s=>s.Town==emptyTown && s.Quantity==0),"Town switch leaked ship/warehouse state or refunded to the wrong town.");
                ui.ToggleTownPanel(false); WorldClick(foreignTown.gameObject,PointerEventData.InputButton.Left); Require(!ui.IsTownPanelOpen,"Enemy town opened the player's stock UI.");
                manager.SetupCurrentTown(town); Next(11); break;
            case 11:
                expected=ExpectedNextHarvest(); BeginStock(0); Keys(InputKey.Backspace); Next(12); break;
            case 12:
                Keys(); Require(!ui.IsTownPanelOpen && !manager.PlayerTurn && town.Warehouse[0]==10 && ui.AllShipIcons.Count==0,"Backspace did not close/refund town UI and start AI.");
                manager.Endturn(); manager.StartNewTurn(); Require(!manager.PlayerTurn,"Duplicate/premature new-turn call escaped the AI turn."); Next(13); break;
            case 13:
                if(!manager.PlayerTurn) break;
                Require(manager.GameTurn==2,"First new turn count incorrect."); VerifyHarvest();
                manager.AccumulateStockAllTowns(); manager.StartNewTurn(); VerifyHarvest();
                manager.SetupCurrentTown(town); Next(14); break;
            case 14:
                Require(ui.StockSlots[0].Quantity==13 && ui.StockSlots[5].Quantity==5 && ui.StockSlots[0].StockText.text=="13" && ui.StockSlots[5].StockText.text=="5","First harvest not reflected in stock slots.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek9Warehouse.png"); Next(15); break;
            case 15:
                DragWorker(ui.AllUnitDrags.Select(o=>o.GetComponent<UnitDrag>()).Single(d=>d.LandUnit==pioneer),ui.OutsideTownParent);
                Require(town.TotalYieldThisTurn[5]==0 && town.Warehouse[5]==5,"Stopping work changed existing stock or retained future yield.");
                expected=ExpectedNextHarvest(); Keys(InputKey.Backspace); Next(16); break;
            case 16: Keys(); if(!manager.PlayerTurn) break;
                Require(manager.GameTurn==3,"Second turn did not complete."); VerifyHarvest(); Require(town.Warehouse[0]==16 && town.Warehouse[5]==5,"Repeated turn or removed worker accrued incorrect yield.");
                Require(ship.CargoList.Where(c=>c!=null).Sum(c=>c.Quantity)==200 && galleon.CargoList[5].Quantity==100,"Turn boundary changed cargo.");
                manager.SelectPlayerUnit(ship); moveBefore=ship.MovePoint; ship.PrepareMoveToHex(sea); Require(ship.IsMoving,"Docked ship could not leave port."); Next(17); break;
            case 17:
                if(ship.IsMoving) break;
                Require(ship.CurHex==sea && ship.MovePoint==moveBefore-sea.MoveCost && passenger.CurHex==sea && ship.CargoList.Where(c=>c!=null).Sum(c=>c.Quantity)==200,"Port departure lost cargo/passenger sync or charged wrong cost.");
                manager.SetupCurrentTown(town); Next(18); break;
            case 18:
                Require(ui.AllShipIcons.Count==1 && ui.CurrentShipInPort.NavalUnit==galleon && ui.CargoSlots.All(s=>s.gameObject.activeSelf) && ui.StockSlots[0].Quantity==16 && ui.StockSlots[5].Quantity==5,"Port list/stock did not refresh after a ship departed.");
                Click(ui.TownPanel.transform.Find("NamePanel/ExitButton").gameObject);
                Require(!ui.IsTownPanelOpen && ui.AllShipIcons.Count==0 && ui.CargoSlots.All(s=>!s.gameObject.activeSelf) && ui.StockSlots.All(s=>!s.StockDrag.IsDragging),"Header exit left stock/ship UI alive.");
                Require(File.Exists("Temp/ColonizationWeek9Port.png") && File.Exists("Temp/ColonizationWeek9Warehouse.png"),"Week 9 screenshots missing.");
                Finish("PASSED\nWeek 5/6/7/8 behavioral regression suites passed first.\n16 native StockSlots and four prefabs; warehouse preview/reopen integrity; two actual Backspace/AI turns accumulate exactly once for all towns and factions; removing a worker stops future yield while existing stock remains; town UI closes/refunds pending stock when ending a turn.\nOwned/enemy town clicks, land-unit visibility, animated friendly-port entry/departure with costs/passenger membership/cargo persistence, rejected enemy/ordinary-land entry, ship selection with two/six holds and empty-town cleanup.\nActual GraphicRaycaster/EventSystem stock drags: maximum 100, sparse hold indices, same-product partial stack/overflow, full/mixed/invalid/cancelled drops, close/switch refunds and conservation, no duplicate ship/stock icons after repeated reopen.\nCargo content icons, unloading, Europe travel/trading and consumption/conversion are outside Week 9. Screenshots: Temp/ColonizationWeek9Port.png and Temp/ColonizationWeek9Warehouse.png.\n"); break;
        }
    }
    private static void StockOperations()
    {
        switch(operation)
        {
            case 0: DragStock(0,ui.CargoSlots[1].gameObject); Require(town.Warehouse[0]==150 && ship.CargoList.Count==2 && ship.CargoList[0]==null && ship.CargoList[1].Quantity==100,"Stock load ignored 100 limit or target hold."); break;
            case 1: DragStock(0,ui.CargoSlots[1].gameObject); Require(town.Warehouse[0]==150 && ship.CargoList[1].Quantity==100,"Full hold did not refund."); ship.CargoList[1].Quantity=60; break;
            case 2: DragStock(1,ui.CargoSlots[1].gameObject); Require(town.Warehouse[1]==80 && ship.CargoList[1].ProductID==0 && ship.CargoList[1].Quantity==60,"Mixed-product drop overwrote cargo."); break;
            case 3: DragStock(0,ui.CargoSlots[1].gameObject); Require(town.Warehouse[0]==110 && ship.CargoList[1].Quantity==100,"Partial stack overflow was not returned."); break;
            case 4: DragStock(0,ui.CargoSlots[0].gameObject); Require(town.Warehouse[0]==10 && ship.CargoList[0].Quantity==100 && ship.CargoList.Sum(c=>c.Quantity)+town.Warehouse[0]==210,"Loading empty first hold lost/duplicated stock."); break;
            case 5: DragStock(0,ui.CargoSlots[0].gameObject); Require(town.Warehouse[0]==10,"Small load into full hold was not refunded."); break;
            case 6: DragStock(2,null); Require(town.Warehouse[2]==7,"Cancelled/outside drop lost a small stock stack."); break;
            case 7: DragStock(0,ui.OutsideTownParent); Require(town.Warehouse[0]==10,"Invalid outside drop lost stock."); break;
            case 8: BeginStock(0); ui.ToggleTownPanel(false); Next(5); return;
        }
        operation++; Next(4);
    }
    private static void BuildFixture()
    {
        ship=manager.PlayerFaction.Units.OfType<NavalUnit>().Single(); pioneer=manager.PlayerFaction.Units.OfType<LandUnit>().Single(u=>u.LandUnitType==LandUnitType.HardyPioneers); soldier=manager.PlayerFaction.Units.OfType<LandUnit>().Single(u=>u.LandUnitType==LandUnitType.VeteranSoldiers);
        sea=manager.AllHexes.Cast<Hex>().First(h=>h.HexType==HexType.Ocean && HexCalculator.GetHexAround(manager.AllHexes,h).Count(n=>!n.HasTown && n.HexType!=HexType.Ocean && n.HexType!=HexType.Mountains && n.HexType!=HexType.Hills)>=3 && HexCalculator.GetHexAround(manager.AllHexes,h).Any(n=>!n.HasTown && n.HexType!=HexType.Ocean && n.HexType!=HexType.Mountains && n.HexType!=HexType.Hills && HexCalculator.GetHexAround(manager.AllHexes,n).Any(f=>!f.HasTown && f.HasForest && f.HexType!=HexType.Ocean)));
        Hex[] land=HexCalculator.GetHexAround(manager.AllHexes,sea).Where(h=>!h.HasTown && h.HexType!=HexType.Ocean && h.HexType!=HexType.Mountains && h.HexType!=HexType.Hills).ToArray();
        Hex home=land.First(h=>HexCalculator.GetHexAround(manager.AllHexes,h).Any(f=>!f.HasTown && f.HasForest && f.HexType!=HexType.Ocean));
        Hex work=HexCalculator.GetHexAround(manager.AllHexes,home).First(f=>!f.HasTown && f.HasForest && f.HexType!=HexType.Ocean);
        Hex foreign=land.First(h=>h!=home && h!=work); neutral=land.First(h=>h!=home && h!=foreign);
        home.ClearForest(); home.ResourceYield=new[]{5,0,0,0,0,0,0,0}; manager.GenerateTown(manager.PlayerFaction,home); town=home.Town;
        foreign.ClearForest(); foreign.ResourceYield=new[]{4,0,0,0,0,0,0,0}; manager.GenerateTown(manager.Factions[1],foreign); foreignTown=foreign.Town;
        Hex empty=manager.AllHexes.Cast<Hex>().First(h=>!h.HasTown && h.HexType!=HexType.Ocean && h.HexType!=HexType.Mountains && !HexCalculator.GetHexAround(manager.AllHexes,home).Contains(h)); empty.ClearForest(); empty.ResourceYield=new[]{5,0,0,0,0,0,0,0}; manager.GenerateTown(manager.PlayerFaction,empty); emptyTown=empty.Town;
        foreach(LandUnit unit in new[]{pioneer,soldier}) { ship.Passengers.Remove(unit); unit.TransportShip=null; unit.transform.SetParent(manager.PlayerFaction.UnitParent,true); unit.UnitStatus=UnitStatus.None; unit.gameObject.SetActive(true); unit.SetupPosition(home); }
        ship.SetupPosition(sea); ship.MovePoint=20;
        GameObject prefab=(GameObject)new SerializedObject(manager).FindProperty("landUnitPrefab").objectReferenceValue;
        passenger=Object.Instantiate(prefab,ship.PassengerParent.transform).GetComponent<LandUnit>(); passenger.UnitInit(manager,manager.PlayerFaction,manager.LandUnitData[0]); passenger.SetupPosition(sea); passenger.TransportShip=ship; passenger.UnitStatus=UnitStatus.OnBoard; ship.Passengers.Add(passenger); manager.PlayerFaction.Units.Add(passenger); passenger.gameObject.SetActive(false);
    }
    private static NavalUnit CreateShip(NavalUnitData data,Hex hex)
    {
        GameObject prefab=(GameObject)new SerializedObject(manager).FindProperty("navalUnitPrefab").objectReferenceValue;
        NavalUnit unit=Object.Instantiate(prefab,manager.PlayerFaction.UnitParent).GetComponent<NavalUnit>(); unit.UnitInit(manager,manager.PlayerFaction,data); if(hex!=null) unit.SetupPosition(hex); manager.PlayerFaction.Units.Add(unit); return unit;
    }
    private static void TestCargoRules()
    {
        NavalUnit fixture=CreateShip(manager.NavalUnitData[2],null);
        try
        {
            Cargo source=new Cargo(1,135); Require(fixture.AddCargo(5,source)==35 && source.Quantity==135 && fixture.CargoList.Count==6 && fixture.CargoList.Take(5).All(c=>c==null) && fixture.CargoList[5].Quantity==100,"Sparse/full hold rule or incoming alias failed.");
            Require(fixture.AddCargo(-1,new Cargo(0,10))==10 && fixture.AddCargo(6,new Cargo(0,10))==10 && fixture.AddCargo(0,new Cargo(16,10))==10,"Cargo index/product bounds failed.");
            fixture.CargoList[5].Quantity=70; Require(fixture.AddCargo(5,new Cargo(2,20))==20 && fixture.CargoList[5].Quantity==70,"Mixed product stack accepted.");
            Require(fixture.AddCargo(5,new Cargo(1,50))==20 && fixture.CargoList[5].Quantity==100,"Same-product stack overflow failed.");
        }
        finally { manager.PlayerFaction.Units.Remove(fixture); Object.Destroy(fixture.gameObject); }
    }
    private static Dictionary<Town,int[]> ExpectedNextHarvest()
    {
        Dictionary<Town,int[]> result=new Dictionary<Town,int[]>();
        foreach(Faction faction in manager.Factions) foreach(Town city in faction.Towns) { city.RecalculateYield(); result[city]=Enumerable.Range(0,16).Select(i=>city.Warehouse[i]+city.TotalYieldThisTurn[i]).ToArray(); }
        return result;
    }
    private static void VerifyHarvest() { foreach(KeyValuePair<Town,int[]> item in expected) Require(item.Key.Warehouse.SequenceEqual(item.Value),"Warehouse accumulation differs for "+item.Key.Faction.Nation+" at "+item.Key.CurPos); }
    private static void RefreshStock() { foreach(StockSlot slot in ui.StockSlots) slot.UpdateStockText(); }
    private static PointerEventData Data(GameObject drag) { return new PointerEventData(EventSystem.current){pointerDrag=drag,position=drag.transform.position,button=PointerEventData.InputButton.Left}; }
    private static GameObject Hit(Vector2 point,bool graphics)
    { List<RaycastResult> hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits); return hits.FirstOrDefault(h=>graphics?h.module is UnityEngine.UI.GraphicRaycaster:h.module is Physics2DRaycaster).gameObject; }
    private static void Click(GameObject target)
    {
        Canvas.ForceUpdateCanvases(); GameObject hit=Hit(target.transform.position,true); Require(hit!=null && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit)==target,"UI click raycast missed "+target.name+"; hit="+(hit!=null?hit.name:"none")); ExecuteEvents.Execute(target,Data(target),ExecuteEvents.pointerClickHandler);
    }
    private static void WorldClick(GameObject target,PointerEventData.InputButton button)
    {
        CameraController.instance.MoveCamera(target.transform.position); Canvas.ForceUpdateCanvases();
        Vector2 point=Camera.main.WorldToScreenPoint(target.transform.position); GameObject hit=Hit(point,false);
        Require(hit!=null && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit)==target,"World click raycast missed "+target.name+"; hit="+(hit!=null?hit.name:"none"));
        ExecuteEvents.Execute(target,new PointerEventData(EventSystem.current){position=point,button=button},ExecuteEvents.pointerClickHandler);
    }
    private static StockDrag BeginStock(int id)
    {
        StockDrag drag=ui.StockSlots[id].StockDrag; Canvas.ForceUpdateCanvases(); GameObject hit=Hit(drag.transform.position,true);
        Require(hit!=null && ExecuteEvents.GetEventHandler<IBeginDragHandler>(hit)==drag.gameObject,"Stock begin raycast missed "+id+"; hit="+(hit!=null?hit.name:"none"));
        ExecuteEvents.Execute(drag.gameObject,Data(drag.gameObject),ExecuteEvents.beginDragHandler); Require(drag.IsDragging,"Stock drag rejected "+id); return drag;
    }
    private static void DragStock(int id,GameObject target)
    {
        StockDrag drag=BeginStock(id); PointerEventData data=Data(drag.gameObject);
        if(target!=null) { data.position=target.transform.position; ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.dragHandler); Canvas.ForceUpdateCanvases(); GameObject hit=Hit(data.position,true); GameObject drop=hit!=null?ExecuteEvents.GetEventHandler<IDropHandler>(hit):null; if(drop!=null) ExecuteEvents.Execute(drop,data,ExecuteEvents.dropHandler); }
        ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.endDragHandler);
    }
    private static void DragWorker(UnitDrag drag,GameObject target)
    {
        Canvas.ForceUpdateCanvases(); GameObject source=Hit(drag.transform.position,true); Require(source!=null && ExecuteEvents.GetEventHandler<IBeginDragHandler>(source)==drag.gameObject,"Worker begin raycast missed.");
        PointerEventData data=Data(drag.gameObject); ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.beginDragHandler); Require(drag.IsDragging,"Worker did not begin dragging."); data.position=target.transform.position; ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.dragHandler); Canvas.ForceUpdateCanvases(); GameObject hit=Hit(data.position,true); GameObject handler=hit!=null?ExecuteEvents.GetEventHandler<IDropHandler>(hit):null; Require(handler==target,"Worker drop raycast missed."); ExecuteEvents.Execute(handler,data,ExecuteEvents.dropHandler); ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.endDragHandler);
    }
    private static void Keys(params InputKey[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
    private static void Next(int next) { phase=next; frames=0; phaseStarted=EditorApplication.timeSinceStartup; SessionState.SetFloat(Key+".LastProgress",(float)phaseStarted); SessionState.SetInt(Key+".Phase",phase); File.WriteAllText(Result,"RUNNING\nWeek 9 phase "+phase+"; stock operation "+operation+".\n"); }
    private static void RestoreInput()
    {
        InputDevice device=InputSystem.GetDeviceById(SessionState.GetInt(Key+".Keyboard",-1)); if(device!=null) InputSystem.RemoveDevice(device); SessionState.SetInt(Key+".Keyboard",-1);
        if(!SessionState.GetBool(Key+".InputSaved",false)) return;
        InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key+".OldInput",0); InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Key+".OldBackground",0); Application.runInBackground=SessionState.GetBool(Key+".OldRun",false); SessionState.SetBool(Key+".InputSaved",false);
    }
    private static void Finish(string result)
    {
        if(ui!=null && ui.IsTownPanelOpen) ui.ToggleTownPanel(false); RestoreInput(); ready=false; SessionState.SetBool(Key+".Pending",false); SessionState.SetBool(Key+".Regression",false); SessionState.SetBool(Key+".Start",false); File.WriteAllText(Result,result); if(EditorApplication.isPlaying) EditorApplication.isPlaying=false;
    }
    private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
}
