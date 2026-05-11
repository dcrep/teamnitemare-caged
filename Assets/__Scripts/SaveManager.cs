
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager
{
    public static SaveManager Instance { get; private set; }

    private Dictionary<string, ISaveable> sceneSaveables =
        new Dictionary<string, ISaveable>();

    private Dictionary<string, object> temporarySceneState;


    private PlayerState playerState;
    private GameState gameState;

    public void Initialize(PlayerState playerState, GameState gameState)
    {
        this.playerState = playerState;
        this.gameState = gameState;
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of SaveManager detected. Destroying new instance.");
            return;
        }
        Instance = this;
    }

    // Called by ISaveable objects in the Scene OnEnable to register themselves with the SaveManager
    public void Register(string id, ISaveable saveable)
    {
        sceneSaveables[id] = saveable;
        //Debug.Log("Registered ISaveable with ID: " + id);
    }

#region Capture to File
    public SaveFile Capture()
    {
        var file = new SaveFile();

        // Global state
        file.playerState = playerState;
        file.gameState = gameState;

        // Scene state
        string sceneName = SceneManager.GetActiveScene().name;
        var sceneDict = new Dictionary<string, object>();

        foreach (var kvp in sceneSaveables)
            sceneDict[kvp.Key] = kvp.Value.CaptureState();

        file.sceneStates[sceneName] = sceneDict;

        return file;
    }


    public void Restore(SaveFile file)
    {
        // Global state
        playerState = file.playerState;
        gameState = file.gameState;

        // Scene state
        string sceneName = SceneManager.GetActiveScene().name;

        if (!file.sceneStates.TryGetValue(sceneName, out var sceneDict))
            return;

        foreach (var kvp in sceneSaveables)
        {
            if (sceneDict.TryGetValue(kvp.Key, out var state))
                kvp.Value.RestoreState(state);
        }
    }
#endregion Capture to File

#region Capture Temporary State (for Hub exit/return)
    public void CaptureTemporarySceneState()
    {
        temporarySceneState = new Dictionary<string, object>();

        foreach (var kvp in sceneSaveables)
        {
            temporarySceneState[kvp.Key] = kvp.Value.CaptureState();
        }
    }

    public void RestoreTemporarySceneState()
    {
        if (temporarySceneState == null)
            return;

        Debug.Log("SM->RestoreTemporarySceneState: Restoring temporary state for " + temporarySceneState.Count + " saveables.");

        // create a copy of sceneSaveables to avoid modifying the collection while iterating
        var saveablesCopy = new Dictionary<string, ISaveable>(sceneSaveables);

        foreach (var kvp in saveablesCopy)
        {
            if (temporarySceneState.TryGetValue(kvp.Key, out var state))
            {
                kvp.Value.RestoreState(state);
            }
        }
        ClearTemporarySceneState();
    }

    public void ClearTemporarySceneState()
    {
        temporarySceneState = null;
    }
#endregion Capture Temporary State
}
