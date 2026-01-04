using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Open Timeline Editor", GUILayout.Height(30)))
        {
            LevelTimelineEditor.OpenWindow((LevelManager)target);
        }
    }
}