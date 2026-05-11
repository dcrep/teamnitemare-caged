using UnityEngine;

[DisallowMultipleComponent]
public class GrappleChainLink : MonoBehaviour
{
    [SerializeField] private float baseLength = 1f;
    [SerializeField] private float closeDistance = 0.35f;
    [SerializeField] private Vector3 scaleAxis = new Vector3(1f, 1f, 1f);

    private Transform startPoint;
    private Transform endPoint;
    private Vector3 initialScale;
    private bool isBound;

    void Awake()
    {
        initialScale = transform.localScale;
    }

    public void Bind(Transform start, Transform end, float destroyDistance)
    {
        startPoint = start;
        endPoint = end;
        closeDistance = Mathf.Max(0.01f, destroyDistance);
        isBound = true;
        UpdateLinkVisual();
    }

    void LateUpdate()
    {
        if (!isBound || startPoint == null || endPoint == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateLinkVisual();

        if (Vector3.Distance(startPoint.position, endPoint.position) <= closeDistance)
        {
            Destroy(gameObject);
        }
    }

    void UpdateLinkVisual()
    {
        Vector3 startPosition = startPoint.position;
        Vector3 endPosition = endPoint.position;
        Vector3 delta = endPosition - startPosition;
        float distance = delta.magnitude;

        transform.position = (startPosition + endPosition) * 0.5f;

        if (distance > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        float normalizedLength = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, baseLength));
        transform.localScale = new Vector3(
            initialScale.x * scaleAxis.x,
            initialScale.y * scaleAxis.y,
            initialScale.z * scaleAxis.z * normalizedLength
        );
    }
}