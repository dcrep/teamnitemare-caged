using System;
using System.Collections.Generic;

[Serializable]
public class SaveFile
{
    public PlayerState playerState;
    public GameState gameState;

    // SceneName → (UniqueID → StateObject)
    public Dictionary<string, Dictionary<string, object>> sceneStates =
        new Dictionary<string, Dictionary<string, object>>();
}

