using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerDRM : MonoBehaviour
{
    private InputSystem_Actions playerControls;
    private InputAction moveAction;
    private InputAction lookAction;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float lookSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip[] landSounds;
    [SerializeField] private AudioClip runningSoundClip;
    [SerializeField] private float runningSoundVolume = 1.8f;
    [SerializeField] private float walkSoundDistance = 1.8f;
    [SerializeField] private float walkSoundVolume = 0.7f;
    [SerializeField] private AudioSource walkSoundSource;
    // separate audio source for running sound to allow it to loop while walking sounds play as one-shots
    [SerializeField] private AudioSource runningSoundSource;

    private CharacterController characterController;
    private bool isRunning;
    private float verticalVelocity;
    private float rotationX;
    private float rotationY;
    private float coyoteTimer;
    private float walkSoundDistanceAccumulator;
    private int walkSoundIndex;
    private int jumpSoundIndex;
    private int landSoundIndex;
    private bool runningSoundIsPlaying;
    private bool runningSoundWaitingForAirborneState;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerControls = new InputSystem_Actions();

        rotationY = transform.eulerAngles.y;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera != null)
        {
            rotationX = NormalizeAngle(playerCamera.transform.localEulerAngles.x);
        }

        if (walkSoundSource == null)
        {
            walkSoundSource = GetComponent<AudioSource>();
            if (walkSoundSource == null)
            {
                walkSoundSource = gameObject.AddComponent<AudioSource>();
            }
        }

        walkSoundSource.playOnAwake = false;
        walkSoundSource.loop = false;
        walkSoundSource.spatialBlend = 0f;

        if (runningSoundSource == null)
        {
            runningSoundSource = gameObject.AddComponent<AudioSource>();
        }

        runningSoundSource.playOnAwake = false;
        runningSoundSource.loop = true;
        runningSoundSource.spatialBlend = 0f;
        runningSoundSource.clip = runningSoundClip;
        runningSoundSource.volume = runningSoundVolume;
    }

    void OnEnable()
    {
        playerControls.Enable();

        moveAction = playerControls.Player.Move;
        lookAction = playerControls.Player.Look;

        playerControls.Player.Jump.performed += JumpActionPerformed;
        playerControls.Player.Attack.performed += AttackActionPerformed;
        playerControls.Player.Interact.performed += InteractActionPerformed;
        playerControls.Player.Sprint.performed += SprintActionPerformed;
        playerControls.Player.Sprint.canceled += SprintActionCanceled;
    }

    void OnDisable()
    {
        playerControls.Player.Jump.performed -= JumpActionPerformed;
        playerControls.Player.Attack.performed -= AttackActionPerformed;
        playerControls.Player.Interact.performed -= InteractActionPerformed;
        playerControls.Player.Sprint.performed -= SprintActionPerformed;
        playerControls.Player.Sprint.canceled -= SprintActionCanceled;

        if (playerControls != null)
        {
            playerControls.Disable();
        }

        StopRunningSound();
    }

    void Start()
    {
        GameManager.Instance.MouseCursorSetForGame();
        coyoteTimer = coyoteTime;
    }

    void Update()
    {
        if (!GameManager.Instance.AreLookControlsDisabled())
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            HandleLook(lookInput, lookAction.activeControl?.device is Mouse);
        }
        if (!GameManager.Instance.AreMoveControlsDisabled())
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            HandleMovement(moveInput);
        }
    }

    void HandleLook(Vector2 lookVector, bool isMouse)
    {
        // only multiply by deltaTime if using mouse input for smoother rotation, not for gamepad etc
        // smooth rotation using lerp?
        
        float deltaTime = isMouse ? Time.deltaTime : 1f;
        rotationY += lookVector.x * lookSpeed * deltaTime;
        rotationX -= lookVector.y * lookSpeed * deltaTime;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }

    void HandleMovement(Vector2 moveInput)
    {
        bool wasGroundedThisFrame = characterController.isGrounded;

        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        float activeMoveSpeed = isRunning ? runSpeed : moveSpeed;
        Vector3 horizontalMove = move * activeMoveSpeed * Time.deltaTime;
        characterController.Move(horizontalMove);

        UpdateWalkSound(horizontalMove, wasGroundedThisFrame);

        if (wasGroundedThisFrame && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (wasGroundedThisFrame)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        bool isGroundedAfterMove = characterController.isGrounded;
        if (!wasGroundedThisFrame && isGroundedAfterMove)
        {
            PlayRandomClip(landSounds, ref landSoundIndex);
        }

        UpdateRunningSound();
    }

    void JumpActionPerformed(InputAction.CallbackContext context)
    {
        if (characterController == null)
        {
            return;
        }
        if (GameManager.Instance.AreMoveControlsDisabled())
        {
            return;
        }

        if (characterController.isGrounded || coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimer = 0f;
            PlayRandomClip(jumpSounds, ref jumpSoundIndex);
            StopRunningSound();
            runningSoundWaitingForAirborneState = true;
        }
    }

    void AttackActionPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.AreControlsDisabled(DisabledControls.Attack))
        {
            return;
        }
        Debug.Log("Attack button pressed");
    }

    void InteractActionPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.AreControlsDisabled(DisabledControls.Interact))
        {
            return;
        }
        Debug.Log("Interact button pressed");
    }

    void SprintActionPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.AreMoveControlsDisabled())
        {
            return;
        }

        isRunning = true;

        if (isRunning && characterController != null && characterController.isGrounded && !runningSoundWaitingForAirborneState)
        {
            StartRunningSound();
        }
    }

    void SprintActionCanceled(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.AreMoveControlsDisabled())
        {
            return;
        }

        isRunning = false;
        StopRunningSound();
        runningSoundWaitingForAirborneState = false;
    }

    void UpdateRunningSound()
    {
        if (runningSoundSource == null || runningSoundClip == null)
        {
            runningSoundIsPlaying = false;
            runningSoundWaitingForAirborneState = false;
            return;
        }

        if (runningSoundWaitingForAirborneState)
        {
            if (characterController != null && !characterController.isGrounded)
            {
                runningSoundWaitingForAirborneState = false;
            }

            StopRunningSound();
            return;
        }

        bool shouldBePlaying = isRunning && characterController != null && characterController.isGrounded;

        if (!shouldBePlaying)
        {
            StopRunningSound();
            return;
        }

        if (!runningSoundIsPlaying)
        {
            StartRunningSound();
        }
    }

    void StartRunningSound()
    {
        if (runningSoundSource == null || runningSoundClip == null || runningSoundIsPlaying)
        {
            return;
        }

        runningSoundSource.clip = runningSoundClip;
        runningSoundSource.Play();
        runningSoundIsPlaying = true;
    }

    void StopRunningSound()
    {
        if (runningSoundSource != null && runningSoundSource.isPlaying)
        {
            runningSoundSource.Stop();
        }

        runningSoundIsPlaying = false;
    }

    void PlayRandomClip(AudioClip[] clips, ref int clipIndex)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        int startIndex = Random.Range(0, clips.Length);
        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[(startIndex + i) % clips.Length];
            if (clip == null)
            {
                continue;
            }

            clipIndex++;
            if (walkSoundSource != null)
            {
                walkSoundSource.PlayOneShot(clip, walkSoundVolume);
            }

            return;
        }
    }

    void UpdateWalkSound(Vector3 horizontalMove, bool wasGroundedThisFrame)
    {
        if (isRunning)
        {
            walkSoundDistanceAccumulator = 0f;
            return;
        }

        if (!wasGroundedThisFrame || walkSounds == null || walkSounds.Length == 0)
        {
            if (!wasGroundedThisFrame)
            {
                walkSoundDistanceAccumulator = 0f;
            }

            return;
        }

        float moveDistance = horizontalMove.magnitude;
        if (moveDistance <= 0.001f)
        {
            walkSoundDistanceAccumulator = 0f;
            return;
        }

        walkSoundDistanceAccumulator += moveDistance;

        float stepDistance = Mathf.Max(0.05f, walkSoundDistance);
        while (walkSoundDistanceAccumulator >= stepDistance)
        {
            walkSoundDistanceAccumulator -= stepDistance;
            AudioClip walkSound = walkSounds[walkSoundIndex % walkSounds.Length];
            walkSoundIndex++;

            if (walkSound == null)
            {
                continue;
            }

            if (walkSoundSource != null)
            {
                walkSoundSource.PlayOneShot(walkSound, walkSoundVolume);
            }
        }
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered trigger with " + other.name);
    }
    void OnTriggerExit(Collider other)
    {
        Debug.Log("Exited trigger with " + other.name);
    }

}
