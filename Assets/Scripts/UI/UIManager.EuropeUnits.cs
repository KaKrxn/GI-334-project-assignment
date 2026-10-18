using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class UIManager
{
    [Header("Week 13 Units and Orders")]
    [SerializeField] private GameObject purchaseShipPanel;
    [SerializeField] private GameObject trainUnitPanel;
    [SerializeField] private GameObject orderPanel;
    [SerializeField] private GameObject unitOrderBlock;
    [SerializeField] private Transform europePortUnitParent;
    [SerializeField] private TMP_Text orderLabel;
    [SerializeField] private LandUnit curLandUnit;
    [SerializeField] private UnitDrag curUnitIcon;
    private readonly List<GameObject> waitingLandUnitsInEuropeIcons = new List<GameObject>();
    public GameObject PurchaseShipPanel { get { return purchaseShipPanel; } }
    public GameObject TrainUnitPanel { get { return trainUnitPanel; } }
    public GameObject OrderPanel { get { return orderPanel; } }
    public LandUnit CurLandUnit { get { return curLandUnit; } }
    public UnitDrag CurUnitIcon { get { return curUnitIcon; } }
    public List<GameObject> WaitingLandUnitsInEuropeIcons { get { return waitingLandUnitsInEuropeIcons; } }
    public bool UnitMenuOpen { get { return (purchaseShipPanel != null && purchaseShipPanel.activeSelf) || (trainUnitPanel != null && trainUnitPanel.activeSelf) || (orderPanel != null && orderPanel.activeSelf); } }
    public bool HasWeek13Configuration { get { return purchaseShipPanel != null && trainUnitPanel != null && orderPanel != null && unitOrderBlock != null && europePortUnitParent != null; } }
    private bool CanUseEuropeUnits { get { return InEurope && GameManager.instance.PlayerTurn && !(DialogManager.instance != null && DialogManager.instance.IsOpen); } }
    public void TogglePurchasePanel(bool show) { ShowUnitMenu(purchaseShipPanel, show); }
    public void ToggleTrainPanel(bool show) { ShowUnitMenu(trainUnitPanel, show); }
    private void ShowUnitMenu(GameObject panel, bool show)
    {
        if (panel == null || (show && !CanUseEuropeUnits)) return;
        CloseUnitMenus(); if (!show) return;
        CancelTradeDrags(); unitOrderBlock.SetActive(true); unitOrderBlock.transform.SetAsLastSibling();
        panel.SetActive(true); panel.transform.SetAsLastSibling();
        UnityEngine.UI.ScrollRect scroll = panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1;
    }
    public void CloseUnitMenus()
    {
        if (purchaseShipPanel != null) purchaseShipPanel.SetActive(false);
        if (trainUnitPanel != null) trainUnitPanel.SetActive(false);
        if (orderPanel != null) orderPanel.SetActive(false);
        if (unitOrderBlock != null) unitOrderBlock.SetActive(false);
        curLandUnit = null; curUnitIcon = null;
    }
    public void AddNewShipInEurope(int id)
    {
        GameManager game = GameManager.instance;
        if (!CanUseEuropeUnits || purchaseShipPanel == null || !purchaseShipPanel.activeSelf || !game.CheckShipPriceAndMoney(id)) { SetEuropeStatus("Not enough gold to purchase this ship."); return; }
        NavalUnit ship = game.GenerateHiddenShip(id);
        EuropeManager.instance.ShipsInEurope.Add(ship); game.PlayerFaction.Money -= game.NavalUnitData[id].price;
        selectedEuropeShip = ship; CloseUnitMenus(); RefreshEuropeShips(); UpdateMoneyTexts();
        SetEuropeStatus("Purchased " + ship.UnitName + ".");
    }
    public void CreateUnitDragInEuropePort(int id)
    {
        GameManager game = GameManager.instance;
        if (!CanUseEuropeUnits || trainUnitPanel == null || !trainUnitPanel.activeSelf || !game.CheckLandUnitPriceAndMoney(id)) { SetEuropeStatus("Not enough gold to train this specialist."); return; }
        LandUnit unit = game.GenerateHiddenLandUnit(id); EuropeManager.instance.LandUnitsInEurope.Add(unit);
        game.PlayerFaction.Money -= game.LandUnitData[id].price;
        CloseUnitMenus(); SetupUnitDragInEuropePort(); UpdateMoneyTexts();
        SetEuropeStatus("Trained " + unit.UnitName + ". Right-click a waiting colonist to give orders.");
    }
    public void SetupUnitDragInEuropePort()
    {
        ClearEuropeUnitIcons();
        if (!InEurope || europePortUnitParent == null) return;
        foreach (LandUnit unit in EuropeManager.instance.LandUnitsInEurope)
        {
            if (unit == null || unit.IsDead) continue;
            GameObject icon = Instantiate(unitDragPrefab, europePortUnitParent); waitingLandUnitsInEuropeIcons.Add(icon);
            icon.GetComponent<UnitDrag>().UnitInit(unit);
        }
    }
    private void ClearEuropeUnitIcons()
    {
        foreach (GameObject icon in waitingLandUnitsInEuropeIcons) if (icon != null) { icon.SetActive(false); Destroy(icon); }
        waitingLandUnitsInEuropeIcons.Clear();
    }
    public bool CanOrder(UnitDrag icon)
    {
        if (icon == null || icon.IsDragging || UnitMenuOpen || IsProfessionPanelOpen || GameManager.instance == null || !GameManager.instance.PlayerTurn || (DialogManager.instance != null && DialogManager.instance.IsOpen)) return false;
        LandUnit unit = icon.LandUnit;
        if (unit == null || unit.IsDead || unit.Faction != GameManager.instance.PlayerFaction || unit.IsMoving || (unit.UnitStatus != UnitStatus.None && unit.UnitStatus != UnitStatus.ToBoard && unit.UnitStatus != UnitStatus.Hidden)) return false;
        return InEurope ? waitingLandUnitsInEuropeIcons.Contains(icon.gameObject) && EuropeManager.instance.LandUnitsInEurope.Contains(unit) :
            IsTownPanelOpen && allUnitDrags.Contains(icon.gameObject) && unit.CurHex == GameManager.instance.CurTown.CurHex;
    }
    public void OpenUnitOrders(UnitDrag icon)
    {
        if (!CanOrder(icon) || orderPanel == null) return;
        CancelTradeDrags(); curLandUnit = icon.LandUnit; curUnitIcon = icon;
        orderLabel.text = curLandUnit.UnitName + " / " + (curLandUnit.UnitStatus == UnitStatus.ToBoard ? "Board the next ship" : "No orders");
        unitOrderBlock.SetActive(true); unitOrderBlock.transform.SetAsLastSibling(); orderPanel.SetActive(true); orderPanel.transform.SetAsLastSibling();
    }
    public void SetOrderForLandUnit(int id)
    {
        if (orderPanel == null || !orderPanel.activeSelf || curLandUnit == null || curUnitIcon == null || id < 0 || id > 2) return;
        if (id < 2) { curLandUnit.UnitStatus = id == 0 ? UnitStatus.None : UnitStatus.ToBoard; curUnitIcon.CheckStatusIcon(); }
        else if (InEurope)
        {
            List<LandUnit> units = EuropeManager.instance.LandUnitsInEurope; if (units.Remove(curLandUnit)) units.Insert(0,curLandUnit);
            SetupUnitDragInEuropePort();
        }
        else
        {
            List<Unit> units = GameManager.instance.CurTown.CurHex.UnitsInHex; if (units.Remove(curLandUnit)) units.Insert(0,curLandUnit);
            DestroyOldUnitDrag(); SetupUnitDragOutsideTown(GameManager.instance.CurTown.CurHex); SetupUnitDragWorkingInTerrain();
        }
        CloseUnitMenus();
    }
    public void CheckShipArriveWithPassenger(Hex hex)
    {
        if (hex == null || hex.Town == null || hex.Town.Faction != GameManager.instance.PlayerFaction) return;
        foreach (Unit unit in new List<Unit>(hex.UnitsInHex))
            if (unit is NavalUnit ship && ship.Faction == GameManager.instance.PlayerFaction && !ship.IsMoving)
                foreach (Unit waiting in new List<Unit>(hex.UnitsInHex))
                    if (waiting is LandUnit land && land.Faction == ship.Faction && land.UnitStatus == UnitStatus.ToBoard && ship.HasBoardingSpace)
                    {
                        land.UnitStatus = UnitStatus.None; land.BoardingShip(ship);
                        if (land.TransportShip == ship) land.gameObject.SetActive(false); else land.UnitStatus = UnitStatus.ToBoard;
                    }
    }
}
