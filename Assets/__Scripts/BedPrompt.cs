using UnityEngine;
using UnityEngine.Rendering;

public class BedPrompt : MonoBehaviour
{

    private bool playerInRange;

    [SerializeField] private GameObject chore1;

    [SerializeField] private GameObject chore2;

    [SerializeField] private GameObject chore3;
    
    [SerializeField] private GameObject sleepTrigger;

    [SerializeField] private GameObject noSleepTrigger;

    [SerializeField] private GameObject prompt;

    [SerializeField] private GameObject visualCue;


    public void Start()
    {
    }

    public void ActivateObject()
    {
        if (chore1.activeSelf != true && chore2.activeSelf != true && chore3.activeSelf != true)
        {
            noSleepTrigger.SetActive(false);
            sleepTrigger.SetActive(true);
            prompt.SetActive(false);
            visualCue.SetActive(true);
        }
        else
        {
            noSleepTrigger.SetActive(true);
            sleepTrigger.SetActive(false);
            prompt.SetActive(true);
            visualCue.SetActive(false);
        }
    }


}
