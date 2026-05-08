using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// TODO: Collectibles with same-name, count etc implementation
// for now, just use QuestComponent's questTaskTag to differentiate collectibles

// QuestManager is per-level, created by Scene, updates Quest info in GameState
[DefaultExecutionOrder(-99)]
public class QuestManager : MonoBehaviour
{
    public List<Quest> quests = new List<Quest>();
    // singleton
    public static QuestManager Instance { get; private set; }

    // completedQuests
    // completedTasks

    private string sceneName = "";
    int visit;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            sceneName = SceneManager.GetActiveScene().name;
        }
    }
    public void Initialize(Scene scene)
    {
        sceneName = SceneManager.GetActiveScene().name;
        visit = scene.VisitCount;
    }
    // void Start()
    // {
    //     // find Quests in the scene and add them to the QuestManager (if they have task groups)
    //     // Instead: Quests add themselves to the QuestManager in their Start() function
    // }

    public Quest FindQuest(string questUniqueId)
    {
        Quest quest = quests.Find(q => q.questUniqueId.ID == questUniqueId);
        if (quest == null)
        {
            Debug.LogWarning("FindQuest: No quest found with unique id: " + questUniqueId);
            return null;
        }
        return quest;
    }
    public Quest FindQuestByName(string questName)
    {
        Quest quest = quests.Find(q => q.QuestName == questName);
        if (quest == null)
        {
            Debug.LogWarning("FindQuestByName: No quest found with name: " + questName);
            return null;
        }
        Debug.Log("FindQuestByName: Found quest with name: " + questName + " in scene: " + sceneName);
        return quest;
    }

    public void AddQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogError("AddQuest: Cannot add null Quest.");
            return;
        }
        //if (quests.Exists(q => q.QuestName == quest.QuestName))
        if (quests.Exists(q => q.questUniqueId.ID == quest.questUniqueId.ID))
        {
            Debug.LogWarning("AddQuest: Quest with unique ID: " + quest.questUniqueId.ID + " already exists in QuestManager.");
            return;
        }
        // set counts to 0
        //group.tasksCompleted = 0;
        quests.Add(quest);

        GameManager.Instance.gameState.AddQuestToScene(sceneName, quest.ConvertToQuestInfo(), visit);
    }
    public void AddTaskObject(Quest quest, QuestComponent go, bool questAddTaskObject = true)
    {
        if (quest == null)
        {
            Debug.LogError("AddTaskObject: Quest is null.");
            return;
        }
        // prevents recursion:
        if (questAddTaskObject)
        {
            quest.AddTaskObject(go);
        }
        Debug.Log("Adding task object: " + go.uniqueID.ID + " to quest with name: " + quest.QuestName + " in scene: " + sceneName);
        GameManager.Instance.gameState.AddTaskObjectToScene(sceneName, quest.questUniqueId.ID, new QuestTask {
            taskUniqueId = go.uniqueID.ID, taskName = go.gameObject.name,
            questTaskTag = go.questTaskTag, isCollectibleTask = go.isCollectible,
            isCompleted = false, taskDescription = "",
            taskValue = 1, taskMaxValue = 1, oneTimeCompletion = true, requiredTasks = null
        }, visit);
    }
    public void AddTaskObject(string questUniqueId, QuestComponent go, bool questAddTaskObject = true)
    {
        Quest quest = quests.Find(g => g.questUniqueId.ID == questUniqueId);
        AddTaskObject(quest, go);
    }
    public int FindQuestGroupIndexForTaskObject(QuestComponent go)
    {
        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i].FindTaskGroupIndexForTaskObject(go) != -1)
            {
                return i;
            }
        }
        return -1; // not found
    }
    public void CompletedQuestObject(Quest quest, QuestComponent go)
    {
        if (quest == null)
        {
            Debug.LogError("CompleteQuestObject: Quest is null.");
            return;
        }
        GameManager.Instance.gameState.MarkTaskComplete(sceneName, quest.questUniqueId.ID, go.uniqueID.ID, visit);
        // quest component removed before this is called
        // also, empty quests are removed afterwards by Quest script 
    }
    public void CompleteTaskObjectForUnknownQuest(QuestComponent go)
    {
        Debug.Log("Completing task object: " + go.uniqueID.ID + " for scene: " + sceneName);
        int groupIndex = FindQuestGroupIndexForTaskObject(go);
        if (groupIndex == -1)
        {
            Debug.LogError("CompleteTaskObject: No Quest group found containing task object: " + go.uniqueID.ID);
            return;
        }
        Quest quest = quests[groupIndex];
        quest.CompleteTaskObject(go);
    }
    public void CompletedNonQuestObject(QuestComponent go)
    {
        Debug.Log("Removing non-quest object: " + go.uniqueID.ID + " for scene: " + sceneName);
        GameManager.Instance.gameState.AddNonTaskObjectToSceneAsCompleted(sceneName, go.uniqueID.ID, visit);
    }
    public void CompletedQuest(Quest quest)
    {
        Debug.Log("Acting on quest complete for quest with quest group: " + quest.QuestName + " in scene: " + sceneName);
        GameManager.Instance.gameState.MarkQuestComplete(sceneName, quest.questUniqueId.ID, visit);
        quests.Remove(quest);
    }
    public void CompleteAllTasksInQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogError("CompleteAllTasksInQuest: Quest is null.");
            return;
        }
        quest.ForceCompleteTaskGroup();
    }
    public void CompleteAllTasksInQuest(string questUniqueId)
    {
        Quest quest = quests.Find(g => g.questUniqueId.ID == questUniqueId);
        CompleteAllTasksInQuest(quest);
    }
    public void ForceCompleteAllQuests(bool actOnComplete = true)
    {
        Debug.Log("Force completing all tasks for scene: " + sceneName);
        foreach (var quest in quests)
        {
            quest.ForceCompleteTaskGroup(actOnComplete);
        }
    }
    public int GetTotalTasksInAllGroups()
    {
        int total = 0;
        foreach (var group in quests)
        {
            total += group.TaskCount;
        }
        return total;
    }
    public int GetTotalQuests()
    {
        return quests.Count;
    }
    public int GetTotalQuestsCompleted()
    {
        int total = 0;
        foreach (var quest in quests)
        {
            if (quest.ActedOnComplete)
            {
                total++;
            }
        }
        return total;
    }

    public bool IsGivenTaskComplete(QuestComponent go)
    {
        bool complete = GameManager.Instance.gameState.IsTaskComplete(sceneName, go.uniqueID.ID, visit);
        Debug.Log("Checking if given task: " + go.uniqueID.ID + " is complete for scene: " + sceneName + ". Result: " + complete);
        return complete;
    }

    public void ClearAllTaskGroups()
    {
        Debug.Log("Clearing task group for scene (with NO progression updates): " + sceneName);
        quests.Clear();
        quests = new List<Quest>();
    }

    public void ClearTasksAndQuestsForScene()
    {
        GameManager.Instance.gameState.ClearTasksAndQuestsForScene(sceneName, visit);
    }
}
