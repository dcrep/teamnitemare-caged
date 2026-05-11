using UnityEngine;
using UnityEngine.InputSystem;

public class NpcFollow : MonoBehaviour
{
    private bool playerInRange;
    private bool hasInteracted;

    [SerializeField] private GameObject prompt;
    [SerializeField] private Transform target;
    [SerializeField] private GameObject visualCue;
    [SerializeField] private Animator animator;
    Follow com;
    NPCFlee com1;

    private InputSystem_Actions playerControls;

    private void Awake()
    {
        playerControls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        playerControls.Enable();
        playerControls.Player.Interact.started += OnInteractPerformed;
    }

    private void OnDisable()
    {
        playerControls.Player.Interact.started -= OnInteractPerformed;
        playerControls.Disable();
    }

    private void Start()
    {
        com = GetComponent<Follow>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
        transform.LookAt(target);

    }

    void Update()
    {
        if (playerInRange)
        {
            transform.LookAt(target);
        }
        else
        {
            
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (!playerInRange || hasInteracted)
        {
            return;
        }

        hasInteracted = true;

        if (prompt != null)
        {
            prompt.SetActive(false);
        }

        if (visualCue != null)
        {
            visualCue.SetActive(false);
        }

        if (com != null)
        {
            com.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (animator != null)
            {
                animator.SetBool("Bounce", true);
            }

            if (visualCue != null)
            {
                visualCue.SetActive(true);
            }

            if (prompt != null && !hasInteracted)
            {
                prompt.SetActive(true);
            }

            playerInRange = true;
            com1.enabled = false;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (prompt != null)
        {
            prompt.SetActive(false);
        }

        if (visualCue != null && !hasInteracted)
        {
            visualCue.SetActive(false);
        }

        if (animator != null)
        {
            animator.SetBool("Bounce", false);
        }

        if (other.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
