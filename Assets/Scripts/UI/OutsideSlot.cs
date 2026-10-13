using UnityEngine;
using UnityEngine.EventSystems;

public class OutsideSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        UnitDrag drag = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<UnitDrag>() : null;
        if (drag == null || !drag.IsDragging || UIManager.instance == null || !UIManager.instance.CanAssign(drag)) return;
        drag.QuitOldTerrainSlot();
        drag.IconParent = transform;
        drag.LandUnit.UnitStatus = UnitStatus.None;
    }
}
