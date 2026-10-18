using UnityEngine;

[System.Serializable]
public class Cargo
{
    [SerializeField] private int productId;
    public int ProductID { get { return productId; } set { productId = value; } }
    [SerializeField] private int quantity;
    public int Quantity { get { return quantity; } set { quantity = value; } }

    public Cargo(int id, int amount) { productId = id; quantity = amount; }
}
