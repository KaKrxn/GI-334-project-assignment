using UnityEngine;
using UnityEngine.EventSystems;

public class ShipDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private UnityEngine.UI.Image shipImage;
    [SerializeField] private NavalUnit ship;
    private Transform iconParent;
    private Vector2 startPosition;
    public NavalUnit Ship { get { return ship; } }
    public bool IsDragging { get; private set; }
    public void Init(NavalUnit owner) { ship = owner; shipImage.sprite = owner.UnitSprite.sprite; }
    public void OnPointerClick(PointerEventData data)
    { if (data.button == PointerEventData.InputButton.Left && UIManager.instance != null) UIManager.instance.SelectShipInEurope(ship); }
    public void OnBeginDrag(PointerEventData data)
    {
        if (IsDragging || UIManager.instance == null || !UIManager.instance.CanSendShip(ship)) return;
        UIManager.instance.CancelTradeDrags();
        iconParent = transform.parent; startPosition = ((RectTransform)transform).anchoredPosition;
        IsDragging = true; shipImage.raycastTarget = false;
        transform.SetParent(UIManager.instance.transform, true); transform.SetAsLastSibling();
    }
    public void OnDrag(PointerEventData data) { if (IsDragging) transform.position = data.position; }
    public void OnEndDrag(PointerEventData data) { CancelDrag(); }
    public void CancelDrag()
    {
        if (!IsDragging) return;
        IsDragging = false;
        if (iconParent != null) { transform.SetParent(iconParent, false); ((RectTransform)transform).anchoredPosition = startPosition; }
        if (shipImage != null) shipImage.raycastTarget = true;
    }
    public void SetSelected(bool selected) { shipImage.color = selected ? Color.white : new Color(.65f,.65f,.65f); }
    private void OnDisable() { CancelDrag(); }
    private void OnDestroy() { CancelDrag(); }
}
