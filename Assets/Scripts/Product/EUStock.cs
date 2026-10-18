using UnityEngine;

[System.Serializable]
public class EUStock
{
    [SerializeField] private int id;
    [SerializeField] private int bidPrice;
    [SerializeField] private int askPrice;
    [SerializeField] private int quantity;
    public int Id { get { return id; } }
    public int BidPrice { get { return bidPrice; } }
    public int AskPrice { get { return askPrice; } }
    public int Quantity { get { return quantity; } set { quantity = Mathf.Max(0, value); } }
    public EUStock(int i, int bid, int ask, int q)
    { id = i; bidPrice = Mathf.Max(0, bid); askPrice = Mathf.Max(bidPrice + 1, ask); quantity = Mathf.Max(0, q); }
    public void UpdatePrice(int increment)
    { bidPrice = Mathf.Max(0, bidPrice + increment); askPrice = Mathf.Max(bidPrice + 1, askPrice + increment); }
}
