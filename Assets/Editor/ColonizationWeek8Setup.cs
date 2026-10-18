using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ColonizationWeek8Setup
{
    public const string MainPath = "Assets/Scenes/MainMenu.unity";
    public const string SelectPath = "Assets/Scenes/SelectNation.unity";
    public const string MapPath = "Assets/Scenes/Map01.unity";
    public const string Request = "Temp/ColonizationWeek8Setup.v1.request";
    public const string Result = "Temp/ColonizationWeek8Setup.result.txt";
    private const string Derived = "Assets/Resources/Week8UI";
    private const string CardPath = "Assets/Prefabs/UI/NationCard.prefab";
    private static readonly string[] Nations = { "England", "France", "Spain", "Netherland", "Portugal" };
    private static readonly Color Gold = new Color32(203,180,135,255);
    private static readonly Color MenuGold = new Color32(203,189,157,255);
    private static readonly Color MenuButtonGold = new Color32(200,173,109,255);

    [InitializeOnLoadMethod] private static void Initialize() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if(File.Exists("Temp/ColonizationEditorStop.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        { File.Delete("Temp/ColonizationEditorStop.request"); if(EditorApplication.isPlaying) EditorApplication.isPlaying=false; return; }
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || File.Exists("Temp/ColonizationEditorRefresh.request")) return;
        File.Delete(Request);
        try { Apply(); } catch (Exception e) { File.WriteAllText(Result,"FAILED\n"+e); Debug.LogException(e); }
    }

    // A native Editor entry point for a closed project as well as the menu.
    public static void ApplyAndQueueSmoke()
    {
        Apply();
        File.WriteAllText(ColonizationWeek8SmokeTest.Request,"run");
    }

    [MenuItem("Tools/Colonization/Apply Week 8 From Existing Assets")]
    public static void Apply()
    {
        Directory.CreateDirectory("Temp");
        if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save open scenes and use Edit Mode before Week 8 setup.");
        if (!AssetDatabase.IsValidFolder(Derived)) AssetDatabase.CreateFolder("Assets/Resources","Week8UI");
        FactionData[] factions = Nations.Select(n=>AssetDatabase.LoadAssetAtPath<FactionData>("Assets/Scripts/FactionsSO/"+n+".asset")).ToArray();
        if (factions.Any(f=>f==null || f.flagIcon==null || f.shieldIcon==null)) throw new InvalidOperationException("Week 5 European faction assets are required.");
        // Derive native Sprite assets from existing textures. Source importers and
        // all their existing sprite GUIDs/references remain intact.
        Slice("MenuFrame","menu_frame_2",new Rect(314,124,710,900),Vector4.zero);
        Slice("WindowFrame","window_frame_4",new Rect(0,0,1024,1024),new Vector4(100,0,100,84));
        Slice("PanelFrame","window_frame_2",new Rect(0,0,512,512),new Vector4(10,10,10,10));
        Slice("CardFrame","window_frame_6",new Rect(16,0,991,512),new Vector4(12,10,12,10));
        Slice("SelectionFrame","map_selection",new Rect(14,4,98,56),new Vector4(5,5,5,5));
        Slice("BottomBar","btn_bg_2",new Rect(428,0,1191,55),Vector4.zero);
        Slice("BackButton","btn_left_1",new Rect(0,20,263,44),Vector4.zero);
        Slice("StartButton","btn_right_2",new Rect(249,20,263,44),Vector4.zero);
        Scene original = SceneManager.GetActiveScene();
        try
        {
            BuildScene(MainPath,BuildMainMenu);
            GameObject card = BuildNationCard();
            BuildScene(SelectPath,scene=>BuildSelectNation(scene,card,factions));
            string[] paths = {MainPath,SelectPath,MapPath};
            EditorBuildSettings.scenes = paths.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!paths.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            ValidateAssets();
            File.WriteAllText(Result,"PASSED\nMainMenu and SelectNation saved through native Unity APIs; NationCard prefab, five European faction toggles, exclusive selection, New Game/Back/Start/Exit wiring, existing Art Deco sprites with manual crops/borders, and MainMenu > SelectNation > Map01 build order.\nLeader portraits are unavailable: existing faction shields are temporary proxies. Load Game/Option remain disabled placeholders; no save/options mechanics or Week 9 production changes.\n");
        }
        finally { if(original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
    }

    private static void BuildScene(string path,Action<Scene> build)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = File.Exists(path) ? EditorSceneManager.OpenScene(path,OpenSceneMode.Additive) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try { build(scene); if(!EditorSceneManager.SaveScene(scene,path)) throw new IOException("Could not save "+path); }
        finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
    }

    private static GameObject CanvasFor(Scene scene,string background)
    {
        GameObject camera = scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="Main Camera");
        if(camera==null) { camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); camera.tag="MainCamera"; camera.transform.position=new Vector3(0,0,-10); camera.GetComponent<Camera>().orthographic=true; }
        GameObject canvas = scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="Canvas");
        if(canvas==null) canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        canvas.layer=5; canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        UnityEngine.UI.CanvasScaler scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        GameObject bg=Child(canvas.transform,"Background",Vector2.zero,Vector2.zero); Stretch((RectTransform)bg.transform); Paint(bg,Source(background),false); bg.transform.SetAsFirstSibling();
        if(!scene.GetRootGameObjects().Any(o=>o.GetComponent<EventSystem>()!=null))
        { GameObject events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions(); }
        return canvas;
    }

    private static void BuildMainMenu(Scene scene)
    {
        GameObject canvas=CanvasFor(scene,"screen_bg_2");
        MainMenu menu=canvas.GetComponent<MainMenu>()??canvas.AddComponent<MainMenu>();
        Text(canvas.transform,"NameText(TMP)","New World",new Vector2(1600,220),new Vector2(0,412),200,MenuGold);
        GameObject frame=Child(canvas.transform,"FramePanel",new Vector2(710,900),new Vector2(0,-90)); Paint(frame,DerivedSprite("MenuFrame"),false);
        GameObject buttons=Child(frame.transform,"MenuPanel",new Vector2(400,200),Vector2.zero);
        UnityEngine.UI.VerticalLayoutGroup layout=buttons.GetComponent<UnityEngine.UI.VerticalLayoutGroup>()??buttons.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childAlignment=TextAnchor.MiddleCenter; layout.childControlWidth=false; layout.childControlHeight=false; layout.childForceExpandWidth=true; layout.childForceExpandHeight=true; layout.spacing=0;
        UnityEngine.UI.Button start=Button(buttons.transform,"NewGameButton","New Game",new Vector2(300,30),Vector2.zero,null,44,MenuButtonGold);
        Wire(start,menu.LoadNextScene);
        Button(buttons.transform,"LoadGameButton","Load Game",new Vector2(300,30),Vector2.zero,null,44,MenuButtonGold).interactable=false;
        Button(buttons.transform,"OptionButton","Option",new Vector2(300,30),Vector2.zero,null,44,MenuButtonGold).interactable=false;
        Paint(Child(frame.transform,"LineImage",new Vector2(256,2),new Vector2(0,135)),Source("sep_closed"),false);
        Paint(Child(frame.transform,"LineImage (1)",new Vector2(256,2),new Vector2(0,-120)),Source("sep_closed"),false);
        Wire(Button(frame.transform,"ExitButton","Exit",new Vector2(400,50),new Vector2(0,-200),null,44,MenuButtonGold),menu.ExitGame);
    }

    private static GameObject BuildNationCard()
    {
        bool existing=AssetDatabase.LoadAssetAtPath<GameObject>(CardPath)!=null;
        GameObject obj=existing?PrefabUtility.LoadPrefabContents(CardPath):new GameObject("NationCard",typeof(RectTransform));
        try
        {
            Size((RectTransform)obj.transform,new Vector2(550,100),Vector2.zero);
            UnityEngine.UI.Toggle toggle=obj.GetComponent<UnityEngine.UI.Toggle>()??obj.AddComponent<UnityEngine.UI.Toggle>();
            GameObject bg=Child(obj.transform,"Background",Vector2.zero,Vector2.zero); Stretch((RectTransform)bg.transform);
            UnityEngine.UI.Image image=Paint(bg,DerivedSprite("CardFrame"),true); image.type=UnityEngine.UI.Image.Type.Sliced;
            GameObject selected=Child(bg.transform,"Checkmark",Vector2.zero,Vector2.zero); Stretch((RectTransform)selected.transform);
            UnityEngine.UI.Image mark=Paint(selected,DerivedSprite("SelectionFrame"),false); mark.type=UnityEngine.UI.Image.Type.Sliced;
            toggle.targetGraphic=image; toggle.graphic=mark; toggle.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;
            UnityEngine.UI.SpriteState state=toggle.spriteState; state.highlightedSprite=DerivedSprite("PanelFrame"); toggle.spriteState=state;
            toggle.SetIsOnWithoutNotify(true);
            NationCard card=obj.GetComponent<NationCard>()??obj.AddComponent<NationCard>();
            while(toggle.onValueChanged.GetPersistentEventCount()>0) UnityEventTools.RemovePersistentListener(toggle.onValueChanged,0);
            UnityEventTools.AddPersistentListener(toggle.onValueChanged,card.OnValueChanged);
            ImageAtLeft(obj.transform,"FlagImage",new Vector2(60,80),60);
            ImageAtLeft(obj.transform,"LeaderImage",new Vector2(80,80),145);
            Text(obj.transform,"NationText(TMP)","England",new Vector2(320,50),new Vector2(90,0),36,Color.white);
            return PrefabUtility.SaveAsPrefabAsset(obj,CardPath);
        }
        finally { if(existing) PrefabUtility.UnloadPrefabContents(obj); else Object.DestroyImmediate(obj); }
    }

    private static void BuildSelectNation(Scene scene,GameObject cardPrefab,FactionData[] factions)
    {
        GameObject canvas=CanvasFor(scene,"screen_bg_1");
        SelectNation screen=canvas.GetComponent<SelectNation>()??canvas.AddComponent<SelectNation>();
        GameObject frame=Child(canvas.transform,"FramePanel",new Vector2(1416,1080),Vector2.zero); UnityEngine.UI.Image outline=Paint(frame,DerivedSprite("WindowFrame"),false); outline.type=UnityEngine.UI.Image.Type.Sliced;
        GameObject title=Child(frame.transform,"TitlePanel",new Vector2(1390,141),new Vector2(0,480.5f)); Paint(title,Source("window_top_4"),false);
        Text(title.transform,"CreateGameText(TMP)","Create Game",new Vector2(600,80),new Vector2(0,-10),64,Gold);
        GameObject choose=Child(frame.transform,"ChooseNationPanel",new Vector2(600,820),new Vector2(-300,0));
        UnityEngine.UI.Image chooseBg=Paint(choose,DerivedSprite("PanelFrame"),false); chooseBg.type=UnityEngine.UI.Image.Type.Sliced; chooseBg.color=new Color(1,1,1,100f/255);
        TMP_Text chooseText=Text(choose.transform,"ChooseText(TMP)","Choose Nation",new Vector2(400,60),new Vector2(0,-40),36,Color.white); chooseText.rectTransform.anchorMin=chooseText.rectTransform.anchorMax=new Vector2(.5f,1);
        GameObject attributes=Child(frame.transform,"NationAttributesPanel",new Vector2(600,820),new Vector2(330,0)); Paint(attributes,null,false).color=new Color(.16f,.12f,.1f,.4f);
        GameObject scroll=Child(choose.transform,"Scroll View",new Vector2(550,715),new Vector2(0,-30)); Paint(scroll,null,true);
        UnityEngine.UI.ScrollRect scrollRect=scroll.GetComponent<UnityEngine.UI.ScrollRect>()??scroll.AddComponent<UnityEngine.UI.ScrollRect>();
        UnityEngine.UI.ToggleGroup group=scroll.GetComponent<UnityEngine.UI.ToggleGroup>()??scroll.AddComponent<UnityEngine.UI.ToggleGroup>(); group.allowSwitchOff=false;
        GameObject viewport=Child(scroll.transform,"Viewport",Vector2.zero,Vector2.zero); Stretch((RectTransform)viewport.transform);
        Paint(viewport,null,true); UnityEngine.UI.Mask mask=viewport.GetComponent<UnityEngine.UI.Mask>()??viewport.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic=false;
        GameObject content=Child(viewport.transform,"Content",new Vector2(0,500),Vector2.zero); RectTransform contentRect=(RectTransform)content.transform;
        contentRect.anchorMin=new Vector2(0,1); contentRect.anchorMax=Vector2.one; contentRect.pivot=new Vector2(.5f,1);
        UnityEngine.UI.VerticalLayoutGroup layout=content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>()??content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childAlignment=TextAnchor.UpperLeft; layout.childControlWidth=true; layout.childControlHeight=false; layout.childForceExpandWidth=true; layout.childForceExpandHeight=false; layout.spacing=0;
        UnityEngine.UI.ContentSizeFitter fit=content.GetComponent<UnityEngine.UI.ContentSizeFitter>()??content.AddComponent<UnityEngine.UI.ContentSizeFitter>(); fit.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained; fit.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content=contentRect; scrollRect.viewport=(RectTransform)viewport.transform; scrollRect.horizontal=false; scrollRect.vertical=true; scrollRect.scrollSensitivity=30;
        // Five 100px cards fit in the 715px viewport. No redundant scrollbar
        // obscures their right edges; the ScrollRect remains usable at smaller views.
        UnityEngine.UI.Toggle[] toggles=new UnityEngine.UI.Toggle[5];
        for(int i=0;i<5;i++)
        {
            Transform found=content.transform.Find("NationCard_"+Nations[i]);
            GameObject card=found!=null?found.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(cardPrefab,content.transform); card.name="NationCard_"+Nations[i];
            UnityEngine.UI.Toggle toggle=card.GetComponent<UnityEngine.UI.Toggle>(); toggle.group=group; toggle.SetIsOnWithoutNotify(i==0); toggles[i]=toggle;
            NationCard binding=card.GetComponent<NationCard>(); Set(binding,"selectNation",screen); Set(binding,"nationIndex",i);
            card.transform.Find("FlagImage").GetComponent<UnityEngine.UI.Image>().sprite=factions[i].flagIcon;
            card.transform.Find("LeaderImage").GetComponent<UnityEngine.UI.Image>().sprite=factions[i].leaderSprite!=null?factions[i].leaderSprite:factions[i].shieldIcon;
            card.transform.Find("NationText(TMP)").GetComponent<TMP_Text>().text=Nations[i];
            PrefabUtility.RecordPrefabInstancePropertyModifications(toggle); PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
            foreach(Component component in card.GetComponentsInChildren<Component>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        GameObject leaderPanel=Child(frame.transform,"LeaderPanel",Vector2.zero,Vector2.zero); Stretch((RectTransform)leaderPanel.transform); ((RectTransform)leaderPanel.transform).offsetMin=new Vector2(708,130); ((RectTransform)leaderPanel.transform).offsetMax=new Vector2(-96,-130);
        // Shield proxies have no portrait's transparent margins. Fit them inside
        // the same panel so they do not cover the nation list or title.
        GameObject leader=Child(leaderPanel.transform,"LeaderImage",new Vector2(500,650),new Vector2(0,52)); UnityEngine.UI.Image leaderImage=Paint(leader,factions[0].leaderSprite!=null?factions[0].leaderSprite:factions[0].shieldIcon,false); leaderImage.preserveAspect=true;
        Paint(Child(canvas.transform,"LeftPanel",new Vector2(400,800),new Vector2(-830,-142.8f)),Source("statue_left"),false);
        Paint(Child(canvas.transform,"RightPanel",new Vector2(400,800),new Vector2(830,-142.8f)),Source("statue_right"),false);
        GameObject down=Child(canvas.transform,"DownPanel",new Vector2(1700,80),new Vector2(0,-500)); Paint(down,DerivedSprite("BottomBar"),false);
        Wire(Button(down.transform,"BackButton","Back",new Vector2(395,50),new Vector2(-600,0),DerivedSprite("BackButton"),30,Color.white),screen.BackToMainMenu);
        Wire(Button(down.transform,"StartGameButton","Start Game",new Vector2(395,50),new Vector2(600,0),DerivedSprite("StartButton"),30,Color.white),screen.StartGame);
        SerializedObject serialized=new SerializedObject(screen); SerializedProperty leaders=serialized.FindProperty("leaderSprites"), nationToggles=serialized.FindProperty("nationToggles"); leaders.arraySize=nationToggles.arraySize=5;
        for(int i=0;i<5;i++) { leaders.GetArrayElementAtIndex(i).objectReferenceValue=factions[i].leaderSprite!=null?factions[i].leaderSprite:factions[i].shieldIcon; nationToggles.GetArrayElementAtIndex(i).objectReferenceValue=toggles[i]; }
        serialized.FindProperty("leaderImage").objectReferenceValue=leaderImage; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void ValidateAssets()
    {
        string[] expected={MainPath,SelectPath,MapPath};
        if(!EditorBuildSettings.scenes.Take(3).Select(s=>s.path).SequenceEqual(expected) || EditorBuildSettings.scenes.Take(3).Any(s=>!s.enabled)) throw new InvalidOperationException("Week 8 build scene order is incorrect.");
        if(expected.Any(p=>AssetDatabase.LoadAssetAtPath<SceneAsset>(p)==null)) throw new InvalidOperationException("Week 8 scene is missing.");
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CardPath);
        if(prefab==null || prefab.GetComponent<NationCard>()==null || prefab.GetComponent<UnityEngine.UI.Toggle>().onValueChanged.GetPersistentMethodName(0)!="OnValueChanged") throw new InvalidOperationException("NationCard prefab is incomplete.");
    }
    private static Sprite Source(string name) { return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art Deco GUI/"+name+".png")??throw new FileNotFoundException(name); }
    private static Sprite DerivedSprite(string name) { return AssetDatabase.LoadAssetAtPath<Sprite>(Derived+"/"+name+".asset")??throw new FileNotFoundException(name); }
    private static void Slice(string name,string textureName,Rect rect,Vector4 border)
    {
        if(AssetDatabase.LoadAssetAtPath<Sprite>(Derived+"/"+name+".asset")!=null) return;
        Texture2D texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art Deco GUI/"+textureName+".png");
        if(texture==null) throw new FileNotFoundException(textureName);
        Sprite sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border); sprite.name=name; AssetDatabase.CreateAsset(sprite,Derived+"/"+name+".asset");
    }
    private static GameObject Child(Transform parent,string name,Vector2 size,Vector2 pos)
    { Transform found=parent.Find(name); GameObject obj=found!=null?found.gameObject:new GameObject(name,typeof(RectTransform)); obj.layer=5; obj.transform.SetParent(parent,false); Size((RectTransform)obj.transform,size,pos); return obj; }
    private static void Size(RectTransform rect,Vector2 size,Vector2 pos) { rect.localScale=Vector3.one; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=size; rect.anchoredPosition=pos; }
    private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
    private static UnityEngine.UI.Image Paint(GameObject obj,Sprite sprite,bool raycast) { UnityEngine.UI.Image image=obj.GetComponent<UnityEngine.UI.Image>()??obj.AddComponent<UnityEngine.UI.Image>(); image.sprite=sprite; image.color=Color.white; image.raycastTarget=raycast; return image; }
    private static void ImageAtLeft(Transform parent,string name,Vector2 size,float x) { GameObject obj=Child(parent,name,size,new Vector2(x,0)); ((RectTransform)obj.transform).anchorMin=((RectTransform)obj.transform).anchorMax=new Vector2(0,.5f); Paint(obj,null,false).preserveAspect=true; }
    private static TMP_Text Text(Transform parent,string name,string value,Vector2 size,Vector2 pos,int fontSize,Color color)
    { GameObject obj=Child(parent,name,size,pos); TMP_Text text=obj.GetComponent<TMP_Text>()??obj.AddComponent<TextMeshProUGUI>(); text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"); text.text=value; text.fontSize=fontSize; text.color=color; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; return text; }
    private static UnityEngine.UI.Button Button(Transform parent,string name,string label,Vector2 size,Vector2 pos,Sprite sprite,int fontSize,Color color)
    { GameObject obj=Child(parent,name,size,pos); UnityEngine.UI.Image image=Paint(obj,sprite,true); if(sprite==null) { image.color=Color.clear; image.canvasRenderer.cullTransparentMesh=false; } UnityEngine.UI.Button button=obj.GetComponent<UnityEngine.UI.Button>()??obj.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image; Text(obj.transform,"Text(TMP)",label,size,Vector2.zero,fontSize,color); return button; }
    private static void Wire(UnityEngine.UI.Button button,UnityEngine.Events.UnityAction callback) { while(button.onClick.GetPersistentEventCount()>0) UnityEventTools.RemovePersistentListener(button.onClick,0); UnityEventTools.AddPersistentListener(button.onClick,callback); }
    private static void Set(Object obj,string field,Object value) { SerializedObject data=new SerializedObject(obj); data.FindProperty(field).objectReferenceValue=value; data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Set(Object obj,string field,int value) { SerializedObject data=new SerializedObject(obj); data.FindProperty(field).intValue=value; data.ApplyModifiedPropertiesWithoutUndo(); }
}
