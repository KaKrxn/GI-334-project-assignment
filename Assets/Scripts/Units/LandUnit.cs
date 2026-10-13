using UnityEngine;
using UnityEngine.InputSystem;

public enum LandUnitType
{
    FreeColonists, VeteranSoldiers, HardyPioneers, PlainIndians, TropicalIndians,
    SeasonedScouts, Farmers, Fishermen, Lumberjacks, Carpenters, OreMiners, Blacksmiths
}

public class LandUnit : Unit
{
    [SerializeField] private LandUnitType landUnitType;
    public LandUnitType LandUnitType { get { return landUnitType; } set { landUnitType = value; } }
    [SerializeField] private int toolsNum;
    public int ToolsNum { get { return toolsNum; } set { toolsNum = value; } }
    [SerializeField] private int musketsNum;
    public int MusketsNum { get { return musketsNum; } set { musketsNum = value; } }
    [SerializeField] private int horseNum;
    public int HorseNum { get { return horseNum; } set { horseNum = value; } }
    [SerializeField] private bool armed;
    public bool Armed { get { return armed; } set { armed = value; } }
    [SerializeField] private bool hasMusket;
    public bool HasMusket { get { return hasMusket; } set { hasMusket = value; } }
    [SerializeField] private bool hasHorse;
    public bool HasHorse { get { return hasHorse; } set { hasHorse = value; } }
    [SerializeField] private NavalUnit transportShip;
    public NavalUnit TransportShip { get { return transportShip; } set { transportShip = value; } }
    private NavalUnit boardingShip;

    public void UnitInit(GameManager manager, Faction fact, LandUnitData data)
    {
        gameMgr = manager;
        faction = fact;
        unitType = UnitType.Land;
        flagSprite.sprite = fact.ShieldIcon;
        unitName = data.unitName;
        movePointMax = movePoint = data.movePointMax;
        strength = data.strength;
        visualRange = data.visualRange;
        unitSprite.sprite = data.unitIcon;
        unitStatus = UnitStatus.None;
        landUnitType = data.landUnitType;
        toolsNum = landUnitType == LandUnitType.HardyPioneers ? 100 : 0;
        armed = data.armed;
        hasMusket = data.hasMusket;
        musketsNum = hasMusket ? 50 : 0;
        hasHorse = data.hasHorse;
        horseNum = hasHorse ? 50 : 0;
        transportShip = null;
    }

    private void MakeLandFall()
    {
        unitStatus = UnitStatus.None;
        transform.SetParent(faction.UnitParent, true);
        transportShip.Passengers.Remove(this);
        transportShip = null;
        // A selected passenger inherits the ship's moving transform while at sea.
        CurPos = transform.position;
    }

    public override void PrepareMoveToHex(Hex target)
    {
        if (target == null || !CanBeginMove(target, true)) return;
        if (target.HexType == HexType.Ocean)
        {
            if (unitStatus == UnitStatus.OnBoard || faction != gameMgr.PlayerFaction) return;
            NavalUnit ship = gameMgr.CheckIfHexHasOurShipToBoard(target);
            if (ship == null || !ship.TryReserveBoarding(this)) return;
            boardingShip = ship;
            base.PrepareMoveToHex(target);
            if (!isMoving) { ship.ReleaseBoarding(this); boardingShip = null; }
            return;
        }
        if (unitStatus == UnitStatus.OnBoard)
        {
            if (transportShip == null || transportShip.IsMoving) return;
            MakeLandFall(); // Only disembark after every movement precondition passed.
        }
        base.PrepareMoveToHex(target);
    }

    public void BoardingShip(NavalUnit ship)
    {
        if (ship == null || isMoving || curHex != ship.CurHex || ship.IsMoving ||
            faction != ship.Faction || ship.Passengers.Contains(this) ||
            unitStatus != UnitStatus.None || transportShip != null ||
            (!ship.HasBoardingReservation(this) && !ship.HasBoardingSpace)) return;
        ship.ReleaseBoarding(this);
        ship.Passengers.Add(this);
        transportShip = ship;
        transform.SetParent(ship.PassengerParent.transform, true);
        transform.localPosition = Vector3.zero;
        unitStatus = UnitStatus.OnBoard;
    }

    protected override void StayOnHex(Hex hex)
    {
        base.StayOnHex(hex);
        if (boardingShip != null)
        {
            BoardingShip(boardingShip);
            boardingShip.ReleaseBoarding(this);
            boardingShip = null;
        }
    }

    protected override void OnDestroy()
    {
        if (boardingShip != null) boardingShip.ReleaseBoarding(this);
        if (transportShip != null) transportShip.Passengers.Remove(this);
        base.OnDestroy();
    }

    private bool CanStartWork()
    {
        return gameMgr != null && gameMgr.PlayerTurn && !gameMgr.TownPanelOpen && faction == gameMgr.PlayerFaction &&
            curHex != null && !isMoving && unitStatus == UnitStatus.None;
    }

    public void ClearingLand()
    {
        if (!CanStartWork()) return;
        if (curHex.HexType == HexType.Ocean || curHex.HexType == HexType.Mountains ||
            curHex.HexType == HexType.Hills || !curHex.HasForest)
            Debug.Log("Must be on Forest");
        else if (toolsNum < 20) Debug.Log("Not enough tools");
        else { unitStatus = UnitStatus.Clearing; toolsNum -= 20; }
    }

    public void BuildSettlement()
    {
        if (!CanStartWork() || curHex.HasTown) return;
        if (curHex.HexType == HexType.Ocean || curHex.HexType == HexType.Mountains || curHex.HasForest)
            Debug.Log("Must be on Cleared Land");
        else if (toolsNum < 20) Debug.Log("Not enough tools");
        else
        {
            foreach (Unit other in faction.Units)
                if (other != this && other.CurHex == curHex && other.UnitStatus == UnitStatus.Building) return;
            unitStatus = UnitStatus.Building;
            toolsNum -= 20;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (Keyboard.current == null || this != gameMgr.CurUnit || !gameMgr.PlayerTurn || gameMgr.TownPanelOpen || isMoving) return;
        if (Keyboard.current.bKey.wasPressedThisFrame) BuildSettlement();
        if (Keyboard.current.pKey.wasPressedThisFrame) ClearingLand();
    }
}
