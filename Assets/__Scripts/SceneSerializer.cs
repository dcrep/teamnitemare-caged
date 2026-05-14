using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Credit for SceneSerializer goes to both this video and the forum post it links to:
// How to load scenes in the Background for games with HUGE worlds | Unity Tutorial @ https://youtu.be/6-0zD9Xyu5c
// Inspector Field for Scene Asset @ https://discussions.unity.com/t/inspector-field-for-scene-asset/40763/3
// also see SceneReference unity asset: https://github.com/starikcetin/Eflatun.SceneReference

[System.Serializable]
public class SceneSerializer
{
    [SerializeField] private Object _sceneAsset;
    [SerializeField] private string _sceneName = "";

    public string SceneName
    {
        get { return _sceneName; }
    }

    // makes it work with existing Unity methods like LoadScene
    public static implicit operator string(SceneSerializer serializer)
    {
        return serializer.SceneName;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneSerializer))]
public class SceneSerializerPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, GUIContent.none, property);

        SerializedProperty sceneAsset = property.FindPropertyRelative("_sceneAsset");
        SerializedProperty sceneName = property.FindPropertyRelative("_sceneName");
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        if (sceneAsset != null)
        {
            sceneAsset.objectReferenceValue = EditorGUI.ObjectField(position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
            if (sceneAsset.objectReferenceValue != null)
            {
                sceneName.stringValue = (sceneAsset.objectReferenceValue as SceneAsset).name;
            }
        }
        EditorGUI.EndProperty();
    }
}
#endif