using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class GameManager : MonoBehaviour
{
    [SerializeField]
    private Grid grid;

    [SerializeField]
    private Transform hexParent;

    [SerializeField]
    private GameObject hexPrefab;

    public const int WIDTH = 50; //no. of Column in this map
    public const int HEIGHT = 60; //no. of Row in this map

    public const int ARCTICNORTH = HEIGHT - 2;
    public const int ARCTICSOUTH = 2;
    public const int TUNDRANORTH = HEIGHT - 6;
    public const int TUNDRASOUTH = 6;
    public const int GRASSNORTH = HEIGHT - 12;
    public const int GRASSSOUTH = 12;
    public const int PRAIRIENORTH = HEIGHT - 20;
    public const int PRAIRIESOUTH = 20;
    public const int SAVANNANORTH = HEIGHT - 24;
    public const int SAVANNASOUTH = 24;
    public const int TROPICALNORTH = HEIGHT - 30;
    public const int TROPICALSOUTH = 30;

    [SerializeField]
    private Hex[,] allHexes = new Hex[WIDTH, HEIGHT];
    public Hex[,] AllHexes { get { return allHexes; } }

    [SerializeField]
    private HexData[] hexData;
    public HexData[] HexData { get { return hexData; } }

    [SerializeField]
    private bool showingText;

    [SerializeField]
    private int oceanEdgeIndex;

    [SerializeField]
    private Faction playerFaction;
    public Faction PlayerFaction { get { return playerFaction; } }

    [SerializeField]
    private Faction[] factions;
    public Faction[] Factions { get { return factions; } }

    [SerializeField]
    private FactionData[] factionData;
    public FactionData[] FactionData { get { return factionData; } }

    public static GameManager instance;

    [Header("Week 4 Units")]
    [SerializeField] private GameObject landUnitPrefab;
    [SerializeField] private GameObject navalUnitPrefab;
    [SerializeField] private GameObject townPrefab;
    [SerializeField] private LandUnitData[] landUnitData;
    public LandUnitData[] LandUnitData { get { return landUnitData; } }
    [SerializeField] private NavalUnitData[] navalUnitData;
    public NavalUnitData[] NavalUnitData { get { return navalUnitData; } }
    [Header("Week 7 Products")]
    [SerializeField] private ProductData[] productData;
    public ProductData[] ProductData { get { return productData; } }
    [SerializeField] private Unit curUnit;
    public Unit CurUnit { get { return curUnit; } set { curUnit = value; } }
    [SerializeField] private Unit curAiUnit;
    public Unit CurAiUnit { get { return curAiUnit; } set { curAiUnit = value; } }
    [Header("Week 5 Turns")]
    [SerializeField] private bool playerTurn = true;
    public bool PlayerTurn { get { return playerTurn; } set { playerTurn = value; } }
    [SerializeField] private int gameTurn = 1;
    public int GameTurn { get { return gameTurn; } set { gameTurn = value; } }
    [SerializeField] private int nativeTownNum;
    [SerializeField] private Town curTown;
    public Town CurTown { get { return curTown; } set { curTown = value; } }
    public bool TownPanelOpen { get { return UIManager.instance != null && UIManager.instance.IsTownPanelOpen; } }
    public bool MapInputBlocked { get { return CombatInProgress || TownPanelOpen || (UIManager.instance != null && UIManager.instance.InEurope) || (DialogManager.instance != null && DialogManager.instance.IsOpen); } }
    [SerializeField] private int year = 1600;
    public int Year { get { return year; } }

    void Awake()
    {
        instance = this;
    }


    private void GenerateAllHexes()
    {
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                Vector3 hexPos = grid.GetCellCenterWorld(new Vector3Int(x, y));
                //Debug.Log(hexPos);

                GameObject hexObj = Instantiate(hexPrefab, hexPos, Quaternion.identity, hexParent);
                Hex hex = hexObj.GetComponent<Hex>();

                int n = Random.Range(oceanEdgeIndex - 3, oceanEdgeIndex + 4);
                if (x >= n) hex.HexInit(x, y, hexPos, this, 0); //Ocean
                else
                {
                    GenerateAllBiomes(x, y, hex, hexPos);
                }

                //Debug.Log($"{x}:{y}");
                allHexes[x, y] = hex;
            }
        }
    }

    void Start()
    {
        SetUpFaction();
        SelectPlayerFaction();
        DetermineOcean();
        GenerateAllHexes();
        GenerateAllEuropeanShips();
        GenerateAllEuropeanExplorerUnits();
        GenerateAllNativeTowns();
        GenerateAllNativeUnits();
        if (EuropeManager.instance != null) EuropeManager.instance.Initialize();
        ShowFirstKingDialogText();
    }

    void Update()
    {
        if (CombatInProgress || (DialogManager.instance != null && DialogManager.instance.IsOpen)) return;
        if (Keyboard.current != null && playerTurn && Keyboard.current.eKey.wasPressedThisFrame) { EuropePanel(); return; }
        if (Keyboard.current != null && playerTurn && Keyboard.current.backspaceKey.wasPressedThisFrame)
        { Endturn(); return; }
        if (MapInputBlocked) return;
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            ToggleHexText();
        if (Keyboard.current != null && playerTurn && Keyboard.current.tabKey.wasPressedThisFrame)
            SelectNextPlayerUnit();
    }

    private void ToggleHexText()
    {
        foreach (Hex hex in allHexes)
            hex.ToggleAllBasicText(!showingText);

        showingText = !showingText;
    }
    private void DetermineOcean()
    {
        oceanEdgeIndex = WIDTH - Random.Range(7, 10);
        //Debug.Log($"min:{oceanEdgeIndex}");
    }

    private void GenerateBiome(int x, int y, Hex hex, Vector3 hexPos,
        int defaultTerrain, List<int> otherTerrain)
    {
        int n = Random.Range(1, 101);
        if (n <= 50)
            hex.HexInit(x, y, hexPos, this, defaultTerrain);
        else
        {
            int i = Random.Range(0, otherTerrain.Count);
            hex.HexInit(x, y, hexPos, this, otherTerrain[i]);
        }
    }

    private void GenerateAllBiomes(int x, int y, Hex hex, Vector3 hexPos)
    {
        int n = Random.Range(1, 101);

        if ((y >= 0 && y < ARCTICSOUTH) || (y >= ARCTICNORTH && y < HEIGHT))
            GenerateBiome(x, y, hex, hexPos, 8, new List<int> { 5 });
        else if ((y >= ARCTICSOUTH && y < TUNDRASOUTH) || (y >= TUNDRANORTH && y < ARCTICNORTH))
            GenerateBiome(x, y, hex, hexPos, 5, new List<int> { 1 });
        else if ((y >= TUNDRASOUTH && y < GRASSSOUTH) || (y >= GRASSNORTH && y < TUNDRANORTH))
            GenerateBiome(x, y, hex, hexPos, 1, new List<int> { 2, 5 });
        else if ((y >= GRASSSOUTH && y < PRAIRIESOUTH) || (y >= PRAIRIENORTH && y < GRASSNORTH))
            GenerateBiome(x, y, hex, hexPos, 2, new List<int> { 1, 3, 4 });
        else if ((y >= PRAIRIESOUTH && y < SAVANNASOUTH) || (y >= SAVANNANORTH && y < PRAIRIENORTH))
            GenerateBiome(x, y, hex, hexPos, 3, new List<int> { 1, 2, 4, 6 });
        else if ((y >= SAVANNASOUTH && y < TROPICALSOUTH) || (y >= TROPICALNORTH && y < SAVANNANORTH))
            GenerateBiome(x, y, hex, hexPos, 4, new List<int> { 1, 7 });

        if (n > 80)
        {
            hex.ClearForest();
            GenerateBiome(x, y, hex, hexPos, 9, new List<int> { 10 });
        }
    }

    public void SelectPlayerFaction()
    {
        int selected = Settings.playerNationId;
        if (selected < 0 || selected >= Settings.EuropeanNationCount) selected = 0;
        Settings.playerNationId = selected;
        playerFaction = factions[selected];
    }

    private void GenerateEuropeanShip(Faction fact)
    {
        GenerateEuropeanShip(fact, Random.Range(0, HEIGHT));
    }
    private void GenerateEuropeanShip(Faction fact, int y)
    {
        Hex hex = allHexes[WIDTH - 1, y];
        GameObject obj = Instantiate(navalUnitPrefab, hex.Pos, Quaternion.identity, fact.UnitParent);
        NavalUnit ship = obj.GetComponent<NavalUnit>();
        ship.UnitInit(this, fact, navalUnitData[0]);
        ship.SetupPosition(hex);
        fact.Units.Add(ship);
        if (fact == playerFaction)
        {
            ClearDarkFogAroundUnit(ship);
            SelectPlayerUnit(ship);
            CameraController.instance.MoveCamera(ship.transform.position);
            ship.Visible = true;
        }
        else ship.SetUnitDestination(FindNearestLandHexToLand(y));
    }

    private void GenerateAllEuropeanShips()
    {
        int interval = HEIGHT / 5, row = HEIGHT;
        List<int> spots = new List<int>();
        for (int i = 0; i < 5; i++) { row -= Random.Range(5, interval); spots.Add(row); }
        for (int i = 0; i < 5; i++) { int index = Random.Range(0, spots.Count); GenerateEuropeanShip(factions[i], spots[index]); spots.RemoveAt(index); }
    }

    private void GeneratePassengerUnit(Faction fact, Hex hex, int unitId, bool show, NavalUnit ship)
    {
        GameObject obj = Instantiate(landUnitPrefab, hex.Pos, Quaternion.identity, ship.PassengerParent.transform);
        LandUnit unit = obj.GetComponent<LandUnit>();
        unit.UnitInit(this, fact, landUnitData[unitId]);
        unit.SetupPosition(hex);
        unit.UnitStatus = UnitStatus.OnBoard;
        unit.TransportShip = ship;
        obj.SetActive(show);
        fact.Units.Add(unit);
        ship.Passengers.Add(unit);
    }

    private void GenerateAllEuropeanExplorerUnits()
    {
        for (int i = 0; i < 5; i++)
        {
            NavalUnit ship = (NavalUnit)factions[i].Units[0];
            GeneratePassengerUnit(factions[i], ship.CurHex, 1, false, ship);
            GeneratePassengerUnit(factions[i], ship.CurHex, 2, false, ship);
        }
    }

    public void ShowToggleBorder(Unit unit)
    {
        unit.ToggleBorder(true, unit.Faction == playerFaction ? Color.green : Color.red);
    }

    public void ClearToggleBorder(Unit unit) { unit.ToggleBorder(false, Color.green); }
    public void FocusPlayerUnit(Unit unit) { ShowToggleBorder(unit); }

    public void SelectPlayerUnit(Unit unit)
    {
        if (!playerTurn || MapInputBlocked || unit == null || unit.CurHex == null || unit.Faction != playerFaction ||
            unit.UnitStatus == UnitStatus.WorkInField || unit.UnitStatus == UnitStatus.WorkInTown) return;
        if (curUnit != null)
        {
            ClearToggleBorder(curUnit);
            curUnit.SetUnitToNormalLayerOrder();
            if (curUnit.UnitStatus == UnitStatus.OnBoard) curUnit.gameObject.SetActive(false);
        }
        unit.gameObject.SetActive(true);
        unit.SetUnitToFrontLayerOrder();
        curUnit = unit;
        HideOtherLandUnits(unit);
        CameraController.instance.MoveCamera(unit.CurPos);
        FocusPlayerUnit(unit);
    }

    public void HideOtherLandUnits(Unit selected)
    {
        if (selected == null || selected.Faction != playerFaction || selected.CurHex == null) return;
        foreach (Unit other in selected.CurHex.UnitsInHex)
            if (other != selected && other.UnitType == UnitType.Land && other.Faction == playerFaction && !other.IsMoving)
                other.gameObject.SetActive(false);
    }

    public bool CheckIfHexIsAdjacent(Hex center, Hex target)
    {
        return center != null && target != null && HexCalculator.GetHexAround(allHexes, center).Contains(target);
    }

    public void ClearDarkFogAroundUnit(Unit unit)
    {
        if (unit.CurHex == null) return;
        unit.CurHex.DiscoverHex();
        foreach (Hex hex in HexCalculator.GetHexAround(allHexes, unit.CurHex)) hex.DiscoverHex();
    }

    public void LeaveSeenFogAroundUnit(Unit unit)
    {
        if (unit.CurHex == null) return;
        unit.CurHex.SeenHex();
        foreach (Hex hex in HexCalculator.GetHexAround(allHexes, unit.CurHex)) hex.SeenHex();
    }

    public void ClearDarkFogAroundEveryUnit(Faction fact)
    {
        foreach (Unit unit in fact.Units) ClearDarkFogAroundUnit(unit);
    }

    private int FindIndexOfCurUnit() { return playerFaction.Units.IndexOf(curUnit); }

    private void SelectNextPlayerUnit()
    {
        if (playerFaction == null || playerFaction.Units.Count == 0) return;
        int current = FindIndexOfCurUnit();
        for (int step = 1; step <= playerFaction.Units.Count; step++)
        {
            Unit next = playerFaction.Units[(current + step) % playerFaction.Units.Count];
            if (next.CurHex == null || next.UnitStatus == UnitStatus.WorkInField || next.UnitStatus == UnitStatus.WorkInTown) continue;
            SelectPlayerUnit(next);
            CameraController.instance.MoveCamera(next.transform.position);
            break;
        }
    }

    private void SetUpFaction()
    {
        for (int i = 0; i < factions.Length; i++)
            factions[i].FactionInit(factionData[i]);
    }

    public void GenerateTown(Faction fact, Hex hex)
    {
        if (hex == null || hex.HasTown || hex.HexType == HexType.Ocean || hex.HexType == HexType.Mountains) return;
        GameObject obj = Instantiate(townPrefab, hex.Pos, Quaternion.identity, fact.TownParent);
        Town town = obj.GetComponent<Town>();
        town.TownInit(this, fact);
        town.CurHex = hex;
        town.CurPos = hex.Pos;
        fact.Towns.Add(town);
        hex.HasTown = true;
        hex.Town = town;
    }

    private void GenerateAllNativeTowns()
    {
        // Bounded candidate sampling reaches 5-9 towns without retrying forever
        // or placing two towns on the same/adjacent tile across different nations.
        List<Hex> candidates = new List<Hex>();
        foreach (Hex hex in allHexes)
            if (hex.X < oceanEdgeIndex - 1 && hex.HexType != HexType.Ocean && hex.HexType != HexType.Mountains)
                candidates.Add(hex);
        for (int i = 5; i < factions.Length; i++)
        {
            nativeTownNum = Random.Range(5, 10);
            int created = 0;
            while (created < nativeTownNum && candidates.Count > 0)
            {
                int n = Random.Range(0, candidates.Count);
                Hex hex = candidates[n];
                candidates[n] = candidates[candidates.Count - 1];
                candidates.RemoveAt(candidates.Count - 1);
                if (hex.HasTown || HexCalculator.CheckIfHexAroundHasTown(allHexes, hex)) continue;
                GenerateTown(factions[i], hex);
                created++;
            }
        }
    }

    private void GenerateLandUnit(Faction fact, Hex hex, int unitId, bool show)
    {
        GameObject obj = Instantiate(landUnitPrefab, hex.Pos, Quaternion.identity, fact.UnitParent);
        LandUnit unit = obj.GetComponent<LandUnit>();
        unit.UnitInit(this, fact, landUnitData[unitId]);
        unit.SetupPosition(hex);
        unit.ShowHideSprite(show);
        fact.Units.Add(unit);
    }

    private void GenerateAllNativeUnits()
    {
        for (int i = 5; i < factions.Length; i++)
            foreach (Town town in factions[i].Towns)
                GenerateLandUnit(factions[i], town.CurHex, factions[i].IsTropicalNatives ? 4 : 3, false);
    }

    public void CheckUnitClearingLand(Faction fact, Unit unit)
    {
        if (unit.UnitStatus != UnitStatus.Clearing) return;
        unit.CurHex.ClearForest();
        unit.UnitStatus = UnitStatus.None;
    }

    public void CheckUnitBuildingSettlement(Faction fact, Unit unit)
    {
        if (unit.UnitStatus != UnitStatus.Building) return;
        GenerateTown(fact, unit.CurHex);
        unit.UnitStatus = UnitStatus.None;
    }

    public void ResetAllUnits(Faction fact)
    {
        foreach (Unit unit in fact.Units)
        {
            CheckUnitClearingLand(fact, unit);
            CheckUnitBuildingSettlement(fact, unit);
            unit.MovePoint = unit.MovePointMax;
        }
    }

    public void SelectAiUnit(Unit unit)
    {
        if (curAiUnit != null) ClearToggleBorder(curAiUnit);
        if (curUnit != null) ClearToggleBorder(curUnit);
        curAiUnit = unit;
        FocusPlayerUnit(unit);
    }

    public void SelectPlayerFirstUnit()
    {
        if (playerFaction.Units.Count == 0) return;
        foreach (Unit first in playerFaction.Units)
        {
            if (first.CurHex == null || first.UnitStatus == UnitStatus.WorkInField || first.UnitStatus == UnitStatus.WorkInTown) continue;
            SelectPlayerUnit(first);
            CameraController.instance.MoveCamera(first.CurPos);
            break;
        }
    }

    public void Endturn()
    {
        if (!playerTurn || CombatInProgress || AIManager.instance == null || (DialogManager.instance != null && DialogManager.instance.IsOpen)) return;
        foreach (Faction fact in factions)
            foreach (Unit unit in fact.Units) if (unit.IsMoving) return;
        if (UIManager.instance != null) UIManager.instance.CheckActiveUIPanel();
        if (curUnit != null) ClearToggleBorder(curUnit);
        if (EuropeManager.instance != null) EuropeManager.instance.UpdateShipInTransitTurn();
        playerTurn = false;
        AIManager.instance.StartAITurn();
    }

    public void AccumulateStockAllTowns()
    {
        foreach (Faction fact in factions)
            foreach (Town town in fact.Towns) town.AccumulateToWarehouse();
    }

    public void StartNewTurn()
    {
        if (playerTurn || (AIManager.instance != null && AIManager.instance.IsRunning)) return;
        AccumulateStockAllTowns();
        playerTurn = true;
        SelectPlayerFirstUnit();
    }

    public NavalUnit CheckIfHexHasOurShipToBoard(Hex hex)
    {
        if (hex == null) return null;
        foreach (Unit unit in hex.UnitsInHex)
            if (unit is NavalUnit ship && ship.Faction == playerFaction && !ship.IsMoving && ship.HasBoardingSpace)
                return ship;
        return null;
    }

    public void SetupCurrentTown(Town town)
    {
        if (!playerTurn || MapInputBlocked || town == null || town.Faction != playerFaction || UIManager.instance == null) return;
        foreach (Unit unit in playerFaction.Units) if (unit.IsMoving) return;
        curTown = town;
        UIManager.instance.SetupCurrentTown(town.CurHex, HexCalculator.GetHexAroundToArray(allHexes, town.CurHex));
        UIManager.instance.ToggleTownPanel(true);
    }

    public void EuropePanel() { if (UIManager.instance != null) UIManager.instance.CheckEuropePanel(); }
    public void CheckShipToEurope()
    {
        if (EuropeManager.instance == null) return;
        foreach (Unit unit in playerFaction.Units) if (unit is NavalUnit ship && ship.CurHex != null && ship.CurHex.X == WIDTH - 1) EuropeManager.instance.QuestionGoToEurope(ship);
    }
    public void CheckToGenerateShipFromEurope(NavalUnit ship)
    {
        if (ship == null || ship.Faction != playerFaction) return;
        // Returning ships keep their identity, cargo and original passenger objects.
        Hex hex = allHexes[WIDTH - 2, Random.Range(0, HEIGHT)];
        ship.SetupPosition(hex); ship.UnitStatus = UnitStatus.None; ship.MovePoint = ship.MovePointMax; ship.Visible = true; ship.ShowHideSprite(true);
        foreach (LandUnit passenger in ship.Passengers) { passenger.SetupPosition(hex); passenger.gameObject.SetActive(false); }
        ship.gameObject.SetActive(true); ClearDarkFogAroundUnit(ship); SelectPlayerUnit(ship);
    }

}
