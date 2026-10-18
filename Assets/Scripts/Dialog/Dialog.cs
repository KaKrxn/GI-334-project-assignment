using TMPro;
using UnityEngine;

public class Dialog : MonoBehaviour
{
    [SerializeField] private TMP_Text notiText;
    [SerializeField] private TMP_Text yesText;
    [SerializeField] private TMP_Text noText;
    [SerializeField] private UnityEngine.UI.Button btnYes;
    [SerializeField] private UnityEngine.UI.Button btnNo;
    [SerializeField] private UnityEngine.UI.Button btnOkay;
    [SerializeField] private TMP_Text okayText;
    [SerializeField] private UnityEngine.UI.Image charImage;
    [SerializeField] private UnityEngine.UI.Image attackerFactionImage;
    [SerializeField] private UnityEngine.UI.Image[] attackerImage;
    [SerializeField] private TMP_Text[] attackerNameText;
    [SerializeField] private TMP_Text[] attackerStrText;
    [SerializeField] private UnityEngine.UI.Image defenderFactionImage;
    [SerializeField] private UnityEngine.UI.Image[] defenderImage;
    [SerializeField] private TMP_Text[] defenderNameText;
    [SerializeField] private TMP_Text[] defenderStrText;
    public TMP_Text NotiText { get { return notiText; } }
    public UnityEngine.UI.Button YesButton { get { return btnYes; } }
    public UnityEngine.UI.Button NoButton { get { return btnNo; } }
    public UnityEngine.UI.Button OkayButton { get { return btnOkay; } }
    public UnityEngine.UI.Image CharImage { get { return charImage; } }
    public void DialogGoToEuropeInit(DialogData data)
    { notiText.text = data.question; yesText.text = data.answers[0]; noText.text = data.answers[1]; Buttons(true); }
    private void Buttons(bool choice)
    {
        if (btnYes != null) btnYes.gameObject.SetActive(choice);
        if (btnNo != null) btnNo.gameObject.SetActive(choice);
        if (btnOkay != null) btnOkay.gameObject.SetActive(!choice);
    }
    public void SetPortrait(Sprite sprite) { if (charImage != null) { charImage.sprite = sprite; charImage.gameObject.SetActive(sprite != null); } }
    public void ShowNotiText(string text) { notiText.text = text; Buttons(false); }
    public void DialogCombatResultInit(string winner, string loser)
    { ShowNotiText("<color=yellow>" + winner + "</color> defeats <color=yellow>" + loser + "</color>"); }
    public void DialogCombatAnalysisInit(Unit attacker, Unit defender)
    {
        attackerFactionImage.sprite = attacker.Faction.ShieldIcon; defenderFactionImage.sprite = defender.Faction.ShieldIcon;
        for (int i = 0; i < attackerImage.Length; i++)
        {
            attackerImage[i].transform.parent.gameObject.SetActive(i == 0);
            defenderImage[i].transform.parent.gameObject.SetActive(i == 0);
        }
        attackerImage[0].sprite = attacker.UnitSprite.sprite; attackerNameText[0].text = attacker.UnitName; attackerStrText[0].text = attacker.Strength.ToString();
        defenderImage[0].sprite = defender.UnitSprite.sprite; defenderNameText[0].text = defender.UnitName; defenderStrText[0].text = defender.Strength.ToString();
    }
    public void DialogNativeIntroInit(Faction faction)
    {
        SetPortrait(faction.LeaderSprite);
        notiText.text = faction.IsEuropean ?
            $"Greetings and welcome to <color=yellow>New {faction.Nation}</color>. We have justly claimed all of this land in the name of King of <color=yellow>{faction.Nation}</color>. Please do not interfere with us. We propose a <color=yellow>peace treaty</color>. Do you agree?" :
            $"The <color=yellow>{faction.Nation}</color> tribe welcomes you. To celebrate our friendship, we generously offer you the land you now occupy as a gift. Will you accept our treaty and live with us in peace as brothers?";
        yesText.text = "Yes"; noText.text = "No"; Buttons(true);
    }
    public void ThisMeansWar(Faction faction)
    {
        ShowNotiText(faction.IsEuropean ? $"This means you are at war with <color=yellow>{faction.Nation}</color> already. See you on the battlefield!" :
            $"My tribe, <color=yellow>{faction.Nation}</color> tribe will kill all of you. Prepare to Die!!!");
    }
}
