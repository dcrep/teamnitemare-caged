using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerBSK : MonoBehaviour
{
    private InputSystem_Actions playerControls;
    private InputAction moveAction;
    private InputAction lookAction;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 0.8f, 0f);
    [SerializeField] CameraSwitchTimed cameraSwitchTimed;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 3f; //1.4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Feather Boost")]
    [SerializeField] Light featherObtainedLight = null;
    [SerializeField] private float featherJumpHeightMultiplier = 5;
    [SerializeField] private float featherFallGravityMultiplier = 0.55f;
    [SerializeField] private float featherAirBoostStrength = 15f;
    [SerializeField] private float featherTimeBetweenBoosts = 1.5f;
    [SerializeField] private GameObject wings;
    [SerializeField] WingAnimationControl wingAnimationControl;
    //[SerializeField] private AlphaControllerForAnimationRenderer wingsAlphaControl;

    [Header("Fracture Anti-Fall")]
    [SerializeField] private float fractureAntiFallTriggerY = -10f;
    [SerializeField] private float fractureAntiFallBoostStrength = 15f;

    [Header("Look")]
    [SerializeField] private float rotateSpeed = 10f;
    //[SerializeField] private float maxLookAngle = 85f;

    [Header("Grapple")]
    [SerializeField] private float grappleHighlightCheckDistance = 30f;
    [SerializeField] private float grappleScreenCenterToleranceX = 0.22f;
    [SerializeField] private float grappleScreenCenterToleranceY = 0.34f;
    [SerializeField] private LayerMask grappleHighlightMask = ~0;
    [SerializeField] private float grapplePointCacheRefreshInterval = 1f;
    [SerializeField] private GameObject grappleChainLinkPrefab;
    [SerializeField] private float grappleMoveSpeed = 6f;
    [SerializeField] private float grappleRotateSpeed = 120f;
    [SerializeField] private float grappleCompletionDistance = 0.25f;
    [SerializeField] private float grappleChainCompletionDistance = 0.35f;

    [Header("Grapple Line")]
    [SerializeField] private bool useGrappleLineRenderer = false;
    [SerializeField] private float grappleLineWidth = 0.04f;
    [SerializeField] private LineRenderer grappleLineRenderer;

    [Header("Audio")]
    [SerializeField] private AudioClip featherJumpSound;
    [SerializeField] private AudioClip grappleLaunchSound;
    [SerializeField] private float grappleLaunchVolume = 0.7f;
    [SerializeField] private AudioSource grappleLaunchSource;
    [SerializeField] private AudioClip grapplingPullSound;
    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private float walkSoundDistance = 1.8f;
    [SerializeField] private float walkSoundVolume = 0.7f;
    [SerializeField] private float grapplePullVolume = 0.4f;
    [SerializeField] private AudioSource walkSoundSource;
    private AudioSource grapplePullSource;


    Vector3[] fragmentDestinations = new Vector3[]
    {
        new Vector3(4.05f, 17.66f, -46.6f),  // Fragment1 "Frag1"
        new Vector3(4.71f, 16.09f, -51.82f),    // Fragment2 "Frag2"
        new Vector3(-3.58f, 17.65f, -53.38f),   // Fragment3 "Frag3"
        new Vector3(-4.86f, 17.7f, -44.82f),  // Fragment4 "Frag4"
        new Vector3(-6.08f, 17.03f, -52.87f)     // Fragment5 "Frag5"
    };

    [SerializeField] TMP_Text helpText;

    [SerializeField] private GameObject audioCurvePrefab;

    List<GameObject> activeAudioCurves = new List<GameObject>();
    //List<ChirpAttract> activeAttractions = new List<ChirpAttract>();

    private CharacterController characterController;
    private float verticalVelocity;
    private float cameraPitch;

    private float rotationX;
    private float rotationY;
    private float coyoteTimer;
    private float walkSoundDistanceAccumulator;
    private float featherBoostCooldownTimer;
    private int walkSoundIndex;
    private bool fractureAntiFallTriggered;
    private bool isFractureScene;
    private bool isGrappleMoveLocked;
    private Transform grappleLockedParent;
    private Transform grapplePlayerOriginalParent;
    private Transform grappleDestinationAnchor;
    private Vector3 grappleMoveDestinationPosition;
    private Animator grappleLockedAnimator;
    private Light[] grappleLockedLights;
    private QuestComponent grappleLockedQuestComponent;
    private GrappleChainLink activeGrappleChainLink;
    private GameObject activeGrappleChainLinkObject;
    private Transform activeGrappleLineSource;
    private Transform activeGrappleLineTarget;
    private GameObject grappleCompletedTargetObject;

    private InteractCollider interactCollider;

    bool lookingAtInteractable = false;
    InteractableBase currentInteractable = null;
    GrappleFromPoint currentGrappleFromPoint = null;
    GrapplePointGlow highlightedGrapplePoint = null;
    readonly List<GrapplePointGlow> cachedGrapplePoints = new List<GrapplePointGlow>();
    float nextGrapplePointCacheRefreshTime;

    [SerializeField] private bool hasFeathers = false;
    int feathersCollected = 0;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerControls = new InputSystem_Actions();
        UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
        isFractureScene = !string.IsNullOrEmpty(activeScene.name) && activeScene.name.IndexOf("Fracture", System.StringComparison.OrdinalIgnoreCase) >= 0;

        rotationX = 0f;
        rotationY = transform.eulerAngles.y;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerControllerBSK could not find a Camera. Assign one in the inspector or add a child Camera.");
        }
        
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(transform);
            playerCamera.transform.localPosition = cameraLocalPosition;
            playerCamera.transform.localRotation = Quaternion.identity;
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
        // Grapple pull audio source (looped while grappling move is active)
        if (grapplePullSource == null)
        {
            grapplePullSource = GetComponent<AudioSource>();
            // If GetComponent returned the same source as walkSoundSource, add a dedicated one
            if (grapplePullSource == walkSoundSource)
            {
                grapplePullSource = null;
            }
            if (grapplePullSource == null)
            {
                grapplePullSource = gameObject.AddComponent<AudioSource>();
            }
        }
        grapplePullSource.playOnAwake = false;
        grapplePullSource.loop = true;
        grapplePullSource.spatialBlend = 0f;    // 0 = 2D sound, not affected by 3D position
        // use adjustable inspector value for volume so launch sound isn't drowned out
        grapplePullSource.volume = Mathf.Clamp01(grapplePullVolume);
        // Ensure any assigned launch source is non-spatial (2D) so it isn't affected by position
        if (grappleLaunchSource != null)
        {
            grappleLaunchSource.spatialBlend = 0f;
        }
        
        if (wingAnimationControl == null)
        {
            wingAnimationControl = GetComponentInChildren<WingAnimationControl>();
            if (wingAnimationControl == null)
                Debug.LogWarning("PlayerControllerBSK could not find a WingAnimationControl in children. Feather boost animations will not work.");
        }
        if (wings == null)
        {
            wings = GameObject.Find("AngelWings");
            if (wings == null)
            {
                Debug.LogWarning("PlayerControllerBSK could not find a GameObject named 'AngelWings' in the scene. Assign the wings GameObject in the inspector for feather boost visuals.");
            }
        }
        if (helpText != null)
        {
            Canvas canvas = helpText.GetComponentInParent<Canvas>();
            Debug.Log("PPU:" + canvas.referencePixelsPerUnit);
        }

        if (grappleLineRenderer == null && useGrappleLineRenderer)
        {
            grappleLineRenderer = GetComponent<LineRenderer>();
            if (grappleLineRenderer == null)
            {
                grappleLineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        if (grappleLineRenderer != null)
        {
            ConfigureGrappleLineRenderer();
            grappleLineRenderer.positionCount = 2;
            grappleLineRenderer.enabled = false;
        }

        fractureAntiFallTriggered = false;
        if (!isFractureScene)
        {
            // Instantiate 5 audio curves
            for (int i = 0; i < 5; i++)
            {
                GameObject curve = Instantiate(audioCurvePrefab);
                activeAudioCurves.Add(curve);
                // keep them hidden until needed
                curve.SetActive(false);
            }
        }

        RefreshCachedGrapplePoints();
    }

    private IEnumerator CheckFeatherState()
    {
        while (true)
        {
            // This is a simple way to toggle feather state for testing. Replace with actual game logic as needed.
            if (hasFeathers)
            {
                if (wings.activeInHierarchy == false)
                {
                    wings.SetActive(true);
                    wingAnimationControl.ResetToIdle();
                    Debug.Log("Feathers!");
                }
            }
            else
            {
                if (wings.activeInHierarchy == true)
                {
                    wings.SetActive(false);
                    //wingAnimationControl.ResetToIdle();
                    Debug.Log("No Feathers!");
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    public void FeatherLightObtained()
    {
        if (featherObtainedLight != null)
        {
            featherObtainedLight.enabled = true;
            feathersCollected++;
            if (feathersCollected > 2)
            {
                feathersCollected = 3;
                FeathersObtained();
                return;
            }
            featherObtainedLight.intensity = 1f + (feathersCollected * 0.5f);
        }
    }

    public void FeathersObtained()
    {
        if (featherObtainedLight != null)
        {
            featherObtainedLight.enabled = false;
            //featherObtainedLight.intensity = 1f;
        }
        hasFeathers = true;
        wings.SetActive(true);
        wingAnimationControl.ResetToIdle();
        if (helpText != null)
        {
            helpText.text = "You got all feathers! Press Jump while in the air to boost yourself upwards.";            
            Invoke(nameof(ClearHelpText), 5f);
        }
    }
    public void FeathersLost()
    {
        hasFeathers = false;
        wings.SetActive(false);
        // if (helpText != null)
        // {
        //     helpText.text = "You lost your feathers! You can no longer boost yourself while in the air.";
        //     Invoke(nameof(ClearHelpText), 5f);
        // }
    }
    void ClearHelpText()
    {
        if (helpText != null)
        {
            helpText.text = "";
        }
    }

    void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
        lookAction = playerControls.Player.Look;
        playerControls.Player.Jump.performed += JumpActionPerformed;
        playerControls.Player.Attack.performed += AttackAction;
        playerControls.Player.Interact.performed += InteractAction;

        // get child InteractArea's InteractCollider and subscribe to its event
        InteractCollider[] interactColliders = GetComponentsInChildren<InteractCollider>();
        if (interactColliders.Length > 0)
        {
            interactCollider = interactColliders[0];
            interactCollider.OnPlayerHitInteractable += InteractTrigger;
            interactCollider.OnPlayerLeaveInteractable += InteractLeaveTrigger;
            interactCollider.OnPlayerHitGrappleFromPoint += GrappleTrigger;
            interactCollider.OnPlayerLeaveGrappleFromPoint += GrappleLeaveTrigger;
        }
        else
        {
            Debug.LogWarning("PlayerControllerBSK could not find a InteractCollider for interaction events.");
        }
    }
    void OnDisable()
    {
        playerControls.Player.Jump.performed -= JumpActionPerformed;
        playerControls.Player.Attack.performed -= AttackAction;
        playerControls.Player.Interact.performed -= InteractAction;

        if (playerControls != null)
        {
            playerControls.Disable();
        }

        // unsubscribe from interactCollider events
        if (interactCollider != null)
        {
            interactCollider.OnPlayerHitInteractable -= InteractTrigger;
            interactCollider.OnPlayerLeaveInteractable -= InteractLeaveTrigger;
            interactCollider.OnPlayerHitGrappleFromPoint -= GrappleTrigger;
            interactCollider.OnPlayerLeaveGrappleFromPoint -= GrappleLeaveTrigger;
        }

        if (currentGrappleFromPoint != null)
        {
            currentGrappleFromPoint.SetSourceActive(false);
            currentGrappleFromPoint = null;
        }

        EndGrappleMoveLock();

        SetHighlightedGrapplePoint(null);
    }
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize coyote timer so player can jump even if not grounded at spawn.
        coyoteTimer = coyoteTime;
        StartCoroutine(nameof(CheckFeatherState), 1f);
    }

    void Update()
    {
        if (!GameManager.Instance.AreLookControlsDisabled())
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            HandleLook(lookInput, lookAction.activeControl?.device is Mouse);
        }
        if (isGrappleMoveLocked)
        {
            //HandleLook(lookInput);
            UpdateGrappleMoveLock();
            return;
        }
        //HandleLook(lookInput);
        UpdateGrappleHighlight();
        if (!GameManager.Instance.AreMoveControlsDisabled())
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            HandleMovement(moveInput);
        }
    }

    void JumpActionPerformed(InputAction.CallbackContext context)
    {
        if (isGrappleMoveLocked || characterController == null)
        {
            return;
        }
        if (GameManager.Instance.AreMoveControlsDisabled())
        {
            return;
        }

        float downwardGravity = -Mathf.Abs(gravity);
        bool wasGroundedThisFrame = characterController.isGrounded;
        bool canUseGroundJump = wasGroundedThisFrame || coyoteTimer > 0f;

        if (canUseGroundJump)
        {
            float jumpHeightToUse = hasFeathers ? jumpHeight * featherJumpHeightMultiplier : jumpHeight;
            verticalVelocity = Mathf.Sqrt(jumpHeightToUse * -2f * downwardGravity);
            if (hasFeathers)
            {
                featherBoostCooldownTimer = featherTimeBetweenBoosts;
            }

            coyoteTimer = 0f;

            if (wingAnimationControl != null)
            {
                wingAnimationControl.WingFlap();
            }

            if (featherJumpSound != null)
            {
                if (walkSoundSource != null)
                {
                    walkSoundSource.PlayOneShot(featherJumpSound);
                }
                else
                {
                    AudioManager.PlayOneShot(featherJumpSound);
                }
            }

            return;
        }

        if (!wasGroundedThisFrame && hasFeathers && featherBoostCooldownTimer <= 0f)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, featherAirBoostStrength);
            featherBoostCooldownTimer = featherTimeBetweenBoosts;

            if (wingAnimationControl != null)
            {
                wingAnimationControl.WingFlap();
            }

            if (featherJumpSound != null)
            {
                if (walkSoundSource != null)
                {
                    walkSoundSource.PlayOneShot(featherJumpSound);
                }
                else
                {
                    AudioManager.PlayOneShot(featherJumpSound);
                }
            }
        }
    }

    void HandleLook(Vector2 lookVector, bool isMouse)
    {
        // only multiply by deltaTime if using mouse input for smoother rotation, not for gamepad which already accounts for frame rate
        float deltaTime = isMouse ? Time.deltaTime : 1f;
        // x-axis of mouse controls pitch (looking up/down)
        rotationY += lookVector.x * rotateSpeed * deltaTime;
        rotationX -= lookVector.y * rotateSpeed * deltaTime;
        if (isFractureScene)
        {
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        }
        else
        {
            rotationX = Mathf.Clamp(rotationX, -90f, 45f);  // 35?
        }
        transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }

    void HandleMovement(Vector2 moveInput)
    {
        float inputX = moveInput.x;
        float inputZ = moveInput.y;
        float downwardGravity = -Mathf.Abs(gravity);
        bool triggeredFeatherJumpThisFrame = false;
        
        // Cache grounded state at frame start for consistency
        bool wasGroundedThisFrame = characterController.isGrounded;
        //Debug.Log($"HandleMovement frame start: groundedCheck={wasGroundedThisFrame}, coyote={coyoteTimer:F3}, buffer={jumpBufferTimer:F3}");

        Vector3 move = (transform.right * inputX + transform.forward * inputZ).normalized;
        Vector3 horizontalMove = move * moveSpeed * Time.deltaTime;
        characterController.Move(horizontalMove);

        UpdateWalkSound(horizontalMove, wasGroundedThisFrame);

        if (wasGroundedThisFrame && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (isFractureScene && transform.position.y > fractureAntiFallTriggerY)
        {
            fractureAntiFallTriggered = false;
        }

        if (wasGroundedThisFrame)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        featherBoostCooldownTimer -= Time.deltaTime;

        if (isFractureScene && hasFeathers && !fractureAntiFallTriggered && transform.position.y <= fractureAntiFallTriggerY && featherBoostCooldownTimer <= 0f)
        {
            fractureAntiFallTriggered = true;
            verticalVelocity = Mathf.Max(verticalVelocity, fractureAntiFallBoostStrength);
            triggeredFeatherJumpThisFrame = true;
            featherBoostCooldownTimer = featherTimeBetweenBoosts;
        }

        if (triggeredFeatherJumpThisFrame)
        {
            if (wingAnimationControl != null)
            {
                wingAnimationControl.WingFlap();
            }

            if (featherJumpSound != null)
            {
                if (walkSoundSource != null)
                {
                    walkSoundSource.PlayOneShot(featherJumpSound);
                }
                else
                {
                    AudioManager.PlayOneShot(featherJumpSound);
                }
            }
        }

        float gravityToUse = downwardGravity;
        if (hasFeathers && verticalVelocity <= 0f)
        {
            gravityToUse *= featherFallGravityMultiplier;
        }

        verticalVelocity += gravityToUse * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void UpdateWalkSound(Vector3 horizontalMove, bool wasGroundedThisFrame)
    {
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
            else
            {
                AudioManager.PlayOneShot(walkSound, walkSoundVolume);
            }
        }
    }

    void InteractTrigger(InteractableBase interactable)
    {
        if (interactable == null)
        {
            return;
        }

        Debug.Log($"PlayerControllerFR received InteractTrigger from {interactable.gameObject.name}");
        Debug.Log($"PlayerControllerFR found InteractableBase: {interactable.gameObject.name}");
        Debug.Log($"Interactable text: {interactable.interactText}");
        interactCollider.SetInteractText(interactable.interactText);
        lookingAtInteractable = true;
        currentInteractable = interactable;

        // (on Interact button): interactable.Interact();
    }
    void InteractLeaveTrigger(InteractableBase interactable)
    {
        if (interactable != null)
        {
            Debug.Log($"PlayerControllerFR received InteractLeaveTrigger from {interactable.gameObject.name}");
        }
        lookingAtInteractable = false;
        currentInteractable = null;
        interactCollider.SetInteractText("");
    }

    void AttackAction(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.AreControlsDisabled(DisabledControls.Attack))
        {
            return;
        }
        if (isGrappleMoveLocked)
        {
            return;
        }

        if (TryDoGrappleAttack())
        {
            return;
        }

        DoInteractIfCan();
    }

    void InteractAction(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.AreControlsDisabled(DisabledControls.Interact))
        {
            return;
        }
        if (isGrappleMoveLocked)
        {
            return;
        }

        DoInteractIfCan();
    }

    void DoInteractIfCan()
    {
        if (lookingAtInteractable && currentInteractable != null)
        {
            Debug.Log($"Interacting with {currentInteractable.gameObject.name}");
            if (currentInteractable.CanInteract())
            {
                currentInteractable.Interact();
            }
            return;
        }
        Debug.Log("Interact/Attack button pressed but no interactable in range");
    }

    void GrappleTrigger(GrappleFromPoint grappleFromPoint)
    {
        if (grappleFromPoint == null)
        {
            return;
        }

        RefreshCachedGrapplePoints();
        currentGrappleFromPoint = grappleFromPoint;
        currentGrappleFromPoint.SetSourceActive(true);
        Debug.Log($"PlayerControllerBSK entered GrappleFromPoint mode for {currentGrappleFromPoint.gameObject.name}");
    }

    void GrappleLeaveTrigger(GrappleFromPoint grappleFromPoint)
    {
        if (grappleFromPoint != null)
        {
            Debug.Log($"PlayerControllerBSK left GrappleFromPoint trigger for {grappleFromPoint.gameObject.name}");
        }

        if (currentGrappleFromPoint != null)
        {
            currentGrappleFromPoint.SetSourceActive(false);
        }

        currentGrappleFromPoint = null;
        SetHighlightedGrapplePoint(null);
    }

    bool TryDoGrappleAttack()
    {
        if (isGrappleMoveLocked || currentGrappleFromPoint == null)
        {
            return false;
        }

        if (highlightedGrapplePoint == null)
        {
            Debug.Log("Attack pressed while in GrappleFromPoint mode, but no GrapplePoint is highlighted");
            return true;
        }

        if (currentGrappleFromPoint.Matches(highlightedGrapplePoint))
        {
            Debug.Log($"Matched GrapplePoint {highlightedGrapplePoint.gameObject.name} from {currentGrappleFromPoint.gameObject.name}");
            BeginGrappleMoveSequence();
            return true;
        }

        Debug.Log($"Highlighted GrapplePoint {highlightedGrapplePoint.gameObject.name} did not match {currentGrappleFromPoint.gameObject.name}");
        return true;
    }

    void UpdateGrappleHighlight()
    {
        if (isGrappleMoveLocked || currentGrappleFromPoint == null || playerCamera == null)
        {
            SetHighlightedGrapplePoint(null);
            SetGrappleLineActive(false);
            return;
        }

        GrapplePointGlow bestTarget = FindBestGrapplePointInView();
        SetHighlightedGrapplePoint(bestTarget);

        if (currentGrappleFromPoint != null && highlightedGrapplePoint != null)
        {
            activeGrappleLineSource = currentGrappleFromPoint.transform;
            activeGrappleLineTarget = highlightedGrapplePoint.transform;
            SetGrappleLinePoints(currentGrappleFromPoint.transform.position, highlightedGrapplePoint.transform.position);
            SetGrappleLineColor(currentGrappleFromPoint.Matches(highlightedGrapplePoint) ? Color.white : Color.red);
            SetGrappleLineActive(true);
        }
        else
        {
            SetGrappleLineActive(false);
        }
    }

    void BeginGrappleMoveSequence()
    {
        if (currentGrappleFromPoint == null || highlightedGrapplePoint == null)
        {
            return;
        }

        Transform movingParent = currentGrappleFromPoint.transform.parent;
        if (movingParent == null)
        {
            Debug.LogWarning($"GrappleFromPoint {currentGrappleFromPoint.gameObject.name} has no parent to move.");
            return;
        }

        int destinationIndex = GetFragmentDestinationIndex(movingParent.name);
        if (destinationIndex < 0 || destinationIndex >= fragmentDestinations.Length)
        {
            Debug.LogWarning($"Unable to map {movingParent.name} to a fragment destination.");
            return;
        }

        if (grappleLaunchSound != null)
        {
            if (grappleLaunchSource != null)
            {
                // Use assigned AudioSource (allows inspector control and separate mixer routing)
                grappleLaunchSource.PlayOneShot(grappleLaunchSound, grappleLaunchVolume);
            }
            else if (walkSoundSource != null)
            {
                // Reuse player's walk sound source (non-spatial by default)
                walkSoundSource.PlayOneShot(grappleLaunchSound, grappleLaunchVolume);
            }
            else
            {
                // Fallback to playing at player position
                AudioSource.PlayClipAtPoint(grappleLaunchSound, transform.position, grappleLaunchVolume);
            }
        }

        grappleMoveDestinationPosition = fragmentDestinations[destinationIndex];

        activeGrappleLineSource = currentGrappleFromPoint.transform;
        activeGrappleLineTarget = highlightedGrapplePoint.transform;
        grappleCompletedTargetObject = highlightedGrapplePoint.gameObject;

        currentGrappleFromPoint.ForceSourceLightOff();
        highlightedGrapplePoint.ForceLightOff();
        DisableLightsInHierarchy(movingParent);
        DisableAnimatorInHierarchy(movingParent);

        grapplePlayerOriginalParent = transform.parent;
        grappleLockedParent = movingParent;
        grappleLockedQuestComponent = movingParent.GetComponent<QuestComponent>();
        isGrappleMoveLocked = true;

        transform.SetParent(grappleLockedParent, true);

        CreateGrappleDestinationAnchor(grappleMoveDestinationPosition);
        CreateGrappleChainLink(grappleLockedParent, grappleDestinationAnchor);

        currentGrappleFromPoint = null;
        highlightedGrapplePoint = null;
        fractureAntiFallTriggered = false;

        cameraSwitchTimed?.SwitchToTargetCamera();
        // Play grappling pull sound using existing grapplePullSource (created in Awake)
        if (grapplingPullSound != null)
        {
            if (grapplePullSource != null)
            {
                if (grapplePullSource.clip != grapplingPullSound)
                {
                    grapplePullSource.clip = grapplingPullSound;
                }
                if (!grapplePullSource.isPlaying)
                {
                    grapplePullSource.Play();
                }
            }
            else
            {
                Debug.LogWarning("PlayerControllerBSK: grapplePullSource is null; cannot play grapplingPullSound.");
            }
        }
    }

    int GetFragmentDestinationIndex(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return -1;
        }

        char lastCharacter = objectName[objectName.Length - 1];
        if (!char.IsDigit(lastCharacter))
        {
            return -1;
        }

        return (lastCharacter - '0') - 1;
    }

    void CreateGrappleDestinationAnchor(Vector3 destinationPosition)
    {
        if (grappleDestinationAnchor != null)
        {
            Destroy(grappleDestinationAnchor.gameObject);
            grappleDestinationAnchor = null;
        }

        GameObject anchorObject = new GameObject("GrappleDestinationAnchor");
        anchorObject.transform.position = destinationPosition;
        grappleDestinationAnchor = anchorObject.transform;
    }

    void CreateGrappleChainLink(Transform startPoint, Transform endPoint)
    {
        if (activeGrappleChainLinkObject != null)
        {
            Destroy(activeGrappleChainLinkObject);
            activeGrappleChainLinkObject = null;
            activeGrappleChainLink = null;
        }

        if (grappleChainLinkPrefab == null || startPoint == null || endPoint == null)
        {
            return;
        }

        activeGrappleChainLinkObject = Instantiate(grappleChainLinkPrefab);
        activeGrappleChainLink = activeGrappleChainLinkObject.GetComponent<GrappleChainLink>();
        if (activeGrappleChainLink == null)
        {
            activeGrappleChainLink = activeGrappleChainLinkObject.AddComponent<GrappleChainLink>();
        }

        activeGrappleChainLink.Bind(startPoint, endPoint, grappleChainCompletionDistance);
    }

    void UpdateGrappleMoveLock()
    {
        if (!isGrappleMoveLocked || grappleLockedParent == null || grappleDestinationAnchor == null)
        {
            EndGrappleMoveLock();
            return;
        }

        grappleDestinationAnchor.position = grappleMoveDestinationPosition;

        Vector3 destinationPosition = grappleMoveDestinationPosition;
        grappleLockedParent.position = Vector3.MoveTowards(grappleLockedParent.position, destinationPosition, grappleMoveSpeed * Time.deltaTime);
        grappleLockedParent.rotation = Quaternion.RotateTowards(grappleLockedParent.rotation, Quaternion.identity, grappleRotateSpeed * Time.deltaTime);

        if (activeGrappleChainLink != null)
        {
            activeGrappleChainLink.Bind(grappleLockedParent, grappleDestinationAnchor, grappleChainCompletionDistance);
        }

        UpdateGrappleLine();

        float remainingDistance = Vector3.Distance(grappleLockedParent.position, destinationPosition);
        if (remainingDistance <= grappleCompletionDistance)
        {
            CompleteGrappleMoveLock();
        }
    }

    void CompleteGrappleMoveLock()
    {
        if (grappleLockedParent != null)
        {
            grappleLockedParent.position = grappleDestinationAnchor != null ? grappleDestinationAnchor.position : grappleLockedParent.position;
            grappleLockedParent.rotation = Quaternion.identity;
        }

        if (grappleCompletedTargetObject != null)
        {
            Destroy(grappleCompletedTargetObject);
            grappleCompletedTargetObject = null;
        }

        if (grappleLockedQuestComponent != null)
        {
            grappleLockedQuestComponent.CompleteTask();
        }

        EndGrappleMoveLock();
    }

    void EndGrappleMoveLock()
    {
        if (activeGrappleChainLinkObject != null)
        {
            Destroy(activeGrappleChainLinkObject);
            activeGrappleChainLinkObject = null;
            activeGrappleChainLink = null;
        }

        SetGrappleLineActive(false);

        if (grappleDestinationAnchor != null)
        {
            Destroy(grappleDestinationAnchor.gameObject);
            grappleDestinationAnchor = null;
            cameraSwitchTimed?.SwitchBackToPreviousCamera();
        }

        if (transform.parent != grapplePlayerOriginalParent)
        {
            transform.SetParent(grapplePlayerOriginalParent, true);
        }

        isGrappleMoveLocked = false;
        grappleLockedParent = null;
        grapplePlayerOriginalParent = null;
        grappleMoveDestinationPosition = Vector3.zero;

        if (grappleLockedAnimator != null)
        {
            grappleLockedAnimator.enabled = false;
            grappleLockedAnimator = null;
        }

        grappleLockedLights = null;
        grappleLockedQuestComponent = null;
        // Stop grapple pull audio if playing
        if (grapplePullSource != null && grapplePullSource.isPlaying)
        {
            grapplePullSource.Stop();
            grapplePullSource.clip = null;
        }
    }

    void SetGrappleLineActive(bool isActive)
    {
        if (!useGrappleLineRenderer || grappleLineRenderer == null)
        {
            return;
        }

        grappleLineRenderer.enabled = isActive;

        if (!isActive)
        {
            activeGrappleLineSource = null;
            activeGrappleLineTarget = null;
            return;
        }

        grappleLineRenderer.positionCount = 2;
        UpdateGrappleLine();
    }

    void SetGrappleLinePoints(Vector3 sourcePosition, Vector3 targetPosition)
    {
        if (!useGrappleLineRenderer || grappleLineRenderer == null)
        {
            return;
        }

        grappleLineRenderer.positionCount = 2;
        grappleLineRenderer.widthMultiplier = 1f;
        grappleLineRenderer.startWidth = grappleLineWidth;
        grappleLineRenderer.endWidth = grappleLineWidth;
        grappleLineRenderer.SetPosition(0, sourcePosition);
        grappleLineRenderer.SetPosition(1, targetPosition);
    }

    void SetGrappleLineColor(Color lineColor)
    {
        if (!useGrappleLineRenderer || grappleLineRenderer == null)
        {
            return;
        }

        grappleLineRenderer.startColor = lineColor;
        grappleLineRenderer.endColor = lineColor;
    }

    void UpdateGrappleLine()
    {
        if (!useGrappleLineRenderer || grappleLineRenderer == null || !grappleLineRenderer.enabled)
        {
            return;
        }

        if (activeGrappleLineSource == null || activeGrappleLineTarget == null)
        {
            return;
        }

        SetGrappleLinePoints(activeGrappleLineSource.position, activeGrappleLineTarget.position);
    }

    void DisableAnimatorInHierarchy(Transform root)
    {
        grappleLockedAnimator = root.GetComponent<Animator>();
        if (grappleLockedAnimator == null)
        {
            grappleLockedAnimator = root.GetComponentInChildren<Animator>(true);
        }

        if (grappleLockedAnimator != null)
        {
            grappleLockedAnimator.enabled = false;
        }
    }

    void DisableLightsInHierarchy(Transform root)
    {
        if (root == null)
        {
            return;
        }

        grappleLockedLights = root.GetComponentsInChildren<Light>(true);
        if (grappleLockedLights == null)
        {
            return;
        }

        for (int i = 0; i < grappleLockedLights.Length; i++)
        {
            if (grappleLockedLights[i] != null)
            {
                grappleLockedLights[i].enabled = false;
            }
        }
    }

    void ConfigureGrappleLineRenderer()
    {
        grappleLineRenderer.useWorldSpace = true;
        grappleLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        grappleLineRenderer.receiveShadows = false;
        grappleLineRenderer.textureMode = LineTextureMode.Stretch;
        grappleLineRenderer.numCapVertices = 6;
        grappleLineRenderer.widthMultiplier = 1f;
        grappleLineRenderer.startWidth = grappleLineWidth;
        grappleLineRenderer.endWidth = grappleLineWidth;

        if (grappleLineRenderer.sharedMaterial == null)
        {
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                grappleLineRenderer.sharedMaterial = new Material(lineShader);
            }
        }

        if (grappleLineRenderer.startColor.a <= 0f && grappleLineRenderer.endColor.a <= 0f)
        {
            grappleLineRenderer.startColor = Color.white;
            grappleLineRenderer.endColor = Color.white;
        }
    }

    void RefreshCachedGrapplePoints()
    {
        cachedGrapplePoints.Clear();
        GrapplePointGlow[] grapplePoints = FindObjectsByType<GrapplePointGlow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (grapplePoints != null && grapplePoints.Length > 0)
        {
            cachedGrapplePoints.AddRange(grapplePoints);
        }

        nextGrapplePointCacheRefreshTime = Time.time + Mathf.Max(0.1f, grapplePointCacheRefreshInterval);
    }

    void SetHighlightedGrapplePoint(GrapplePointGlow newHighlightedPoint)
    {
        if (highlightedGrapplePoint == newHighlightedPoint)
        {
            return;
        }

        if (highlightedGrapplePoint != null)
        {
            highlightedGrapplePoint.SetHighlighted(false);
        }

        highlightedGrapplePoint = newHighlightedPoint;

        if (highlightedGrapplePoint != null)
        {
            highlightedGrapplePoint.SetHighlighted(true);
        }
    }

    GrapplePointGlow FindBestGrapplePointInView()
    {
        GrapplePointGlow bestTarget = null;
        float bestCenterScore = float.MaxValue;

        for (int i = 0; i < cachedGrapplePoints.Count; i++)
        {
            GrapplePointGlow grapplePointGlow = cachedGrapplePoints[i];
            if (grapplePointGlow == null)
            {
                continue;
            }

            Transform candidateTransform = grapplePointGlow.transform;
            if (candidateTransform == null || !candidateTransform.CompareTag("GrapplePoint"))
            {
                continue;
            }

            Vector3 targetPoint = candidateTransform.position;
            Vector3 toTarget = targetPoint - playerCamera.transform.position;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= 0.01f || distanceToTarget > grappleHighlightCheckDistance)
            {
                continue;
            }

            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(targetPoint);
            if (viewportPoint.z <= 0f)
            {
                continue;
            }

            if (viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
            {
                continue;
            }

            float centerOffsetX = Mathf.Abs(viewportPoint.x - 0.5f);
            float centerOffsetY = Mathf.Abs(viewportPoint.y - 0.5f);
            if (centerOffsetX > grappleScreenCenterToleranceX || centerOffsetY > grappleScreenCenterToleranceY)
            {
                continue;
            }

            float normalizedX = centerOffsetX / Mathf.Max(grappleScreenCenterToleranceX, 0.0001f);
            float normalizedY = centerOffsetY / Mathf.Max(grappleScreenCenterToleranceY, 0.0001f);
            float centerScore = (normalizedX * normalizedX) + (normalizedY * normalizedY);

            if (centerScore < bestCenterScore)
            {
                bestTarget = grapplePointGlow;
                bestCenterScore = centerScore;
            }
        }

        return bestTarget;
    }

    public void OnTeleported(Quaternion targetRotation, bool applyRotation)
    {
        if (applyRotation)
        {
            // Apply only yaw (Y rotation) on teleport and keep current camera pitch.
            rotationY = targetRotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        }

        // Keep a small downward velocity so grounded logic settles immediately.
        verticalVelocity = -2f;
        coyoteTimer = coyoteTime;
        walkSoundDistanceAccumulator = 0f;

        if (hasFeathers && wingAnimationControl != null)
        {
            wingAnimationControl.ResetToIdle();
        }
    }

}
