using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class ColonizationWeek9Setup
{
    public const string Request="Temp/ColonizationWeek9Setup.v1.request";
    public const string Result="Temp/ColonizationWeek9Setup.result.txt";
    private const string Map="Assets/Scenes/Map01.unity";
    private const string Folder="Assets/Prefabs/UI/";
    private static readonly Color Paper=new Color(.83f,.76f,.61f);

    [InitializeOnLoadMethod] private static void Initialize() { EditorApplication.update+=Tick; }
    private static void Tick()
    {
        if(!File.Exists(Request) || File.Exists("Temp/ColonizationEditorRefresh.request") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request);
        try { Apply(); } catch(Exception e) { File.WriteAllText(Result,"FAILED\n"+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Colonization/Apply Week 9 From Existing Assets")]
    public static void Apply()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=Map || scene.isDirty || SceneManager.sceneCount!=1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode before Week 9 setup.");
        GameManager manager=Object.FindAnyObjectByType<GameManager>(); UIManager ui=Object.FindAnyObjectByType<UIManager>();
        ColonizationWeek7Setup.Validate(manager,ui);
        GameObject stockPrefab=Prefab("StockSlot",new Vector2(120,120),obj=>ConfigureStock(obj,manager.ProductData[0].icons[0]));
        GameObject dragPrefab=Prefab("StockDrag",new Vector2(64,64),obj=>
        {
            UnityEngine.UI.Image image=Paint(obj,Color.white,true); image.sprite=manager.ProductData[0].icons[0]; image.preserveAspect=true;
            StockDrag drag=Component<StockDrag>(obj); Set(drag,"image",image);
        });
        GameObject shipPrefab=Prefab("ShipInPortIcon",new Vector2(128,128),obj=>
        {
            UnityEngine.UI.Image image=Paint(obj,Color.white,true); image.sprite=manager.NavalUnitData[0].shipIcon; image.preserveAspect=true;
            ShipInPort icon=Component<ShipInPort>(obj); Set(icon,"unitImage",image);
        });
        GameObject cargoPrefab=Prefab("CargoSlot",new Vector2(64,64),obj=> { Paint(obj,new Color(0,0,0,.75f),true); Component<CargoSlot>(obj); });
        Transform panel=ui.TownPanel.transform, product=panel.Find("ProductPanel");
        UnityEngine.UI.HorizontalLayoutGroup products=Component<UnityEngine.UI.HorizontalLayoutGroup>(product.gameObject);
        products.childAlignment=TextAnchor.MiddleLeft; products.padding=new RectOffset(); products.spacing=0; products.childControlWidth=products.childControlHeight=false; products.childForceExpandWidth=products.childForceExpandHeight=false;
        // Week 9 uses the full 1920px row for sixteen 120px stock slots. The
        // existing header exit remains available; retain the old footer object.
        Transform footer=product.Find("ExitButton"); if(footer!=null) footer.gameObject.SetActive(false);
        StockSlot[] slots=new StockSlot[16]; TMP_Text[] amounts=new TMP_Text[16];
        for(int i=0;i<16;i++)
        {
            GameObject obj=Child(product,"ProductSlot"+i,new Vector2(120,120),Vector2.zero); ConfigureStock(obj,manager.ProductData[i].icons[0]);
            slots[i]=obj.GetComponent<StockSlot>(); amounts[i]=obj.transform.Find("Amount").GetComponent<TMP_Text>();
        }
        Transform port=panel.Find("UnitPanel/PortPanel");
        TMP_Text label=port.Find("Label").GetComponent<TMP_Text>(); Size(label.rectTransform,new Vector2(400,30),new Vector2(0,115)); label.fontSize=20;
        GameObject ships=Child(port,"ShipPanel",new Vector2(420,160),new Vector2(40,45));
        UnityEngine.UI.GridLayoutGroup grid=Component<UnityEngine.UI.GridLayoutGroup>(ships); grid.cellSize=new Vector2(128,128); grid.spacing=Vector2.zero; grid.startCorner=UnityEngine.UI.GridLayoutGroup.Corner.LowerLeft; grid.startAxis=UnityEngine.UI.GridLayoutGroup.Axis.Horizontal; grid.childAlignment=TextAnchor.LowerLeft;
        GameObject hold=Child(port,"CargoHoldPanel",new Vector2(450,80),new Vector2(0,-90));
        UnityEngine.UI.HorizontalLayoutGroup layout=Component<UnityEngine.UI.HorizontalLayoutGroup>(hold); layout.padding=new RectOffset(10,10,10,10); layout.spacing=10; layout.childAlignment=TextAnchor.MiddleLeft; layout.childControlWidth=layout.childControlHeight=false; layout.childForceExpandWidth=false; layout.childForceExpandHeight=true;
        CargoSlot[] cargoSlots=new CargoSlot[6];
        for(int i=0;i<6;i++)
        {
            Transform found=hold.transform.Find("CargoSlot"+i);
            GameObject obj=found!=null?found.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(cargoPrefab,hold.transform); obj.name="CargoSlot"+i;
            cargoSlots[i]=obj.GetComponent<CargoSlot>(); obj.SetActive(false);
        }
        SerializedObject data=new SerializedObject(ui);
        SetArray(data.FindProperty("stockSlots"),slots); SetArray(data.FindProperty("cargoSlots"),cargoSlots); SetArray(data.FindProperty("warehouseTexts"),amounts);
        data.FindProperty("stockDragPrefab").objectReferenceValue=dragPrefab; data.FindProperty("shipInPortPrefab").objectReferenceValue=shipPrefab; data.FindProperty("portShipsParent").objectReferenceValue=ships;
        data.ApplyModifiedPropertiesWithoutUndo();
        ui.TownPanel.SetActive(false); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Week 9 Map01.");
        Validate(manager,ui);
        File.WriteAllText(Result,"PASSED\nSixteen 120x120 StockSlots, 64px icons and draggable stock, 42pt warehouse amounts, 128px ShipInPortIcon, 420x160 ship grid and 450x80 cargo panel with six 64px slots. References saved natively in Map01, existing objects/GUIDs retained; original footer exit retained inactive and header EXIT available.\nExisting project sprites only; cargo-content icons/unloading and Europe trading remain later milestones.\n");
    }
    public static void Validate(GameManager manager,UIManager ui)
    {
        ColonizationWeek7Setup.Validate(manager,ui);
        if(!ui.HasStockConfiguration || ui.StockSlots.Any(s=>s==null) || ui.CargoSlots.Any(s=>s==null)) throw new InvalidOperationException("Week 9 references incomplete.");
        if(ui.StockSlots.Distinct().Count()!=16 || ui.CargoSlots.Distinct().Count()!=6) throw new InvalidOperationException("Duplicate Week 9 slot references.");
        foreach(StockSlot slot in ui.StockSlots)
            if(((RectTransform)slot.transform).sizeDelta!=new Vector2(120,120) || slot.Image.raycastTarget || slot.StockText.raycastTarget || slot.StockText.fontSize!=42) throw new InvalidOperationException("Invalid StockSlot dimensions/text/raycast configuration.");
        foreach(string name in new[]{"StockSlot","StockDrag","ShipInPortIcon","CargoSlot"})
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+name+".prefab")==null) throw new InvalidOperationException("Missing Week 9 prefab "+name);
    }
    private static void ConfigureStock(GameObject obj,Sprite sprite)
    {
        Paint(obj,new Color(.32f,.25f,.16f),false);
        GameObject icon=Child(obj.transform,"Icon",new Vector2(64,64),new Vector2(0,-40)); RectTransform rect=(RectTransform)icon.transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,1); rect.anchoredPosition=new Vector2(0,-40);
        UnityEngine.UI.Image image=Paint(icon,Color.white,false); image.sprite=sprite; image.preserveAspect=true;
        GameObject amount=Child(obj.transform,"Amount",new Vector2(120,50),new Vector2(0,-36));
        TMP_Text text=Component<TextMeshProUGUI>(amount); text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"); text.text="0"; text.fontSize=42; text.color=Color.white; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
        StockSlot slot=Component<StockSlot>(obj); Set(slot,"image",image); Set(slot,"stockText",text);
    }
    private static GameObject Prefab(string name,Vector2 size,Action<GameObject> configure)
    {
        string path=Folder+name+".prefab"; bool existing=AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null;
        GameObject obj=existing?PrefabUtility.LoadPrefabContents(path):new GameObject(name,typeof(RectTransform));
        try { Size((RectTransform)obj.transform,size,Vector2.zero); obj.layer=5; configure(obj); return PrefabUtility.SaveAsPrefabAsset(obj,path); }
        finally { if(existing) PrefabUtility.UnloadPrefabContents(obj); else Object.DestroyImmediate(obj); }
    }
    private static T Component<T>(GameObject obj) where T:Component { return obj.GetComponent<T>()??obj.AddComponent<T>(); }
    private static GameObject Child(Transform parent,string name,Vector2 size,Vector2 pos) { Transform found=parent.Find(name); GameObject obj=found!=null?found.gameObject:new GameObject(name,typeof(RectTransform)); obj.layer=5; obj.transform.SetParent(parent,false); Size((RectTransform)obj.transform,size,pos); return obj; }
    private static void Size(RectTransform rect,Vector2 size,Vector2 pos) { rect.localScale=Vector3.one; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=pos; }
    private static UnityEngine.UI.Image Paint(GameObject obj,Color color,bool raycast) { UnityEngine.UI.Image image=Component<UnityEngine.UI.Image>(obj); image.color=color; image.raycastTarget=raycast; return image; }
    private static void Set(Object obj,string field,Object value) { SerializedObject data=new SerializedObject(obj); data.FindProperty(field).objectReferenceValue=value; data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void SetArray(SerializedProperty property,Object[] objects) { property.arraySize=objects.Length; for(int i=0;i<objects.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue=objects[i]; }
}
