/*
#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private bool showDictionary;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gameManager = (GameManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        if (GUILayout.Button("Display Scene Progression Dictionary"))
        {
            showDictionary = true;
            LogDictionary(gameManager);
        }

        if (!showDictionary)
        {
            return;
        }

        DrawDictionary(gameManager);
    }

    private void DrawDictionary(GameManager gameManager)
    {
        if (gameManager == null || gameManager.gameState == null)
        {
            EditorGUILayout.HelpBox("GameManager or GameState is null.", MessageType.Warning);
            return;
        }

        var dictionary = gameManager.gameState.sceneProgressionInfo;
        if (dictionary == null)
        {
            EditorGUILayout.HelpBox("sceneProgressionInfo is null.", MessageType.Warning);
            return;
        }

        if (dictionary.Count == 0)
        {
            EditorGUILayout.HelpBox("sceneProgressionInfo is empty.", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox($"Entries: {dictionary.Count}", MessageType.None);

        foreach (var pair in dictionary)
        {
            string values = pair.Value == null || pair.Value.Count == 0
                ? "(no values)"
                : string.Join(", ", pair.Value);

            EditorGUILayout.LabelField(pair.Key, values);
        }
    }

    private static void LogDictionary(GameManager gameManager)
    {
        if (gameManager == null || gameManager.gameState == null)
        {
            Debug.LogWarning("GameManager or GameState is null.");
            return;
        }

        var dictionary = gameManager.gameState.sceneProgressionInfo;
        if (dictionary == null)
        {
            Debug.LogWarning("sceneProgressionInfo is null.");
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"sceneProgressionInfo entries: {dictionary.Count}");

        foreach (var pair in dictionary)
        {
            string values = pair.Value == null || pair.Value.Count == 0
                ? "(no values)"
                : string.Join(", ", pair.Value);

            builder.AppendLine($"{pair.Key}: {values}");
        }

        Debug.Log(builder.ToString());
    }
}
#endif
*/