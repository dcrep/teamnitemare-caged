using UnityEngine;

public class AutoPlayDialogueNoTrigger : MonoBehaviour
{

    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;
    [SerializeField] private GameObject autodialogue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke(nameof(PlayDialogue), 12f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void PlayDialogue()
    {
        DialogueManager.GetInstance().EnterDialogueMode(inkJSON);
        autodialogue.SetActive(false);
    }

}
