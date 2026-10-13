using UnityEngine;
using static Hex;

[CreateAssetMenu(fileName = "HexData", menuName = "Scriptable Objects/HexData")]
public class HexData : ScriptableObject
{
    public string hexName;
    public HexType type;
    public Sprite[] terrainSprites;
    public Sprite[] forestSprites;
    public int[] resourceYield = new int[8];
    public int moveCost;
}
