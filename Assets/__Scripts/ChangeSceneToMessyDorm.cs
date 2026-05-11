using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeSceneToMessyDorm : MonoBehaviour
{
    private bool playerInRange;

    [SerializeField] private GameObject prompt;

    [SerializeField] private GameObject fade;


    void Update()
    {
        if (playerInRange)
        {

            if (Input.GetKeyDown(KeyCode.E))
            {

                prompt.gameObject.SetActive(false);
                fade.gameObject.SetActive(true);
                SceneManager.LoadScene("Dialogue System 2");
            }
        }
        else
        {
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
