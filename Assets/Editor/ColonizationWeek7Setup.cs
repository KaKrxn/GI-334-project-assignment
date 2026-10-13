using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ColonizationWeek7Setup
{
    public const string RequestPath = "Temp/ColonizationWeek7Setup.v1.request";
    public const string ResultPath = "Temp/ColonizationWeek7Setup.result.txt";
    private const string MapPath = "Assets/Scenes/Map01.unity";
    private const string IconPath = "Assets/Prefabs/UI/YieldIcon.prefab";
    private static readonly string[] Names = { "Food", "Sugar", "Tobacco", "Cotton", "Furs", "Lumber", "IronOres", "Silver", "Horses", "Rum", "Cigars", "Clothes", "Coats", "TradeGoods", "Tools", "Musket" };
    private static readonly string[] Professions = { "Farmer / Fisherman", "Sugar Planter", "Tobacco Planter", "Cotton Planter", "Fur Trapper", "Lumberjack", "Ore Miner", "Silver Miner" };
    private static readonly string[] Icons = {
        "Icons Food Spices Drinks/Icons/maize", "Icons Strategic Resources Goods/Icons/sugarcane",
        "Icons Strategic Resources Goods/Icons/tobacco", "Icons Strategic Resources Goods/Icons/cotton",
        "Icons Strategic Resources Goods/Icons/furs", "Icons Strategic Resources Goods/Icons/planks",
        "Icons Strategic Resources Goods/Icons/iron_ore", "Icons Strategic Resources Goods/Icons/silver_ingots",
        "Icons Strategic Resources Goods/Icons/horse", "Icons Pirates Age of Sail/icons/rum_bottle",
        "Icons Old West Cowboys Miners/icons/cigar_box", "Icons Strategic Resources Goods/Icons/cloth_bolts",
        "Icons Strategic Resources Goods/Icons/furs", "Icons Strategic Resources Goods/Icons/naval_supplies",
        "Icons Old West Cowboys Miners/icons/pickaxe2", "Icons Old West Cowboys Miners/icons/rifle_repeater" };
    // The manual fixes Ocean/Plain/Grassland examples; other rows are editable
    // starting choices for the terrain-yield design exercise, not extra rules.
    private static readonly int[][] Yields = {
        new[]{4,0,0,0,0,0,0,0}, new[]{3,0,5,0,5,5,0,0}, new[]{3,0,0,5,3,3,0,0},
        new[]{4,3,3,0,3,3,0,0}, new[]{5,4,3,3,3,3,0,0}, new[]{2,0,0,0,6,4,0,0},
        new[]{1,0,0,3,0,0,2,1}, new[]{2,5,0,0,4,4,2,0}, new[]{0,0,0,0,0,0,0,0},
        new[]{2,0,0,0,4,5,4,1}, new[]{0,0,0,0,0,0,5,5} };
    private static readonly Color Paper = new Color(.83f,.76f,.61f);
    private static readonly Color Ink = new Color(.12f,.085f,.06f);

    [InitializeOnLoadMethod] private static void Initialize() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if(File.Exists("Temp/ColonizationEditorStatus.request"))
        {
            File.Delete("Temp/ColonizationEditorStatus.request");
            Scene active=SceneManager.GetActiveScene();
            File.WriteAllText("Temp/ColonizationEditorStatus.result.txt","playing="+EditorApplication.isPlaying+"; changing="+EditorApplication.isPlayingOrWillChangePlaymode+"; paused="+EditorApplication.isPaused+"; compiling="+EditorApplication.isCompiling+"; updating="+EditorApplication.isUpdating+"\nactive="+active.path+"; dirty="+active.isDirty+"; count="+SceneManager.sceneCount+"\n"+string.Join("\n",Enumerable.Range(0,SceneManager.sceneCount).Select(i=> { Scene s=SceneManager.GetSceneAt(i); return s.path+" dirty="+s.isDirty+" loaded="+s.isLoaded; })));
        }
        if(File.Exists("Temp/ColonizationWeek7Save.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            File.Delete("Temp/ColonizationWeek7Save.request");
            try
            {
                Scene active=SceneManager.GetActiveScene();
                if(active.path!=MapPath || SceneManager.sceneCount!=1) throw new InvalidOperationException("Save requires Map01 alone in Edit Mode.");
                Validate(Object.FindAnyObjectByType<GameManager>(),Object.FindAnyObjectByType<UIManager>());
                AssetDatabase.SaveAssets();
                if(!EditorSceneManager.SaveScene(active)) throw new IOException("Could not save finalized Week 7 scene references.");
                File.WriteAllText("Temp/ColonizationWeek7Save.result.txt","PASSED\nCurrent native Week 7 Map01 configuration saved; no scene reload or reset.\n");
            }
            catch(Exception e) { File.WriteAllText("Temp/ColonizationWeek7Save.result.txt","FAILED\n"+e); }
        }
        if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || File.Exists("Temp/ColonizationEditorRefresh.request")) return;
        File.Delete(RequestPath);
        try { Apply(); } catch (Exception e) { File.WriteAllText(ResultPath,"FAILED\n"+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Colonization/Apply Week 7 From Existing Assets")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetSceneByPath(MapPath);
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded || scene.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Open saved Map01 alone in Edit Mode before applying Week 7.");
        GameManager manager = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();
        UIManager ui = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UIManager>(true)).Single();
        ColonizationWeek6Setup.ValidateConfiguration(manager,ui);
        if (!AssetDatabase.IsValidFolder("Assets/Scripts/ProductSO")) AssetDatabase.CreateFolder("Assets/Scripts","ProductSO");
        ProductData[] products = new ProductData[16];
        for (int i=0;i<16;i++)
        {
            string path="Assets/Scripts/ProductSO/"+i.ToString("00")+"_"+Names[i]+".asset";
            ProductData data=AssetDatabase.LoadAssetAtPath<ProductData>(path);
            if(data==null) { data=ScriptableObject.CreateInstance<ProductData>(); AssetDatabase.CreateAsset(data,path); }
            data.id=i; data.productName=Names[i];
            data.icons=i==0 ? new[]{Sprite(Icons[i]),Sprite("Icons Strategic Resources Goods/Icons/fish")} : new[]{Sprite(Icons[i])};
            EditorUtility.SetDirty(data); products[i]=data;
        }
        for(int i=0;i<manager.HexData.Length;i++)
        {
            HexData data=manager.HexData[i];
            if(data.resourceYield==null || data.resourceYield.Length!=8) { data.resourceYield=(int[])Yields[i].Clone(); EditorUtility.SetDirty(data); }
        }
        SerializedObject gm=new SerializedObject(manager); SerializedProperty productArray=gm.FindProperty("productData"); productArray.arraySize=16;
        for(int i=0;i<16;i++) productArray.GetArrayElementAtIndex(i).objectReferenceValue=products[i];
        gm.ApplyModifiedProperties();
        GameObject iconPrefab=CreateIcon(products[0].icons[0]);
        Transform panel=ui.TownPanel.transform;
        GameObject block=Child(panel,"BlockImage",new Vector2(1920,1080),Vector2.zero);
        RectTransform blockRect=(RectTransform)block.transform; blockRect.anchorMin=Vector2.zero; blockRect.anchorMax=Vector2.one; blockRect.sizeDelta=Vector2.zero;
        Paint(block,Color.clear,true); block.GetComponent<CanvasRenderer>().cullTransparentMesh=false;
        GameObject profession=Child(panel,"ProfessionPanel",new Vector2(800,600),Vector2.zero); Paint(profession,Paper,true);
        GameObject label=Child(profession.transform,"LabelPanel",new Vector2(800,100),new Vector2(0,250));
        TMP_Text question=Text(label.transform,"QuestionText","Select a profession",new Vector2(760,100),Vector2.zero,30,Ink);
        GameObject select=Child(profession.transform,"SelectPanel",new Vector2(800,480),new Vector2(0,-50));
        UnityEngine.UI.VerticalLayoutGroup vertical=select.GetComponent<UnityEngine.UI.VerticalLayoutGroup>()??select.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vertical.childAlignment=TextAnchor.MiddleCenter; vertical.childControlWidth=vertical.childControlHeight=false; vertical.childForceExpandWidth=vertical.childForceExpandHeight=false; vertical.spacing=0;
        TMP_Text[] yieldTexts=new TMP_Text[8]; UnityEngine.UI.Image corn=null,fish=null;
        for(int i=0;i<8;i++)
        {
            GameObject obj=Child(select.transform,"Button"+i,new Vector2(800,60),Vector2.zero);
            Paint(obj,i%2==0?new Color(.9f,.85f,.74f):Paper,true);
            UnityEngine.UI.Button button=obj.GetComponent<UnityEngine.UI.Button>()??obj.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic=obj.GetComponent<UnityEngine.UI.Image>();
            Text(obj.transform,"ProfessionText",Professions[i],new Vector2(300,60),new Vector2(-225,0),24,Ink).alignment=TextAlignmentOptions.MidlineLeft;
            yieldTexts[i]=Text(obj.transform,"YieldText","0 "+Names[i],new Vector2(300,60),new Vector2(120,0),24,Ink);
            UnityEngine.UI.Image image=Paint(Child(obj.transform,"Image",new Vector2(60,60),new Vector2(320,0)),Color.white,false); image.sprite=products[i].icons[0]; image.preserveAspect=true;
            if(i==0)
            {
                corn=image; fish=Paint(Child(obj.transform,"FishImage",new Vector2(60,60),new Vector2(320,0)),Color.white,false); fish.sprite=products[0].icons[1]; fish.preserveAspect=true; fish.gameObject.SetActive(false);
            }
            for(int n=button.onClick.GetPersistentEventCount()-1;n>=0;n--) UnityEventTools.RemovePersistentListener(button.onClick,n);
            UnityEventTools.AddIntPersistentListener(button.onClick,ui.SelectYield,i);
        }
        Transform unit=panel.Find("UnitPanel");
        TMP_Text foodText=Text(unit,"FoodText","0",new Vector2(60,60),new Vector2(30,-83),30,Paper);
        RectTransform textRect=(RectTransform)foodText.transform; textRect.anchorMin=textRect.anchorMax=new Vector2(0,.5f); textRect.anchoredPosition=new Vector2(30,-83);
        GameObject food=Child(unit,"FoodPanel",new Vector2(300,100),new Vector2(210,-83));
        RectTransform foodRect=(RectTransform)food.transform; foodRect.anchorMin=foodRect.anchorMax=new Vector2(0,.5f); foodRect.anchoredPosition=new Vector2(210,-83);
        Horizontal(food);
        // Make room for the Week 7 food row within the existing idle-unit panel.
        ui.OutsideTownParent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().padding.bottom=65;
        TMP_Text[] warehouse=new TMP_Text[16];
        for(int i=0;i<16;i++)
        {
            Transform slot=panel.Find("ProductPanel/ProductSlot"+i);
            UnityEngine.UI.Image image=Paint(Child(slot,"Icon",new Vector2(64,64),new Vector2(0,12)),Color.white,false); image.sprite=products[i].icons[0]; image.preserveAspect=true;
            warehouse[i]=Text(slot,"Amount","0",new Vector2(96,30),new Vector2(0,-32),22,Paper);
        }
        GameObject exit=Child(panel.Find("NamePanel"),"ExitButton",new Vector2(40,40),new Vector2(-50,0));
        RectTransform exitRect=(RectTransform)exit.transform; exitRect.anchorMin=exitRect.anchorMax=new Vector2(1,.5f); exitRect.anchoredPosition=new Vector2(-50,0);
        Paint(exit,new Color(.48f,.18f,.12f),true); Text(exit.transform,"Label","X",new Vector2(40,40),Vector2.zero,26,Paper);
        UnityEngine.UI.Button close=exit.GetComponent<UnityEngine.UI.Button>()??exit.AddComponent<UnityEngine.UI.Button>(); close.targetGraphic=exit.GetComponent<UnityEngine.UI.Image>();
        for(int n=close.onClick.GetPersistentEventCount()-1;n>=0;n--) UnityEventTools.RemovePersistentListener(close.onClick,n);
        UnityEventTools.AddBoolPersistentListener(close.onClick,ui.ToggleTownPanel,false);
        panel.Find("UnitPanel/SpecialPanel/Label").GetComponent<TMP_Text>().text="Drag a unit onto a Hex and choose its profession.\nRight-click a worker to change profession.\nDrag outside town to stop working.";
        SerializedObject u=new SerializedObject(ui);
        u.FindProperty("yieldIconPrefab").objectReferenceValue=iconPrefab;
        u.FindProperty("blockImage").objectReferenceValue=block; u.FindProperty("professionPanel").objectReferenceValue=profession;
        u.FindProperty("labelQuestionText").objectReferenceValue=question;
        u.FindProperty("foodProfessionIcon").objectReferenceValue=corn; u.FindProperty("fishProfessionIcon").objectReferenceValue=fish;
        u.FindProperty("foodParent").objectReferenceValue=food.transform; u.FindProperty("foodText").objectReferenceValue=foodText;
        SetTexts(u.FindProperty("btnYieldTexts"),yieldTexts); SetTexts(u.FindProperty("warehouseTexts"),warehouse);
        u.ApplyModifiedProperties(); block.SetActive(false); profession.SetActive(false); ui.TownPanel.SetActive(false);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Week 7 Map01.");
        Validate(manager,ui);
        File.WriteAllText(ResultPath,"PASSED\n16 ordered ProductData assets (Food corn/fish), 11 eight-resource terrain configurations, 64x64 YieldIcon, 800x600 profession modal with eight persistent buttons, food total row, warehouse icon/amount labels and header EXIT wired in saved Map01.\nExisting project assets only; Coats uses fur art and Musket uses the existing rifle as temporary proxies.\n");
    }
    public static void Validate(GameManager manager,UIManager ui)
    {
        ColonizationWeek6Setup.ValidateConfiguration(manager,ui);
        if(manager.ProductData==null || manager.ProductData.Length!=16 || !ui.HasYieldConfiguration || ui.BlockImage==null || ui.FoodText==null) throw new InvalidOperationException("Week 7 references incomplete.");
        for(int i=0;i<16;i++)
        {
            ProductData p=manager.ProductData[i]; if(p==null || p.id!=i || p.productName!=Names[i] || p.icons==null || p.icons.Length!=(i==0?2:1) || p.icons.Any(s=>s==null)) throw new InvalidOperationException("Invalid ProductData "+i);
        }
        if(manager.HexData.Any(d=>d.resourceYield==null || d.resourceYield.Length!=8 || d.resourceYield.Any(n=>n<0))) throw new InvalidOperationException("Terrain yields incomplete.");
        if(((RectTransform)ui.YieldIconPrefab.transform).sizeDelta!=new Vector2(64,64) || ((RectTransform)ui.ProfessionPanel.transform).sizeDelta!=new Vector2(800,600)) throw new InvalidOperationException("Week 7 dimensions invalid.");
        UnityEngine.UI.Button[] buttons=ui.ProfessionPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        if(buttons.Length!=8 || buttons.Any(b=>b.onClick.GetPersistentEventCount()!=1 || b.onClick.GetPersistentMethodName(0)!="SelectYield")) throw new InvalidOperationException("Profession buttons not wired.");
    }
    private static Sprite Sprite(string path) { return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/"+path+".png")??throw new FileNotFoundException("Existing sprite missing: "+path); }
    private static GameObject CreateIcon(Sprite sprite)
    {
        bool existing=AssetDatabase.LoadAssetAtPath<GameObject>(IconPath)!=null;
        GameObject obj=existing?PrefabUtility.LoadPrefabContents(IconPath):new GameObject("YieldIcon",typeof(RectTransform));
        try { Size((RectTransform)obj.transform,new Vector2(64,64),Vector2.zero); UnityEngine.UI.Image image=Paint(obj,Color.white,false); image.sprite=sprite; image.preserveAspect=true; return PrefabUtility.SaveAsPrefabAsset(obj,IconPath); }
        finally { if(existing) PrefabUtility.UnloadPrefabContents(obj); else Object.DestroyImmediate(obj); }
    }
    private static void SetTexts(SerializedProperty property,TMP_Text[] texts) { property.arraySize=texts.Length; for(int i=0;i<texts.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue=texts[i]; }
    private static GameObject Child(Transform parent,string name,Vector2 size,Vector2 position)
    {
        Transform found=parent.Find(name); GameObject obj=found!=null?found.gameObject:new GameObject(name,typeof(RectTransform)); obj.layer=5; obj.transform.SetParent(parent,false); Size((RectTransform)obj.transform,size,position); return obj;
    }
    private static void Size(RectTransform rect,Vector2 size,Vector2 position) { rect.localScale=Vector3.one; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=position; }
    private static UnityEngine.UI.Image Paint(GameObject obj,Color color,bool raycast) { UnityEngine.UI.Image image=obj.GetComponent<UnityEngine.UI.Image>()??obj.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=raycast; return image; }
    private static TMP_Text Text(Transform parent,string name,string value,Vector2 size,Vector2 position,int fontSize,Color color)
    {
        GameObject obj=Child(parent,name,size,position); TMP_Text text=obj.GetComponent<TMP_Text>()??obj.AddComponent<TextMeshProUGUI>(); text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"); text.text=value; text.fontSize=fontSize; text.color=color; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; return text;
    }
    private static void Horizontal(GameObject obj) { UnityEngine.UI.HorizontalLayoutGroup layout=obj.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>()??obj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>(); layout.childAlignment=TextAnchor.MiddleLeft; layout.childControlWidth=layout.childControlHeight=layout.childForceExpandWidth=layout.childForceExpandHeight=false; layout.spacing=0; }
}
