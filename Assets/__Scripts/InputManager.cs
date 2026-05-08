using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public InputSystem_Actions playerControls;
    //private InputAction numKeyAction;
    //private InputAction mouseUpDown;
    //private InputAction mouseRightClickAction;
    private InputAction mouseWheelAction;

    private InputAction numKeyAction;

    private InputAction pauseAction;
    private InputAction quitAction;
    private InputAction reloadAction;

    //public PlayerX activePlayer = null;

    void Awake()
    {
        playerControls = new InputSystem_Actions();
    }
    void OnEnable()
    {
        // Enable ALL Player Controls on the new InputSystem (optional if we only use specific subsets)
        playerControls.Enable();
        
        // mouseRightClickAction = playerControls.Player.RightClick;
        // mouseRightClickAction.Enable();
        // mouseRightClickAction.performed += MouseRightButtonPressed;
        // mouseRightClickAction.canceled += MouseRightButtonReleased;

        mouseWheelAction = playerControls.Player.ScrollWheel;
        mouseWheelAction.performed += MouseWheelScrolled;
        mouseWheelAction.Enable();

        numKeyAction = playerControls.Player.NumKeys;
        numKeyAction.Enable();
        numKeyAction.performed += NumKeyPressed;

        pauseAction = playerControls.Player.Pause;
        pauseAction.Enable();
        pauseAction.performed += PausePressed;

        quitAction = playerControls.Player.Quit;
        quitAction.Enable();
        quitAction.performed += QuitPressed;

        reloadAction = playerControls.Player.Reload;
        reloadAction.Enable();
        reloadAction.performed += ReloadPressed;

    }
    void OnDisable()
    {
        reloadAction.performed -= ReloadPressed;
        reloadAction.Disable();

        quitAction.performed -= QuitPressed;
        quitAction.Disable();

        pauseAction.performed -= PausePressed;
        pauseAction.Disable();

        numKeyAction.performed -= NumKeyPressed;
        numKeyAction.Disable();        

        mouseWheelAction.performed -= MouseWheelScrolled;
        mouseWheelAction.Disable();

        // mouseRightClickAction.performed -= MouseRightButtonPressed;
        // mouseRightClickAction.canceled -= MouseRightButtonReleased;
        // mouseRightClickAction.Disable();

        // Disable ALL player Controls on the new InputSystem
        playerControls.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        if (playerControls.Player.Mute.triggered)
        {
            //GameManager.Instance.MuteGame();
            AudioManager.MuteToggle();
        }
        else if (playerControls.Player.Quit.triggered)
        {
            //Debug.Log("Quit triggered!");
            //GameManager.Quit();
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            //UIManager.StartUITestTwoPlayer();
        }
    }

    private void PausePressed(InputAction.CallbackContext context)
    {
        Debug.Log("Esc/{p}ause triggered!");
        if (GameManager.Instance.gameState.currentGameState == GameStates.Playing)
        {
            //PauseMenuClose();
            GameManager.Instance.PauseGame();
        }
        else if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            //PauseMenuOpen();
            GameManager.Instance.ResumeGame();
        }
    }
    private void QuitPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Quit triggered!");
        GameManager.Quit();
    }

    private void ReloadPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Reload triggered!");
        //GameManager.Instance.ReloadCurrentScene();
        var sceneScript = GameManager.Instance.gameState.currentSceneScript;
        if (sceneScript != null)
        {
            sceneScript.ReloadScene();
        }
        else
        {
            Debug.LogWarning("ReloadPressed: No currentSceneScript found in GameState to reload scene!");
        }
    }

    void MouseRightButtonPressed(InputAction.CallbackContext context)
    {
        //Debug.Log("Right Mouse Button Pressed");
        if (GameManager.Instance.gameState.currentGameState != GameStates.Playing || GameManager.Instance.IsModalDialogueActive())
            return;
    }
    
    void MouseRightButtonReleased(InputAction.CallbackContext context)
    {
        // Ignore for now..
    }

    private void MouseWheelScrolled(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.gameState.currentGameState != GameStates.Playing || GameManager.Instance.IsModalDialogueActive())
            return;
        //Debug.Log("Mouse Wheel scrolled!");
        float scrollValue = context.ReadValue<float>();
        if (Math.Abs(scrollValue) < 0.01)
            return; // Ignore small scroll values

        //Debug.Log("Mouse Wheel scrolled with value: " + scrollValue);

        /*
        // TODO: change? Doesn't seem all that noticeable
        // ! Maybe move this into Update() checks instead of Input event system due to EventSystem last-frame warning
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Mouse Wheel scrolled over UI! [maybe should unsubscribe from event and check in Update() loop because of EventSystem last-frame error]");
            return; // Ignore if mouse is over UI element
        }
        GameManager.Instance.cameraMovement.CameraZoomOnUpdate(scrollValue / 120);
        */
    }

    private void NumKeyPressed(InputAction.CallbackContext context)
    {
        //Debug.Log("NumKey pressed!");
        
        // IMPORTANT! The NumKeys in PlayerControl must be ordered 0 through 9 then Numpad 0-9
        // 0 - 19. 0-9 are top of the keyboard, 10 - 19 are numpad
        int keyValue = context.action.GetBindingIndexForControl(context.control);
        
        // NumPad key?
        if (keyValue > 9)
        {
            keyValue -= 10;
            //numpadKeyPressed = true;
        }
        else
        {
            //numpadKeyPressed = false;
        }

        //Debug.Log("NumKey pressed: " + keyValue);

        if (GameManager.Instance.gameState.currentGameState == GameStates.Playing && !GameManager.Instance.IsModalDialogueActive())
        {
            switch (keyValue)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
                case 8:
                    break;
                case 9:
                    break;                
                default:
                    Debug.LogWarning("Unhandled NumKey pressed: " + keyValue);
                    break;
            }
        }
    }
}
