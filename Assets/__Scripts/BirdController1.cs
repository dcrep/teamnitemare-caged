using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class BirdController1 : MonoBehaviour
{
	[Header("Collision")]
	[SerializeField] private float controllerHeight = 1.35f;
	[SerializeField] private float controllerRadius = 0.35f;
	[SerializeField] private Vector3 controllerCenter = new Vector3(0f, 0.67f, 0f);
	[SerializeField] private Color colliderGizmoColor = new Color(0.2f, 0.9f, 0.7f, 1f);
	//[SerializeField] private bool ignoreVisualModelColliders = true;
	//[SerializeField] private bool debugCollidersOnVisualModel = false;

	[Header("Build")]
	[SerializeField] private string birdRootName = "BirdModel";
	[SerializeField] private Transform visualRootOverride;

	[Header("Movement")]
	[SerializeField] private float moveSpeed = 8f;
	[SerializeField] private float walkSpeed = 4f;
	[SerializeField] private float verticalSpeed = 6f;
	[SerializeField] private float soarSpeed = 14f;
	[SerializeField] private bool logGroundEvents = true;
	[SerializeField] private bool logMovementStateChanges = true;
	[SerializeField] private float mouseSensitivity = 2.2f;
	[SerializeField] private float cameraSmooth = 14f;

	[Header("Camera")]
	[SerializeField] private Vector3 firstPersonLocalOffset = new Vector3(0f, 0.22f, 0.3f);
	[SerializeField] private Vector3 thirdPersonLocalOffset = new Vector3(0f, 0.55f, -3.8f);
	[SerializeField] private float pitchMin = -70f;
	[SerializeField] private float pitchMax = 80f;

	[Header("Animation")]
    [SerializeField] private Animator animator;

	private InputSystem_Actions playerControls;

	private CharacterController controller;
	private Camera playerCamera;

	private Transform birdVisualRoot;
	private Transform cameraPivot;
	private Transform firstPersonAnchor;

	private bool firstPersonMode;
	private bool wasGrounded;
	private bool walkingMode;
	private float yaw;
	private float pitch;
	private Vector2 planarInput;
	private bool jumpHeld;
	private bool descendHeld;
	private bool soarHeld;
	private bool warnedMissingAnimator;

	private enum BirdAnimState
	{
		Idle,
		Walking,
		Flying
	}

	private enum BirdMovementState
	{
		Walking,
		Flying,
		Soaring
	}

	private BirdAnimState currentAnimState = BirdAnimState.Idle;
	private BirdMovementState currentMovementState = BirdMovementState.Flying;

	public bool IsGrounded => controller != null && controller.isGrounded;
	public bool IsWalkingMode => walkingMode;

	private void Awake()
	{
		controller = GetComponent<CharacterController>();
		playerControls = new InputSystem_Actions();
		ApplyControllerShape();

		BindExistingBirdVisual();
		ResolveAnimatorReference();
		//ValidateVisualModelColliders();
		EnsureCameraRig();
	}

	private void OnEnable()
	{
		playerControls.Enable();
		playerControls.Player.View.performed += OnViewSwitchPressed;
		playerControls.Player.Jump.performed += OnJumpPressed;
		playerControls.Player.Jump.canceled += OnJumpReleased;
		playerControls.Player.Descend.performed += OnDescendPressed;
		playerControls.Player.Descend.canceled += OnDescendReleased;
		playerControls.Player.Sprint.performed += OnSoarPressed;
		playerControls.Player.Sprint.canceled += OnSoarReleased;
	}

	private void OnDisable()
	{
		playerControls.Player.View.performed -= OnViewSwitchPressed;
		playerControls.Player.Jump.performed -= OnJumpPressed;
		playerControls.Player.Jump.canceled -= OnJumpReleased;
		playerControls.Player.Descend.performed -= OnDescendPressed;
		playerControls.Player.Descend.canceled -= OnDescendReleased;
		playerControls.Player.Sprint.performed -= OnSoarPressed;
		playerControls.Player.Sprint.canceled -= OnSoarReleased;
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

		moveSpeed = Mathf.Max(0f, moveSpeed);
		walkSpeed = Mathf.Max(0f, walkSpeed);
		verticalSpeed = Mathf.Max(0f, verticalSpeed);
		soarSpeed = Mathf.Max(moveSpeed, soarSpeed);

		if (controller == null)
		{
			controller = GetComponent<CharacterController>();
		}

		ApplyControllerShape();
		ResolveAnimatorReference();
	}

	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		ResolveAnimatorReference();

		yaw = transform.eulerAngles.y;
		UpdateGroundState();
		UpdateMovementState(force: true);
		ApplyCameraImmediate();
		UpdateAnimationState(force: true);
	}

	private void Update()
	{
		HandleLook();
		HandleMove();
		UpdateGroundState();
		UpdateMovementState();
		SyncBirdVisual();
		UpdateCamera();
		UpdateAnimationState();
	}

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

	public void OnSoarPressed(InputAction.CallbackContext context)
	{
		soarHeld = true;
	}

	public void OnSoarReleased(InputAction.CallbackContext context)
	{
		soarHeld = false;
	}

	private void BindExistingBirdVisual()
	{
		if (visualRootOverride != null)
		{
			birdVisualRoot = visualRootOverride;
		}
		else
		{
			Transform namedChild = transform.Find(birdRootName);
			birdVisualRoot = namedChild != null ? namedChild : transform;
		}

		ResolveAnimatorReference();
	}

	private void ResolveAnimatorReference()
	{
		if (animator != null)
		{
			return;
		}

		if (birdVisualRoot != null)
		{
			animator = birdVisualRoot.GetComponentInChildren<Animator>(true);
		}

		if (animator == null)
		{
			animator = GetComponentInChildren<Animator>(true);
		}
	}

	private bool EnsureAnimator()
	{
		if (animator == null)
		{
			ResolveAnimatorReference();
		}

		if (animator != null)
		{
			return true;
		}

		if (!warnedMissingAnimator)
		{
			Debug.LogWarning("BirdController1: No Animator found on this object or its children.", this);
			warnedMissingAnimator = true;
		}

		return false;
	}
/*
	private void ValidateVisualModelColliders()
	{
		if (birdVisualRoot == null)
		{
			return;
		}

		Collider[] colliders = birdVisualRoot.GetComponentsInChildren<Collider>(true);
		if (colliders.Length == 0)
		{
			if (debugCollidersOnVisualModel)
			{
				Debug.Log($"BirdController1: No colliders found on visual model '{birdVisualRoot.name}'.", this);
			}
			return;
		}

		if (debugCollidersOnVisualModel)
		{
			Debug.Log($"BirdController1: Found {colliders.Length} collider(s) on visual model '{birdVisualRoot.name}':", this);
			for (int i = 0; i < colliders.Length; i++)
			{
				Debug.Log($"  [{i}] {colliders[i].gameObject.name} - {colliders[i].GetType().Name} (enabled: {colliders[i].enabled})", this);
			}
		}

		if (ignoreVisualModelColliders)
		{
			for (int i = 0; i < colliders.Length; i++)
			{
				if (colliders[i].isTrigger)
				{
					continue;
				}

				colliders[i].enabled = false;
			}

			if (debugCollidersOnVisualModel)
			{
				Debug.Log($"BirdController1: Disabled {colliders.Length} non-trigger collider(s) on visual model to avoid grounding interference.", this);
			}
		}
	}
	*/

	private void SyncBirdVisual()
	{
		if (birdVisualRoot == null)
		{
			return;
		}

		if (birdVisualRoot == transform || birdVisualRoot.IsChildOf(transform))
		{
			return;
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
		if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }

		if (controller == null || !controller.enabled)
		{
			return;
		}

		planarInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

		float y = 0f;
		if (jumpHeld)
		{
			y += 1f;
		}
		if (descendHeld)
		{
			y -= 1f;
		}

		Vector3 planarDirection;
		float currentPlanarSpeed;

		if (soarHeld)
		{
			planarDirection = Vector3.forward;
			currentPlanarSpeed = soarSpeed;
		}
		else
		{
			Vector3 rawPlanar = new Vector3(planarInput.x, 0f, planarInput.y);
			if (rawPlanar.sqrMagnitude > 1f)
			{
				rawPlanar.Normalize();
			}

			planarDirection = rawPlanar;
			currentPlanarSpeed = walkingMode ? walkSpeed : moveSpeed;
		}

		if (walkingMode)
		{
			y = jumpHeld ? 1f : -0.2f;
		}

		Vector3 worldPlanar = transform.TransformDirection(planarDirection) * currentPlanarSpeed;
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
		bool grounded = controller != null && controller.enabled && controller.isGrounded;
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
		if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }
		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

		yaw += mouseX;
		pitch = Mathf.Clamp(pitch - mouseY, pitchMin, pitchMax);

		transform.rotation = Quaternion.Euler(0f, yaw, 0f);
		cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
	}

	private void UpdateMovementState(bool force = false)
	{
		BirdMovementState nextState = ResolveMovementState();
        if (!force && nextState == currentMovementState)
		{
			return;
		}

		currentMovementState = nextState;
        if (logMovementStateChanges)
		{
			Debug.Log($"Bird movement state: {currentMovementState}", this);
		}
	}

	private BirdMovementState ResolveMovementState()
	{
		if (soarHeld)
		{
			return BirdMovementState.Soaring;
		}

		return walkingMode ? BirdMovementState.Walking : BirdMovementState.Flying;
	}

	private void UpdateCamera()
	{
		if (playerCamera == null)
		{
			return;
		}

		Vector3 cameraFocus = GetCameraFocusPosition();
		Vector3 focusDelta = cameraFocus - cameraPivot.position;

		Vector3 desiredPosition = firstPersonMode
			? firstPersonAnchor.position
			: cameraPivot.TransformPoint(thirdPersonLocalOffset) + focusDelta;

		Quaternion desiredRotation = firstPersonMode
			? cameraPivot.rotation
			: Quaternion.LookRotation(cameraFocus - desiredPosition, Vector3.up);

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

		Vector3 cameraFocus = GetCameraFocusPosition();
		Vector3 focusDelta = cameraFocus - cameraPivot.position;

		Vector3 initialPos = firstPersonMode
			? firstPersonAnchor.position
			: cameraPivot.TransformPoint(thirdPersonLocalOffset) + focusDelta;

		Quaternion initialRot = firstPersonMode
			? cameraPivot.rotation
			: Quaternion.LookRotation(cameraFocus - initialPos, Vector3.up);

		playerCamera.transform.position = initialPos;
		playerCamera.transform.rotation = initialRot;
	}

	private Vector3 GetCameraFocusPosition()
	{
		if (birdVisualRoot != null)
		{
			return birdVisualRoot.position;
		}

		if (cameraPivot != null)
		{
			return cameraPivot.position;
		}

		return transform.position;
	}

	private void UpdateAnimationState(bool force = false)
	{
		BirdAnimState nextState;

		if (currentMovementState == BirdMovementState.Soaring || currentMovementState == BirdMovementState.Flying)
		{
			nextState = BirdAnimState.Flying;
		}
		else if (planarInput.sqrMagnitude > 0.01f)
		{
			nextState = BirdAnimState.Walking;
		}
		else
		{
			nextState = BirdAnimState.Idle;
		}

		if (!force && nextState == currentAnimState)
		{
			return;
		}

		currentAnimState = nextState;

		switch (currentAnimState)
		{
			case BirdAnimState.Walking:
				StartWalkAnimation();
				break;
			case BirdAnimState.Flying:
				StartFlyingAnimation();
				break;
			default:
				StartIdleAnimation();
				break;
		}
	}

	private void StartWalkAnimation()
	{
		if (!EnsureAnimator())
		{
			return;
		}

		animator.SetBool("PC_Walking", true);
		animator.SetBool("PC_Flying", false);
		animator.SetBool("PC_Idle", false);
		
    }

	private void StartIdleAnimation()
	{
		if (!EnsureAnimator())
		{
			return;
		}

		animator.SetBool("PC_Walking", false);
		animator.SetBool("PC_Flying", false);
		animator.SetBool("PC_Idle", true);
    }

	private void StartFlyingAnimation()
	{
		if (!EnsureAnimator())
		{
			return;
		}

		animator.SetBool("PC_Walking", false);
		animator.SetBool("PC_Flying", true);
		animator.SetBool("PC_Idle", false);
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
