using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private BillboardType billboardType;
    //[SerializeField] private BillboardShowType showType;

    [Header("Lock Rotation")]
    [SerializeField] private bool lockX;
    [SerializeField] private bool lockY;
    [SerializeField] private bool lockZ;

    [Header("Positioning")]
    [SerializeField] private Vector3 positionOffset;


    [Header("Distance Scaling")]
    [SerializeField] private bool scaleByDistance;
    [SerializeField] private Transform distanceTarget;
    [SerializeField] private float referenceDistance = 10f;
    [SerializeField] private float minScaleMultiplier = 0.5f;
    [SerializeField] private float maxScaleMultiplier = 2f;

    [Header("Editor Gizmos")]
    [SerializeField] private bool showDistanceGizmos = true;

    private Vector3 originalRotation;
    private Vector3 originalScale;


    public enum BillboardType { LookAtCamera, CameraForward, FixedDirection };
    //public enum BillboardShowType { AlwaysShow, ShowOnCollide, ManualShow };

    private void Awake()
    {
        originalRotation = transform.rotation.eulerAngles;
        originalScale = transform.localScale;
        if (positionOffset != Vector3.zero)
        {
            transform.position += positionOffset;
        }
    }

    //Use Late update so everything should have finished moving.
    private void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        //There are a few ways people billboard things.
        switch (billboardType)
        {
            case BillboardType.LookAtCamera:
                transform.LookAt(mainCamera.transform.position, Vector3.up);

                break;
            case BillboardType.CameraForward:
                transform.forward = mainCamera.transform.forward;
                break;
            case BillboardType.FixedDirection:
                // Do nothing, keep the original rotation
                break;
            default:
                break;
        }
        //Modify the rotation in Euler space to lock certain dimensions.
        Vector3 rotation = transform.rotation.eulerAngles;
        if (lockX) { rotation.x = originalRotation.x; }
        if (lockY) { rotation.y = originalRotation.y; }
        if (lockZ) { rotation.z = originalRotation.z; }
        transform.rotation = Quaternion.Euler(rotation);

        if (scaleByDistance)
        {
            Transform target = distanceTarget != null ? distanceTarget : mainCamera.transform;
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

    private void OnDrawGizmosSelected()
    {
        if (!showDistanceGizmos || !scaleByDistance)
        {
            return;
        }

        Transform target = distanceTarget;
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }

        if (target == null)
        {
            return;
        }

        float safeReferenceDistance = Mathf.Max(0.01f, referenceDistance);
        float minDistance = safeReferenceDistance * Mathf.Max(0f, minScaleMultiplier);
        float maxDistance = safeReferenceDistance * Mathf.Max(minScaleMultiplier, maxScaleMultiplier);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position, safeReferenceDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position, minDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, maxDistance);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(target.position, transform.position);
    }
}
