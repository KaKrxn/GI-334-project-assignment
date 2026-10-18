using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StockSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Town town;
    [SerializeField] private int productId;
    [SerializeField] private int quantity;
    [SerializeField] private TMP_Text stockText;
    [SerializeField] private UnityEngine.UI.Image image;
    [SerializeField] private StockDrag stockDrag;
    [SerializeField] private bool inEurope;
    public bool InEurope { get { return inEurope; } }
    public Town Town { get { return town; } }
    public int ProductId { get { return productId; } }
    public int Quantity { get { return inEurope ? EuropeManager.instance.EuropeStocks[productId].Quantity : town != null && productId >= 0 && productId < town.Warehouse.Length ? town.Warehouse[productId] : 0; } }
    public TMP_Text StockText { get { return stockText; } }
    public UnityEngine.UI.Image Image { get { return image; } }
    public StockDrag StockDrag { get { return stockDrag; } }

    public void StockInit(int id, Town owner, Sprite sprite)
    {
        if (stockDrag != null) stockDrag.CancelDrag();
        town = owner; productId = id; image.sprite = sprite; inEurope = false;
        InitDrag();
    }
    public void StockInitEurope(int id)
    {
        if (stockDrag != null) stockDrag.CancelDrag();
        town = null; productId = id; inEurope = true; image.sprite = GameManager.instance.ProductData[id].icons[0];
        InitDrag();
    }
    private void InitDrag()
    {
        UpdateStockText();
        if (stockDrag == null && UIManager.instance != null && UIManager.instance.StockDragPrefab != null)
        {
            GameObject obj = Instantiate(UIManager.instance.StockDragPrefab, transform);
            stockDrag = obj.GetComponent<StockDrag>();
            obj.transform.position = image.transform.position;
        }
        if (stockDrag != null) stockDrag.StockDragInit(this);
    }
    public void UpdateStockText()
    {
        quantity = Quantity;
        if (stockText != null) stockText.text = inEurope ? EuropeManager.instance.EuropeStocks[productId].BidPrice + "/" + EuropeManager.instance.EuropeStocks[productId].AskPrice : quantity.ToString();
    }
    public void UpdateQuantityStock(int amount)
    {
        if (productId < 0 || productId >= 16 || (long)Quantity + amount < 0 || (long)Quantity + amount > int.MaxValue) return;
        if (inEurope) EuropeManager.instance.EuropeStocks[productId].Quantity += amount;
        else if (town != null) town.Warehouse[productId] += amount;
        UpdateStockText();
    }
    private void OnDisable() { if (stockDrag != null) stockDrag.CancelDrag(); }
    public void ToggleRayCastStockDrag(bool flag) { if (stockDrag != null) stockDrag.Image.raycastTarget = flag; }
    public void OnDrop(PointerEventData data)
    {
        CargoDrag drag = data.pointerDrag != null ? data.pointerDrag.GetComponent<CargoDrag>() : null;
        UIManager ui = UIManager.instance;
        if (drag == null || !drag.IsDragging || drag.Cargo.ProductID != productId || ui == null || !ui.CanLoadStock(this) || !ui.CanUnloadCargo(drag)) return;
        if (inEurope) { if (!EuropeManager.instance.SellCargo(drag, this)) return; }
        else
        {
            if ((long)Quantity + drag.Cargo.Quantity > int.MaxValue || drag.Ship.CurHex != town.CurHex) return;
            UpdateQuantityStock(drag.Cargo.Quantity); drag.AcceptAll();
        }
        NavalUnit owner = drag.Ship; drag.CancelDrag(); ui.UpdateCargoSlots(owner);
    }
}
