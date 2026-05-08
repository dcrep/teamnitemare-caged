using UnityEngine;

// Important: Empty object -> prefab in Resources folder named "BootInitializer")
public class BootInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Load()
    {
        //Debug.Log("BootInitializer->Load()");
	    GameObject bootInit = GameObject.Instantiate(Resources.Load("BootInitializer")) as GameObject;
	    GameObject.DontDestroyOnLoad(bootInit);
        
        // !! IMPORTANT: Order of Initialization is important in case there's a dependency on another script !!
        GameObject playerPrefsObject = new("PlayerPreferences");
        playerPrefsObject.AddComponent<PlayerPreferences>();
        DontDestroyOnLoad(playerPrefsObject);
        Debug.Log("[BI]: PlayerPreferences initialized.");

        // AudioManager create object + script component (can also be done in Script
        // with RuntimeInitializeOnLoadMethod, but this way keeps it centralized)
        GameObject audioManager = new("AudioManager");
        audioManager.AddComponent<AudioManager>();
        //AudioManager am = audioManager.GetComponent<AudioManager>();
        AudioManager.uiAudioSourcesSO = Resources.Load<UIAudioSourcesSO>("UIAudioSourcesSO");
        DontDestroyOnLoad(audioManager);
        Debug.Log("[BI]: AudioManager initialized.");

        // GameManager create object + script component (can also be done in Script
        // with RuntimeInitializeOnLoadMethod, but this way keeps it centralized)
        GameObject gameManagerObject = new("GameManager");        
        gameManagerObject.AddComponent<GameManager>();
        //GameManager gm =gameManagerObject.GetComponent<GameManager>();        
        DontDestroyOnLoad(gameManagerObject);
        Debug.Log("[BI]: GameManager initialized..");

        GameState.scenesSO = Resources.Load<ScenesSO>("ScenesSO");

        // Input Manager
        GameObject inputManagerObject = new("InputManager");
        InputManager inputManager =inputManagerObject.AddComponent<InputManager>();
        DontDestroyOnLoad(inputManagerObject);
        Debug.Log("[BI]: InputManager initialized..");

		// GameManager - reference InputManager
        gameManagerObject.GetComponent<GameManager>().inputManager = inputManager;
    } 
}