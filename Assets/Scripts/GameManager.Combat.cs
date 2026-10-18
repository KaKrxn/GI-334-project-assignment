using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager
{
    [SerializeField] private Unit winner;
    [SerializeField] private Unit loser;
    [SerializeField] private Faction curForeignFaction;
    public Faction CurForeignFaction { get { return curForeignFaction; } set { curForeignFaction = value; } }
    public bool CombatInProgress { get; private set; }
    public Unit Winner { get { return winner; } }
    public Unit Loser { get { return loser; } }
    public static bool AttackerWins(int attackerStrength, int defenderStrength)
    { return attackerStrength == defenderStrength ? Random.Range(0,2) == 0 : attackerStrength > defenderStrength; }
    public bool CanStartCombat(Unit attacker, Unit defender)
    {
        if (!playerTurn || MapInputBlocked || DialogManager.instance == null || !DialogManager.instance.HasWeek12Configuration ||
            !(attacker is NavalUnit attackingShip) || !(defender is NavalUnit defendingShip) || attacker.IsDead || defender.IsDead ||
            attacker != curUnit || attacker.Faction != playerFaction || defender.Faction == playerFaction || attacker.IsMoving || defender.IsMoving ||
            attacker.UnitStatus != UnitStatus.None || attackingShip.HasBoardingReservations || defendingShip.HasBoardingReservations ||
            attacker.CurHex == null || defender.CurHex == null || !CheckIfHexIsAdjacent(attacker.CurHex,defender.CurHex) || defender.CurHex.MoveCost > attacker.MovePoint) return false;
        return true;
    }
    public void StartCombat(Unit attacker, Unit defender)
    {
        if (!CanStartCombat(attacker,defender)) return;
        CombatInProgress = true; attacker.TargetUnit = defender; attacker.TargetHex = defender.CurHex;
        bool win = AttackerWins(attacker.Strength,defender.Strength);
        winner = win ? attacker : defender; loser = win ? defender : attacker;
        string winningName = winner.Faction.Nation + " " + winner.UnitName, losingName = loser.Faction.Nation + " " + loser.UnitName;
        DialogManager.instance.CombatAnalysisInit(attacker,defender);
        StartCoroutine(ResolveCombat(attacker,winningName,losingName));
    }
    private IEnumerator ResolveCombat(Unit attacker,string winningName,string losingName)
    {
        yield return new WaitForSeconds(.5f);
        DialogManager.instance.ToggleCombatPanel(true);
        yield return new WaitForSeconds(3.5f);
        DialogManager.instance.ToggleCombatPanel(false);
        SetupDeadUnit(loser);
        if (attacker != null) { attacker.TargetHex = null; attacker.TargetUnit = null; }
        CombatInProgress = false;
        DialogManager.instance.ShowCombatResult(winningName,losingName);
    }
    public void SetupDeadUnit(Unit deadUnit)
    {
        if (deadUnit == null || deadUnit.IsDead) return;
        if (deadUnit is NavalUnit ship)
        {
            foreach (LandUnit passenger in new List<LandUnit>(ship.Passengers))
            { passenger.TransportShip = null; RemoveDeadUnit(passenger); }
            ship.Passengers.Clear(); ship.CargoList.Clear();
        }
        RemoveDeadUnit(deadUnit);
    }
    private void RemoveDeadUnit(Unit unit)
    {
        if (unit == null || unit.IsDead) return;
        unit.MarkDead();
        if (unit.CurHex != null) unit.CurHex.UnitsInHex.Remove(unit);
        unit.CurHex = null; unit.Faction.Units.Remove(unit);
        if (curUnit == unit) curUnit = null;
        if (curAiUnit == unit) curAiUnit = null;
        Destroy(unit.gameObject);
    }
    public void CompleteCombatPresentation()
    { if (!CombatInProgress && curUnit == null) SelectPlayerFirstUnit(); }
    public void CheckUnmetFaction(Unit unit)
    {
        if (unit == null || unit.Faction != playerFaction || unit.CurHex == null || DialogManager.instance == null) return;
        Faction faction = HexCalculator.CheckNeverMetFaction(allHexes,unit);
        if (faction != null) DialogManager.instance.RequestForeignFaction(faction);
    }
    public void ShowFirstKingDialogText() { if (DialogManager.instance != null) DialogManager.instance.ShowFirstKingQuestDialogText(); }
    public bool CheckIfAroundHexHasTown(Hex hex) { return HexCalculator.CheckIfHexAroundHasTown(allHexes,hex); }
}
