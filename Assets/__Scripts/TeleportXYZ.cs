using UnityEngine;

public class TeleportXYZ : MonoBehaviour
{
    [SerializeField] private Transform targetToTeleport;
    public Vector3 teleportLocation;

    void Awake()
    {
        if (targetToTeleport == null)
        {
            targetToTeleport = transform;
        }
    }

    public void Teleport()
    {
        Transform target = targetToTeleport != null ? targetToTeleport : transform;
        Debug.Log("Teleporting " + target.gameObject.name + " to: " + teleportLocation);

        CharacterController controller = target.GetComponent<CharacterController>();
        bool wasControllerEnabled = controller != null && controller.enabled;

        if (wasControllerEnabled)
        {
            controller.enabled = false;
        }

        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = teleportLocation;
        }
        else
        {
            target.position = teleportLocation;
        }

        if (wasControllerEnabled)
        {
            controller.enabled = true;
        }

        Physics.SyncTransforms();
    }
}
