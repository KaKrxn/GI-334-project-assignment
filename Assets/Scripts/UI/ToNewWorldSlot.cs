using UnityEngine;
using UnityEngine.EventSystems;

public class ToNewWorldSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData data)
    {
        ShipDrag drag = data.pointerDrag != null ? data.pointerDrag.GetComponent<ShipDrag>() : null;
        if (drag == null || !drag.IsDragging || UIManager.instance == null || !UIManager.instance.CanSendShip(drag.Ship)) return;
        drag.CancelDrag(); EuropeManager.instance.AllowToGoToNewWorld(drag.Ship);
    }
}
