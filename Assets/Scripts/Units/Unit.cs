using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum UnitType { Land, Naval }
public enum UnitStatus { None, OnBoard, Fortified, Clearing, Building, WorkInField, WorkInTown, Hidden, ToBoard }

public class Unit : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] protected string unitName;
    public string UnitName { get { return unitName; } }
    [SerializeField] protected UnitType unitType;
    public UnitType UnitType { get { return unitType; } }
    [SerializeField] protected UnitStatus unitStatus;
    public UnitStatus UnitStatus { get { return unitStatus; } set { unitStatus = value; ChangeStatusIcon(); } }
    [SerializeField] protected SpriteRenderer statusSprite;
    [SerializeField] private Sprite[] statusSpritesList;
    public SpriteRenderer StatusSprite { get { return statusSprite; } }
    public Sprite[] StatusSpritesList { get { return statusSpritesList; } }
    [SerializeField] protected Hex destinationHex;
    public Hex DestinationHex { get { return destinationHex; } set { destinationHex = value; } }
    public void SetUnitDestination(Hex hex) { destinationHex = hex; }
    public void CheckMoveToDestination()
    {
        if (gameMgr == null || isMoving || IsDead || curHex == null || destinationHex == null) return;
        Hex next = HexCalculator.FindNextHexToGo(this, curHex, destinationHex, gameMgr.AllHexes);
        if (next != null) PrepareMoveToHex(next);
    }
    public void ChangeStatusIcon()
    {
        if (statusSprite == null) return;
        int i = unitStatus == UnitStatus.Clearing ? 0 : unitStatus == UnitStatus.Building ? 1 : -1;
        bool show = i >= 0 && statusSpritesList != null && i < statusSpritesList.Length && statusSpritesList[i] != null;
        statusSprite.sprite = show ? statusSpritesList[i] : null; statusSprite.gameObject.SetActive(show);
    }
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
    [SerializeField] protected Unit targetUnit;
    public Unit TargetUnit { get { return targetUnit; } set { targetUnit = value; } }
    public bool IsDead { get; private set; }
    public void MarkDead() { IsDead = true; isMoving = false; targetHex = null; targetUnit = null; gameObject.SetActive(false); }
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
            gameMgr.PlayerTurn && !gameMgr.MapInputBlocked && gameMgr.CurUnit != null &&
            gameMgr.CheckIfHexIsAdjacent(gameMgr.CurUnit.CurHex, curHex))
        {
            if (faction == gameMgr.PlayerFaction) gameMgr.CurUnit.PrepareMoveToHex(curHex);
            else if (gameMgr.CurUnit is NavalUnit && this is NavalUnit) gameMgr.CurUnit.AttackUnit(curHex,this);
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
    public void AttackUnit(Hex target, Unit defender)
    { if (target != null && defender != null && defender.CurHex == target) gameMgr.StartCombat(this,defender); }

    protected bool CanBeginMove(Hex target, bool allowOnBoard = false)
    {
        if (gameMgr == null || IsDead || (faction == gameMgr.PlayerFaction && gameMgr.MapInputBlocked) || target == null || isMoving ||
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
            Unit selected = gameMgr.CurUnit;
            gameMgr.HideOtherLandUnits(selected != null && selected.CurHex == curHex ? selected : this);
        }
    }

    protected virtual void Update()
    {
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame &&
            gameMgr != null && gameMgr.PlayerTurn && !gameMgr.MapInputBlocked && this == gameMgr.CurUnit &&
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
