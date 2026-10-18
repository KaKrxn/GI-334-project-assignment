using UnityEngine.EventSystems;

// Older milestone scenarios predate the mandatory Week 12 notices. Acknowledge
// only those notices through their wired UI buttons, preserving the runtime rules.
public static class ColonizationLegacyDialogFixture
{
    public static bool AcknowledgeNotice()
    {
        DialogManager manager=DialogManager.instance;
        if(manager==null || !manager.HasWeek12Configuration || !manager.IsOpen) return false;
        Dialog dialog=manager.ActiveDialog;
        if(dialog!=manager.Dialogs[3] && dialog!=manager.Dialogs[5] && dialog!=manager.Dialogs[6]) return false;
        UnityEngine.UI.Button button=dialog.YesButton.gameObject.activeSelf?dialog.YesButton:dialog.OkayButton;
        if(button==null || !button.gameObject.activeSelf || !button.interactable) return false;
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        return true;
    }
}
