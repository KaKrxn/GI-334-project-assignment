using System.Collections.Generic;
using UnityEngine;

public class EuropeManager : MonoBehaviour
{
    public const int EUROPE_DISTANCE = 3;
    public static EuropeManager instance;
    [SerializeField] private List<ShipInTransit> shipsToEurope = new List<ShipInTransit>();
    [SerializeField] private List<ShipInTransit> shipsFromEurope = new List<ShipInTransit>();
    [SerializeField] private List<NavalUnit> shipsInEurope = new List<NavalUnit>();
    [SerializeField] private EUStock[] europeStocks = new EUStock[16];
    [SerializeField] private NavalUnit curShip;
    [SerializeField] private List<LandUnit> landUnitsInEurope = new List<LandUnit>();
    public List<LandUnit> LandUnitsInEurope { get { return landUnitsInEurope; } }
    private readonly Queue<NavalUnit> questions = new Queue<NavalUnit>();
    private int lastTransitTurn = -1;
    public List<ShipInTransit> ShipsToEurope { get { return shipsToEurope; } }
    public List<ShipInTransit> ShipsFromEurope { get { return shipsFromEurope; } }
    public List<NavalUnit> ShipsInEurope { get { return shipsInEurope; } }
    public EUStock[] EuropeStocks { get { return europeStocks; } }
    public NavalUnit CurShip { get { return curShip; } }
    private void Awake() { instance = this; }
    private void OnDestroy() { if (instance == this) instance = null; }
    public void Initialize()
    {
        shipsToEurope.Clear(); shipsFromEurope.Clear(); shipsInEurope.Clear(); landUnitsInEurope.Clear(); questions.Clear(); curShip = null; lastTransitTurn = -1;
        europeStocks = new EUStock[16];
        for (int i = 0; i < 16; i++) europeStocks[i] = new EUStock(i, GameManager.instance.ProductData[i].startBid, GameManager.instance.ProductData[i].startAsk, 500);
    }
    private bool IsAway(NavalUnit ship)
    {
        if (shipsInEurope.Contains(ship)) return true;
        foreach (ShipInTransit trip in shipsToEurope) if (trip.Ship == ship) return true;
        foreach (ShipInTransit trip in shipsFromEurope) if (trip.Ship == ship) return true;
        return false;
    }
    public bool CanDepart(NavalUnit ship)
    {
        GameManager manager = GameManager.instance;
        return manager != null && manager.PlayerTurn && ship != null && ship.Faction == manager.PlayerFaction &&
            ship.CurHex != null && ship.CurHex.X == GameManager.WIDTH - 1 && ship.CurHex.HexType == HexType.Ocean &&
            !ship.IsMoving && !ship.HasBoardingReservations && !IsAway(ship);
    }
    public void QuestionGoToEurope(NavalUnit ship)
    {
        if (!CanDepart(ship) || curShip == ship || questions.Contains(ship)) return;
        questions.Enqueue(ship); NextQuestion();
    }
    private void NextQuestion()
    {
        if (curShip != null) return;
        while (questions.Count > 0)
        {
            NavalUnit ship = questions.Dequeue(); if (!CanDepart(ship)) continue;
            curShip = ship; CameraController.instance.MoveCamera(ship.CurPos);
            DialogManager.instance.GoToEuropeQuestion(); break;
        }
    }
    public void DeclineGoToEurope() { curShip = null; NextQuestion(); }
    public void AllowToGoToEurope()
    {
        NavalUnit ship = curShip; curShip = null;
        if (CanDepart(ship))
        {
            GameManager manager = GameManager.instance;
            if (UIManager.instance != null) UIManager.instance.CheckActiveUIPanel();
            manager.LeaveSeenFogAroundUnit(ship);
            ship.CurHex.UnitsInHex.Remove(ship); ship.CurHex = null; ship.ToggleBorder(false, Color.green);
            foreach (LandUnit passenger in ship.Passengers)
            {
                if (passenger.CurHex != null) passenger.CurHex.UnitsInHex.Remove(passenger);
                passenger.CurHex = null; passenger.gameObject.SetActive(false);
            }
            ship.gameObject.SetActive(false); shipsToEurope.Add(new ShipInTransit(ship, EUROPE_DISTANCE));
            if (manager.CurUnit == ship || (manager.CurUnit is LandUnit land && land.TransportShip == ship)) manager.CurUnit = null;
            manager.SelectPlayerFirstUnit();
        }
        NextQuestion();
    }
    public void AllowToGoToNewWorld(NavalUnit ship)
    {
        if (UIManager.instance == null || !UIManager.instance.CanSendShip(ship)) return;
        UIManager.instance.CancelTradeDrags();
        CheckIfPassengerInEuropeReadyToBoardShip(ship);
        shipsInEurope.Remove(ship); shipsFromEurope.Add(new ShipInTransit(ship, EUROPE_DISTANCE));
        UIManager.instance.RefreshEuropeShips();
    }
    public void UpdateShipInTransitTurn()
    {
        GameManager manager = GameManager.instance;
        if (!manager.PlayerTurn || lastTransitTurn == manager.GameTurn) return;
        lastTransitTurn = manager.GameTurn;
        for (int i = shipsToEurope.Count - 1; i >= 0; i--)
        {
            ShipInTransit trip = shipsToEurope[i]; trip.TurnLeft--;
            if (trip.TurnLeft > 0) continue;
            if (trip.Ship != null && !shipsInEurope.Contains(trip.Ship))
            {
                shipsInEurope.Add(trip.Ship);
                foreach (LandUnit passenger in trip.Ship.Passengers)
                {
                    passenger.TransportShip = null; passenger.UnitStatus = UnitStatus.None;
                    passenger.transform.SetParent(manager.PlayerFaction.UnitParent, true);
                    passenger.gameObject.SetActive(false);
                    if (!landUnitsInEurope.Contains(passenger)) landUnitsInEurope.Add(passenger);
                }
                trip.Ship.Passengers.Clear();
            }
            shipsToEurope.RemoveAt(i);
        }
        for (int i = shipsFromEurope.Count - 1; i >= 0; i--)
        {
            ShipInTransit trip = shipsFromEurope[i]; trip.TurnLeft--;
            if (trip.TurnLeft > 0) continue;
            if (trip.Ship != null) manager.CheckToGenerateShipFromEurope(trip.Ship);
            shipsFromEurope.RemoveAt(i);
        }
    }
    public int BuyCargo(NavalUnit ship, int hold, Cargo offered)
    {
        if (offered == null || UIManager.instance == null || !UIManager.instance.CanSendShip(ship) || offered.ProductID < 0 || offered.ProductID >= 16) return 0;
        Faction player = GameManager.instance.PlayerFaction; EUStock stock = europeStocks[offered.ProductID];
        int quantity = Mathf.Min(offered.Quantity, stock.AskPrice > 0 ? player.Money / stock.AskPrice : offered.Quantity);
        if (quantity <= 0) { UIManager.instance.SetEuropeStatus("Not enough gold."); return 0; }
        int accepted = quantity - ship.AddCargo(hold, new Cargo(offered.ProductID, quantity));
        player.Money -= accepted * stock.AskPrice;
        UIManager.instance.UpdateMoneyTexts();
        UIManager.instance.SetEuropeStatus(accepted > 0 ? "Bought " + accepted + " " + GameManager.instance.ProductData[offered.ProductID].productName + "." : "This hold cannot accept that cargo.");
        return accepted;
    }
    public void CheckIfPassengerInEuropeReadyToBoardShip(NavalUnit ship)
    {
        if (!shipsInEurope.Contains(ship)) return;
        int goods = 0; foreach (Cargo cargo in ship.CargoList) if (cargo != null && cargo.Quantity > 0) goods++;
        for (int i = 0; i < landUnitsInEurope.Count && ship.Passengers.Count + goods < ship.CargoHoldNum;)
        {
            LandUnit unit = landUnitsInEurope[i];
            if (unit == null || unit.IsDead) { landUnitsInEurope.RemoveAt(i); continue; }
            if (unit.UnitStatus != UnitStatus.ToBoard) { i++; continue; }
            unit.UnitStatus = UnitStatus.OnBoard; unit.TransportShip = ship;
            unit.transform.SetParent(ship.PassengerParent.transform, false); unit.transform.localPosition = Vector3.zero;
            ship.Passengers.Add(unit); unit.gameObject.SetActive(false); landUnitsInEurope.RemoveAt(i);
        }
        UIManager.instance.SetupUnitDragInEuropePort();
    }
    public bool SellCargo(CargoDrag drag, StockSlot destination)
    {
        if (drag == null || !drag.IsDragging || !destination.InEurope || !UIManager.instance.CanUnloadCargo(drag) || drag.Cargo.ProductID != destination.ProductId) return false;
        Cargo cargo = drag.Cargo; EUStock stock = europeStocks[cargo.ProductID]; Faction player = GameManager.instance.PlayerFaction;
        long total = (long)player.Money + (long)cargo.Quantity * stock.BidPrice;
        if (total > int.MaxValue || (long)stock.Quantity + cargo.Quantity > int.MaxValue) return false;
        stock.Quantity += cargo.Quantity; player.Money = (int)total;
        UIManager.instance.SetEuropeStatus("Sold " + cargo.Quantity + " " + GameManager.instance.ProductData[cargo.ProductID].productName + ".");
        drag.AcceptAll(); UIManager.instance.UpdateMoneyTexts(); return true;
    }
    public void UpdateEuropePrice(int id, int increment)
    {
        if (id < 0 || id >= europeStocks.Length || europeStocks[id] == null) return;
        europeStocks[id].UpdatePrice(increment); if (UIManager.instance != null) UIManager.instance.RefreshEuropeStocks();
    }
}
