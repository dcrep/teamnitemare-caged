using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

// Note: QuestInfo defined in GameState module

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
        foreach (var questComponent in taskObjects)
        {
            // Race condition workaround:
            var uid = questComponent.uniqueID ?? questComponent.GetComponent<UniqueID>();
            if (uid == null) { Debug.LogError($"Missing UniqueID on {questComponent.name}"); continue; }
            Debug.Log("Q->ConvertToQuestInfo uniqueID: " + uid.ID + ", name: " + questComponent.gameObject.name + ", tag: " + questComponent.questTaskTag + ", isCollectible: " + questComponent.isCollectible);
            questInfo.questTasks.Add(new QuestTask {
                taskUniqueId = uid.ID, taskName = questComponent.gameObject.name,
                questTaskTag = questComponent.questTaskTag, isCollectibleTask = questComponent.isCollectible,
                isCompleted = actedOnComplete, taskDescription = "",
                taskValue = 1, taskMaxValue = 1, oneTimeCompletion = true, requiredTasks = null});
        }
        Debug.Log("Q->Converted TaskGroup: " + taskGroupName + " to QuestInfo with " + questInfo.questTasks.Count + " tasks for scene: " + SceneManager.GetActiveScene().name);
        return questInfo;
    }
    public QuestTask ConvertToQuestTask(int index)
    {
        if (index < 0 || index >= taskObjects.Count)
        {
            Debug.LogError("ConvertToQuestTask: Index out of range for task objects. Index: " + index + ", Count: " + taskObjects.Count);
            return null;
        }
        var questComponent = taskObjects[index];
        return new QuestTask {
            taskUniqueId = questComponent.uniqueID.ID, taskName = questComponent.gameObject.name,
            questTaskTag = questComponent.questTaskTag, isCollectibleTask = questComponent.isCollectible,
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
        Debug.Log("Q->UniqueId: " + questUniqueId.ID + " for quest: " + QuestName + " in scene: " + sceneName);
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

    public void AddTaskObject(QuestComponent questComponent)
    {
        taskGroup.taskObjects.Add(questComponent);
        // ! Minimum loses its meaning if I adjust every time I add a task object??
        if (taskGroup.taskMinimumForCompletion <= 0 || taskGroup.taskMinimumForCompletion < taskGroup.taskObjects.Count)
        {
            taskGroup.taskMinimumForCompletion = taskGroup.taskObjects.Count;
        }
        else
        {
            Debug.Log("Added task object w/o increment: " + questComponent.uniqueID.ID + " to group: " + taskGroup.taskGroupName + " in scene: " + sceneName + ". Tasks remaining to complete group: " + TasksRemaining);
        }
        QuestManager.Instance.AddTaskObject(this, questComponent, false);
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

    public int FindTaskGroupIndexForTaskObject(QuestComponent questComponent)
    {
        for (int i = 0; i < taskGroup.taskObjects.Count; i++)
        {
            if (taskGroup.taskObjects[i] == questComponent)
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

    public void CompleteTaskObject(QuestComponent questComponent)
    {
        int groupIndex = FindTaskGroupIndexForTaskObject(questComponent);
        if (groupIndex == -1)
        {
            Debug.LogWarning("CompleteTaskObject: No TaskGroup found containing task object: " + questComponent.uniqueID.ID);
            return;
        }
        CompleteTaskObjectAndActOnComplete(questComponent);
    }

    // CompleteTaskObject calls this and checks that the task object exists in group
    private void CompleteTaskObjectAndActOnComplete(QuestComponent questComponent)
    {
        QuestManager.Instance.CompletedQuestObject(this, questComponent);

        if (taskGroup.taskObjects.Remove(questComponent))
        {
            taskGroup.tasksCompleted++;
        }
        //if (questComponent.isCollectible)
        if (questComponent.destroyOnComplete)
        {
            Destroy(questComponent.gameObject);
        }

        Debug.Log("Removing Quest Object: " + questComponent.uniqueID.ID + ", tasks left: " + TasksRemaining + " for group: " + taskGroup.taskGroupName + " in scene: " + sceneName);

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
        //!! No gameObject destruction here. It is literally just calling Invoke.
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
        foreach (var questComponent in taskGroup.taskObjects)
        {
            QuestManager.Instance.CompletedQuestObject(this, questComponent);
            //if (questComponent.isCollectible)
            if (questComponent.destroyOnComplete)
            {
                Destroy(questComponent.gameObject);
            }
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

    public void CompleteNonTaskObject(QuestComponent questComponent)
    {
        Debug.Log("Completing non-task object: " + questComponent.uniqueID.ID + " for scene: " + sceneName);
        QuestManager.Instance.CompletedNonQuestObject(questComponent);
    }

    // Note this isn't "correct" in terms of checking GameState task completion,
    // however it is quick if this Quest did have the given task as an objective
    public bool IsGivenTaskComplete(QuestComponent questComponent)
    {
        // check if the given task object is still in the task group (i.e. not complete)
        foreach (var taskObject in taskGroup.taskObjects)
        {
            if (taskObject == questComponent)
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
        foreach (var questComponent in taskObjects)
        {
            if (IsGivenTaskComplete(questComponent) == false)
            {
                Debug.Log("Task object: " + questComponent.uniqueID.ID + " is not yet complete for scene: " + sceneName);
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
        foreach (var questComponent in taskGroup.taskObjects)
        {
            data.taskGroupObjectIds.Add(questComponent.uniqueID.ID);
        }
        // clear up anything referencing scene objects before saving
        data.taskGroup.taskObjectToCompleteFirst = null;
        data.taskGroup.taskObjects = null;
        data.taskGroup.onTasksCompleted = null;
        return data;
    }
    // public string taskGroupName = "";
    // public bool isCollectibleGroup = false;
    // public bool completeReferencedTaskFirst = false;
    // public TaskGroup taskObjectToCompleteFirst;
    // public List<QuestComponent> taskObjects = new List<QuestComponent>();
    // public bool autoActOnComplete = false;
    // public UnityEvent onTasksCompleted = new UnityEvent();
    // public bool actedOnComplete = false;
    // public int taskMinimumForCompletion = 0;
    // public int tasksCompleted = 0;
    public void RestoreState(object state)
    {
        if (state is QuestData data)
        {
            sceneName = data.sceneName;
            taskGroup.actedOnComplete = data.actedOnComplete;
            taskGroup.tasksCompleted = data.taskGroup.tasksCompleted;

            // go through GameState completed quests and remove from the tasks list 

            // uniqueID might not be initialized yet, so we use the UniqueID component to get the ID for lookup in GameState
            string uniqueID = questUniqueId == null ? GetComponent<UniqueID>().ID : questUniqueId.ID;

            QuestInfo questInfo = GameManager.Instance.gameState.GetQuestInfo(sceneName, uniqueID);
            if (questInfo == null)
            {
                Debug.LogError("Q->RestoreState: No QuestInfo found in GameState for quest: " + QuestName + " in scene: " + sceneName);
                return;
            }
            // restore quest state from GameState QuestInfo
            if (taskGroup.tasksCompleted != questInfo.numObjectivesCompleted)
            {
                Debug.LogWarning("Q->RestoreState: Mismatch between saved task completion count and GameState QuestInfo for quest: " + QuestName + " in scene: " + sceneName + ". Saved tasks completed: " + taskGroup.tasksCompleted + ", GameState tasks completed: " + questInfo.numObjectivesCompleted);
                taskGroup.tasksCompleted = questInfo.numObjectivesCompleted;
            }
            if (questInfo.isCompleted)
            {
                Debug.Log("Q->RestoreState: Quest: " + QuestName + " in scene: " + sceneName + " is marked as completed in GameState. # tasks completed: " + questInfo.numObjectivesCompleted + "/" + questInfo.totalObjectives);
            }
            // remove completed tasks within taskGroup.taskObjects based on GameState QuestInfo
            foreach(var questTask in questInfo.questTasks)
            {
                if (questTask.isCompleted)
                {
                    Debug.Log("Q->RestoreState: Removing completed task with id: " + questTask.taskUniqueId + " from quest: " + QuestName + " in scene: " + sceneName);
                    // remove completed task within the list
                    taskGroup.taskObjects.RemoveAt(taskGroup.taskObjects.FindIndex(qc => qc.SAFEGetUniqueID() == questTask.taskUniqueId));
                }
            }


            // This is pointless I realize since these are references that exist in the scene, I shouldn't have to add them back:
            // data.taskGroup.taskObjects = new List<QuestComponent>();
            // // find all QuestComponent objects matching the saved task group object ids and add them back to the task group
            // // Needed because we can't save/restore references to scene objects, but using UniqueIDs we can locate them
            // foreach (var go in FindObjectsByType<QuestComponent>(FindObjectsSortMode.None))
            // {
            //     // get index of the task object in the saved task group object ids, if it exists
            //     int index = data.taskGroupObjectIds.IndexOf(go.uniqueID.ID);
            //     if (index != -1)
            //     {
            //         data.taskGroup.taskObjects.Add(go);
            //         Debug.Log("Restored task object with id: " + go.uniqueID.ID + " to quest: " + QuestName + " in scene: " + sceneName);
            //     }
            // }
            // replace data that references scene objects with current level data
        //     data.taskGroup.taskObjects = taskGroup.taskObjects;
        //     data.taskGroup.onTasksCompleted = taskGroup.onTasksCompleted;
        //     data.taskGroup.taskObjectToCompleteFirst = taskGroup.taskObjectToCompleteFirst;
        //     // tasks counts should be same, unless we call for them to remove themselves again

        //     taskGroup = new TaskGroup();
        //     taskGroup = data.taskGroup;
        //     taskGroup.actedOnComplete = data.actedOnComplete;

        //     Debug.Log("Q->Restored Quest: " + QuestName + " in scene: " + sceneName + ". Tasks remaining: " + TasksRemaining);
        //     // redo any completion actions if needed
        //     if (taskGroup.actedOnComplete)
        //     {
        //         //Debug.Log("Restoring completed Quest: " + QuestName + " in scene: " + sceneName + ". INVOKING onTasksComplete.");
        //         //! Change - Invoke is 'playback' and we want to restore STATE
        //         //taskGroup.onTasksCompleted.Invoke();
        //     }
        //     Debug.Log("Q->Restored Quest state for quest: " + QuestName + " in scene: " + sceneName + ". Tasks remaining: " + TasksRemaining);
        // }
        // else
        // {
        //     Debug.LogError("Q->RestoreState: Invalid state object for Quest: " + QuestName + " in scene: " + sceneName);
        // }



            Debug.Log("Q->Restored Quest state for quest: " + QuestName + " in scene: " + sceneName + " from GameState. Tasks remaining: " + TasksRemaining);

        }
    }
#endregion ISaveable implementation


}