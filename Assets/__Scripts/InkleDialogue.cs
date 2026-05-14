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

// Inkle: https://www.inklestudios.com/ink/
// @ Unity Store (version lag): https://assetstore.unity.com/packages/tools/integration/ink-integration-for-unity-60055
// Inkle Programming Docs: https://github.com/inkle/ink/blob/master/Documentation/RunningYourInk.md
// Inkle Writing Docs: https://github.com/inkle/ink/blob/master/Documentation/WritingWithInk.md

//TODO: Separate out InkleStoryComponent objects & variable/tag watches
// Can keep general events for variable/tag watchers but these will
// need to know story state..
// InkleStoryComponent should be Interactable derived w/billboard that turns off
// during interaction, and optionally back on afterwards
// also option to disable player movement during dialogue, or allow player
// on=exit collider to end dialogue..
//TODO: animation? (fade-in/out of characters, move to left/center/right), audio? (dialogue,music,sfx), AudioSource

//TODO: Use cases - renPy option, subtitles
// Future Tags:
//  #scale:x.xx
//  #pos:x=-x.xx;y=-y.yy
//  #vfx: fadeIn, fadeOut, slideLeft, slideRight (moving speaker location)
//  #sfx: audioClip;vol:x.xx;tm:1.5-2.5 (or start-2.5/1.5-end)
//  #vo: audioClip;vol:x.xx;tm:1.5-2.5 (or start-2.5/1.5-end) [maybe oneShot vs music/longer or looping audio options]
//  #bg: backgroundImage (+vfx crossFade, etc)
// Subtitle setup:
// Tags for time #sub=1.5;2.5
// OR include in start of dialogue like [1.5-2.5] subtitless..

// special Inkle-related UnityEvents that take strings as parameters

// string tagName
[Serializable]
public class StringEvent : UnityEvent<string> { }
[Serializable]
public class StringListEvent : UnityEvent<List<string>> { }

// string variableName, string triggerValue
[Serializable]
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

public enum InkleSpeakerLocation
{
    Left = 1,
    Center = 2,
    Right = 3        
}

[Serializable]
public struct InkleSpeakerInfo
{
    public string speakerName;
    public InkleSpeakerLocation speakerLocation;
    public Sprite portrait;
}

public class InkleDialogue : MonoBehaviour
{
    [SerializeField] private GameObject inkleDialoguePanelPrefab;
    GameObject dialoguePanel;
    //private InkleUILayout uiLayout;
    private InkleUI inkleUI;

    [SerializeField] List<InkleSpeakerImages> speakerImages = new List<InkleSpeakerImages>();

    [SerializeField] string speakerTagPrefix = "actor:";
    [SerializeField] string speakerLocationTagPrefix = "actorLoc:";
    [SerializeField] string speakerImageTagPrefix = "actorPic:";

    List<InkleSpeakerInfo> currentSpeakers = new List<InkleSpeakerInfo>();

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

    public int LastSelectedChoiceIndex { get; private set; } = -1;
    public string LastSelectedChoiceText { get; private set; } = "";

    private Story currentStory = null;
    private string storyName = "";
    public string CurrentStoryName => storyName;
    private string currentText = "";
    public string CurrentStoryText => currentText;
    private List<Choice> currentChoices = null;
    private List<string> currentTags = new List<string>();
    bool speakerActive = false;
    //int currentSpeakerIndex = 0;

    private InputSystem_Actions inputActions;
    private Coroutine restoreControlsAfterDialogueCoroutine;
    private bool consumeOpeningInput = false;

    static public InkleDialogue Instance { get; private set; }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("InkD-> Multiple instances of InkleDialogue detected in scene. There should only be one instance. Destroying duplicate.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        LoadAndConfigureInklePanelPrefab();

        ConfigureEventSystemForDialogue();

        inputActions = new InputSystem_Actions();

        if (dialoguePanel == null)
        {
            Debug.LogError("InkD-> One or more UI components not assigned in inspector.");
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
            Debug.LogError("InkD-> Inkle dialogue panel prefab not assigned in inspector.");
            //inkleDialoguePanelPrefab = Resources.Load<GameObject>("Prefabs/" + "InklePanel");
            return;
        }
        dialoguePanel = Instantiate(inkleDialoguePanelPrefab, canvas.transform);
        //dialoguePanel.transform.SetParent(canvas.transform, false);

        inkleUI = dialoguePanel.GetComponent<InkleUI>();

        //uiLayout = dialoguePanel.GetComponent<InkleUI>().layout;
        
    //! prefab structure should be ignored and maybe have a script
    // that acts as an interface for communication from this script?
    // ! OR we will have to adjust to allow speaker and portrait to be separate from speaker object..
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
        Debug.Log("InkD-> Loaded dialogue panel prefab and assigned UI components. Hiding dialogue panel.");
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
        inkleUI.HideDialogueInterface();
        InternalResetState();
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
    public void StartDialogueAtKnot(TextAsset inkStoryJSON, string startingKnot)
    {
        StartDialogue(inkStoryJSON.text, inkStoryJSON.name, startingKnot);
    }
    public void StartDialogueAtKnot(string inkStoryJSON, string storyName, string startingKnot)
    {
        StartDialogue(inkStoryJSON, storyName, startingKnot);
    }
    public void StartDialogue(string inkStoryJSON, string storyName, string startingKnot = "")
    {
        Debug.Log("InkD-> Starting dialogue with story: " + storyName);
        if (DialogueIsPlaying)
        {
            Debug.LogWarning("InkD-> Attempted to start dialogue while another dialogue is already playing. Ending current dialogue and starting new one.");
            EndDialogue();
        }
        else
            InternalResetState();

        dialoguePanel.SetActive(true);

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

        if (!string.IsNullOrEmpty(startingKnot))
        {
            currentStory.ChoosePathString(startingKnot);
        }

        ConfigureEventSystemForDialogue();
        SetupVariableListeners();
        inkleUI.ShowDialogueInterface(true);

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
            //Debug.Log("InkD-> Consuming opening input to prevent unintended advance or choice selection.");
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
            Debug.LogError("InkD-> No EventSystem found in scene. Please add an EventSystem to the scene for dialogue choices to work.");
            return;
        }

        if (!es.enabled)
        {
            es.enabled = true;
        }

        InputSystemUIInputModule uiModule = es.GetComponent<InputSystemUIInputModule>();
        if (uiModule == null)
        {
            Debug.LogError("InkD-> EventSystem is missing InputSystemUIInputModule. Hover and button clicks will not work.");
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
        inkleUI.HideDialogueInterface();
        DialogueIsPlaying = false;
        InternalResetState();
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
            inkleUI.UpdateDialogueText(currentText);
            //uiLayout.dialogueText.text = currentText;
            InternalCheckTagsAndHandleSpecialTags();
            CheckTagsAndInvokeTagEvents();
            
            currentChoices = currentStory.currentChoices;
            // ShowChoices():
            if (currentChoices != null && currentChoices.Count > 0)
            {
                inkleUI.ShowChoiceUI(currentChoices, 0);
                onChoicesPresented.Invoke();
            }
            else
            {
                inkleUI.HideChoiceUI();
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
            Debug.LogError("InkD-> No instance of InkleDialogue found in scene. Cannot make choice.");
            return;
        }
        Instance.MakeChoice(choiceIndex);
    }

    public void MakeChoice(int choiceIndex)
    {
        Debug.Log("InkD-> Making choice with index: " + choiceIndex);
        if (currentChoices == null || choiceIndex < 0 || choiceIndex >= currentChoices.Count)
        {
            Debug.LogError("InkD-> Invalid choice index: " + choiceIndex);
            return;
        }
        LastSelectedChoiceIndex = choiceIndex;
        LastSelectedChoiceText = currentChoices[choiceIndex].text;
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
            Debug.LogError("InkD-> No EventSystem found in scene. Cannot submit selected choice.");
            return;
        }
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
        {
            Debug.Log("InkD-> No selected object. Cannot submit selected choice.");
            return;
        }
        for (int i = 0; i < inkleUI.layout.choices.Count; i++)
        {
            if (selectedObject == inkleUI.layout.choices[i] || selectedObject.transform.IsChildOf(inkleUI.layout.choices[i].transform))
            {
                MakeChoice(i);
                return;
            }
        }
    }

    void InternalCheckTagsAndHandleSpecialTags()
    {
        // SpeakerLocation currentSpeakerLocation = SpeakerLocation.Right; // default location
        bool speakerFound = false;
        bool speakerLocationFound = false;
        bool speakerPortraitFound = false;
        int changeOfSpeakers = 0;
        // add current speakers
        string speakerName = "";

        // handle special tags that have built-in functionality in this script, such as showing the speaker panel or setting the speaker location
        foreach (var tag in currentStory.currentTags)
        {
            Debug.Log("InkD-> Checking tag: " + tag);

            if (tag.Trim().StartsWith(speakerTagPrefix))
            {
                speakerName = tag.Trim().Substring(speakerTagPrefix.Length);
                // check for "+" which indicates more than one speaker
                if (speakerName.Contains("+"))
                {
                    // clearing previous speakers 1st
                    currentSpeakers.Clear();
                    Debug.Log("InkD-> Multiple speakers specified in tag: " + tag);
                    // get speakers, strip whitespace, and add to active speakers list
                    String[] speakerNames = speakerName.Split('+');
                    foreach (var name in speakerNames)
                    {
                        string trimmedName = name.Trim();
                        if (!string.IsNullOrEmpty(trimmedName))
                        {
                            currentSpeakers.Add(new InkleSpeakerInfo { speakerName = trimmedName,
                                speakerLocation = InkleSpeakerLocation.Right, portrait = null });
                            changeOfSpeakers++;
                        }
                    }
                    speakerActive = true;
                    speakerFound = true;
                }
                else if (string.IsNullOrEmpty(speakerName) || speakerName == "OFF")
                {
                    changeOfSpeakers = currentSpeakers.Count == 0 ? 0 : currentSpeakers.Count; // 0 if none or all off
                    currentSpeakers.Clear();
                    // if speaker name is empty or "OFF", hide speaker panel(s)
                    inkleUI.HideAllSpeakerPanels();
                    speakerActive = false;
                    //currentSpeakerIndex = 0;
                    continue;
                }
                else
                {
                    changeOfSpeakers = currentSpeakers.Count == 0 ? 1 : currentSpeakers.Count + 1;
                    currentSpeakers.Clear();
                    currentSpeakers.Add(new InkleSpeakerInfo { speakerName = speakerName });
                    //speaker1Panel.SetActive(true);
                    speakerActive = true;
                    //displayNameText.text = speakerName;
                    speakerFound = true;
                }
            }
            else if (tag.Trim().StartsWith(speakerLocationTagPrefix))
            {
                string location = tag.Trim().Substring(speakerLocationTagPrefix.Length);
                // check for "+" which indicates more than one speaker
                if (location.Contains("+"))
                {
                    Debug.Log("InkD-> Multiple speaker locations specified in tag: " + tag);
                    // get speakers, strip whitespace, and add to active speakers list
                    String[] speakerLocations = location.Split('+');
                    if (speakerLocations.Length != currentSpeakers.Count)
                    {
                        Debug.LogWarning("InkD-> Number of speaker locations specified in tag: " + tag + " does not match number of active speakers. Must come AFTER speaker tags. Ignoring speaker location tag.");
                        continue;
                    }
            
                    for (int index = 0; index < speakerLocations.Length; index++)
                    {
                        // structs require pulling, modifying, and re-adding to list
                        var speaker = currentSpeakers[index];
                        speaker.speakerLocation = GetSpeakerLocationFromTag(speakerLocations[index].Trim());
                        currentSpeakers[index] = speaker;
                    }
                }
                else if (!string.IsNullOrEmpty(location))
                {
                    if (currentSpeakers.Count == 0)
                    {
                        Debug.LogWarning("InkD-> Speaker location tag found but no active speakers. Tag: " + tag + " Ignoring speaker location tag.");
                        continue;
                    }
                    // structs require pulling, modifying, and re-adding to list
                    var speaker = currentSpeakers[0];
                    speaker.speakerLocation = GetSpeakerLocationFromTag(location); // already trimmed above
                    currentSpeakers[0] = speaker;
                    if (currentSpeakers.Count > 1)
                    {
                        Debug.LogWarning("InkD-> Speaker location tag found but multiple active speakers. Tag: " + tag + " Setting only 1st speaker location. Must specify multiple locations with '+' if multiple speakers are active.");
                    }
                }
                speakerLocationFound = true;
            }
            else if (tag.Trim().StartsWith(speakerImageTagPrefix))
            {
                string imageName = tag.Trim().Substring(speakerImageTagPrefix.Length);
                // check for "+" which indicates more than one speaker
                if (imageName.Contains("+"))
                {   
                    Debug.Log("InkD-> Multiple speaker images specified in tag: " + tag);
                    // get images, strip whitespace, and add to active speakers list
                    String[] speakerImages = imageName.Split('+');
                    if (speakerImages.Length != currentSpeakers.Count)
                    {
                        Debug.LogWarning("InkD-> Number of speaker images specified in tag: " + tag + " does not match number of active speakers. Must come AFTER speaker tags. Ignoring speaker image tag.");
                        continue;
                    }
            
                    for (int index = 0; index < speakerImages.Length; index++)
                    {
                        // structs require pulling, modifying, and re-adding to list
                        var speaker = currentSpeakers[index];
                        speaker.portrait = GetPortrait(speaker.speakerName, speakerImages[index].Trim());
                        currentSpeakers[index] = speaker;
                    }
                }
                else if (!string.IsNullOrEmpty(imageName))
                {
                    if (currentSpeakers.Count == 0)
                    {
                        Debug.LogWarning("InkD-> Speaker image tag found but no active speakers. Tag: " + tag + " Ignoring speaker image tag.");
                        continue;
                    }
                    else if (currentSpeakers.Count > 0)
                    {
                        var speaker = currentSpeakers[0];
                        speaker.portrait = GetPortrait(speaker.speakerName, imageName);
                        currentSpeakers[0] = speaker;
                        if (currentSpeakers.Count > 1)
                        {
                            Debug.LogWarning("InkD-> Speaker image tag found but multiple active speakers. Tag: " + tag + " Setting only 1st speaker image tag. Must specify multiple images with '+' if multiple speakers are active.");
                        }                        
                    }
                }
                speakerPortraitFound = true;
            }
        }
        // if (!speakerFound)
        // {
        //     if (!speakerActive)
        //     {
        //         inkleUI.HideAllSpeakerPanels();
        //         //currentSpeakerIndex = 0;
        //     }
        // }
        // else 
        if (changeOfSpeakers > 0 && currentSpeakers.Count > 0)
        {
            inkleUI.ShowSpeakers(currentSpeakers);
        }
        else if ( (speakerFound || speakerLocationFound || speakerPortraitFound) && currentSpeakers.Count > 0)
        {
            inkleUI.ShowSpeakers(currentSpeakers);
        }
    }

    InkleSpeakerLocation GetSpeakerLocationFromTag(string location)
    {
        switch (location.ToLower())
        {
            case "left":
                return InkleSpeakerLocation.Left;
            case "center":
                return InkleSpeakerLocation.Center;
            case "right":
                return InkleSpeakerLocation.Right;
            default:
                Debug.LogWarning("InkD-> Invalid speaker location in tag: " + location + ". Defaulting to right.");
                return InkleSpeakerLocation.Right;
        }
    }

    Sprite GetPortrait(string speakerName, string imageName = "")
    {
        Debug.Log("InkD-> GetPortrait called with speakerName: " + speakerName + " and imageName: " + imageName);
        if (!string.IsNullOrEmpty(speakerName))
        {
            // ignoring case and whitespace
            speakerName = speakerName.Trim().ToLower();
            foreach (var speakerImage in speakerImages)
            {
                // ignore case and whitespace when comparing speaker names to find portrait
                if (speakerImage.speakerName.Trim().ToLower() == speakerName)
                {
                    Debug.Log("InkD->GetPortrait: Found portrait sprite in speakerImages list for speaker: " + speakerName);
                    if (string.IsNullOrEmpty(imageName) || speakerImage.portrait.name.ToLower() == imageName.ToLower())
                    {
                        return speakerImage.portrait;
                    }
                    else
                    {
                        Debug.LogWarning("InkD->GetPortrait: Portrait name: " + speakerImage.portrait.name + " does not match imageName from tag: " + imageName + ".");
                    }
                }
            }
            //TODO: try linked InkleStoryComponent
        }
        // else:
        //  speakerName is empty OR no matching speakername's image+imagename found

        // Last: try resources
        Sprite newPortrait = Resources.Load<Sprite>("InklePortraits/" + imageName);
        if (newPortrait == null)
        {
            newPortrait = Resources.Load<Sprite>("InklePortraits/" + "npcDefault");
        }
        Debug.Log("InkD->GetPortrait: Loaded portrait sprite: " + (newPortrait != null ? newPortrait.name : "null") + " for image: " + imageName);
        return newPortrait;
    }

    void InternalResetState()
    {
        DialogueIsPlaying = false;
        DialoguePanelIsActive = false;
        currentSpeakers = new List<InkleSpeakerInfo>();
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
            Debug.LogWarning("InkD-> Attempted to save story state, but no current story exists.");
            return "";
        }
        return currentStory.state.ToJson();
        //PlayerPrefs.SetString("inkleStorySaveState", currentStory.state.ToJson());
    }
    public void SetStoryFromSaveState(string jsonState)
    {
        if (currentStory == null)
        {
            Debug.LogWarning("InkD-> Attempted to load story state, but no current story exists.");
            return;
        }
        currentStory.state.LoadJson(jsonState);
    }
    public void ResetStoryState()
    {
        if (currentStory == null)
        {
            Debug.LogWarning("InkD-> Attempted to reset story state, but no current story exists.");
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
    public string GetStoryName()
    {
        return storyName;
    }
    public string GetCurrentText()
    {
        return currentText;
    }
    public List<string> GetCurrentTags()
    {
        return currentTags;
    }
    public List<Choice> GetCurrentChoices()
    {
        return currentChoices;
    }
    public int GetCurrentChoicesCount()
    {
        if (currentChoices == null)
        {
            return 0;
        }
        return currentChoices.Count;
    }
    public string[] GetCurrentChoicesAsStrings()
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
    public object GetVariableValue(string variableName)
    {
        if (currentStory == null)
        {
            return "";
        }
        return currentStory.variablesState[variableName];
    }
#endregion Queries: Dialogue state

#region Set variable value
    public void SetVariableValue(string variableName, object value)
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
