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

public static class ColonizationWeek12SmokeTest
{
    public const string Request="Temp/ColonizationWeek12SmokeTest.v1.request";
    public const string Result="Temp/ColonizationWeek12SmokeTest.result.txt";
    private const string Key="Colonization.Week12Smoke";
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
    [MenuItem("Tools/Colonization/Run Week 12 Play Mode Smoke Test")]
    public static void Run()
    {
        Preflight(); SessionState.SetBool(Key+".Regression",true); SessionState.SetFloat(Key+".RegressionStarted",(float)EditorApplication.timeSinceStartup);
        File.WriteAllText(Result,"RUNNING\nWeek 5–11 behavioral regression suites first.\n"); ColonizationWeek11SmokeTest.Run();
    }
    private static void Preflight()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount!=1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode for Week 12 validation.");
        ColonizationWeek12Setup.Validate(Object.FindAnyObjectByType<GameManager>(),Object.FindAnyObjectByType<UIManager>());
    }
    private static void Tick()
    {
        if(SessionState.GetBool(Key+".Pending",false) && EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPaused)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if((!ready && EditorApplication.timeSinceStartup-initialized>5) || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup)>60)
            { Finish("FAILED\nWeek 12 player loop/reload interrupted phase "+SessionState.GetInt(Key+".Phase",0)); return; }
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) stableSince=EditorApplication.timeSinceStartup;
        if(File.Exists(Request) && !File.Exists(ColonizationWeek12Setup.Request) && !File.Exists("Temp/ColonizationEditorRefresh.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string mode=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try { Preflight(); if(mode=="extend") { Require(File.Exists("Temp/ColonizationWeek11SmokeTest.result.txt") && File.ReadAllText("Temp/ColonizationWeek11SmokeTest.result.txt").StartsWith("PASSED"),"A passed Week 11 regression is required."); SessionState.SetBool(Key+".Start",true); } else Run(); }
            catch(Exception e) { Finish("FAILED\n"+e); }
        }
        if(SessionState.GetBool(Key+".Regression",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            string result=File.ReadAllText("Temp/ColonizationWeek11SmokeTest.result.txt");
            if(result.StartsWith("FAILED") || EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".RegressionStarted",0)>1000) Finish("FAILED\nWeek 11 regression:\n"+result);
            else if(result.StartsWith("PASSED") && SceneManager.GetActiveScene().path=="Assets/Scenes/Map01.unity") { SessionState.SetBool(Key+".Regression",false); SessionState.SetBool(Key+".Start",true); }
        }
        if(SessionState.GetBool(Key+".Start",false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !File.Exists("Temp/ColonizationEditorRefresh.request") && EditorApplication.timeSinceStartup-stableSince>10)
        {
            SessionState.SetBool(Key+".Start",false);
            try
            {
                Preflight(); Settings.ResetForNewGame(); SessionState.SetBool(Key+".Pending",true); SessionState.SetInt(Key+".Phase",0); SessionState.SetFloat(Key+".LastProgress",(float)EditorApplication.timeSinceStartup);
                if(!Application.isBatchMode) { Type type=typeof(Editor).Assembly.GetType("UnityEditor.GameView"); if(type!=null) { EditorWindow window=EditorWindow.GetWindow(type); window.Show(); window.Focus(); } }
                File.WriteAllText(Result,"RUNNING\nEntering Week 12 Play Mode.\n"); EditorApplication.isPlaying=true;
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
        if(state==PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key+".Pending",false); File.WriteAllText(Result,"FAILED\nPlay Mode exited before Week 12 completed.\n"); }
    }
    private static IEnumerator RuntimeLoop()
    {
        while(ready && Application.isPlaying && SessionState.GetBool(Key+".Pending",false))
        {
            yield return null; if(EditorApplication.isPaused) continue;
            try { if(error!=null) throw new InvalidOperationException(error); if(EditorApplication.timeSinceStartup-started>300) throw new TimeoutException("Week 12 phase "+phase); Step(); }
            catch(Exception e) { Finish("FAILED\nPhase "+phase+", operation "+operation+"\n"+e); }
        }
    }
    private static DialogManager dialogs;
    private static LandUnit pioneer, native, defeatedPassenger;
    private static NavalUnit opponent, defeatedShip;
    private static Hex forestHex, clearHex, startHex, encounterHex, shipOrigin, enemyHex;
    private static Vector3 cameraBefore;
    private static float zoomBefore;
    private static int turnBefore, battle;
    private static double combatStarted;
    private static Unit expectedWinner;
    private static Faction defeatedFaction;
    private static void Step()
    {
        frames++; if(frames<8 || EditorApplication.timeSinceStartup-phaseStarted<.2) return;
        switch(phase)
        {
            case 0:
                manager=GameManager.instance; ui=UIManager.instance; dialogs=DialogManager.instance;
                if(manager==null || manager.AllHexes[0,0]==null) break;
                ColonizationWeek12Setup.Validate(manager,ui);
                Require(dialogs.IsOpen && dialogs.ActiveDialog==dialogs.Dialogs[5] && dialogs.ActiveDialog.NotiText.text.Contains("Sugar") && dialogs.ActiveDialog.NotiText.text.Contains("Tobacco") && dialogs.ActiveDialog.NotiText.text.Contains("Cotton"),"Initial King mission missing.");
                Require(dialogs.ActiveDialog.CharImage.sprite==manager.PlayerFaction.LeaderSprite && dialogs.ActiveDialog.OkayButton.gameObject.activeSelf && !dialogs.ActiveDialog.YesButton.gameObject.activeSelf && manager.Factions.All(f=>!f.MetByPlayer && f.AtPeaceWithPlayer),"King portrait/buttons or relationship reset incorrect.");
                ship=manager.PlayerFaction.Units.OfType<NavalUnit>().Single(); cameraBefore=Camera.main.transform.position; zoomBefore=Camera.main.orthographicSize;
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek12King.png"); Keys(InputKey.E,InputKey.Backspace,InputKey.Tab,InputKey.W,InputKey.Z,InputKey.Enter); Next(1); break;
            case 1:
                Keys(); Require(manager.GameTurn==1 && manager.PlayerTurn && manager.CurUnit==ship && !ui.InEurope && !ui.IsTownPanelOpen && Camera.main.transform.position==cameraBefore && Camera.main.orthographicSize==zoomBefore,"King modal leaked map input.");
                Click(dialogs.ActiveDialog.OkayButton.gameObject); Require(!dialogs.IsOpen,"King Okay did not close.");
                TestStrengthRules(); BuildWorkFixture(); Next(2); break;
            case 2:
                manager.SelectPlayerUnit(pioneer); pioneer.BuildSettlement();
                Require(dialogs.ActiveDialog==dialogs.Dialogs[6] && pioneer.ToolsNum==100 && pioneer.UnitStatus==UnitStatus.None && dialogs.ActiveDialog.NotiText.text.Contains("cleared land"),"Forest settlement warning or charge incorrect.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek12Colony.png"); Next(3); break;
            case 3:
                Click(dialogs.ActiveDialog.OkayButton.gameObject); pioneer.ToolsNum=19; pioneer.ClearingLand();
                Require(dialogs.IsOpen && dialogs.ActiveDialog.NotiText.text.Contains("19") && pioneer.ToolsNum==19 && pioneer.UnitStatus==UnitStatus.None,"Clearing tool warning failed."); Next(4); break;
            case 4:
                Click(dialogs.ActiveDialog.OkayButton.gameObject); pioneer.SetupPosition(clearHex); pioneer.BuildSettlement();
                Require(dialogs.IsOpen && dialogs.ActiveDialog.NotiText.text.Contains("19") && pioneer.ToolsNum==19,"Building tool warning failed."); Next(5); break;
            case 5:
                Click(dialogs.ActiveDialog.OkayButton.gameObject); pioneer.ToolsNum=100;
                Hex adjacent=HexCalculator.GetHexAround(manager.AllHexes,clearHex).First(h=>h.HexType!=HexType.Ocean && h.HexType!=HexType.Mountains && !h.HasTown); manager.GenerateTown(manager.PlayerFaction,adjacent);
                Require(adjacent.HasTown,"Construction-warning fixture failed to create a neighboring town.");
                pioneer.BuildSettlement(); Require(dialogs.IsOpen && dialogs.ActiveDialog.NotiText.text.Contains("next to another") && pioneer.ToolsNum==100 && pioneer.UnitStatus==UnitStatus.None,"Adjacent-town construction warning failed."); Next(6); break;
            case 6:
                Click(dialogs.ActiveDialog.OkayButton.gameObject);
                LandUnit soldier=ship.Passengers.Single(); soldier.ToolsNum=100; soldier.UnitStatus=UnitStatus.None; soldier.ClearingLand(); soldier.BuildSettlement(); soldier.UnitStatus=UnitStatus.OnBoard;
                Require(!dialogs.IsOpen && soldier.ToolsNum==100,"Non-pioneer began pioneer work.");
                startHex=manager.AllHexes.Cast<Hex>().First(h=>h.HexType!=HexType.Ocean && !h.HasTown && HexCalculator.GetHexAround(manager.AllHexes,h).Count(n=>n.HexType!=HexType.Ocean && !n.HasTown)>=3);
                encounterHex=HexCalculator.GetHexAround(manager.AllHexes,startHex).First(h=>h.HexType!=HexType.Ocean && !h.HasTown);
                Hex nativeHex=HexCalculator.GetHexAround(manager.AllHexes,encounterHex).First(h=>h!=startHex && h.HexType!=HexType.Ocean && !h.HasTown);
                native=CreateLand(manager.Factions[5],nativeHex); manager.Factions[5].MetByPlayer=false;
                pioneer.SetupPosition(startHex); pioneer.MovePoint=30; manager.SelectPlayerUnit(pioneer); pioneer.PrepareMoveToHex(encounterHex); Require(pioneer.IsMoving,"Land encounter approach failed."); Next(7); break;
            case 7:
                if(pioneer.IsMoving) break;
                Require(dialogs.IsOpen && dialogs.ActiveDialog==dialogs.Dialogs[3] && manager.CurForeignFaction==manager.Factions[5] && dialogs.ActiveDialog.NotiText.text.Contains("tribe welcomes") && dialogs.ActiveDialog.CharImage.sprite==manager.Factions[5].LeaderSprite,"Native first contact on land failed.");
                manager.CheckUnmetFaction(pioneer); manager.CheckUnmetFaction(pioneer); Next(8); break;
            case 8:
                Click(dialogs.ActiveDialog.YesButton.gameObject); Require(manager.Factions[5].MetByPlayer && manager.Factions[5].AtPeaceWithPlayer && manager.CurForeignFaction==null,"Native peace decision not saved.");
                pioneer.PrepareMoveToHex(startHex); Next(9); break;
            case 9:
                if(pioneer.IsMoving) break;
                Require(!dialogs.IsOpen,"Already-met nation repeated its introduction.");
                manager.Factions[1].MetByPlayer=false;
                enemyHex=manager.AllHexes[48,24]; opponent=CreateShip(manager.Factions[1],enemyHex); PositionShip(ship,manager.AllHexes[47,25]);
                ship.MovePoint=30; manager.SelectPlayerUnit(ship); ship.PrepareMoveToHex(manager.AllHexes[47,24]); Next(10); break;
            case 10:
                if(ship.IsMoving) break;
                Require(dialogs.IsOpen && manager.CurForeignFaction==manager.Factions[1] && dialogs.ActiveDialog.NotiText.text.Contains("New France") && dialogs.ActiveDialog.CharImage.sprite==manager.Factions[1].LeaderSprite,"European first contact at sea failed.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek12Foreign.png"); Next(11); break;
            case 11:
                Click(dialogs.ActiveDialog.NoButton.gameObject);
                Require(manager.Factions[1].MetByPlayer && !manager.Factions[1].AtPeaceWithPlayer && dialogs.ActiveDialog.OkayButton.gameObject.activeSelf && !dialogs.ActiveDialog.YesButton.gameObject.activeSelf && dialogs.ActiveDialog.NotiText.text.Contains("at war"),"War declaration UI/state incorrect.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek12War.png"); Next(12); break;
            case 12:
                Click(dialogs.ActiveDialog.OkayButton.gameObject); Require(manager.CurForeignFaction==null && !dialogs.IsOpen,"Foreign Okay did not clear current faction.");
                turnBefore=manager.GameTurn; Keys(InputKey.Backspace); Next(13); break;
            case 13:
                Keys(); Require(!manager.PlayerTurn && dialogs.IsAIMoveOpen && dialogs.ActiveDialog.NotiText.text.Contains("AI is moving") && !dialogs.ActiveDialog.OkayButton.gameObject.activeSelf,"AI movement dialog missing or dismissible.");
                ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek12AI.png"); Keys(InputKey.E,InputKey.Backspace,InputKey.Tab); Next(14); break;
            case 14:
                Keys(); if(!manager.PlayerTurn) break;
                Require(manager.GameTurn==turnBefore+1 && !dialogs.IsOpen && !AIManager.instance.IsRunning,"AI dialog did not close at handoff.");
                battle=0; PlaceBattle(); Next(15); break;
            case 15:
                ship.MovePoint=0; ship.AttackUnit(opponent.CurHex,opponent); Require(!manager.CombatInProgress,"Zero-point attack accepted.");
                ship.MovePoint=30; ship.AttackUnit(ship.CurHex,ship); Require(!manager.CombatInProgress,"Friendly attack accepted.");
                Hex near=opponent.CurHex; opponent.SetupPosition(manager.AllHexes[47,40]); ship.AttackUnit(opponent.CurHex,opponent); Require(!manager.CombatInProgress,"Distant attack accepted."); opponent.SetupPosition(near);
                WorldClick(opponent.gameObject,PointerEventData.InputButton.Right); Require(manager.CombatInProgress && !ship.IsMoving && ship.CurHex==shipOrigin && ship.TargetUnit==opponent,"Actual enemy right-click did not start stationary naval combat.");
                expectedWinner=manager.Winner; defeatedShip=(NavalUnit)manager.Loser; defeatedFaction=defeatedShip.Faction; defeatedPassenger=defeatedShip.Passengers[0];
                if(battle==0) Require(expectedWinner==ship,"Stronger attacker lost."); if(battle==1) Require(expectedWinner==opponent,"Stronger defender lost.");
                combatStarted=EditorApplication.timeSinceStartup; turnBefore=manager.GameTurn; Keys(InputKey.Backspace,InputKey.E,InputKey.Tab); Next(16); break;
            case 16:
                Keys(); Require(manager.PlayerTurn && manager.GameTurn==turnBefore && !ui.InEurope,"Combat leaked end-turn/Europe input.");
                manager.StartCombat(ship,opponent); Require(manager.Winner==expectedWinner,"Second combat replaced the pending outcome.");
                if(dialogs.ActiveDialog!=dialogs.Dialogs[9] || !dialogs.IsOpen) break;
                Require(EditorApplication.timeSinceStartup-combatStarted>=.45 && !defeatedShip.IsDead,"Analysis timing incorrect or ship died too early.");
                Require(dialogs.Dialogs[9].transform.Find("InfoPanel/AttackPanel/UnitPanel0/NameText").GetComponent<TMP_Text>().text==ship.UnitName && dialogs.Dialogs[9].transform.Find("InfoPanel/DefendPanel/UnitPanel0/NumberText").GetComponent<TMP_Text>().text==opponent.Strength.ToString(),"Combat analysis data incorrect.");
                if(battle==0) ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek12Combat.png"); Next(17); break;
            case 17:
                if(manager.CombatInProgress) break;
                Require(EditorApplication.timeSinceStartup-combatStarted>=3.8 && dialogs.ActiveDialog==dialogs.NavalDialog && dialogs.NavalDialog.NotiText.text.Contains("defeats") && expectedWinner!=null && !expectedWinner.IsDead,"Combat timer/result failed.");
                Require(defeatedShip==null && defeatedPassenger==null && defeatedFaction.Units.All(u=>u!=null && !u.IsDead) && manager.AllHexes.Cast<Hex>().All(h=>h.UnitsInHex.All(u=>u!=null && !u.IsDead)),"Dead ship/passengers remain in faction or Hex lists.");
                Require(!dialogs.NavalDialog.YesButton.gameObject.activeSelf && dialogs.NavalDialog.OkayButton.gameObject.activeSelf,"Combat result retained Europe answer buttons.");
                if(battle==0) ScreenCapture.CaptureScreenshot("Temp/ColonizationWeek12Result.png"); Next(18); break;
            case 18:
                Click(dialogs.NavalDialog.OkayButton.gameObject); Require(!dialogs.IsOpen && !manager.MapInputBlocked && manager.CurUnit!=null,"Combat acknowledgment left controls locked or selected corpse.");
                battle++; if(battle<3) { PlaceBattle(); Next(15); } else Next(19); break;
            case 19:
                // The original Europe confirmation must still work after using NavalDialog for results.
                ship=CreateShip(manager.PlayerFaction,manager.AllHexes[49,12]); manager.SelectPlayerUnit(ship); EuropeManager.instance.QuestionGoToEurope(ship); Next(20); break;
            case 20:
                Require(dialogs.IsEuropeQuestion && dialogs.NavalDialog.YesButton.gameObject.activeSelf && !dialogs.NavalDialog.OkayButton.gameObject.activeSelf,"Naval result broke the next Europe confirmation.");
                Click(dialogs.NavalDialog.NoButton.gameObject); Require(!dialogs.IsOpen && ship.CurHex!=null,"Europe decline after combat failed.");
                Finish("PASSED\nWeek 12: initial King mission/portrait/Okay and modal keyboard/camera blocking; three Colony warnings with no tools charged; non-pioneer restrictions; native land arrival and European sea arrival first-contact dialogs, duplicate/already-met suppression, leader binding, peace and war state/acknowledgment; visible AI progress through a real Backspace turn.\nThree actual Physics2DRaycaster/EventSystem naval attacks: stronger attacker wins, stronger defender wins and equal-strength outcome; deterministic strength comparison and both tie branches; adjacency/points/friendly guards, combat input/re-entry lock, 0.5s analysis and 4s resolution, correct strength/icon/name UI, complete ship/passenger/faction/Hex death cleanup, surviving selection and snapshotted result after destruction. The Europe confirmation still works after NavalDialog combat reuse.\nScreenshots: Temp/ColonizationWeek12King.png, Colony.png, Foreign.png, War.png, AI.png, Combat.png and Result.png (each has ColonizationWeek12 prefix). Runtime fixtures discarded and Edit Mode restored. Existing assets only.\n"); break;
        }
    }
    private static void TestStrengthRules()
    {
        Require(GameManager.AttackerWins(10,2) && !GameManager.AttackerWins(2,10),"Strength comparison failed.");
        UnityEngine.Random.State old=UnityEngine.Random.state; UnityEngine.Random.InitState(1200); int wins=0;
        for(int i=0;i<64;i++) if(GameManager.AttackerWins(4,4)) wins++;
        UnityEngine.Random.state=old; Require(wins>0 && wins<64,"Equal strengths never selected both outcomes.");
    }
    private static void BuildWorkFixture()
    {
        foreach(Faction faction in manager.Factions) faction.MetByPlayer=true;
        pioneer=ship.Passengers.Single(p=>p.LandUnitType==LandUnitType.HardyPioneers); ship.Passengers.Remove(pioneer); pioneer.TransportShip=null; pioneer.transform.SetParent(manager.PlayerFaction.UnitParent,true); pioneer.UnitStatus=UnitStatus.None; pioneer.gameObject.SetActive(true);
        forestHex=manager.AllHexes.Cast<Hex>().First(h=>h.HasForest && !h.HasTown && h.HexType!=HexType.Ocean && h.HexType!=HexType.Mountains && h.HexType!=HexType.Hills && !HexCalculator.CheckIfHexAroundHasTown(manager.AllHexes,h));
        clearHex=manager.AllHexes.Cast<Hex>().First(h=>h!=forestHex && !h.HasTown && h.HexType!=HexType.Ocean && h.HexType!=HexType.Mountains && !HexCalculator.CheckIfHexAroundHasTown(manager.AllHexes,h) && HexCalculator.GetHexAround(manager.AllHexes,h).Any(n=>n.HexType!=HexType.Ocean && !n.HasTown)); clearHex.ClearForest(); pioneer.SetupPosition(forestHex);
    }
    private static NavalUnit CreateShip(Faction owner,Hex hex)
    {
        GameObject prefab=(GameObject)new SerializedObject(manager).FindProperty("navalUnitPrefab").objectReferenceValue; NavalUnit vessel=Object.Instantiate(prefab,owner.UnitParent).GetComponent<NavalUnit>(); vessel.UnitInit(manager,owner,manager.NavalUnitData[0]); vessel.SetupPosition(hex); owner.Units.Add(vessel); vessel.ShowHideSprite(true); return vessel;
    }
    private static LandUnit CreateLand(Faction owner,Hex hex)
    {
        GameObject prefab=(GameObject)new SerializedObject(manager).FindProperty("landUnitPrefab").objectReferenceValue; LandUnit unit=Object.Instantiate(prefab,owner.UnitParent).GetComponent<LandUnit>(); unit.UnitInit(manager,owner,manager.LandUnitData[3]); unit.SetupPosition(hex); owner.Units.Add(unit); return unit;
    }
    private static void AddPassenger(NavalUnit vessel)
    { LandUnit unit=CreateLand(vessel.Faction,vessel.CurHex); unit.TransportShip=vessel; unit.UnitStatus=UnitStatus.OnBoard; unit.transform.SetParent(vessel.PassengerParent.transform,true); vessel.Passengers.Add(unit); unit.gameObject.SetActive(false); }
    private static void PositionShip(NavalUnit vessel,Hex hex) { vessel.SetupPosition(hex); foreach(LandUnit unit in vessel.Passengers) unit.SetupPosition(hex); }
    private static void PlaceBattle()
    {
        foreach(Faction faction in manager.Factions) faction.MetByPlayer=true;
        shipOrigin=manager.AllHexes[46,30+battle*3]; enemyHex=manager.AllHexes[47,30+battle*3];
        if(ship==null) { ship=CreateShip(manager.PlayerFaction,shipOrigin); AddPassenger(ship); } else PositionShip(ship,shipOrigin);
        if(opponent==null) opponent=CreateShip(manager.Factions[1],enemyHex); else PositionShip(opponent,enemyHex);
        if(ship.Passengers.Count==0) AddPassenger(ship);
        if(opponent.Passengers.Count==0) { AddPassenger(opponent); AddPassenger(opponent); }
        opponent.AddCargo(1,new Cargo(2,100)); ship.AddCargo(1,new Cargo(3,70));
        ship.Strength=battle==0?8:battle==1?2:4; opponent.Strength=battle==0?2:battle==1?8:4;
        ship.MovePoint=30; manager.SelectPlayerUnit(ship);
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
    private static void Next(int next) { phase=next; frames=0; phaseStarted=EditorApplication.timeSinceStartup; SessionState.SetFloat(Key+".LastProgress",(float)phaseStarted); SessionState.SetInt(Key+".Phase",phase); File.WriteAllText(Result,"RUNNING\nWeek 12 phase "+phase+"; stock operation "+operation+".\n"); }
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
