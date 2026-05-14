using UnityEngine;

public class UIManager : MonoBehaviour
{
    GameObject pauseMenuPrefab = null;
    GameObject pauseMenuInstance = null;
    bool pauseMenuOpen = false;

    public GameObject UICanvas;

    //public AnimationManager animationManager;

    void Awake()
    {
        // Defer canvas resolution to a safe helper so UIManager works even when
        // the Canvas is created later in the scene lifecycle. We still try to
        // load the prefab here.
        pauseMenuPrefab = Resources.Load<GameObject>("Prefabs/" + "PauseModalDialog");
        if (pauseMenuPrefab == null)
        {
            Debug.Log("UI->Pause menu prefab not found!");
            return;
        }
    }

    // Try to find an existing Canvas GameObject. Do NOT create one here; return
    // null if none found. Caller (e.g. PauseMenuOpen) should handle absence.
    GameObject GetCanvasIfExists()
    {
        if (UICanvas != null)
            return UICanvas;

        Canvas found = Object.FindFirstObjectByType<Canvas>();
        if (found != null)
        {
            UICanvas = found.gameObject;
            return UICanvas;
        }

        // Do not search by name (unsafe). If no Canvas component exists, signal caller to handle it.
        return null;
    }
    public void ResetCanvas()
    {
        UICanvas = null;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // called from GameManager when PauseGame is triggered
    public bool PauseMenuClose()
    {
        if (pauseMenuOpen)
        {
            pauseMenuInstance.SetActive(false);
            Destroy(pauseMenuInstance);
            pauseMenuInstance = null;
            Debug.Log("UI->Pause menu closed!");
            pauseMenuOpen = false;
        }
        return true;
    }
    // called from GameManager when PauseGame is triggered
    public bool PauseMenuOpen()
    {
        if (pauseMenuOpen)
        {
            return true;    // previously toggleable: with PauseMenuClose(); (but should call GameManager.Instance.ResumeGame())
        }
        else if (GameManager.Instance.gameState.currentGameState == GameStates.Playing)
        {
            Debug.Log("UI->Pause triggered!");
            if (pauseMenuPrefab != null)
            {
                pauseMenuInstance = Instantiate(pauseMenuPrefab, Vector3.zero, Quaternion.identity);
                if (pauseMenuInstance == null)
                {
                    Debug.LogError("UI->Pause menu prefab not found!");
                    return false;
                }
                var canvas = GetCanvasIfExists();
                if (canvas == null)
                {
                    Debug.LogError("UI->Canvas not found for Pause Menu! Aborting open — ensure a Canvas exists in the scene before opening the pause menu.");
                    return false;
                }
                pauseMenuInstance.transform.SetParent(canvas.transform, false);
                pauseMenuInstance.SetActive(true);

                // This is what calls this function
                //GameManager.Instance.PauseGame();
                pauseMenuOpen = true;
            }
        }
        return pauseMenuOpen;
    }
}
