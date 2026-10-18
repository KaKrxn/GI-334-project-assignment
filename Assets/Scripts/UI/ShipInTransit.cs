using UnityEngine;

[System.Serializable]
public class ShipInTransit
{
    [SerializeField] private NavalUnit ship;
    [SerializeField] private int turnLeft;
    public NavalUnit Ship { get { return ship; } }
    public int TurnLeft { get { return turnLeft; } set { turnLeft = Mathf.Max(0, value); } }
    public ShipInTransit(NavalUnit s, int turn) { ship = s; turnLeft = turn; }
}
