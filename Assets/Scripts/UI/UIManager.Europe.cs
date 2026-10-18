using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class UIManager
{
    [Header("Week 10–11 Europe")]
    [SerializeField] private GameObject cargoDragPrefab;
    [SerializeField] private GameObject shipDragPrefab;
    [SerializeField] private GameObject europePanel;
    [SerializeField] private Transform toEuropeShipsParent;
    [SerializeField] private Transform fromEuropeShipsParent;
    [SerializeField] private Transform europePortShipsParent;
    [SerializeField] private StockSlot[] stockSlotsEurope;
    [SerializeField] private CargoSlot[] cargoSlotsEurope;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text moneyEuropeText;
    [SerializeField] private TMP_Text europeTitle;
    [SerializeField] private TMP_Text europeStatus;
    [SerializeField] private NavalUnit selectedEuropeShip;
    private readonly List<GameObject> allShipToEuropeIcons = new List<GameObject>();
    private readonly List<GameObject> allShipFromEuropeIcons = new List<GameObject>();
    private readonly List<GameObject> allShipInEuropeIcons = new List<GameObject>();
    public bool InEurope { get { return europePanel != null && europePanel.activeSelf; } }
    public bool HasEuropeConfiguration { get { return europePanel != null && cargoDragPrefab != null && shipDragPrefab != null && stockSlotsEurope != null && stockSlotsEurope.Length == 16 && cargoSlotsEurope != null && cargoSlotsEurope.Length == 6; } }
    public GameObject EuropePanel { get { return europePanel; } }
    public GameObject ShipDragPrefab { get { return shipDragPrefab; } }
    public StockSlot[] StockSlotsEurope { get { return stockSlotsEurope; } }
    public CargoSlot[] CargoSlotsEurope { get { return cargoSlotsEurope; } }
    public NavalUnit SelectedEuropeShip { get { return selectedEuropeShip; } }
    public List<GameObject> AllShipInEuropeIcons { get { return allShipInEuropeIcons; } }
    public List<GameObject> AllShipToEuropeIcons { get { return allShipToEuropeIcons; } }
    public List<GameObject> AllShipFromEuropeIcons { get { return allShipFromEuropeIcons; } }
    public void CheckEuropePanel() { ToggleEuropePanel(!InEurope); }
    public void ToggleEuropePanel(bool show)
    {
        if (!HasEuropeConfiguration) return;
        if (show && (GameManager.instance == null || !GameManager.instance.PlayerTurn || GameManager.instance.CombatInProgress || (DialogManager.instance != null && DialogManager.instance.IsOpen))) return;
        CancelTradeDrags(); CancelShipDrags();
        CloseUnitMenus(); ClearEuropeUnitIcons();
        ClearEuropeIcons(); DisableCargoSet(cargoSlotsEurope); selectedEuropeShip = null;
        if (!show) { europePanel.SetActive(false); return; }
        if (IsTownPanelOpen) ToggleTownPanel(false);
        europePanel.SetActive(true);
        europeTitle.text = GameManager.instance.PlayerFaction.Nation + " / Europe / Year " + GameManager.instance.Year;
        SetEuropeStatus("Drag goods to buy or sell. Drag a docked ship to BOUND FOR NEW WORLD to sail.");
        for (int i = 0; i < stockSlotsEurope.Length; i++) stockSlotsEurope[i].StockInitEurope(i);
        RefreshEuropeShips(); SetupUnitDragInEuropePort(); UpdateMoneyTexts();
    }
    public void RefreshEuropeStocks() { if (InEurope) foreach (StockSlot slot in stockSlotsEurope) slot.UpdateStockText(); }
    public void UpdateMoneyTexts()
    {
        if (GameManager.instance == null || GameManager.instance.PlayerFaction == null) return;
        string text = GameManager.instance.PlayerFaction.Money.ToString();
        if (moneyText != null) moneyText.text = text;
        if (moneyEuropeText != null) moneyEuropeText.text = text;
    }
    public void SetEuropeStatus(string text) { if (europeStatus != null) europeStatus.text = text; }
    public bool CanSendShip(NavalUnit ship)
    {
        return InEurope && !UnitMenuOpen && GameManager.instance.PlayerTurn && ship != null && !ship.IsMoving && ship.Faction == GameManager.instance.PlayerFaction &&
            EuropeManager.instance.ShipsInEurope.Contains(ship) && !(DialogManager.instance != null && DialogManager.instance.IsOpen);
    }
    public bool IsSelectedCargoShip(NavalUnit ship, bool europe)
    {
        return europe ? CanSendShip(ship) && selectedEuropeShip == ship : IsTownPanelOpen && currentShipInPort != null && currentShipInPort.NavalUnit == ship;
    }
    public bool CanUnloadCargo(CargoDrag drag)
    {
        if (drag == null || drag.Ship == null || UnitMenuOpen || !GameManager.instance.PlayerTurn || IsProfessionPanelOpen || (DialogManager.instance != null && DialogManager.instance.IsOpen)) return false;
        NavalUnit ship = drag.Ship;
        if (ship.Faction != GameManager.instance.PlayerFaction || ship.IsMoving) return false;
        if (InEurope) return IsSelectedCargoShip(ship, true);
        return IsSelectedCargoShip(ship, false) && GameManager.instance.CurTown != null && ship.CurHex == GameManager.instance.CurTown.CurHex;
    }
    public void ToggleStockDragRaycast(bool flag)
    {
        if (stockSlots != null) foreach (StockSlot slot in stockSlots) if (slot != null) slot.ToggleRayCastStockDrag(flag);
        if (stockSlotsEurope != null) foreach (StockSlot slot in stockSlotsEurope) if (slot != null) slot.ToggleRayCastStockDrag(flag);
    }
    public void CancelTradeDrags()
    {
        CancelStockDrags();
        if (stockSlotsEurope != null) foreach (StockSlot slot in stockSlotsEurope) if (slot != null && slot.StockDrag != null) slot.StockDrag.CancelDrag();
        CancelCargoSet(cargoSlots); CancelCargoSet(cargoSlotsEurope); ToggleStockDragRaycast(true);
    }
    private static void CancelCargoSet(CargoSlot[] slots)
    { if (slots != null) foreach (CargoSlot slot in slots) if (slot != null && slot.CargoDrag != null) slot.CargoDrag.CancelDrag(); }
    private void CancelShipDrags()
    { foreach (GameObject obj in allShipInEuropeIcons) if (obj != null && obj.GetComponent<ShipInPort>().ShipDrag != null) obj.GetComponent<ShipInPort>().ShipDrag.CancelDrag(); }
    private static void DisableCargoSet(CargoSlot[] slots)
    { if (slots != null) foreach (CargoSlot slot in slots) if (slot != null) { slot.Clear(); slot.gameObject.SetActive(false); } }
    public void RefreshCargoTexts(NavalUnit ship)
    {
        foreach (CargoSlot[] slots in new[] { cargoSlots, cargoSlotsEurope })
            if (slots != null) foreach (CargoSlot slot in slots) if (slot != null && slot.Ship == ship && slot.CargoDrag != null) slot.CargoDrag.RefreshStored();
    }
    public void UpdateCargoSlots(NavalUnit ship)
    {
        CargoSlot[] slots = InEurope ? cargoSlotsEurope : cargoSlots;
        if (slots == null || cargoDragPrefab == null || ship == null) return;
        CancelCargoSet(slots);
        foreach (CargoSlot slot in slots)
            if (slot.CargoDrag != null) { slot.CargoDrag.gameObject.SetActive(false); Destroy(slot.CargoDrag.gameObject); slot.CargoDrag = null; }
        for (int i = 0; i < slots.Length && i < ship.CargoList.Count && i < ship.CargoHoldNum; i++)
        {
            Cargo stored = ship.CargoList[i];
            if (stored == null || stored.Quantity <= 0 || !slots[i].gameObject.activeSelf) continue;
            GameObject obj = Instantiate(cargoDragPrefab, slots[i].transform); CargoDrag drag = obj.GetComponent<CargoDrag>();
            drag.CargoDragInit(ship, GameManager.instance.ProductData[stored.ProductID].icons[0], i); slots[i].CargoDrag = drag;
        }
    }
    public void SelectShipInEurope(NavalUnit ship)
    {
        if (!CanSendShip(ship)) return;
        CancelTradeDrags(); selectedEuropeShip = ship;
        foreach (GameObject obj in allShipInEuropeIcons) obj.GetComponent<ShipInPort>().SetSelected(obj.GetComponent<ShipInPort>().NavalUnit == ship);
        for (int i = 0; i < cargoSlotsEurope.Length; i++)
        {
            cargoSlotsEurope[i].Clear(); bool enabled = i < ship.CargoHoldNum;
            cargoSlotsEurope[i].gameObject.SetActive(enabled); if (enabled) cargoSlotsEurope[i].SlotInit(ship, i, true);
        }
        UpdateCargoSlots(ship);
    }
    public void RefreshEuropeShips()
    {
        if (!InEurope) return;
        CancelTradeDrags(); CancelShipDrags(); ClearEuropeIcons(); DisableCargoSet(cargoSlotsEurope);
        EuropeManager europe = EuropeManager.instance;
        foreach (ShipInTransit trip in europe.ShipsToEurope) AddEuropeIcon(trip.Ship, toEuropeShipsParent, allShipToEuropeIcons, false, trip.TurnLeft);
        foreach (ShipInTransit trip in europe.ShipsFromEurope) AddEuropeIcon(trip.Ship, fromEuropeShipsParent, allShipFromEuropeIcons, false, trip.TurnLeft);
        foreach (NavalUnit ship in europe.ShipsInEurope) AddEuropeIcon(ship, europePortShipsParent, allShipInEuropeIcons, true, 0);
        if (selectedEuropeShip == null || !europe.ShipsInEurope.Contains(selectedEuropeShip)) selectedEuropeShip = europe.ShipsInEurope.Count > 0 ? europe.ShipsInEurope[0] : null;
        if (selectedEuropeShip != null) SelectShipInEurope(selectedEuropeShip);
    }
    private void AddEuropeIcon(NavalUnit ship, Transform parent, List<GameObject> list, bool drag, int turns)
    {
        if (ship == null) return;
        GameObject obj = Instantiate(shipInPortPrefab, parent); list.Add(obj); ShipInPort icon = obj.GetComponent<ShipInPort>(); icon.UnitInit(ship, drag);
        if (turns > 0) icon.UpdateTurnText(turns);
    }
    private void ClearEuropeIcons()
    {
        foreach (List<GameObject> list in new[] { allShipToEuropeIcons, allShipFromEuropeIcons, allShipInEuropeIcons })
        { foreach (GameObject obj in list) if (obj != null) { obj.SetActive(false); Destroy(obj); } list.Clear(); }
    }
}
