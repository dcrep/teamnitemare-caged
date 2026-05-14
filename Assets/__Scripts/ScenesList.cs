using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

// Good info:
// sceneCount, loadedSceneCount, GetSceneAt(), etc etc
// https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.SceneManager.html
public class ScenesList : MonoBehaviour
{
    [SerializeField] private List<SceneSerializer> scenes = new List<SceneSerializer>();

    private string currentSceneName = "";
    private string sceneBeingLoaded = "";
    private float sceneLoadStartTime, sceneUnloadStartTime;

    private GameObject currentEventSystem;
    private GameObject currentCamera;
    private AudioListener currentAudioListener;
    private GameObject currentCanvas;


    void Awake()
    {
        // find current event system and main camera in the scene
        currentEventSystem = FindFirstObjectByType<EventSystem>()?.gameObject;
        if (currentEventSystem == null)
        {
            Debug.LogWarning("ScenesList->Awake: No EventSystem found in the scene");
        }
        currentAudioListener = FindFirstObjectByType<AudioListener>();
        if (currentAudioListener == null)
        {
            Debug.LogWarning("ScenesList->Awake: No AudioListener found in the scene");
        }
        currentCamera = Camera.main != null ? Camera.main.gameObject : null;
        if (currentCamera == null)
        {
            // try find by component
            Camera cam = FindFirstObjectByType<Camera>();
            if (cam != null)            
            {
                currentCamera = cam.gameObject;
            }
            else
            {
                Debug.LogWarning("ScenesList->Awake: No main camera found in the scene");
            }
        }
        currentCanvas = FindFirstObjectByType<Canvas>()?.gameObject;
        if (currentCanvas == null)
        {
            Debug.LogWarning("ScenesList->Awake: No Canvas found in the scene");
        }
        currentSceneName = SceneManager.GetActiveScene().name;
    }

    

    public void LoadSceneAsyncByIndex(int index)
    {
        if (index < 0 || index >= scenes.Count)
        {
            Debug.LogError("LoadSceneByIndex: Invalid scene index: " + index);
            return;
        }
        LoadSceneAsync(scenes[index]);
    }

    public void LoadSceneAsync(SceneSerializer scene)
    {
        if (scene == null)
        {
            Debug.LogError("LoadSceneAsync: SceneSerializer is null");
            return;
        }
        sceneBeingLoaded  = scene.SceneName;
        
        Debug.Log("Loading scene asynchronously: " + sceneBeingLoaded);
        sceneLoadStartTime = Time.time;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneBeingLoaded, LoadSceneMode.Additive);
        if (asyncLoad == null)
        {
            Debug.LogError("LoadSceneAsync: Failed to load scene (not found or not in Build settings): " + sceneBeingLoaded);
            return;
        }
        // this prevents the scene from becoming active until we specifically set it as such
        // HOWEVER, this also means that the .completed callback won't trigger because progress never exceeds 0.9f
        asyncLoad.allowSceneActivation = false;
        // the following is only useful if alloweSceneActivation is true (otherwise it will fail to trigger)
        //asyncLoad.completed += OnSceneLoaded;

        // since allowSceneActivation is false, we must run a Coroutine to check when the scene load progress is >= 0..9f
        StartCoroutine(WaitForSceneLoad(asyncLoad, false));
    }

    // Another unique approach:
    //Object.Instantiate(prefab, position, rotation, SceneManager.GetSceneByName("TargetScene").GetRootGameObjects()[0].transform);
    // requires a prefab, positioning, rotation and combines the objects into the new scene

    private IEnumerator WaitForSceneLoad(AsyncOperation asyncLoad, bool allowSceneActivation)
    {
        yield return null; // wait one frame before starting to check progress

        // Hasn't finished (won't if allowSceneActivation is false)
        while (!asyncLoad.isDone)
        {
            // load progress info
            Debug.Log("Scene loading progress: " + (asyncLoad.progress * 100) + "%");
        
            // Has it REALLY finished (if allowSceneActivation is false)?
            if (asyncLoad.progress >= 0.9f)
            {
                //exit loop
                break;
            }
            // wait for 1/10th of a second
            yield return new WaitForSeconds(0.1f);
            // wait for next frame
            //yield return null;
        }
        Debug.Log("Scene loaded in: " + (Time.time - sceneLoadStartTime) + " seconds");

        if (!allowSceneActivation)
        {
            asyncLoad.allowSceneActivation = true;
        }

        // disable current EventSystem and camera to avoid conflicts
        DisableCurrentSystems();
        // now allow the scene to activate. This adds it but doesn't set it as Active (confusing)
        // we are using faster progress checking this time
        if (!allowSceneActivation)
        {
            asyncLoad.allowSceneActivation = true;
            while (!asyncLoad.isDone)
            {
                // load progress info
                //Debug.Log("Scene loading progress: " + (asyncLoad.progress * 100) + "%");
                // wait until end of frame to check again
                yield return new WaitForEndOfFrame();
            }
        }
        // set the newly loaded scene as active
        UnityEngine.SceneManagement.Scene loadedScene = SceneManager.GetSceneByName(sceneBeingLoaded);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
            Debug.Log("Set active scene to: " + sceneBeingLoaded);
        }
        else
        {
            Debug.LogError("WaitForSceneLoad: Loaded scene is not valid: " + sceneBeingLoaded);
        }
        CameraDisable();
        // now unload in 10 seconds (just testing)
        Invoke(nameof(UnloadAddedSceneAsync), 10f);
    }

    private void OnSceneLoaded(AsyncOperation operation)
    {
        Debug.Log("Scene loaded: " + sceneBeingLoaded + " operation info: " + operation.isDone);
         if (operation.isDone)
         {
             // set the newly loaded scene as active
             UnityEngine.SceneManagement.Scene loadedScene = SceneManager.GetSceneByName(sceneBeingLoaded);
             if (loadedScene.IsValid())
             {
                 SceneManager.SetActiveScene(loadedScene);
                 Debug.Log("OnSceneLoaded->Set active scene to: " + sceneBeingLoaded);
                 DisableCurrentSystems();
                 CameraDisable();
             }
             else
             {
                 Debug.LogError("OnSceneLoaded: Loaded scene is not valid: " + sceneBeingLoaded);
             }
         }
         else
         {
             Debug.LogError("OnSceneLoaded: Scene loading operation is not done yet for scene: " + sceneBeingLoaded);
         }
        Debug.Log("Scene loaded: " + sceneBeingLoaded + " operation info: " + operation);
        Debug.Log("Scene loaded in: " + (Time.time - sceneLoadStartTime) + " seconds");

        // now unload in 10 seconds (just testing)
        Invoke(nameof(UnloadAddedSceneAsync), 10f);
    }

    private void FindAndDisableDuplicateSystems()
    {
        // find EventSystem, main Camera, and Canvas that don't match our already assigned ones and disable them to avoid conflicts
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        EventSystem newEventSystem = null;
        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] != currentEventSystem)
            {
                Debug.Log("Found new EventSystem in loaded scene: " + eventSystems[i].name + " disabling it to avoid conflicts with current EventSystem: " + currentEventSystem.name);
                newEventSystem = eventSystems[i];
                break;
            }
        }
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        Camera newCamera = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].gameObject != currentCamera)
            {
                Debug.Log("Found new Camera in loaded scene: " + cameras[i].name + " disabling it to avoid conflicts with current Camera: " + currentCamera.name);
                newCamera = cameras[i];
                break;
            }
        }
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas newCanvas = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject != currentCanvas)
            {
                Debug.Log("Found new Canvas in loaded scene: " + canvases[i].name + " disabling it to avoid conflicts with current Canvas: " + currentCanvas.name);
                newCanvas = canvases[i];
                break;
            }
        }
        newEventSystem.enabled = false;
        newCamera.gameObject.SetActive(false);
        newCanvas.gameObject.SetActive(false);
    }
    private void DisableCurrentSystems()
    {
        if (currentEventSystem != null)
        {
            currentEventSystem.SetActive(false);
        }
        if (currentAudioListener != null)
        {
            currentAudioListener.enabled = false;
        }
        // if (currentCamera != null)
        // {
        //     currentCamera.SetActive(false);
        // }
        if (currentCanvas != null)
        {
            currentCanvas.SetActive(false);
            GameManager.Instance.uiManager.ResetCanvas();
        }
        // Start Coroutine to switch off camera next frame
        //StartCoroutine(CameraDisable());
    }
    private void CameraDisable()
    {
        //
        if (currentCamera != null)
        {
            currentCamera.SetActive(false);
        }
    }
    private void ReEnableCurrentSystems()
    {
        if (currentEventSystem != null)
        {
            currentEventSystem.SetActive(true);
        }
        if (currentAudioListener != null)
        {
            currentAudioListener.enabled = true;
        }
        if (currentCamera != null)
        {
            currentCamera.SetActive(true);
        }
        if (currentCanvas != null)
        {
            currentCanvas.SetActive(true);
            GameManager.Instance.uiManager.ResetCanvas();
        }
    }

    public void UnloadSceneAsyncByIndex(int index)
    {
        if (index < 0 || index >= scenes.Count)
        {
            Debug.LogError("UnloadSceneAsyncByIndex: Invalid scene index: " + index);
            return;
        }
        // check if scene is loaded before trying to unload
        if (!SceneManager.GetSceneByName(scenes[index].SceneName).isLoaded)
        {
            Debug.LogWarning("UnloadSceneAsyncByIndex: Scene is not loaded, cannot unload: " + scenes[index].SceneName);
            return;
        }
        UnloadAddedSceneAsync(scenes[index].SceneName);
    }

    public void UnloadAddedSceneAsync()
    {
        if (string.IsNullOrEmpty(sceneBeingLoaded))
        {
            Debug.LogError("UnloadSceneAsync: Scene name is null or empty");
            return;
        }
        string scene = sceneBeingLoaded;
        sceneBeingLoaded = "";
        UnloadAddedSceneAsync(scene);
    }
    public void UnloadAddedSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("UnloadSceneAsync: Scene name is null or empty");
            return;
        }
        Debug.Log("Unloading scene asynchronously: " + sceneName);
        sceneUnloadStartTime = Time.time;
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
        if (asyncUnload == null)
        {
            Debug.LogError("UnloadSceneAsync: Failed to unload scene (not found or not loaded): " + sceneName);
            return;
        }
        asyncUnload.completed += OnSceneUnloaded;
    }

    private void OnSceneUnloaded(AsyncOperation operation)
    {
        Debug.Log("Scene unloaded: " + sceneBeingLoaded + " operation info: " + operation);
        Debug.Log("Scene unloaded in: " + (Time.time - sceneUnloadStartTime) + " seconds");
        sceneBeingLoaded = "";
        // re-enable current EventSystem and camera
        ReEnableCurrentSystems();
        // set the newly loaded scene as active
        UnityEngine.SceneManagement.Scene previousScene = SceneManager.GetSceneByName(currentSceneName);
        if (previousScene.IsValid())
        {
            SceneManager.SetActiveScene(previousScene);
            Debug.Log("Set active scene back to: " + currentSceneName);
        }
        else
        {
            Debug.LogError("OnSceneUnloaded: Loaded scene is not valid: " + currentSceneName);
        }
    }

}
