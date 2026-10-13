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
    public bool HasBoardingSpace { get { return passengers.Count + boardingReservations.Count < cargoHoldNum; } }

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
    }

    public override void PrepareMoveToHex(Hex target)
    {
        // Reject land before changing movement/fog state.
        if (target == null || target.HexType != HexType.Ocean || boardingReservations.Count > 0) return;
        base.PrepareMoveToHex(target);
    }

    protected override void StayOnHex(Hex hex)
    {
        foreach (LandUnit passenger in passengers)
        {
            passenger.CurHex.UnitsInHex.Remove(passenger);
            if (!hex.UnitsInHex.Contains(passenger)) hex.UnitsInHex.Add(passenger);
            passenger.CurHex = hex;
            passenger.CurPos = hex.Pos;
        }
        base.StayOnHex(hex);
    }
}
