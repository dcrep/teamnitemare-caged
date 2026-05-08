//using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerFR : MonoBehaviour, ISaveable
{

    InputSystem_Actions playerControls;
    InputAction moveAction, lookAction, hitAction, interactAction, jumpAction;

    public float moveSpeed = 5f;
    public float rotateSpeed = 10f;
    public float jumpForce = 10f;
    public float gravity = -30f;
    public float grappleCheckDistance = 30f;
    public float grappleScreenCenterToleranceX = 0.22f;
    public float grappleScreenCenterToleranceY = 0.34f;
    public float grapplePullSpeed = 18f;
    public float grappleStopDistance = 0.2f;
    public float grappleActivationRadius = 6f;
    public bool keepLineAfterGrapple = false;
    public bool uprightOnGrappleEnd = true;
    public bool pauseOnGrappleEnd = false;
    public float grappleLineWidth = 0.04f;
    public Vector3 grappleLineStartOffset = new Vector3(0f, 1.4f, 0.2f);
    public LayerMask grappleCheckMask = ~0;
    public Camera playerCamera;
    public LineRenderer grappleLineRenderer;

    private float rotationX;
    private float rotationY;
    private float verticalVelocity;
    private bool isGrappling;
    private bool isPlatformLocked;
    private Transform grappleTarget;
    private Transform grapplePawnPositionTarget;
    private Transform nearestGrappleSource;
    private Transform lockedPlatformTransform;
    private FragmentConnection lockedPlatformFragment;
    private Transform lockedPlatformSourcePoint;
    private Transform lockedPlatformTargetPoint;
    private Transform playerOriginalParent;
    private Vector3 lockedPlatformLastPosition;
    private float lockedPlatformStillTime;
    private Vector3 grappleDestinationCenter;
    private float playerBottomToCenterOffset;

    private CharacterController characterController;

    private InteractCollider interactCollider;

    bool lookingAtInteractable = false;
    InteractableBase currentInteractable = null;
    

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerControls = new InputSystem_Actions();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (grappleLineRenderer == null)
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

        Vector3 currentCenter = transform.position + characterController.center;
        playerBottomToCenterOffset = currentCenter.y - GetPlayerBottomWorldY();
        rotationY = transform.eulerAngles.y;

        if (playerCamera != null)
        {
            rotationX = NormalizeAngle(playerCamera.transform.localEulerAngles.x);
        }

    }
    void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
        lookAction = playerControls.Player.Look;
        hitAction = playerControls.Player.Attack;
        interactAction = playerControls.Player.Interact;
        jumpAction = playerControls.Player.Jump;

        hitAction.performed += AttackAction;
        interactAction.performed += InteractAction;
        jumpAction.performed += JumpAction;
        //playerControls.Player.Jump.performed += JumpAction;

        // get child InteractArea's InteractCollider and subscribe to its event
        InteractCollider[] interactColliders = GetComponentsInChildren<InteractCollider>();
        if (interactColliders.Length > 0)
        {
            interactCollider = interactColliders[0];
            interactCollider.OnPlayerHitInteractable += InteractTrigger;
            interactCollider.OnPlayerLeaveInteractable += InteractLeaveTrigger;
        }
        else
        {
            Debug.LogWarning("PlayerControllerFR could not find a InteractCollider for interaction events.");
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
    void OnDisable()
    {
        // unsubscribe from interactCollider events
        if (interactCollider != null)
        {
            interactCollider.OnPlayerHitInteractable -= InteractTrigger;
            interactCollider.OnPlayerLeaveInteractable -= InteractLeaveTrigger;
        }

        hitAction.performed -= AttackAction;
        interactAction.performed -= InteractAction;
        jumpAction.performed -= JumpAction;
        //playerControls.Player.Jump.performed -= JumpAction;
        playerControls.Disable();
        StopGrapple(false);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlatformLocked)
        {
            UpdatePlatformLock();

            // Keep look active while the platform is auto-moving.
            Vector2 lockedLookVector = lookAction.ReadValue<Vector2>();
            Rotate(lockedLookVector, lookAction.activeControl?.device is Mouse);
            return;
        }

        if (isGrappling)
        {
            UpdateGrapple();
            UpdateGrappleLine();
            return;
        }

        if (GameManager.Instance.AreAllControlsDisabled())
        {
            return;
        }

        Vector2 moveVector = moveAction.ReadValue<Vector2>();
        //characterController.Move(moveVector);
        Move(moveVector);

        // Handle look input
        Vector2 lookVector = lookAction.ReadValue<Vector2>();
        //characterController.Rotate(lookVector);
        Rotate(lookVector, lookAction.activeControl?.device is Mouse);
    }

    void JumpAction(InputAction.CallbackContext context)
    {
        if (isPlatformLocked)
        {
            return;
        }

        Debug.Log("Jump button pressed");
        if (characterController.isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }

    void AttackAction(InputAction.CallbackContext context)
    {
        if (isPlatformLocked)
        {
            return;
        }

        if (GameManager.Instance.AreAllControlsDisabled())
        {
            return;
        }

        if (lookingAtInteractable && currentInteractable != null)
        {
            DoInteractIfCan();
            return;
        }

        Debug.Log("Attack button pressed");

        Vector3 playerCenter = transform.position + characterController.center;
        if (FindNearestGrapplePoint(playerCenter, grappleActivationRadius) == null)
        {
            Debug.Log("No GrapplePoint within activation radius");
            return;
        }

        Camera activeCamera = playerCamera != null ? playerCamera : Camera.main;
        if (activeCamera == null)
        {
            Debug.LogWarning("No camera found for grapple targeting");
            return;
        }

        Vector3 origin = activeCamera.transform.position;
        Vector3 forward = activeCamera.transform.forward;

        // Prefer exact center hit first, but ignore hidden collider-only blockers.
        if (TryFindGrapplePointOnRay(origin, forward, grappleCheckDistance, out Collider centerGrappleTarget))
        {
            Debug.Log($"Found GrapplePoint (center): {centerGrappleTarget.gameObject.name}");
            StartGrapple(centerGrappleTarget);
            return;
        }

        // Fallback: choose visible GrapplePoints near the screen center, with LOS.
        Collider[] nearby = Physics.OverlapSphere(
            origin,
            grappleCheckDistance,
            grappleCheckMask,
            QueryTriggerInteraction.Collide
        );

        Collider bestTarget = null;
        float bestCenterScore = float.MaxValue;
        float bestDistance = float.MaxValue;

        foreach (Collider candidate in nearby)
        {
            if (!candidate.CompareTag("GrapplePoint"))
            {
                continue;
            }

            Vector3 targetPoint = candidate.bounds.center;
            Vector3 toTarget = targetPoint - origin;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= 0.01f)
            {
                continue;
            }

            Vector3 viewportPoint = activeCamera.WorldToViewportPoint(targetPoint);
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

            Debug.Log($"Candidate: {candidate.gameObject.name}, OffsetX: {centerOffsetX}, OffsetY: {centerOffsetY}, Distance: {distanceToTarget}");

            Vector3 directionToTarget = toTarget / distanceToTarget;

            if (!HasLineOfSightToCandidate(origin, directionToTarget, distanceToTarget, candidate))
            {
                continue;
            }

            if (centerScore < bestCenterScore || (Mathf.Approximately(centerScore, bestCenterScore) && distanceToTarget < bestDistance))
            {
                bestTarget = candidate;
                bestCenterScore = centerScore;
                bestDistance = distanceToTarget;
            }
        }

        if (bestTarget != null)
        {
            Debug.Log($"Found GrapplePoint (camera assist): {bestTarget.gameObject.name}");
            StartGrapple(bestTarget);
            return;
        }

        Debug.Log("No GrapplePoint found in line of sight");
    }

    void InteractAction(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.AreAllControlsDisabled())
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

    void StartGrapple(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return;
        }

        Transform candidateDestination = FindGrapplePawnPositionSibling(targetCollider.transform);
        if (candidateDestination == null)
        {
            Debug.LogWarning($"No sibling with tag GrapplePawnPosition found for {targetCollider.gameObject.name}");
            return;
        }

        Vector3 playerCenter = transform.position + characterController.center;
        Transform sourcePoint = FindNearestGrapplePoint(playerCenter, grappleActivationRadius);
        if (sourcePoint == targetCollider.transform)
        {
            Debug.Log($"Source and target GrapplePoint are the same ({targetCollider.gameObject.name}), ignoring grapple");
            return;
        }

        if (TryConnectParentFragments(sourcePoint, targetCollider.transform))
        {
            return;
        }

        grappleTarget = targetCollider.transform;
        grapplePawnPositionTarget = candidateDestination;
        nearestGrappleSource = sourcePoint;
        isGrappling = true;
        verticalVelocity = 0f;
        playerBottomToCenterOffset = playerCenter.y - GetPlayerBottomWorldY();
        grappleDestinationCenter = GetGrapplePawnDestinationCenter();

        if (grappleLineRenderer != null)
        {
            grappleLineRenderer.enabled = true;
            grappleLineRenderer.positionCount = 2;
            UpdateGrappleLine();
        }
    }

    void UpdateGrapple()
    {
        if (grappleTarget == null || grapplePawnPositionTarget == null)
        {
            StopGrapple(true);
            return;
        }

        grappleDestinationCenter = GetGrapplePawnDestinationCenter();
        Vector3 playerCenter = transform.position + characterController.center;
        Vector3 toTarget = grappleDestinationCenter - playerCenter;
        float distance = toTarget.magnitude;
        if (distance <= grappleStopDistance)
        {
            CompleteGrappleAtDestination();
            StopGrapple(true);
            return;
        }

        float step = grapplePullSpeed * Time.deltaTime;
        Vector3 nextCenter = Vector3.MoveTowards(playerCenter, grappleDestinationCenter, step);
        SetPlayerCenterPosition(nextCenter);
    }

    void UpdateGrappleLine()
    {
        if (!isGrappling || grappleTarget == null || grappleLineRenderer == null)
        {
            return;
        }

        // Keep width synced with inspector value while testing/tuning.
        grappleLineRenderer.widthMultiplier = 1f;
        grappleLineRenderer.startWidth = grappleLineWidth;
        grappleLineRenderer.endWidth = grappleLineWidth;

        Vector3 lineStart = GetGrappleLineStartPoint();
        Vector3 lineEnd = grappleTarget.position;
        grappleLineRenderer.SetPosition(0, lineStart);
        grappleLineRenderer.SetPosition(1, lineEnd);
    }

    void StopGrapple(bool shouldPause)
    {
        bool wasGrappling = isGrappling;
        isGrappling = false;
        grappleTarget = null;
        grapplePawnPositionTarget = null;
        nearestGrappleSource = null;

        if (wasGrappling && uprightOnGrappleEnd)
        {
            SnapPlayerUpright();
        }

        if (grappleLineRenderer != null && !keepLineAfterGrapple)
        {
            grappleLineRenderer.enabled = false;
        }

        if (shouldPause && pauseOnGrappleEnd)
        {
            Debug.Break();
        }
    }

    void CompleteGrappleAtDestination()
    {
        if (characterController == null)
        {
            return;
        }

        SetPlayerCenterPosition(grappleDestinationCenter);
    }

    void SnapPlayerUpright()
    {
        rotationY = transform.eulerAngles.y;
        rotationX = 0f;
        ApplyLookRotation();
    }

    void ApplyLookRotation()
    {
        transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
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

    void SetPlayerCenterPosition(Vector3 worldCenterPosition)
    {
        Vector3 finalPosition = worldCenterPosition - characterController.center;
        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.position = finalPosition;
        characterController.enabled = wasEnabled;
    }

    Transform FindGrapplePawnPositionSibling(Transform grapplePointTransform)
    {
        if (grapplePointTransform == null)
        {
            return null;
        }

        Transform parent = grapplePointTransform.parent;
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling == grapplePointTransform)
            {
                continue;
            }

            if (sibling.CompareTag("GrapplePawnPosition"))
            {
                return sibling;
            }
        }

        return null;
    }

    Vector3 GetGrapplePawnDestinationCenter()
    {
        if (grapplePawnPositionTarget == null)
        {
            return transform.position + characterController.center;
        }

        Vector3 targetCenter = GetWorldCenter(grapplePawnPositionTarget);
        Transform parent = grapplePawnPositionTarget.parent;
        float targetBottomY = parent != null ? GetTopWorldY(parent) : GetBottomWorldY(grapplePawnPositionTarget);
        float desiredCenterY = targetBottomY + playerBottomToCenterOffset;
        return new Vector3(targetCenter.x, desiredCenterY, targetCenter.z);
    }

    float GetPlayerBottomWorldY()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float minY = float.MaxValue;
        bool foundRenderer = false;

        foreach (Renderer rendererComponent in renderers)
        {
            if (!rendererComponent.enabled || rendererComponent is LineRenderer)
            {
                continue;
            }

            foundRenderer = true;
            if (rendererComponent.bounds.min.y < minY)
            {
                minY = rendererComponent.bounds.min.y;
            }
        }

        if (foundRenderer)
        {
            return minY;
        }

        return characterController.bounds.min.y;
    }

    float GetBottomWorldY(Transform target)
    {
        if (target == null)
        {
            return transform.position.y;
        }

        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.min.y;
        }

        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            return targetRenderer.bounds.min.y;
        }

        return target.position.y;
    }

    float GetTopWorldY(Transform target)
    {
        if (target == null)
        {
            return transform.position.y;
        }

        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.max.y;
        }

        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            return targetRenderer.bounds.max.y;
        }

        return target.position.y;
    }

    Vector3 GetWorldCenter(Transform target)
    {
        if (target == null)
        {
            return transform.position + characterController.center;
        }

        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            return targetRenderer.bounds.center;
        }

        return target.position;
    }

    Vector3 GetGrappleLineStartPoint()
    {
        if (nearestGrappleSource != null)
        {
            return nearestGrappleSource.position;
        }

        Vector3 basePoint = characterController.bounds.min;
        Vector3 horizontalOffset = (transform.right * grappleLineStartOffset.x) + (transform.forward * grappleLineStartOffset.z);
        return basePoint + Vector3.up * grappleLineStartOffset.y + horizontalOffset;
    }

    bool TryFindGrapplePointOnRay(Vector3 origin, Vector3 direction, float maxDistance, out Collider grapplePoint)
    {
        grapplePoint = null;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            maxDistance,
            grappleCheckMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider = hit.collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.CompareTag("GrapplePoint"))
            {
                grapplePoint = hitCollider;
                return true;
            }

            if (ShouldIgnoreAsInvisibleLosBlocker(hitCollider))
            {
                continue;
            }

            // A visible non-grapple collider blocks center line of sight.
            return false;
        }

        return false;
    }

    bool HasLineOfSightToCandidate(Vector3 origin, Vector3 directionToTarget, float distanceToTarget, Collider candidate)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            directionToTarget,
            distanceToTarget,
            grappleCheckMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider = hit.collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider == candidate)
            {
                return true;
            }

            if (ShouldIgnoreAsInvisibleLosBlocker(hitCollider))
            {
                continue;
            }

            return false;
        }

        return false;
    }

    bool ShouldIgnoreAsInvisibleLosBlocker(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.CompareTag("GrapplePoint"))
        {
            return false;
        }

        Renderer hitRenderer = hitCollider.GetComponent<Renderer>();
        if (hitRenderer == null)
        {
            return false;
        }

        return !hitRenderer.enabled;
    }

    Transform FindNearestGrapplePoint(Vector3 worldPosition, float radius)
    {
        Collider[] candidates = Physics.OverlapSphere(worldPosition, radius, grappleCheckMask, QueryTriggerInteraction.Collide);
        Transform nearest = null;
        float nearestSqDist = float.MaxValue;

        foreach (Collider candidate in candidates)
        {
            if (!candidate.CompareTag("GrapplePoint"))
            {
                continue;
            }

            float sqDist = (candidate.bounds.center - worldPosition).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest = candidate.transform;
            }
        }

        return nearest;
    }

    bool TryConnectParentFragments(Transform sourcePoint, Transform targetPoint)
    {
        if (sourcePoint == null || targetPoint == null)
        {
            return false;
        }

        Transform sourceParent = sourcePoint.parent;
        Transform targetParent = targetPoint.parent;
        if (sourceParent == null || targetParent == null)
        {
            return false;
        }

        FragmentConnection sourceFragment = sourceParent.GetComponent<FragmentConnection>();
        FragmentConnection targetFragment = targetParent.GetComponent<FragmentConnection>();
        if (sourceFragment == null || targetFragment == null)
        {
            return false;
        }

        bool isMatch = sourceFragment.fragmentToConnectTo == targetFragment || targetFragment.fragmentToConnectTo == sourceFragment;
        if (!isMatch)
        {
            return false;
        }

        sourceFragment.ConnectToFragment(targetFragment);
        BeginPlatformLock(sourceParent, sourceFragment, sourcePoint, targetPoint);
        return true;
    }

    void BeginPlatformLock(Transform platformTransform, FragmentConnection platformFragment, Transform sourcePoint, Transform targetPoint)
    {
        if (platformTransform == null || platformFragment == null)
        {
            return;
        }

        isPlatformLocked = true;
        lockedPlatformTransform = platformTransform;
        lockedPlatformFragment = platformFragment;
        lockedPlatformSourcePoint = sourcePoint;
        lockedPlatformTargetPoint = targetPoint;
        playerOriginalParent = transform.parent;
        lockedPlatformLastPosition = platformTransform.position;
        lockedPlatformStillTime = 0f;

        StopGrapple(false);
        transform.SetParent(platformTransform, true);

        if (grappleLineRenderer != null && lockedPlatformSourcePoint != null && lockedPlatformTargetPoint != null)
        {
            grappleLineRenderer.enabled = true;
            grappleLineRenderer.positionCount = 2;
            grappleLineRenderer.widthMultiplier = 1f;
            grappleLineRenderer.startWidth = grappleLineWidth;
            grappleLineRenderer.endWidth = grappleLineWidth;
            grappleLineRenderer.SetPosition(0, lockedPlatformSourcePoint.position);
            grappleLineRenderer.SetPosition(1, lockedPlatformTargetPoint.position);
        }
    }

    void UpdatePlatformLock()
    {
        if (grappleLineRenderer != null && grappleLineRenderer.enabled && lockedPlatformSourcePoint != null && lockedPlatformTargetPoint != null)
        {
            grappleLineRenderer.SetPosition(0, lockedPlatformSourcePoint.position);
            grappleLineRenderer.SetPosition(1, lockedPlatformTargetPoint.position);
        }

        if (lockedPlatformTransform == null || lockedPlatformFragment == null)
        {
            EndPlatformLock();
            return;
        }

        float movedDistance = Vector3.Distance(lockedPlatformTransform.position, lockedPlatformLastPosition);
        if (movedDistance <= 0.0005f)
        {
            lockedPlatformStillTime += Time.deltaTime;
        }
        else
        {
            lockedPlatformStillTime = 0f;
        }

        lockedPlatformLastPosition = lockedPlatformTransform.position;

        // End lock as soon as the fragment reports connected, or when the platform has stopped moving.
        if (lockedPlatformFragment.isConnected || lockedPlatformStillTime >= 0.15f)
        {
            EndPlatformLock();
        }
    }

    void EndPlatformLock()
    {
        transform.SetParent(playerOriginalParent, true);

        if (grappleLineRenderer != null)
        {
            grappleLineRenderer.enabled = false;
        }

        isPlatformLocked = false;
        lockedPlatformTransform = null;
        lockedPlatformFragment = null;
        lockedPlatformSourcePoint = null;
        lockedPlatformTargetPoint = null;
        playerOriginalParent = null;
        lockedPlatformStillTime = 0f;
        lockedPlatformLastPosition = Vector3.zero;
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


    public void Move(Vector2 moveVector)
    {
        Vector3 move = transform.forward * moveVector.y + transform.right * moveVector.x;
        move = move * moveSpeed * Time.deltaTime;
        characterController.Move(move);

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    public void Rotate(Vector2 lookVector, bool isMouse)
    {
        // only multiply by deltaTime if using mouse input for smoother rotation, not for gamepad which already accounts for frame rate
        float deltaTime = isMouse ? Time.deltaTime : 1f;

        // x-axis of mouse controls pitch (looking up/down)
        rotationY += lookVector.x * rotateSpeed * deltaTime;
        rotationX -= lookVector.y * rotateSpeed * deltaTime;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        ApplyLookRotation();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Entered trigger: {other.gameObject.name}");
    }

#region ISaveable implementation
    private class TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }
    public object CaptureState()
    {
        return new TransformState {
            position = transform.position,
            rotation = transform.rotation,
            localScale = transform.localScale
        };
    }
    public void RestoreState(object state)
    {
        Debug.Log("Restoring PlayerControllerFR state");
        if (state is TransformState transformState)
        {
            Debug.Log($"Restoring position: {transformState.position}, rotation: {transformState.rotation}, localScale: {transformState.localScale}");
            this.transform.position = transformState.position;
            this.transform.rotation = transformState.rotation;
            this.transform.localScale = transformState.localScale;
        }
    }
#endregion ISaveable implementation
}
