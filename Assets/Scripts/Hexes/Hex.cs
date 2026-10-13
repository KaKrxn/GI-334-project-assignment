using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum HexType
{
    Ocean,
    Grassland,
    Prairie,
    Savanna,
    Plain,
    Tundra,
    Desert,
    Swamp,
    Arctic,
    Hills,
    Mountains
}
public class Hex : MonoBehaviour, IPointerClickHandler
{

    [SerializeField]
    private int x;
    public int X { get { return x; } set { x = value; } }

    [SerializeField]
    private int y;
    public int Y { get { return y; } set { y = value; } }

    [SerializeField]
    private Vector2 pos;
    public Vector2 Pos { get { return pos; } set { pos = value; } }

    [SerializeField]
    private HexType hexType = HexType.Plain;
    public HexType HexTypes { get { return hexType; } }
    public HexType HexType { get { return hexType; } }


    [Header("Basic")]
    [SerializeField]
    private SpriteRenderer terrainSprite;
    public SpriteRenderer TerrainSprite { get { return terrainSprite; } }

    [SerializeField]
    private SpriteRenderer forestSprite;
    public SpriteRenderer ForestSprite { get { return forestSprite; } }

    [Header("Fog of War")]
    [SerializeField]
    private SpriteRenderer fogSprite;

    [SerializeField]
    private SpriteRenderer darkSprite;

    [Header("Town")]
    [SerializeField]
    private bool hasTown;
    public bool HasTown { get { return hasTown; } set { hasTown = value; } }
    [SerializeField] private Town town;
    public Town Town { get { return town; } set { town = value; } }
    [SerializeField] private List<Unit> unitsInHex = new List<Unit>();
    public List<Unit> UnitsInHex { get { return unitsInHex; } }
    [SerializeField] private Unit labor;
    public Unit Labor { get { return labor; } set { labor = value; } }
    [SerializeField] private int yieldId = -1;
    public int YieldID { get { return yieldId; } set { yieldId = value; } }

    [Header("River")]
    private bool hasRiver;
    public bool HasRiver { get { return hasRiver; } set { hasRiver = value; } }

    [Header("Forest")]
    private bool hasForest;
    public bool HasForest { get { return hasForest; } set { hasForest = value; } }

    [SerializeField]
    private int moveCost = 1;
    public int MoveCost { get { return moveCost; } }

    [Header("Terrain")]
    [SerializeField]
    private Sprite[] terrainSprites;

    [Header("Forest")]
    [SerializeField]
    private Sprite[] forestSprites;

    [SerializeField]
    private string hexName;
    public string HexName { get { return hexName; } set { hexName = value; } }

    [SerializeField]
    private int[] resourceYield;
    public int[] ResourceYield { get { return resourceYield; } set { resourceYield = value; } }

    [Header("Special")]
    [SerializeField]
    private bool specialHex;
    public bool SpecialHex { get { return specialHex; } set { specialHex = value; } }

    [SerializeField]
    private TMP_Text hexText;

    private GameManager gameMgr;

    [SerializeField] protected bool visible;
    public bool Visible { get { return visible; } set { visible = value; } }

    private void ToggleFog(bool show) { fogSprite.gameObject.SetActive(show); }
    private void ToggleDark(bool show) { darkSprite.gameObject.SetActive(show); }

    public void DiscoverHex()
    {
        ToggleFog(false);
        ToggleDark(false);
        visible = true;
    }

    public void SeenHex()
    {
        ToggleFog(true);
        ToggleDark(false);
        visible = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || gameMgr == null) return;
        Unit unit = gameMgr.CurUnit;
        if (unit != null && unit.Faction == gameMgr.PlayerFaction &&
            gameMgr.CheckIfHexIsAdjacent(unit.CurHex, this))
            unit.PrepareMoveToHex(this);
    }


    private void RandomTerrainSprite(Sprite[] sprites)
    {
        int i = Random.Range(0, sprites.Length);
        terrainSprite.sprite = sprites[i];
    }
    private void RandomForestSprite(Sprite[] sprites)
    {
        int i = Random.Range(0, sprites.Length);
        forestSprite.gameObject.SetActive(true);
        forestSprite.sprite = sprites[i];
    }

    public void HexInit(int x, int y, Vector2 pos, GameManager gameMgr, int i)
    {
        this.x = x;
        this.y = y;
        this.pos = pos;
        this.gameMgr = gameMgr;
        hexText.text = $"{x},{y}";

        // Biome generation can initialize a tile again as Hills/Mountains.
        ClearForest();

        hexName = gameMgr.HexData[i].hexName;
        hexType = gameMgr.HexData[i].type;
        terrainSprites = gameMgr.HexData[i].terrainSprites;
        forestSprites = gameMgr.HexData[i].forestSprites;
        resourceYield = gameMgr.HexData[i].resourceYield;
        moveCost = gameMgr.HexData[i].moveCost;
        RandomTerrainSprite(terrainSprites);

        // 85% forest, as in the production manual.
        int n = Random.Range(1, 101);
        if (n <= 85)
        {
            if (forestSprites.Length > 0)
            {
                RandomForestSprite(forestSprites);
                hasForest = true;
                moveCost += 1;
            }
        }
        SetSortingOrder();
    }

    public void ClearForest()
    {
        if (hasForest)
            moveCost -= 1;
        hasForest = false;
        forestSprite.gameObject.SetActive(false);
    }

    private void SetSortingOrder()
    {
        int baseOrder = y * -10;
        terrainSprite.sortingOrder = baseOrder;
        forestSprite.sortingOrder = baseOrder + 1;
        fogSprite.sortingOrder = baseOrder + 5;
        darkSprite.sortingOrder = baseOrder + 6;

        switch (hexType)
        {
            case HexType.Arctic:
            case HexType.Hills:
            case HexType.Mountains:
                terrainSprite.sortingOrder = baseOrder + 1;
                break;
        }
    }

    public void ToggleAllBasicText(bool flag)
    {
        hexText.gameObject.SetActive(flag);
    }

}
