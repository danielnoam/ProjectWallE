using ProjectWallE.GameLoop;
using UnityEditor;
using UnityEngine;

internal static class SpawnEnemyInspectorGUI
{
    public static void DrawEnemyAndPositionFields(SerializedObject serializedObject)
    {
        var enemyType = serializedObject.FindProperty("enemyType");
        var count = serializedObject.FindProperty("count");
        var enemies = serializedObject.FindProperty("enemies");
        var enemyEntries = serializedObject.FindProperty("enemyEntries");
        var spawnPosition = serializedObject.FindProperty("spawnPosition");
        var spawnPoints = serializedObject.FindProperty("spawnPoints");

        EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enemyType);

        switch ((EnemySourceType)enemyType.enumValueIndex)
        {
            case EnemySourceType.Random:
                EditorGUILayout.PropertyField(count);
                break;
            case EnemySourceType.RandomByType:
                EditorGUILayout.PropertyField(count);
                EditorGUILayout.PropertyField(enemies);
                break;
            case EnemySourceType.ByType:
                EditorGUILayout.PropertyField(enemyEntries);
                break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnPosition);

        if ((SpawnPositionType)spawnPosition.enumValueIndex == SpawnPositionType.Specific)
        {
            EditorGUILayout.PropertyField(spawnPoints);
        }
    }
}

[CustomEditor(typeof(SpawnEnemyWaveMarker))]
[CanEditMultipleObjects]
public class SpawnEnemyWaveMarkerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

#if UNITY_EDITOR
        var skipInEditor = serializedObject.FindProperty("skipInEditor");
        if (skipInEditor != null)
        {
            EditorGUILayout.PropertyField(skipInEditor);
            EditorGUILayout.Space();
        }
#endif

        SpawnEnemyInspectorGUI.DrawEnemyAndPositionFields(serializedObject);

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(SpawnEnemyEvent))]
public class SpawnEnemyEventInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

#if UNITY_EDITOR
        var skipInEditor = serializedObject.FindProperty("skipInEditor");
        if (skipInEditor != null)
        {
            EditorGUILayout.PropertyField(skipInEditor);
            EditorGUILayout.Space();
        }
#endif

        var spawnInterval = serializedObject.FindProperty("spawnInterval");
        EditorGUILayout.LabelField("Spawn", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnInterval);
        EditorGUILayout.Space();

        SpawnEnemyInspectorGUI.DrawEnemyAndPositionFields(serializedObject);

        serializedObject.ApplyModifiedProperties();
    }
}
