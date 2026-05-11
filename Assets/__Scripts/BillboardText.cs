using UnityEngine;
using TMPro;

// Text component is optional (created at runtime if not assigned)

[RequireComponent(typeof(Collider))]
public class BillboardText : MonoBehaviour
{
    public enum BillboardOrientation
    {
        FaceCamera,
        FaceCameraForward,
        FixedWorldRotation
    }

    public enum BillboardShowType
    {
        AlwaysShow,
        ShowOnCollide,
        ManualShow
    }

    [Header("Text")]
    [Tooltip("TextMeshPro component. (if null, created at runtime as a child of this GameObject.")]
    [SerializeField] private TextMeshPro worldText;
    [SerializeField] private string message = "Interact";
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private BillboardOrientation billboardOrientation = BillboardOrientation.FaceCamera;

    [Header("Rotation Options")]
    [Tooltip("Change Y rotation to the given fixedYRotation value (for FixedWorldRotation mode).")]
    [SerializeField] private bool useFixedYRotation = false;
    [Tooltip("FixedWorldRotation:this sets the Y rotation in degrees. Does respond in editor/playtesting")]
    [SerializeField] private float fixedYRotation = 0f;
    [Tooltip("FaceCamera mode: billboard keeps text upright (ignores Y rotation when facing camera).")]
    [SerializeField] private bool ignoreYRotation = false;

    [Header("Distance Scaling")]
    [SerializeField] private bool scaleByDistance;
    [Tooltip("If null, scales based on distance to main camera.")]
    [SerializeField] private Transform distanceTarget;
    [SerializeField] private float referenceDistance = 10f;
    [SerializeField] private float minScaleMultiplier = 0.5f;
    [SerializeField] private float maxScaleMultiplier = 2f;

    private Vector3 originalRotation;
    private Vector3 originalScale;

    [Header("Trigger")]
    [SerializeField] private BillboardShowType showType = BillboardShowType.ShowOnCollide;
    [SerializeField] private bool onlyShowForPlayer = true;
    [SerializeField] private string playerTag = "InteractCollider"; //"Player";
    [SerializeField] private float timeOutOnTriggerEnter = 0f;
    private float timeOutOnTriggerEnterCountdown;

    private int validTargetsInside;
    private Transform targetCameraTransform;

    private void Awake()
    {
        originalRotation = transform.rotation.eulerAngles;
        originalScale = transform.localScale;
        //"InteractCollider" has a collider set to IsTrigger so this isn't a concern:
        //WarnIfColliderNotTrigger();
        EnsureTextObject();
        ApplyTextSettings();
        SetTextVisible(showType == BillboardShowType.AlwaysShow);
        timeOutOnTriggerEnterCountdown = timeOutOnTriggerEnter;
    }

    public void ShowBillboard()
    {
        SetTextVisible(true);
    }
    public void HideBillboard()
    {
        SetTextVisible(false);
    }

    private void LateUpdate()
    {
        if (worldText == null || !worldText.gameObject.activeSelf)
        {
            return;
        }

        if (showType == BillboardShowType.ShowOnCollide &&
            timeOutOnTriggerEnter > 0.01f && validTargetsInside > 0)
        {
            timeOutOnTriggerEnterCountdown -= Time.deltaTime;
            if (timeOutOnTriggerEnterCountdown <= 0.01f)
            {
                Debug.Log("BillboardText: Time out reached for ShowOnCollide type. Hiding billboard until next trigger enter. GameObject: " + gameObject.name, this);
                SetTextVisible(false);
                return;
            }
        }

        Vector3 textPosition = transform.position + worldOffset;
        worldText.transform.position = textPosition;

        if (targetCameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                targetCameraTransform = mainCam.transform;
                if (targetCameraTransform == null)
                {
                    Debug.LogWarning($"BillboardText on {gameObject.name} could not find a valid camera to face. Please ensure there is a Camera tagged MainCamera in the scene.", this);
                    return;
                }
            }
        }

        switch (billboardOrientation)
        {
            case BillboardOrientation.FaceCamera:
                //worldText.transform.LookAt(targetCameraTransform.position, Vector3.up);
                //worldText.transform.rotation = Quaternion.LookRotation(-targetCameraTransform.forward, Vector3.up);
                Vector3 toCamera = targetCameraTransform.position - textPosition;
                if (ignoreYRotation)
                    toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.0001f)
                {
                    worldText.transform.rotation = Quaternion.LookRotation(-toCamera, Vector3.up);
                }
                break;
            case BillboardOrientation.FaceCameraForward:
                worldText.transform.forward = targetCameraTransform.forward;
                //worldText.transform.rotation
                //worldText.transform.rotation = Quaternion.LookRotation(-targetCameraTransform.forward, Vector3.up);
                break;
            case BillboardOrientation.FixedWorldRotation:
                // keep original rotation, or if fixedYRotation is used, adjust that
                // (note this isn't really useful outside of editor/play mode testing)
                if (useFixedYRotation)
                    worldText.transform.rotation = Quaternion.Euler(0f, fixedYRotation, 0f);
                break;
            default:
                break;
        }
        // size by distance?
        if (scaleByDistance)
        {
            Transform target = distanceTarget != null ? distanceTarget : targetCameraTransform.transform;
            float distance = Vector3.Distance(transform.position, target.position);
            float safeReferenceDistance = Mathf.Max(0.01f, referenceDistance);
            float scaleMultiplier = distance / safeReferenceDistance;
            scaleMultiplier = Mathf.Clamp(scaleMultiplier, minScaleMultiplier, maxScaleMultiplier);
            transform.localScale = originalScale * scaleMultiplier;
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (showType == BillboardShowType.AlwaysShow || showType == BillboardShowType.ManualShow)
            return;

        if (!IsValidTarget(other))
        {
            return;
        }

        validTargetsInside++;
        ResolveCameraFromCollider(other);
        SetTextVisible(true);
        timeOutOnTriggerEnterCountdown = timeOutOnTriggerEnter;
    }

    private void OnTriggerExit(Collider other)
    {
        if (showType == BillboardShowType.AlwaysShow || showType == BillboardShowType.ManualShow)
            return;
        
        if (!IsValidTarget(other))
        {
            return;
        }

        validTargetsInside = Mathf.Max(0, validTargetsInside - 1);
        if (validTargetsInside == 0)
        {
            SetTextVisible(false);
        }
    }

    private bool IsValidTarget(Collider other)
    {
        if (!onlyShowForPlayer)
        {
            return true;
        }

        return other.CompareTag(playerTag);
    }

    private void ResolveCameraFromCollider(Collider other)
    {
        PlayerControllerFR playerController = other.GetComponentInParent<PlayerControllerFR>();
        if (playerController != null && playerController.playerCamera != null)
        {
            targetCameraTransform = playerController.playerCamera.transform;
            return;
        }

        Camera playerCamera = other.GetComponentInParent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = other.GetComponentInChildren<Camera>(true);
        }

        if (playerCamera != null)
        {
            targetCameraTransform = playerCamera.transform;
            return;
        }

        if (Camera.main != null)
        {
            targetCameraTransform = Camera.main.transform;
        }
    }

    private void SetTextVisible(bool isVisible)
    {
        if (worldText != null)
        {
            worldText.gameObject.SetActive(isVisible);
        }
    }

    private void WarnIfColliderNotTrigger()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"BillboardText on {gameObject.name} requires a trigger collider to receive OnTrigger events. Assign a dedicated trigger collider or enable Is Trigger in the Inspector.", this);
        }
    }

    private void EnsureTextObject()
    {
        if (worldText != null)
        {
            return;
        }

        worldText = GetComponentInChildren<TextMeshPro>(true);
        if (worldText != null)
        {
            return;
        }

        GameObject textObject = new GameObject("BillboardText");
        textObject.transform.SetParent(transform, false);
        worldText = textObject.AddComponent<TextMeshPro>();
        worldText.alignment = TextAlignmentOptions.Center;
        worldText.fontSize = 2.5f;
    }

    private void ApplyTextSettings()
    {
        if (worldText == null)
        {
            return;
        }

        worldText.text = message;
        worldText.transform.position = transform.position + worldOffset;
    }

    private void OnValidate()
    {
        ApplyTextSettings();
    }
}
