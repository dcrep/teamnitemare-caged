using UnityEngine;
using System;
using System.Collections.Generic;

// TODO: Implement visit-dependent quests for scenes (?)
// TODO: Quest task dependencies ignored for now - eventually this should mean something!
// TODO: Quest-completion-in-order (Task-completion too?) to do UnityEvents in proper order
// (note that TimerObjects need to be invoked right away after previous events)
// TODO: For every ISaveable that has UnityEvents attached, need to change RestoreState() to not invoke UnityEvents
// (this is what the Quest completion order will be for)

[Serializable]
public enum Scenes
{
    LoadingScreen,
    MainMenu,
    Game,
    GameOver,
    DCExperiments,
    UITest,
    UILayout
}

[Serializable]
public enum GameStates
{
    Loading,
    Playing,
    Paused,
    UI,
    GameOver,
    Win,
    Lose
 };

 [Serializable]
 public enum MultiplayerModes
 {
    Disconnected,
    LocalHotseat,
    Online
 };

 [Serializable]
 public enum GameLevels
 {
    Interlude,
    Level1,
    Level2,
    Level3,
    Level4,
    Level5,
    Experimental,
    None
 };

 [Serializable]
 public class QuestTask
 {
    public string taskUniqueId;
    public string taskName;
    public string questTaskTag;
    public bool isCollectibleTask;    
    public string taskDescription;
    public bool isCompleted;
    public int taskValue;
    public int taskMaxValue;
    public bool oneTimeCompletion;
    [SerializeReference] 
    public List<QuestTask> requiredTasks = null; // Tasks that must be completed before this task can be completed
 };

 [Serializable]
 public class QuestInfo
{
    public string questUniqueId;
    public bool isGlobalQuest;
    public string questName;
    public string sceneBelongingTo; // null/empty if global quest
    public string questDescription;
    public bool isCompleted;
    public int numObjectivesCompleted;
    public int totalObjectives;
    public List<QuestTask> questTasks;
}

[Serializable]
public class SceneQuestInfo
{
    public string sceneName;    // "GLOBAL" for global quests
    public int visit;
    public List<QuestInfo> questsInScene;
    public List<string> questUniqueIdCompletionInOrder;
    public List<string> taskUniqueIdCompletionInOrder;
}

[Serializable]
public class GameState
{
    public string GameName = "Team Nitemare's Caged? Game";

    public GameStates currentGameState = GameStates.Loading;

    public bool inGameModalDialogueActive = false;

    public static ScenesSO scenesSO;

    // unique scene names list
    
    public List<string> sceneNames = new List<string>();

    public Scenes currentScene = Scenes.LoadingScreen;
    public Scenes previousScene = Scenes.LoadingScreen;
    public string currentSceneName = "";
    public string previousSceneName = "";

    public Scene currentSceneScript = null;
    //public GameObject playerPawn = null;
    public GameLevels currentLevel = GameLevels.None;

    // Scene, Tasks Completed
    //[field: SerializeField] public Dictionary<string, List<string>> sceneProgressionInfo = new Dictionary<string, List<string>>();

    public List<int> scenesInOrderOfVisit = new List<int>();
    public SceneQuestInfo globalQuestInfo = new SceneQuestInfo { sceneName = "GLOBAL", visit = -1, 
        questsInScene = new List<QuestInfo>(),
        questUniqueIdCompletionInOrder = new List<string>(), taskUniqueIdCompletionInOrder = new List<string>() };
    public List<SceneQuestInfo> sceneQuestInfos = new List<SceneQuestInfo>();

    public int numCurrentLevelObjectivesCompleted = 0;
    public int totalCurrentLevelObjectives = 0;
    public bool currentLevelCompleted = false;

    public void Initialize()
    {
        if (scenesSO == null)
        {
            scenesSO = Resources.Load<ScenesSO>("ScenesSO"); 
            if (scenesSO == null)
            {
                Debug.LogError("GameState->Initialize: Failed to load ScenesSO from Resources. Make sure there is a ScenesSO asset in a Resources folder.");
            }
        }
        sceneNames = scenesSO.GetAllScenes();
    }
    public void ResetGameState()
    {
        currentGameState = GameStates.Loading;
        inGameModalDialogueActive = false;
        currentScene = Scenes.LoadingScreen;
        previousScene = Scenes.LoadingScreen;
        currentSceneName = "";
        previousSceneName = "";
        currentSceneScript = null;
        currentLevel = GameLevels.None;
        scenesInOrderOfVisit.Clear();
        globalQuestInfo.questsInScene.Clear();
        globalQuestInfo.questUniqueIdCompletionInOrder.Clear();
        globalQuestInfo.taskUniqueIdCompletionInOrder.Clear();
        sceneQuestInfos.Clear();
        numCurrentLevelObjectivesCompleted = 0;
        totalCurrentLevelObjectives = 0;
        currentLevelCompleted = false;
    }

#region Scene Management
    // Enforces one scene in list and optionally visit order
    public void AddScene(string sceneName, bool addToVisitOrder = true)
    {
        int index = sceneNames.FindIndex(s => s == sceneName);
        if (index == -1)
        {
            sceneNames.Add(sceneName);
            index = sceneNames.Count - 1;
            //Debug.Log("Added scene: " + sceneName + " to game state with index: " + index);
        }
        if (addToVisitOrder)
        {
            scenesInOrderOfVisit.Add(index);
        }
    }
    public bool SceneExists(string sceneName)
    {
        return sceneNames.Contains(sceneName);
    }
    public int SceneIndex(string sceneName)
    {
        // returns index of scene if found, otherwise -1
        return sceneNames.FindIndex(s => s == sceneName);
    }
    public int GetSceneVisitCount(string sceneName)
    {
        int index = SceneIndex(sceneName);
        if (index == -1)
        {
            Debug.LogError("GetSceneVisitCount: Scene: " + sceneName + " not found in game state.");
            return 0;
        }
        return scenesInOrderOfVisit.FindAll(s => s == index).Count;
    }
    public int GetSceneVisitCount(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= sceneNames.Count)
        {
            Debug.LogError("GetSceneVisitCount: Scene index: " + sceneIndex + " is out of bounds in game state.");
            return 0;
        }
        return scenesInOrderOfVisit.FindAll(s => s == sceneIndex).Count;
    }
#endregion Scene Management

#region Quest Progression

#region Quest Querying
    public int GetQuestIndex(string sceneName, string questUniqueId, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            return sceneInfo.questsInScene.FindIndex(q => q.questUniqueId == questUniqueId);
        }
        return -1; // Quest not found
    }
    public int GetTaskIndex(string sceneName, string questUniqueId, string taskName, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
            if (questInfo != null)
            {
                return questInfo.questTasks.FindIndex(t => t.taskName == taskName);
            }
        }
        return -1; // Task not found
    }
    public SceneQuestInfo GetSceneQuestInfo(string sceneName, int visit = -1)
    {
        return sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
    }
    public QuestInfo GetQuestInfo(string sceneName, string questUniqueId, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            return sceneInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
        }
        return null; // Quest not found
    }
    public QuestTask GetTaskInfo(string sceneName, string questUniqueId, string taskUniqueId, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
            if (questInfo != null)
            {
                return questInfo.questTasks.Find(t => t.taskUniqueId == taskUniqueId);
            }
        }
        return null; // Task not found
    }
    public QuestTask GetTaskInfo(string sceneName, string taskUniqueId, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            foreach (var quest in sceneInfo.questsInScene)
            {
                QuestTask taskInfo = quest.questTasks.Find(t => t.taskUniqueId == taskUniqueId);
                if (taskInfo != null)
                {
                    return taskInfo;
                }
            }
        }
        return null; // Task not found
    }
    public bool IsTaskComplete(string sceneName, string taskUniqueId, int visit = -1)
    {
        QuestTask taskInfo = GetTaskInfo(sceneName, taskUniqueId, visit);
        if (taskInfo != null)
        {
            return taskInfo.isCompleted;
        }
        return false; // Task not found
    }
    public bool IsQuestComplete(string sceneName, string questUniqueId, int visit = -1)
    {
        QuestInfo questInfo = GetQuestInfo(sceneName, questUniqueId, visit);
        if (questInfo != null)
        {
            return questInfo.isCompleted;
        }
        return false; // Quest not found
    }

    public List<QuestInfo> GetQuestsInScene(string sceneName, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            return sceneInfo.questsInScene;
        }
        return new List<QuestInfo>(); // No quests found for this scene
    }

    public List<QuestInfo> GetCompletedQuestsInScene(string sceneName, int visit = -1)
    {
        List<QuestInfo> questsInScene = GetQuestsInScene(sceneName, visit);
        return questsInScene.FindAll(q => q.isCompleted);
    }
#endregion Quest Querying

    public void AddQuestToScene(string sceneName, QuestInfo questInfo, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            if (sceneInfo.questsInScene.Exists(q => q.questUniqueId == questInfo.questUniqueId))
            {
                Debug.LogWarning("Cannot add quest: " + questInfo.questName + " to scene: " + sceneName + " because a quest with the same unique ID already exists in this scene's progression info.");
                return;
            }
            sceneInfo.questsInScene.Add(questInfo);
        }
        else
        {
            SceneQuestInfo newSceneInfo = new SceneQuestInfo { sceneName = sceneName, visit = visit, 
                questsInScene = new List<QuestInfo> { questInfo },
                questUniqueIdCompletionInOrder = new List<string>(),
                taskUniqueIdCompletionInOrder = new List<string>()
             };
            sceneQuestInfos.Add(newSceneInfo);
        }
    }

    public void AddTaskObjectToScene(string sceneName, string questUniqueId, QuestTask taskInfo, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
            if (questInfo != null)
            {
                if (questInfo.questTasks.Exists(t => t.taskUniqueId == taskInfo.taskUniqueId))
                {
                    Debug.LogWarning("Cannot add task: " + taskInfo.taskName + " to quest: " + questUniqueId + " in scene: " + sceneName + " because a task with the same unique ID already exists in this quest's progression info.");
                    return;
                }
                questInfo.questTasks.Add(taskInfo);
            }
            else
            {
                Debug.LogWarning("Q-Cannot add task: " + taskInfo.taskName + " for quest: " + questUniqueId + " in scene: " + sceneName + " because no info found for this quest in game state.");
            }
        }
        else
        {
            Debug.LogError("S-Cannot add task: " + taskInfo.taskName + " for quest: " + questUniqueId + " in scene: " + sceneName + " because no info found for this scene in game state.");
        }
    }
    public void AddNonTaskObjectToSceneAsCompleted(string sceneName, string nonTaskUniqueId, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            QuestInfo nonTaskObjectAsQuest = new QuestInfo
            {
                questUniqueId = "NonQuest_" + nonTaskUniqueId,
                isGlobalQuest = false,
                questName = "NonQuest_" + nonTaskUniqueId,
                sceneBelongingTo = sceneName,
                questDescription = "NonQuest - Non-task object: " + nonTaskUniqueId,
                isCompleted = true,
                numObjectivesCompleted = 0,
                totalObjectives = 0,
                questTasks = new List<QuestTask>()
            };
            sceneInfo.questsInScene.Add(nonTaskObjectAsQuest);
        }
        else
        {
            Debug.LogWarning("Cannot add non-task object: " + nonTaskUniqueId + " as completed for scene: " + sceneName + " because no progression info found for this scene in game state.");
        }
    }

    public void MarkQuestComplete(string sceneName, string questUniqueId, int visit = -1)
    {
        SceneQuestInfo sceneQuestInfo = GetSceneQuestInfo(sceneName, visit);
        if (sceneQuestInfo != null)
        {
            QuestInfo questInfo = sceneQuestInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
            if (questInfo != null)
            {
                questInfo.isCompleted = true;
                // make sure it wasn't the last one added
                if (sceneQuestInfo.questUniqueIdCompletionInOrder.Contains(questUniqueId))
                {
                    Debug.LogWarning("Quest: " + questInfo.questName + " in scene: " + sceneName + " was already marked as complete before. Duplicate completion?");
                }
                else
                {
                    sceneQuestInfo.questUniqueIdCompletionInOrder.Add(questUniqueId);
                    Debug.Log("Marked quest: " + questInfo.questName + " in scene: " + sceneName + " as complete.");
                }
            }
            else
            {
                Debug.LogWarning("Cannot mark quest: " + questUniqueId + " as complete because it was not found in scene: " + sceneName);
            }
        }
        else
        {
            Debug.LogWarning("Cannot mark quest: " + questUniqueId + " as complete because no progression info found for scene: " + sceneName + " in game state.");
        }
    }
    public void MarkTaskComplete(string sceneName, string questUniqueId, string taskUniqueId, int visit = -1)
    {
        SceneQuestInfo sceneQuestInfo = GetSceneQuestInfo(sceneName, visit);
        if (sceneQuestInfo != null)
        {
            QuestInfo questInfo = sceneQuestInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
            if (questInfo != null)
            {
                QuestTask taskInfo = questInfo.questTasks.Find(t => t.taskUniqueId == taskUniqueId);
                if (taskInfo != null)
                {
                    taskInfo.isCompleted = true;
                    // make sure it wasn't the last one added
                    if (sceneQuestInfo.taskUniqueIdCompletionInOrder.Contains(taskUniqueId))
                    {
                        Debug.LogWarning("Task: " + taskInfo.taskName + " in quest: " + questUniqueId + " in scene: " + sceneName + " was already marked as complete before. Duplicate completion?");
                    }
                    else
                    {
                        sceneQuestInfo.taskUniqueIdCompletionInOrder.Add(taskUniqueId); // add task to completion order list   
                        Debug.Log("Marked task: " + taskInfo.taskName + " in quest: " + questUniqueId + " in scene: " + sceneName + " as complete.");
                    }
                }
                else
                {
                    Debug.LogWarning("Cannot mark task: " + taskUniqueId + " as complete because it was not found in quest: " + questUniqueId + " in scene: " + sceneName);
                }
            }
            else
            {
                Debug.LogWarning("Cannot mark task: " + taskUniqueId + " as complete because quest: " + questUniqueId + " was not found in scene: " + sceneName);
            }
        }
        else
        {
            Debug.LogWarning("Cannot mark task: " + taskUniqueId + " as complete because no progression info found for scene: " + sceneName + " in game state.");
        }
    }

    public void AddGlobalQuest(QuestInfo questInfo)
    {
        globalQuestInfo.questsInScene.Add(questInfo);
    }

    public void ClearTasksAndQuestsForScene(string sceneName, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName && (visit > 0) ? s.visit == visit : true);
        if (sceneInfo != null)
        {
            sceneInfo.questsInScene.Clear();
            sceneInfo.questUniqueIdCompletionInOrder.Clear();
            sceneInfo.taskUniqueIdCompletionInOrder.Clear();
        }
        else
        {
            Debug.LogWarning("Cannot clear tasks and quests for scene: " + sceneName + " because no progression info found for this scene in game state.");
        }
    }
#endregion Quest Progression
}
