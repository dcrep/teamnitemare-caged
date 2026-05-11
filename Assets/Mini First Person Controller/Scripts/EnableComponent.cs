using UnityEngine;

public class EnableComponent : MonoBehaviour
{
    private FirstPersonMovement playerController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = GetComponent<FirstPersonMovement>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (DialogueManager.GetInstance().dialogueIsPlaying)
        {
            GetComponent<FirstPersonMovement>().enabled = false;
        }
        else
        {
            GetComponent<FirstPersonMovement>().enabled = true;
        }
    }
}
