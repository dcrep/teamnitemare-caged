using UnityEngine;

public class BirdCagePrimitive : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    private void FixedUpdate()
    {
        transform.Rotate(0f, rotationSpeed * Time.fixedDeltaTime, 0f, Space.Self);
    }
}
