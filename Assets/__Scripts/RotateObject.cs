using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationX = 0f;
    [SerializeField] private float rotationY = 10f;
    [SerializeField] private float rotationZ = 0f;

    private void FixedUpdate()
    {
        transform.Rotate(rotationX * Time.fixedDeltaTime, rotationY * Time.fixedDeltaTime, rotationZ * Time.fixedDeltaTime, Space.Self);
    }
}
