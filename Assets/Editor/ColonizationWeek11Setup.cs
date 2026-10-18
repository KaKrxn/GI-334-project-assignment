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

public static class ColonizationWeek11Setup
{
    public const string Request = "Temp/ColonizationWeek11Setup.v1.request";
    public const string Result = "Temp/ColonizationWeek11Setup.result.txt";
    private const string Folder = "Assets/Prefabs/UI/";
    private static TMP_FontAsset Font { get { return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"); } }
    private static readonly Color Gold = new Color32(203,189,157,255);
    [InitializeOnLoadMethod] private static void Initialize() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (!File.Exists(Request) || File.Exists("Temp/ColonizationEditorRefresh.request") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request); try { Apply(); } catch (Exception e) { File.WriteAllText(Result, "FAILED\n" + e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Colonization/Apply Week 10–11 From Existing Assets")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount != 1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode before Week 10–11 setup.");
        GameManager manager = Object.FindAnyObjectByType<GameManager>(); UIManager ui = Object.FindAnyObjectByType<UIManager>(); ColonizationWeek9Setup.Validate(manager, ui);
        EuropeManager europe = Component<EuropeManager>(manager.gameObject);
        DialogManager dialogManager = Component<DialogManager>(ui.gameObject);
        int[] bid = { 2,4,4,3,5,4,6,19,5,13,13,13,13,13,13,13 }, ask = { 8,5,5,4,6,5,7,20,6,14,14,14,14,14,14,14 };
        for (int i = 0; i < 16; i++)
        {
            ProductData product = manager.ProductData[i];
            if (product.startAsk <= product.startBid || product.startAsk > 20 || product.startBid < 0) { product.startBid = bid[i]; product.startAsk = ask[i]; EditorUtility.SetDirty(product); }
        }
        GameObject cargoDrag = Prefab("CargoDrag", new Vector2(64,64), obj =>
        {
            UnityEngine.UI.Image image = Paint(obj, Color.white, true); image.sprite = manager.ProductData[0].icons[0]; image.preserveAspect = true;
            CargoDrag drag = Component<CargoDrag>(obj); Set(drag,"image",image);
            TMP_Text text = Text(obj.transform,"CargoText","100",new Vector2(64,32),new Vector2(0,-16),28,Color.white); text.fontStyle = FontStyles.Bold;
            Set(drag,"cargoText",text);
        });
        GameObject shipDrag = Prefab("ShipDrag", new Vector2(128,128), obj =>
        {
            UnityEngine.UI.Image image = Paint(obj, Color.white, true); image.sprite = manager.NavalUnitData[0].shipIcon; image.preserveAspect = true;
            Set(Component<ShipDrag>(obj),"shipImage",image);
        });
        Prefab("ShipInPortIcon",new Vector2(128,128), obj =>
        {
            ShipInPort icon = Component<ShipInPort>(obj);
            TMP_Text turn = Text(obj.transform,"TurnText","3\nTurn(s)",new Vector2(128,80),new Vector2(0,-24),42,Color.yellow);
            turn.enableAutoSizing = true; turn.fontSizeMin = 18; turn.fontSizeMax = 72; turn.gameObject.SetActive(false); Set(icon,"turnText",turn);
        });
        Prefab("StockSlot",new Vector2(120,120),obj => Paint(obj,obj.GetComponent<UnityEngine.UI.Image>().color,true));
        foreach (StockSlot slot in ui.StockSlots) slot.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        GameObject panel = Child(ui.transform,"EuropePanel",new Vector2(1920,1080),Vector2.zero); Paint(panel,new Color(.11f,.17f,.21f),true);
        GameObject background = Child(panel.transform,"Background",new Vector2(1920,1080),Vector2.zero);
        Paint(background,Color.white,false).sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art Deco GUI/screen_bg_1.png"); background.transform.SetAsFirstSibling();
        GameObject header = Child(panel.transform,"NamePanel",new Vector2(1920,50),new Vector2(0,515)); Paint(header,new Color(.2f,.14f,.09f),false);
        TMP_Text title = Text(header.transform,"EuropeTitle","Europe",new Vector2(1100,50),new Vector2(-200,0),30,Gold);
        UnityEngine.UI.Button exit = Button(header.transform,"ExitButton","X",new Vector2(40,40),new Vector2(910,0),28);
        BindBool(exit,ui.ToggleEuropePanel,false);
        TMP_Text euMoney = Money(header.transform);
        Transform townHeader = ui.TownPanel.transform.Find("Header") ?? ui.TownPanel.transform.Find("NamePanel");
        TMP_Text townMoney = Money(townHeader);
        // Tall terrain sprites extend above their slots; keep header text and exit above them.
        townHeader.SetAsLastSibling();
        // Manual's three action buttons are reserved for Week 13.
        GameObject buttons = Child(panel.transform,"ButtonPanel",new Vector2(260,200),new Vector2(830,5));
        string[] actions = { "RECRUIT", "PURCHASE", "TRAIN" };
        for (int i = 0; i < actions.Length; i++) Button(buttons.transform,actions[i]+"Button",actions[i],new Vector2(260,55),new Vector2(0,65-i*65),28).interactable = false;
        GameObject units = Child(panel.transform,"UnitPanel",new Vector2(1920,330),new Vector2(0,-260));
        Transform inbound = TransitPanel(units.transform,"InboundPanel","EUROPE BOUND",-745);
        Transform outbound = TransitPanel(units.transform,"OutboundPanel","BOUND FOR NEW WORLD",-315); Component<ToNewWorldSlot>(outbound.gameObject); Paint(outbound.gameObject,new Color(1,1,1,.01f),true);
        GameObject port = Child(units.transform,"PortPanel",new Vector2(480,330),new Vector2(140,0)); Paint(port,new Color(.23f,.29f,.34f,.95f),true);
        Text(port.transform,"Label","IN PORT",new Vector2(480,45),new Vector2(0,140),28,Gold);
        GameObject ships = Child(port.transform,"ShipPanel",new Vector2(490,200),new Vector2(0,20)); Grid(ships);
        GameObject hold = Child(port.transform,"CargoHoldPanel",new Vector2(450,80),new Vector2(0,-120));
        UnityEngine.UI.HorizontalLayoutGroup layout = Component<UnityEngine.UI.HorizontalLayoutGroup>(hold); layout.padding = new RectOffset(10,10,10,10); layout.spacing = 10; layout.childAlignment = TextAnchor.MiddleLeft; layout.childControlWidth = layout.childControlHeight = false; layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
        CargoSlot[] holds = new CargoSlot[6]; GameObject cargoSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"CargoSlot.prefab");
        for (int i = 0; i < 6; i++)
        {
            Transform found = hold.transform.Find("CargoSlot"+i); GameObject obj = found != null ? found.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(cargoSlotPrefab,hold.transform); obj.name = "CargoSlot"+i; holds[i] = obj.GetComponent<CargoSlot>(); obj.SetActive(false);
        }
        GameObject special = Child(units.transform,"SpecialPanel",new Vector2(580,330),new Vector2(670,0)); Paint(special,new Color(.28f,.25f,.18f,.95f),true);
        TMP_Text status = Text(special.transform,"StatusText","Drag goods to buy or sell.",new Vector2(520,240),Vector2.zero,28,Gold);
        GameObject products = Child(panel.transform,"ProductPanel",new Vector2(1920,115),new Vector2(0,-480));
        UnityEngine.UI.HorizontalLayoutGroup row = Component<UnityEngine.UI.HorizontalLayoutGroup>(products); row.spacing = 0; row.childAlignment = TextAnchor.MiddleLeft; row.childControlWidth = row.childControlHeight = row.childForceExpandWidth = row.childForceExpandHeight = false;
        StockSlot[] stocks = new StockSlot[16]; GameObject stockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"StockSlot.prefab");
        for (int i = 0; i < 16; i++)
        {
            Transform found = products.transform.Find("StockSlot"+i); GameObject obj = found != null ? found.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(stockPrefab,products.transform); obj.name = "StockSlot"+i; stocks[i] = obj.GetComponent<StockSlot>();
            stocks[i].Image.sprite = manager.ProductData[i].icons[0]; stocks[i].StockText.text = manager.ProductData[i].startBid+"/"+manager.ProductData[i].startAsk;
        }
        SerializedObject data = new SerializedObject(ui);
        Ref(data,"cargoDragPrefab",cargoDrag); Ref(data,"shipDragPrefab",shipDrag); Ref(data,"europePanel",panel); Ref(data,"toEuropeShipsParent",inbound); Ref(data,"fromEuropeShipsParent",outbound); Ref(data,"europePortShipsParent",ships.transform); Ref(data,"moneyText",townMoney); Ref(data,"moneyEuropeText",euMoney); Ref(data,"europeTitle",title); Ref(data,"europeStatus",status);
        Array(data.FindProperty("stockSlotsEurope"),stocks); Array(data.FindProperty("cargoSlotsEurope"),holds); data.ApplyModifiedPropertiesWithoutUndo();
        BuildDialog(ui.transform,dialogManager);
        panel.SetActive(false); ui.TownPanel.SetActive(false);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Week 10–11 Map01."); Validate(manager,ui);
        File.WriteAllText(Result,"PASSED\nWeek 10–11 native references, CargoDrag/ShipDrag prefabs, ship transit text, 16 market prices/stocks, six Europe holds, Europe panels and naval Yes/No dialog saved in Map01. Existing GUIDs retained. Existing project art only; missing EuropeMockup/wood download replaced by existing Art Deco background/native panels. Week 13 buttons remain disabled.\n");
    }
    private static void BuildDialog(Transform canvas,DialogManager manager)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scripts/DialogSO")) AssetDatabase.CreateFolder("Assets/Scripts","DialogSO");
        const string path = "Assets/Scripts/DialogSO/00_ToEurope.asset";
        DialogData data = AssetDatabase.LoadAssetAtPath<DialogData>(path);
        if (data == null) { data = ScriptableObject.CreateInstance<DialogData>(); AssetDatabase.CreateAsset(data,path); }
        data.question = "We have reached the high seas, Your Excellency. Shall we sail for Europe?";
        data.answers = new[] { "Yes, steady as she goes.", "No, let us remain in these waters." }; EditorUtility.SetDirty(data);
        GameObject block = Child(canvas,"NavalModalBlock",new Vector2(1920,1080),Vector2.zero); Paint(block,new Color(0,0,0,.45f),true);
        GameObject dialogs = Child(canvas,"Dialogs",new Vector2(1920,1080),Vector2.zero);
        GameObject naval = Child(dialogs.transform,"NavalDialog",new Vector2(940,400),Vector2.zero); Paint(naval,new Color(.25f,.16f,.09f),true);
        Dialog dialog = Component<Dialog>(naval);
        GameObject noti = Child(naval.transform,"NotiPanel",new Vector2(860,160),new Vector2(0,80));
        TMP_Text text = Text(noti.transform,"Text",data.question,new Vector2(820,120),Vector2.zero,48,Color.white); text.alignment = TextAlignmentOptions.MidlineLeft; text.enableAutoSizing = true; text.fontSizeMin = 38; text.fontSizeMax = 48;
        GameObject answers = Child(naval.transform,"AnswerPanel",new Vector2(800,160),new Vector2(0,-100));
        UnityEngine.UI.Button yes = Button(answers.transform,"YesButton",data.answers[0],new Vector2(800,80),new Vector2(0,40),48), no = Button(answers.transform,"NoButton",data.answers[1],new Vector2(800,80),new Vector2(0,-40),48);
        yes.GetComponentInChildren<TMP_Text>().alignment = no.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;
        Bind(yes,manager.YesGoToEurope); Bind(no,manager.NoGoToEurope);
        Set(dialog,"notiText",text); Set(dialog,"yesText",yes.GetComponentInChildren<TMP_Text>()); Set(dialog,"noText",no.GetComponentInChildren<TMP_Text>());
        Set(manager,"navalDialog",dialog); Set(manager,"toEuropeData",data); Set(manager,"modalBlock",block); block.SetActive(false); naval.SetActive(false);
    }
    public static void Validate(GameManager manager,UIManager ui)
    {
        ColonizationWeek9Setup.Validate(manager,ui);
        if (!ui.HasEuropeConfiguration || manager.GetComponent<EuropeManager>() == null || ui.GetComponent<DialogManager>() == null || ui.GetComponent<DialogManager>().NavalDialog == null) throw new InvalidOperationException("Week 10–11 references incomplete.");
        if (ui.StockSlotsEurope.Any(s=>s==null) || ui.CargoSlotsEurope.Any(s=>s==null) || ui.StockSlotsEurope.Distinct().Count()!=16 || ui.CargoSlotsEurope.Distinct().Count()!=6) throw new InvalidOperationException("Invalid Europe slot references.");
        foreach (ProductData product in manager.ProductData) if (product.startBid<0 || product.startAsk<=product.startBid || product.startAsk>20) throw new InvalidOperationException("Invalid market start prices.");
        foreach (string name in new[]{"CargoDrag","ShipDrag"}) if (AssetDatabase.LoadAssetAtPath<GameObject>(Folder+name+".prefab")==null) throw new InvalidOperationException("Missing "+name);
    }
    private static Transform TransitPanel(Transform parent,string name,string title,float x)
    {
        GameObject panel=Child(parent,name,new Vector2(430,330),new Vector2(x,0)); Paint(panel,new Color(.19f,.25f,.28f,.94f),true);
        Text(panel.transform,"Label",title,new Vector2(430,60),new Vector2(0,130),25,Gold);
        GameObject ships=Child(panel.transform,"ShipPanel",new Vector2(430,200),Vector2.zero); Grid(ships); return ships.transform;
    }
    private static void Grid(GameObject obj)
    { UnityEngine.UI.GridLayoutGroup grid=Component<UnityEngine.UI.GridLayoutGroup>(obj); grid.cellSize=new Vector2(128,128); grid.spacing=Vector2.zero; grid.startCorner=UnityEngine.UI.GridLayoutGroup.Corner.LowerLeft; grid.startAxis=UnityEngine.UI.GridLayoutGroup.Axis.Horizontal; grid.childAlignment=TextAnchor.LowerLeft; }
    private static TMP_Text Money(Transform header) { TMP_Text text=Text(header,"MoneyText","1000",new Vector2(200,50),new Vector2(400,0),42,Color.yellow); text.fontStyle=FontStyles.Bold; return text; }
    private static T Component<T>(GameObject obj) where T:Component { return obj.GetComponent<T>()??obj.AddComponent<T>(); }
    private static GameObject Child(Transform parent,string name,Vector2 size,Vector2 pos)
    { Transform found=parent.Find(name); GameObject obj=found!=null?found.gameObject:new GameObject(name,typeof(RectTransform)); obj.layer=5; obj.transform.SetParent(parent,false); RectTransform rect=(RectTransform)obj.transform; rect.localScale=Vector3.one; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=pos; return obj; }
    private static UnityEngine.UI.Image Paint(GameObject obj,Color color,bool raycast) { UnityEngine.UI.Image image=Component<UnityEngine.UI.Image>(obj); image.color=color; image.raycastTarget=raycast; return image; }
    private static TMP_Text Text(Transform parent,string name,string value,Vector2 size,Vector2 pos,float fontSize,Color color)
    { TMP_Text text=Component<TextMeshProUGUI>(Child(parent,name,size,pos)); text.font=Font; text.text=value; text.fontSize=fontSize; text.color=color; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; return text; }
    private static UnityEngine.UI.Button Button(Transform parent,string name,string label,Vector2 size,Vector2 pos,float fontSize)
    { GameObject obj=Child(parent,name,size,pos); UnityEngine.UI.Image image=Paint(obj,new Color(.30f,.22f,.13f),true); UnityEngine.UI.Button button=Component<UnityEngine.UI.Button>(obj); button.targetGraphic=image; Text(obj.transform,"Text",label,size-Vector2.one*8,Vector2.zero,fontSize,Color.white); return button; }
    private static void Bind(UnityEngine.UI.Button button,UnityAction action) { button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent(); UnityEventTools.AddPersistentListener(button.onClick,action); }
    private static void BindBool(UnityEngine.UI.Button button,UnityAction<bool> action,bool value) { button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent(); UnityEventTools.AddBoolPersistentListener(button.onClick,action,value); }
    private static void Set(Object obj,string field,Object value) { SerializedObject data=new SerializedObject(obj); Ref(data,field,value); data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Ref(SerializedObject data,string field,Object value) { data.FindProperty(field).objectReferenceValue=value; }
    private static void Array(SerializedProperty property,Object[] objects) { property.arraySize=objects.Length; for(int i=0;i<objects.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue=objects[i]; }
    private static GameObject Prefab(string name,Vector2 size,Action<GameObject> configure)
    {
        string path=Folder+name+".prefab"; bool exists=AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null;
        GameObject obj=exists?PrefabUtility.LoadPrefabContents(path):new GameObject(name,typeof(RectTransform));
        try { ((RectTransform)obj.transform).sizeDelta=size; obj.layer=5; configure(obj); return PrefabUtility.SaveAsPrefabAsset(obj,path); }
        finally { if(exists) PrefabUtility.UnloadPrefabContents(obj); else Object.DestroyImmediate(obj); }
    }
}
