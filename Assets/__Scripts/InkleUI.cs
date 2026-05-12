using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using System;


[System.Serializable]
public struct InkleUILayout
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Image continueIcon;
    public List<GameObject> speakerPortraitPanels;
    public List<Image> portraitImages;
    public List<GameObject> displayNamePanels;
    public List<TextMeshProUGUI> displayNameText;
    public List<GameObject> choices;
    public List<TextMeshProUGUI> choicesText;
}

public class InkleUI : MonoBehaviour
{
    [SerializeField] public InkleUILayout layout;

    public bool DialoguePanelIsActive { get; private set; } = false;
    public bool ChoicesAreActive { get; private set; } = false;
    public bool SpeakerPanelActive { get; private set; } = false;
    bool isValid = false;

    void Awake()
    {
        isValid = ValidateUI();
    }

    void OnEnable()
    {
        isValid = ValidateUI();
    }
    void OnDisable()
    {
        DialoguePanelIsActive = false;
        ChoicesAreActive = false;
        SpeakerPanelActive = false;
        isValid = false;        
    }
    public bool ValidateUI()
    {
        // struct can't be null
        //if (layout == null)

        // verify fields
        if (layout.dialoguePanel == null || layout.dialogueText == null || layout.continueIcon == null)
        {
            Debug.LogError("InkUI-> Dialogue panel elements not assigned.");
            return false;
        }
        if (layout.dialoguePanel.activeInHierarchy)
        {
            DialoguePanelIsActive = true;
        }
        if (layout.speakerPortraitPanels.Count == 0 || layout.portraitImages.Count == 0 || layout.displayNamePanels.Count == 0 || layout.displayNameText.Count == 0)
        {
            Debug.LogError("InkUI-> Speaker panel elements not assigned.");
            return false;
        }
        else if (layout.speakerPortraitPanels.Count != layout.portraitImages.Count || layout.speakerPortraitPanels.Count != layout.displayNamePanels.Count || layout.speakerPortraitPanels.Count != layout.displayNameText.Count)
        {
            Debug.LogError("InkUI-> Speaker panel elements count mismatch.");
            return false;
        }
        if (layout.choices.Count == 0 || layout.choicesText.Count == 0)
        {
            Debug.LogError("InkUI-> Choice panel elements not assigned.");
            return false;
        }
        else if (layout.choices.Count != layout.choicesText.Count)
        {
            Debug.LogError("InkUI-> Choice panel elements count mismatch.");
            return false;
        }
        if (layout.choices[0].activeInHierarchy)
        {
            ChoicesAreActive = true;
        }
        isValid = true;
        UpdateSpeakerPanelActiveState();
        return true;
    }
    void UpdateSpeakerPanelActiveState()
    {
        if (!isValid) return;
        SpeakerPanelActive = false;
        for (int i = 0; i < layout.speakerPortraitPanels.Count; i++)
        {
            if (layout.speakerPortraitPanels[i].activeInHierarchy)
            {
                SpeakerPanelActive = true;
                break;
            }
        }
    }

    public void UpdateDialogueText(string text)
    {
        if (isValid)
        {
            layout.dialogueText.text = text;
        }
    }

#region Hide-Show UI Elements
    public void ShowPanel()
    {
        if (isValid)
        {
            layout.dialoguePanel.SetActive(true);
            DialoguePanelIsActive = true;
        }
    }

    public void HidePanel()
    {
        if (isValid)
        {
            layout.dialoguePanel.SetActive(false);
            DialoguePanelIsActive = false;
        }
    }
    public void ShowSpeakers(List<InkleSpeakerInfo> speakers)
    {
        if (!isValid) return;
        if (speakers == null || speakers.Count == 0)
        {
            Debug.LogWarning("InkUI->ShowSpeakers called with null or empty speakers list.");
            return;
        }
        int numSpeakers = speakers.Count; 

        if (numSpeakers > layout.speakerPortraitPanels.Count)
        {
            Debug.LogWarning("InkUI->Number of speakers exceeds the number of speaker panel UI elements. Some speakers will not be displayed.");
            numSpeakers = layout.speakerPortraitPanels.Count; // limit to max available UI elements
        }

        //Debug.Log("InkUI->Showing " + numSpeakers + " speakers.");

        // because this can be complex, we'll disable all panels,
        // then re-enable only the ones we need based on speaker location, and set their portraits and names
        for (int i = 0; i < layout.speakerPortraitPanels.Count; i++)
        {
            layout.speakerPortraitPanels[i].SetActive(false);
            layout.displayNamePanels[i].SetActive(false);
        }

        // populate speaker locations
        for (int i = 0; i < numSpeakers; i++)
        {
            int locationInt = (int)speakers[i].speakerLocation;
            if (locationInt < 1 || locationInt > layout.speakerPortraitPanels.Count)
            {
                Debug.LogWarning("InkUI->ShowSpeakers: Invalid speaker location for speaker " + speakers[i].speakerName + ". Defaulting to right.");
                locationInt = 3; // default to right
            }
            //Debug.Log("InkUI->Showing speaker " + speakers[i].speakerName + " at location " + speakers[i].speakerLocation);
            layout.speakerPortraitPanels[locationInt - 1].SetActive(true);
            layout.displayNamePanels[locationInt - 1].SetActive(true);
            if (!string.IsNullOrEmpty(speakers[i].speakerName))
            {
                layout.displayNameText[locationInt - 1].text = speakers[i].speakerName;
            }
            if (speakers[i].portrait != null)
            {
                Debug.Log("InkUI->Setting portrait for speaker " + speakers[i].speakerName + " at location " + speakers[i].speakerLocation);
                layout.portraitImages[locationInt - 1].sprite = speakers[i].portrait; // == null ? Resources.Load<Sprite>("InklePortraits/" + "npcDefault") : speakers[locationInt - 1].portrait;
            }
        }
        SpeakerPanelActive = true;
    }

    public void HideSpeakerPanel(int speakerIndex)
    {
        if (!isValid) return;
        if (speakerIndex < 0 || speakerIndex >= layout.speakerPortraitPanels.Count)
        {
            Debug.LogWarning("InkUI->HideSpeakerPanel called with invalid speaker index: " + speakerIndex);
            return;
        }
        layout.speakerPortraitPanels[speakerIndex].SetActive(false);
        layout.displayNamePanels[speakerIndex].SetActive(false);
        UpdateSpeakerPanelActiveState();
    }

    public void HideAllSpeakerPanels()
    {
        if (!isValid) return;
        for (int i = 0; i < layout.speakerPortraitPanels.Count; i++)
        {
            Debug.Log("InkUI->Hiding speaker panel at index: " + i + " with name: " + layout.displayNameText[i].text);
            layout.speakerPortraitPanels[i].SetActive(false);
            layout.displayNamePanels[i].SetActive(false);
        }
        SpeakerPanelActive = false;
    }

    public void ShowDialogueInterface(bool bypassChecks = false)
    {
        if (DialoguePanelIsActive && !bypassChecks)
        {
            return;
        }
        if (!isValid) return;

        ShowPanel();
        HideAllSpeakerPanels();
        HideChoiceUI();
    }

    public void HideDialogueInterface()
    {
        if (!DialoguePanelIsActive)
        {
            return;
        }

        if (layout.dialoguePanel != null)
        {
            layout.dialoguePanel.SetActive(false);
        }

        if (layout.speakerPortraitPanels != null)
        {
            for (int i = 0; i < layout.speakerPortraitPanels.Count; i++)
            {
                if (layout.speakerPortraitPanels[i] != null)
                {
                    layout.speakerPortraitPanels[i].SetActive(false);
                    layout.displayNamePanels[i]?.SetActive(false);
                }
            }
        }

        HideChoiceUI();
        DialoguePanelIsActive = false;
    }

    public bool HideUI()
    {
        if (!isValid) return false;
        
        if (layout.speakerPortraitPanels != null)
        {
            for (int i = 0; i < layout.speakerPortraitPanels.Count; i++)
            {
                if (layout.speakerPortraitPanels[i] != null)
                {
                    layout.speakerPortraitPanels[i].SetActive(false);
                    layout.displayNamePanels[i]?.SetActive(false);
                }
            }
        }
        HideChoiceUI();
        // hide dialogue panel last so that it doesn't interfere with hiding other elements
        layout.dialoguePanel.SetActive(false);

        DialoguePanelIsActive = false;
        return true;
    }

    public void ShowChoiceUI(List<Choice> choices, int defaultChoiceIndex = 0)
    {
        if (!isValid) return;
        if (choices == null || choices.Count == 0)
        {
            Debug.LogWarning("InkUI->ShowChoiceUI called with null or empty choices list.");
            return;
        }
        int numChoices = choices.Count; 

        if (numChoices > layout.choices.Count)
        {
            Debug.LogWarning("InkUI->Number of choices exceeds the number of choice UI elements. Some choices will not be displayed.");
            numChoices = layout.choices.Count; // limit to max available UI elements
        }

        for (int i = 0; i < layout.choices.Count; i++)
        {
            if (i < numChoices)
            {
                layout.choices[i].SetActive(true);
            }
            else
            {
                layout.choices[i].SetActive(false);
            }
        }
        // hide continue icon when choices are present
        if (layout.continueIcon != null)
        {
            layout.continueIcon.enabled = false;
        }
                    
        for (int i = 0; i < numChoices; i++)
        {
            layout.choicesText[i].text = choices[i].text;
        }

                // Select 1st choice by default (done in ShowChoiceUI).

        //onChoicesPresented.Invoke();

        ChoicesAreActive = true;
        // select first choice by default
        if (numChoices > 0)
        {
            Debug.Log("InkUI->Highlighting choice # " + defaultChoiceIndex + " by default: " + layout.choices[defaultChoiceIndex].name);
            StartCoroutine(SelectUIChoice(defaultChoiceIndex));
            // This supposedly works but not reliably:
            // layout.choices[defaultChoiceIndex].GetComponent<UnityEngine.UI.Button>().Select();
            // The following might not work so a coroutine might be needed (see SelectFirstChoice coroutine)
            // EventSystem.current.SetSelectedGameObject(null);
            // EventSystem.current.SetSelectedGameObject(layout.choices[defaultChoiceIndex].gameObject);
        }
    }

    private IEnumerator SelectUIChoice(int choiceIndex = 0)
    {
        //Event System requires we clear it first, then wait
        //for at least one frame before we set the current selected object
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(layout.choices[choiceIndex].gameObject);
    }

    public bool HideChoiceUI()
    {
        if (!isValid) return false;
        if (layout.choices == null)
        {
            ChoicesAreActive = false;
            return true;
        }

        foreach (var choice in layout.choices)
        {
            if (choice != null)
            {
                choice.SetActive(false);
            }
        }
        // show continue icon when choices are hidden (and supposedly more dialogue to continue)
        if (layout.continueIcon != null)
        {
            layout.continueIcon.enabled = true;
        }
        ChoicesAreActive = false;
        return true;
    }
#endregion Hide-Show UI Elements

#region Button-Choice Interaction
    public void InkleUIMakeChoice(int choiceIndex)
    {
        InkleDialogue.MakeUIChoice(choiceIndex);
    }
#endregion Button-Choice Interaction
}
