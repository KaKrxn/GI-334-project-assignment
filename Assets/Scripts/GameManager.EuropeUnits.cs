using UnityEngine;

public partial class GameManager
{
    public bool CheckShipPriceAndMoney(int id)
    { return id >= 0 && id < 5 && id < navalUnitData.Length && navalUnitData[id].price > 0 && playerFaction.Money >= navalUnitData[id].price; }
    public bool CheckLandUnitPriceAndMoney(int id)
    { return id >= 0 && id < landUnitData.Length && landUnitData[id].price > 0 && playerFaction.Money >= landUnitData[id].price; }
    // Off-map objects retain faction ownership but never occupy the tutorial's (0,0) placeholder Hex.
    public NavalUnit GenerateHiddenShip(int id)
    {
        GameObject obj = Instantiate(navalUnitPrefab, playerFaction.UnitParent);
        NavalUnit ship = obj.GetComponent<NavalUnit>(); ship.UnitInit(this, playerFaction, navalUnitData[id]);
        ship.UnitStatus = UnitStatus.Hidden; playerFaction.Units.Add(ship); obj.SetActive(false); return ship;
    }
    public LandUnit GenerateHiddenLandUnit(int id)
    {
        GameObject obj = Instantiate(landUnitPrefab, playerFaction.UnitParent);
        LandUnit unit = obj.GetComponent<LandUnit>(); unit.UnitInit(this, playerFaction, landUnitData[id]);
        unit.UnitStatus = UnitStatus.Hidden; playerFaction.Units.Add(unit); obj.SetActive(false); return unit;
    }
    private Hex FindNearestLandHexToLand(int y)
    {
        Hex destination = null;
        for (int x = Mathf.Max(0, oceanEdgeIndex - 3); x <= Mathf.Min(WIDTH - 1, oceanEdgeIndex + 4); x++)
            if (allHexes[x,y].HexType != HexType.Ocean) destination = allHexes[x,y];
        return destination;
    }
}
