using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

// TODO: Collectibles with same-name, count etc implementation(?)
// for now, just use QuestComponent's questTaskTag to differentiate collectibles

[Serializable]
public class TaskGroup
{
    public string taskGroupName = "";
    public bool isCollectibleGroup = false;
    public bool completeReferencedTaskFirst = false;
    public TaskGroup taskObjectToCompleteFirst;
    public List<QuestComponent> taskObjects = new List<QuestComponent>();
    public bool autoActOnComplete = false;
    public UnityEvent onTasksCompleted = new UnityEvent();
    public bool actedOnComplete = false;
    public int taskMinimumForCompletion = 0;
    public int tasksCompleted = 0;

    public QuestInfo ConvertToQuestInfo(string questUniqueId)
    {
        // full assignment
        QuestInfo questInfo = new QuestInfo {
            questUniqueId = questUniqueId,
            isGlobalQuest = false,
            questName = taskGroupName, 
            sceneBelongingTo = SceneManager.GetActiveScene().name,
            questDescription = "",
            isCompleted = actedOnComplete,
            numObjectivesCompleted = tasksCompleted,
            totalObjectives = taskObjects.Count,
            questTasks = new List<QuestTask>(),
        };
        foreach (var go in taskObjects)
        {
            // Race condition workaround:
            var uid = go.uniqueID ?? go.GetComponent<UniqueID>();
            if (uid == null) { Debug.LogError($"Missing UniqueID on {go.name}"); continue; }
            Debug.Log("Q-> uniqueID: " + uid.ID + ", name: " + go.gameObject.name + ", tag: " + go.questTaskTag + ", isCollectible: " + go.isCollectible);
            questInfo.questTasks.Add(new QuestTask {
                taskUniqueId = uid.ID, taskName = go.gameObject.name,
                questTaskTag = go.questTaskTag, isCollectibleTask = go.isCollectible,
                isCompleted = actedOnComplete, taskDescription = "",
                taskValue = 1, taskMaxValue = 1, oneTimeCompletion = true, requiredTasks = null});
        }
        Debug.Log("Converted TaskGroup: " + taskGroupName + " to QuestInfo with " + questInfo.questTasks.Count + " tasks for scene: " + SceneManager.GetActiveScene().name);
        return questInfo;
    }
    public QuestTask ConvertToQuestTask(int index)
    {
        if (index < 0 || index >= taskObjects.Count)
        {
            Debug.LogError("ConvertToQuestTask: Index out of range for task objects. Index: " + index + ", Count: " + taskObjects.Count);
            return null;
        }
        var go = taskObjects[index];
        return new QuestTask {
            taskUniqueId = go.uniqueID.ID, taskName = go.gameObject.name,
            questTaskTag = go.questTaskTag, isCollectibleTask = go.isCollectible,
            isCompleted = actedOnComplete, taskDescription = "",
            taskValue = 1, taskMaxValue = 1, oneTimeCompletion = true, requiredTasks = null
        };
    }
}
[RequireComponent(typeof(UniqueID))]
[DefaultExecutionOrder(-80)]
public class Quest : MonoBehaviour, ISaveable
{
    public UniqueID questUniqueId;
    [SerializeField] private TaskGroup taskGroup = new TaskGroup();

    public string QuestName => taskGroup.taskGroupName;
    public int TaskCount => taskGroup.taskObjects.Count;
    public int TasksRemaining => taskGroup.taskMinimumForCompletion <= 0 ? 0 : taskGroup.taskMinimumForCompletion - taskGroup.tasksCompleted;

    private string sceneName = "";

    private string QuestGroupId => gameObject.name + "_" + taskGroup.taskGroupName;

    public bool ActedOnComplete => taskGroup.actedOnComplete;

    bool questAdded = false;


    private void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;
        if (questUniqueId == null)
            questUniqueId = GetComponent<UniqueID>();

    }

    void OnEnable()
    {
        Debug.Log("UniqueId: " + questUniqueId.ID + " for quest: " + QuestName + " in scene: " + sceneName);
        if (!questAdded)
        {
            Debug.Log("Quest->Start: Adding quest with name: " + QuestName + " to QuestManager from Start() in scene: " + sceneName);
            QuestManager.Instance.AddQuest(this);
            questAdded = true;
        }  
    }

    private void Start()
    {

        if (taskGroup.taskMinimumForCompletion <= 0)
        {
            taskGroup.taskMinimumForCompletion = taskGroup.taskObjects.Count;
        }
    }
// ! I can't develop a CreateQuest scenario where new uniqueID's are generated every time
//!  which might be fine for a global quest created outside of a scene, but not for scene quests

    // public static Quest CreateQuest(string questName, string sceneName, TaskGroup taskGroup)
    // {
    //     GameObject questGO = new GameObject(questName);
    //     Quest quest = questGO.AddComponent<Quest>();
    //     quest.sceneName = sceneName;
    //     quest.taskGroup = taskGroup;
    //     if (quest.taskGroup.onTasksCompleted == null)
    //     {
    //         quest.taskGroup.onTasksCompleted = new UnityEvent();
    //     }
    //     if (string.IsNullOrEmpty(quest.taskGroup.taskGroupName))
    //     {
    //         quest.taskGroup.taskGroupName = questName;
    //     }
    //     Debug.Log("Created quest with name: " + questName + " in scene: " + sceneName);

    //     //!! Letting Start() handle this so UniqueID is properly set first:
    //     //QuestManager.Instance.AddQuest(quest);
    //     //quest.questAdded = true;
    //     return quest;
    // }
    // public static Quest CreateQuest(string questName, string sceneName, string taskGroupName, int taskMinimumForCompletion = 0)
    // {
    //     TaskGroup taskGroup = new TaskGroup { taskGroupName = taskGroupName, taskMinimumForCompletion = taskMinimumForCompletion };
    //     return CreateQuest(questName, sceneName, taskGroup);
    // }


    public Quest FindQuestByName(string questName)
    {
        return QuestManager.Instance.FindQuestByName(questName);
    }

    public Quest FindQuestById(string questUniqueId)
    {
        return QuestManager.Instance.FindQuest(questUniqueId);
    }

    public TaskGroup GetTaskGroup()
    {
        return taskGroup;
    }

    public void AddTaskObject(QuestComponent go)
    {
        taskGroup.taskObjects.Add(go);
        // ! Minimum loses its meaning if I adjust every time I add a task object??
        if (taskGroup.taskMinimumForCompletion <= 0 || taskGroup.taskMinimumForCompletion < taskGroup.taskObjects.Count)
        {
            taskGroup.taskMinimumForCompletion = taskGroup.taskObjects.Count;
        }
        else
        {
            Debug.Log("Added task object w/o increment: " + go.uniqueID.ID + " to group: " + taskGroup.taskGroupName + " in scene: " + sceneName + ". Tasks remaining to complete group: " + TasksRemaining);
        }
        QuestManager.Instance.AddTaskObject(this, go, false);
    }

    private bool IsItOkayToActOnComplete()
    {
        if (TasksRemaining > 0 || taskGroup.actedOnComplete)
            return false;

        Debug.Log("IsItOkayToActOnComplete for group: " + taskGroup.taskGroupName + " in scene: " + sceneName + "? Tasks Remaining: " + TasksRemaining + ", ActedOnComplete: " + taskGroup.actedOnComplete);

        if (taskGroup.taskObjectToCompleteFirst != null && !taskGroup.completeReferencedTaskFirst)
        {
            if (taskGroup.taskObjectToCompleteFirst.actedOnComplete == true)
            {
                taskGroup.completeReferencedTaskFirst = true;
                return true;
            }
        }
        // either nothing to complete first or that first thing was completed
        return true;
    }

    public int FindTaskGroupIndexForTaskObject(QuestComponent go)
    {
        for (int i = 0; i < taskGroup.taskObjects.Count; i++)
        {
            if (taskGroup.taskObjects[i] == go)
            {
                return i;
            }
        }
        return -1; // not found
    }

    public QuestInfo ConvertToQuestInfo()
    {
        return taskGroup.ConvertToQuestInfo(questUniqueId.ID);
    }

    public void CompleteTaskObject(QuestComponent go)
    {
        int groupIndex = FindTaskGroupIndexForTaskObject(go);
        if (groupIndex == -1)
        {
            Debug.LogWarning("CompleteTaskObject: No TaskGroup found containing task object: " + go.uniqueID.ID);
            return;
        }
        CompleteTaskObjectAndActOnComplete(go);
    }

    // CompleteTaskObject calls this and checks that the task object exists in group
    private void CompleteTaskObjectAndActOnComplete(QuestComponent go)
    {
        QuestManager.Instance.CompletedQuestObject(this, go);

        if (taskGroup.taskObjects.Remove(go))
        {
            taskGroup.tasksCompleted++;
        }

        Debug.Log("Removing Quest Object: " + go.uniqueID.ID + ", tasks left: " + TasksRemaining + " for group: " + taskGroup.taskGroupName + " in scene: " + sceneName);

        if (IsItOkayToActOnComplete())
        {
            taskGroup.onTasksCompleted.Invoke();
            taskGroup.actedOnComplete = true;
            QuestManager.Instance.CompletedQuest(this);
        }
    }


    [ContextMenu("Manual Force OnTaskComplete")]
    public void ManualForceOnTaskComplete()
    {
        taskGroup.onTasksCompleted.Invoke();
    }

    [ContextMenu("Manual Force Complete Task Group")]
    public void ManualForceCompleteTaskGroup()
    {
        ForceCompleteTaskGroup();
    }

    public void ForceCompleteTaskGroup(bool actOnComplete = true)
    {
        Debug.Log("Force completing all tasks for group: " + taskGroup.taskGroupName + " in scene: " + sceneName);
        foreach (var go in taskGroup.taskObjects)
        {
            QuestManager.Instance.CompletedQuestObject(this, go);
        }
        taskGroup.taskObjects.Clear();
        taskGroup.tasksCompleted = taskGroup.taskMinimumForCompletion;
        if (taskGroup.autoActOnComplete || actOnComplete)
        {
            taskGroup.onTasksCompleted.Invoke();
            taskGroup.actedOnComplete = true;
            QuestManager.Instance.CompletedQuest(this);
        }
    }

    public void ClearTaskGroup()
    {
        Debug.Log("Clearing task group for scene (with NO progression updates): " + sceneName);
        taskGroup = new TaskGroup();
    }

    public void CompleteNonTaskObject(QuestComponent go)
    {
        Debug.Log("Completing non-task object: " + go.uniqueID.ID + " for scene: " + sceneName);
        QuestManager.Instance.CompletedNonQuestObject(go);
    }

    // Note this isn't "correct" in terms of checking GameState task completion,
    // however it is quick if this Quest did have the given task as an objective
    public bool IsGivenTaskComplete(QuestComponent go)
    {
        // check if the given task object is still in the task group (i.e. not complete)
        foreach (var taskObject in taskGroup.taskObjects)
        {
            if (taskObject == go)
            {
                return false;
            }
        }
        return true;
    }

    // Note this isn't "correct" in terms of checking GameState task completion,
    // however it is quick if this Quest did have the given tasks as objectives
    public bool AreGivenTasksComplete(List<QuestComponent> taskObjects)
    {
        // find components in task group and check if they are all complete (i.e. not in the task group anymore)
        foreach (var go in taskObjects)
        {
            if (IsGivenTaskComplete(go) == false)
            {
                Debug.Log("Task object: " + go.uniqueID.ID + " is not yet complete for scene: " + sceneName);
                return false;
            }
        }
        return true;
    }

#region ISaveable implementation
    private class QuestData
    {
        public string sceneName;
        public TaskGroup taskGroup;
        public List<string> taskGroupObjectIds;
        public bool actedOnComplete;
    }
    public object CaptureState()
    {
        var data = new QuestData
        {
            sceneName = sceneName,
            taskGroup = taskGroup,
            taskGroupObjectIds = new List<string>(),
            actedOnComplete = taskGroup.actedOnComplete
        };
        foreach (var go in taskGroup.taskObjects)
        {
            data.taskGroupObjectIds.Add(go.uniqueID.ID);
        }
        // clear up anything referencing scene objects before saving
        data.taskGroup.taskObjectToCompleteFirst = null;
        data.taskGroup.taskObjects = null;
        data.taskGroup.onTasksCompleted = null;
        return data;
    }
    public void RestoreState(object state)
    {
        // if (state is QuestData data)
        // {
        //     sceneName = data.sceneName;

        //     // This is pointless I realize since these are references that exist in the scene, I shouldn't have to add them back:
        //     // data.taskGroup.taskObjects = new List<QuestComponent>();
        //     // // find all QuestComponent objects matching the saved task group object ids and add them back to the task group
        //     // // Needed because we can't save/restore references to scene objects, but using UniqueIDs we can locate them
        //     // foreach (var go in FindObjectsByType<QuestComponent>(FindObjectsSortMode.None))
        //     // {
        //     //     // get index of the task object in the saved task group object ids, if it exists
        //     //     int index = data.taskGroupObjectIds.IndexOf(go.uniqueID.ID);
        //     //     if (index != -1)
        //     //     {
        //     //         data.taskGroup.taskObjects.Add(go);
        //     //         Debug.Log("Restored task object with id: " + go.uniqueID.ID + " to quest: " + QuestName + " in scene: " + sceneName);
        //     //     }
        //     // }
        //     // replace data that references scene objects with current level data
        //     data.taskGroup.taskObjects = taskGroup.taskObjects;
        //     data.taskGroup.onTasksCompleted = taskGroup.onTasksCompleted;
        //     data.taskGroup.taskObjectToCompleteFirst = taskGroup.taskObjectToCompleteFirst;
        //     // tasks counts should be same, unless we call for them to remove themselves again

        //     taskGroup = new TaskGroup();
        //     taskGroup = data.taskGroup;
        //     taskGroup.actedOnComplete = data.actedOnComplete;

        //     Debug.Log("Restored Quest: " + QuestName + " in scene: " + sceneName + ". Tasks remaining: " + TasksRemaining);
        //     // redo any completion actions if needed
        //     if (taskGroup.actedOnComplete)
        //     {
        //         Debug.Log("Restoring completed Quest: " + QuestName + " in scene: " + sceneName + ". INVOKING onTasksComplete.");
        //         taskGroup.onTasksCompleted.Invoke();
        //     }
        //     Debug.Log("Restored Quest state for quest: " + QuestName + " in scene: " + sceneName + ". Tasks remaining: " + TasksRemaining);
        // }
        // else
        // {
        //     Debug.LogError("RestoreState: Invalid state object for Quest: " + QuestName + " in scene: " + sceneName);
        // }
    }
#endregion ISaveable implementation


}