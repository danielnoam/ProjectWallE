using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

[CustomPropertyDrawer(typeof(LevelEvent), true)]
public class LevelEventDrawer : PropertyDrawer
{
    private static readonly Type[] eventTypes;
    private static readonly string[] eventNames;

    static LevelEventDrawer()
    {
        eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(LevelEvent)))
            .ToArray();

        eventNames = eventTypes.Select(t => FormatName(t.Name)).ToArray();
    }
    
    private static string FormatName(string name)
    {
        // Remove "Event" suffix if present
        if (name.EndsWith("Event"))
        {
            name = name.Substring(0, name.Length - 5);
        }
    
        // Add spaces before capital letters
        string result = "";
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                result += " ";
            }
            result += name[i];
        }
    
        return result;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get current type
        string typeName = property.managedReferenceFullTypename;
        
        int selectedIndex = -1;
        if (!string.IsNullOrEmpty(typeName))
        {
            selectedIndex = Array.FindIndex(eventTypes, t => typeName.Contains(t.Name));
        }

        // Draw dropdown with label
        Rect dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUI.Popup(dropdownRect, "Event Type", selectedIndex, eventNames);
        
        if (EditorGUI.EndChangeCheck() && newIndex >= 0)
        {
            var newEvent = (LevelEvent)Activator.CreateInstance(eventTypes[newIndex]);
            
            // Preserve common fields if possible
            if (property.managedReferenceValue != null)
            {
                var oldEvent = property.managedReferenceValue as LevelEvent;
                if (oldEvent != null)
                {
                    newEvent.triggerTime = oldEvent.triggerTime;
                    newEvent.description = oldEvent.description;
                    newEvent.hasTriggered = oldEvent.hasTriggered;
                }
            }
            
            property.managedReferenceValue = newEvent;
        }

        // Draw fields
        if (property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            float yOffset = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty prop = property.Copy();
            SerializedProperty endProperty = prop.GetEndProperty();
            
            prop.NextVisible(true);
            
            while (!SerializedProperty.EqualContents(prop, endProperty))
            {
                float height = EditorGUI.GetPropertyHeight(prop, true);
                Rect fieldRect = new Rect(position.x, yOffset, position.width, height);
                EditorGUI.PropertyField(fieldRect, prop, true);
                yOffset += height + EditorGUIUtility.standardVerticalSpacing;
                
                if (!prop.NextVisible(false))
                    break;
            }
            
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        if (property.managedReferenceValue != null)
        {
            SerializedProperty prop = property.Copy();
            SerializedProperty endProperty = prop.GetEndProperty();
            
            prop.NextVisible(true);
            
            while (!SerializedProperty.EqualContents(prop, endProperty))
            {
                height += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                
                if (!prop.NextVisible(false))
                    break;
            }
        }
        
        return height;
    }
}