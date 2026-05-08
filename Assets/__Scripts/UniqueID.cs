using UnityEngine;

//!! Important: Incorporating this as a data member requires referencing a component
// Also, this script is paired with:
// Editor/UniqueIDValidator to ensure every ISaveable has a UniqueID, and that all UniqueIDs are unique
// Editor/UniqueIDInspector to display the UniqueID in the Inspector (read-only) for debugging purposes
[DefaultExecutionOrder(-250)]
public class UniqueID : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string id;

    private static readonly System.Collections.Generic.HashSet<string> RuntimeAssignedIds =
        new System.Collections.Generic.HashSet<string>();

    public string ID => id;

    private void Awake()
    {
        EnsureRuntimeUniqueId();

        // Register if saveable
        if (TryGetComponent<ISaveable>(out var saveable))
        {
            SaveManager.Instance.Register(id, saveable);
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(id))
        {
            RuntimeAssignedIds.Remove(id);
        }
    }

    private void EnsureRuntimeUniqueId()
    {
        if (string.IsNullOrEmpty(id) || RuntimeAssignedIds.Contains(id))
        {
            id = System.Guid.NewGuid().ToString();
            while (RuntimeAssignedIds.Contains(id))
            {
                id = System.Guid.NewGuid().ToString();
            }
        }

        RuntimeAssignedIds.Add(id);
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        // Prefab assets should never carry runtime/save instance IDs.
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
        {
            if (!string.IsNullOrEmpty(id))
            {
                id = string.Empty;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            return;
        }

        bool needsNewId = string.IsNullOrEmpty(id);
        if (!needsNewId)
        {
            var allUniqueIds = UnityEngine.Object.FindObjectsByType<UniqueID>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allUniqueIds.Length; i++)
            {
                var other = allUniqueIds[i];
                if (other == null || other == this)
                {
                    continue;
                }

                if (other.id == id)
                {
                    needsNewId = true;
                    break;
                }
            }
        }

        if (needsNewId)
        {
            id = UnityEditor.GUID.Generate().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}

