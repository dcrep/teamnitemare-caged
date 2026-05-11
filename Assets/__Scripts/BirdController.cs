using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class BirdController : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private float controllerHeight = 1.35f;
    [SerializeField] private float controllerRadius = 0.35f;
    [SerializeField] private Vector3 controllerCenter = new Vector3(0f, 0.67f, 0f);
    [SerializeField] private Color colliderGizmoColor = new Color(0.2f, 0.9f, 0.7f, 1f);

    [Header("Build")]
    [SerializeField] private string birdRootName = "BirdModel";
    [SerializeField] private Transform visualRootOverride;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float verticalSpeed = 6f;
    [SerializeField] private bool logGroundEvents = true;
    [SerializeField] private float mouseSensitivity = 2.2f;
    [SerializeField] private float cameraSmooth = 14f;

    [Header("Animation")]
    [SerializeField] private float wingFlapInterval = 0.2f;
    [SerializeField] private float wingFlapAngle = 32f;
    [SerializeField] private float footStepInterval = 0.35f;
    [SerializeField] private float footStepDistance = 0.08f;

    [Header("Camera")]
    [SerializeField] private Vector3 firstPersonLocalOffset = new Vector3(0f, 0.22f, 0.3f);
    [SerializeField] private Vector3 thirdPersonLocalOffset = new Vector3(0f, 0.55f, -3.8f);
    [SerializeField] private float pitchMin = -70f;
    [SerializeField] private float pitchMax = 80f;

    InputSystem_Actions playerControls;

    private CharacterController controller;
    private Camera playerCamera;

    private Transform birdVisualRoot;
    private Transform cameraPivot;
    private Transform firstPersonAnchor;
    private readonly List<Renderer> firstPersonHiddenRenderers = new List<Renderer>();
    private Transform leftWing;
    private Transform rightWing;
    private Transform leftFootRoot;
    private Transform rightFootRoot;
    private Quaternion leftWingBaseRotation;
    private Quaternion rightWingBaseRotation;
    private Vector3 leftFootBasePosition;
    private Vector3 rightFootBasePosition;

    private bool firstPersonMode;
    private bool wasGrounded;
    private bool walkingMode;
    private bool firstPersonVisualStateApplied;
    private float yaw;
    private float pitch;
    private float wingCycleTime;
    private float footCycleTime;
    private float forwardBackInput;
    private bool jumpHeld;
    private bool descendHeld;

    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool IsWalkingMode => walkingMode;

    

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerControls = new InputSystem_Actions();
        ApplyControllerShape();

        BindExistingBirdVisual();

        EnsureCameraRig();
        
    }
    void OnEnable()
    {
        playerControls.Enable();
        playerControls.Player.View.performed += OnViewSwitchPressed;
        playerControls.Player.Jump.performed += OnJumpPressed;
        playerControls.Player.Jump.canceled += OnJumpReleased;
        playerControls.Player.Descend.performed += OnDescendPressed;
        playerControls.Player.Descend.canceled += OnDescendReleased;
    }
    void OnDisable()
    {
        playerControls.Player.View.performed -= OnViewSwitchPressed;
        playerControls.Player.Jump.performed -= OnJumpPressed;
        playerControls.Player.Jump.canceled -= OnJumpReleased;
        playerControls.Player.Descend.performed -= OnDescendPressed;
        playerControls.Player.Descend.canceled -= OnDescendReleased;
        playerControls.Disable();
    }

    private void OnValidate()
    {
        controllerHeight = Mathf.Max(0.1f, controllerHeight);
        controllerRadius = Mathf.Max(0.01f, controllerRadius);
        if (controllerHeight < controllerRadius * 2f)
        {
            controllerHeight = controllerRadius * 2f;
        }

        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        ApplyControllerShape();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.eulerAngles.y;
        UpdateGroundState();
        ApplyCameraImmediate();
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        UpdateGroundState();
        UpdateFirstPersonVisuals();
        AnimateBirdVisuals();
        SyncBirdVisual();
        UpdateCamera();
    }

    // Input System callback hooks
    public void OnViewSwitchPressed(InputAction.CallbackContext context)
    {
        firstPersonMode = !firstPersonMode;
    }

    public void OnJumpPressed(InputAction.CallbackContext context)
    {
        jumpHeld = true;
    }

    public void OnJumpReleased(InputAction.CallbackContext context)
    {
        jumpHeld = false;
    }

    public void OnDescendPressed(InputAction.CallbackContext context)
    {
        descendHeld = true;
    }

    public void OnDescendReleased(InputAction.CallbackContext context)
    {
        descendHeld = false;
    }

    private void RegisterFirstPersonHidden(Transform part)
    {
        if (part == null)
        {
            return;
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            firstPersonHiddenRenderers.Add(renderer);
        }
    }

    private void BindExistingBirdVisual()
    {
        firstPersonHiddenRenderers.Clear();

        if (visualRootOverride != null)
        {
            birdVisualRoot = visualRootOverride;
        }
        else
        {
            Transform namedChild = transform.Find(birdRootName);
            birdVisualRoot = namedChild != null ? namedChild : transform;
        }

        leftWing = RequirePart("WingLeft");
        rightWing = RequirePart("WingRight");
        leftFootRoot = RequirePart("FootLeft");
        rightFootRoot = RequirePart("FootRight");

        RegisterFirstPersonHidden(RequirePart("Head"));
        RegisterFirstPersonHidden(RequirePart("BeakTop"));
        RegisterFirstPersonHidden(RequirePart("BeakBottom"));
        RegisterFirstPersonHidden(RequirePart("EyeLeft"));
        RegisterFirstPersonHidden(RequirePart("EyeRight"));

        CaptureAnimationBases();
        firstPersonVisualStateApplied = !firstPersonMode;
        UpdateFirstPersonVisuals();
    }

    private Transform RequirePart(string partName)
    {
        if (birdVisualRoot == null)
        {
            return null;
        }

        Transform part = birdVisualRoot.Find(partName);
        if (part != null)
        {
            return part;
        }

        Debug.LogError($"BirdController: Missing required part '{partName}' under '{birdVisualRoot.name}'.", this);
        return null;
    }

    private void UpdateFirstPersonVisuals()
    {
        bool shouldShow = !firstPersonMode;
        if (firstPersonVisualStateApplied == shouldShow)
        {
            return;
        }

        for (int i = 0; i < firstPersonHiddenRenderers.Count; i++)
        {
            if (firstPersonHiddenRenderers[i] != null)
            {
                firstPersonHiddenRenderers[i].enabled = shouldShow;
            }
        }

        firstPersonVisualStateApplied = shouldShow;
    }

    private void SyncBirdVisual()
    {
        if (birdVisualRoot == null)
        {
            return;
        }

        // If the visual is part of the controller hierarchy (typical prefab setup),
        // do not force world-space sync.
        if (birdVisualRoot == transform || birdVisualRoot.IsChildOf(transform))
        {
            return;
        }

    }

    private void CaptureAnimationBases()
    {
        if (leftWing != null)
        {
            leftWingBaseRotation = leftWing.localRotation;
        }

        if (rightWing != null)
        {
            rightWingBaseRotation = rightWing.localRotation;
        }

        if (leftFootRoot != null)
        {
            leftFootBasePosition = leftFootRoot.localPosition;
        }

        if (rightFootRoot != null)
        {
            rightFootBasePosition = rightFootRoot.localPosition;
        }

        wingCycleTime = 0f;
        footCycleTime = 0f;
    }

    private void AnimateBirdVisuals()
    {
        if (birdVisualRoot == null)
        {
            return;
        }

        if (!walkingMode)
        {
            
            AnimateWings();
            ResetFeetToBase();
            return;
        }

        ResetWingsToBase();
        if (Mathf.Abs(forwardBackInput) > 0.01f)
        {
            AnimateFeet();
        }
        else
        {
            ResetFeetToBase();
        }
    }

    private void AnimateWings()
    {
        
        if (leftWing == null || rightWing == null)
        {
            return;
        }

        float interval = Mathf.Max(0.01f, wingFlapInterval);
        wingCycleTime += Time.deltaTime;
        float phase = wingCycleTime * (Mathf.PI * 2f / interval);
        float flap = Mathf.Sin(phase) * wingFlapAngle;

        // Flap around local Z so wings swing outward/inward from the body.
        leftWing.localRotation = leftWingBaseRotation * Quaternion.Euler(0f, 0f, flap);
        rightWing.localRotation = rightWingBaseRotation * Quaternion.Euler(0f, 0f, -flap);
    }

    private void AnimateFeet()
    {
        if (leftFootRoot == null || rightFootRoot == null)
        {
            return;
        }

        float interval = Mathf.Max(0.01f, footStepInterval);
        footCycleTime += Time.deltaTime;
        float phase = footCycleTime * (Mathf.PI * 2f / interval);
        float stride = Mathf.Sin(phase) * footStepDistance;

        leftFootRoot.localPosition = leftFootBasePosition + new Vector3(0f, 0f, stride);
        rightFootRoot.localPosition = rightFootBasePosition + new Vector3(0f, 0f, -stride);
    }

    private void ResetWingsToBase()
    {
        if (leftWing != null)
        {
            leftWing.localRotation = leftWingBaseRotation;
        }

        if (rightWing != null)
        {
            rightWing.localRotation = rightWingBaseRotation;
        }
    }

    private void ResetFeetToBase()
    {
        if (leftFootRoot != null)
        {
            leftFootRoot.localPosition = leftFootBasePosition;
        }

        if (rightFootRoot != null)
        {
            rightFootRoot.localPosition = rightFootBasePosition;
        }
    }

    private void EnsureCameraRig()
    {
        Transform existingPivot = transform.Find("CameraPivot");
        if (existingPivot != null)
        {
            cameraPivot = existingPivot;
        }
        else
        {
            cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.17f, 0.1f);
        }

        Transform existingAnchor = cameraPivot.Find("FirstPersonAnchor");
        if (existingAnchor != null)
        {
            firstPersonAnchor = existingAnchor;
        }
        else
        {
            firstPersonAnchor = new GameObject("FirstPersonAnchor").transform;
            firstPersonAnchor.SetParent(cameraPivot, false);
            firstPersonAnchor.localPosition = firstPersonLocalOffset;
        }
        firstPersonAnchor.localPosition = firstPersonLocalOffset;

        Transform existingBirdCameraTransform = cameraPivot.Find("Bird Camera");
        if (existingBirdCameraTransform != null)
        {
            playerCamera = existingBirdCameraTransform.GetComponent<Camera>();
            if (playerCamera == null)
            {
                playerCamera = existingBirdCameraTransform.gameObject.AddComponent<Camera>();
            }

            if (existingBirdCameraTransform.GetComponent<AudioListener>() == null)
            {
                existingBirdCameraTransform.gameObject.AddComponent<AudioListener>();
            }

            existingBirdCameraTransform.tag = "MainCamera";
        }
        else
        {
            GameObject cameraObject = new GameObject("Bird Camera");
            cameraObject.transform.SetParent(cameraPivot, false);
            cameraObject.tag = "MainCamera";

            playerCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        Camera existingMain = Camera.main;
        if (existingMain != null && existingMain != playerCamera)
        {
            existingMain.enabled = false;

            AudioListener existingListener = existingMain.GetComponent<AudioListener>();
            if (existingListener != null)
            {
                existingListener.enabled = false;
            }
        }
    }

    private void HandleMove()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        forwardBackInput = z;

        float y = 0f;
        if (jumpHeld)
        {
            y += 1f;
        }
        if (descendHeld)
        {
            y -= 1f;
        }

        Vector3 planarInput = new Vector3(x, 0f, z);
        if (planarInput.sqrMagnitude > 1f)
        {
            planarInput.Normalize();
        }

        float currentPlanarSpeed = walkingMode ? walkSpeed : moveSpeed;
        if (walkingMode)
        {
            // Walking keeps the bird grounded unless player takes off.
            y = jumpHeld ? 1f : -0.2f;
        }

        Vector3 worldPlanar = transform.TransformDirection(planarInput) * currentPlanarSpeed;
        Vector3 vertical = Vector3.up * y * verticalSpeed;
        controller.Move((worldPlanar + vertical) * Time.deltaTime);
    }

    private void ApplyControllerShape()
    {
        if (controller == null)
        {
            return;
        }

        controller.height = controllerHeight;
        controller.radius = controllerRadius;
        controller.center = controllerCenter;
    }

    private void UpdateGroundState()
    {
        bool grounded = controller != null && controller.isGrounded;
        if (!wasGrounded && grounded)
        {
            walkingMode = true;

            if (logGroundEvents)
            {
                Debug.Log("Bird landed: switched to walking movement.", this);
            }
        }
        else if (wasGrounded && !grounded)
        {
            walkingMode = false;

            if (logGroundEvents)
            {
                Debug.Log("Bird airborne: switched to flying movement.", this);
            }
        }

        wasGrounded = grounded;
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateCamera()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 desiredPosition = firstPersonMode
            ? firstPersonAnchor.position
            : cameraPivot.TransformPoint(thirdPersonLocalOffset);

        Quaternion desiredRotation = firstPersonMode
            ? cameraPivot.rotation
            : Quaternion.LookRotation(cameraPivot.position - desiredPosition, Vector3.up);

        float t = 1f - Mathf.Exp(-cameraSmooth * Time.deltaTime);
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desiredPosition, t);
        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, desiredRotation, t);
    }

    private void ApplyCameraImmediate()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 initialPos = firstPersonMode
            ? firstPersonAnchor.position
            : cameraPivot.TransformPoint(thirdPersonLocalOffset);

        Quaternion initialRot = firstPersonMode
            ? cameraPivot.rotation
            : Quaternion.LookRotation(cameraPivot.position - initialPos, Vector3.up);

        playerCamera.transform.position = initialPos;
        playerCamera.transform.rotation = initialRot;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = colliderGizmoColor;

        float radius = Mathf.Max(0.01f, controllerRadius);
        float height = Mathf.Max(controllerHeight, radius * 2f);
        Vector3 worldCenter = transform.TransformPoint(controllerCenter);
        float axisHalf = Mathf.Max(0f, (height * 0.5f) - radius);

        Vector3 top = worldCenter + transform.up * axisHalf;
        Vector3 bottom = worldCenter - transform.up * axisHalf;

        Gizmos.DrawWireSphere(top, radius);
        Gizmos.DrawWireSphere(bottom, radius);

        Vector3 rightOffset = transform.right * radius;
        Vector3 forwardOffset = transform.forward * radius;

        Gizmos.DrawLine(top + rightOffset, bottom + rightOffset);
        Gizmos.DrawLine(top - rightOffset, bottom - rightOffset);
        Gizmos.DrawLine(top + forwardOffset, bottom + forwardOffset);
        Gizmos.DrawLine(top - forwardOffset, bottom - forwardOffset);
    }
}
