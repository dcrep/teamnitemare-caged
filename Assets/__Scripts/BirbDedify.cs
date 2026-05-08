using UnityEngine;
using System.Collections;

public class BirbDedify : MonoBehaviour
{
    [Header("Feathers")]
    [SerializeField] GameObject featherEmitterPrefab;
    [SerializeField] float featherEmitterLifetime = 4.5f;
    [SerializeField] Vector3 featherEmitterPositionOffset = Vector3.zero;
    [SerializeField] Vector3 featherEmitterRotationOffset = Vector3.zero;

    [Header("Rotation")]
    [SerializeField] float rotationDelay = 3f;
    [SerializeField] float rotationDuration = 1f;
    [SerializeField] float targetZRotation = 90f;

    bool isDeBirdifying;
    
    public void DeBirdify()
    {
        if (isDeBirdifying)
            return;

        StartCoroutine(DeBirdifyRoutine());
    }

    private IEnumerator DeBirdifyRoutine()
    {
        isDeBirdifying = true;

        if (rotationDelay > 0f)
            yield return new WaitForSeconds(rotationDelay);

        Vector3 startEuler = transform.eulerAngles;
        Vector3 endEuler = new Vector3(startEuler.x, startEuler.y, targetZRotation);

        float duration = Mathf.Max(0.01f, rotationDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.eulerAngles = new Vector3(
                Mathf.LerpAngle(startEuler.x, endEuler.x, t),
                Mathf.LerpAngle(startEuler.y, endEuler.y, t),
                Mathf.LerpAngle(startEuler.z, endEuler.z, t));

            yield return null;
        }

        transform.eulerAngles = endEuler;

        if (featherEmitterPrefab != null)
        {
            SpawnFeatherEmitterAtExactPosition(transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }

    private GameObject SpawnFeatherEmitterAtExactPosition(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (featherEmitterPrefab == null)
        {
            return null;
        }

        Vector3 finalPosition = worldPosition + worldRotation * featherEmitterPositionOffset;
        Quaternion finalRotation = worldRotation * Quaternion.Euler(featherEmitterRotationOffset);
        GameObject emitter = Instantiate(featherEmitterPrefab, finalPosition, finalRotation);

        float safeLifetime = Mathf.Max(0.05f, featherEmitterLifetime);
        Destroy(emitter, safeLifetime);
        return emitter;
    }

}
