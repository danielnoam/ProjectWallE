using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;

[CustomPropertyDrawer(typeof(ExposedReference<>))]
internal class ExposedReferenceDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var director = TimelineEditor.inspectedDirector;
        var exposedNameProp = property.FindPropertyRelative("exposedName");
        string nameStr = exposedNameProp.stringValue;

        Object resolved = null;
        if (director && !string.IsNullOrEmpty(nameStr))
            resolved = director.GetReferenceValue(new PropertyName(nameStr), out _);

        var type = fieldInfo?.FieldType.IsGenericType == true
            ? fieldInfo.FieldType.GetGenericArguments()[0]
            : typeof(Object);

        EditorGUI.BeginProperty(position, label, property);

        var newValue = EditorGUI.ObjectField(position, label, resolved, type, true);

        if (newValue != resolved && director)
        {
            Undo.RecordObject(director, "Assign Binding");
            if (string.IsNullOrEmpty(nameStr))
            {
                nameStr = System.Guid.NewGuid().ToString();
                exposedNameProp.stringValue = nameStr;
                property.serializedObject.ApplyModifiedProperties();
            }
            director.SetReferenceValue(new PropertyName(nameStr), newValue);
            EditorUtility.SetDirty(director);
        }

        EditorGUI.EndProperty();
    }
}