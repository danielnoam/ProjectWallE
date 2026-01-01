using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(LevelEvent))]
public class LevelEventDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var triggerTimeProperty = property.FindPropertyRelative("triggerTime");
        var descriptionProperty = property.FindPropertyRelative("description");
        var eventTypeProperty = property.FindPropertyRelative("eventType");
        var enemyCountProperty = property.FindPropertyRelative("enemyCount");
        var structurePrefabProperty = property.FindPropertyRelative("structurePrefab");
        var spawnPositionProperty = property.FindPropertyRelative("spawnPosition");
        var onTriggerProperty = property.FindPropertyRelative("onTrigger");
        
        float yPos = position.y;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // Trigger Time
        EditorGUI.PropertyField(
            new Rect(position.x, yPos, position.width, lineHeight),
            triggerTimeProperty
        );
        yPos += lineHeight + spacing;
        
        // Description
        EditorGUI.PropertyField(
            new Rect(position.x, yPos, position.width, lineHeight),
            descriptionProperty
        );
        yPos += lineHeight + spacing;
        
        // Event Type
        EditorGUI.PropertyField(
            new Rect(position.x, yPos, position.width, lineHeight),
            eventTypeProperty
        );
        yPos += lineHeight + spacing;
        
        // Type-specific fields
        LevelEvent.EventType eventType = (LevelEvent.EventType)eventTypeProperty.enumValueIndex;
        
        switch (eventType)
        {
            case LevelEvent.EventType.SpawnEnemyWave:
                EditorGUI.PropertyField(
                    new Rect(position.x, yPos, position.width, lineHeight),
                    enemyCountProperty,
                    new GUIContent("Enemy Count")
                );
                break;
            
            case LevelEvent.EventType.SpawnStructure:
                EditorGUI.PropertyField(
                    new Rect(position.x, yPos, position.width, lineHeight),
                    structurePrefabProperty,
                    new GUIContent("Structure Prefab")
                );
                yPos += lineHeight + spacing;
                
                EditorGUI.PropertyField(
                    new Rect(position.x, yPos, position.width, lineHeight),
                    spawnPositionProperty,
                    new GUIContent("Spawn Position")
                );
                break;
            
            case LevelEvent.EventType.Custom:
                float propertyHeight = EditorGUI.GetPropertyHeight(onTriggerProperty);
                EditorGUI.PropertyField(
                    new Rect(position.x, yPos, position.width, propertyHeight),
                    onTriggerProperty,
                    new GUIContent("On Trigger")
                );
                break;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var eventTypeProperty = property.FindPropertyRelative("eventType");
        var onTriggerProperty = property.FindPropertyRelative("onTrigger");
        
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // Base height: triggerTime + description + eventType
        float height = (lineHeight + spacing) * 3;
        
        LevelEvent.EventType eventType = (LevelEvent.EventType)eventTypeProperty.enumValueIndex;
        
        switch (eventType)
        {
            case LevelEvent.EventType.SpawnEnemyWave:
                height += lineHeight + spacing; // enemyCount
                break;
            
            case LevelEvent.EventType.SpawnStructure:
                height += (lineHeight + spacing) * 2; // prefab + position
                break;
            
            case LevelEvent.EventType.Custom:
                height += EditorGUI.GetPropertyHeight(onTriggerProperty) + spacing;
                break;
        }
        
        return height;
    }
}