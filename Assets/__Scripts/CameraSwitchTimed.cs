using UnityEngine;

public class CameraSwitchTimed : MonoBehaviour
{

    [SerializeField] private Camera fromCamera;
    private GameObject fromCameraGameObject;
    [SerializeField] private Camera targetCamera;
    private AudioListener fromCameraAudioListener;
    private AudioListener targetCameraAudioListener;

    private GameObject targetCameraGameObject;
    bool targetGameObjectWasActive = false;

    // no use for this yet..
    // private Camera mainCamera;

    private Camera switchedFromCamera = null;

    void Awake()
    {
        if (fromCamera == null)
        {
            Debug.LogError("FromCamera reference is missing.");
        }
        else
        {
            fromCameraGameObject = fromCamera.gameObject;
            fromCameraAudioListener = fromCamera.GetComponent<AudioListener>();
        }

        if (targetCamera == null)
        {
            Debug.LogError("TargetCamera reference is missing.");
        }
        else
        {
            targetCameraGameObject = targetCamera.gameObject;
            targetCameraAudioListener = targetCamera.GetComponent<AudioListener>();
        }
    }

    // void Awake()
    // {
            // Don't use Camera.current, use Camera.main (per user responses)
    //     fromCamera = Camera.current;

    //     if (fromCamera == null)
    //     {
    //         Debug.LogError("FromCamera reference is missing.");
    //     }
    //     mainCamera = Camera.main;
    //     // if (mainCamera == null)
    //     // {
    //     //     Debug.LogError("Main camera reference is missing.");
    //     // }
    // }

    public void SwitchToTargetCameraForDuration(float duration)
    {

        if (fromCamera == null || targetCamera == null)
        {
            Debug.LogError("Camera references cannot be null.");
            return;
        }
        SwitchToTargetCamera();
        Invoke(nameof(SwitchBackToPreviousCamera), duration);
    }

    
    [ContextMenu("Manual Camera Toggle")]
    public void ManualCameraToggle()
    {
        SwitchCameraToggle();
    }

    public void SwitchCameraToggle()
    {
        if (fromCamera == null || targetCamera == null)
        {
            Debug.LogError("Camera references cannot be null.");
            return;
        }

        if (targetCamera.enabled)
        {
            // Manual inspector toggles can reach this path without a prior scripted switch.
            if (switchedFromCamera == null)
            {
                switchedFromCamera = fromCamera;
            }
            SwitchBackToPreviousCamera();
        }
        else
        {
            SwitchToTargetCamera();
        }
    }

    public void SwitchToTargetCamera()
    {
        if (fromCamera == null || targetCamera == null)
        {
            Debug.LogError("Camera references cannot be null.");
            return;
        }
        if (fromCameraGameObject.activeInHierarchy == false)
        {
            Debug.LogWarning("From camera game object is not active.");
            return;
        }

        if (fromCamera.enabled)
        {
            targetGameObjectWasActive = targetCameraGameObject.activeInHierarchy;
            if (!targetGameObjectWasActive)
            {
                targetCameraGameObject.SetActive(true);
            }
            else if (targetCamera.enabled)
            {
                Debug.LogWarning("Target camera is already enabled.");
                return;
            }
            SetCameraAndListenerEnabled(fromCamera, fromCameraAudioListener, false);
            SetCameraAndListenerEnabled(targetCamera, targetCameraAudioListener, true);
            switchedFromCamera = fromCamera;
        }
    }
    public void SwitchBackToPreviousCamera()
    {
        if (switchedFromCamera == null)
        {
            Debug.LogWarning("No previous camera to switch back to. Switch to target first or set fromCamera as fallback.");
            return;
        }
        SetCameraAndListenerEnabled(switchedFromCamera, fromCameraAudioListener, true);
        SetCameraAndListenerEnabled(targetCamera, targetCameraAudioListener, false);
        switchedFromCamera = null;
        if (!targetGameObjectWasActive)
        {
            targetCameraGameObject.SetActive(false);
        }
    }

    static void SetCameraAndListenerEnabled(Camera cameraToSet, AudioListener listenerToSet, bool isEnabled)
    {
        if (cameraToSet != null)
        {
            cameraToSet.enabled = isEnabled;
        }

        if (listenerToSet != null)
        {
            listenerToSet.enabled = isEnabled;
        }
    }

}
