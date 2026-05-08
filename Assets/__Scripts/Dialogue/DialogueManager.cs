using UnityEngine;
using TMPro;
using Ink.Runtime;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using UnityEngine.SearchService;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]

    [SerializeField] private GameObject dialoguePanel;

    [SerializeField] private TextMeshProUGUI dialogueText;

    [SerializeField] private TextMeshProUGUI displayNameText;

    [SerializeField] private Animator portraitAnimator;

    private Animator layoutAnimator;

    [Header("Choices UI")]

    [SerializeField] private GameObject[] choices;

    private TextMeshProUGUI[] choicesText; 

    private Story currentStory;
    private InputSystem_Actions inputActions;
    private bool consumeNextInteract;

    public bool dialogueIsPlaying { get; private set; }
    public event Action<bool> DialogueStateChanged;

    private static DialogueManager instance;

    private const string SPEAKER_TAG = "speaker";
    private const string PORTRAIT_TAG = "portrait";
    private const string LAYOUT_TAG = "layout";


    private DialogueVariables dialogueVariables;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the Scene");
        }
        instance = this;

        dialogueVariables = new DialogueVariables();
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputSystem_Actions();
        }

        inputActions.Enable();
        inputActions.Player.Interact.started += OnInteractPerformed;
    }

    private void OnDisable()
    {
        if (inputActions == null)
        {
            return;
        }

        inputActions.Player.Interact.started -= OnInteractPerformed;
        inputActions.Disable();
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        SetDialogueState(false);
        dialoguePanel.SetActive(false);

        //get the layout animator
        layoutAnimator = dialoguePanel.GetComponent<Animator>();

        //get all of the choices text
        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>(true);
            index++;
        }

    }

    private void OnClickAnywhereToContinue(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }
        if (!dialogueIsPlaying)
        {
            return;
        }

        if (currentStory.currentChoices.Count > 0)
        {
            return;
        }

        ContinueStory();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }
        if (consumeNextInteract)
        {
            consumeNextInteract = false;
            //Debug.Log("Consumed interact input to prevent immediately advancing dialogue.");
            return;
        }

        if (!dialogueIsPlaying || currentStory == null)
        {
            //Debug.Log("Interact performed, but no dialogue is currently playing.");
            return;
        }

        if (currentStory.currentChoices.Count == 0)
        {
            //Debug.Log("Continuing story with no choices available.");
            ContinueStory();
            return;
        }
        //Debug.Log("Attempting to submit selected choice on interact.");

        TrySubmitSelectedChoice();
    }

    public void EnterDialogueMode(TextAsset inkJSON, bool consumeCurrentInteract = false)
    {
        if (inkJSON == null)
        {
            Debug.LogError("Cannot enter dialogue mode with a null Ink JSON asset.");
            return;
        }

        currentStory = new Story(inkJSON.text);
        consumeNextInteract = consumeCurrentInteract;
        SetDialogueState(true);
        dialoguePanel.SetActive(true);
        
       //reset portrait, layout, and speaker
       if (displayNameText != null)
       {
           displayNameText.text = "???";
       }
       if (portraitAnimator != null)
       {
           portraitAnimator.Play("default");
       }
        if (layoutAnimator != null)
        {
            layoutAnimator.Play("right");
        }

        dialogueVariables.StartListening(currentStory);

        GameManager.Instance.ModalDialogueSetIsOpen();

        inputActions.Player.Attack.started += OnClickAnywhereToContinue;

        ContinueStory();
    }

    private IEnumerator ExitDialogueMode()
    {
        inputActions.Player.Attack.started -= OnClickAnywhereToContinue;
        yield return new WaitForSeconds(0.2f);

        if (currentStory != null)
        {
            dialogueVariables.StopListening(currentStory);
        }

        currentStory = null;
        SetDialogueState(false);
        dialoguePanel.SetActive(false);
        dialogueText.text = "";

        GameManager.Instance.ModalDialogueSetIsClosed();
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            //set text for the current dialogue line
            dialogueText.text = currentStory.Continue();
            // display choices, if any, for this dialogue line
            DisplayChoices();
            // handle tags
            HandleTags(currentStory.currentTags); 
        }
        else
        {
            StartCoroutine(ExitDialogueMode());
        }
    }

    private void HandleTags(List<string> currentTags)
    {
        //Loop through each tag and handle it accordingly
        foreach (string tag in currentTags)
        {
            //parse the tag
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be appropriately parsed: " + tag);
            }
            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            // handle the tag
            switch (tagKey)
            {
                case SPEAKER_TAG:
                    Debug.Log("speaker=" + tagValue);
                    displayNameText.text = tagValue;
                    break;
                case PORTRAIT_TAG:
                    Debug.Log("portrait=" + tagValue);
                    portraitAnimator.Play(tagValue);
                    break;
                case LAYOUT_TAG:
                    Debug.Log("layout=" + tagValue);
                    layoutAnimator.Play(tagValue);
                    break;
                default:
                    Debug.LogWarning("Tag came in but is currently not being handled: " + tag);
                    break;
            }
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;
        
        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError("More choices were given than the UI can support. Number of choices given: " + currentChoices.Count);
        }

        int index = 0;
        //enable and initialize the choices up to the amount of choices for this line of dialogue
        foreach (Choice choice in currentChoices)
        {
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }
        //go through the remaining choices the UI supports and make sure they're hidden
        for (int i = index; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(false);
        }

        if (currentChoices.Count > 0)
        {
            StartCoroutine(SelectFirstChoice());
        }

    }
    private IEnumerator SelectFirstChoice()
    {
        //Event System requires we clear it first, then wait
        //for at least one frame before we set the current selected object
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }

    public void MakeChoice(int choiceIndex)
    {
        if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }
        if (currentStory == null)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= currentStory.currentChoices.Count)
        {
            Debug.LogWarning($"Choice index {choiceIndex} is out of range.");
            return;
        }

        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    private void TrySubmitSelectedChoice()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
        {
            return;
        }

        for (int i = 0; i < choices.Length; i++)
        {
            if (choices[i] == selectedObject || selectedObject.transform.IsChildOf(choices[i].transform))
            {
                MakeChoice(i);
                return;
            }
        }
    }

    private void SetDialogueState(bool isPlaying)
    {
        if (dialogueIsPlaying == isPlaying)
        {
            return;
        }

        dialogueIsPlaying = isPlaying;
        DialogueStateChanged?.Invoke(dialogueIsPlaying);
    }

}

