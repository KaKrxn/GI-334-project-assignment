using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum UnitType { Land, Naval }
public enum UnitStatus { None, OnBoard, Fortified, Clearing, Building, WorkInField, WorkInTown }

public class Unit : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] protected string unitName;
    public string UnitName { get { return unitName; } }
    [SerializeField] protected UnitType unitType;
    public UnitType UnitType { get { return unitType; } }
    [SerializeField] protected UnitStatus unitStatus;
    public UnitStatus UnitStatus { get { return unitStatus; } set { unitStatus = value; } }
    [SerializeField] protected int strength;
    public int Strength { get { return strength; } set { strength = value; } }
    [SerializeField] protected int movePoint;
    public int MovePoint { get { return movePoint; } set { movePoint = value; } }
    [SerializeField] protected int movePointMax;
    public int MovePointMax { get { return movePointMax; } }
    [SerializeField] protected Faction faction;
    public Faction Faction { get { return faction; } }
    [SerializeField] protected int visualRange;
    public int VisualRange { get { return visualRange; } }
    [SerializeField] private Vector2 curPos;
    public Vector2 CurPos { get { return curPos; } set { curPos = value; } }
    [SerializeField] protected Hex curHex;
    public Hex CurHex { get { return curHex; } set { curHex = value; } }
    [SerializeField] protected Hex targetHex;
    public Hex TargetHex { get { return targetHex; } set { targetHex = value; } }
    [SerializeField] protected bool isMoving = false;
    public bool IsMoving { get { return isMoving; } set { isMoving = value; } }
    [SerializeField] protected bool visible = false;
    public bool Visible { get { return visible; } set { visible = value; } }
    [Header("Unit")]
    [SerializeField] protected SpriteRenderer unitSprite;
    public SpriteRenderer UnitSprite { get { return unitSprite; } }
    [Header("Flag")]
    [SerializeField] protected SpriteRenderer flagSprite;
    [Header("Border")]
    [SerializeField] protected SpriteRenderer borderSprite;
    [SerializeField] protected GameManager gameMgr;

    public void SetupPosition(Hex hex)
    {
        if (curHex != null) curHex.UnitsInHex.Remove(this);
        curHex = hex;
        curPos = hex.Pos;
        transform.position = hex.Pos;
        if (!hex.UnitsInHex.Contains(this)) hex.UnitsInHex.Add(this);
    }

    public void ToggleBorder(bool show, Color32 color)
    {
        borderSprite.gameObject.SetActive(show);
        borderSprite.color = color;
        // The project has no WhiteRing texture. Use the same hex outline as geometry.
        LineRenderer outline = borderSprite.GetComponent<LineRenderer>();
        if (outline != null) outline.startColor = outline.endColor = color;
    }

    public void ShowHideSprite(bool show)
    {
        unitSprite.gameObject.SetActive(show);
        flagSprite.gameObject.SetActive(show);
    }

    public void SetUnitToFrontLayerOrder()
    {
        unitSprite.sortingOrder = 5;
        flagSprite.sortingOrder = 6;
        GetComponent<SpriteRenderer>().sortingOrder = 5;
    }

    public void SetUnitToNormalLayerOrder()
    {
        unitSprite.sortingOrder = 2;
        flagSprite.sortingOrder = 3;
        GetComponent<SpriteRenderer>().sortingOrder = 2;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && faction == gameMgr.PlayerFaction)
            gameMgr.SelectPlayerUnit(this);
        else if (eventData.button == PointerEventData.InputButton.Right && curHex != null &&
            gameMgr.PlayerTurn && !gameMgr.TownPanelOpen && gameMgr.CurUnit != null &&
            gameMgr.CheckIfHexIsAdjacent(gameMgr.CurUnit.CurHex, curHex))
        {
            if (faction == gameMgr.PlayerFaction) gameMgr.CurUnit.PrepareMoveToHex(curHex);
            else Debug.Log($"{gameMgr.CurUnit.UnitName} Attacks {unitName}");
        }
    }

    public virtual void PrepareMoveToHex(Hex target)
    {
        if (!CanBeginMove(target)) return;
        targetHex = target;
        isMoving = true;
        if (faction == gameMgr.PlayerFaction) gameMgr.LeaveSeenFogAroundUnit(this);
    }

    protected bool CanBeginMove(Hex target, bool allowOnBoard = false)
    {
        if (gameMgr == null || gameMgr.TownPanelOpen || target == null || isMoving ||
            (unitStatus != UnitStatus.None && !(allowOnBoard && unitStatus == UnitStatus.OnBoard)) ||
            !gameMgr.CheckIfHexIsAdjacent(curHex, target) || target.MoveCost > movePoint)
            return false;
        return faction == gameMgr.PlayerFaction ? gameMgr.PlayerTurn :
            !gameMgr.PlayerTurn && gameMgr.CurAiUnit == this;
    }

    protected virtual void StayOnHex(Hex target)
    {
        isMoving = false;
        SetupPosition(target);
        targetHex = null; // Clear the field, not the parameter in the manual's snippet.
        if (faction == gameMgr.PlayerFaction)
        {
            gameMgr.ClearDarkFogAroundEveryUnit(faction);
            ToggleBorder(gameMgr.CurUnit == this, Color.green);
        }
    }

    protected virtual void Update()
    {
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame &&
            gameMgr != null && gameMgr.PlayerTurn && !gameMgr.TownPanelOpen && this == gameMgr.CurUnit &&
            !isMoving && curHex != null && curHex.Town != null && curHex.Town.Faction == gameMgr.PlayerFaction)
            gameMgr.SetupCurrentTown(curHex.Town);
        if (!isMoving) return;
        ToggleBorder(false, Color.green);
        transform.position = Vector2.MoveTowards(curPos, targetHex.Pos, 4f * Time.deltaTime);
        curPos = transform.position;
        if (curPos == targetHex.Pos)
        {
            movePoint -= targetHex.MoveCost;
            StayOnHex(targetHex);
        }
    }

    protected virtual void OnDestroy()
    {
        if (curHex != null) curHex.UnitsInHex.Remove(this);
    }
}
