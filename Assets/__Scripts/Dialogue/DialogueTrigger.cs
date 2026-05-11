using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Visual Cue")]
    [SerializeField] private GameObject visualCue;

    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    private bool playerInRange;
    private InputSystem_Actions inputActions;
    private DialogueManager dialogueManager;

    private void Awake()
    {
        playerInRange = false;
        visualCue.SetActive(false);
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
        TryBindDialogueManager();
        UpdateVisualCue();
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Interact.started -= OnInteractPerformed;
            inputActions.Disable();
        }

        if (dialogueManager != null)
        {
            dialogueManager.DialogueStateChanged -= OnDialogueStateChanged;
            dialogueManager = null;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }
        if (!playerInRange)
        {
            return;
        }

        if (!TryBindDialogueManager())
        {
            return;
        }

        if (!dialogueManager.dialogueIsPlaying)
        {
            dialogueManager.EnterDialogueMode(inkJSON, true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerInRange = true;
            UpdateVisualCue();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
            UpdateVisualCue();
        }
    }

    private bool TryBindDialogueManager()
    {
        DialogueManager currentManager = DialogueManager.GetInstance();
        if (currentManager == null)
        {
            return false;
        }

        if (dialogueManager == currentManager)
        {
            return true;
        }

        if (dialogueManager != null)
        {
            dialogueManager.DialogueStateChanged -= OnDialogueStateChanged;
        }

        dialogueManager = currentManager;
        dialogueManager.DialogueStateChanged += OnDialogueStateChanged;
        return true;
    }

    private void OnDialogueStateChanged(bool isPlaying)
    {
        UpdateVisualCue();
    }

    private void UpdateVisualCue()
    {
        bool shouldShowCue = false;
        if (TryBindDialogueManager())
        {
            shouldShowCue = playerInRange && !dialogueManager.dialogueIsPlaying;
        }

        visualCue.SetActive(shouldShowCue);
    }
}
