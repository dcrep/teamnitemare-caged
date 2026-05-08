using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowAttackSequence : MonoBehaviour
{
    [SerializeField] float timeBetweenAttackAndExpire = 3f;

    [SerializeField] List<lb_BirdExperiment> crowChildren = new List<lb_BirdExperiment>();
    [SerializeField] GameObject birbBeingAttacked;
    [SerializeField] Transform fleeTarget;
    [SerializeField] float timeBetweenCalls = 0.1f;

    [SerializeField] AudioClip geometryFallApartSound;

    [SerializeField] List<GameObject> worldGeometryToFallApart = new List<GameObject>();

    [SerializeField] BalanceController playerBalanceController;
    bool attackSequenceStarted = false;

    void Awake()
    {
        //lb_BirdExperiment[] children = GetComponentsInChildren<lb_BirdExperiment>();
        //crowChildren = new List<lb_BirdExperiment>(children);
        //System.Array.Copy(children, crowChildren, children.Length);
    }

    public void AttackSequenceStart()
    {
        if (attackSequenceStarted) return;
        attackSequenceStarted = true;
        foreach (lb_BirdExperiment crowChild in crowChildren)
        {
            crowChild.gameObject.SetActive(true);
        }
        Invoke(nameof(AttackSequenceContinue), timeBetweenAttackAndExpire);
    }
    void AttackSequenceContinue()
    {
        BirbDedify birbDedify = birbBeingAttacked.GetComponent<BirbDedify>();
        if (birbDedify != null)
        {
            birbDedify.DeBirdify();
        }

        if (fleeTarget == null)
        {
            Debug.LogWarning("CrowAttackSequence has no fleeTarget assigned. Crows will not flee toward a shared target.", this);
            attackSequenceStarted = false;
            return;
        }

        FleeTowardTransform(fleeTarget);

        attackSequenceStarted = false;

        Invoke(nameof(WorldGeometryFallApart), 4f);
        Invoke(nameof(PlayerFreefall), 5f);
    }

    public void FleeTowardTransform(Transform transform)
    {
        StartCoroutine(FleeTowardTransformCoroutine(transform));
    }

    IEnumerator FleeTowardTransformCoroutine(Transform transform)
    {
        foreach (lb_BirdExperiment crowChild in crowChildren)
        {
            // call each with a slight delay between them
            yield return new WaitForSeconds(timeBetweenCalls);
            crowChild.FleeTowardTransform(transform);
        }
    }

    void PlayerFreefall()
    {
        if (playerBalanceController != null)
        {
            playerBalanceController.Freefall(4f);
        }
    }

    void PlayGeometryFallApartSoundAgain()
    {
        if (geometryFallApartSound != null)
        {
            AudioManager.PlayOneShot(geometryFallApartSound, 0.7f);
        }
    }

    void WorldGeometryFallApart()
    {
        StartCoroutine(WorldGeometryFallApartCoroutine());
    }

    // move pieces of world geometry down and to random sides while also rotating them,
    //  to create the effect of the world falling apart after the attack sequence (no rigidbodies)
    IEnumerator WorldGeometryFallApartCoroutine()
    {
        List<Vector3> originalPositions = new List<Vector3>();
        List<Quaternion> originalRotations = new List<Quaternion>();
        List<Vector3> fallOffsets = new List<Vector3>();
        List<Vector3> rotationAxes = new List<Vector3>();
        List<float> rotationAngles = new List<float>();

        foreach (GameObject piece in worldGeometryToFallApart)
        {
            if (piece == null)
            {
                continue;
            }

            originalPositions.Add(piece.transform.position);
            originalRotations.Add(piece.transform.rotation);

            Vector3 sideDirection = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f));
            if (sideDirection.sqrMagnitude < 0.0001f)
            {
                sideDirection = Vector3.right;
            }

            float sideDistance = Random.Range(2.5f, 6f);
            float fallDistance = Random.Range(5f, 10f);
            fallOffsets.Add(sideDirection.normalized * sideDistance + Vector3.down * fallDistance);

            Vector3 axis = Random.onUnitSphere;
            if (axis.sqrMagnitude < 0.0001f)
            {
                axis = Vector3.up;
            }
            rotationAxes.Add(axis.normalized);
            rotationAngles.Add(Random.Range(180f, 540f));
        }

        if (originalPositions.Count == 0)
        {
            yield break;
        }
        if (geometryFallApartSound != null)
        {
            AudioManager.PlayOneShot(geometryFallApartSound, 0.7f);
            Invoke(nameof(PlayGeometryFallApartSoundAgain), 0.8f);
        }

        float duration = 5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            int pieceIndex = 0;
            for (int i = 0; i < worldGeometryToFallApart.Count; i++)
            {
                GameObject piece = worldGeometryToFallApart[i];
                if (piece == null)
                {
                    continue;
                }

                piece.transform.position = originalPositions[pieceIndex] + fallOffsets[pieceIndex] * easedT;
                piece.transform.rotation = originalRotations[pieceIndex] * Quaternion.AngleAxis(rotationAngles[pieceIndex] * easedT, rotationAxes[pieceIndex]);
                pieceIndex++;
            }

            yield return null;
        }
    }
}
