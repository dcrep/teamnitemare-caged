using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public struct InkleUILayout
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Image continueIcon;
    public List<GameObject> speakerPanels;
    public List<Image> portraitImages;
    public List<TextMeshProUGUI> displayNameText;
    public List<GameObject> choices;
    public List<TextMeshProUGUI> choicesText;
}

public class InkleUIPrefab : MonoBehaviour
{
    [SerializeField] public InkleUILayout layout;

    public void InkleUIMakeChoice(int choiceIndex)
    {
        InkleDialogue.MakeUIChoice(choiceIndex);
    }
}
