using UnityEngine;
using UnityEngine.EventSystems;

public class Town : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] protected Faction faction;
    public Faction Faction { get { return faction; } }
    [SerializeField] protected int visualRange;
    public int VisualRange { get { return visualRange; } }
    [SerializeField] private Vector2 curPos;
    public Vector2 CurPos { get { return curPos; } set { curPos = value; } }
    [SerializeField] protected Hex curHex;
    public Hex CurHex { get { return curHex; } set { curHex = value; } }
    [Header("Town")]
    [SerializeField] protected SpriteRenderer townSprite;
    public SpriteRenderer TownSprite { get { return townSprite; } }
    [Header("Border")]
    [SerializeField] protected SpriteRenderer borderSprite;
    [Header("Flag")]
    [SerializeField] protected SpriteRenderer flagSprite;
    [SerializeField] protected GameManager gameManager;
    [SerializeField] private int[] warehouse = new int[16];
    public int[] Warehouse { get { return warehouse; } set { warehouse = value; } }
    [SerializeField] private int[] totalYieldThisTurn = new int[16];
    public int[] TotalYieldThisTurn { get { return totalYieldThisTurn; } set { totalYieldThisTurn = value; } }
    [SerializeField] private int crossNum;
    public int CrossNum { get { return crossNum; } set { crossNum = value; } }
    [SerializeField] private int bellNum;
    public int BellNum { get { return bellNum; } set { bellNum = value; } }
    [SerializeField] private int lastAccumulatedTurn = -1;

    public void TownInit(GameManager gameMgr, Faction fact)
    {
        gameManager = gameMgr;
        faction = fact;
        flagSprite.sprite = fact.FlagIcon;
        townSprite.sprite = fact.TownIcon;
        warehouse = new int[16];
        totalYieldThisTurn = new int[16];
        crossNum = bellNum = 0;
        lastAccumulatedTurn = -1;
    }

    public void RecalculateYield()
    {
        if (totalYieldThisTurn == null || totalYieldThisTurn.Length != 16) totalYieldThisTurn = new int[16];
        System.Array.Clear(totalYieldThisTurn, 0, totalYieldThisTurn.Length);
        if (curHex == null || gameManager == null) return;
        curHex.YieldID = 0;
        totalYieldThisTurn[0] = TerrainYield.Calculate(curHex, 0);
        foreach (Hex hex in HexCalculator.GetHexAroundToArray(gameManager.AllHexes, curHex))
        {
            if (hex == null || hex.HasTown || hex.YieldID < 0 || hex.YieldID >= 8 || hex.Labor == null) continue;
            Unit worker = hex.Labor;
            if (worker.Faction == faction && worker.CurHex == curHex && worker.UnitStatus == UnitStatus.WorkInField)
                totalYieldThisTurn[hex.YieldID] += TerrainYield.Calculate(hex, hex.YieldID);
        }
    }

    public void AccumulateToWarehouse()
    {
        if (gameManager == null || lastAccumulatedTurn == gameManager.GameTurn) return;
        RecalculateYield();
        if (warehouse == null) warehouse = new int[16];
        else if (warehouse.Length != 16) System.Array.Resize(ref warehouse, 16);
        for (int i = 0; i < 16; i++) warehouse[i] += totalYieldThisTurn[i];
        lastAccumulatedTurn = gameManager.GameTurn;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || curHex == null || faction != gameManager.PlayerFaction) return;
        if (eventData.button == PointerEventData.InputButton.Left) gameManager.SetupCurrentTown(this);
        else if (eventData.button == PointerEventData.InputButton.Right)
            curHex.OnPointerClick(eventData);
    }
}
