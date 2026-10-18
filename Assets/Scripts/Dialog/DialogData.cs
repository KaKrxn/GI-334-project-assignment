using UnityEngine;

[CreateAssetMenu(fileName = "DialogData", menuName = "Scriptable Objects/DialogData")]
public class DialogData : ScriptableObject
{
    public Sprite advisorSprite;
    public Sprite dialogSprite;
    public string question;
    public string[] answers;
}
