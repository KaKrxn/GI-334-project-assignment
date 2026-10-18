using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class UIManager : MonoBehaviour
{
    public static UIManager instance;
    [SerializeField] private GameObject townPanel;
    public GameObject TownPanel { get { return townPanel; } }
    public bool IsTownPanelOpen { get { return townPanel != null && townPanel.activeSelf; } }
    [SerializeField] private TerrainSlot centerSlot;
    public TerrainSlot CenterSlot { get { return centerSlot; } }
    [SerializeField] private TerrainSlot[] areaSlots;
    public TerrainSlot[] AreaSlots { get { return areaSlots; } }
    [SerializeField] private GameObject unitDragPrefab;
    [SerializeField] private GameObject outsideTownParent;
    public GameObject OutsideTownParent { get { return outsideTownParent; } }
    [SerializeField] private GameObject yieldIconPrefab;
    public GameObject YieldIconPrefab { get { return yieldIconPrefab; } }
    [SerializeField] private List<GameObject> allUnitDrags = new List<GameObject>();
    public List<GameObject> AllUnitDrags { get { return allUnitDrags; } }
    [SerializeField] private TMP_Text townTitle;
    [Header("Week 7 Yield")]
    [SerializeField] private TerrainSlot currentSlot;
    [SerializeField] private GameObject blockImage;
    [SerializeField] private GameObject professionPanel;
    [SerializeField] private TMP_Text labelQuestionText;
    [SerializeField] private TMP_Text[] btnYieldTexts;
    [SerializeField] private UnityEngine.UI.Image foodProfessionIcon;
    [SerializeField] private UnityEngine.UI.Image fishProfessionIcon;
    [SerializeField] private Transform foodParent;
    [SerializeField] private TMP_Text foodText;
    [SerializeField] private List<GameObject> foodIconList = new List<GameObject>();
    [SerializeField] private TMP_Text[] warehouseTexts;
    public bool HasYieldConfiguration { get { return yieldIconPrefab != null && professionPanel != null; } }
    public bool IsProfessionPanelOpen { get { return professionPanel != null && professionPanel.activeSelf; } }
    public TerrainSlot CurrentSlot { get { return currentSlot; } }
    public GameObject ProfessionPanel { get { return professionPanel; } }
    public GameObject BlockImage { get { return blockImage; } }
    public TMP_Text FoodText { get { return foodText; } }
    public List<GameObject> FoodIconList { get { return foodIconList; } }
    [Header("Week 9 Stock and Port")]
    [SerializeField] private StockSlot[] stockSlots;
    [SerializeField] private GameObject stockDragPrefab;
    [SerializeField] private GameObject shipInPortPrefab;
    [SerializeField] private GameObject portShipsParent;
    [SerializeField] private CargoSlot[] cargoSlots;
    [SerializeField] private List<GameObject> allShipIcons = new List<GameObject>();
    [SerializeField] private ShipInPort currentShipInPort;
    public StockSlot[] StockSlots { get { return stockSlots; } }
    public CargoSlot[] CargoSlots { get { return cargoSlots; } }
    public GameObject StockDragPrefab { get { return stockDragPrefab; } }
    public List<GameObject> AllShipIcons { get { return allShipIcons; } }
    public ShipInPort CurrentShipInPort { get { return currentShipInPort; } }
    public bool HasStockConfiguration { get { return stockSlots != null && stockSlots.Length == 16 && stockDragPrefab != null && cargoSlots != null && cargoSlots.Length == 6; } }

    private void Awake() { instance = this; }
    private void OnDestroy() { if (instance == this) instance = null; }
    private void Update()
    {
        if (UnitMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { CloseUnitMenus(); return; }
        if (InEurope && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { ToggleEuropePanel(false); return; }
        if (IsTownPanelOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsProfessionPanelOpen) CancelProfession();
            else ToggleTownPanel(false);
        }
    }

    public bool CanAssign(UnitDrag drag)
    {
        GameManager manager = GameManager.instance;
        LandUnit unit = drag != null ? drag.LandUnit : null;
        return IsTownPanelOpen && !UnitMenuOpen && !IsProfessionPanelOpen && manager != null && manager.PlayerTurn && manager.CurTown != null &&
            manager.CurTown.Faction == manager.PlayerFaction && unit != null && unit.Faction == manager.PlayerFaction &&
            unit.CurHex == manager.CurTown.CurHex && !unit.IsMoving &&
            (unit.UnitStatus == UnitStatus.None || unit.UnitStatus == UnitStatus.WorkInField);
    }

    public void SetupHexSlots(Hex centerHex, Hex[] aroundHexes)
    {
        centerSlot.HexSlotInit(centerHex);
        for (int i = 0; i < areaSlots.Length; i++) areaSlots[i].HexSlotInit(aroundHexes[i]);
    }
    private void DestroyOldUnitDrag()
    {
        foreach (GameObject obj in allUnitDrags)
        {
            if (obj == null) continue;
            obj.GetComponent<UnitDrag>().CancelDrag();
            obj.SetActive(false);
            Destroy(obj);
        }
        allUnitDrags.Clear();
    }
    public void SetupUnitDragOutsideTown(Hex hex)
    {
        foreach (Unit unit in hex.UnitsInHex)
            if (unit is LandUnit land && unit.Faction == GameManager.instance.PlayerFaction && (unit.UnitStatus == UnitStatus.None || unit.UnitStatus == UnitStatus.ToBoard))
                CreateDrag(land, outsideTownParent.transform, null);
    }
    public void SetupUnitDragWorkingInTerrain()
    {
        foreach (TerrainSlot slot in areaSlots)
            if (slot.Hex != null && slot.Hex.Labor is LandUnit land && land.Faction == GameManager.instance.PlayerFaction &&
                land.CurHex == centerSlot.Hex && land.UnitStatus == UnitStatus.WorkInField)
                CreateDrag(land, slot.LaborParent, slot);
    }
    private void CreateDrag(LandUnit unit, Transform parent, TerrainSlot slot)
    {
        GameObject obj = Instantiate(unitDragPrefab, parent);
        allUnitDrags.Add(obj);
        UnitDrag drag = obj.GetComponent<UnitDrag>();
        drag.UnitInit(unit);
        drag.WorkAtNewTerrainSlot(slot);
    }
    public void SetupCurrentTown(Hex centerHex, Hex[] aroundHexes)
    {
        CloseUnitMenus(); CheckShipArriveWithPassenger(centerHex);
        if (InEurope) ToggleEuropePanel(false);
        CancelTradeDrags();
        DestroyAllShipIcons();
        DisableAllCargoSlots();
        CancelProfession();
        RemoveAllYieldIcons();
        DestroyOldUnitDrag();
        SetupHexSlots(centerHex, aroundHexes);
        SetupUnitDragOutsideTown(centerHex);
        SetupUnitDragWorkingInTerrain();
        townTitle.text = GameManager.instance.CurTown.Faction.Nation + " Settlement " +
            (GameManager.instance.CurTown.Faction.Towns.IndexOf(GameManager.instance.CurTown) + 1) +
            "  /  Turn " + GameManager.instance.GameTurn;
        RefreshTownYield();
        SetupStockSlots(centerHex);
        SetupShipsInPort(centerHex);
        UpdateMoneyTexts();
    }
    public void ToggleTownPanel(bool show)
    {
        if (!show)
        {
            CloseUnitMenus();
            CancelTradeDrags();
            DestroyAllShipIcons();
            DisableAllCargoSlots();
            CancelProfession();
            RemoveAllYieldIcons();
            DestroyOldUnitDrag();
            GameManager manager = GameManager.instance;
            if (manager != null && manager.CurTown != null)
                foreach (Unit unit in manager.CurTown.CurHex.UnitsInHex)
                    if (unit.UnitType == UnitType.Land)
                        unit.gameObject.SetActive(unit.UnitStatus != UnitStatus.WorkInField && unit.UnitStatus != UnitStatus.WorkInTown);
            townPanel.SetActive(false);
            if (manager != null && manager.CurUnit != null &&
                (manager.CurUnit.UnitStatus == UnitStatus.WorkInField || manager.CurUnit.UnitStatus == UnitStatus.WorkInTown))
                manager.SelectPlayerFirstUnit();
            return;
        }
        townPanel.SetActive(true);
    }

    public void SetupParentSpacing(int n, Transform parent, int iconWidth, int parentWidth)
    {
        UnityEngine.UI.HorizontalLayoutGroup layout = parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = n > 1 && iconWidth * n > parentWidth ? -(iconWidth * n - parentWidth) / (float)(n - 1) : 0;
    }
    public void RefreshTownYield()
    {
        GameManager manager = GameManager.instance;
        if (!HasYieldConfiguration || manager == null || manager.CurTown == null || centerSlot.Hex != manager.CurTown.CurHex) return;
        Town town = manager.CurTown;
        town.RecalculateYield();
        centerSlot.RefreshYield(true);
        foreach (TerrainSlot slot in areaSlots)
        {
            Unit worker = slot.Hex != null ? slot.Hex.Labor : null;
            slot.RefreshYield(worker != null && worker.Faction == town.Faction && worker.CurHex == town.CurHex && worker.UnitStatus == UnitStatus.WorkInField && !slot.Hex.HasTown);
        }
        UpdateTotalFoodIcons();
        for (int i = 0; i < warehouseTexts.Length; i++) warehouseTexts[i].text = town.Warehouse[i].ToString();
        if (HasStockConfiguration) foreach (StockSlot slot in stockSlots) slot.UpdateStockText();
    }
    public void RemoveAllYieldIcons()
    {
        centerSlot.RemoveYieldIcons();
        foreach (TerrainSlot slot in areaSlots) slot.RemoveYieldIcons();
        RemoveFoodIcons();
    }
    private void RemoveFoodIcons()
    {
        foreach (GameObject obj in foodIconList)
            if (obj != null) { obj.SetActive(false); Destroy(obj); }
        foodIconList.Clear();
        if (foodText != null) foodText.gameObject.SetActive(false);
        if (foodParent != null) SetupParentSpacing(0, foodParent, 64, 300);
    }
    public GameObject GenerateFoodIcon()
    {
        GameObject obj = Instantiate(yieldIconPrefab, foodParent);
        obj.GetComponent<UnityEngine.UI.Image>().sprite = GameManager.instance.ProductData[0].icons[0];
        return obj;
    }
    public void UpdateTotalFoodIcons()
    {
        RemoveFoodIcons();
        int count = GameManager.instance.CurTown.TotalYieldThisTurn[0];
        foodText.text = count.ToString();
        foodText.gameObject.SetActive(true);
        for (int i = 0; i < count; i++) foodIconList.Add(GenerateFoodIcon());
        SetupParentSpacing(count, foodParent, 64, 300);
    }
    public void UpdateLabelQuestionText()
    {
        labelQuestionText.text = "Select a profession for " + currentSlot.Hex.Labor.UnitName;
    }
    public void UpdateButtonTextsYield()
    {
        for (int i = 0; i < btnYieldTexts.Length; i++)
            btnYieldTexts[i].text = currentSlot.NormalYield[i] + " " + GameManager.instance.ProductData[i].productName;
        bool ocean = currentSlot.Hex.HexType == HexType.Ocean;
        foodProfessionIcon.gameObject.SetActive(!ocean);
        fishProfessionIcon.gameObject.SetActive(ocean);
    }
    public void SelectProfession(TerrainSlot slot)
    {
        if (!HasYieldConfiguration || !IsTownPanelOpen || IsProfessionPanelOpen || slot == null || slot.CenterHex || slot.Hex == null || slot.Hex.HasTown || slot.Hex.Labor == null) return;
        Town town = GameManager.instance.CurTown;
        if (town == null || slot.Hex.Labor.Faction != town.Faction || slot.Hex.Labor.CurHex != town.CurHex) return;
        currentSlot = slot;
        UpdateLabelQuestionText();
        UpdateButtonTextsYield();
        blockImage.SetActive(true);
        professionPanel.SetActive(true);
        blockImage.transform.SetAsLastSibling();
        professionPanel.transform.SetAsLastSibling();
    }
    public void SelectYield(int id)
    {
        if (!IsTownPanelOpen || !IsProfessionPanelOpen || currentSlot == null || id < 0 || id >= 8) return;
        currentSlot.SelectYield(id);
        CloseProfession();
    }
    private void CloseProfession()
    {
        if (blockImage != null) blockImage.SetActive(false);
        if (professionPanel != null) professionPanel.SetActive(false);
        currentSlot = null;
    }
    public void CancelProfession()
    {
        // Cancel a new uncommitted job; changing an existing job keeps its yield.
        if (currentSlot != null && currentSlot.Hex != null && currentSlot.Hex.YieldID == -1)
        {
            Unit worker = currentSlot.Hex.Labor;
            foreach (GameObject obj in allUnitDrags)
            {
                if (obj == null) continue;
                UnitDrag drag = obj.GetComponent<UnitDrag>();
                if (drag.LandUnit != worker) continue;
                drag.QuitOldTerrainSlot();
                drag.LandUnit.UnitStatus = UnitStatus.None;
                drag.IconParent = outsideTownParent.transform;
                drag.CancelDrag();
                drag.transform.SetParent(drag.IconParent, false);
                ((RectTransform)drag.transform).anchoredPosition = Vector2.zero;
                break;
            }
        }
        CloseProfession();
    }

    public void CheckActiveUIPanel() { if (IsTownPanelOpen) ToggleTownPanel(false); if (InEurope) ToggleEuropePanel(false); }
    public bool CanLoadStock(StockSlot slot)
    {
        GameManager manager = GameManager.instance;
        if (manager == null || !manager.PlayerTurn || UnitMenuOpen || slot == null || (DialogManager.instance != null && DialogManager.instance.IsOpen)) return false;
        if (slot.InEurope) return InEurope && System.Array.IndexOf(stockSlotsEurope, slot) >= 0;
        return IsTownPanelOpen && !IsProfessionPanelOpen && manager != null && manager.PlayerTurn &&
            slot != null && slot.Town != null && slot.Town == manager.CurTown && slot.Town.Faction == manager.PlayerFaction;
    }
    private void CancelStockDrags()
    {
        if (stockSlots == null) return;
        foreach (StockSlot slot in stockSlots) if (slot != null && slot.StockDrag != null) slot.StockDrag.CancelDrag();
    }
    public void SetupStockSlots(Hex hex)
    {
        if (!HasStockConfiguration) return;
        for (int i = 0; i < stockSlots.Length; i++) stockSlots[i].StockInit(i, hex.Town, GameManager.instance.ProductData[i].icons[0]);
    }
    private void SetupShipsInPort(Hex hex)
    {
        if (!HasStockConfiguration || shipInPortPrefab == null) return;
        foreach (Unit unit in hex.UnitsInHex)
        {
            if (!(unit is NavalUnit ship) || ship.Faction != GameManager.instance.PlayerFaction || ship.IsMoving) continue;
            GameObject obj = Instantiate(shipInPortPrefab, portShipsParent.transform);
            ShipInPort icon = obj.GetComponent<ShipInPort>(); icon.UnitInit(ship); allShipIcons.Add(obj);
            if (currentShipInPort == null) currentShipInPort = icon;
            icon.SetSelected(icon == currentShipInPort);
        }
        if (currentShipInPort != null) SetupCargoSlot(currentShipInPort.NavalUnit);
    }
    public void SelectShipInPort(ShipInPort icon)
    {
        GameManager manager = GameManager.instance;
        if (!IsTownPanelOpen || IsProfessionPanelOpen || manager == null || !manager.PlayerTurn || icon == null ||
            !allShipIcons.Contains(icon.gameObject) || icon.NavalUnit.IsMoving || icon.NavalUnit.Faction != manager.PlayerFaction || icon.NavalUnit.CurHex != manager.CurTown.CurHex) return;
        CancelTradeDrags();
        currentShipInPort = icon;
        foreach (GameObject obj in allShipIcons) obj.GetComponent<ShipInPort>().SetSelected(obj == icon.gameObject);
        SetupCargoSlot(icon.NavalUnit);
    }
    public void SetupCargoSlot(NavalUnit ship)
    {
        if (cargoSlots == null) return;
        for (int i = 0; i < cargoSlots.Length; i++)
        {
            bool enabled = ship != null && i < ship.CargoHoldNum;
            cargoSlots[i].gameObject.SetActive(enabled);
            if (enabled) cargoSlots[i].SlotInit(ship, i); else cargoSlots[i].Clear();
        }
        UpdateCargoSlots(ship);
    }
    private void DisableAllCargoSlots()
    {
        if (cargoSlots == null) return;
        foreach (CargoSlot slot in cargoSlots) { slot.Clear(); slot.gameObject.SetActive(false); }
    }
    private void DestroyAllShipIcons()
    {
        foreach (GameObject obj in allShipIcons) if (obj != null) { obj.SetActive(false); Destroy(obj); }
        allShipIcons.Clear(); currentShipInPort = null;
    }
}
