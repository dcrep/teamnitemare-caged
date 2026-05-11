using Unity.VisualScripting;
using UnityEngine;

public class DoorRotate : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private float rotationSpeed = 30f; // degrees per second
    [SerializeField] private float openAngle = 90f; // target rotation angle in degrees
    [SerializeField] private float closedAngle = 0f; // target rotation angle in degrees
    private bool isRotating = false;

    public void Awake()
    {
        if (door == null)
        {
            door = this.gameObject;
        }
    }
    public void OpenDoor()
    {
        if (!isRotating)
        {
            isRotating = true;
            StartCoroutine(RotateDoor(openAngle));
        }
    }
    public void CloseDoor()
    {
        if (!isRotating)
        {
            isRotating = true;
            StartCoroutine(RotateDoor(closedAngle));
        }
    }

    private System.Collections.IEnumerator RotateDoor(float targetAngle)
    {
        float currentAngle = door.transform.localEulerAngles.y;
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        Debug.Log($"Starting rotation. Current angle: {currentAngle}, Target angle: {targetAngle}, Angle difference: {angleDifference}");
        while (Mathf.Abs(angleDifference) > 0.1f)
        {
            Debug.Log($"Rotating... Current angle: {currentAngle}, Target angle: {targetAngle}, Angle difference: {angleDifference}");
            float step = rotationSpeed * Time.deltaTime;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);
            door.transform.localEulerAngles = new Vector3(door.transform.localEulerAngles.x, newAngle, door.transform.localEulerAngles.z);
            currentAngle = newAngle;
            angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            yield return null;
        }

        // Ensure the door is exactly at the target angle at the end
        door.transform.localEulerAngles = new Vector3(door.transform.localEulerAngles.x, targetAngle, door.transform.localEulerAngles.z);
        isRotating = false;
    }
}
