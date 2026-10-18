using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CargoDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private UnityEngine.UI.Image image;
    [SerializeField] private TMP_Text cargoText;
    [SerializeField] private NavalUnit ship;
    [SerializeField] private Cargo cargo;
    [SerializeField] private int cargoListId;
    private Transform iconParent;
    private Vector2 startPosition;
    public NavalUnit Ship { get { return ship; } }
    public Cargo Cargo { get { return cargo; } }
    public int HoldId { get { return cargoListId; } }
    public bool IsDragging { get; private set; }
    public void CargoDragInit(NavalUnit owner, Sprite sprite, int id)
    {
        ship = owner; cargoListId = id; image.sprite = sprite;
        Cargo stored = owner.CargoList[id]; cargo = new Cargo(stored.ProductID, stored.Quantity);
        cargoText.text = cargo.Quantity.ToString();
    }
    public void OnBeginDrag(PointerEventData data)
    {
        UIManager ui = UIManager.instance;
        if (IsDragging || ui == null || !ui.CanUnloadCargo(this)) return;
        Cargo stored = ship.CargoList[cargoListId];
        if (stored == null || stored.Quantity <= 0) return;
        cargo = new Cargo(stored.ProductID, stored.Quantity); stored.Quantity = 0;
        iconParent = transform.parent; startPosition = ((RectTransform)transform).anchoredPosition;
        IsDragging = true; image.raycastTarget = false;
        transform.SetParent(ui.transform, true); transform.SetAsLastSibling();
        ui.ToggleStockDragRaycast(false);
    }
    public void OnDrag(PointerEventData data) { if (IsDragging) transform.position = data.position; }
    public void OnEndDrag(PointerEventData data) { CancelDrag(); }
    public void AcceptAll() { if (IsDragging) cargo.Quantity = 0; }
    public void RefreshStored()
    {
        if (IsDragging || ship == null || cargoListId < 0 || cargoListId >= ship.CargoList.Count) return;
        Cargo stored = ship.CargoList[cargoListId];
        cargoText.text = stored != null ? stored.Quantity.ToString() : "0";
        if (stored == null || stored.Quantity <= 0) gameObject.SetActive(false);
    }
    public void CancelDrag()
    {
        if (!IsDragging) return;
        IsDragging = false;
        // The numbered hold is reserved until this drag finishes; never shift later holds.
        if (ship != null && cargo != null && cargo.Quantity > 0) ship.AddCargo(cargoListId, cargo);
        if (iconParent != null) { transform.SetParent(iconParent, false); ((RectTransform)transform).anchoredPosition = startPosition; }
        if (image != null) image.raycastTarget = true;
        if (UIManager.instance != null) { UIManager.instance.ToggleStockDragRaycast(true); UIManager.instance.RefreshCargoTexts(ship); }
    }
    private void OnDisable() { CancelDrag(); }
    private void OnDestroy() { CancelDrag(); }
}
