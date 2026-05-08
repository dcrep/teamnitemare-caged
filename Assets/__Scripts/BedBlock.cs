using UnityEngine;

public class BedBlock : MonoBehaviour
{
    private bool playerInRange;

    [SerializeField] private GameObject prompt;

    [SerializeField] private GameObject chore1;

    [SerializeField] private GameObject chore2;

    [SerializeField] private GameObject chore3;

    [SerializeField] private GameObject sleepTrigger;

   

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //prompt.gameObject.SetActive(true);
            //playerInRange = true;

            if (!chore1.activeInHierarchy && !chore2.activeInHierarchy && !chore3.activeInHierarchy)
            {
                Debug.Log("All chores finished!");
                sleepTrigger.gameObject.SetActive(true);

                this.enabled = false;
            }
            else
            { 
                prompt.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
       

        if (other.gameObject.CompareTag("Player"))
        {
            prompt.gameObject.SetActive(false);
        }
    }
}
