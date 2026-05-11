using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class GrappleFromPoint : MonoBehaviour
{
    [SerializeField] private Light sourceLight;
    [SerializeField] private bool autoFindLightInChildren = true;
    [SerializeField] private float activeIntensity = 2f;
    [SerializeField] private Color activeColor = Color.white;

    [Header("Match")]
    [SerializeField] private GrapplePointGlow linkedGrapplePoint;
    [SerializeField] private UnityEvent onMatchedGrapple;

    [Header("Animation")]
    [SerializeField] private Animator animationAnimator;
    [SerializeField] private string animationTrigger = "Activate";
    [SerializeField] private Transform objectToMove;
    [SerializeField] private Transform moveTarget;
    [SerializeField] private float moveDuration = 0.35f;

    private bool baseLightEnabled;
    private float baseLightIntensity;
    private Color baseLightColor;
    private bool hasCachedBaseState;
    private Coroutine moveRoutine;

    void Awake()
    {
        CacheLightState();
    }

    public void SetSourceActive(bool isActive)
    {
        CacheLightState();

        if (sourceLight == null)
        {
            return;
        }

        if (isActive)
        {
            sourceLight.enabled = true;
            sourceLight.intensity = activeIntensity;
            sourceLight.color = activeColor;
            return;
        }

        sourceLight.enabled = baseLightEnabled;
        sourceLight.intensity = baseLightIntensity;
        sourceLight.color = baseLightColor;
    }

    public void ForceSourceLightOff()
    {
        CacheLightState();

        if (sourceLight != null)
        {
            sourceLight.enabled = false;
        }
    }

    public bool Matches(GrapplePointGlow grapplePointGlow)
    {
        return linkedGrapplePoint != null && linkedGrapplePoint == grapplePointGlow;
    }

    public void TriggerMatchedGrapple()
    {
        if (animationAnimator != null && !string.IsNullOrWhiteSpace(animationTrigger))
        {
            animationAnimator.SetTrigger(animationTrigger);
        }

        onMatchedGrapple?.Invoke();

        if (objectToMove != null && moveTarget != null)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveObjectToTarget());
        }
    }

    private IEnumerator MoveObjectToTarget()
    {
        Vector3 startPosition = objectToMove.position;
        Vector3 targetPosition = moveTarget.position;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, moveDuration);

        while (elapsed < duration && objectToMove != null && moveTarget != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            objectToMove.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        if (objectToMove != null && moveTarget != null)
        {
            objectToMove.position = moveTarget.position;
        }

        moveRoutine = null;
    }

    private void CacheLightState()
    {
        if (sourceLight == null && autoFindLightInChildren)
        {
            sourceLight = GetComponentInChildren<Light>(true);
        }

        if (sourceLight == null || hasCachedBaseState)
        {
            return;
        }

        baseLightEnabled = sourceLight.enabled;
        baseLightIntensity = sourceLight.intensity;
        baseLightColor = sourceLight.color;
        hasCachedBaseState = true;
    }
}