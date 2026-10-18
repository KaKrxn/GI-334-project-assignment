using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// A bounded, automated demonstration using the real movement/work/turn/UI APIs.
// All fixture positioning is performed before recording and is discarded on exit.
[InitializeOnLoad]
public static class ColonizationGameplayRecording
{
    const string Dir="Art/GameplayVideo/FirstColony/";
    const string Key="Colonization.GameplayRecording";
    static double readySince,nextFrame;
    static int frame,phase,phaseFrame;
    static GameManager manager;
    static NavalUnit ship;
    static LandUnit pioneer;
    static Hex coast,sea,sea2;
    static Town town;
    static bool initialized;
    static RenderTexture target;
    static Texture2D pixels;
    static ColonizationGameplayRecording()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode){initialized=false;readySince=EditorApplication.timeSinceStartup;}};
    }
    static void Tick()
    {
        if(File.Exists("Temp/GameplayRecordingRestoreBackground.request")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
        {
            File.Delete("Temp/GameplayRecordingRestoreBackground.request");
            // Recover the original project setting after an interrupted capture.
            PlayerSettings.runInBackground=false;Application.runInBackground=false;AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/GameplayRecordingRestoreBackground.result.txt","PASSED: background setting restored\n");
        }
        if(File.Exists("Temp/GameplayRecording.request")&&!File.Exists("Temp/ColonizationEditorRefresh.request")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
        {
            File.Delete("Temp/GameplayRecording.request");Directory.CreateDirectory(Dir+"Frames");
            if(SceneManager.GetActiveScene().path!="Assets/Scenes/Map01.unity"||SceneManager.GetActiveScene().isDirty){File.WriteAllText(Dir+"Result.txt","FAILED: open saved Map01");return;}
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;File.WriteAllText(Dir+"Result.txt","RUNNING\n");EditorApplication.isPlaying=true;
        }
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isPaused)return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if(!initialized)
            {
                if(EditorApplication.timeSinceStartup-readySince<6)return;
                if(ColonizationLegacyDialogFixture.AcknowledgeNotice())return;
                Initialize();initialized=true;return;
            }
            if(EditorApplication.timeSinceStartup-readySince>900)throw new TimeoutException("Recording phase "+phase);
            if(!manager.PlayerTurn)CameraController.instance.MoveCamera(sea.Pos);
            if(ColonizationLegacyDialogFixture.AcknowledgeNotice())return;
            double hold=(frame-phaseFrame)/4.0;
            switch(phase)
            {
                case 0:if(hold>=4){manager.SelectPlayerUnit(ship);ship.PrepareMoveToHex(sea2);Next(1,"คลิกขวาช่องทะเลที่ติดกัน เพื่อเดินเรือ (แต้มเดินเริ่มต้น 4)");}break;
                case 1:if(!ship.IsMoving&&hold>=3){ship.PrepareMoveToHex(sea);Next(2,"แต่ละช่องทะเลใช้ 1 แต้ม เดินกลับอีก 1 ช่อง");}break;
                case 2:if(!ship.IsMoving&&hold>=3){ship.PrepareMoveToHex(sea2);Next(3,"ตอนนี้ใช้ไป 2 แต้ม เดินต่อได้อีก 2 ช่อง");}break;
                case 3:if(!ship.IsMoving&&hold>=3){ship.PrepareMoveToHex(sea);Next(4,"เดินครบ 4 ช่องแล้ว เรือจะหมดแต้มเดินในเทิร์นนี้");}break;
                case 4:if(!ship.IsMoving&&hold>=5){Require(ship.MovePoint==0,"Ship did not spend four moves");manager.Endturn();Next(5,"กด Backspace เพื่อจบเทิร์น แล้วรอ AI เดินให้เสร็จ");}break;
                case 5:if(manager.PlayerTurn&&hold>=4){Require(ship.MovePoint==4,"Moves not restored");manager.SelectPlayerUnit(pioneer);Next(6,"ขึ้นเทิร์นใหม่ แต้มเดินเติมแล้ว กด Tab เลือก Hardy Pioneers บนเรือ");}break;
                case 6:if(hold>=5){coast.OnPointerClick(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Right});Next(7,"เลือก Pioneers แล้วคลิกขวาพื้นดินข้างเรือ เพื่อขึ้นฝั่ง");}break;
                case 7:if(!pioneer.IsMoving&&hold>=5){Require(pioneer.CurHex==coast,"Landfall failed");pioneer.ClearingLand();Require(pioneer.UnitStatus==UnitStatus.Clearing,"Clearing not queued");Next(8,"กด P เพื่อถางป่า ใช้เครื่องมือ 20 หน่วย งานยังไม่เสร็จทันที");}break;
                case 8:if(hold>=5){manager.Endturn();Next(9,"กด Backspace อีกครั้ง งานถางป่าจะเสร็จเมื่อขึ้นเทิร์นใหม่");}break;
                case 9:if(manager.PlayerTurn&&hold>=4){manager.SelectPlayerUnit(pioneer);Require(!coast.HasForest,"Forest not cleared");Next(10,"พื้นที่โล่งแล้ว พร้อมสร้างเมือง ต้องไม่อยู่ติดเมืองอื่น");}break;
                case 10:if(hold>=5){pioneer.BuildSettlement();Require(pioneer.UnitStatus==UnitStatus.Building,"Building not queued");Next(11,"กด B เพื่อเริ่มสร้างเมือง ใช้เครื่องมืออีก 20 หน่วย");}break;
                case 11:if(hold>=4){manager.Endturn();Next(12,"กด Backspace แล้วรอเทิร์นใหม่ เพื่อให้สร้างเมืองเสร็จ");}break;
                case 12:if(manager.PlayerTurn&&hold>=4){town=coast.Town;Require(town!=null,"Town was not built");manager.SelectPlayerUnit(pioneer);Next(13,"เมืองแรกสร้างเสร็จแล้ว คลิกซ้ายเมือง หรือกด Enter เพื่อเปิดเมือง");}break;
                case 13:if(hold>=5){manager.SetupCurrentTown(town);Require(UIManager.instance.IsTownPanelOpen,"Town did not open");Next(14,"คนงานว่างอยู่ใน Outside Town ด้านล่างซ้าย");}break;
                case 14:if(hold>=5){DragWorker();Next(15,"ลากคนงานจาก Outside Town ลง Hex รอบเมือง แล้วเลือกอาชีพ");}break;
                case 15:if(hold>=5){UIManager.instance.SelectYield(0);Next(16,"เลือก Farmer หรือ Fisherman เพื่อผลิตอาหาร ดูผลผลิตที่แสดงบน Hex");}break;
                case 16:if(hold>=6){UIManager.instance.ToggleTownPanel(false);manager.Endturn();Next(17,"ปิดเมืองและกด Backspace เพื่อให้เมืองเก็บผลผลิตประจำเทิร์น");}break;
                case 17:if(manager.PlayerTurn&&hold>=4){manager.SetupCurrentTown(town);Require(town.Warehouse[0]>0,"Food not accumulated");Next(18,"เปิดเมืองอีกครั้ง อาหารสะสมอยู่ในคลังด้านล่างแล้ว");}break;
                case 18:if(hold>=6){UIManager.instance.TownPanel.transform.Find("UnitPanel/SpecialPanel/ProductionGuideButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Next(19,"ปุ่ม PRODUCTION GUIDE ใช้ดูแผนภาพสายการผลิต");}break;
                case 19:if(hold>=6){Next(20,"จำไว้: คลิกซ้ายเลือก คลิกขวาเดิน Tab สลับยูนิต Backspace จบเทิร์น");}break;
                case 20:if(hold>=6){Finish("PASSED\nFrames="+frame+"; FPS=4; real movement, turn, landfall, clearing, town construction and food harvest demonstrated.\n");return;}break;
            }
            if(EditorApplication.timeSinceStartup>=nextFrame){Capture();nextFrame=EditorApplication.timeSinceStartup+.25;}
        }
        catch(Exception e){Finish("FAILED\n"+e);Debug.LogException(e);}
    }
    static void Initialize()
    {
        manager=GameManager.instance;Require(manager!=null,"No game manager");ship=manager.PlayerFaction.Units.OfType<NavalUnit>().First();pioneer=ship.Passengers.First(p=>p.LandUnitType==LandUnitType.HardyPioneers);
        foreach(Hex h in manager.AllHexes)
        {
            if(h==null||h.X<4||h.X>45||h.Y<10||h.Y>48||!h.HasForest||h.HexType==HexType.Hills||h.HexType==HexType.Mountains||h.HasTown||manager.CheckIfAroundHexHasTown(h))continue;
            foreach(var s in HexCalculator.GetHexAround(manager.AllHexes,h).Where(n=>n.HexType==HexType.Ocean))
            {
                var s2=HexCalculator.GetHexAround(manager.AllHexes,s).FirstOrDefault(n=>n!=h&&n.HexType==HexType.Ocean);
                if(s2!=null){coast=h;sea=s;sea2=s2;break;}
            }
            if(coast!=null)break;
        }
        Require(coast!=null,"No suitable coastal site");
        // Stage the opening position before recording; subsequent actions use gameplay APIs.
        ship.SetupPosition(sea);foreach(var p in ship.Passengers)p.SetupPosition(sea);ship.MovePoint=ship.MovePointMax;pioneer.MovePoint=pioneer.MovePointMax;
        foreach(Hex h in manager.AllHexes)if((h.Pos-sea.Pos).sqrMagnitude<250)h.DiscoverHex();
        manager.SelectPlayerUnit(ship);Camera.main.orthographicSize=5;
        target=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        frame=phase=phaseFrame=0;File.WriteAllText(Dir+"Chapters.tsv","0\tสาธิตเล่นเกม: เริ่มจากเรือใกล้ชายฝั่ง เพื่อฝึกตั้งเมืองแรก\n");readySince=EditorApplication.timeSinceStartup;nextFrame=0;
    }
    static void DragWorker()
    {
        var ui=UIManager.instance;var drag=ui.OutsideTownParent.GetComponentsInChildren<UnitDrag>().First(d=>d.LandUnit==pioneer);
        var slot=ui.AreaSlots.First(s=>s.Hex!=null&&!s.Hex.HasTown&&s.Hex.HexType!=HexType.Mountains&&s.Hex.Labor==null);
        var data=new PointerEventData(EventSystem.current){pointerDrag=drag.gameObject,position=drag.transform.position,button=PointerEventData.InputButton.Left};
        drag.OnBeginDrag(data);Require(drag.IsDragging,"Drag blocked");data.position=slot.transform.position;drag.OnDrag(data);slot.OnDrop(data);drag.OnEndDrag(data);Require(ui.IsProfessionPanelOpen,"Profession did not open");
    }
    static void Next(int next,string caption){phase=next;phaseFrame=frame;File.AppendAllText(Dir+"Chapters.tsv",frame+"\t"+caption+"\n");}
    static void Require(bool yes,string why){if(!yes)throw new InvalidOperationException(why);}
    static void Capture()
    {
        Camera camera=Camera.main;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var flags=camera.clearFlags;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
        var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
        var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);var enabled=renderers.Select(r=>r.enabled).ToArray();
        try
        {
            camera.targetTexture=target;
            bool fullUI=UIManager.instance!=null&&UIManager.instance.IsTownPanelOpen;
            if(fullUI)
            {
                foreach(var r in renderers)r.enabled=false;
                for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=1;}
                Canvas.ForceUpdateCanvases();
            }
            camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Dir+"Frames/"+frame.ToString("D5")+".jpg",pixels.EncodeToJPG(87));frame++;
        }
        finally
        {
            camera.clearFlags=flags;for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
        }
    }
    static void Finish(string result)
    {
        Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"Result.txt",result);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);SessionState.SetBool(Key,false);Application.runInBackground=SessionState.GetBool(Key+".Background",false);EditorApplication.isPlaying=false;
    }
}
