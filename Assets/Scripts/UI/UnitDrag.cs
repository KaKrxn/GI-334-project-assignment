using UnityEngine;
using UnityEngine.EventSystems;

public class UnitDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private UnityEngine.UI.Image unitImage;
    [SerializeField] private LandUnit landUnit;
    public LandUnit LandUnit { get { return landUnit; } }
    [SerializeField] private Transform iconParent;
    public Transform IconParent { get { return iconParent; } set { iconParent = value; } }
    [SerializeField] private TerrainSlot terrainSlot;
    public TerrainSlot TerrainSlot { get { return terrainSlot; } }
    public bool IsDragging { get; private set; }

    public void UnitInit(LandUnit unit)
    {
        landUnit = unit;
        unitImage.sprite = unit.UnitSprite.sprite;
        iconParent = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (UIManager.instance == null || !UIManager.instance.CanAssign(this)) return;
        iconParent = transform.parent;
        IsDragging = true;
        transform.SetParent(UIManager.instance.transform, true);
        transform.SetAsLastSibling();
        unitImage.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsDragging) transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData) { CancelDrag(); }
    public void CancelDrag()
    {
        if (!IsDragging) return;
        IsDragging = false;
        transform.SetParent(iconParent, false);
        ((RectTransform)transform).anchoredPosition = Vector2.zero;
        unitImage.raycastTarget = true;
    }

    public void QuitOldTerrainSlot()
    {
        if (terrainSlot != null && terrainSlot.Hex != null && terrainSlot.Hex.Labor == landUnit)
        {
            terrainSlot.Hex.Labor = null;
            terrainSlot.Hex.YieldID = -1;
        }
        terrainSlot = null;
        if (UIManager.instance != null) UIManager.instance.RefreshTownYield();
    }
    public void WorkAtNewTerrainSlot(TerrainSlot slot) { terrainSlot = slot; }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && terrainSlot != null && UIManager.instance != null && UIManager.instance.CanAssign(this))
            UIManager.instance.SelectProfession(terrainSlot);
    }
}
