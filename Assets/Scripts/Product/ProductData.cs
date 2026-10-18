using UnityEngine;

[CreateAssetMenu(fileName = "ProductData", menuName = "Scriptable Objects/ProductData")]
public class ProductData : ScriptableObject
{
    public int id;
    public string productName;
    public Sprite[] icons;
    public int startBid;
    public int startAsk;
}
