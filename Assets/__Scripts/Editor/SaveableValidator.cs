#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
public static class SaveableValidator
{
    static SaveableValidator()
    {
        EditorApplication.update += Validate;
    }

    private static void Validate()
    {
        if (Application.isPlaying)
            return;

        var saveables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(mb => mb is ISaveable);

        foreach (var saveable in saveables)
        {
            var go = saveable.gameObject;

            if (!go.TryGetComponent<UniqueID>(out _))
            {
                Undo.AddComponent<UniqueID>(go);
                Debug.Log($"Added UniqueID to {go.name} because it implements ISaveable.");
            }
        }
    }
}
#endif
