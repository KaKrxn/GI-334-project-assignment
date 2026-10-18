using UnityEngine;
using UnityEngine.EventSystems;

public class CargoSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private NavalUnit ship;
    [SerializeField] private int holdId;
    [SerializeField] private CargoDrag cargoDrag;
    [SerializeField] private bool inEurope;
    public CargoDrag CargoDrag { get { return cargoDrag; } set { cargoDrag = value; } }
    public bool InEurope { get { return inEurope; } }
    public NavalUnit Ship { get { return ship; } }
    public int HoldId { get { return holdId; } }
    public void SlotInit(NavalUnit owner, int index, bool europe = false) { ship = owner; holdId = index; inEurope = europe; }
    public void OnDrop(PointerEventData eventData)
    {
        StockDrag drag = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<StockDrag>() : null;
        UIManager ui = UIManager.instance;
        if (!isActiveAndEnabled || drag == null || !drag.IsDragging || ui == null || !ui.CanLoadStock(drag.StockSlot) ||
            ship == null || ship.IsMoving || drag.StockSlot.InEurope != inEurope || !ui.IsSelectedCargoShip(ship, inEurope) ||
            ship.Faction != GameManager.instance.PlayerFaction || (!inEurope && ship.CurHex != drag.StockSlot.Town.CurHex)) return;
        if (inEurope) drag.Cargo.Quantity -= EuropeManager.instance.BuyCargo(ship, holdId, drag.Cargo);
        else drag.Cargo.Quantity = ship.AddCargo(holdId, drag.Cargo);
        ui.UpdateCargoSlots(ship);
    }
    public void Clear()
    {
        if (cargoDrag != null) { cargoDrag.CancelDrag(); cargoDrag.gameObject.SetActive(false); Destroy(cargoDrag.gameObject); cargoDrag = null; }
        ship = null; holdId = -1;
    }
}
