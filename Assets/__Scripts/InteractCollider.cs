using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class InteractCollider : MonoBehaviour
{
    // delegate + event to notify parent InteractableObject of trigger enter
    public delegate void PlayerEnterTriggerHandler(InteractableBase interactable);
    public event PlayerEnterTriggerHandler OnPlayerHitInteractable;
    public event PlayerEnterTriggerHandler OnPlayerLeaveInteractable;

    public delegate void PlayerGrappleTriggerHandler(GrappleFromPoint grappleFromPoint);
    public event PlayerGrappleTriggerHandler OnPlayerHitGrappleFromPoint;
    public event PlayerGrappleTriggerHandler OnPlayerLeaveGrappleFromPoint;

    InteractableBase interactable = null;
    GrappleFromPoint grappleFromPoint = null;

    [SerializeField] private TMP_Text interactText;

    void Awake()
    {
        if (interactText == null)
        {
            interactText = GetComponentInChildren<TMP_Text>(true);            
        }
        SetInteractText("");
    }

    public void SetInteractText(string text)
    {
        if (interactText != null)
        {
            interactText.text = text;
        }
    }

    void FixedUpdate()
    {
        if (interactable != null)
        {
            // if object is disabled or non-interactable, treat as if player left the trigger
            if (!interactable.gameObject.activeInHierarchy || !interactable.isInteractable)
            {
                //Debug.Log($"InteractCollider treating {interactableObject.gameObject.name} as left trigger because it is no longer active or interactable");
                OnPlayerLeaveInteractable?.Invoke(interactable);
                interactable = null;
                SetInteractText("");
            }
        }

        if (grappleFromPoint != null)
        {
            if (!grappleFromPoint.gameObject.activeInHierarchy)
            {
                OnPlayerLeaveGrappleFromPoint?.Invoke(grappleFromPoint);
                grappleFromPoint = null;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"InteractCollider entered trigger: {other.gameObject.name}");
        if (other.TryGetComponent(out InteractableBase interactableLocal))
        {
            if (!interactableLocal.CanInteract())
            {
                //Debug.Log($"InteractCollider found InteractableBase but it cannot interact: {interactable.gameObject.name}");
                return;
            }
            interactable = interactableLocal;
            OnPlayerHitInteractable?.Invoke(interactable);
            Debug.Log($"InteractCollider found Interactable: {interactable.gameObject.name}");
            Debug.Log($"Interactable text: {interactable.interactText}");
            interactable.SetBillboardText(interactable.interactText);
            interactable.SetBillboardVisibility(true);
        }

        if (other.CompareTag("GrappleFromPoint") && other.TryGetComponent(out GrappleFromPoint grappleFromPointLocal))
        {
            grappleFromPoint = grappleFromPointLocal;
            OnPlayerHitGrappleFromPoint?.Invoke(grappleFromPoint);
            grappleFromPoint.SetSourceActive(true);
            Debug.Log($"InteractCollider found GrappleFromPoint: {grappleFromPoint.gameObject.name}");
        }
    }
    void OnTriggerExit(Collider other)
    {
        //Debug.Log($"InteractCollider exited trigger: {other.gameObject.name}");
        if (interactable != null)
        {
            OnPlayerLeaveInteractable?.Invoke(interactable);
            interactable.SetBillboardVisibility(false);
            interactable = null;
        }

        if (grappleFromPoint != null && other.TryGetComponent(out GrappleFromPoint exitedGrappleFromPoint) && exitedGrappleFromPoint == grappleFromPoint)
        {
            OnPlayerLeaveGrappleFromPoint?.Invoke(grappleFromPoint);
            grappleFromPoint.SetSourceActive(false);
            grappleFromPoint = null;
        }
    }
}
