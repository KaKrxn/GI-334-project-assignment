using UnityEngine;
using UnityEngine.EventSystems;

public class StockDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private StockSlot stockSlot;
    [SerializeField] private UnityEngine.UI.Image image;
    [SerializeField] private Cargo cargo;
    [SerializeField] private Transform iconParent;
    private Vector2 startPosition;
    public StockSlot StockSlot { get { return stockSlot; } }
    public UnityEngine.UI.Image Image { get { return image; } }
    public Cargo Cargo { get { return cargo; } }
    public bool IsDragging { get; private set; }

    public void StockDragInit(StockSlot slot) { stockSlot = slot; image.sprite = slot.Image.sprite; }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsDragging || UIManager.instance == null || !UIManager.instance.CanLoadStock(stockSlot) || stockSlot.Quantity <= 0) return;
        cargo = new Cargo(stockSlot.ProductId, Mathf.Min(100, stockSlot.Quantity));
        stockSlot.UpdateQuantityStock(-cargo.Quantity);
        iconParent = transform.parent; startPosition = ((RectTransform)transform).anchoredPosition;
        IsDragging = true; image.raycastTarget = false;
        transform.SetParent(UIManager.instance.transform, true); transform.SetAsLastSibling();
    }
    public void OnDrag(PointerEventData eventData) { if (IsDragging) transform.position = eventData.position; }
    public void OnEndDrag(PointerEventData eventData) { CancelDrag(); }
    public void CancelDrag()
    {
        if (!IsDragging) return;
        IsDragging = false;
        // Return only the remainder; CargoSlot has already committed accepted units.
        if (stockSlot != null && cargo != null && cargo.Quantity > 0) stockSlot.UpdateQuantityStock(cargo.Quantity);
        if (cargo != null) cargo.Quantity = 0;
        if (iconParent != null) { transform.SetParent(iconParent, false); ((RectTransform)transform).anchoredPosition = startPosition; }
        if (image != null) image.raycastTarget = true;
    }
    private void OnDisable() { CancelDrag(); }
    private void OnDestroy() { CancelDrag(); }
}
