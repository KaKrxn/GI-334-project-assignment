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
using Object=UnityEngine.Object;
using InputKey=UnityEngine.InputSystem.Key;

public static class ColonizationWeek13SmokeTest
{
    public const string Request="Temp/ColonizationWeek13SmokeTest.v1.request";
    public const string Result="Temp/ColonizationWeek13SmokeTest.result.txt";
    private const string Key="Colonization.Week13Smoke";
    private static double stableSince,initialized,started,phaseStarted;
    private static int phase,frames,operation;
    private static bool ready;
    private static string error;
    private static GameManager manager;
    private static UIManager ui;
    private static NavalUnit ship;
    private static Keyboard keyboard;

    [InitializeOnLoadMethod] private static void Initialize()
    {
        initialized=stableSince=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=PlayChanged;
        Application.logMessageReceived+=(message,stack,type)=> { if(SessionState.GetBool(Key+".Pending",false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) error=message+"\n"+stack; };
    }
    [MenuItem("Tools/Colonization/Run Week 13 Play Mode Smoke Test")]
    public static void Run()
    {
        Preflight(); SessionState.SetBool(Key+".Regression",true); SessionState.SetFloat(Key+".RegressionStarted",(float)EditorApplication.timeSinceStartup);
        File.WriteAllText(Result,"RUNNING\nWeek 5–12 behavioral regression suites first.\n"); ColonizationWeek12SmokeTest.Run();
    }
    private static void Preflight()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount!=1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode for Week 13 validation.");
        ColonizationWeek13Setup.Validate(Object.FindAnyObjectByType<GameManager>(),Object.FindAnyObjectByType<UIManager>());
    }
    private static void Tick()
    {
        if(SessionState.GetBool(Key+".Pending",false) && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPaused)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if((!ready && EditorApplication.timeSinceStartup-initialized>5) || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup)>60)
            { Finish("FAILED\nWeek 13 player loop/reload interrupted phase "+SessionState.GetInt(Key+".Phase",0)); return; }
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) stableSince=EditorApplication.timeSinceStartup;
        if(File.Exists(Request) && !File.Exists(ColonizationWeek13Setup.Request) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string mode=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try { Preflight(); if(mode=="extend") { Require(File.Exists("Temp/ColonizationWeek12SmokeTest.result.txt") && File.ReadAllText("Temp/ColonizationWeek12SmokeTest.result.txt").StartsWith("PASSED"),"A passed Week 12 regression is required."); SessionState.SetBool(Key+".Start",true); } else Run(); }
            catch(Exception e) { Finish("FAILED\n"+e); }
        }
        if(SessionState.GetBool(Key+".Regression",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string result=File.ReadAllText("Temp/ColonizationWeek12SmokeTest.result.txt");
            if(result.StartsWith("FAILED") || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".RegressionStarted",0)>1000) Finish("FAILED\nWeek 12 regression:\n"+result);
            else if(result.StartsWith("PASSED") && SceneManager.GetActiveScene().path=="Assets/Scenes/Map01.unity") { SessionState.SetBool(Key+".Regression",false); SessionState.SetBool(Key+".Start",true); }
        }
        if(SessionState.GetBool(Key+".Start",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !File.Exists("Temp/ColonizationEditorRefresh.request") && EditorApplication.timeSinceStartup-stableSince>10)
        {
            SessionState.SetBool(Key+".Start",false);
            try
            {
                Preflight(); Settings.ResetForNewGame(); SessionState.SetBool(Key+".Pending",true); SessionState.SetInt(Key+".Phase",0); SessionState.SetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup);
                if(!Application.isBatchMode) { Type type=typeof(Editor).Assembly.GetType("UnityEditor.GameView"); if(type!=null) { EditorWindow window=EditorWindow.GetWindow(type); window.Show(); window.Focus(); } }
                File.WriteAllText(Result,"RUNNING\nEntering Week 13 Play Mode.\n"); EditorApplication.isPlaying=true;
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
        if(state==PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key+".Pending",false); File.WriteAllText(Result,"FAILED\nPlay Mode exited before Week 13 completed.\n"); }
    }
    private static IEnumerator RuntimeLoop()
    {
        while(ready && Application.isPlaying && SessionState.GetBool(Key+".Pending",false))
        {
            yield return null; if(EditorApplication.isPaused) continue;
            try { if(error!=null) throw new InvalidOperationException(error); if(EditorApplication.timeSinceStartup-started>300) throw new TimeoutException("Week 13 phase "+phase); Step(); }
            catch(Exception e) { Finish("FAILED\nPhase "+phase+", operation "+operation+"\n"+e); }
        }
    }
    private static EuropeManager europe;
    private static readonly List<NavalUnit> purchases = new List<NavalUnit>();
    private static readonly List<LandUnit> trainees = new List<LandUnit>();
    private static readonly List<LandUnit> manifest = new List<LandUnit>();
    private static LandUnit ordered;
    private static int gold, turnBefore, leg;
    private static Hex portHex, coastHex;
    private static Town town;
    private static Dictionary<NavalUnit,Hex> aiOrigins;
    private static void Step()
    {
        frames++; if(frames<8 || EditorApplication.timeSinceStartup-phaseStarted<.2) return;
        switch(phase)
        {
            case 0:
                manager=GameManager.instance; ui=UIManager.instance; europe=EuropeManager.instance;
                if(manager==null || manager.AllHexes[0,0]==null) break;
                ColonizationWeek13Setup.Validate(manager,ui); purchases.Clear(); trainees.Clear(); manifest.Clear();
                Require(manager.Factions.Where(f=>f!=manager.PlayerFaction && f.IsEuropean).SelectMany(f=>f.Units.OfType<NavalUnit>()).All(s=>s.DestinationHex!=null && s.DestinationHex.Y==s.CurHex.Y && s.DestinationHex.HexType!=HexType.Ocean),"European AI coastal destinations missing.");
                Click(DialogManager.instance.ActiveDialog.OkayButton.gameObject); foreach(Faction faction in manager.Factions) faction.MetByPlayer=true;
                ship=manager.PlayerFaction.Units.OfType<NavalUnit>().Single(); Keys(InputKey.E); Next(1); break;
            case 1:
                Keys(); Require(ui.InEurope && manager.PlayerFaction.Money==1000,"Europe initial state incorrect.");
                Click(ui.EuropePanel.transform.Find("ButtonPanel/PURCHASEButton").gameObject); Next(201); break;
            case 201:
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13Purchase.png"); Next(2); break;
            case 2:
                Require(ui.PurchaseShipPanel.activeSelf && ui.UnitMenuOpen,"Purchase menu missing.");
                Click(ui.PurchaseShipPanel.transform.Find("SelectPanel/Ship4").gameObject);
                Require(europe.ShipsInEurope.Count==0 && manager.PlayerFaction.Money==1000 && ui.PurchaseShipPanel.activeSelf,"Unaffordable ship changed money/units.");
                ui.AddNewShipInEurope(-1); Require(europe.ShipsInEurope.Count==0,"Invalid ship ID accepted.");
                manager.PlayerFaction.Money=50000; gold=50000; operation=0; Click(ui.PurchaseShipPanel.transform.Find("SelectPanel/Ship0").gameObject); Next(3); break;
            case 3:
                NavalUnit bought=ui.SelectedEuropeShip; gold-=manager.NavalUnitData[operation].price; purchases.Add(bought);
                Require(bought!=null && bought.NavalUnitType==manager.NavalUnitData[operation].navalUnitType && bought.UnitStatus==UnitStatus.Hidden && bought.CurHex==null && !bought.gameObject.activeSelf && bought.Passengers.Count==0 && manager.PlayerFaction.Money==gold && manager.PlayerFaction.Units.Count(u=>u==bought)==1,"Purchased ship type/price/hidden ownership failed.");
                Require(ui.CargoSlotsEurope.Count(s=>s.gameObject.activeSelf)==bought.CargoHoldNum,"Purchased ship hold selection incorrect.");
                operation++;
                if(operation<5) { Click(ui.EuropePanel.transform.Find("ButtonPanel/PURCHASEButton").gameObject); Next(4); }
                else { operation=0; Click(ui.EuropePanel.transform.Find("ButtonPanel/TRAINButton").gameObject); Next(5); } break;
            case 4:
                Click(ui.PurchaseShipPanel.transform.Find("SelectPanel/Ship"+operation).gameObject); Next(3); break;
            case 5:
                Require(ui.TrainUnitPanel.activeSelf,"Train menu missing."); ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13Train.png"); Next(6); break;
            case 6:
                ScrollToTrainee(operation); Next(7); break;
            case 7:
                int id=ColonizationWeek13Setup.TrainIds[operation]; Click(ui.TrainUnitPanel.transform.Find("SelectPanel/Scroll View/Viewport/Content/Train"+id).gameObject);
                LandUnit trained=europe.LandUnitsInEurope.Last(); trainees.Add(trained); gold-=manager.LandUnitData[id].price;
                Require(trained.LandUnitType==manager.LandUnitData[id].landUnitType && trained.UnitStatus==UnitStatus.Hidden && trained.CurHex==null && !trained.gameObject.activeSelf && manager.PlayerFaction.Money==gold && ui.WaitingLandUnitsInEuropeIcons.Count==operation+1,"Trained specialist type/price/hidden ownership failed.");
                operation++; if(operation<ColonizationWeek13Setup.TrainIds.Length) { Click(ui.EuropePanel.transform.Find("ButtonPanel/TRAINButton").gameObject); Next(6); } else Next(8); break;
            case 8:
                Require(europe.LandUnitsInEurope.Count==10 && manager.PlayerFaction.Units.Count==18 && ui.WaitingLandUnitsInEuropeIcons.Count==10,"Unit creation duplicated/lost ownership.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13Waiting.png"); Next(81); break;
            case 81:
                manager.PlayerFaction.Money=0; ui.UpdateMoneyTexts(); Click(ui.EuropePanel.transform.Find("ButtonPanel/TRAINButton").gameObject); ScrollToTrainee(0); Next(9); break;
            case 9:
                Click(ui.TrainUnitPanel.transform.Find("SelectPanel/Scroll View/Viewport/Content/Train7").gameObject); ui.CreateUnitDragInEuropePort(-1);
                Require(europe.LandUnitsInEurope.Count==10 && manager.PlayerFaction.Money==0 && ui.TrainUnitPanel.activeSelf,"Unaffordable/invalid training accepted.");
                Keys(InputKey.Escape); Next(10); break;
            case 10:
                Keys(); Require(ui.InEurope && !ui.UnitMenuOpen,"Escape did not close only the unit menu.");
                ordered=europe.LandUnitsInEurope[0]; RightUnit(ordered); Next(111); break;
            case 111:
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13Orders.png"); Next(11); break;
            case 11:
                Require(ui.OrderPanel.activeSelf && ui.CurLandUnit==ordered,"Right-click order menu missing.");
                Click(ui.OrderPanel.transform.Find("SelectPanel/Order1").gameObject); Require(ordered.UnitStatus==UnitStatus.ToBoard && Icon(ordered).StatusIcon.gameObject.activeSelf,"Board order/icon missing."); Next(12); break;
            case 12: RightUnit(ordered); Next(13); break;
            case 13:
                Click(ui.OrderPanel.transform.Find("SelectPanel/Order0").gameObject); Require(ordered.UnitStatus==UnitStatus.None && !Icon(ordered).StatusIcon.gameObject.activeSelf,"None order did not clear boarding icon.");
                ordered=europe.LandUnitsInEurope.Last(); Next(14); break;
            case 14: RightUnit(ordered); Next(15); break;
            case 15:
                Click(ui.OrderPanel.transform.Find("SelectPanel/Order2").gameObject); Require(europe.LandUnitsInEurope[0]==ordered && ui.WaitingLandUnitsInEuropeIcons[0].GetComponent<UnitDrag>().LandUnit==ordered,"Move-to-front queue/icon order incorrect."); operation=0; Next(16); break;
            case 16: ordered=europe.LandUnitsInEurope[operation]; RightUnit(ordered); Next(17); break;
            case 17:
                Click(ui.OrderPanel.transform.Find("SelectPanel/Order1").gameObject); operation++;
                if(operation<7) Next(16); else
                {
                    manifest.AddRange(europe.LandUnitsInEurope.Take(7));
                    purchases[0].AddCargo(0,new Cargo(0,100)); purchases[0].AddCargo(1,new Cargo(1,100));
                    purchases[2].AddCargo(5,new Cargo(2,80)); ui.SelectShipInEurope(purchases[2]); Next(18);
                } break;
            case 18:
                Require(purchases[2].CargoList.Count==6 && ui.CargoSlotsEurope[5].CargoDrag!=null,"Sparse sixth hold missing.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13Boarding.png"); Next(181); break;
            case 181:
                Send(purchases[0]);
                Require(purchases[0].Passengers.Count==0 && europe.LandUnitsInEurope.Count==10,"Full cargo ship boarded passengers."); Next(19); break;
            case 19:
                Send(purchases[2]); Require(purchases[2].Passengers.SequenceEqual(manifest.Take(5)) && europe.LandUnitsInEurope.Count==5,"Combined hold capacity or FIFO boarding failed for sparse cargo."); Next(20); break;
            case 20:
                Send(purchases[1]); Require(purchases[1].Passengers.SequenceEqual(manifest.Skip(5)) && europe.LandUnitsInEurope.Count==3 && ui.WaitingLandUnitsInEuropeIcons.Count==3,"Overflow passengers did not board the next ship.");
                Require(manifest.All(p=>p.TransportShip!=null && p.UnitStatus==UnitStatus.OnBoard && p.CurHex==null && !p.gameObject.activeSelf),"Europe passenger links/status invalid.");
                leg=0; aiOrigins=manager.Factions.Where(f=>f!=manager.PlayerFaction && f.IsEuropean).SelectMany(f=>f.Units.OfType<NavalUnit>()).ToDictionary(s=>s,s=>s.CurHex); Next(21); break;
            case 21:
                turnBefore=manager.GameTurn; Keys(InputKey.Backspace); Next(22); break;
            case 22:
                Keys(); if(!manager.PlayerTurn) break; leg++;
                Require(manager.GameTurn==turnBefore+1 && !AIManager.instance.IsRunning && manager.Factions.SelectMany(f=>f.Units).All(u=>!u.IsMoving),"Turn handoff preceded destination arrivals.");
                if(leg<3) { Require(europe.ShipsFromEurope.Count==3 && europe.ShipsFromEurope.All(t=>t.TurnLeft==3-leg),"New ship transit duration incorrect."); Next(21); }
                else
                {
                    Require(europe.ShipsFromEurope.Count==0 && purchases.Take(3).All(s=>s.CurHex!=null && s.CurHex.X==48 && s.UnitStatus==UnitStatus.None && s.gameObject.activeSelf),"Purchased ships did not return as original types/objects.");
                    Require(manifest.All(p=>p.CurHex==p.TransportShip.CurHex && p.CurHex.UnitsInHex.Count(u=>u==p)==1 && manager.PlayerFaction.Units.Count(u=>u==p)==1),"Trained passenger arrival occupancy failed.");
                    Require(aiOrigins.Any(p=>p.Key.CurHex.X<p.Value.X-1) && aiOrigins.All(p=>p.Key.MovePoint>=0 && p.Key.Passengers.All(r=>r.CurHex==p.Key.CurHex)),"Destination AI did not chain moves or lost passengers.");
                    TestDirectionBoundaries(); PlacePortFixture(); Next(23);
                } break;
            case 23:
                Require(ui.IsTownPanelOpen,"Town order fixture did not open."); RightUnit(ordered); Next(24); break;
            case 24:
                Click(ui.OrderPanel.transform.Find("SelectPanel/Order1").gameObject); Require(ordered.UnitStatus==UnitStatus.ToBoard && Icon(ordered).StatusIcon.gameObject.activeSelf,"Town boarding order missing.");
                ui.ToggleTownPanel(false); purchases[1].MovePoint=20; purchases[1].PrepareMoveToHex(portHex); Require(purchases[1].IsMoving,"Ship cannot approach friendly port."); Next(25); break;
            case 25:
                if(purchases[1].IsMoving) break;
                Require(ordered.TransportShip==purchases[1] && ordered.UnitStatus==UnitStatus.OnBoard && !ordered.gameObject.activeSelf && purchases[1].Passengers.Count==2,"Arriving town ship did not board waiting ordered unit.");
                // The clearing/building icon is driven by authoritative status changes.
                LandUnit pioneer=trainees.Single(u=>u.LandUnitType==LandUnitType.HardyPioneers);
                pioneer.UnitStatus=UnitStatus.Clearing; Require(pioneer.StatusSprite.gameObject.activeSelf && pioneer.StatusSprite.sprite==pioneer.StatusSpritesList[0],"Clearing status icon missing."); pioneer.UnitStatus=UnitStatus.Building; Require(pioneer.StatusSprite.sprite==pioneer.StatusSpritesList[1],"Building status icon missing."); pioneer.UnitStatus=pioneer.TransportShip!=null?UnitStatus.OnBoard:UnitStatus.None; Require(!pioneer.StatusSprite.gameObject.activeSelf,"Status icon not cleared.");
                purchases[2].SetupPosition(manager.AllHexes[48,15]); foreach(LandUnit rider in purchases[2].Passengers) rider.SetupPosition(purchases[2].CurHex);
                manager.SelectPlayerUnit(purchases[2]); purchases[2].MovePoint=20; purchases[2].PrepareMoveToHex(manager.AllHexes[49,15]); Next(26); break;
            case 26:
                if(purchases[2].IsMoving || !DialogManager.instance.IsEuropeQuestion) break;
                Next(261); break;
            case 261:
                Click(DialogManager.instance.NavalDialog.YesButton.gameObject); Require(europe.ShipsToEurope.Count==1 && purchases[2].CurHex==null,"Purchased ship cannot sail back to Europe."); leg=0; Next(27); break;
            case 27:
                turnBefore=manager.GameTurn; Keys(InputKey.Backspace); Next(28); break;
            case 28:
                Keys(); if(!manager.PlayerTurn) break; leg++;
                if(leg<3) { Require(europe.ShipsToEurope.Single().TurnLeft==3-leg,"Return to Europe duration incorrect."); Next(27); }
                else
                {
                    Require(europe.ShipsToEurope.Count==0 && europe.ShipsInEurope.Contains(purchases[2]) && purchases[2].Passengers.Count==0 && europe.LandUnitsInEurope.Count==8,"Europe arrival did not disembark passengers into waiting queue.");
                    Require(manifest.Take(5).All(p=>p.TransportShip==null && p.CurHex==null && p.UnitStatus==UnitStatus.None && europe.LandUnitsInEurope.Contains(p) && p.transform.parent==manager.PlayerFaction.UnitParent) && purchases[2].CargoList[5].Quantity==80,"Disembarking lost passenger identity/ownership/cargo.");
                    Keys(InputKey.E); Next(29);
                } break;
            case 29:
                Keys(); Require(ui.WaitingLandUnitsInEuropeIcons.Count==8 && ui.AllShipInEuropeIcons.Count==3,"Return port icons incorrect.");
                ui.SelectShipInEurope(purchases[2]); ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek13Returned.png"); Next(291); break;
            case 291:
                ui.ToggleEuropePanel(false); Require(ui.WaitingLandUnitsInEuropeIcons.Count==0 && !ui.UnitMenuOpen,"Closing Europe leaked unit menus/icons."); ui.ToggleEuropePanel(true); Next(30); break;
            case 30:
                Require(ui.WaitingLandUnitsInEuropeIcons.Count==8 && manager.PlayerFaction.Units.Count==18 && manager.AllHexes.Cast<Hex>().All(h=>h.UnitsInHex.All(u=>u!=null && !u.IsDead && u.CurHex==h)),"Reopening duplicated units or off-map occupancy remains.");
                Finish("PASSED\nWeek 13: five native ship purchase buttons/types/prices/hold selection, ten scrollable specialist buttons/types/prices, insufficient funds/invalid IDs, hidden ownership without map occupancy; real UI right-click order panel and all three commands, queue/icon reorder and boarding icon. Full cargo departure boards nobody; sparse sixth-hold cargo plus five FIFO passengers fills Galleon, overflow boards the next Merchantman. Three actual Backspace/AI turns preserve purchased ship types, passenger identities, map membership and cargo on return. Town boarding order and animated friendly-port boarding work. Clearing/building status icons update and clear.\nEuropean AI has coastal targets, chains multiple animated ocean moves within points, handles map/null/reached/zero-point boundaries, preserves passengers and finishes before player handoff. Purchased Galleon and five trained passengers sail back through real naval confirmation and three further Backspace turns, disembark into Europe queue with identities/ownership/cargo intact. Reopening/closing cleans icons and modals. Week 8 regression covers all three introduction panels for every nation. Existing project assets only; runtime fixtures discarded, Edit Mode restored.\n"); break;
        }
    }
    private static void ScrollToTrainee(int n)
    {
        UnityEngine.UI.ScrollRect scroll=ui.TrainUnitPanel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(); Canvas.ForceUpdateCanvases();
        float range=Mathf.Max(1,scroll.content.rect.height-scroll.viewport.rect.height); scroll.StopMovement(); scroll.verticalNormalizedPosition=1-Mathf.Clamp01(n*120f/range); Canvas.ForceUpdateCanvases();
    }
    private static UnitDrag Icon(LandUnit unit) { return (ui.InEurope?ui.WaitingLandUnitsInEuropeIcons:ui.AllUnitDrags).Select(o=>o.GetComponent<UnitDrag>()).Single(i=>i.LandUnit==unit); }
    private static void RightUnit(LandUnit unit)
    {
        UnitDrag icon=Icon(unit); Canvas.ForceUpdateCanvases(); Vector3[] corners=new Vector3[4]; ((RectTransform)icon.transform).GetWorldCorners(corners);
        for(int y=1;y<10;y++) for(int x=1;x<10;x++)
        {
            Vector2 point=Vector2.Lerp(corners[0],corners[2],0); point=new Vector2(Mathf.Lerp(corners[0].x,corners[2].x,x/10f),Mathf.Lerp(corners[0].y,corners[2].y,y/10f));
            GameObject hit=Hit(point,true); if(hit==null || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit)!=icon.gameObject) continue;
            ExecuteEvents.Execute(icon.gameObject,new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Right},ExecuteEvents.pointerClickHandler); Require(ui.OrderPanel.activeSelf,"Order click rejected."); return;
        }
        throw new InvalidOperationException("No raycast-visible point for waiting unit "+unit.UnitName);
    }
    private static void Send(NavalUnit vessel)
    {
        ShipDrag drag=ui.AllShipInEuropeIcons.Select(o=>o.GetComponent<ShipInPort>()).Single(i=>i.NavalUnit==vessel).ShipDrag;
        Canvas.ForceUpdateCanvases(); GameObject hit=Hit(drag.transform.position,true); Require(hit!=null && ExecuteEvents.GetEventHandler<IBeginDragHandler>(hit)==drag.gameObject,"Ship drag raycast missed.");
        ExecuteEvents.Execute(drag.gameObject,Data(drag.gameObject),ExecuteEvents.beginDragHandler); Require(drag.IsDragging,"Ship begin rejected.");
        PointerEventData data=Data(drag.gameObject); data.position=ui.EuropePanel.transform.Find("UnitPanel/OutboundPanel/ShipPanel").position; ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.dragHandler); Canvas.ForceUpdateCanvases();
        hit=Hit(data.position,true); GameObject drop=hit!=null?ExecuteEvents.GetEventHandler<IDropHandler>(hit):null; Require(drop!=null,"Outbound drop raycast missed."); ExecuteEvents.Execute(drop,data,ExecuteEvents.dropHandler);
        if(drag!=null) ExecuteEvents.Execute(drag.gameObject,data,ExecuteEvents.endDragHandler);
    }
    private static void TestDirectionBoundaries()
    {
        NavalUnit vessel=purchases[0]; Hex origin=vessel.CurHex; int points=vessel.MovePoint; vessel.MovePoint=1;
        Require(HexCalculator.FindNextHexToGo(vessel,manager.AllHexes[49,0],manager.AllHexes[49,59],manager.AllHexes)!=null,"Map corner direction failed.");
        Require(HexCalculator.FindNextHexToGo(vessel,origin,origin,manager.AllHexes)==null && HexCalculator.FindNextHexToGo(vessel,origin,null,manager.AllHexes)==null,"Reached/null target did not stop.");
        vessel.MovePoint=0; Require(HexCalculator.FindNextHexToGo(vessel,origin,manager.AllHexes[49,0],manager.AllHexes)==null,"Zero points found a move."); vessel.MovePoint=points;
    }
    private static void PlacePortFixture()
    {
        portHex=manager.AllHexes.Cast<Hex>().First(h=>h.HexType!=HexType.Ocean && h.HexType!=HexType.Mountains && !h.HasForest && !h.HasTown && !HexCalculator.CheckIfHexAroundHasTown(manager.AllHexes,h) && HexCalculator.GetHexAround(manager.AllHexes,h).Any(n=>n.HexType==HexType.Ocean));
        manager.GenerateTown(manager.PlayerFaction,portHex); town=portHex.Town; coastHex=HexCalculator.GetHexAround(manager.AllHexes,portHex).First(h=>h.HexType==HexType.Ocean);
        NavalUnit vessel=purchases[1]; ordered=vessel.Passengers[0]; vessel.Passengers.Remove(ordered); ordered.TransportShip=null; ordered.transform.SetParent(manager.PlayerFaction.UnitParent,true); ordered.UnitStatus=UnitStatus.None; ordered.SetupPosition(portHex); ordered.gameObject.SetActive(true);
        vessel.SetupPosition(coastHex); foreach(LandUnit passenger in vessel.Passengers) passenger.SetupPosition(coastHex);
        manager.SelectPlayerUnit(ordered); manager.SetupCurrentTown(town);
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
    private static void Next(int next) { phase=next; frames=0; phaseStarted=EditorApplication.timeSinceStartup; SessionState.SetFloat(Key+".LastProgress",(float)phaseStarted); SessionState.SetInt(Key+".Phase",phase); File.WriteAllText(Result,"RUNNING\nWeek 13 phase "+phase+"; stock operation "+operation+".\n"); }
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
