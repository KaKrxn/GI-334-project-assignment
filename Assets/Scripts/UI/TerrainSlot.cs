using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TerrainSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Hex hex;
    public Hex Hex { get { return hex; } }
    [SerializeField] private UnityEngine.UI.Image terrainImage;
    [SerializeField] private UnityEngine.UI.Image forestImage;
    [SerializeField] private UnityEngine.UI.Image townImage;
    [SerializeField] private Transform yieldParent;
    [SerializeField] private Transform laborParent;
    public Transform LaborParent { get { return laborParent; } }
    [SerializeField] private int[] normalYield = new int[8];
    public int[] NormalYield { get { return normalYield; } set { normalYield = value; } }
    [SerializeField] private int[] actualYield = new int[8];
    public int[] ActualYield { get { return actualYield; } set { actualYield = value; } }
    [SerializeField] private bool centerHex;
    public bool CenterHex { get { return centerHex; } }
    [SerializeField] private TMP_Text yieldText;
    [SerializeField] private List<GameObject> yieldIconList = new List<GameObject>();
    public List<GameObject> YieldIconList { get { return yieldIconList; } }

    public void HexSlotInit(Hex data)
    {
        RemoveYieldIcons();
        hex = data;
        System.Array.Clear(actualYield, 0, actualYield.Length);
        for (int i = 0; i < 8; i++) normalYield[i] = TerrainYield.Calculate(data, i);
        gameObject.SetActive(data != null);
        terrainImage.sprite = data != null ? data.TerrainSprite.sprite : null;
        forestImage.sprite = data != null && data.HasForest ? data.ForestSprite.sprite : null;
        forestImage.gameObject.SetActive(data != null && data.HasForest);
        townImage.sprite = data != null && data.Town != null ? data.Town.TownSprite.sprite : null;
        townImage.gameObject.SetActive(data != null && data.Town != null);
        yieldText.gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        UnitDrag drag = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<UnitDrag>() : null;
        if (centerHex || hex == null || hex.HasTown || hex.Labor != null || drag == null || !drag.IsDragging ||
            UIManager.instance == null || !UIManager.instance.CanAssign(drag)) return;
        drag.QuitOldTerrainSlot();
        drag.WorkAtNewTerrainSlot(this);
        drag.IconParent = laborParent;
        drag.LandUnit.UnitStatus = UnitStatus.WorkInField;
        hex.Labor = drag.LandUnit;
        hex.YieldID = -1;
        UIManager.instance.RefreshTownYield();
        UIManager.instance.SelectProfession(this);
    }

    public void SelectYield(int id)
    {
        if (centerHex || hex == null || hex.HasTown || hex.Labor == null || id < 0 || id >= 8) return;
        hex.YieldID = id;
        UIManager.instance.RefreshTownYield();
    }

    public GameObject GenerateYieldIcon(int id)
    {
        GameObject obj = Instantiate(UIManager.instance.YieldIconPrefab, yieldParent);
        ProductData product = GameManager.instance.ProductData[id];
        obj.GetComponent<UnityEngine.UI.Image>().sprite = product.icons[id == 0 && hex.HexType == HexType.Ocean ? 1 : 0];
        return obj;
    }
    public void GenerateYieldIcons(int id)
    {
        yieldText.text = actualYield[id].ToString();
        yieldText.gameObject.SetActive(true);
        for (int i = 0; i < actualYield[id]; i++) yieldIconList.Add(GenerateYieldIcon(id));
        UIManager.instance.SetupParentSpacing(actualYield[id], yieldParent, 64, 250);
    }
    public void RefreshYield(bool producing)
    {
        RemoveYieldIcons();
        System.Array.Clear(actualYield, 0, actualYield.Length);
        for (int i = 0; i < 8; i++) normalYield[i] = TerrainYield.Calculate(hex, i);
        if (!producing || hex == null || hex.YieldID < 0 || hex.YieldID >= 8) return;
        actualYield[hex.YieldID] = normalYield[hex.YieldID];
        GenerateYieldIcons(hex.YieldID);
    }
    public void RemoveYieldIcons()
    {
        foreach (GameObject obj in yieldIconList)
            if (obj != null) { obj.SetActive(false); Destroy(obj); }
        yieldIconList.Clear();
        if (yieldText != null) yieldText.gameObject.SetActive(false);
        if (yieldParent != null && yieldParent.TryGetComponent(out UnityEngine.UI.HorizontalLayoutGroup layout)) layout.spacing = 0;
    }
}
