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

public static class ColonizationWeek11SmokeTest
{
    public const string Request="Temp/ColonizationWeek11SmokeTest.v1.request";
    public const string Result="Temp/ColonizationWeek11SmokeTest.result.txt";
    private const string Key="Colonization.Week11Smoke";
    private static double stableSince,initialized,started,phaseStarted;
    private static int phase,frames,operation;
    private static bool ready;
    private static string error;
    private static GameManager manager;
    private static UIManager ui;
    private static NavalUnit ship,galleon;
    private static Town town;
    private static Keyboard keyboard;

    [InitializeOnLoadMethod] private static void Initialize()
    {
        initialized=stableSince=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=PlayChanged;
        Application.logMessageReceived+=(message,stack,type)=> { if(SessionState.GetBool(Key+".Pending",false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) error=message+"\n"+stack; };
    }
    [MenuItem("Tools/Colonization/Run Week 10–11 Play Mode Smoke Test")]
    public static void Run()
    {
        Preflight(); SessionState.SetBool(Key+".Regression",true); SessionState.SetFloat(Key+".RegressionStarted",(float)EditorApplication.timeSinceStartup);
        File.WriteAllText(Result,"RUNNING\nWeek 5/6/7/8/9 behavioral regression suites first.\n"); ColonizationWeek9SmokeTest.Run();
    }
    private static void Preflight()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount!=1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode for Week 10–11 validation.");
        ColonizationWeek11Setup.Validate(Object.FindAnyObjectByType<GameManager>(),Object.FindAnyObjectByType<UIManager>());
    }
    private static void Tick()
    {
        if(SessionState.GetBool(Key+".Pending",false) && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPaused)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if((!ready && EditorApplication.timeSinceStartup-initialized>5) || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup)>60)
            { Finish("FAILED\nWeek 10–11 player loop/reload interrupted phase "+SessionState.GetInt(Key+".Phase",0)); return; }
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) stableSince=EditorApplication.timeSinceStartup;
        if(File.Exists(Request) && !File.Exists(ColonizationWeek11Setup.Request) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string mode=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try { Preflight(); if(mode=="extend") { Require(File.Exists("Temp/ColonizationWeek9SmokeTest.result.txt") && File.ReadAllText("Temp/ColonizationWeek9SmokeTest.result.txt").StartsWith("PASSED"),"A passed Week 9 regression is required."); SessionState.SetBool(Key+".Start",true); } else Run(); }
            catch(Exception e) { Finish("FAILED\n"+e); }
        }
        if(SessionState.GetBool(Key+".Regression",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string result=File.ReadAllText("Temp/ColonizationWeek9SmokeTest.result.txt");
            if(result.StartsWith("FAILED") || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".RegressionStarted",0)>1000) Finish("FAILED\nWeek 9 regression:\n"+result);
            else if(result.StartsWith("PASSED") && SceneManager.GetActiveScene().path=="Assets/Scenes/Map01.unity") { SessionState.SetBool(Key+".Regression",false); SessionState.SetBool(Key+".Start",true); }
        }
        if(SessionState.GetBool(Key+".Start",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !File.Exists("Temp/ColonizationEditorRefresh.request") && EditorApplication.timeSinceStartup-stableSince>10)
        {
            SessionState.SetBool(Key+".Start",false);
            try
            {
                Preflight(); Settings.ResetForNewGame(); SessionState.SetBool(Key+".Pending",true); SessionState.SetInt(Key+".Phase",0); SessionState.SetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup);
                if(!Application.isBatchMode) { Type type=typeof(Editor).Assembly.GetType("UnityEditor.GameView"); if(type!=null) { EditorWindow window=EditorWindow.GetWindow(type); window.Show(); window.Focus(); } }
                File.WriteAllText(Result,"RUNNING\nEntering Week 10–11 Play Mode.\n"); EditorApplication.isPlaying=true;
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
        if(state==PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key+".Pending",false); File.WriteAllText(Result,"FAILED\nPlay Mode exited before Week 10–11 completed.\n"); }
    }
    private static IEnumerator RuntimeLoop()
    {
        while(ready && Application.isPlaying && SessionState.GetBool(Key+".Pending",false))
        {
            yield return null; if(EditorApplication.isPaused) continue;
            try { if(error!=null) throw new InvalidOperationException(error); if(EditorApplication.timeSinceStartup-started>300) throw new TimeoutException("Week 10–11 phase "+phase); Step(); }
            catch(Exception e) { Finish("FAILED\nPhase "+phase+", operation "+operation+"\n"+e); }
        }
    }
    private static EuropeManager europe;
    private static Hex coast, home;
    private static int turnBefore, tripLeg, stockBefore;
    private static void Step()
    {
        if(ColonizationLegacyDialogFixture.AcknowledgeNotice()) return;
        frames++; if (frames < 8 || EditorApplication.timeSinceStartup - phaseStarted < .2) return;
        switch (phase)
        {
            case 0:
                manager=GameManager.instance; ui=UIManager.instance; europe=EuropeManager.instance;
                if(manager==null || ui==null || manager.AllHexes[0,0]==null) break;
                ColonizationWeek11Setup.Validate(manager,ui);
                Require(manager.PlayerFaction.Money==1000 && europe.EuropeStocks.All(s=>s.Quantity==500 && s.AskPrice>s.BidPrice && s.AskPrice<=20),"Market initialization failed.");
                Require(!DialogManager.instance.IsOpen && manager.Factions.Take(5).Select(f=>f.Units[0].CurHex.Y).Distinct().Count()==5,"Startup prompt or duplicate ship rows.");
                BuildFixture(); manager.SelectPlayerUnit(ship); manager.SetupCurrentTown(town); Next(1); break;
            case 1:
                TownOperations(); break;
            case 2:
                Require(ship.CargoList[0].Quantity==11 && ship.CargoList[1].Quantity==25,"Close refunded into the wrong hold.");
                manager.SetupCurrentTown(town); Next(3); break;
            case 3:
                BeginCargo(ui.CargoSlots[0]); ui.SelectShipInPort(ui.AllShipIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==galleon));
                Require(ship.CargoList[0].Quantity==11 && ui.CargoSlots.All(s=>s.gameObject.activeSelf),"Switching ships lost cargo escrow/capacity.");
                town.Warehouse[2]=120; ui.StockSlots[2].UpdateStockText(); Next(4); break;
            case 4:
                DragStockSlot(ui.StockSlots[2],ui.CargoSlots[5].gameObject);
                Require(galleon.CargoList.Count==6 && galleon.CargoList[5].Quantity==100 && ui.CargoSlots[5].CargoDrag.HoldId==5 && town.Warehouse[2]==20,"Sparse cargo icon/loading failed."); Next(5); break;
            case 5:
                DragCargo(ui.CargoSlots[5],ui.StockSlots[2].gameObject);
                Require(galleon.CargoList.Count==6 && galleon.CargoList[5].Quantity==0 && town.Warehouse[2]==120,"Sparse unload shifted holds or lost goods.");
                ui.SelectShipInPort(ui.AllShipIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==ship)); Next(6); break;
            case 6:
                DragCargo(ui.CargoSlots[0],ui.StockSlots[2].gameObject); Next(7); break;
            case 7:
                Require(town.Warehouse[2]==131,"Unloading after switching ship failed.");
                DragStockSlot(ui.StockSlots[0],ui.CargoSlots[0].gameObject);
                Require(ship.CargoList[0].ProductID==0 && ship.CargoList[0].Quantity==100,"Round-trip town load failed.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek11TownCargo.png"); Next(8); break;
            case 8:
                ui.ToggleTownPanel(false); PositionWithPassengers(ship,manager.AllHexes[48,20]); ship.MovePoint=20;
                ship.PrepareMoveToHex(manager.AllHexes[49,20]); Require(ship.IsMoving,"High seas approach did not animate."); Next(9); break;
            case 9:
                if(ship.IsMoving) break;
                Require(DialogManager.instance.IsOpen && europe.CurShip==ship,"Eastern arrival did not ask to sail.");
                turnBefore=manager.GameTurn; Vector3 cameraPosition=Camera.main.transform.position; Keys(InputKey.Backspace,InputKey.E,InputKey.Tab,InputKey.W,InputKey.Z); Next(10); break;
            case 10:
                Keys(); Require(manager.PlayerTurn && manager.GameTurn==turnBefore && !ui.InEurope,"Naval modal leaked keyboard input.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek11NavalDialog.png"); Next(11); break;
            case 11:
                Click(DialogManager.instance.NavalDialog.transform.Find("AnswerPanel/NoButton").gameObject);
                Require(!DialogManager.instance.IsOpen && europe.ShipsToEurope.Count==0 && ship.CurHex!=null,"Declining Europe lost ship.");
                ship.PrepareMoveToHex(manager.AllHexes[48,20]); Next(12); break;
            case 12:
                if(ship.IsMoving) break; ship.PrepareMoveToHex(manager.AllHexes[49,20]); Next(13); break;
            case 13:
                if(ship.IsMoving) break;
                Require(DialogManager.instance.IsOpen,"Return to edge did not reopen question."); Next(131); break;
            case 131:
                Click(DialogManager.instance.NavalDialog.transform.Find("AnswerPanel/YesButton").gameObject);
                Require(europe.ShipsToEurope.Count==1 && europe.ShipsToEurope[0].TurnLeft==3 && ship.CurHex==null && !ship.gameObject.activeSelf && ship.Passengers.All(p=>p.CurHex==null),"Departure did not preserve/remove ship and passenger map membership.");
                Require(manager.AllHexes.Cast<Hex>().All(h=>!h.UnitsInHex.Contains(ship) && ship.Passengers.All(p=>!h.UnitsInHex.Contains(p))),"Off-map occupants remain in Hex lists.");
                PositionWithPassengers(galleon,manager.AllHexes[48,24]); galleon.MovePoint=20; galleon.PrepareMoveToHex(manager.AllHexes[49,24]); Next(14); break;
            case 14:
                if(galleon.IsMoving) break;
                Require(DialogManager.instance.IsOpen,"Second ship did not open question."); Next(141); break;
            case 141:
                Click(DialogManager.instance.NavalDialog.transform.Find("AnswerPanel/YesButton").gameObject);
                Require(europe.ShipsToEurope.Count==2 && manager.CurUnit==null,"Second departure or no-map-unit selection failed.");
                Keys(InputKey.E); Next(15); break;
            case 15:
                Keys(); Require(ui.InEurope && ui.AllShipToEuropeIcons.Count==2 && ui.AllShipInEuropeIcons.Count==0 && ui.CargoSlotsEurope.All(s=>!s.gameObject.activeSelf),"Inbound Europe UI incorrect.");
                Require(ui.StockSlotsEurope[0].StockText.text=="2/8" && ui.StockSlotsEurope.All(s=>s.StockDrag!=null),"Market prices or drags missing.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek11Transit.png"); tripLeg=0; Next(16); break;
            case 16:
                turnBefore=manager.GameTurn; Keys(InputKey.Backspace); Next(17); break;
            case 17:
                Keys(); if(!manager.PlayerTurn) break;
                Require(manager.GameTurn==turnBefore+1 && !ui.InEurope,"Travel turn did not finish/close Europe."); tripLeg++;
                if(tripLeg<3) Require(europe.ShipsToEurope.Count==2 && europe.ShipsToEurope.All(t=>t.TurnLeft==3-tripLeg),"Inbound travel duration is not three turns.");
                else Require(europe.ShipsToEurope.Count==0 && europe.ShipsInEurope.Count==2,"Ships did not reach Europe after three turns.");
                Keys(InputKey.E); Next(18); break;
            case 18:
                Keys(); Require(ui.InEurope,"E cannot open Europe when all player units are off-map.");
                if(tripLeg<3) { Require(ui.AllShipToEuropeIcons.Count==2,"Transit icons duplicated/missing."); Next(16); }
                else { Click(ui.AllShipInEuropeIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==ship).ShipDrag.gameObject); operation=0; Next(19); } break;
            case 19:
                MarketOperations(); break;
            case 20:
                Require(ui.SelectedEuropeShip==ship && ship.CargoList[0].Quantity==100 && ship.CargoList[1].Quantity==7 && manager.PlayerFaction.Money==312,"Market accounting final balance incorrect.");
                BeginCargo(ui.CargoSlotsEurope[0]); Click(ui.AllShipInEuropeIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==galleon).ShipDrag.gameObject);
                Require(ship.CargoList[0].Quantity==100 && ui.CargoSlotsEurope.All(s=>s.gameObject.activeSelf),"Europe ship switch lost cargo or six-hold capacity."); Next(21); break;
            case 21:
                DragStockSlot(ui.StockSlotsEurope[1],ui.CargoSlotsEurope[5].gameObject);
                Require(galleon.CargoList[5].Quantity==62 && manager.PlayerFaction.Money==2 && europe.EuropeStocks[1].Quantity==463,"Sixth-hold market purchase/affordable remainder failed.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek11EuropeMarket.png"); Next(22); break;
            case 22:
                BeginStockSlot(ui.StockSlotsEurope[0]);
                ShipDrag icon=ui.AllShipInEuropeIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==ship).ShipDrag;
                DragShip(icon,null); Require(europe.ShipsInEurope.Count==2 && europe.EuropeStocks[0].Quantity==500,"Invalid ship drop or cancelling reserved stock lost state.");
                BeginShip(icon); ui.ToggleEuropePanel(false); Require(!icon.IsDragging && europe.ShipsInEurope.Count==2,"Closing Europe left a detached ship drag."); Keys(InputKey.E); Next(23); break;
            case 23:
                Keys(); Require(ui.AllShipInEuropeIcons.Count==2 && ui.StockSlotsEurope.All(s=>s.GetComponentsInChildren<StockDrag>(true).Length==1),"Europe reopening duplicated icons.");
                ShipDrag returning=ui.AllShipInEuropeIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==ship).ShipDrag;
                DragShip(returning,ui.EuropePanel.transform.Find("UnitPanel/OutboundPanel/ShipPanel").gameObject);
                Require(europe.ShipsInEurope.Count==1 && europe.ShipsFromEurope.Count==1 && europe.ShipsFromEurope[0].TurnLeft==3 && ui.AllShipFromEuropeIcons.Count==1 && ui.SelectedEuropeShip==galleon,"Outbound drag did not transfer the exact ship once.");
                europe.AllowToGoToNewWorld(ship); Require(europe.ShipsFromEurope.Count==1,"Duplicate outbound departure accepted.");
                tripLeg=0; Next(24); break;
            case 24:
                if(tripLeg==0) BeginCargo(ui.CargoSlotsEurope[5]);
                turnBefore=manager.GameTurn; Keys(InputKey.Backspace); Next(25); break;
            case 25:
                Keys(); if(!manager.PlayerTurn) break; tripLeg++;
                Require(manager.GameTurn==turnBefore+1 && galleon.CargoList[5].Quantity==62 && europe.ShipsInEurope.Contains(galleon),"Outbound turn lost the Europe ship's pending cargo.");
                if(tripLeg<3) { Require(ship.CurHex==null && europe.ShipsFromEurope.Single().TurnLeft==3-tripLeg,"Outbound travel duration incorrect."); Keys(InputKey.E); Next(26); }
                else
                {
                    Require(europe.ShipsFromEurope.Count==0 && ship.CurHex!=null && ship.CurHex.X==48 && ship.gameObject.activeSelf && manager.PlayerFaction.Units.Count(u=>u==ship)==1,"Return recreated/duplicated ship or used wrong column.");
                    Require(ship.Passengers.All(p=>p.CurHex==ship.CurHex && ship.CurHex.UnitsInHex.Count(u=>u==p)==1 && p.TransportShip==ship) && ship.CurHex.UnitsInHex.Count(u=>u==ship)==1,"Return passenger synchronization failed.");
                    Require(ship.CargoList[0].Quantity==100 && ship.CargoList[1].ProductID==3 && ship.CargoList[1].Quantity==7,"Return lost traded cargo.");
                    PositionWithPassengers(ship,coast); ship.MovePoint=20; ship.PrepareMoveToHex(home); Require(ship.IsMoving,"Returned ship cannot enter colony port."); Next(27);
                } break;
            case 26:
                Keys(); Require(ui.InEurope && ui.AllShipFromEuropeIcons.Count==1 && ui.AllShipInEuropeIcons.Count==1,"Outbound status list is stale."); Next(24); break;
            case 27:
                if(ship.IsMoving) break; manager.SetupCurrentTown(town); Next(28); break;
            case 28:
                stockBefore=town.Warehouse[0]; DragCargo(ui.CargoSlots[0],ui.StockSlots[0].gameObject);
                Require(town.Warehouse[0]==stockBefore+100 && ship.CargoList[0].Quantity==0,"Imported Food cannot unload in colony."); Next(29); break;
            case 29:
                DragCargo(ui.CargoSlots[1],ui.StockSlots[3].gameObject);
                Require(town.Warehouse[3]==7 && ship.CargoList[1].Quantity==0 && manager.PlayerFaction.Money==2,"Imported Cotton cannot unload or colony unload changed money.");
                Click(ui.TownPanel.transform.Find("NamePanel/ExitButton").gameObject); Keys(InputKey.E); Next(30); break;
            case 30:
                Keys(); for(int i=0;i<3;i++) { ui.ToggleEuropePanel(false); ui.ToggleEuropePanel(true); Require(ui.AllShipInEuropeIcons.Count==1 && ui.AllShipToEuropeIcons.Count==0 && ui.AllShipFromEuropeIcons.Count==0,"Repeated Europe opening duplicated ships."); }
                Click(ui.EuropePanel.transform.Find("NamePanel/ExitButton").gameObject);
                Require(!ui.InEurope && ui.AllShipInEuropeIcons.Count==0 && ui.CargoSlotsEurope.All(s=>!s.gameObject.activeSelf) && ui.StockSlotsEurope.All(s=>!s.StockDrag.IsDragging),"Europe exit cleanup failed.");
                Finish("PASSED\nWeek 10–11: actual EventSystem cargo load/unload, numbered sparse holds, invalid/mixed/cancelled drops and close/ship-switch refunds.\nAnimated high-seas approach, modal Yes/No and input blocking, both original ships/passengers leave map occupancy, no-unit map selection, exact three Backspace/AI turns each way, inbound/outbound counts, original ship identity and passenger/cargo membership on return, imported goods unload in colony.\nSixteen market stocks/prices, initial 1000 gold/500 units, matching-product selling at Bid, buying at Ask, same-product partial stacks, full/mixed/invalid rejection, insufficient funds and partially affordable purchases, small/exhausted stock, cancelled reservation conservation, two/six-hold selection, sparse sixth-hold purchase, outbound ship drag cancellation/close/duplicate guard, repeated reopen and complete cleanup. Final verified trade balance 2 gold; no cargo/money loss or duplicate ship.\nScreenshots: Temp/ColonizationWeek11TownCargo.png, Temp/ColonizationWeek11NavalDialog.png, Temp/ColonizationWeek11Transit.png, Temp/ColonizationWeek11EuropeMarket.png. Existing project art only. Runtime state discarded; Edit Mode restored.\n"); break;
        }
    }
    private static void TownOperations()
    {
        switch(operation)
        {
            case 0: DragStockSlot(ui.StockSlots[0],ui.CargoSlots[0].gameObject); Require(ship.CargoList[0].Quantity==100 && town.Warehouse[0]==100 && ui.CargoSlots[0].CargoDrag!=null,"Loaded cargo icon missing."); break;
            case 1: DragCargo(ui.CargoSlots[0],ui.StockSlots[1].gameObject); Require(ship.CargoList[0].Quantity==100 && town.Warehouse[1]==70,"Wrong stock accepted cargo."); break;
            case 2: DragCargo(ui.CargoSlots[0],ui.OutsideTownParent); Require(ship.CargoList[0].Quantity==100,"Outside town lost cargo."); break;
            case 3: DragCargo(ui.CargoSlots[0],ui.AreaSlots.First(s=>s.Hex!=null).gameObject); Require(ship.CargoList[0].Quantity==100,"Terrain accepted cargo."); break;
            case 4: DragCargo(ui.CargoSlots[0],ui.StockSlots[0].gameObject); Require(ship.CargoList.Count==2 && ship.CargoList[0].Quantity==0 && ship.CargoList[1].Quantity==25 && town.Warehouse[0]==200,"Unload shifted numbered holds."); break;
            case 5: DragStockSlot(ui.StockSlots[2],ui.CargoSlots[0].gameObject); Require(ship.CargoList[0].ProductID==2 && ship.CargoList[0].Quantity==11,"Small product reload failed."); break;
            case 6: BeginCargo(ui.CargoSlots[1]); ui.ToggleTownPanel(false); Require(ship.CargoList[1].Quantity==25,"Closing pending cargo lost stock."); Next(2); return;
        }
        operation++; Next(1);
    }
    private static void MarketOperations()
    {
        switch(operation)
        {
            case 0: Require(ui.CargoSlotsEurope.Count(s=>s.gameObject.activeSelf)==2 && ui.CargoSlotsEurope[0].CargoDrag!=null,"Europe first ship cargo missing."); DragCargo(ui.CargoSlotsEurope[0],ui.StockSlotsEurope[1].gameObject); Require(manager.PlayerFaction.Money==1000 && ship.CargoList[0].Quantity==100,"Mismatched market sale accepted."); break;
            case 1: DragCargo(ui.CargoSlotsEurope[0],ui.StockSlotsEurope[0].gameObject); Require(manager.PlayerFaction.Money==1200 && europe.EuropeStocks[0].Quantity==600 && ship.CargoList[1].Quantity==25,"Bid sale/accounting failed."); break;
            case 2: DragStockSlot(ui.StockSlotsEurope[1],ui.CargoSlotsEurope[1].gameObject); Require(manager.PlayerFaction.Money==825 && europe.EuropeStocks[1].Quantity==425 && ship.CargoList[1].Quantity==100,"Partial-stack Ask charged rejected remainder."); break;
            case 3: DragStockSlot(ui.StockSlotsEurope[0],ui.CargoSlotsEurope[0].gameObject); Require(manager.PlayerFaction.Money==25 && ship.CargoList[0].Quantity==100 && europe.EuropeStocks[0].Quantity==500,"Ask purchase/accounting failed."); break;
            case 4: DragStockSlot(ui.StockSlotsEurope[0],ui.CargoSlotsEurope[0].gameObject); Require(manager.PlayerFaction.Money==25 && europe.EuropeStocks[0].Quantity==500,"Full-hold purchase charged money."); break;
            case 5: DragStockSlot(ui.StockSlotsEurope[2],ui.CargoSlotsEurope[1].gameObject); Require(manager.PlayerFaction.Money==25 && europe.EuropeStocks[2].Quantity==500,"Mixed-hold purchase charged money."); break;
            case 6: DragStockSlot(ui.StockSlotsEurope[0],null); Require(manager.PlayerFaction.Money==25 && europe.EuropeStocks[0].Quantity==500,"Cancelled market reservation lost stock/money."); break;
            case 7: DragCargo(ui.CargoSlotsEurope[1],ui.StockSlotsEurope[1].gameObject); Require(manager.PlayerFaction.Money==425 && europe.EuropeStocks[1].Quantity==525,"Second sale incorrect."); break;
            case 8: DragStockSlot(ui.StockSlotsEurope[2],ui.CargoSlotsEurope[1].gameObject); Require(manager.PlayerFaction.Money==0 && ship.CargoList[1].Quantity==85 && europe.EuropeStocks[2].Quantity==415,"Partially affordable buy went negative or lost remainder."); break;
            case 9: DragStockSlot(ui.StockSlotsEurope[2],ui.CargoSlotsEurope[1].gameObject); Require(manager.PlayerFaction.Money==0 && ship.CargoList[1].Quantity==85 && europe.EuropeStocks[2].Quantity==415,"Insufficient money bought goods."); break;
            case 10: DragCargo(ui.CargoSlotsEurope[1],ui.StockSlotsEurope[2].gameObject); Require(manager.PlayerFaction.Money==340 && europe.EuropeStocks[2].Quantity==500,"Reselling affordable lot incorrect."); europe.EuropeStocks[3].Quantity=7; break;
            case 11: DragStockSlot(ui.StockSlotsEurope[3],ui.CargoSlotsEurope[1].gameObject); Require(manager.PlayerFaction.Money==312 && europe.EuropeStocks[3].Quantity==0 && ship.CargoList[1].Quantity==7,"Small/exhausted market stock incorrect."); break;
            case 12: StockDrag drag=ui.StockSlotsEurope[3].StockDrag; ExecuteEvents.Execute(drag.gameObject,Data(drag.gameObject),ExecuteEvents.beginDragHandler); Require(!drag.IsDragging && manager.PlayerFaction.Money==312,"Empty stock started a purchase."); Next(20); return;
        }
        operation++; Next(19);
    }
    private static void BuildFixture()
    {
        ship=manager.PlayerFaction.Units.OfType<NavalUnit>().Single();
        coast=manager.AllHexes.Cast<Hex>().First(h=>h.HexType==HexType.Ocean && HexCalculator.GetHexAround(manager.AllHexes,h).Any(n=>!n.HasTown && n.HexType!=HexType.Ocean && n.HexType!=HexType.Mountains));
        home=HexCalculator.GetHexAround(manager.AllHexes,coast).First(n=>!n.HasTown && n.HexType!=HexType.Ocean && n.HexType!=HexType.Mountains); home.ClearForest(); home.ResourceYield=new[]{5,0,0,0,0,0,0,0};
        manager.GenerateTown(manager.PlayerFaction,home); town=home.Town; town.Warehouse[0]=150; town.Warehouse[1]=70; town.Warehouse[2]=11;
        PositionWithPassengers(ship,home); ship.AddCargo(0,new Cargo(0,50)); ship.AddCargo(1,new Cargo(1,25));
        GameObject prefab=(GameObject)new SerializedObject(manager).FindProperty("navalUnitPrefab").objectReferenceValue;
        galleon=Object.Instantiate(prefab,manager.PlayerFaction.UnitParent).GetComponent<NavalUnit>(); galleon.UnitInit(manager,manager.PlayerFaction,manager.NavalUnitData[2]); galleon.SetupPosition(home); manager.PlayerFaction.Units.Add(galleon);
    }
    private static void PositionWithPassengers(NavalUnit vessel,Hex hex)
    { vessel.SetupPosition(hex); foreach(LandUnit rider in vessel.Passengers) { rider.SetupPosition(hex); rider.gameObject.SetActive(false); } }
    private static void DragStockSlot(StockSlot slot,GameObject target)
    { StockDrag drag=BeginStockSlot(slot); DropAndEnd(drag.gameObject,target); }
    private static StockDrag BeginStockSlot(StockSlot slot)
    { StockDrag drag=slot.StockDrag; Begin(drag.gameObject); Require(drag.IsDragging,"Stock begin rejected "+slot.ProductId); return drag; }
    private static CargoDrag BeginCargo(CargoSlot slot)
    { Require(slot.CargoDrag!=null,"Cargo icon missing at hold "+slot.HoldId); CargoDrag drag=slot.CargoDrag; Begin(drag.gameObject); Require(drag.IsDragging,"Cargo begin rejected "+slot.HoldId); return drag; }
    private static void DragCargo(CargoSlot slot,GameObject target) { CargoDrag drag=BeginCargo(slot); DropAndEnd(drag.gameObject,target); }
    private static void BeginShip(ShipDrag drag) { Begin(drag.gameObject); Require(drag.IsDragging,"Ship begin rejected."); }
    private static void DragShip(ShipDrag drag,GameObject target) { BeginShip(drag); DropAndEnd(drag.gameObject,target); }
    private static void Begin(GameObject obj)
    {
        Canvas.ForceUpdateCanvases(); GameObject hit=Hit(obj.transform.position,true);
        Require(hit!=null && ExecuteEvents.GetEventHandler<IBeginDragHandler>(hit)==obj,"Begin raycast missed "+obj.name+"; hit="+(hit!=null?hit.name:"none"));
        ExecuteEvents.Execute(obj,Data(obj),ExecuteEvents.beginDragHandler);
    }
    private static void DropAndEnd(GameObject obj,GameObject target)
    {
        PointerEventData data=Data(obj);
        if(target!=null)
        {
            data.position=target.transform.position; ExecuteEvents.Execute(obj,data,ExecuteEvents.dragHandler); Canvas.ForceUpdateCanvases();
            GameObject hit=Hit(data.position,true), drop=hit!=null?ExecuteEvents.GetEventHandler<IDropHandler>(hit):null;
            if(drop!=null) ExecuteEvents.Execute(drop,data,ExecuteEvents.dropHandler);
        }
        if(obj!=null) ExecuteEvents.Execute(obj,data,ExecuteEvents.endDragHandler);
    }
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
    private static void Keys(params InputKey[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
    private static void Next(int next) { phase=next; frames=0; phaseStarted=EditorApplication.timeSinceStartup; SessionState.SetFloat(Key+".LastProgress",(float)phaseStarted); SessionState.SetInt(Key+".Phase",phase); File.WriteAllText(Result,"RUNNING\nWeek 10–11 phase "+phase+"; stock operation "+operation+".\n"); }
    private static void RestoreInput()
    {
        InputDevice device=InputSystem.GetDeviceById(SessionState.GetInt(Key+".Keyboard",-1)); if(device!=null) InputSystem.RemoveDevice(device); SessionState.SetInt(Key+".Keyboard",-1);
        if(!SessionState.GetBool(Key+".InputSaved",false)) return;
        InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key+".OldInput",0); InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Key+".OldBackground",0); Application.runInBackground=SessionState.GetBool(Key+".OldRun",false); SessionState.SetBool(Key+".InputSaved",false);
    }
    private static void Finish(string result)
    {
        if(ui!=null) ui.CheckActiveUIPanel(); RestoreInput(); ready=false; SessionState.SetBool(Key+".Pending",false); SessionState.SetBool(Key+".Regression",false); SessionState.SetBool(Key+".Start",false); File.WriteAllText(Result,result); if(EditorApplication.isPlaying) EditorApplication.isPlaying=false;
    }
    private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
}
