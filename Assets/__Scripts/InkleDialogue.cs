using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Ink.Runtime;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
//using System.Diagnostics;

//TODO: Separate out InkleStoryComponent objects & variable/tag watches
// Can keep general events for variable/tag watchers but these will
// need to know story state..
// InkleStoryComponent should be Interactable derived w/billboard that turns off
// during interaction, and optionally back on afterwards
// also option to disable player movement during dialogue, or allow player
// on=exit collider to end dialogue..

// special Inkle-related UnityEvents that take strings as parameters

// string tagName
public class StringEvent : UnityEvent<string> { }
public class StringListEvent : UnityEvent<List<string>> { }

// string variableName, string triggerValue
public class TwoStringEvent : UnityEvent<string, string> { }

[Serializable]
public struct InkleVariableWatch
{
    public string variableName;
    public string triggerValueOrAll;
    public TwoStringEvent onVariableMatch;
}
[Serializable]
public struct InkleTagWatch
{
    public string tagName;
    public StringEvent onTagFound;
}

[System.Serializable]
public struct InkleSpeakerImages
{
    public string speakerName;
    public Sprite portrait;
}

[Serializable]
public struct InkleDialogueData
{
    public string text;
    public string[] options;
}

public class InkleDialogue : MonoBehaviour
{
    [SerializeField] private GameObject inkleDialoguePanelPrefab;
    GameObject dialoguePanel;
    private InkleUILayout uiLayout;

    [SerializeField] List<InkleSpeakerImages> speakerImages = new List<InkleSpeakerImages>();

    [SerializeField] string speakerTagPrefix = "speaker:";
    [SerializeField] string speakerLocationTagPrefix = "speakerLocation:";
    [SerializeField] string speakerImageTagPrefix = "speakerImage:";

    [SerializeField] List<InkleVariableWatch> variableWatches = new List<InkleVariableWatch>();
    [SerializeField] List<InkleTagWatch> tagWatches = new List<InkleTagWatch>();
    [SerializeField] StringListEvent onTagsFound = new StringListEvent();

    [SerializeField] UnityEvent onDialogueStarted;
    [SerializeField] UnityEvent onDialogueEnded;
    [SerializeField] UnityEvent onChoicesPresented;
    [SerializeField] UnityEvent onChoiceSelection;

    [SerializeField] bool playerMovementDisabledDuringDialogue = true;
    //[SerializeField] bool playerMovementStopsDialogue = false;    

    //[SerializeField] private TextAsset inkJSON;

    // public get, private set
    public bool DialogueIsPlaying { get; private set; } = false;
    public bool DialoguePanelIsActive { get; private set; } = false;

    private Story currentStory = null;
    private string storyName = "";
    private string currentText = "";
    private List<Choice> currentChoices = null;
    private List<string> currentTags = new List<string>();
    bool speakerActive = false;
    int currentSpeakerIndex = 0;

    private InputSystem_Actions inputActions;
    private Coroutine restoreControlsAfterDialogueCoroutine;
    private bool consumeOpeningInput = false;

    static public InkleDialogue Instance { get; private set; }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("InkleDialogue: Multiple instances of InkleDialogue detected in scene. There should only be one instance. Destroying duplicate.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        LoadAndConfigureInklePanelPrefab();

        ConfigureEventSystemForDialogue();

        inputActions = new InputSystem_Actions();

        if (dialoguePanel == null)
        {
            Debug.LogError("InkleDialogue: One or more UI components not assigned in inspector.");
        }
    }
    void Start()
    {
        dialoguePanel.SetActive(false);
    }

    // I would like to include this script in the prefab but it's tricky with a canvas element prefab

    void LoadAndConfigureInklePanelPrefab()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (inkleDialoguePanelPrefab == null)
        {
            Debug.LogError("InkleDialogue: Inkle dialogue panel prefab not assigned in inspector.");
            //inkleDialoguePanelPrefab = Resources.Load<GameObject>("Prefabs/" + "InklePanel");
            return;
        }
        dialoguePanel = Instantiate(inkleDialoguePanelPrefab, canvas.transform);
        //dialoguePanel.transform.SetParent(canvas.transform, false);

        uiLayout = dialoguePanel.GetComponent<InkleUIPrefab>().layout;
        
    // prefab structure:
    // InklePanel (Canvas)
    // - InkleText (TextMeshProUGUI)
    // - DialogueChoices (GameObject)
    //   - Choice0 (Button)
    //     - Choice0Text (TextMeshProUGUI)
    //   - Choice1 (Button)
    //     - Choice1Text (TextMeshProUGUI)
    // - Speaker1 (GameObject) (left location)
    //   - PortraitFrame (GameObject)
    //     - PortraitImage (Image)
    //   - SpeakerFrame (GameObject)
    //     - Border (GameObject)
    //     - DisplayNameText (TextMeshProUGUI)
    // - Speaker2 (dupe of Speaker1, with center location)
    // - Speaker3 (dupe of Speaker1, with right location)
        // dialogueText = dialoguePanel.transform.Find("InkleText").GetComponent<TextMeshProUGUI>();
        // GameObject dialogueChoicesPanel = dialoguePanel.transform.Find("DialogueChoices").gameObject;
        // choices = new List<GameObject>();
        // choicesText = new List<TextMeshProUGUI>();
        // for (int i = 0; i < 10; i++)
        // {
        //     GameObject choice = dialogueChoicesPanel.transform.Find($"Choice{i}")?.gameObject;
        //     Debug.Log("Looking for choice: " + $"Choice{i}, found: " + (choice != null) + " with name: " + (choice != null ? choice.name : "null"));
        //     if (choice == null)
        //         break;
        //     choices.Add(choice);
        //     choicesText.Add(choice.transform.Find($"Choice{i}Text").GetComponent<TextMeshProUGUI>());
        // }
        // speakerPanels = new List<GameObject>();
        // displayNameText = new List<TextMeshProUGUI>();
        // portraitImages = new List<Image>();
        // for (int i = 0; i < 3; i++)
        // {
        //     GameObject speakerPanel = dialoguePanel.transform.Find($"Speaker{i+1}").gameObject;
        //     if (speakerPanel == null)
        //         break;
        //     speakerPanels.Add(speakerPanel);
        //     portraitImages.Add(speakerPanel.transform.Find("PortraitFrame").Find("PortraitImage").GetComponent<Image>());
        //     displayNameText.Add(speakerPanel.transform.Find("SpeakerFrame").Find("DisplayNameText").GetComponent<TextMeshProUGUI>());
        // }
        Debug.Log("InkleDialogue: Loaded dialogue panel prefab and assigned UI components. Hiding dialogue panel.");
        dialoguePanel.SetActive(false);
    }

    void OnEnable()
    {
        inputActions.Enable();
        
        if (DialogueIsPlaying)
        {
            inputActions.Player.Interact.performed += OnInteractPerformed;
            inputActions.Player.Attack.performed += OnClickAnywhereToContinue; // test: disable click-anywhere advance
        }
    }

    void OnDisable()
    {
        HideDialogueInterface();
        ResetState();
        inputActions.Player.Attack.performed -= OnClickAnywhereToContinue;
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        inputActions.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start() {}

    // Update is called once per frame
    // void Update() {}

    public void StartDialogue(TextAsset inkStoryJSON)
    {
        StartDialogue(inkStoryJSON.text, inkStoryJSON.name);
    }
    public void StartDialogue(string inkStoryJSON, string storyName)
    {
        Debug.Log("InkleDialogue: Starting dialogue with story: " + storyName);
        if (DialogueIsPlaying)
        {
            Debug.LogWarning("InkleDialogue: Attempted to start dialogue while another dialogue is already playing. Ending current dialogue and starting new one.");
            EndDialogue();
        }
        else
            ResetState();

        this.storyName = storyName;

        DialogueIsPlaying = true;
        if (playerMovementDisabledDuringDialogue)
        {
            // disable player movement 
            // TODO: need to check this state in playerController scripts!
            GameManager.Instance.DisableAllControls();
        }
        currentStory = new Story(inkStoryJSON);

        currentStory.onError += (msg, type) => {
            if( type == Ink.ErrorType.Warning )
                Debug.LogWarning(msg);
            else
                Debug.LogError(msg);
        };

        ConfigureEventSystemForDialogue();
        SetupVariableListeners();
        ShowDialogueInterface();

        GameManager.Instance.ModalDialogueSetIsOpen();

        inputActions.Player.Attack.performed += OnClickAnywhereToContinue; // test: disable click-anywhere advance
        inputActions.Player.Interact.performed += OnInteractPerformed;
        consumeOpeningInput = true;
        
        onDialogueStarted.Invoke();
        ContinueStory();        
    }

    bool ConsumeOpeningInputIfNeeded()
    {
        if (!consumeOpeningInput)
        {
            return false;
        }

        if (inputActions == null)
        {
            consumeOpeningInput = false;
            return false;
        }

        if (inputActions.Player.Attack.IsPressed() || inputActions.Player.Interact.IsPressed())
        {
            //Debug.Log("InkleDialogue: Consuming opening input to prevent unintended advance or choice selection.");
            consumeOpeningInput = false;
            return true;
        }

        consumeOpeningInput = false;
        return false;
    }

    void ConfigureEventSystemForDialogue()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
        {
            Debug.LogError("InkleDialogue: No EventSystem found in scene. Please add an EventSystem to the scene for dialogue choices to work.");
            return;
        }

        if (!es.enabled)
        {
            es.enabled = true;
        }

        InputSystemUIInputModule uiModule = es.GetComponent<InputSystemUIInputModule>();
        if (uiModule == null)
        {
            Debug.LogError("InkleDialogue: EventSystem is missing InputSystemUIInputModule. Hover and button clicks will not work.");
            return;
        }

        if (!uiModule.enabled)
        {
            uiModule.enabled = true;
        }

        // Ensure background clicks don't clear focused choice while dialogue options are visible.
        uiModule.deselectOnBackgroundClick = false;

        // If UI actions were not assigned in the scene/prefab, hovering and OnClick won't fire.
        if (uiModule.point == null || uiModule.leftClick == null)
        {
            uiModule.AssignDefaultActions();
        }
    }

    public void EndDialogue()
    {
        if (!DialogueIsPlaying)
        {
            return;
        }
        inputActions.Player.Attack.performed -= OnClickAnywhereToContinue;
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        DisableVariableListeners();
        currentStory = null;        
        onDialogueEnded.Invoke();
        HideDialogueInterface();
        DialogueIsPlaying = false;
        ResetState();
        GameManager.Instance.ModalDialogueSetIsClosed();
        if (playerMovementDisabledDuringDialogue) 
        {
            // if (restoreControlsAfterDialogueCoroutine != null)
            // {
            //     StopCoroutine(restoreControlsAfterDialogueCoroutine);
            // }
            // restoreControlsAfterDialogueCoroutine = StartCoroutine(RestoreControlsAfterDialogueInputRelease());
            GameManager.Instance.EnableAllControls();
        }
    }

    // private IEnumerator RestoreControlsAfterDialogueInputRelease()
    // {
    //     yield return null;

    //     while (inputActions != null && (inputActions.Player.Attack.IsPressed() || inputActions.Player.Interact.IsPressed()))
    //     {
    //         yield return null;
    //     }

    //     GameManager.Instance.EnableAllControls();
    //     restoreControlsAfterDialogueCoroutine = null;
    // }

    void ContinueStory(bool maximalContinue = false)
    {
        // if "canContinue" it means there is more text and no choices
        if (currentStory.canContinue)
        {
            if (maximalContinue)
            {
                currentText = currentStory.ContinueMaximally();
            }
            else
            {
                currentText = currentStory.Continue();
            }
            uiLayout.dialogueText.text = currentText;
            InternalCheckTagsAndHandleSpecialTags();
            CheckTagsAndInvokeTagEvents();
            
            currentChoices = currentStory.currentChoices;
            // ShowChoices():
            if (currentChoices != null && currentChoices.Count > 0)
            {
                ShowChoiceUI(currentChoices.Count);
                for (int i = 0; i < currentChoices.Count; i++)
                {
                    uiLayout.choicesText[i].text = currentChoices[i].text;
                }
                onChoicesPresented.Invoke();
                // Select 1st choice by default (done in ShowChoiceUI).
            }
            else
            {
                HideChoiceUI();
            }
        }
        else
        {
            EndDialogue();
        }
    }

    void ContinueStoryMaximally()
    {
        ContinueStory(true);
    }

    // Static call for UI buttons absent a direct InkleDialogue reference.
    public static void MakeUIChoice(int choiceIndex)
    {
        if (Instance == null)
        {
            Debug.LogError("InkleDialogue: No instance of InkleDialogue found in scene. Cannot make choice.");
            return;
        }
        Instance.MakeChoice(choiceIndex);
    }

    public void MakeChoice(int choiceIndex)
    {
        Debug.Log("InkleDialogue: Making choice with index: " + choiceIndex);
        if (currentChoices == null || choiceIndex < 0 || choiceIndex >= currentChoices.Count)
        {
            Debug.LogError("InkleDialogue: Invalid choice index: " + choiceIndex);
            return;
        }
        onChoiceSelection.Invoke();
        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    // choose path using a named knot in the story
    public void ChoosePathString(string path)
    {
        currentStory.ChoosePathString(path);
    }

    public int GetVisitCountAtPathString(string path)
    {
        return currentStory.state.VisitCountAtPathString(path);
    }

    void TrySubmitSelectedChoice()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("InkleDialogue: No EventSystem found in scene. Cannot submit selected choice.");
            return;
        }
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
        {
            Debug.Log("InkleDialogue: No selected object. Cannot submit selected choice.");
            return;
        }
        for (int i = 0; i < uiLayout.choices.Count; i++)
        {
            if (selectedObject == uiLayout.choices[i] || selectedObject.transform.IsChildOf(uiLayout.choices[i].transform))
            {
                MakeChoice(i);
                return;
            }
        }
    }


    enum SpeakerLocation
    {
        Left = 1,
        Center = 2,
        Right = 3        
    }

    void InternalCheckTagsAndHandleSpecialTags()
    {
        // SpeakerLocation currentSpeakerLocation = SpeakerLocation.Right; // default location
        bool speakerFound = false;
        bool speakerPortraitFound = false;
        string speakerName = "";
        Sprite newPortrait = null;

        // handle special tags that have built-in functionality in this script, such as showing the speaker panel or setting the speaker location
        foreach (var tag in currentStory.currentTags)
        {
            Debug.Log("InkleDialogue: Checking tag: " + tag);

            if (tag.Trim().StartsWith(speakerTagPrefix))
            {
                speakerName = tag.Substring(speakerTagPrefix.Length);
                if (string.IsNullOrEmpty(speakerName) || speakerName == "OFF")
                {
                    // if speaker name is empty or "OFF", hide speaker panel(s)
                    for (int i = 0; i < uiLayout.speakerPanels.Count; i++)
                    {
                        uiLayout.speakerPanels[i].SetActive(false);
                    }
                    speakerActive = false;
                    currentSpeakerIndex = 0;
                    continue;
                }
                else
                {
                    //speaker1Panel.SetActive(true);
                    speakerActive = true;
                    //displayNameText.text = speakerName;
                    speakerFound = true;
                }
            }
            else if (tag.Trim().StartsWith(speakerLocationTagPrefix))
            {
                string location = tag.Substring(speakerLocationTagPrefix.Length);
                // set speaker panel location based on location string, e.g. "left", "right", "center"
                switch (location)
                {
                    case "left":
                        // currentSpeakerLocation = SpeakerLocation.Left;
                        currentSpeakerIndex = 1;
                        break;
                    case "center":
                        // currentSpeakerLocation = SpeakerLocation.Center;
                        currentSpeakerIndex = 2;
                        break;
                    case "right":
                        // currentSpeakerLocation = SpeakerLocation.Right;
                        currentSpeakerIndex = 3;
                        break;
                    default:
                        Debug.LogWarning("InkleDialogue: Unrecognized speaker location tag: " + tag + " valid options: left, center, right, OFF");
                        break;
                }
            }
            else if (tag.Trim().StartsWith(speakerImageTagPrefix))
            {
                speakerPortraitFound = true;
                newPortrait = GetPortrait(speakerName, tag.Substring(speakerImageTagPrefix.Length));
            }
        }
        if (!speakerFound)
        {
            if (!speakerActive)
            {
                for (int i = 0; i < uiLayout.speakerPanels.Count; i++)
                {
                    uiLayout.speakerPanels[i].SetActive(false);
                }
                currentSpeakerIndex = 0;
            }
        }
        else
        {
            if (currentSpeakerIndex == 0)
            {
                // if no location tag was found, default to right
                currentSpeakerIndex = (int)SpeakerLocation.Right;
            }
            // set speakers other than currentSpeakerIndex to inactive
            for (int i = 0; i < uiLayout.speakerPanels.Count; i++)
            {
                if (i != currentSpeakerIndex - 1)
                {
                    uiLayout.speakerPanels[i].SetActive(false);
                }
            }
            uiLayout.speakerPanels[currentSpeakerIndex - 1].SetActive(true);
            uiLayout.displayNameText[currentSpeakerIndex - 1].text = speakerName;
            if (newPortrait == null)
            {
                newPortrait = GetPortrait(speakerName);
            }
        }
        if (speakerPortraitFound && speakerActive)
        {
            uiLayout.portraitImages[currentSpeakerIndex - 1].sprite = newPortrait;
        }
    }
    Sprite GetPortrait(string speakerName, string imageName = "")
    {
        if (!string.IsNullOrEmpty(speakerName))
        {
            // ignoring case and whitespace
            speakerName = speakerName.Trim().ToLower();
            foreach (var speakerImage in speakerImages)
            {
                // ignore case and whitespace when comparing speaker names to find portrait
                if (speakerImage.speakerName.Trim().ToLower() == speakerName)
                {
                    Debug.Log("InkleDialogue.GetPortrait: Found portrait sprite in speakerImages list for speaker: " + speakerName);
                    //uiLayout.portraitImages[currentSpeakerIndex - 1].sprite = speakerImage.portrait;
                    return speakerImage.portrait;
                }
            }
            //TODO: try linked InkleStoryComponent
        }
        // else:
        //  speakerName is empty OR no matching speakername's image found

        // Last: try resources
        Sprite newPortrait = Resources.Load<Sprite>("InklePortraits/" + imageName);
        if (newPortrait == null)
        {
            newPortrait = Resources.Load<Sprite>("InklePortraits/" + "npcDefault");
        }
        Debug.Log("InkleDialogue.GetPortrait: Loaded portrait sprite: " + (newPortrait != null ? newPortrait.name : "null") + " for tag: " + tag);
        return newPortrait;
    }

    void ResetState()
    {
        DialogueIsPlaying = false;
        DialoguePanelIsActive = false;
        currentStory = null;
        currentChoices = null;
        currentText = "";
        currentTags.Clear();
        speakerActive = false;
        consumeOpeningInput = false;
    }

#region Save/Load Reset story state
    public string GetStorySaveState()
    {
        if (currentStory == null)
        {
            Debug.LogWarning("InkleDialogue: Attempted to save story state, but no current story exists.");
            return "";
        }
        return currentStory.state.ToJson();
        //PlayerPrefs.SetString("inkleStorySaveState", currentStory.state.ToJson());
    }
    public void SetStoryFromSaveState(string jsonState)
    {
        if (currentStory == null)
        {
            Debug.LogWarning("InkleDialogue: Attempted to load story state, but no current story exists.");
            return;
        }
        currentStory.state.LoadJson(jsonState);
    }
    public void ResetStoryState()
    {
        if (currentStory == null)
        {
            Debug.LogWarning("InkleDialogue: Attempted to reset story state, but no current story exists.");
            return;
        }
        currentStory.ResetState();
    }
#endregion Save/Load story state

#region Input Callbacks
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }

        if (!DialogueIsPlaying || currentStory == null)
        {
            //Debug.Log("Interact performed, but no dialogue is currently playing.");
            return;
        }

        if (ConsumeOpeningInputIfNeeded())
        {
            return;
        }

        if (GetCurrentChoicesCount() == 0)
        {
            Debug.Log("Continuing story with no choices available.");
            ContinueStory();
            return;
        }
        //Debug.Log("Attempting to submit selected choice on interact.");

        TrySubmitSelectedChoice();
    }
    private void OnClickAnywhereToContinue(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }
        if (!DialogueIsPlaying)
        {
            return;
        }

        if (ConsumeOpeningInputIfNeeded())
        {
            return;
        }

        // clicking outside of choices only continues story if
        // there are no choices
        if (GetCurrentChoicesCount() > 0)
        {
            // we may have deactivated the UI by NOT clicking a choice,so we need to refocus the UI

            return;
        }

        ContinueStory();
    }
#endregion Input Callbacks

#region UI Methods
    public void ShowDialogueInterface()
    {
        if (DialoguePanelIsActive)
        {
            return;
        }
        DialoguePanelIsActive = true;
        // enable interface
        dialoguePanel.SetActive(true);
        for (int i = 0; i < uiLayout.speakerPanels.Count; i++)
        {
            uiLayout.speakerPanels[i].SetActive(false);
        }
        HideChoiceUI();
    }
    void ShowChoiceUI(int numChoices, int defaultChoiceIndex = 0)
    {
        //onChoicesPresented.Invoke();
        for (int i = 0; i < uiLayout.choices.Count; i++)
        {
            if (i < numChoices)
            {
                uiLayout.choices[i].SetActive(true);
            }
            else
            {
                uiLayout.choices[i].SetActive(false);
            }
        }
        // hide continue icon when choices are present
        if (uiLayout.continueIcon != null)
        {
            uiLayout.continueIcon.enabled = false;
        }
        // select first choice by default
        if (numChoices > 0)
        {
            Debug.Log("Highlighting first choice by default: " + uiLayout.choices[0].name);
            StartCoroutine(SelectFirstChoice());
            // uiLayout.choices[0].GetComponent<UnityEngine.UI.Button>().Select();
            // The following might not work so a coroutine might be needed (see SelectFirstChoice coroutine)
            // EventSystem.current.SetSelectedGameObject(null);
            // EventSystem.current.SetSelectedGameObject(uiLayout.choices[0].gameObject);
        }
    }

    private IEnumerator SelectFirstChoice()
    {
        //Event System requires we clear it first, then wait
        //for at least one frame before we set the current selected object
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(uiLayout.choices[0].gameObject);
    }

    void HideChoiceUI()
    {
        if (uiLayout.choices == null)
        {
            return;
        }

        foreach (var choice in uiLayout.choices)
        {
            if (choice != null)
            {
                choice.SetActive(false);
            }
        }
        // show continue icon when choices are hidden (and supposedly more dialogue to continue)
        if (uiLayout.continueIcon != null)
        {
            uiLayout.continueIcon.enabled = true;
        }
    }
    public void HideDialogueInterface()
    {
        if (!DialogueIsPlaying || !DialoguePanelIsActive)
        {
            return;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (uiLayout.speakerPanels != null)
        {
            for (int i = 0; i < uiLayout.speakerPanels.Count; i++)
            {
                if (uiLayout.speakerPanels[i] != null)
                {
                    uiLayout.speakerPanels[i].SetActive(false);
                }
            }
        }

        HideChoiceUI();
        DialoguePanelIsActive = false;
        //DialogueManager.GetInstance().HideDialogueInterface();
    }
#endregion UI Methods

#region Listeners for Ink story state changes
    void SetupVariableListeners()
    {
        currentStory.variablesState.variableChangedEvent += VariableChanged;
        // foreach (var variableWatch in variableWatches)
        // {
        //     currentStory.ObserveVariable(variableWatch.variableName, (variableName, newValue) =>
        //     {
        //         if (variableWatch.triggerValueOrAll == "all" || variableWatch.triggerValueOrAll == newValue.ToString())
        //         {
        //             variableWatch.onVariableMatch.Invoke(variableName, newValue.ToString());
        //         }
        //     });
        // }
    }
    void VariableChanged(string variableName, Ink.Runtime.Object newValue)
    {
        foreach (var variableWatch in variableWatches)
        {
            if (variableWatch.variableName == variableName && (variableWatch.triggerValueOrAll == "all" || variableWatch.triggerValueOrAll == newValue.ToString()))
            {
                variableWatch.onVariableMatch.Invoke(variableName, newValue.ToString());
            }
        }
    }
    void DisableVariableListeners()
    {
        currentStory.variablesState.variableChangedEvent -= VariableChanged;
        // foreach (var variableWatch in variableWatches)
        // {
        //     currentStory.ObserveVariable(variableWatch.variableName, null);
        // }
    }
    void CheckTagsAndInvokeTagEvents()
    {
        currentTags = currentStory.currentTags;
        onTagsFound.Invoke(currentTags);
        foreach (var tag in currentTags)
        {
            foreach (var tagWatch in tagWatches)
            {
                if (tagWatch.tagName == tag)
                {
                    tagWatch.onTagFound.Invoke(tag);
                }
            }
        }
    }
#endregion Listeners for Ink story state changes

#region Player Move Options
    public bool IsPlayerMovementDisabledDuringDialogue()
    {
        return playerMovementDisabledDuringDialogue;
    }
    // public bool DoesPlayerMovementStopDialogue()
    // {
    //     return playerMovementStopsDialogue;
    // }
    public void DisablePlayerMovementDuringDialogue()
    {
        playerMovementDisabledDuringDialogue = true;
        if (DialogueIsPlaying)
        {
            GameManager.Instance.DisableAllControls();
        }
    }
    public void EnablePlayerMovementDuringDialogue()
    {
        if (DialogueIsPlaying)
        {
            GameManager.Instance.EnableAllControls();
        }
        playerMovementDisabledDuringDialogue = false;
    }
    // public void SetPlayerMovementStopsDialogue(bool stops)
    // {
    //     playerMovementStopsDialogue = stops;
    // }
#endregion Player Move Options

#region Listeners: Dialogue events

    public void AddTagListener(UnityAction<string> action, string tagName)
    {
        foreach (var tagWatch in tagWatches)
        {
            if (tagWatch.tagName == tagName)
            {
                tagWatch.onTagFound.AddListener(action);
                return;
            }
        }
        // no current tag watch for this tag, so add one
        var newTagWatch = new InkleTagWatch
        {
            tagName = tagName,
            onTagFound = new StringEvent()
        };
        tagWatches.Add(newTagWatch);
        newTagWatch.onTagFound.AddListener(action);
    }
    public void RemoveTagListener(UnityAction<string> action, string tagName)
    {
        foreach (var tagWatch in tagWatches)
        {
            if (tagWatch.tagName == tagName)
            {
                tagWatch.onTagFound.RemoveListener(action);
                return;
            }
        }
    }
    public void AddTagsListener(UnityAction<List<string>> action)
    {
        onTagsFound.AddListener(action);
    }
    public void RemoveTagsListener(UnityAction<List<string>> action)
    {
        onTagsFound.RemoveListener(action);
    }

    public void AddVariableListener(UnityAction<string, string> action, string variableName, string triggerValueOrAll = "all")
    {
        foreach (var variableWatch in variableWatches)
        {
            if (variableWatch.variableName == variableName && variableWatch.triggerValueOrAll == triggerValueOrAll)
            {
                variableWatch.onVariableMatch.AddListener(action);
                return;
            }
        }
        // no current variable watch for this variable and trigger value, so add one
        var newVariableWatch = new InkleVariableWatch
        {
            variableName = variableName,
            triggerValueOrAll = triggerValueOrAll,
            onVariableMatch = new TwoStringEvent()
        };
        variableWatches.Add(newVariableWatch);
        newVariableWatch.onVariableMatch.AddListener(action);
    }
    public void RemoveVariableListener(UnityAction<string, string> action, string variableName, string triggerValueOrAll = "all")
    {
        foreach (var variableWatch in variableWatches)
        {
            if (variableWatch.variableName == variableName && variableWatch.triggerValueOrAll == triggerValueOrAll)
            {
                variableWatch.onVariableMatch.RemoveListener(action);
                return;
            }
        }
    }
    public void AddDialogueStartedListener(UnityAction action)
    {
        onDialogueStarted.AddListener(action);
    }
    public void RemoveDialogueStartedListener(UnityAction action)
    {
        onDialogueStarted.RemoveListener(action);
    }
    public void AddDialogueEndedListener(UnityAction action)
    {
        onDialogueEnded.AddListener(action);
    }
    public void RemoveDialogueEndedListener(UnityAction action)
    {
        onDialogueEnded.RemoveListener(action);
    }
    public void AddChoiceSelectionListener(UnityAction action)
    {
        onChoiceSelection.AddListener(action);
    }
    public void RemoveChoiceSelectionListener(UnityAction action)
    {
        onChoiceSelection.RemoveListener(action);
    }
#endregion Listeners: Dialogue events

#region Queries: Dialogue state
    List<string> GetCurrentTags()
    {
        return currentTags;
    }
    string GetCurrentText()
    {
        return currentText;
    }
    List<Choice> GetCurrentChoices()
    {
        return currentChoices;
    }
    int GetCurrentChoicesCount()
    {
        if (currentChoices == null)
        {
            return 0;
        }
        return currentChoices.Count;
    }
    string[] GetCurrentChoicesAsStrings()
    {
        if (currentChoices == null)
        {
            return new string[0];
        }
        string[] choiceStrings = new string[currentChoices.Count];
        for (int i = 0; i < currentChoices.Count; i++)
        {
            choiceStrings[i] = currentChoices[i].text;
        }
        return choiceStrings;
    }
    object GetVariableValue(string variableName)
    {
        if (currentStory == null)
        {
            return "";
        }
        return currentStory.variablesState[variableName];
    }
#endregion Queries: Dialogue state

#region Set variable value
    void SetVariableValue(string variableName, object value)
    {
        if (currentStory == null)
        {
            return;
        }
        currentStory.variablesState[variableName] = value;
        // check if any variable watches should be triggered by this variable change
        foreach (var variableWatch in variableWatches)
        {
            if (variableWatch.variableName == variableName && (variableWatch.triggerValueOrAll == "all" || variableWatch.triggerValueOrAll == value.ToString()))
            {
                variableWatch.onVariableMatch.Invoke(variableName, value.ToString());
            }
        }
    }
#endregion Set variable value


}
