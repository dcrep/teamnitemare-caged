using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

[Serializable]
public enum InteractableType
{
    None,
    Object,
    NPC,
    Other
}

[Serializable]
public abstract class InteractableBase : MonoBehaviour, ISaveable
{
    protected InteractableType interactableType = InteractableType.None;
    public string interactText = "Interact";
    public string interactResponseText = "You interacted with the object!";
    
    public bool isInteractable = true;
    public bool isOneTimeUse = false;
    public int interactionCount = 0;
    public int interactionExitCount = 0;

    [SerializeField] Collider interactionCollider = null;
    [SerializeField] bool autoInteractOnColliderTrigger = false;
    [SerializeField] bool autoInteractOnColliderExitTrigger = false;
    [SerializeField] GameObject playerAutoInteractOrNullForAll = null;

    [SerializeField] GameObject billBoardObject = null;
    [SerializeField] Vector3 billBoardOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] TMP_Text billBoardTextObject = null;
    [SerializeField] Vector3 billBoardTextOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] string billBoardInteractText = "Interact";
    [SerializeField] bool showBillboardOnStart = true;
    //[SerializeField] InkleStoryComponent inkStoryComponentOrNull = null;

    //!!
    [SerializeField] bool hideBillboardOnInteract = true;
    [SerializeField] bool showBillboardOnInteractExit = true;

    GameObject objectTriggeredBy = null;
    bool bDataRestored;

    protected virtual void Awake()
    {
        if (billBoardObject != null)
        {
            // Check if billBoardObject is actually a billBoardText object
            if (billBoardObject.GetComponent<TMP_Text>() != null)
            {
                billBoardTextObject = billBoardObject.GetComponent<TMP_Text>();
                // don't need this anymore
                billBoardObject = null;
            }
            // else it's possible to have BOTH a billboard (say, image) and billboardtext object
        }
        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider>();
            if (interactionCollider == null)
            {
                Debug.LogError("InteractableBase: No interaction collider assigned and no collider found on the object. Please assign a collider to be used as the interaction trigger.");
            }
        }
    }
    protected virtual void Start()
    {
        //SetBillboardVisibility(showBillboardOnStart);
        if (billBoardObject != null)
        {
            billBoardObject.transform.position = transform.position + billBoardOffset;
        }
        if (billBoardTextObject != null)
        {

            billBoardTextObject.text = billBoardInteractText;
            billBoardTextObject.transform.position = transform.position + billBoardTextOffset;
        }
        Debug.Log("IB->Start: Starting interactable of type: " + interactableType + " with interactText: " + interactText + " and isInteractable: " + isInteractable);
        if (bDataRestored)
        {
            if (interactionCount > 0)
            {
                Interact(true);
                // Interact will increment interactionCount (presumably, base class here)
                interactionCount--;
            }
        }
    }

    public abstract bool CanInteract();

    public abstract void SetIsInteractable(bool value);

    public abstract void Interact(bool forceOverride = false);
    public abstract void InteractExit();

    public void SetBillboardText(string text)
    {
        if (billBoardTextObject != null)
        {
            billBoardTextObject.text = text;
        }
    }

    public void SetBillboardVisibility(bool visible)
    {
        if (billBoardObject != null)
        {
            billBoardObject.SetActive(visible);
        }
        if (billBoardTextObject != null)
        {
            billBoardTextObject.gameObject.SetActive(visible);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (autoInteractOnColliderTrigger && (playerAutoInteractOrNullForAll == null || other.gameObject == playerAutoInteractOrNullForAll))
        {
            if (isInteractable)
            {
                Interact();
                objectTriggeredBy = other.gameObject;
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (autoInteractOnColliderTrigger && objectTriggeredBy == other.gameObject)
        {
            if (autoInteractOnColliderExitTrigger && isInteractable)
            {
                InteractExit();
                //inkStoryComponentOrNull?.ExitTriggerArea();
            }
            objectTriggeredBy = null;
        }
    }

    #region ISaveable implementation
    private class InteractableData
    {
        public InteractableType interactableType;
        public string interactText;
        public string interactResponseText;
        
        public bool isInteractable;
        public bool isOneTimeUse;
        public int interactionCount;
    }
    public object CaptureState()
    {
        var data = new InteractableData
        {
            interactableType = this.interactableType,
            interactText = this.interactText,
            interactResponseText = this.interactResponseText,
            isInteractable = this.isInteractable,
            isOneTimeUse = this.isOneTimeUse,
            interactionCount = this.interactionCount
        };
        return data;
    }
    public void RestoreState(object state)
    {
        if (state is InteractableData data)
        {
            this.interactableType = data.interactableType;
            this.interactText = data.interactText;
            this.interactResponseText = data.interactResponseText;
            this.isInteractable = data.isInteractable;
            this.isOneTimeUse = data.isOneTimeUse;            
            this.interactionCount = data.interactionCount;


            Debug.Log("IB->RestoreState: Restored state for interactable of type: " + interactableType + " with interactText: " + interactText + " and isInteractable: " + isInteractable + " and interactionCount: " + interactionCount);
            bDataRestored = true;
            // if (interactionCount > 0)
            // {
            //     Interact(true);
            //     // Interact will increment interactionCount (presumably, base class here)
            //     interactionCount--;
            // }
        }
    }
#endregion ISaveable implementation
}

