using System;
using UnityEngine;

// TODO: Collectibles with same-name, count etc implementation

[RequireComponent(typeof(UniqueID))]
public class QuestComponent : MonoBehaviour
{
    // just need an id & collectible bool
    public UniqueID uniqueID;
    public string questTaskTag;
    public bool isCollectible = false;
    public bool onceOnly = true;
    //public int count = 1;

    private void Awake()
    {
        if (uniqueID == null)
            uniqueID = GetComponent<UniqueID>();
    }

    // void OnValidate()
    // {
    //     // make sure COLLECTIBLE prefix is there if collectible
    //     if (isCollectible && !questComponentId.StartsWith("COLLECTIBLE:"))
    //     {
    //         questComponentId = "COLLECTIBLE:" + questComponentId;
    //     }
    //     else if (!isCollectible && questComponentId.StartsWith("COLLECTIBLE:"))
    //     {
    //         isCollectible = true;
    //     }
    // }

    
    [ContextMenu("Manual TaskCompletion")]
    public void ManualTaskCompletion()
    {
        CompleteTask();
    }

    public void CompleteTask()
    {
        QuestManager.Instance.CompleteTaskObjectForUnknownQuest(this);
    }
}