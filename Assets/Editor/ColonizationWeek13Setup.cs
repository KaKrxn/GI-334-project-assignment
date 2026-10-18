using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ColonizationWeek13Setup
{
    public const string Request = "Temp/ColonizationWeek13Setup.v1.request";
    public const string Result = "Temp/ColonizationWeek13Setup.result.txt";
    public static readonly int[] TrainIds = { 7,6,10,8,2,9,1,11,5,0 };
    private static TMP_FontAsset Font { get { return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"); } }
    [InitializeOnLoadMethod] private static void Initialize() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (!File.Exists(Request) || File.Exists("Temp/ColonizationEditorRefresh.request") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request); try { Apply(); } catch (Exception e) { File.WriteAllText(Result,"FAILED\n"+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Colonization/Apply Week 13 From Existing Assets")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount != 1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode before Week 13 setup.");
        GameManager game = Object.FindAnyObjectByType<GameManager>(); UIManager ui = Object.FindAnyObjectByType<UIManager>(); ColonizationWeek12Setup.Validate(game,ui);
        Scene selected = SceneManager.GetSceneByPath("Assets/Scenes/SelectNation.unity");
        if (selected.IsValid() && selected.isDirty) throw new InvalidOperationException("Save SelectNation before setup.");
        int[] shipPrices = {1000,2000,3000,2000,5000};
        for (int i=0;i<5;i++) { game.NavalUnitData[i].price=shipPrices[i]; EditorUtility.SetDirty(game.NavalUnitData[i]); }
        int[] landPrices = {600,1900,1400,0,0,1800,900,800,1100,1600,1000,2000};
        for (int i=0;i<12;i++) { game.LandUnitData[i].price=landPrices[i]; EditorUtility.SetDirty(game.LandUnitData[i]); }
        UpgradePrefabs(game);
        GameObject block=Child(ui.transform,"UnitOrderBlock",new Vector2(1920,1080),Vector2.zero); Paint(block,new Color(0,0,0,.55f),true); block.SetActive(false);
        GameObject purchase=BuildPurchase(ui,game); GameObject train=BuildTrain(ui,game);
        GameObject order=Child(ui.transform,"OrderPanel",new Vector2(500,250),Vector2.zero); Paint(order,new Color(.25f,.18f,.12f),true);
        GameObject label=Child(order.transform,"LabelPanel",new Vector2(500,60),new Vector2(0,95));
        TMP_Text orderText=Text(label.transform,"StatusText","",new Vector2(500,50),Vector2.zero,30,Color.white);
        Transform options=Child(order.transform,"SelectPanel",new Vector2(500,195),new Vector2(0,-27.5f)).transform;
        string[] commands={"None","Board the next ship","Move to the front"};
        for(int i=0;i<3;i++) BindInt(Button(options,"Order"+i,commands[i],new Vector2(500,60),new Vector2(0,65-i*65),30),ui.SetOrderForLandUnit,i);
        order.SetActive(false);
        Transform special=ui.EuropePanel.transform.Find("UnitPanel/SpecialPanel");
        if(special==null) special=ui.EuropePanel.transform.Find("UnitPanel/UnitPanel");
        special.name="UnitPanel";
        RectTransform specialRect=(RectTransform)special; specialRect.sizeDelta=new Vector2(580,330); specialRect.anchoredPosition=new Vector2(670,0);
        Transform status=special.Find("StatusText"); ((RectTransform)status).sizeDelta=new Vector2(540,90); ((RectTransform)status).anchoredPosition=new Vector2(0,115); status.GetComponent<TMP_Text>().fontSize=23;
        GameObject waiting=Child(special,"WaitingUnits",new Vector2(550,225),new Vector2(0,-45));
        UnityEngine.UI.GridLayoutGroup grid=Component<UnityEngine.UI.GridLayoutGroup>(waiting); grid.cellSize=new Vector2(120,160); grid.spacing=new Vector2(-10,-55); grid.childAlignment=TextAnchor.LowerLeft;
        grid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount=5;
        Transform buttons=ui.EuropePanel.transform.Find("ButtonPanel"); ((RectTransform)buttons).sizeDelta=new Vector2(250,200); ((RectTransform)buttons).anchoredPosition=new Vector2(830,5);
        UnityEngine.UI.VerticalLayoutGroup group=Component<UnityEngine.UI.VerticalLayoutGroup>(buttons.gameObject); group.childAlignment=TextAnchor.LowerCenter; group.childControlWidth=group.childControlHeight=false; group.childForceExpandWidth=group.childForceExpandHeight=true;
        string[] names={"RECRUITButton","PURCHASEButton","TRAINButton"};
        for(int i=0;i<3;i++) { UnityEngine.UI.Button button=buttons.Find(names[i]).GetComponent<UnityEngine.UI.Button>(); ((RectTransform)button.transform).sizeDelta=new Vector2(220,65); button.interactable=i>0; if(i==1) BindBool(button,ui.TogglePurchasePanel,true); if(i==2) BindBool(button,ui.ToggleTrainPanel,true); }
        Set(ui,"purchaseShipPanel",purchase); Set(ui,"trainUnitPanel",train); Set(ui,"orderPanel",order); Set(ui,"unitOrderBlock",block); Set(ui,"orderLabel",orderText); Set(ui,"europePortUnitParent",waiting.transform);
        ui.EuropePanel.SetActive(false); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Map01.");
        BuildIntro(scene); Validate(game,ui);
        File.WriteAllText(Result,"PASSED\nWeek 13 native Map01 purchase/train/order panels, ten specialist buttons, five ship buttons/prices, UnitDrag status icon and LandUnit status renderer/sprites saved through Unity APIs. SelectNation now has three sequential intro panels and BlackPanel. Existing scenes, GUIDs and project assets retained; no external packages/art. Recruit remains outside the manual's implemented scope.\n");
    }
    private static GameObject BuildPurchase(UIManager ui,GameManager game)
    {
        GameObject panel=Child(ui.transform,"PurchasePanel",new Vector2(1150,1080),Vector2.zero); Paint(panel,Color.clear,true);
        GameObject table=Child(panel.transform,"TableBackground",new Vector2(1150,600),new Vector2(0,-240)); Paint(table,new Color(.20f,.14f,.09f),false); table.transform.SetAsFirstSibling();
        UnityEngine.UI.Image art=Paint(Child(panel.transform,"ShipImage",new Vector2(460,440),new Vector2(0,280)),Color.white,false); art.sprite=game.NavalUnitData[4].shipIcon; art.preserveAspect=true;
        Transform label=Child(panel.transform,"LabelPanel",new Vector2(1150,120),new Vector2(0,-30)).transform;
        Text(label,"QuestionText","The following items are available. What shall we purchase?",new Vector2(1040,110),Vector2.zero,36,Color.white);
        BindBool(Button(panel.transform,"ExitButton","X",new Vector2(40,40),new Vector2(550,53),28),ui.TogglePurchasePanel,false);
        Transform select=Child(panel.transform,"SelectPanel",new Vector2(1150,450),new Vector2(0,-315)).transform;
        for(int i=0;i<5;i++)
        {
            UnityEngine.UI.Button button=Button(select,"Ship"+i,"",new Vector2(1150,90),new Vector2(0,180-i*90),36); Paint(button.gameObject,new Color(.30f,.22f,.14f,.01f),true);
            Text(button.transform,"ProfessionText",game.NavalUnitData[i].unitName,new Vector2(500,80),new Vector2(-245,0),36,Color.white).alignment=TextAlignmentOptions.MidlineLeft;
            UnityEngine.UI.Image image=Paint(Child(button.transform,"UnitImage",new Vector2(100,80),new Vector2(125,0)),Color.white,false); image.sprite=game.NavalUnitData[i].shipIcon; image.preserveAspect=true;
            Text(button.transform,"PriceText",game.NavalUnitData[i].price.ToString(),new Vector2(200,80),new Vector2(425,0),36,Color.white); BindInt(button,ui.AddNewShipInEurope,i);
        }
        panel.SetActive(false); return panel;
    }
    private static GameObject BuildTrain(UIManager ui,GameManager game)
    {
        GameObject panel=Child(ui.transform,"TrainPanel",new Vector2(800,950),Vector2.zero); Paint(panel,new Color(.24f,.19f,.13f),true);
        Transform label=Child(panel.transform,"LabelPanel",new Vector2(800,160),new Vector2(0,395)).transform;
        TMP_Text question=Text(label,"QuestionText","The <color=yellow>Royal University</color> can provide us with specialists for the right fee. Which skill shall we request?",new Vector2(750,150),Vector2.zero,38,Color.white); question.alignment=TextAlignmentOptions.MidlineLeft;
        BindBool(Button(label,"ExitButton","X",new Vector2(40,40),new Vector2(380,60),28),ui.ToggleTrainPanel,false);
        Transform select=Child(panel.transform,"SelectPanel",new Vector2(800,775),new Vector2(0,-72.5f)).transform;
        GameObject root=Child(select,"Scroll View",new Vector2(800,775),Vector2.zero); UnityEngine.UI.ScrollRect scroll=Component<UnityEngine.UI.ScrollRect>(root); Paint(root,new Color(.15f,.12f,.09f),true);
        GameObject viewport=Child(root.transform,"Viewport",new Vector2(780,775),new Vector2(-10,0)); Paint(viewport,Color.white,true); Component<UnityEngine.UI.Mask>(viewport).showMaskGraphic=false;
        GameObject content=Child(viewport.transform,"Content",new Vector2(780,0),Vector2.zero); RectTransform cr=(RectTransform)content.transform; cr.anchorMin=new Vector2(0,1); cr.anchorMax=new Vector2(1,1); cr.pivot=new Vector2(.5f,1); cr.sizeDelta=Vector2.zero;
        UnityEngine.UI.VerticalLayoutGroup layout=Component<UnityEngine.UI.VerticalLayoutGroup>(content); layout.childControlHeight=false; layout.childControlWidth=true; layout.childForceExpandHeight=false; layout.childForceExpandWidth=true;
        UnityEngine.UI.ContentSizeFitter fitter=Component<UnityEngine.UI.ContentSizeFitter>(content); fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        string[] labels={"Fishermen","Farmers","Ore Miners","Lumberjacks","Hardy Pioneers","Master Carpenters","Veteran Soldiers","Blacksmiths","Seasoned Scouts","Free Colonists"};
        for(int n=0;n<TrainIds.Length;n++)
        {
            int id=TrainIds[n]; UnityEngine.UI.Button button=Button(content.transform,"Train"+id,"",new Vector2(780,120),Vector2.zero,34);
            Text(button.transform,"ProfessionText",labels[n],new Vector2(320,90),new Vector2(-170,0),34,Color.white).alignment=TextAlignmentOptions.MidlineLeft;
            UnityEngine.UI.Image image=Paint(Child(button.transform,"UnitImage",new Vector2(100,100),new Vector2(80,0)),Color.white,false); image.sprite=game.LandUnitData[id].unitIcon; image.preserveAspect=true;
            Text(button.transform,"PriceText",game.LandUnitData[id].price.ToString(),new Vector2(150,90),new Vector2(285,0),34,Color.white); BindInt(button,ui.CreateUnitDragInEuropePort,id);
        }
        GameObject bar=Child(root.transform,"Scrollbar Vertical",new Vector2(20,775),new Vector2(390,0)); Paint(bar,new Color(.1f,.1f,.1f),true);
        UnityEngine.UI.Scrollbar scrollbar=Component<UnityEngine.UI.Scrollbar>(bar); GameObject handle=Child(bar.transform,"Handle",Vector2.zero,Vector2.zero); UnityEngine.UI.Image hi=Paint(handle,new Color(.75f,.65f,.4f),true); scrollbar.handleRect=(RectTransform)handle.transform; scrollbar.targetGraphic=hi; scrollbar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        scroll.viewport=(RectTransform)viewport.transform; scroll.content=cr; scroll.horizontal=false; scroll.vertical=true; scroll.scrollSensitivity=30; scroll.verticalScrollbar=scrollbar; scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide; scroll.verticalScrollbarSpacing=-20;
        panel.SetActive(false); return panel;
    }
    private static void UpgradePrefabs(GameManager game)
    {
        SerializedObject gm=new SerializedObject(game); GameObject landPrefab=(GameObject)gm.FindProperty("landUnitPrefab").objectReferenceValue;
        string path=AssetDatabase.GetAssetPath(landPrefab); GameObject root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform found=root.transform.Find("StatusSprite"); GameObject status=found!=null?found.gameObject:new GameObject("StatusSprite"); status.transform.SetParent(root.transform,false); status.transform.localPosition=new Vector3(.25f,-.2f,0); status.transform.localScale=new Vector3(.25f,.25f,1);
            SpriteRenderer renderer=Component<SpriteRenderer>(status); renderer.sharedMaterial=root.GetComponent<LandUnit>().UnitSprite.sharedMaterial; renderer.sortingLayerID=root.GetComponent<LandUnit>().UnitSprite.sortingLayerID; renderer.sortingOrder=7;
            Sprite[] icons={game.ProductData[14].icons[0],game.LandUnitData[9].unitIcon,game.NavalUnitData[0].shipIcon};
            Set(root.GetComponent<LandUnit>(),"statusSprite",renderer); SerializedObject data=new SerializedObject(root.GetComponent<LandUnit>()); Array(data.FindProperty("statusSpritesList"),icons); data.ApplyModifiedPropertiesWithoutUndo(); status.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        path="Assets/Prefabs/UI/UnitDrag.prefab"; root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            UnityEngine.UI.Image image=Paint(Child(root.transform,"StatusIcon",new Vector2(64,64),new Vector2(-28,-48)),Color.white,false); image.preserveAspect=true; image.gameObject.SetActive(false); Set(root.GetComponent<UnitDrag>(),"statusIcon",image); PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void BuildIntro(Scene original)
    {
        Scene scene=EditorSceneManager.OpenScene("Assets/Scenes/SelectNation.unity",OpenSceneMode.Additive);
        try
        {
            SelectNation screen=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<SelectNation>(true)).Single(); Transform canvas=screen.transform;
            GameObject black=Child(canvas,"BlackPanel",new Vector2(1920,1080),Vector2.zero); Paint(black,Color.black,true); Stretch((RectTransform)black.transform); black.SetActive(false);
            string[] names={"KingsCommandPanel","PrepareToLeavePanel","LeftThePortPanel"};
            string[] titles={"THE KING'S COMMAND","PREPARE TO LEAVE","LEAVING THE PORT"};
            string[] messages={"Establish a colony in the New World. Seek land rich in Sugar, Tobacco and Cotton, and build trade with Europe.","Your ship and colonists are ready. Set sail, explore the coast and choose a site for your first settlement.","The voyage begins. Guide your expedition to the New World."};
            GameObject[] panels=new GameObject[3];
            for(int i=0;i<3;i++)
            {
                GameObject panel=Child(canvas,names[i],new Vector2(1920,1080),Vector2.zero); panels[i]=panel; Stretch((RectTransform)panel.transform); Paint(panel,new Color(0,0,0,0),true);
                UnityEngine.UI.Image background=Paint(Child(panel.transform,"Image",new Vector2(1640,920),Vector2.zero),Color.white,false); background.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art Deco GUI/screen_bg_1.png");
                Paint(Child(panel.transform,"TextBackground",new Vector2(1450,340),new Vector2(0,-235)),new Color(.05f,.08f,.12f,.92f),false);
                Text(panel.transform,"Title",titles[i],new Vector2(1400,90),new Vector2(0,350),54,new Color(1,.85f,.35f));
                UnityEngine.UI.Image art=Paint(Child(panel.transform,"ExpeditionImage",new Vector2(350,350),new Vector2(0,75)),Color.white,false); art.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(i==0?"Assets/Textures/Emblems_of_civ/EnglandShield.png":"Assets/Resources/Icons Pirates Age of Sail/icons/ship_caravel.png"); if(art.sprite==null) art.sprite=screen.LeaderImage.sprite; art.preserveAspect=true;
                TMP_Text body=Text(panel.transform,"MissionText",messages[i],new Vector2(1360,270),new Vector2(0,-235),46,Color.white); body.enableAutoSizing=true; body.fontSizeMin=32; body.fontSizeMax=46;
                UnityEngine.UI.Button next=Button(panel.transform,"Button",">>",new Vector2(120,70),new Vector2(750,-460),42);
                if(i==0) Bind(next,screen.ShowPrepareToLeave); else if(i==1) Bind(next,screen.ShowLeftThePort); else Bind(next,screen.StartGame);
                panel.SetActive(false);
            }
            Set(screen,"blackPanel",black); SerializedObject refs=new SerializedObject(screen); Array(refs.FindProperty("panels"),panels); refs.ApplyModifiedPropertiesWithoutUndo();
            Bind(canvas.Find("DownPanel/StartGameButton").GetComponent<UnityEngine.UI.Button>(),screen.ShowKingsCommand);
            EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save SelectNation.");
        }
        finally { EditorSceneManager.CloseScene(scene,true); SceneManager.SetActiveScene(original); }
    }
    public static void Validate(GameManager game,UIManager ui)
    {
        ColonizationWeek12Setup.Validate(game,ui);
        if(!ui.HasWeek13Configuration || game.NavalUnitData.Take(5).Any(d=>d.price<=0) || TrainIds.Any(i=>game.LandUnitData[i].price<=0)) throw new InvalidOperationException("Week 13 references/prices missing.");
        UnityEngine.UI.ScrollRect scroll=ui.TrainUnitPanel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
        if(scroll==null || scroll.viewport==null || scroll.content==null || scroll.content.childCount!=TrainIds.Length || scroll.horizontal || scroll.verticalScrollbar==null) throw new InvalidOperationException("Train ScrollRect incomplete.");
        if(ui.PurchaseShipPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length!=6 || ui.OrderPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length!=3) throw new InvalidOperationException("Purchase/order buttons incomplete.");
    }
    private static T Component<T>(GameObject obj) where T:Component { T component=obj.GetComponent<T>(); return component!=null?component:obj.AddComponent<T>(); }
    private static GameObject Child(Transform parent,string name,Vector2 size,Vector2 pos)
    { Transform found=parent.Find(name); GameObject obj=found!=null?found.gameObject:new GameObject(name,typeof(RectTransform)); obj.layer=5; obj.transform.SetParent(parent,false); RectTransform rect=(RectTransform)obj.transform; rect.localScale=Vector3.one; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=pos; return obj; }
    private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
    private static UnityEngine.UI.Image Paint(GameObject obj,Color color,bool raycast) { UnityEngine.UI.Image image=Component<UnityEngine.UI.Image>(obj); image.color=color; image.raycastTarget=raycast; return image; }
    private static TMP_Text Text(Transform parent,string name,string value,Vector2 size,Vector2 pos,float fontSize,Color color)
    { TMP_Text text=Component<TextMeshProUGUI>(Child(parent,name,size,pos)); text.font=Font; text.text=value; text.fontSize=fontSize; text.color=color; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; return text; }
    private static UnityEngine.UI.Button Button(Transform parent,string name,string label,Vector2 size,Vector2 pos,float fontSize)
    { GameObject obj=Child(parent,name,size,pos); UnityEngine.UI.Image image=Paint(obj,new Color(.30f,.22f,.13f),true); UnityEngine.UI.Button button=Component<UnityEngine.UI.Button>(obj); button.targetGraphic=image; Text(obj.transform,"Text",label,size-Vector2.one*8,Vector2.zero,fontSize,Color.white); return button; }
    private static void Bind(UnityEngine.UI.Button button,UnityAction action) { button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent(); UnityEventTools.AddPersistentListener(button.onClick,action); }
    private static void BindBool(UnityEngine.UI.Button button,UnityAction<bool> action,bool value) { button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent(); UnityEventTools.AddBoolPersistentListener(button.onClick,action,value); }
    private static void BindInt(UnityEngine.UI.Button button,UnityAction<int> action,int value) { button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent(); UnityEventTools.AddIntPersistentListener(button.onClick,action,value); }
    private static void Set(Object obj,string field,Object value) { SerializedObject data=new SerializedObject(obj); data.FindProperty(field).objectReferenceValue=value; data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Array(SerializedProperty property,Object[] objects) { property.arraySize=objects.Length; for(int i=0;i<objects.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue=objects[i]; }
}
