using System.Collections.Generic;
using UnityEngine;

public enum Nation
{
    England,
    France,
    Spain,
    Netherland,
    Portugal,
    Powhatan,
    Taino,
    Onondaga,
    Mapuche,
    Mexica,
    Wendat,
    Wampanoag,
    Inca
}

public class Faction : MonoBehaviour
{
    [SerializeField] private int money = 1000;
    public int Money { get { return money; } set { money = Mathf.Max(0, value); } }
    [SerializeField] private Sprite leaderSprite;
    [SerializeField] private bool metByPlayer;
    [SerializeField] private bool atPeaceWithPlayer = true;
    public Sprite LeaderSprite { get { return leaderSprite; } }
    public bool MetByPlayer { get { return metByPlayer; } set { metByPlayer = value; } }
    public bool AtPeaceWithPlayer { get { return atPeaceWithPlayer; } set { atPeaceWithPlayer = value; } }
    [SerializeField] private Nation nation;
    public Nation Nation { get { return nation; } }
    [SerializeField] private bool isEuropean;
    public bool IsEuropean { get { return isEuropean; } }
    [SerializeField] private bool isTropicalNatives;
    public bool IsTropicalNatives { get { return isTropicalNatives; } }
    [SerializeField] private Transform townParent;
    public Transform TownParent { get { return townParent; } }
    [SerializeField] private Transform unitParent;
    public Transform UnitParent { get { return unitParent; } }
    [SerializeField] private Sprite flagIcon;
    public Sprite FlagIcon { get { return flagIcon; } }
    [SerializeField] private Sprite shieldIcon;
    public Sprite ShieldIcon { get { return shieldIcon; } }
    [SerializeField] private Sprite unitIcon;
    public Sprite UnitIcon { get { return unitIcon; } }
    [SerializeField] private Sprite townIcon;
    public Sprite TownIcon { get { return townIcon; } }
    [SerializeField] private List<Town> towns = new List<Town>();
    public List<Town> Towns { get { return towns; } set { towns = value; } }
    [SerializeField] private List<Unit> units = new List<Unit>();
    public List<Unit> Units { get { return units; } set { units = value; } }

    public void FactionInit(FactionData data)
    {
        money = 1000;
        leaderSprite = data.leaderSprite;
        metByPlayer = false; atPeaceWithPlayer = true;
        nation = data.nation;
        isEuropean = data.isEuropean;
        isTropicalNatives = data.isTropicalNatives;
        flagIcon = data.flagIcon;
        shieldIcon = data.shieldIcon;
        unitIcon = data.unitIcon;
        townIcon = data.townIcon;
    }
}
