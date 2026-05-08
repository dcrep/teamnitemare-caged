using UnityEngine;

public class InteractableObject : InteractableBase
{
    public UnityEngine.Events.UnityEvent onInteract;
    public UnityEngine.Events.UnityEvent onInteractExit;

    InteractableObject() : base()
    {
        interactableType = InteractableType.Object;
    }
    protected override void Awake()
    {
        base.Awake();
        // Additional initialization if needed
    }
    protected override void Start()
    {
        base.Start();
        // Additional initialization if needed
    }

    public override bool CanInteract()
    {
        return isInteractable;
    }

    public override void SetIsInteractable(bool value)
    {
        isInteractable = value;
        if (value && interactionCount > 0 && isOneTimeUse)
        {
            interactionCount = 0;
        }
    }

    [ContextMenu("Manual Trigger")]
    public void ManualInteract()
    {
        if (isInteractable)
        {
            onInteract.Invoke();
            interactionCount++;
        }
    }

    public override void Interact(bool forceOverride = false)
    {
        if (!isInteractable && !forceOverride)
        {
            return;
        }
        Debug.Log("IBO->Interacted with object of type: " + interactableType + " with interactText: " + interactText);
        // default?
        //SetBillboardVisibility(false);

        onInteract.Invoke();
        interactionCount++;

        if (isOneTimeUse)
        {
            isInteractable = false;
        }
    }
    public override void InteractExit()
    {
        if (!isInteractable)
        {
            return;
        }
        onInteractExit.Invoke();
        interactionExitCount++;
    }
    public void AddInteractListener(UnityEngine.Events.UnityAction action)
    {
        onInteract.AddListener(action);
    }
    public void RemoveInteractListener(UnityEngine.Events.UnityAction action)
    {
        onInteract.RemoveListener(action);
    }

}
