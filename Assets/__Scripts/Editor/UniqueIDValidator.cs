#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public static class UniqueIDValidator
{
    static UniqueIDValidator()
    {
        EditorApplication.hierarchyChanged += ValidateScene;
    }

    private static void ValidateScene()
    {
        // Only validate when not playing
        if (Application.isPlaying)
            return;

        var allUniqueIDs = Object.FindObjectsByType<UniqueID>(FindObjectsSortMode.None);
        var seen = new HashSet<string>();

        foreach (var uid in allUniqueIDs)
        {
            // Auto-generate missing IDs
            if (string.IsNullOrEmpty(uid.ID))
            {
                Undo.RecordObject(uid, "Generate UniqueID");
                uid.gameObject.GetComponent<UniqueID>().OnValidate();
            }

            // Detect duplicates
            if (!seen.Add(uid.ID))
            {
                Debug.LogError($"Duplicate UniqueID detected: {uid.ID} on {uid.gameObject.name}");
            }
        }
    }
}
#endif
