using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogManager : MonoBehaviour
{
    public static DialogManager instance;
    [SerializeField] private Dialog navalDialog;
    [SerializeField] private DialogData toEuropeData;
    [SerializeField] private GameObject modalBlock;
    // Manual order: Naval, AI, Army, Foreign, Scout, King, Colony, Trade, Religious, Combat.
    [SerializeField] private Dialog[] dialogs;
    private readonly Queue<Action> pending = new Queue<Action>();
    private readonly HashSet<Faction> queuedFactions = new HashSet<Faction>();
    private Dialog activeDialog;
    private bool europeQuestion;
    public bool HasWeek12Configuration { get { return dialogs != null && dialogs.Length == 10 && dialogs[9] != null; } }
    public bool IsOpen { get { return activeDialog != null && activeDialog.gameObject.activeSelf; } }
    public bool IsAIMoveOpen { get { return HasWeek12Configuration && activeDialog == dialogs[1] && IsOpen; } }
    public bool IsEuropeQuestion { get { return europeQuestion && activeDialog == navalDialog && IsOpen; } }
    public Dialog ActiveDialog { get { return activeDialog; } }
    public Dialog NavalDialog { get { return navalDialog; } }
    public Dialog[] Dialogs { get { return dialogs; } }
    private void Awake() { instance = this; }
    private void OnDestroy() { if (instance == this) instance = null; }
    private void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsEuropeQuestion) NoGoToEurope();
            else if (activeDialog.OkayButton != null && activeDialog.OkayButton.gameObject.activeSelf && !IsAIMoveOpen) AcknowledgeActive();
        }
        Pump();
    }
    private void Pump()
    { if (!IsOpen && pending.Count > 0 && GameManager.instance.PlayerTurn && !GameManager.instance.CombatInProgress) pending.Dequeue()(); }
    private void Queue(Action action) { pending.Enqueue(action); Pump(); }
    private void Show(Dialog dialog)
    {
        UIManager.instance.CheckActiveUIPanel(); activeDialog = dialog;
        modalBlock.SetActive(true); modalBlock.transform.SetAsLastSibling(); dialog.transform.parent.SetAsLastSibling();
        dialog.gameObject.SetActive(true); dialog.transform.SetAsLastSibling();
    }
    private void Close()
    { if (activeDialog != null) activeDialog.gameObject.SetActive(false); activeDialog = null; modalBlock.SetActive(false); europeQuestion = false; }
    public void GoToEuropeQuestion()
    { Queue(() => { navalDialog.DialogGoToEuropeInit(toEuropeData); europeQuestion = true; Show(navalDialog); }); }
    public void YesGoToEurope() { if (!IsEuropeQuestion) return; Close(); EuropeManager.instance.AllowToGoToEurope(); }
    public void NoGoToEurope() { if (!IsEuropeQuestion) return; Close(); EuropeManager.instance.DeclineGoToEurope(); }
    public void ToggleNavalDialog(bool show)
    { if (show) Show(navalDialog); else if (activeDialog == navalDialog) { if (IsEuropeQuestion) NoGoToEurope(); else { Close(); GameManager.instance.CompleteCombatPresentation(); } } }
    public void CombatAnalysisInit(Unit attacker, Unit defender) { dialogs[9].DialogCombatAnalysisInit(attacker,defender); }
    public void ToggleCombatPanel(bool show)
    { if (show) Show(dialogs[9]); else if (activeDialog == dialogs[9]) Close(); }
    public void ShowCombatResult(string winner, string loser)
    { if (IsOpen) Close(); navalDialog.DialogCombatResultInit(winner,loser); Show(navalDialog); }
    public void ToggleAiMoveDialog(bool show)
    {
        if (!HasWeek12Configuration) return;
        if (show) { dialogs[1].ShowNotiText($"Year: {GameManager.instance.Year}\nAI is moving..."); dialogs[1].OkayButton.gameObject.SetActive(false); Show(dialogs[1]); }
        else if (activeDialog == dialogs[1]) Close();
    }
    public void RequestForeignFaction(Faction faction)
    {
        if (!HasWeek12Configuration || faction == null || faction.MetByPlayer || (activeDialog == dialogs[3] && GameManager.instance.CurForeignFaction == faction) || !queuedFactions.Add(faction)) return;
        Queue(() => { queuedFactions.Remove(faction); if (faction == null || faction.MetByPlayer) return; GameManager.instance.CurForeignFaction = faction; dialogs[3].DialogNativeIntroInit(faction); Show(dialogs[3]); });
    }
    public void ToggleForeignDialog(bool show)
    {
        if (show) RequestForeignFaction(GameManager.instance.CurForeignFaction);
        else if (HasWeek12Configuration && activeDialog == dialogs[3]) { Close(); GameManager.instance.CurForeignFaction = null; }
    }
    public void MakePeaceWithNation(bool peace)
    {
        Faction faction = GameManager.instance.CurForeignFaction;
        if (!HasWeek12Configuration || activeDialog != dialogs[3] || faction == null || faction.MetByPlayer) return;
        faction.MetByPlayer = true; faction.AtPeaceWithPlayer = peace;
        if (peace) ToggleForeignDialog(false); else dialogs[3].ThisMeansWar(faction);
    }
    public void ShowFirstKingQuestDialogText()
    {
        if (!HasWeek12Configuration) return;
        Queue(() => { dialogs[5].SetPortrait(GameManager.instance.PlayerFaction.LeaderSprite); dialogs[5].ShowNotiText("Establish few colonies and sell these raw materials to our market: <color=yellow>Sugar</color>, <color=yellow>Tobacco</color> and <color=yellow>Cotton</color>."); Show(dialogs[5]); });
    }
    public void ToggleKingDialog(bool show) { if (show) ShowFirstKingQuestDialogText(); else if (HasWeek12Configuration && activeDialog == dialogs[5]) Close(); }
    private void Colony(string text) { if (HasWeek12Configuration) Queue(() => { dialogs[6].ShowNotiText(text); Show(dialogs[6]); }); }
    public void ShowColonyClearLandText() { Colony("You must build a settlement on cleared land or hill, not forest or mountain. Press <color=yellow>P</color> to clear forest."); }
    public void ShowColonyNotEnoughToolsText(int count) { Colony($"You don't have enough tools. This unit currently has only <color=yellow>{count}</color> tools. You must have at least 20 tools. Buy more tools from Europe or get it from Blacksmith's house"); }
    public void ShowColonyHasOtherTownAround() { Colony("You can't build a settlement next to another settlement or village."); }
    public void ToggleColonyDialog(bool show) { if (!show && HasWeek12Configuration && activeDialog == dialogs[6]) Close(); }
    public void AcknowledgeActive()
    {
        if (!IsOpen || activeDialog.OkayButton == null || !activeDialog.OkayButton.gameObject.activeSelf) return;
        if (activeDialog == navalDialog) ToggleNavalDialog(false);
        else if (HasWeek12Configuration && activeDialog == dialogs[3]) ToggleForeignDialog(false);
        else Close();
    }
}
