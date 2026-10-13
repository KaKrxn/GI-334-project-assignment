using System.Collections;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager instance;
    public bool IsRunning { get; private set; }
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    private void Awake() { instance = this; }

    private void AutoMoveToHex(Unit unit)
    {
        Hex target = HexCalculator.FindHexByDir(unit.CurHex, (HexDirection)Random.Range(0, 6), GameManager.instance.AllHexes);
        if (target == null) return;
        unit.ShowHideSprite(true);
        unit.PrepareMoveToHex(target);
    }

    public void StartAITurn()
    {
        if (IsRunning || GameManager.instance.PlayerTurn) return;
        IsRunning = true;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(EnemyUnitMoves());
    }

    private IEnumerator EnemyUnitMoves()
    {
        GameManager manager = GameManager.instance;
        foreach (Faction fact in manager.Factions)
        {
            manager.ResetAllUnits(fact);
            if (fact == manager.PlayerFaction) continue;
            foreach (Unit unit in fact.Units)
            {
                if (unit.UnitStatus == UnitStatus.OnBoard) continue;
                manager.SelectAiUnit(unit);
                if (unit.Visible) CameraController.instance.MoveCamera(unit.CurPos);
                AutoMoveToHex(unit);
                yield return new WaitForSeconds(0.1f);
                manager.ClearToggleBorder(unit);
            }
        }
        // Keep the manual's 0.1-second stagger, but finish all animations before
        // handing control back to the player.
        foreach (Faction fact in manager.Factions)
            foreach (Unit unit in fact.Units)
                while (unit.IsMoving) yield return null;
        manager.CurAiUnit = null;
        manager.GameTurn++;
        RestoreCursor();
        IsRunning = false;
        manager.PlayerTurn = true;
        manager.SelectPlayerFirstUnit();
    }

    private void RestoreCursor()
    {
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void OnDisable()
    {
        if (IsRunning) { StopAllCoroutines(); RestoreCursor(); IsRunning = false; }
        if (instance == this) instance = null;
    }
}
