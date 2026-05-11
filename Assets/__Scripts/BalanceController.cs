using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BalanceController : MonoBehaviour, ISaveable
{
    [SerializeField] Animator anim;
    private InputSystem_Actions playerControls;
    private InputAction moveAction;
    CharacterController characterController;
    float lean;           // current balance
    float leanVelocity;   // optional inertia
    //float gustForce;
    //float gustTimer;
    [SerializeField] float driftStrength = 0.35f;  // overall drift magnitude
    [SerializeField] float driftChangeInterval = 1.25f;
    [SerializeField] float driftSmoothing = 1.5f;
    [SerializeField] float driftMinTargetAbs = 0.45f;
    [SerializeField] float driftFlipCenterThreshold = 1f;
    [SerializeField] float counterStrength = 2f;   // how strong the player counterbalance is
    [SerializeField] float inputDeadzone = 0.15f;
    [SerializeField] float holdRampTime = 0.35f;
    [SerializeField] float maxHoldCounterMultiplier = 1.8f;
    [SerializeField] float damping = 4f;           // damping rate per second
    [SerializeField] float leanAcceleration = 20; // how quickly lean reacts to force
    [SerializeField] float maxLeanVelocity = 30f;  // cap lean speed for stability
    [SerializeField] float failThreshold = 40f;    // how far you have to lean before you fail

    [SerializeField] Camera playerCam;
    [SerializeField] float maxRollAngle = 10f;
    [SerializeField] float rollSmoothing = 8f;
    [SerializeField] bool invertInputDirection = true;
    [SerializeField] bool invertCameraRoll = true;

    [SerializeField] float struggleBobAmount = 0.05f;
    [SerializeField] float struggleBobSpeed = 8f;

    [SerializeField] float forwardMoveInterval = 3f;
    [SerializeField] float forwardMoveDistance = 1.25f;
    [SerializeField] float centerThresholdForForwardMove = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] walkSounds;
    // [SerializeField] private float walkSoundDistance = 1.8f;
    [SerializeField] private float walkSoundVolume = 0.7f;
    [SerializeField] private AudioSource walkSoundSource;

    private float walkSoundDistanceAccumulator;
    private int walkSoundIndex;

    [SerializeField] bool invertNeedle = true;

    [SerializeField] RectTransform needle;
    [SerializeField] float needleMaxOffset = 200f;

    [SerializeField] FadeToColor screenFade;
     [SerializeField] TMP_Text helpText;
    //[SerializeField] private RectTransform helpTextRect;

    Vector3 cameraBaseLocalPosition;
    Quaternion cameraBaseLocalRotation;
    float driftCurrent;
    float driftTarget;
    float driftChangeTimer;
    bool driftFlipPending;
    int lastDriftSign = 1;
    int holdDirection;
    float holdTimer;
    float forwardMoveTimer;
    bool waitingForCenteredForwardMove;

    bool stopMovement = false;
    bool dontResetCameraOnStop = false;
    float canvasPPU = 100;
    Coroutine freefallCoroutine;
    bool isFreefalling;

    [Header("Freefall")]
    [SerializeField] float freefallSpeed = 12f;
    [SerializeField] float defaultFreefallDuration = 1.5f;
    [SerializeField] float freefallRightRollDegrees = -25f;
    [SerializeField] float freefallSkyPitchDegrees = -55f;
    [SerializeField] float freefallCameraAimDuration = 1.2f;

    Vector3 originalPosition;
    Quaternion originalRotation;
    [SerializeField] GameObject otherPlayerController = null;
    bool bReloadedScene = false;
    bool bWasInControl = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (bReloadedScene)
        {
            Debug.Log("Awake: Detected scene reload. Restoring position and rotation.");
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            if (bWasInControl)
            {
                if (otherPlayerController != null)
                {
                    otherPlayerController.SetActive(false);
                    this.gameObject.SetActive(true);
                }
                //ResumeMovement();
            }
            else
            {
                if (otherPlayerController != null)
                {
                    otherPlayerController.SetActive(true);
                    this.gameObject.SetActive(false);
                }
            }
            // StopMovement();
            // ResumeMovement();
        }
        else
        {
            if (otherPlayerController != null)
            {
                otherPlayerController.SetActive(true);
                this.gameObject.SetActive(false);
            }
            Debug.Log("Awake: Storing original position and rotation.");
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }
        playerControls = new InputSystem_Actions();
        characterController = GetComponent<CharacterController>();
        if (helpText != null)
        {
            Canvas canvas = helpText.GetComponentInParent<Canvas>();
            Debug.Log("PPU:" + canvas.referencePixelsPerUnit);
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
    }

    void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
        if (helpText != null)
        {
            helpText.gameObject.SetActive(true);
        }
        if (needle != null)
        {
            needle.parent.gameObject.SetActive(true);
        }
    }

    void OnDisable()
    {
        StopFreefall();

        if (playerControls != null)
        {
            playerControls.Disable();
        }
    }

    void Start()
    {
        if (playerCam == null)
        {
            playerCam = GetComponentInChildren<Camera>();
        }

        if (playerCam != null)
        {
            cameraBaseLocalPosition = playerCam.transform.localPosition;
            cameraBaseLocalRotation = playerCam.transform.localRotation;
        }

        driftTarget = Random.Range(0f, 1f);
        driftCurrent = driftTarget;
        lastDriftSign = 1;
        driftChangeTimer = driftChangeInterval;
        driftFlipPending = false;
        holdDirection = 0;
        holdTimer = 0f;
        forwardMoveTimer = Mathf.Max(0.1f, forwardMoveInterval);
        waitingForCenteredForwardMove = false;

        StopMovement();
        Invoke(nameof(ResumeMovement), 2f);

        //StartCoroutine(GustRoutine());

    }

    void Update()
    {
        if (GameManager.Instance.AreMoveControlsDisabled())
        {
            return;
        }
        if (stopMovement)
        {
            if (!isFreefalling && !dontResetCameraOnStop)
            {
                ResetLeanAndCamera();
            }
            return;
        }
        float dt = Time.deltaTime;

        UpdateForwardStep(dt);

        //Smoothed random drift (changes slowly to allow reacting)
        float drift = UpdateDrift(dt);

        // Player counterbalance
        float horizontalRaw = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;
        float horizontal = ApplyDeadzone(horizontalRaw, inputDeadzone);
        float inputSign = invertInputDirection ? -1f : 1f;
        float holdMultiplier = UpdateHoldMultiplier(horizontal, dt);
        float counter = horizontal * counterStrength * holdMultiplier * inputSign;

        // Wind gusts? meh
        //UpdateGust(dt);

        // Combine forces
        float totalForce = drift + counter; // + gustForce;

        // Apply inertia
        leanVelocity += totalForce * leanAcceleration * dt;
        leanVelocity = Mathf.Clamp(leanVelocity, -maxLeanVelocity, maxLeanVelocity);
        leanVelocity *= Mathf.Exp(-damping * dt);

        lean += leanVelocity * dt;
        lean = Mathf.Clamp(lean, -failThreshold, failThreshold);

        // UI + Animation
        UpdateUI();
        //UpdateAnimation();
        UpdateCameraTilt();
        UpdateStruggleBob();
        
        //LogDirectionDebug(dt, horizontal, drift, counter, totalForce);

        // Failure check
        if (Mathf.Abs(lean) >= failThreshold)
            Fail();
    }

    public void StopMovement()
    {
        stopMovement = true;
        ResetLeanAndCamera();
    }

    // This is called shortly before the CrowAttackSequence and World Geometry falls apart sequence
    // and finally the freefall sequence, so stop movement, look down at crow attack
    public void StopMovementAndDisableBalanceVisuals()
    {
        StopMovement();
        dontResetCameraOnStop = true;
        if (helpText != null)
        {
            helpText.gameObject.SetActive(false);
        }
        if (needle != null)
        {
            needle.parent.gameObject.SetActive(false);
        }
        // also rotate camera down over the course of 1 second
        if (playerCam != null)
        {
            StartCoroutine(RotateCameraDown());
        }
    }
    IEnumerator RotateCameraDown()
    {
        if (playerCam == null)
        {
            yield break;
        }

        Quaternion startRotation = playerCam.transform.localRotation;
        // target is 25 degrees down, nothing to do with freefall
        Quaternion targetRotation = cameraBaseLocalRotation * Quaternion.Euler(25f, 0f, 0f);
        
        float elapsed = 0f;
        float duration = 0.8f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            playerCam.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCam.transform.localRotation = targetRotation;
    }

    public void ResumeMovement()
    {
        stopMovement = false;
        forwardMoveTimer = Mathf.Max(0.1f, forwardMoveInterval);
        waitingForCenteredForwardMove = false;
    }

    // Starts freefall movement only. Screen fade should be triggered externally.
    public void Freefall()
    {
        Freefall(defaultFreefallDuration);
    }

    public void Freefall(float duration)
    {
        if (freefallCoroutine != null)
        {
            StopCoroutine(freefallCoroutine);
        }

        StopMovementAndDisableBalanceVisuals();
        isFreefalling = true;
        freefallCoroutine = StartCoroutine(FreefallRoutine(duration));
    }

    public void StopFreefall()
    {
        isFreefalling = false;

        if (freefallCoroutine != null)
        {
            StopCoroutine(freefallCoroutine);
            freefallCoroutine = null;
        }

        ResetLeanAndCamera();
    }

    IEnumerator FreefallRoutine(float duration)
    {
        float safeDuration = Mathf.Max(0f, duration);
        float safeSpeed = Mathf.Max(0.01f, freefallSpeed);
        float elapsed = 0f;
        float safeAimDuration = Mathf.Max(0.01f, freefallCameraAimDuration);
        Quaternion startCamRotation = playerCam != null ? playerCam.transform.localRotation : Quaternion.identity;
        Quaternion freefallTargetCamRotation = cameraBaseLocalRotation * Quaternion.Euler(freefallSkyPitchDegrees, 0f, freefallRightRollDegrees);

        while (safeDuration <= 0f || elapsed < safeDuration)
        {
            float dt = Time.deltaTime;
            Vector3 fallStep = Vector3.down * safeSpeed * dt;

            if (playerCam != null)
            {
                float cameraT = Mathf.Clamp01(elapsed / safeAimDuration);
                float easedCameraT = Mathf.SmoothStep(0f, 1f, cameraT);
                playerCam.transform.localRotation = Quaternion.Slerp(startCamRotation, freefallTargetCamRotation, easedCameraT);
            }

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(fallStep);
            }
            else
            {
                transform.position += fallStep;
            }

            elapsed += dt;
            yield return null;
        }

        isFreefalling = false;
        freefallCoroutine = null;
    }

    void UpdateForwardStep(float dt)
    {
        if (!waitingForCenteredForwardMove)
        {
            forwardMoveTimer -= dt;
            if (forwardMoveTimer > 0f)
            {
                return;
            }

            waitingForCenteredForwardMove = true;
        }

        if (Mathf.Abs(lean) > centerThresholdForForwardMove)
        {
            return;
        }

        waitingForCenteredForwardMove = false;
        forwardMoveTimer = Mathf.Max(0.1f, forwardMoveInterval);
        Vector3 forwardStep = transform.forward * forwardMoveDistance;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(forwardStep);
        }
        else
        {
            transform.position += forwardStep;
        }

        UpdateWalkSound(forwardStep, true);
    }

    void UpdateWalkSound(Vector3 horizontalMove, bool wasGroundedThisFrame)
    {
        AudioClip walkSound = walkSounds[walkSoundIndex % walkSounds.Length];
        walkSoundIndex++;

        if (walkSound == null)
        {
            return;
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

    void Fail()
    {
        Debug.Log("Failed! Lean was: " + lean);
        // fade to red, reload scene
        screenFade.StartFadeOut();
        helpText.enabled = false;
        // You can add failure logic here, like restarting the level or showing a game over screen.
        // For now, we'll just reset the lean for testing purposes.
        lean = 0f;
        leanVelocity = 0f;
        StopMovement();
        bWasInControl = true;
        Invoke(nameof(ReloadScene), 1f);
    }

    void ReloadScene()
    {
        GameManager.Instance.gameState.currentSceneScript.ReloadScene();
    }

    void ResetLeanAndCamera()
    {
        lean = 0f;
        leanVelocity = 0f;
        //gustForce = 0f;
        //gustTimer = 0f;
        holdDirection = 0;
        holdTimer = 0f;

        if (playerCam != null)
        {
            playerCam.transform.localRotation = cameraBaseLocalRotation;
            playerCam.transform.localPosition = cameraBaseLocalPosition;
        }
    }

    void UpdateStruggleBob()
    {
        if (playerCam == null)
        {
            return;
        }

        float danger = Mathf.InverseLerp(0.6f * failThreshold, failThreshold, Mathf.Abs(lean));
        if (danger <= 0f)
        {
            playerCam.transform.localPosition = Vector3.Lerp(
                playerCam.transform.localPosition,
                cameraBaseLocalPosition,
                Time.deltaTime * rollSmoothing);
            return;
        }

        float bob = Mathf.Sin(Time.time * struggleBobSpeed) * struggleBobAmount * danger;

        Vector3 pos = cameraBaseLocalPosition;
        pos.y += bob;
        playerCam.transform.localPosition = pos;
    }

    void UpdateCameraTilt()
    {
        if (playerCam == null)
        {
            return;
        }

        float normalized = Mathf.Clamp(lean / failThreshold, -1f, 1f);
        float rollSign = invertCameraRoll ? -1f : 1f;
        float targetRoll = normalized * maxRollAngle * rollSign;

        Quaternion targetRot = Quaternion.Euler(
            cameraBaseLocalRotation.eulerAngles.x,
            cameraBaseLocalRotation.eulerAngles.y,
            targetRoll
        );

        playerCam.transform.localRotation =
            Quaternion.Slerp(playerCam.transform.localRotation, targetRot, Time.deltaTime * rollSmoothing);
    }

    void UpdateUI()
    {
        //Debug.Log("Updating UI. Lean: " + lean);
        float normalized = Mathf.Clamp(lean / failThreshold, -1f, 1f);
        float needleSign = invertNeedle ? -1f : 1f;
        needle.anchoredPosition = new Vector2(normalized * needleMaxOffset * needleSign, 0f);

        float danger = Mathf.InverseLerp(0.25f * failThreshold, failThreshold, Mathf.Abs(lean));
        Color target = new Color(1f, 0f, 0f, danger * 0.6f);
        screenFade.SetColorInstantly(target);
    }

    void LateUpdate()
    {
        float tiltAmount = playerCam.transform.localRotation.z;
        
        if (helpText != null)
        {
            //Debug.Log("helpTextRect rotate: tiltAmount:" + -tiltAmount);
            //helpText.transform.Rotate(0, 0, -tiltAmount, Space.Self);
            //helpTextRect.transform.localRotation = Quaternion.Euler(0,0, -tiltAmount);

            // translate worldspace rotation to local rotation
            Vector3 currentRotation = helpText.rectTransform.localEulerAngles;
            helpText.rectTransform.localEulerAngles = new Vector3(
                currentRotation.x,
                currentRotation.y,
                tiltAmount * canvasPPU // ExtractCameraRoll(playerCam)
            );
        }
    }
    float ExtractCameraRoll(Camera cam)
    {
        // Camera basis vectors
        Vector3 forward = cam.transform.forward;
        Vector3 up = cam.transform.up;
        Vector3 right = cam.transform.right;

        // Remove vertical component from right vector
        Vector3 flatRight = Vector3.ProjectOnPlane(right, Vector3.up).normalized;

        // If camera is looking straight up/down, fallback to avoid NaN
        if (flatRight.sqrMagnitude < 0.0001f)
            return 0f;

        // Compare camera's right vector to flattened right vector
        float roll = Vector3.SignedAngle(flatRight, right, forward);

        return roll;
    }

    float UpdateDrift(float dt)
    {
        if (!driftFlipPending)
        {
            driftChangeTimer -= dt;
            if (driftChangeTimer <= 0f)
            {
                driftFlipPending = true;
                driftChangeTimer = 0f;
            }
        }

        if (driftFlipPending && Mathf.Abs(lean) <= driftFlipCenterThreshold)
        {
            lastDriftSign *= -1;
            float magnitude = Random.Range(driftMinTargetAbs, 1f);
            driftTarget = magnitude * lastDriftSign;
            driftChangeTimer = driftChangeInterval;
            driftFlipPending = false;
        }

        driftCurrent = Mathf.MoveTowards(driftCurrent, driftTarget, driftSmoothing * dt);
        return driftCurrent * driftStrength;
    }

    float ApplyDeadzone(float value, float deadzone)
    {
        if (Mathf.Abs(value) <= deadzone)
        {
            return 0f;
        }

        float sign = Mathf.Sign(value);
        float normalized = (Mathf.Abs(value) - deadzone) / (1f - deadzone);
        return sign * Mathf.Clamp01(normalized);
    }

    float UpdateHoldMultiplier(float horizontal, float dt)
    {
        int direction = horizontal > 0f ? 1 : horizontal < 0f ? -1 : 0;

        if (direction == 0)
        {
            holdDirection = 0;
            holdTimer = 0f;
            return 1f;
        }

        if (direction != holdDirection)
        {
            holdDirection = direction;
            holdTimer = 0f;
        }
        else
        {
            holdTimer += dt;
        }

        if (holdRampTime <= 0f)
        {
            return maxHoldCounterMultiplier;
        }

        float t = Mathf.Clamp01(holdTimer / holdRampTime);
        return Mathf.Lerp(1f, maxHoldCounterMultiplier, t);
    }

    // public void TriggerGust(float strength, float duration)
    // {
    //     gustForce = strength;
    //     gustTimer = duration;
    // }

    // void UpdateGust(float dt)
    // {
    //     if (gustTimer > 0f)
    //     {
    //         gustTimer -= dt;
    //         leanVelocity += gustForce * dt;
    //     }
    // }


    // IEnumerator GustRoutine()
    // {
    //     while (true)
    //     {
    //         yield return new WaitForSeconds(Random.Range(3f, 8f));
    //         if (stopMovement)
    //         {
    //             continue;
    //         }
    //         float strength = Random.Range(-3f, 3f);
    //         float duration = Random.Range(0.5f, 1.5f);
    //         TriggerGust(strength, duration);
    //     }
    // }


    // void UpdateAnimation()
    // {
    //     float normalized = Mathf.Clamp(lean / failThreshold, -1f, 1f);
    //     anim.SetFloat("LeanAmount", normalized);
    // }

    #region ISaveable implementation
    private class BalanceData
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool bWasInControl;
    }
    public object CaptureState()
    {
        var data = new BalanceData();
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.bWasInControl = bWasInControl;
        return data;
    }
    public void RestoreState(object state)
    {
        Debug.Log("Restoring BalanceController state");
        if (state is BalanceData data)
        {
            bReloadedScene = true;
            // Debug.Log($"Restoring position: {data.position}, rotation: {data.rotation}");
            // transform.position = data.position;
            // transform.rotation = data.rotation;
            originalPosition = data.position;
            originalRotation = data.rotation;
            bWasInControl = data.bWasInControl;
        }
    }
#endregion ISaveable implementation

}
