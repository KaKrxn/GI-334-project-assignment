using UnityEngine;
using UnityEngine.EventSystems;

public class ShipInPort : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private UnityEngine.UI.Image unitImage;
    [SerializeField] private NavalUnit navalUnit;
    [SerializeField] private TMPro.TMP_Text turnText;
    [SerializeField] private ShipDrag shipDrag;
    public ShipDrag ShipDrag { get { return shipDrag; } }
    public NavalUnit NavalUnit { get { return navalUnit; } }
    public void UnitInit(NavalUnit ship, bool draggable = false)
    {
        navalUnit = ship; unitImage.sprite = ship.UnitSprite.sprite;
        if (turnText != null) turnText.gameObject.SetActive(false);
        if (draggable && UIManager.instance.ShipDragPrefab != null)
        {
            GameObject obj = Instantiate(UIManager.instance.ShipDragPrefab, transform); shipDrag = obj.GetComponent<ShipDrag>(); shipDrag.Init(ship);
        }
    }
    public void UpdateTurnText(int turn)
    { if (turnText != null) { turnText.gameObject.SetActive(true); turnText.text = turn + "\nTurn(s)"; turnText.color = Color.yellow; } }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && UIManager.instance != null)
            UIManager.instance.SelectShipInPort(this);
    }
    public void SetSelected(bool selected) { unitImage.color = selected ? Color.white : new Color(.65f,.65f,.65f); if (shipDrag != null) shipDrag.SetSelected(selected); }
}
