using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ColonizationWeek6Setup
{
    public const string RequestPath = "Temp/ColonizationWeek6Setup.v2.request";
    public const string ResultPath = "Temp/ColonizationWeek6Setup.result.txt";
    private const string MapPath = "Assets/Scenes/Map01.unity";
    private const string SlotPath = "Assets/Prefabs/UI/TerrainSlot.prefab";
    private const string DragPath = "Assets/Prefabs/UI/UnitDrag.prefab";
    private static readonly Vector2[] SlotPositions = { new Vector2(-128,192), new Vector2(128,192),
        new Vector2(256,0), new Vector2(128,-192), new Vector2(-128,-192), new Vector2(-256,0) };
    private static readonly Color Ink = new Color(.12f,.085f,.06f,1);
    private static readonly Color Paper = new Color(.83f,.76f,.61f,1);

    [InitializeOnLoadMethod] private static void Initialize() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (File.Exists("Temp/ColonizationEditorRefresh.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            File.Delete("Temp/ColonizationEditorRefresh.request");
            // delayCall depends on an Inspector update and can remain queued
            // while the Editor is in the background. This tick is already gated
            // to a stable Edit Mode, so request the import directly.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return;
        }
        if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(RequestPath);
        try { Apply(); } catch (Exception e) { File.WriteAllText(ResultPath, "FAILED\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Tools/Colonization/Apply Week 6 From Existing Assets")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before Week 6 setup.");
        Scene map = SceneManager.GetSceneByPath(MapPath);
        bool opened = !map.IsValid() || !map.isLoaded;
        if (!opened && map.isDirty) throw new InvalidOperationException("Save Map01 before applying Week 6.");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        GameObject slotPrefab = CreateTerrainSlot();
        GameObject dragPrefab = CreateUnitDrag();
        if (opened) map = EditorSceneManager.OpenScene(MapPath, OpenSceneMode.Additive);
        try
        {
            GameManager manager = map.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).Single();
            ColonizationWeek5Setup.ValidateConfiguration(manager);
            for (int i = 0; i < manager.FactionData.Length; i++)
            {
                FactionData data = manager.FactionData[i];
                // The manual exposes this as configurable faction data. These
                // are the initial regional artwork choices, not extra unit types.
                data.isTropicalNatives = data.nation == Nation.Taino || data.nation == Nation.Mexica || data.nation == Nation.Inca;
                EditorUtility.SetDirty(data);
                manager.Factions[i].FactionInit(data);
                PrefabUtility.RecordPrefabInstancePropertyModifications(manager.Factions[i]);
            }
            Canvas canvas = map.GetRootGameObjects().Select(r => r.GetComponent<Canvas>()).Where(c => c != null).Single();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1;
            // The previously unused Canvas is authored with a zero transform scale.
            canvas.transform.localScale = Vector3.one;
            UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            UIManager ui = canvas.GetComponent<UIManager>();
            if (ui == null) ui = Undo.AddComponent<UIManager>(canvas.gameObject);
            GameObject panel = Child(canvas.transform, "TownPanel", new Vector2(1920,1080), Vector2.zero);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one; panelRect.sizeDelta = Vector2.zero;
            Paint(panel, Ink, true);
            GameObject name = Child(panel.transform, "NamePanel", new Vector2(1920,50), new Vector2(0,515));
            Paint(name, new Color(.2f,.14f,.09f), false);
            TMP_Text title = Text(name.transform, "TownTitle", "Settlement", new Vector2(1750,50), Vector2.zero, 30, Paper);
            GameObject content = Child(panel.transform,"ContentPanel",new Vector2(1920,650),new Vector2(0,170));
            GameObject buildings = Child(content.transform,"BuildingPanel",new Vector2(1130,650),new Vector2(-400,0));
            Paint(buildings,Paper,false);
            Text(buildings.transform,"Label","BUILDINGS",new Vector2(1050,60),new Vector2(0,280),32,Ink);
            // Reserve the manual's sixteen building positions without inventing
            // production, costs or unlocked buildings from subsequent weeks.
            for(int i=0;i<16;i++)
            {
                GameObject space=Child(buildings.transform,"BuildingSlot"+i,new Vector2(230,118),new Vector2(-405+(i%4)*270,190-(i/4)*130));
                Paint(space,new Color(.72f,.64f,.49f),false);
            }
            GameObject terrain = Child(content.transform,"TerrainPanel",new Vector2(790,650),new Vector2(565,0));
            Paint(terrain,Color.black,false);
            TerrainSlot center = SceneSlot(terrain.transform,"TerrainSlot",slotPrefab,Vector2.zero,true,map);
            TerrainSlot[] areas = new TerrainSlot[6];
            for(int i=0;i<6;i++) areas[i]=SceneSlot(terrain.transform,"TerrainSlot ("+(i+1)+")",slotPrefab,SlotPositions[i],false,map);
            GameObject unit = Child(panel.transform,"UnitPanel",new Vector2(1920,265),new Vector2(0,-290));
            GameObject outside=Child(unit.transform,"OutsidePanel",new Vector2(725,265),new Vector2(-600,0));
            Paint(outside,new Color(.27f,.35f,.29f),true);
            if(outside.GetComponent<OutsideSlot>()==null) outside.AddComponent<OutsideSlot>();
            UnityEngine.UI.HorizontalLayoutGroup layout=outside.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if(layout==null) layout=outside.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            Layout(layout,TextAnchor.MiddleLeft); layout.padding=new RectOffset(20,20,0,0); layout.spacing=20;
            // Labels live beside the layout container so they cannot become workers.
            Text(unit.transform,"OutsideLabel","OUTSIDE TOWN",new Vector2(650,45),new Vector2(-600,105),25,Paper);
            GameObject port=Child(unit.transform,"PortPanel",new Vector2(500,265),new Vector2(30,0));
            Paint(port,new Color(.23f,.29f,.34f),false);
            Text(port.transform,"Label","PORT",new Vector2(400,50),new Vector2(0,95),25,Paper);
            GameObject special=Child(unit.transform,"SpecialPanel",new Vector2(675,265),new Vector2(620,0));
            Paint(special,new Color(.28f,.24f,.17f),false);
            Text(special.transform,"Label","Drag a unit onto a surrounding Hex to work.\nDrag it outside town to stop working.\nEscape closes this window.",new Vector2(600,180),Vector2.zero,25,Paper);
            GameObject product=Child(panel.transform,"ProductPanel",new Vector2(1920,115),new Vector2(0,-480));
            Paint(product,new Color(.18f,.13f,.085f),false);
            for(int i=0;i<16;i++) Paint(Child(product.transform,"ProductSlot"+i,new Vector2(100,95),new Vector2(-900+i*108,0)),new Color(.32f,.25f,.16f),false);
            GameObject close=Child(product.transform,"ExitButton",new Vector2(150,95),new Vector2(870,0));
            Paint(close,new Color(.48f,.18f,.12f),true);
            UnityEngine.UI.Button button=close.GetComponent<UnityEngine.UI.Button>();
            if(button==null) button=close.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic=close.GetComponent<UnityEngine.UI.Image>();
            Text(close.transform,"Label","EXIT",new Vector2(150,95),Vector2.zero,28,Paper);
            for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--) UnityEventTools.RemovePersistentListener(button.onClick,i);
            UnityEventTools.AddBoolPersistentListener(button.onClick,ui.ToggleTownPanel,false);
            SerializedObject so=new SerializedObject(ui);
            so.FindProperty("townPanel").objectReferenceValue=panel;
            so.FindProperty("centerSlot").objectReferenceValue=center;
            SerializedProperty array=so.FindProperty("areaSlots"); array.arraySize=6;
            for(int i=0;i<6;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=areas[i];
            so.FindProperty("unitDragPrefab").objectReferenceValue=dragPrefab;
            so.FindProperty("outsideTownParent").objectReferenceValue=outside;
            so.FindProperty("townTitle").objectReferenceValue=title;
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(map);
            if(!EditorSceneManager.SaveScene(map)) throw new IOException("Could not save Map01.");
            ValidateConfiguration(manager,ui);
            File.WriteAllText(ResultPath,"PASSED\nNative Plain/Tropical faction configuration, UnitDrag/TerrainSlot prefabs, seven ordered Hex slots, OutsideSlot, TownPanel and EXIT button wired in saved Map01.\nManual dimensions retained; existing assets only. Missing TownMockup replaced by panels; building/resource positions reserved for later weeks.\n");
        }
        finally { if(opened) EditorSceneManager.CloseScene(map,true); }
    }

    private static GameObject CreateTerrainSlot()
    {
        bool existing=AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath)!=null;
        GameObject root=existing?PrefabUtility.LoadPrefabContents(SlotPath):new GameObject("TerrainSlot",typeof(RectTransform));
        try
        {
            Size((RectTransform)root.transform,new Vector2(160,160),Vector2.zero);
            Paint(root,Color.clear,true);
            TerrainSlot slot=root.GetComponent<TerrainSlot>()??root.AddComponent<TerrainSlot>();
            GameObject terrain=Child(root.transform,"TerrainImage",new Vector2(256,384),new Vector2(0,64));
            Paint(terrain,Color.white,false).sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Terrain/Tiles/hexPlains01.png");
            GameObject forest=Child(root.transform,"ForestImage",new Vector2(256,384),new Vector2(0,64));
            Paint(forest,Color.white,false); forest.SetActive(false);
            GameObject town=Child(root.transform,"TownImage",new Vector2(256,384),new Vector2(0,64));
            Paint(town,Color.white,false); town.SetActive(false);
            GameObject yields=Child(root.transform,"YieldParent",new Vector2(250,100),new Vector2(0,40));
            Layout(yields.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>()??yields.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>(),TextAnchor.MiddleCenter);
            GameObject labor=Child(root.transform,"LaborParent",new Vector2(120,100),new Vector2(0,-40));
            Layout(labor.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>()??labor.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>(),TextAnchor.MiddleCenter);
            TMP_Text label=Text(root.transform,"YieldText","XX",new Vector2(80,80),new Vector2(-80,40),48,Color.white); label.gameObject.SetActive(false);
            SerializedObject so=new SerializedObject(slot);
            so.FindProperty("terrainImage").objectReferenceValue=terrain.GetComponent<UnityEngine.UI.Image>();
            so.FindProperty("forestImage").objectReferenceValue=forest.GetComponent<UnityEngine.UI.Image>();
            so.FindProperty("townImage").objectReferenceValue=town.GetComponent<UnityEngine.UI.Image>();
            so.FindProperty("yieldParent").objectReferenceValue=yields.transform;
            so.FindProperty("laborParent").objectReferenceValue=labor.transform;
            so.FindProperty("yieldText").objectReferenceValue=label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root,SlotPath);
        }
        finally { if(existing) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root); }
    }
    private static GameObject CreateUnitDrag()
    {
        bool existing=AssetDatabase.LoadAssetAtPath<GameObject>(DragPath)!=null;
        GameObject root=existing?PrefabUtility.LoadPrefabContents(DragPath):new GameObject("UnitDrag",typeof(RectTransform));
        try
        {
            Size((RectTransform)root.transform,new Vector2(120,160),Vector2.zero);
            UnityEngine.UI.Image image=Paint(root,Color.white,true);
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/LandUnits/FreeColonists.png"); image.preserveAspect=true;
            UnitDrag drag=root.GetComponent<UnitDrag>()??root.AddComponent<UnitDrag>();
            SerializedObject so=new SerializedObject(drag); so.FindProperty("unitImage").objectReferenceValue=image; so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root,DragPath);
        }
        finally { if(existing) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root); }
    }
    private static TerrainSlot SceneSlot(Transform parent,string name,GameObject prefab,Vector2 pos,bool center,Scene scene)
    {
        Transform found=parent.Find(name);
        GameObject obj=found!=null?found.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        obj.name=name; obj.transform.SetParent(parent,false); Size((RectTransform)obj.transform,new Vector2(160,160),pos);
        TerrainSlot slot=obj.GetComponent<TerrainSlot>(); SerializedObject so=new SerializedObject(slot);
        so.FindProperty("centerHex").boolValue=center; so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(slot); PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        return slot;
    }
    private static GameObject Child(Transform parent,string name,Vector2 size,Vector2 pos)
    {
        Transform found=parent.Find(name);
        GameObject obj=found!=null?found.gameObject:new GameObject(name,typeof(RectTransform));
        obj.layer=5; obj.transform.SetParent(parent,false); Size((RectTransform)obj.transform,size,pos); return obj;
    }
    private static void Size(RectTransform rect,Vector2 size,Vector2 pos)
    {
        rect.localScale=Vector3.one; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=pos;
    }
    private static UnityEngine.UI.Image Paint(GameObject obj,Color color,bool raycast)
    {
        UnityEngine.UI.Image image=obj.GetComponent<UnityEngine.UI.Image>()??obj.AddComponent<UnityEngine.UI.Image>();
        image.color=color; image.raycastTarget=raycast; return image;
    }
    private static TMP_Text Text(Transform parent,string name,string value,Vector2 size,Vector2 pos,float fontSize,Color color)
    {
        GameObject obj=Child(parent,name,size,pos);
        TextMeshProUGUI text=obj.GetComponent<TextMeshProUGUI>()??obj.AddComponent<TextMeshProUGUI>();
        text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.text=value; text.fontSize=fontSize; text.color=color; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
        return text;
    }
    private static void Layout(UnityEngine.UI.HorizontalLayoutGroup group,TextAnchor anchor)
    { group.childAlignment=anchor; group.childControlWidth=group.childControlHeight=false; group.childForceExpandWidth=group.childForceExpandHeight=false; }

    public static void ValidateConfiguration(GameManager manager,UIManager ui)
    {
        ColonizationWeek5Setup.ValidateConfiguration(manager);
        if(ui==null || ui.TownPanel==null || ui.CenterSlot==null || !ui.CenterSlot.CenterHex || ui.AreaSlots.Length!=6 || ui.AreaSlots.Any(s=>s==null || s.CenterHex) || ui.OutsideTownParent.GetComponent<OutsideSlot>()==null)
            throw new InvalidOperationException("Week 6 TownPanel references incomplete.");
        Canvas canvas=ui.GetComponent<Canvas>();
        if(canvas==null || canvas.sortingOrder!=1 || canvas.renderMode!=RenderMode.ScreenSpaceOverlay || canvas.transform.localScale.x<=0 || canvas.transform.localScale.y<=0 || canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()==null)
            throw new InvalidOperationException("Town Canvas configuration invalid.");
        if(ui.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EventSystem>(true)).Count()!=1) throw new InvalidOperationException("Map01 requires exactly one EventSystem.");
        for(int i=0;i<6;i++) if(((RectTransform)ui.AreaSlots[i].transform).anchoredPosition!=SlotPositions[i]) throw new InvalidOperationException("Hex slot order/position incorrect.");
        foreach(FactionData data in manager.FactionData)
        {
            bool tropical=data.nation==Nation.Taino || data.nation==Nation.Mexica || data.nation==Nation.Inca;
            if(data.isTropicalNatives!=tropical) throw new InvalidOperationException("Native variant not configured.");
        }
        foreach(string path in new[]{SlotPath,DragPath}) if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null) throw new InvalidOperationException("Missing UI prefab: "+path);
    }
}
