using UnityEngine;
using System;
using System.Collections.Generic;

public class BirbGangEscape : MonoBehaviour
{
    int requiredBirbs = 3; // Number of birbs required to trigger escape
    bool escapeTriggered = false; // To prevent multiple triggers

    public UnityEngine.Events.UnityEvent onAllBirbsEntered;
    public UnityEngine.Events.UnityEvent onNotEnoughBirbs;

    void Awake()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (escapeTriggered) return; // Already triggered, do nothing

        Debug.Log("Collider entered by: " + other.gameObject.name + " with tag: " + other.gameObject.tag);

        if (other.CompareTag("Player"))
        {
            if (Follow.numberOfFollowers >= requiredBirbs)
            {
                TriggerEscape();
            }
            else
            {
                onNotEnoughBirbs.Invoke();
            }
        }
    }

    void TriggerEscape()
    {
        escapeTriggered = true;
        Debug.Log("Escape triggered! All birbs have entered the collider.");
        onAllBirbsEntered.Invoke();
        // Add your escape logic here (e.g., open a door, start a cutscene, etc.)
    }
}
