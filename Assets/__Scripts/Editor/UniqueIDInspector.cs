#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UniqueID))]
public class UniqueIDInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UniqueID uniqueID = (UniqueID)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unique ID", uniqueID.ID);
    }
}
#endif