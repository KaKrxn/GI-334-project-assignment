using UnityEngine;

public class NationCard : MonoBehaviour
{
    [SerializeField] private SelectNation selectNation;
    [SerializeField] private int nationIndex;
    public int NationIndex { get { return nationIndex; } }

    public void OnValueChanged(bool selected)
    {
        if (selected && selectNation != null) selectNation.SelectEuropeNation(nationIndex);
    }
}
