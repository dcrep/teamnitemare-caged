using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowChildrenPropagateCalls : MonoBehaviour
{
    [SerializeField] float timeBetweenCalls = 0.1f;

    lb_BirdExperiment[] crowChildren = new lb_BirdExperiment[0];

    void Awake()
    {
        lb_BirdExperiment[] children = GetComponentsInChildren<lb_BirdExperiment>();
        crowChildren = new lb_BirdExperiment[children.Length];
        System.Array.Copy(children, crowChildren, children.Length);
    }

    public void FleeTowardLocation(Vector3 position)
    {
        StartCoroutine(FleeTowardLocationCoroutine(position));
    }

    public void FleeTowardTransform(Transform transform)
    {
        StartCoroutine(FleeTowardTransformCoroutine(transform));
    }

    IEnumerator FleeTowardLocationCoroutine(Vector3 position)
    {
        foreach (lb_BirdExperiment crowChild in crowChildren)
        {
            // call each with a slight delay between them
            yield return new WaitForSeconds(timeBetweenCalls);
            crowChild.FleeTowardLocation(position);
        }
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
}
