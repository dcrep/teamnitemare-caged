using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseScript : MonoBehaviour
{
    [SerializeField] private Button backButton;
    EventSystem eventSystem;

    public void Awake()
    {
        eventSystem = EventSystem.current;
        if (eventSystem != null && backButton != null)
        {
            eventSystem.SetSelectedGameObject(backButton.gameObject);
        }
    }
    public void ResumeGamePressed()
    {
        Debug.Log("Resume menu button!");
        //GameManager.Instance.uiManager.PauseMenuClose();
        //var inputManager = GameObject.FindFirstObjectByType<InputManager>();
        //if (inputManager != null)
        //{            
         //   inputManager.GetComponent<InputManager>().PauseMenuClose();
        //}
        AudioManager.PlayDialogueButtonPressAudioClip();
        GameManager.Instance.ResumeGame();
    }
    public void MainMenuButtonPressed()
    {
        Debug.Log("Main menu button!");
        //GameManager.Instance.UnpauseAndRestoreCursor();
        //GameManager.Instance.ResumeGame();
        // LoadScene detects and does this:
        //GameManager.Instance.ClosePauseMenuAndResumeTime();
        AudioManager.PlayDialogueButtonCancelAudioClip();
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }
}
