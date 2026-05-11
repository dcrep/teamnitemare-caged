using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestionDialogueUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private Button yesBtn;
    [SerializeField] private Button noBtn;

    private void Awake()
    {
        ShowQuestion("Do you want to clean the dishes?", () => {
            Debug.Log("Yes");
        }, () =>
        {
            Debug.Log("No");
        });
    }

    public void ShowQuestion(string questionText, Action yesAction, Action noAction)
    {
        textMeshPro.text = questionText;
        yesBtn.onClick.AddListener(() =>
        {
            Hide();
            yesAction();
        });
        noBtn.onClick.AddListener(() =>
        {
            Hide(); noAction();
        });
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }

}
