using ProjectWallE.GameLoop;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnemySpawnEntry))]
public class EnemySpawnEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var enemy = property.FindPropertyRelative("enemy");
        var count = property.FindPropertyRelative("count");

        float countWidth = 48f;
        float spacing = 4f;

        var enemyRect = new Rect(position.x, position.y, position.width - countWidth - spacing, position.height);
        var countRect = new Rect(position.xMax - countWidth, position.y, countWidth, position.height);

        EditorGUI.PropertyField(enemyRect, enemy, GUIContent.none);
        EditorGUI.PropertyField(countRect, count, GUIContent.none);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
}