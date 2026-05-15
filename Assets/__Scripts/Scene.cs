using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Unity.VisualScripting;
using UnityEngine.UI;

// TODO: 'hubconnectedscene' minigames (going IN to a minigame scene, returing BACK to this scene)
// TODO: Restart should reset tasks/quests, which means using visit # in GameState
// (or just have 1 visit)
// TODO: Work with mutiple scenes (additive loaded)
// Good info:
// sceneCount, loadedSceneCount, GetSceneAt(), etc etc
// https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.SceneManager.html

[Serializable]
class SceneVisitTasks
{
    public int visitCount = 0;
    [SerializeField] public UnityEvent onSceneAwake;
    [SerializeField] public UnityEvent onSceneStart;
    [SerializeField] public bool useIfVisitCountExceeded = true; // if true, will use this SceneVisitTasks if visitCount exceeds the specified visitCount (instead of doing nothing)
}

// This script is mandatory to attach to each Scene. Easiest is to attach it to the Main Camera
// It communicates with the GameManager currently
[DefaultExecutionOrder(-100)]
public class Scene : MonoBehaviour
{
    [Header("Scene Visit Groups")]
    [SerializeField] List<SceneVisitTasks> sceneVisitTasks = new List<SceneVisitTasks>()
    {
        new SceneVisitTasks { visitCount = 1, onSceneAwake = new UnityEvent(), onSceneStart = new UnityEvent() }
    };
    [SerializeField] SceneVisitTasks onHubReturnTasks = new SceneVisitTasks() { visitCount = -1, onSceneAwake = new UnityEvent(), onSceneStart = new UnityEvent() };

    [SerializeField] SceneVisitTasks onReloadTasks = new SceneVisitTasks() { visitCount = -1, onSceneAwake = new UnityEvent(), onSceneStart = new UnityEvent() };

    [SerializeField] SceneVisitTasks onAdditiveLoadTasks = new SceneVisitTasks() { visitCount = -1, onSceneAwake = new UnityEvent(), onSceneStart = new UnityEvent() };

    [SerializeField] public Transform hubReturnPosition = null;
    [SerializeField] public GameObject hubReturnPlayerObject = null;

    [SerializeField] AudioClip sceneMusicOrAmbience;
    [SerializeField] bool playSceneMusicOnSceneStart = false;
    [SerializeField] bool stopSceneMusicOnSceneEnd = true;

    string sceneName = "";  // set at Awake from SceneManager.GetActiveScene().name

    private UnityEvent onSceneAwake;
    private UnityEvent onSceneStart;

    private QuestManager questManager;

    private int visitCount;

    public int VisitCount => visitCount;

    private bool sceneWasReloaded = false;
    public bool SceneWasReloaded => sceneWasReloaded;
    bool sceneWasRestarted = false;
    public bool SceneWasRestarted => sceneWasRestarted;
    bool returnedToHubFromScene = false;
    public bool ReturnedToHubFromScene => returnedToHubFromScene;

    bool restoredSavedGameState = false;
    public bool RestoredSavedGameState => restoredSavedGameState;
    
    void Awake()
    {
        //sceneName = SceneManager.GetActiveScene().name;
        Debug.Log("Scene->Awake: Scene: " + SceneManager.GetActiveScene().name + " gameObject.scene.name: " + gameObject.scene.name);
        sceneName = gameObject.scene.name;

        Debug.Log("SceneManager.sceneCount:" + SceneManager.sceneCount + ", .loadedSceneCount: " + SceneManager.loadedSceneCount);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            UnityEngine.SceneManagement.Scene s = SceneManager.GetSceneAt(i);
            // Note isLoaded returns False on Awake() for the active scene,
            // probably related to the sceneLoaded callback (called after Awake & OnEnable but before Start)
            Debug.Log("Scene at index " + i + ": " + s.name + ", isLoaded: " + s.isLoaded);
        }

        // save reload/restart/hub-return state from GameManager (it resets them in SceneAwake, and adjusts visit count)
        sceneWasReloaded = GameManager.Instance.reloadCurrentSceneCalled;
        sceneWasRestarted = GameManager.Instance.restartCurrentSceneCalled;
        returnedToHubFromScene = GameManager.Instance.hubSubSceneVisited;
        //TODO: restoring game state
        restoredSavedGameState = GameManager.Instance.restoreSavedGameStateOnSceneLoad;

        GameManager.Instance.SceneAwake(this, sceneName);

        visitCount = GameManager.Instance.gameState.GetSceneVisitCount(sceneName);
        Debug.Log("Scene->Awake: Scene (after GM->SceneAwake): " + sceneName + ", Visit Count: " + visitCount);

        PlayerPreferences.Instance.RefreshAudioSettings();

        questManager = FindFirstObjectByType<QuestManager>();
        if (questManager == null)
        {
            GameObject questManagerGO = new GameObject("QuestManager");
            questManager = questManagerGO.AddComponent<QuestManager>();
        }
        questManager.Initialize(this);

        if (returnedToHubFromScene)
        {
            onSceneAwake = onHubReturnTasks.onSceneAwake;
            onSceneStart = onHubReturnTasks.onSceneStart;
            Debug.Log("Scene->Awake: Hub return visit to Scene: " + sceneName + ", using onHubReturnTasks");
            if (hubReturnPosition != null && hubReturnPlayerObject != null)
            {
                hubReturnPlayerObject.transform.position = hubReturnPosition.position;
                Debug.Log("Scene->Awake: Moved player to hub return position: " + hubReturnPosition.position);
            }
            else
            {
                Debug.LogWarning("Scene->Awake: Hub return position or player object not set for Scene: " + sceneName);
            }
        }
        else    // non-hub visit
        {
            // Restart means resetting all progress in the scene but not incrementing visit count
            if (sceneWasRestarted)
            {
                questManager.ClearTasksAndQuestsForScene();
                // No that will stil be a problem 
                //GameManager.Instance.gameState.ResetSceneProgress(sceneName);
                //Debug.Log("Scene->Awake: Restart visit to Scene: " + sceneName + ", Restoring scene state");
            }
            else if (sceneWasReloaded)
            {
                // Just restore scene state, don't reset progress or increment visit count
                //SaveManager.Instance.RestoreTemporarySceneState();
                //Debug.Log("Scene->Awake: Reload visit to Scene: " + sceneName + ", Restoring scene state");
            }
            

            SceneVisitTasks visitTasks = sceneVisitTasks.Find(s => s.visitCount == visitCount);
            if (visitTasks != null)
            {
                Debug.Log("Scene->Awake: Found SceneVisitTasks for Scene: " + sceneName + ", Visit Count: " + visitCount);
                onSceneAwake = visitTasks.onSceneAwake;
                onSceneStart = visitTasks.onSceneStart;
            }
            else
            {
                // check for 'useIfVisitCountExceeded' tasks moving from last to first in list
                visitTasks = sceneVisitTasks.FindLast(s => s.useIfVisitCountExceeded && s.visitCount <= visitCount);
                if (visitTasks != null)
                {
                    onSceneAwake = visitTasks.onSceneAwake;
                    onSceneStart = visitTasks.onSceneStart;
                }
                else
                {
                    Debug.LogWarning("Scene->Awake: No SceneVisitTasks found for Scene: " + sceneName + ", Visit Count: " + visitCount + " defaulting to 1st tasks in list");
                    onSceneAwake = sceneVisitTasks[0].onSceneAwake;
                    onSceneStart = sceneVisitTasks[0].onSceneStart;
                }
            }
        }

        // ensure there is a canvas and and EventSystem
        if (FindFirstObjectByType<Canvas>() == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvasGO.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("Scene->Awake: No Canvas found in Scene: " + sceneName + ", created new Canvas.");
        }
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Use the new Input System UI input module (namespace: UnityEngine.InputSystem.UI)
            // Fully-qualified name used to avoid needing an extra using directive here.
            eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // NOTE: Adding both input modules can cause conflicts. If you're using the old
            // input system, remove the line above and keep the StandaloneInputModule instead.
            //eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("Scene->Awake: No EventSystem found in Scene: " + sceneName + ", created new EventSystem.");
        }

        onSceneAwake.Invoke();
        if (SceneWasReloaded)
        {
            onReloadTasks.onSceneAwake.Invoke();
        }
    }

    void OnEnable()
    {
        Debug.Log("Scene->OnEnable: Scene: " + sceneName);
        if (sceneWasReloaded || returnedToHubFromScene)
        {
            SaveManager.Instance.RestoreTemporarySceneState();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.SceneStart();

        if (playSceneMusicOnSceneStart && sceneMusicOrAmbience != null)
        {
            AudioManager.Play(sceneMusicOrAmbience);
        }

        onSceneStart.Invoke();
        if (SceneWasReloaded)
        {
            onReloadTasks.onSceneStart.Invoke();
        }
    }

    // Update is called once per frame
    //void Update() { }

    void OnDestroy()
    {
        GameManager.Instance.SceneDestroyed();
        if (stopSceneMusicOnSceneEnd)
        {
            AudioManager.Stop();
        }
    }


    /*[ContextMenu("Manual OnSceneAwake Invoke")]
    public void ManualOnSceneAwakeInvoke()
    {
        InvokeSceneVisitEventAtIndex(0, true);
    }
    [ContextMenu("Manual OnSceneStart Invoke")]
    public void ManualOnSceneStartInvoke()
    {
        InvokeSceneVisitEventAtIndex(0, false);
    }

    private void InvokeSceneVisitEventAtIndex(int index, bool invokeAwakeEvent)
    {
        if (sceneVisitTasks == null || sceneVisitTasks.Count <= index || sceneVisitTasks[index] == null)
        {
            Debug.LogWarning("Scene->ManualInvoke: Scene visit task entry is missing at index " + index + ".");
            return;
        }

        UnityEvent sceneEvent = invokeAwakeEvent
            ? sceneVisitTasks[index].onSceneAwake
            : sceneVisitTasks[index].onSceneStart;

        if (sceneEvent == null)
        {
            Debug.LogWarning("Scene->ManualInvoke: Selected UnityEvent is null at index " + index + ".");
            return;
        }

        Debug.Log(
            "Scene->ManualInvoke: Invoking " + (invokeAwakeEvent ? "onSceneAwake" : "onSceneStart") +
            " at index " + index +
            ", persistent listeners: " + sceneEvent.GetPersistentEventCount() +
            ", isPlaying: " + Application.isPlaying +
            ". In edit mode, Runtime Only listeners will not execute."
        );

        sceneEvent.Invoke();
    }*/

#region Scene Progression
    // For use with Events in the inspector:
    public void LoadNextScene()
    {
        GameManager.Instance.LoadNextScene();
    }
    public void LoadScene(Scenes scene)
    {
        GameManager.Instance.LoadScene(scene);
    }
    // TODO: minigames
    // public void LoadHubConnectedScene(Scenes hubConnectedScene)
    // {
    //     // Set up logic - could have GM.minigameLoaded field
    //     GameManager.Instance.LoadHubConnectedScene(hubConnectedScene);
    // }

    // increments visit count, and restores scene state
    public void ReloadScene()
    {
        SaveManager.Instance.CaptureTemporarySceneState();
        GameManager.Instance.ReloadCurrentScene();
    }

    // Restart doesn't increment visit counter, but does reset scene progress
    public void RestartScene()
    {
        GameManager.Instance.RestartCurrentScene();
    }

#endregion Scene Progression

#region GameManager Helpers
    public void DisableMoveControlsGM()
    {
        GameManager.Instance.DisableMoveControls();
    }
    public void EnableMoveControlsGM()
    {
        GameManager.Instance.EnableMoveControls();
    }
    public void DisableLookControlsGM()
    {
        GameManager.Instance.DisableLookControls();
    }
    public void EnableLookControlsGM()
    {
        GameManager.Instance.EnableLookControls();
    }
    public void PauseGame()
    {
        GameManager.Instance.PauseGame();
    }
    public void ResumeGame()
    {
        GameManager.Instance.ResumeGame();
    }
    public void SetModalDialogueOpen(bool mouseCursorForUI = true)
    {
        GameManager.Instance.ModalDialogueSetIsOpen(mouseCursorForUI);
    }
    public void SetModalDialogueClosed(bool mouseCursorForGame = true)
    {
        GameManager.Instance.ModalDialogueSetIsClosed(mouseCursorForGame);
    }

#endregion GameManager Helpers

#region AudioManager Helpers

    public void SetMainVolume(float volume)
    {
        AudioManager.SetMainVolumeMixer(volume);
    }
    public void SetMusicVolume(float volume)
    {
        AudioManager.SetMusicVolume(volume);
    }
    public void SetSFXVolume(float volume)
    {
        AudioManager.SetSFXVolume(volume);
    }
    public void SetVoiceVolume(float volume)
    {
        AudioManager.SetVoiceVolume(volume);
    }
    public void PlaySceneClip()
    {
        if (sceneMusicOrAmbience != null)
        {
            AudioManager.Play(sceneMusicOrAmbience);
        }
        else
        {
            Debug.LogWarning("PlaySceneClip: No scene music set for Scene: " + sceneName);
        }
    }
    public void StopSceneClip()
    {
        AudioManager.Stop();
    }
    public void PlayLongClip(AudioClip clip)
    {
        AudioManager.Play(clip);
    }
    public void StopPlayingLongClip()
    {
        AudioManager.Stop();
    }
    public void PlayMusic(AudioClip clip)
    {
        AudioManager.Play(clip);
    }
    public void StopMusic()
    {
        AudioManager.Stop();
    }
    public void MuteAudio()
    {
        AudioManager.Mute();
    }
    public void UnMuteAudio()
    {
        AudioManager.UnMute();
    }
    public void PlayOneShot(AudioClip clip)
    {
        AudioManager.PlayOneShot(clip);
    }
    public void PlayOneShotFromArray(AudioClip[] clips)
    {
        AudioManager.PlayOneShotFromArray(clips);
    }
    public void SetOneShotVolume(float volume)
    {
        AudioManager.PlayVolumeForOneShot(volume);
    }

#endregion AudioManager Helpers


// TODO: Remove these? (just drag object into events and set directly in inspector)
#region Interaction Helpers

    public void TriggerableSetIsActive(GameObject go, bool active)
    {
        Triggerable triggerable = go.GetComponent<Triggerable>();
        if (triggerable == null)
        {
            Debug.LogError("TriggerableSetIsActive: GameObject: " + go.name + " does not have a Triggerable component.");
            return;
        }
        else
        {
            triggerable.SetIsActive(active);
        }
    }
    public void TriggerableSetIsActive(GameObject go)
    {
        TriggerableSetIsActive(go, true);
    }
    public void TriggerableSetIsInactive(GameObject go)
    {
        TriggerableSetIsActive(go, false);
    }
    public void InteractableSetIsInteractable(GameObject go, bool isInteractable)
    {
        InteractableBase interactable = go.GetComponent<InteractableBase>();
        if (interactable == null)
        {
            Debug.LogError("InteractableSetIsInteractable: GameObject: " + go.name + " does not have an InteractableBase component.");
            return;
        }
        else
        {
            interactable.SetIsInteractable(isInteractable);
        }
    }
    public void InteractableSetIsInteractable(GameObject go)
    {
        InteractableSetIsInteractable(go, true);
    }
    public void InteractableSetIsNonInteractable(GameObject go)
    {
        InteractableSetIsInteractable(go, false);
    }

    public void TimerSetIsEnabled(GameObject go, bool enabled)
    {
        TimerObject timerObject = go.GetComponent<TimerObject>();
        if (timerObject == null)
        {
            Debug.LogError("TimerSetIsEnabled: GameObject: " + go.name + " does not have a TimerObject component.");
            return;
        }
        else
        {
            timerObject.SetIsEnabled(enabled);
        }
    }
    public void TimerSetIsEnabled(GameObject go)
    {
        TimerSetIsEnabled(go, true);
    }
    public void TimerSetIsDisabled(GameObject go)
    {
        TimerSetIsEnabled(go, false);
    }
    public void TimerReset(GameObject go)
    {
        TimerObject timerObject = go.GetComponent<TimerObject>();
        if (timerObject == null)
        {
            Debug.LogError("TimerReset: GameObject: " + go.name + " does not have a TimerObject component.");
            return;
        }
        else
        {
            timerObject.ResetTimer();
        }
    }
    // Can't expose in Unity (2 parameters) - in this case, call Timer object's SetDuration directly
    //public void TimerSetDuration(GameObject go, float duration)

#endregion Interaction Helpers

}
