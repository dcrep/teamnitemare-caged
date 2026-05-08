using UnityEngine;

public class TeleportTarget : MonoBehaviour
{
    public bool matchTargetRotation = true;
    public void TeleportPlayerHere(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("TeleportTarget->TeleportPlayerHere: Player GameObject is null!");
            return;
        }

        Quaternion yawOnlyRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            // Snap teleport for CharacterController to avoid swept-collision side effects.
            bool wasEnabled = controller.enabled;
            if (wasEnabled)
            {
                controller.enabled = false;
            }

            // Align controller feet to the target position for consistent landing.
            Vector3 destination = transform.position;
            float feetOffset = controller.center.y - (controller.height * 0.5f);
            destination.y -= feetOffset;
            player.transform.position = destination;
            
            if (matchTargetRotation)
                player.transform.rotation = yawOnlyRotation;

            if (wasEnabled)
            {
                controller.enabled = true;
            }

        }
        else if (player.TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = transform.position;
            if (matchTargetRotation)
                body.rotation = yawOnlyRotation;
        }
        else
        {
            player.transform.position = transform.position;
            if (matchTargetRotation)
                player.transform.rotation = yawOnlyRotation;
        }

        // Stop all player movement on teleport.
        PlayerControllerBSK playerController = player.GetComponent<PlayerControllerBSK>();
        if (playerController != null)
        {
            playerController.OnTeleported(yawOnlyRotation, matchTargetRotation);
        }
        // update physics transforms immediately in case player was teleported while mid-air or next to a wall, to prevent unintended collisions in the next frame.
        Physics.SyncTransforms();
    }
    public void TeleportDumbObjectHere(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogError("TeleportTarget->TeleportDumbObjectHere: Target GameObject is null!");
            return;
        }

        Rigidbody body = obj.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = transform.position;
            if (matchTargetRotation)
                body.rotation = transform.rotation;
        }
        else
        {
            obj.transform.position = transform.position;
            if (matchTargetRotation)
                obj.transform.rotation = transform.rotation;
        }
        // update physics transforms immediately in case the object was teleported while mid-air or next to a wall, to prevent unintended collisions in the next frame.
        Physics.SyncTransforms();
    }
}
