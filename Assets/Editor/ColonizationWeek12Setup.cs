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
using Object=UnityEngine.Object;

public static class ColonizationWeek12Setup
{
    public const string Request="Temp/ColonizationWeek12Setup.v1.request";
    public const string Result="Temp/ColonizationWeek12Setup.result.txt";
    private static TMP_FontAsset Font { get { return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"); } }
    [InitializeOnLoadMethod] private static void Initialize() { EditorApplication.update+=Tick; }
    private static void Tick()
    {
        if(!File.Exists(Request) || File.Exists("Temp/ColonizationEditorRefresh.request") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Request); try { Apply(); } catch(Exception e) { File.WriteAllText(Result,"FAILED\n"+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Colonization/Apply Week 12 From Existing Assets")]
    public static void Apply()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/Map01.unity" || scene.isDirty || SceneManager.sceneCount!=1) throw new InvalidOperationException("Open saved Map01 alone in Edit Mode for Week 12 setup.");
        GameManager game=Object.FindAnyObjectByType<GameManager>(); UIManager ui=Object.FindAnyObjectByType<UIManager>(); ColonizationWeek11Setup.Validate(game,ui);
        DialogManager manager=ui.GetComponent<DialogManager>(); Transform parent=ui.transform.Find("Dialogs");
        foreach(FactionData data in game.FactionData) if(data.leaderSprite==null) { data.leaderSprite=data.shieldIcon; EditorUtility.SetDirty(data); }
        string[] names={"NavalDialog","AIMoveDialog","ArmyDialog","ForeignDialog","ScoutDialog","KingDialog","ColonyDialog","TradeDialog","ReligiousDialog"};
        Dialog[] dialogs=new Dialog[10];
        for(int i=0;i<9;i++)
        {
            GameObject obj=Child(parent,names[i],new Vector2(1024,717),Vector2.zero); Paint(obj,new Color(.25f,.16f,.09f),true);
            Dialog dialog=Component<Dialog>(obj); dialogs[i]=dialog;
            bool naval=i==0;
            GameObject noti=Child(obj.transform,"NotiPanel",naval?new Vector2(820,144):new Vector2(860,400),naval?new Vector2(0,-160):new Vector2(0,90));
            TMP_Text text=Text(noti.transform,"Text","",naval?new Vector2(800,124):new Vector2(820,380),Vector2.zero,naval?48:38,Color.white);
            text.alignment=TextAlignmentOptions.MidlineLeft; text.enableAutoSizing=true; text.fontSizeMin=naval?32:30; text.fontSizeMax=naval?48:38;
            GameObject answers=Child(obj.transform,"AnswerPanel",new Vector2(800,120),new Vector2(0,-290));
            UnityEngine.UI.VerticalLayoutGroup layout=Component<UnityEngine.UI.VerticalLayoutGroup>(answers); layout.spacing=0; layout.childAlignment=TextAnchor.UpperLeft; layout.childControlWidth=layout.childControlHeight=false; layout.childForceExpandWidth=layout.childForceExpandHeight=true;
            UnityEngine.UI.Button yes=Button(answers.transform,"YesButton",naval?"Yes, steady as she goes.":"Yes",new Vector2(800,60),Vector2.zero,32);
            UnityEngine.UI.Button no=Button(answers.transform,"NoButton",naval?"No, let us remain in these waters.":"No",new Vector2(800,60),Vector2.zero,32);
            UnityEngine.UI.Button okay=Button(answers.transform,"OkayButton",i==5?"As you wish, Sire.":"Okay!",new Vector2(800,60),Vector2.zero,32);
            Set(dialog,"notiText",text); Set(dialog,"yesText",yes.GetComponentInChildren<TMP_Text>()); Set(dialog,"noText",no.GetComponentInChildren<TMP_Text>());
            Set(dialog,"btnYes",yes); Set(dialog,"btnNo",no); Set(dialog,"btnOkay",okay); Set(dialog,"okayText",okay.GetComponentInChildren<TMP_Text>());
            if(naval) { Bind(yes,manager.YesGoToEurope); Bind(no,manager.NoGoToEurope); }
            else if(i==3) { BindBool(yes,manager.MakePeaceWithNation,true); BindBool(no,manager.MakePeaceWithNation,false); }
            else { yes.interactable=no.interactable=false; }
            Bind(okay,manager.AcknowledgeActive);
            GameObject art=Child(obj.transform,"Image",naval?new Vector2(256,256):new Vector2(300,400),naval?new Vector2(0,100):new Vector2(-665,0));
            UnityEngine.UI.Image portrait=Paint(art,Color.white,false); portrait.preserveAspect=true;
            portrait.sprite=naval?game.NavalUnitData[4].shipIcon:i==6?game.LandUnitData[0].unitIcon:game.FactionData[0].leaderSprite;
            Set(dialog,"charImage",portrait);
            yes.gameObject.SetActive(naval || i==3); no.gameObject.SetActive(naval || i==3); okay.gameObject.SetActive(!naval && i!=3 && i!=1);
            obj.SetActive(false);
        }
        dialogs[9]=BuildCombat(parent,manager);
        SerializedObject refs=new SerializedObject(manager); Array(refs.FindProperty("dialogs"),dialogs); refs.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Week 12 Map01."); Validate(game,ui);
        File.WriteAllText(Result,"PASSED\nTen ordered Dialog references, 1200x650 CombatPanel with three 600x140 rows per side, 1024x717 naval/foreign/advisor dialogs, exact manual text/actions, native button wiring and all thirteen leader references saved through Unity APIs. Existing project ship/unit/shield sprites substitute missing portraits/background downloads. Other advisor panels are reserved inactive; no Week 13 systems added.\n");
    }
    private static Dialog BuildCombat(Transform parent,DialogManager manager)
    {
        GameObject obj=Child(parent,"CombatPanel",new Vector2(1200,650),Vector2.zero); Paint(obj,new Color(.25f,.16f,.09f),true); Dialog dialog=Component<Dialog>(obj);
        TMP_Text title=Text(obj.transform,"Title","Combat Analysis",new Vector2(400,60),new Vector2(0,270),42,new Color(1,1,.4f)); title.fontStyle=FontStyles.Bold;
        Text(obj.transform,"AttackText","Attacker",new Vector2(200,50),new Vector2(-350,200),32,Color.white);
        Text(obj.transform,"DefendText","Defender",new Vector2(200,50),new Vector2(350,200),32,Color.white);
        UnityEngine.UI.Image attackFlag=Paint(Child(obj.transform,"AttackImage",new Vector2(64,64),new Vector2(-200,200)),Color.white,false), defendFlag=Paint(Child(obj.transform,"DefendImage",new Vector2(64,64),new Vector2(200,200)),Color.white,false); attackFlag.preserveAspect=defendFlag.preserveAspect=true;
        Set(dialog,"attackerFactionImage",attackFlag); Set(dialog,"defenderFactionImage",defendFlag); Set(dialog,"notiText",title);
        Transform info=Child(obj.transform,"InfoPanel",new Vector2(1200,420),new Vector2(0,-75)).transform;
        for(int side=0;side<2;side++)
        {
            GameObject column=Child(info,side==0?"AttackPanel":"DefendPanel",new Vector2(600,420),new Vector2(side==0?-300:300,0));
            UnityEngine.UI.VerticalLayoutGroup layout=Component<UnityEngine.UI.VerticalLayoutGroup>(column); layout.childAlignment=TextAnchor.UpperLeft; layout.childControlWidth=layout.childControlHeight=false; layout.childForceExpandWidth=layout.childForceExpandHeight=true;
            UnityEngine.UI.Image[] images=new UnityEngine.UI.Image[3]; TMP_Text[] labels=new TMP_Text[3], strengths=new TMP_Text[3];
            for(int i=0;i<3;i++)
            {
                Transform row=Child(column.transform,"UnitPanel"+i,new Vector2(600,140),Vector2.zero).transform;
                images[i]=Paint(Child(row,"Image",new Vector2(128,128),new Vector2(-226,0)),Color.white,false); images[i].preserveAspect=true;
                labels[i]=Text(row,"NameText","",new Vector2(280,60),new Vector2(-5,0),42,new Color(1,1,.4f)); labels[i].enableAutoSizing=true; labels[i].fontSizeMin=28; labels[i].fontSizeMax=42;
                strengths[i]=Text(row,"NumberText","",new Vector2(80,60),new Vector2(175,0),42,Color.white);
            }
            SerializedObject refs=new SerializedObject(dialog); string prefix=side==0?"attacker":"defender"; Array(refs.FindProperty(prefix+"Image"),images); Array(refs.FindProperty(prefix+"NameText"),labels); Array(refs.FindProperty(prefix+"StrText"),strengths); refs.ApplyModifiedPropertiesWithoutUndo();
        }
        UnityEngine.UI.Button exit=Button(obj.transform,"ExitButton","X",new Vector2(40,40),new Vector2(565,290),28); BindBool(exit,manager.ToggleCombatPanel,false);
        obj.SetActive(false); return dialog;
    }
    public static void Validate(GameManager game,UIManager ui)
    {
        ColonizationWeek11Setup.Validate(game,ui); DialogManager manager=ui.GetComponent<DialogManager>();
        if(!manager.HasWeek12Configuration || manager.Dialogs.Any(d=>d==null) || manager.Dialogs.Distinct().Count()!=10 || game.FactionData.Any(d=>d.leaderSprite==null)) throw new InvalidOperationException("Week 12 references incomplete.");
        if(((RectTransform)manager.Dialogs[9].transform).sizeDelta!=new Vector2(1200,650)) throw new InvalidOperationException("CombatPanel dimensions incorrect.");
        for(int i=0;i<9;i++) if(manager.Dialogs[i].OkayButton==null || manager.Dialogs[i].YesButton==null || manager.Dialogs[i].NoButton==null) throw new InvalidOperationException("Dialog buttons missing.");
    }
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
}
