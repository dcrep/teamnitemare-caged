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
        // if (animationManager == null)
        // {
        //     animationManager = gameObject.GetComponent<AnimationManager>();
        //     if (animationManager == null)
        //     {
        //         animationManager = gameObject.AddComponent<AnimationManager>();
        //     }
        // }
        if (UICanvas == null)
        {
            UICanvas = GameObject.Find("Canvas");
            if (UICanvas == null)
            {
                Debug.LogError("UI->Canvas not found for UIManager!");
            }
        }
        pauseMenuPrefab = Resources.Load<GameObject>("Prefabs/" + "PauseModalDialog");
        if (pauseMenuPrefab == null)
        {
            Debug.Log("UI->Pause menu prefab not found!");
            return;
        }
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
                var canvas = UICanvas;  //GameObject.Find("Canvas");
                if (canvas == null)
                {
                    Debug.LogError("UI->Canvas not found for Pause Menu!");
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
