using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ColonizationWeek1To8ArtSetup
{
    public const string Request="Temp/Week1To8Art.request";
    public const string Result="Docs/AI/Validation/Week1To8Art/Import.result.txt";
    public const string Root="Assets/Textures/GeneratedWeek1To8";
    static readonly string[] Products={"Corn","Sugar","Tobacco","Cotton","Furs","Lumber","IronOres","Silver","Horses","Rum","Cigars","Clothes","Coats","TradeGoods","Tools","Musket"};
    static readonly string[] Nations={"England","France","Spain","Netherland","Portugal"};
    static ColonizationWeek1To8ArtSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||File.Exists("Temp/ColonizationEditorRefresh.request")||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        File.Delete(Request);try{Apply();}catch(Exception e){Directory.CreateDirectory(Path.GetDirectoryName(Result));File.WriteAllText(Result,"FAILED\n"+e);Debug.LogException(e);}
    }
    [MenuItem("Tools/Colonization/Apply Generated Week 1-8 Artwork")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("Use saved scenes in Edit Mode.");
        Directory.CreateDirectory(Path.GetDirectoryName(Result));
        foreach(string src in Directory.GetFiles("Art/GeneratedWeek1-8/2026-10-18","*.png",SearchOption.AllDirectories))
        {
            string rel=src.Substring("Art/GeneratedWeek1-8/2026-10-18".Length).TrimStart('/','\\');
            string dst=Root+"/"+rel.Replace('\\','/');Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(src,dst,true);AssetDatabase.ImportAsset(dst,ImportAssetOptions.ForceSynchronousImport);
            var imp=(TextureImporter)AssetImporter.GetAtPath(dst);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=100;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=rel.StartsWith("Products")?512:2048;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
        }
        var products=AssetDatabase.FindAssets("t:ProductData",new[]{"Assets/Scripts/ProductSO"}).Select(g=>AssetDatabase.LoadAssetAtPath<ProductData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(d=>d.id).ToArray();
        if(products.Length!=16)throw new InvalidOperationException("Expected sixteen products.");
        for(int i=0;i<16;i++){Undo.RecordObject(products[i],"Replace product artwork");products[i].icons=i==0?new[]{Sprite("Products/Corn"),Sprite("Products/Fish")}:new[]{Sprite("Products/"+Products[i])};EditorUtility.SetDirty(products[i]);}
        var leaders=Nations.Select(n=>Sprite("Leaders/"+n)).ToArray();
        for(int i=0;i<5;i++){var d=AssetDatabase.LoadAssetAtPath<FactionData>("Assets/Scripts/FactionsSO/"+Nations[i]+".asset");Undo.RecordObject(d,"Replace leader artwork");d.leaderSprite=leaders[i];EditorUtility.SetDirty(d);}
        Scene select=SceneManager.GetSceneByPath(ColonizationWeek8Setup.SelectPath);bool opened=!select.IsValid()||!select.isLoaded;
        if(opened)select=EditorSceneManager.OpenScene(ColonizationWeek8Setup.SelectPath,OpenSceneMode.Additive);
        try
        {
            var screen=select.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SelectNation>(true)).Single();
            var so=new SerializedObject(screen);var array=so.FindProperty("leaderSprites");array.arraySize=5;for(int i=0;i<5;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=leaders[i];so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var card in select.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NationCard>(true)))
            {
                var portrait=card.transform.Find("LeaderImage").GetComponent<Image>();portrait.sprite=leaders[card.NationIndex];
                PrefabUtility.RecordPrefabInstancePropertyModifications(portrait);
            }
            screen.LeaderImage.sprite=leaders[0];screen.LeaderImage.preserveAspect=true;
            EditorSceneManager.MarkSceneDirty(select);EditorSceneManager.SaveScene(select);
        }finally{if(opened)EditorSceneManager.CloseScene(select,true);}
        Scene map=SceneManager.GetSceneByPath(ColonizationWeek8Setup.MapPath);opened=!map.IsValid()||!map.isLoaded;if(opened)map=EditorSceneManager.OpenScene(ColonizationWeek8Setup.MapPath,OpenSceneMode.Additive);
        try
        {
            var ui=map.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UIManager>(true)).Single();
            var town=ui.TownPanel;town.GetComponent<Image>().sprite=Sprite("UI/TownPanel");town.GetComponent<Image>().color=Color.white;
            foreach(string path in new[]{"NamePanel","ContentPanel/BuildingPanel","ContentPanel/TerrainPanel","UnitPanel/OutsidePanel","UnitPanel/PortPanel","UnitPanel/SpecialPanel","ProductPanel"})
            {var image=town.transform.Find(path)?.GetComponent<Image>();if(image!=null)image.color=new Color(1,1,1,0);}
            // Generated backdrop sits beneath existing controls and reserved slots.
            AddGuide(town.transform);
            foreach(var f in map.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Faction>(true)))if(f.IsEuropean)
            {var so=new SerializedObject(f);so.FindProperty("leaderSprite").objectReferenceValue=leaders[(int)f.Nation];so.ApplyModifiedPropertiesWithoutUndo();}
            EditorSceneManager.MarkSceneDirty(map);EditorSceneManager.SaveScene(map);
        }finally{if(opened)EditorSceneManager.CloseScene(map,true);}
        AssetDatabase.SaveAssets();
        File.WriteAllText(Result,"PASSED\n"+DateTime.Now+"\n24 generated PNGs imported: 17 resource icons, 5 fictional European leader portraits, TownPanel background and eight-row production concept guide. ProductData, faction leaders, selection scene and TownPanel wired through Unity APIs.\n");
    }
    public static Sprite Sprite(string path){var s=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+path+".png");if(s==null)throw new InvalidOperationException(path);return s;}
    static void AddGuide(Transform town)
    {
        var panel=Child(town,"ProductionGuide",new Vector2(1560,990),Vector2.zero);var image=panel.GetComponent<Image>()??panel.AddComponent<Image>();image.sprite=Sprite("UI/ProductionChains");image.color=Color.white;image.preserveAspect=true;image.raycastTarget=true;
        var open=Button(town.Find("UnitPanel/SpecialPanel"),"ProductionGuideButton","PRODUCTION GUIDE",new Vector2(280,42),new Vector2(0,-100));
        var close=Button(panel.transform,"CloseButton","CLOSE",new Vector2(140,42),new Vector2(690,465));
        for(int i=open.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(open.onClick,i);
        for(int i=close.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(close.onClick,i);
        UnityEventTools.AddBoolPersistentListener(open.onClick,panel.SetActive,true);UnityEventTools.AddBoolPersistentListener(close.onClick,panel.SetActive,false);panel.transform.SetAsLastSibling();panel.SetActive(false);
    }
    static GameObject Child(Transform parent,string name,Vector2 size,Vector2 pos)
    {var t=parent.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;return go;}
    static Button Button(Transform parent,string name,string label,Vector2 size,Vector2 pos)
    {
        var go=Child(parent,name,size,pos);var image=go.GetComponent<Image>()??go.AddComponent<Image>();image.color=new Color(.25f,.13f,.07f);var button=go.GetComponent<Button>()??go.AddComponent<Button>();button.targetGraphic=image;
        var textGo=Child(go.transform,"Label",size,Vector2.zero);var text=textGo.GetComponent<TextMeshProUGUI>()??textGo.AddComponent<TextMeshProUGUI>();text.text=label;text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");text.fontSize=22;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.9f,.81f,.6f);text.raycastTarget=false;return button;
    }
}
