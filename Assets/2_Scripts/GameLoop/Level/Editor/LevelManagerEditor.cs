using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelManager))]
internal class LevelManagerEditor : Editor
{
    private static readonly Color CompletedColor = new Color(0.3f, 0.9f, 0.3f);
    private static readonly Color ActiveColor = new Color(1f, 0.85f, 0.2f);
    private static readonly Color PausedColor = new Color(0.6f, 0.6f, 1f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying) return;

        var manager = (LevelManager)target;

        EditorGUILayout.Space(10);

        DrawLevelState(manager);
        DrawTimeline(manager);
        DrawObjectives(manager);

        Repaint();
    }

    private void DrawLevelState(LevelManager manager)
    {
        string state;
        Color color;

        if (!manager.IsLevelActive)
        {
            state = "Inactive";
            color = Color.gray;
        }
        else if (manager.HasActiveObjectives)
        {
            state = "Objectives Active";
            color = PausedColor;
        }
        else
        {
            state = "Timeline Playing";
            color = CompletedColor;
        }

        Color prev = GUI.color;
        GUI.color = color;
        EditorGUILayout.LabelField("State", state, EditorStyles.boldLabel);
        GUI.color = prev;
    }

    private void DrawTimeline(LevelManager manager)
    {
        double time = manager.TimelineTime;
        double duration = manager.TimelineDuration;
        float progress = duration > 0 ? (float)(time / duration) : 0f;

        Rect rect = EditorGUILayout.GetControlRect(false, 18);
        EditorGUI.ProgressBar(rect, progress, $"{time:F1}s / {duration:F1}s");
    }

    private void DrawObjectives(LevelManager manager)
    {
        if (!manager.HasActiveObjectives) return;

        EditorGUILayout.Space(5);

        foreach (var objective in manager.ActiveObjectives)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = objective.IsCompleted ? CompletedColor : ActiveColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prev;

            EditorGUILayout.LabelField(objective.Description, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(objective.ProgressText);

            EditorGUILayout.EndVertical();
        }
    }
}