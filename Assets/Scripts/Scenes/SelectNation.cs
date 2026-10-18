using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectNation : MonoBehaviour
{
    [SerializeField] private Sprite[] leaderSprites;
    [SerializeField] private UnityEngine.UI.Image leaderImage;
    [SerializeField] private UnityEngine.UI.Toggle[] nationToggles;
    [SerializeField] private string nextScene = "Map01";
    [SerializeField] private string previousScene = "MainMenu";
    [SerializeField] private int index;
    public int Index { get { return index; } }
    public UnityEngine.UI.Image LeaderImage { get { return leaderImage; } }
    public UnityEngine.UI.Toggle[] NationToggles { get { return nationToggles; } }
    [SerializeField] private GameObject blackPanel;
    [SerializeField] private GameObject[] panels;
    public int IntroStep { get; private set; } = -1;
    public GameObject[] IntroPanels { get { return panels; } }
    public bool HasIntroConfiguration { get { return blackPanel != null && panels != null && panels.Length == 3; } }
    public void ShowKingsCommand()
    {
        if (!HasIntroConfiguration || IntroStep != -1) return;
        IntroStep = 0; blackPanel.SetActive(true); blackPanel.transform.SetAsLastSibling();
        UnityEngine.UI.Image portrait = panels[0].transform.Find("ExpeditionImage")?.GetComponent<UnityEngine.UI.Image>();
        if (portrait != null && leaderImage != null) portrait.sprite = leaderImage.sprite;
        panels[0].SetActive(true); panels[0].transform.SetAsLastSibling();
    }
    public void ShowPrepareToLeave() { ShowIntroStep(0,1); }
    public void ShowLeftThePort() { ShowIntroStep(1,2); }
    private void ShowIntroStep(int previous, int next)
    {
        if (!HasIntroConfiguration || IntroStep != previous) return;
        panels[previous].SetActive(false); IntroStep = next; panels[next].SetActive(true); panels[next].transform.SetAsLastSibling();
    }

    private void Start()
    {
        int selected = Settings.playerNationId;
        SelectEuropeNation(selected >= 0 && selected < Settings.EuropeanNationCount ? selected : 0);
    }

    public void SelectEuropeNation(int i)
    {
        if (IntroStep != -1 || i < 0 || i >= Settings.EuropeanNationCount) return;
        index = Settings.playerNationId = i;
        if (leaderImage != null && leaderSprites != null && i < leaderSprites.Length && leaderSprites[i] != null)
            leaderImage.sprite = leaderSprites[i];
        // ToggleGroup emits both the old false and new true notifications.
        // NationCard forwards only true; synchronize without recursive callbacks.
        if (nationToggles != null)
            for (int n = 0; n < nationToggles.Length; n++)
                if (nationToggles[n] != null) nationToggles[n].SetIsOnWithoutNotify(n == i);
    }

    public void StartGame() { if (!HasIntroConfiguration || IntroStep == 2) SceneManager.LoadScene(nextScene); }
    public void BackToMainMenu() { if (IntroStep == -1) SceneManager.LoadScene(previousScene); }
}
