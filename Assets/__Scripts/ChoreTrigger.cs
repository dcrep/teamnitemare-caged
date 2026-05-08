using UnityEngine;
using TMPro;
using UnityEngine.Rendering;

public class ChoreTrigger : MonoBehaviour
{
    public static int NumberOfChoresCompleted { get; private set; } = 0;
    private bool playerInRange;
   
    [SerializeField] private GameObject prompt;

    [SerializeField] private GameObject choreObject;

    [SerializeField] private GameObject choreSound;

    [SerializeField] private GameObject fade;

    [SerializeField] DormTransitionToNight dormTransition;

    void Awake()
    {
        if (dormTransition == null)
        {
            dormTransition = FindObjectsByType<DormTransitionToNight>(FindObjectsSortMode.None)[0];
        }
    }

    void Update()
    {
        if (playerInRange)
        {

            if (Input.GetKeyDown(KeyCode.E))
            {
                //question.gameObject.SetActive(true);
                prompt.gameObject.SetActive(false);
                fade.gameObject.SetActive(true);
                choreSound.gameObject.SetActive(true);
                Invoke(nameof(ClearChore), 1f);
            }
        }
        else
        {
            //question.gameObject.SetActive(false);
        }
    }

    void ClearChore()
    {
        choreObject.gameObject.SetActive(false);
        NumberOfChoresCompleted++;
        if (NumberOfChoresCompleted == 3)
        {
            dormTransition.TransitionToNight();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            prompt.gameObject.SetActive(true);
            fade.gameObject.SetActive(false);
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        prompt.gameObject.SetActive(false);

        if (other.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
