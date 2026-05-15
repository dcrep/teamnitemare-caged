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
    [SerializeField] private float crouchMoveSpeed = 2.5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float lookSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Heights")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standHeight = 1.8f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip[] landSounds;
    [SerializeField] private AudioClip[] crouchSounds;
    [SerializeField] private AudioClip[] standSounds;
    [SerializeField] private AudioClip runningSoundClip;
    [SerializeField] private float runningSoundVolume = 1.8f;
    [SerializeField] private float walkSoundDistance = 1.8f;
    [SerializeField] private float walkSoundVolume = 0.7f;
    [SerializeField] private AudioSource walkSoundSource;
    // separate audio source for running sound to allow it to loop while walking sounds play as one-shots
    [SerializeField] private AudioSource runningSoundSource;

    private CharacterController characterController;
    private bool isRunning;
    private bool isCrouching;
    private bool justEndedCrouchGrounded = false;
    private bool wasGroundedLastFrame = true;

    private float verticalVelocity;
    private float rotationX;
    private float rotationY;
    private float coyoteTimer;
    private float walkSoundDistanceAccumulator;
    private int walkSoundIndex;
    private int jumpSoundIndex;
    private int landSoundIndex;
    private int crouchSoundIndex;
    private int standSoundIndex;
    private bool runningSoundIsPlaying;
    private bool runningSoundWaitingForAirborneState;
    private bool sprintHeld;
    private bool hasMoveInput;
    private Vector3 standingCameraLocalPosition;
    private Vector3 standingControllerCenter;
    private Vector3 crouchingCameraLocalPosition;
    private Vector3 crouchingControllerCenter;

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

        // set characterController height to standing height at start
        var originalHeight = characterController.height;
        var differenceInHeight = standHeight - originalHeight;
        characterController.height = standHeight;
        // adjust camera position to match new height
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition += new Vector3(0f, differenceInHeight, 0f);
        }

        standingControllerCenter = characterController.center;
        crouchingControllerCenter = standingControllerCenter + Vector3.down * ((standHeight - crouchHeight) * 0.5f);
        standingCameraLocalPosition = playerCamera != null ? playerCamera.transform.localPosition : Vector3.zero;
        crouchingCameraLocalPosition = standingCameraLocalPosition + Vector3.down * (standHeight - crouchHeight);

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
        // audio mixer -> AudioManager.sfxMixerGroup
        walkSoundSource.outputAudioMixerGroup = AudioManager.sfxMixerGroup;

        if (runningSoundSource == null)
        {
            runningSoundSource = gameObject.AddComponent<AudioSource>();
        }

        runningSoundSource.playOnAwake = false;
        runningSoundSource.loop = true;
        runningSoundSource.spatialBlend = 0f;
        runningSoundSource.outputAudioMixerGroup = AudioManager.sfxMixerGroup;
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
        playerControls.Player.Crouch.performed += CrouchActionPerformed;
        GameManager.PauseStateChange += OnPauseStateChanged;
    }

    void OnDisable()
    {
        playerControls.Player.Crouch.performed -= CrouchActionPerformed;
        playerControls.Player.Jump.performed -= JumpActionPerformed;
        playerControls.Player.Attack.performed -= AttackActionPerformed;
        playerControls.Player.Interact.performed -= InteractActionPerformed;
        playerControls.Player.Sprint.performed -= SprintActionPerformed;
        playerControls.Player.Sprint.canceled -= SprintActionCanceled;
        GameManager.PauseStateChange -= OnPauseStateChanged;

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
        //StartCoroutine(SnapDownOnSpawn());
    }

    // System.Collections.IEnumerator SnapDownOnSpawn()
    // {
    //     yield return null;

    //     if (characterController == null)
    //     {
    //         yield break;
    //     }

    //     if (!characterController.isGrounded)
    //     {
    //         float maxDistance = standHeight + 0.5f;
    //         if (SnapDownToGround(maxDistance))
    //         {
    //             characterController.Move(Vector3.zero);
    //             verticalVelocity = -2f;
    //         }
    //     }
    // }

    void Update()
    {

        if (!GameManager.Instance.AreLookControlsDisabled())
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            HandleLook(lookInput, lookAction.activeControl?.device is Mouse);
        }
        if (!GameManager.Instance.AreMoveControlsDisabled())
        {
            // Capture isGrounded BEFORE HandleMovement (which calls Move())
            // This is the result from last frame's Move(), so it's reliable
            wasGroundedLastFrame = characterController.isGrounded;
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            HandleMovement(moveInput);
        }
        else
        {
            // if move controls are disabled, make sure to stop movement and running sound immediately
            isRunning = false;
            StopRunningSound();
        }
    }

    void OnResumeFromPause()
    {
        if (characterController == null)
            return;

        Physics.SyncTransforms();
        StartCoroutine(VerifyGroundingAfterResume());
    }

    System.Collections.IEnumerator VerifyGroundingAfterResume()
    {
        yield return null; // Wait one frame for isGrounded to update
        
        if (!characterController.isGrounded)
        {
            Debug.Log("Still airborne after resume, snapping down");
            float maxDistance = standHeight + 0.5f;
            if (SnapDownToGround(maxDistance))
            {
                characterController.Move(Vector3.zero);
            }
        }
    }
    void OnPauseStateChanged(bool isPaused)
    {
        if (!isPaused)
        {
            OnResumeFromPause();
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
        // Use grounding state from PREVIOUS frame (checked before last frame's Move())
        bool wasGroundedThisFrame = wasGroundedLastFrame;
        hasMoveInput = moveInput.sqrMagnitude > 0.0001f;

        // Running is only active while sprint is held, grounded, the player is actively moving, and not crouching.
        isRunning = sprintHeld && wasGroundedThisFrame && hasMoveInput && !isCrouching;

        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        float activeMoveSpeed = isRunning ? runSpeed : (isCrouching ? crouchMoveSpeed : moveSpeed);
        Vector3 horizontalMove = move * activeMoveSpeed * Time.deltaTime;

        UpdateWalkSound(horizontalMove, wasGroundedThisFrame);

        // Always apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        // When grounded, cap downward velocity to maintain ground contact
        if (wasGroundedThisFrame)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, -2f);
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // Combine horizontal and vertical movement into one Move call
        Vector3 totalMove = horizontalMove + Vector3.up * verticalVelocity * Time.deltaTime;
        characterController.Move(totalMove);

        // Do NOT check isGrounded here. It won't be updated until next frame.
        // Transitions will be detected NEXT frame when wasGroundedLastFrame is captured.
        
        // If we just ended a crouch while grounded, try to snap down to ground if CharacterController reports ungrounded.
        if (justEndedCrouchGrounded)
        {
            // Use delayed state for this check
            if (wasGroundedThisFrame)
            {
                float maxDiff = Mathf.Max(0f, standHeight - crouchHeight) + 0.1f;
                if (SnapDownToGround(maxDiff))
                {
                    // Force CharacterController to update collision state immediately
                    characterController.Move(Vector3.zero);
                }
            }
            justEndedCrouchGrounded = false;
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
        // Reset crouch instead of jumping
        if (isCrouching)
        {
            EndCrouch();
            return;
        }

        if (characterController.isGrounded || coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimer = 0f;
            isRunning = false;
            PlayRandomClip(jumpSounds, ref jumpSoundIndex, walkSoundVolume);
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

        sprintHeld = true;
    }

    void SprintActionCanceled(InputAction.CallbackContext context)
    {
        // special-case this because the toggle can happen while controls are disabled
        //if (GameManager.Instance.AreMoveControlsDisabled())
        sprintHeld = false;
        isRunning = false;
        StopRunningSound();
        runningSoundWaitingForAirborneState = false;
    }

    void CrouchActionPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.AreMoveControlsDisabled())
        {
            return;
        }

        if (isCrouching)
        {
            EndCrouch();
        }
        else
        {
            StartCrouch();
        }
    }

    void StartCrouch()
    {
        if (characterController == null)
        {
            return;
        }

        //float bottomWorldY = characterController.bounds.min.y;

        isCrouching = true;
        characterController.height = crouchHeight;
        characterController.center = crouchingControllerCenter;

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = crouchingCameraLocalPosition;
        }

        PlayRandomClip(crouchSounds, ref crouchSoundIndex, 1f);
        isRunning = false;
        StopRunningSound();
    }

    void EndCrouch()
    {
        if (characterController == null)
        {
            return;
        }

        float crouchedBottomWorldY = characterController.bounds.min.y;
        float targetCenterY = crouchedBottomWorldY - transform.position.y + standHeight * 0.5f;

        // Compute how much extra vertical space (headroom) we'd need to stand up,
        // then scan only that distance above the current crouched top.
        float crouchTopWorldY = characterController.bounds.max.y;
        float standingTopWorldY = transform.position.y + targetCenterY + standHeight * 0.5f;
        float scanDistance = standingTopWorldY - crouchTopWorldY;
        if (scanDistance > 0f && ScanAboveHead(scanDistance))
        {
            return;
        }

        isCrouching = false;
        characterController.height = standHeight;
        characterController.center = new Vector3(standingControllerCenter.x, targetCenterY, standingControllerCenter.z);

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = standingCameraLocalPosition;
        }

        PlayRandomClip(standSounds, ref standSoundIndex, 1f);

        if (characterController.isGrounded)
        {
            verticalVelocity = Mathf.Min(verticalVelocity, -2f);
            // mark that we just ended crouch while grounded to avoid a transient ungrounded frame
            justEndedCrouchGrounded = true;
        }
    }

    // Scans `distance` units above the current crouched top for any blocking colliders.
    bool ScanAboveHead(float distance)
    {
        float crouchTopWorldY = characterController.bounds.max.y;
        Vector3 worldBottom = new Vector3(transform.position.x, crouchTopWorldY, transform.position.z);
        Vector3 worldTop = worldBottom + Vector3.up * distance;
        Collider[] overlaps = Physics.OverlapCapsule(worldBottom, worldTop, characterController.radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (overlaps[i] == null)
            {
                continue;
            }

            if (overlaps[i].transform == transform)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    // Raycast down from the current capsule top up to `maxDistance`. If ground found,
    // move the `transform.position` so the CharacterController bottom sits on the hit point.
    bool SnapDownToGround(float maxDistance)
    {
        Debug.Log($"[PlayerControllerDRM] SnapDownToGround maxDistance={maxDistance}");
        float topWorldY = characterController.bounds.max.y;
        Vector3 rayOrigin = new Vector3(transform.position.x, topWorldY + 0.05f, transform.position.z);
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxDistance + 0.05f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null)
            {
                return false;
            }

            // ignore hits against self
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                return false;
            }

            float bottomWorldY = characterController.bounds.min.y;
            float delta = hit.point.y - bottomWorldY;
            if (Mathf.Abs(delta) > 0.0001f)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y + delta, transform.position.z);
                Physics.SyncTransforms();
                return true;
            }
        }

        return false;
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

        bool shouldBePlaying = isRunning && hasMoveInput && characterController != null && characterController.isGrounded;

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

    void PlayRandomClip(AudioClip[] clips, ref int clipIndex, float volume = 1f)
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
                walkSoundSource.PlayOneShot(clip, volume);
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
