using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HubScenes
{
    public string hubSceneName;
    public List<string> connectedScenes;
}

[CreateAssetMenu(fileName = "ScenesSO", menuName = "Scriptable Objects/ScenesSO")]
public class ScenesSO : ScriptableObject
{
    // Unfortunately Unity has no built-in SceneReference type
    // There are community implementations, but for now we'll just use strings
    //public SceneReference mainMenuScene;
    public string mainMenuScene;
    public Scenes mainMenuSceneEnum = Scenes.MainMenu;
    public List<string> gameScenes;
    public List<HubScenes> hubScenes;
    public Scenes gameSceneEnum = Scenes.Game;
    public string gameOverScene;
    public Scenes gameOverSceneEnum = Scenes.GameOver;
    //public string creditsScene;
    public string DCExperimentsScene;
    public Scenes DCExperimentsSceneEnum = Scenes.Game; //Scenes.DCExperiments;

    public string UITestScene;
    public Scenes UITestSceneEnum = Scenes.Game;    //UITest;
    public string UILayoutScene;
    public Scenes UILayoutSceneEnum = Scenes.Game;  //UILayout;

    public List<string> GetHubConnectedScenes()
    {
        List<string> hubConnectedScenes = new List<string>();
        foreach (var hub in hubScenes)
        {
            hubConnectedScenes.AddRange(hub.connectedScenes);
        }
        // remove any empty or null entries
        hubConnectedScenes.RemoveAll(s => string.IsNullOrEmpty(s));
        return hubConnectedScenes;
    }

    public List<string> GetGameScenes()
    {
        List<string> allGameScenes = new List<string>(gameScenes);
        // remove any empty or null entries
        allGameScenes.RemoveAll(s => string.IsNullOrEmpty(s));
        var hubConnectedScenes = GetHubConnectedScenes();
        allGameScenes.AddRange(hubConnectedScenes);
        return allGameScenes;
    }

    public List<string> GetAllScenes()
    {
        List<string> allScenes = new List<string>
        {
            mainMenuScene,
            gameOverScene,
            DCExperimentsScene,
            UITestScene,
            UILayoutScene
        };
        // remove any empty or null entries
        allScenes.RemoveAll(s => string.IsNullOrEmpty(s));
        
        var gameScenes = GetGameScenes();
        allScenes.AddRange(gameScenes);
        return allScenes;
    }
}
