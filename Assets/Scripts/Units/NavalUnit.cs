using System.Collections.Generic;
using UnityEngine;

public enum NavalUnitType { Caravel, Merchantman, Galleon, Privateer, Frigate, ManOWar }

public class NavalUnit : Unit
{
    [SerializeField] private NavalUnitType navalUnitType;
    public NavalUnitType NavalUnitType { get { return navalUnitType; } set { navalUnitType = value; } }
    [SerializeField] private bool armed;
    public bool Armed { get { return armed; } }
    [SerializeField] private int cargoHoldNum;
    public int CargoHoldNum { get { return cargoHoldNum; } }
    [SerializeField] private List<LandUnit> passengers = new List<LandUnit>();
    public List<LandUnit> Passengers { get { return passengers; } set { passengers = value; } }
    [SerializeField] private GameObject passengerParent;
    public GameObject PassengerParent { get { return passengerParent; } }
    private readonly HashSet<LandUnit> boardingReservations = new HashSet<LandUnit>();
    [SerializeField] private List<Cargo> cargoList = new List<Cargo>();
    public List<Cargo> CargoList { get { return cargoList; } }
    public bool HasBoardingSpace { get { return passengers.Count + boardingReservations.Count < cargoHoldNum; } }
    public bool HasBoardingReservations { get { return boardingReservations.Count > 0; } }

    public bool TryReserveBoarding(LandUnit unit)
    {
        if (isMoving || !HasBoardingSpace || unit.Faction != faction) return false;
        return boardingReservations.Add(unit);
    }
    public void ReleaseBoarding(LandUnit unit) { boardingReservations.Remove(unit); }
    public bool HasBoardingReservation(LandUnit unit) { return boardingReservations.Contains(unit); }

    public void UnitInit(GameManager manager, Faction fact, NavalUnitData data)
    {
        gameMgr = manager;
        faction = fact;
        unitType = UnitType.Naval;
        flagSprite.sprite = fact.ShieldIcon;
        unitName = data.unitName;
        movePointMax = movePoint = data.movePointMax;
        strength = data.strength;
        visualRange = data.visualRange;
        unitSprite.sprite = data.shipIcon;
        unitStatus = UnitStatus.None;
        navalUnitType = data.navalUnitType;
        armed = data.armed;
        cargoHoldNum = data.cargoHoldNum;
        cargoList.Clear();
    }

    public override void PrepareMoveToHex(Hex target)
    {
        // A friendly town is a port. Check before changing movement/fog state.
        if (target == null || boardingReservations.Count > 0) return;
        if (target.HexType != HexType.Ocean && (target.Town == null || target.Town.Faction != faction)) return;
        base.PrepareMoveToHex(target);
    }

    // Return the amount that did not fit, without taking ownership of the
    // incoming object. Empty gaps preserve the actual hold index.
    public int AddCargo(int holdId, Cargo incoming)
    {
        if (incoming == null) return 0;
        int remaining = incoming.Quantity;
        if (holdId < 0 || holdId >= cargoHoldNum || remaining <= 0 || incoming.ProductID < 0 || incoming.ProductID >= 16) return remaining;
        while (cargoList.Count <= holdId) cargoList.Add(null);
        Cargo existing = cargoList[holdId];
        if (existing != null && existing.Quantity > 0 && existing.ProductID != incoming.ProductID) return remaining;
        int quantity = existing != null ? Mathf.Max(0, existing.Quantity) : 0;
        int accepted = Mathf.Min(remaining, Mathf.Max(0, 100 - quantity));
        if (accepted == 0) return remaining;
        if (existing == null || existing.Quantity == 0) cargoList[holdId] = new Cargo(incoming.ProductID, accepted);
        else existing.Quantity += accepted;
        return remaining - accepted;
    }

    protected override void StayOnHex(Hex hex)
    {
        foreach (LandUnit passenger in passengers)
        {
            if (passenger.CurHex != null) passenger.CurHex.UnitsInHex.Remove(passenger);
            if (!hex.UnitsInHex.Contains(passenger)) hex.UnitsInHex.Add(passenger);
            passenger.CurHex = hex;
            passenger.CurPos = hex.Pos;
        }
        base.StayOnHex(hex);
        if (faction == gameMgr.PlayerFaction && UIManager.instance != null) UIManager.instance.CheckShipArriveWithPassenger(hex);
        if (faction == gameMgr.PlayerFaction) gameMgr.CheckUnmetFaction(this);
        if (faction == gameMgr.PlayerFaction && hex.X == GameManager.WIDTH - 1 && EuropeManager.instance != null) EuropeManager.instance.QuestionGoToEurope(this);
        if (faction != gameMgr.PlayerFaction && destinationHex != null) CheckMoveToDestination();
    }
}
