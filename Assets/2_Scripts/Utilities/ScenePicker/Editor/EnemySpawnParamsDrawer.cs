// using ProjectWallE.GameLoop;
// using UnityEditor;
// using UnityEditor.Timeline;
// using UnityEngine;
// using UnityEngine.Playables;
//
// [CustomPropertyDrawer(typeof(EnemySpawnParams))]
// internal class EnemySpawnParamsDrawer : PropertyDrawer
// {
//     private static EnemySpawnPoint[] _cachedSpawnPoints;
//     private static Object _activeTarget;
//     private static string _activePropertyPath;
//     private static EnemySpawnPoint _pendingAssignment;
//     private static bool _scenePickerActive;
//
//     private const float PickRadius = 50f;
//
//     public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//     {
//         if (!property.isExpanded)
//             return EditorGUIUtility.singleLineHeight;
//
//         float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
//
//         var iter = property.Copy();
//         var end = property.GetEndProperty();
//         if (!iter.NextVisible(true)) return height;
//
//         while (!SerializedProperty.EqualContents(iter, end))
//         {
//             height += EditorGUI.GetPropertyHeight(iter, true) + EditorGUIUtility.standardVerticalSpacing;
//             if (!iter.NextVisible(false)) break;
//         }
//
//         if (ShowPickButton(property))
//             height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
//
//         return height;
//     }
//
//     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//     {
//         bool isThisProperty = _activeTarget == property.serializedObject.targetObject &&
//                               _activePropertyPath == property.propertyPath;
//
//         if (isThisProperty && _pendingAssignment)
//         {
//             ApplyAssignment(property, _pendingAssignment);
//             _pendingAssignment = null;
//             StopPicking();
//         }
//
//         var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//         property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
//
//         if (!property.isExpanded) return;
//
//         float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
//
//         var iter = property.Copy();
//         var end = property.GetEndProperty();
//         if (!iter.NextVisible(true)) return;
//
//         while (!SerializedProperty.EqualContents(iter, end))
//         {
//             float h = EditorGUI.GetPropertyHeight(iter, true);
//             EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), iter, true);
//             y += h + EditorGUIUtility.standardVerticalSpacing;
//             if (!iter.NextVisible(false)) break;
//         }
//
//         if (ShowPickButton(property))
//         {
//             bool isPicking = isThisProperty && _scenePickerActive;
//             var prevColor = GUI.color;
//             if (isPicking) GUI.color = Color.yellow;
//
//             if (GUI.Button(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
//                 isPicking ? "Click in Scene... (Esc to cancel)" : "Pick from Scene"))
//             {
//                 if (isPicking)
//                 {
//                     StopPicking();
//                 }
//                 else
//                 {
//                     _activeTarget = property.serializedObject.targetObject;
//                     _activePropertyPath = property.propertyPath;
//                     _scenePickerActive = true;
//                     _cachedSpawnPoints = Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Include);
//                     SceneView.duringSceneGui += OnSceneGUI;
//                     SceneView.RepaintAll();
//                 }
//             }
//
//             GUI.color = prevColor;
//         }
//     }
//
//     private static bool ShowPickButton(SerializedProperty property)
//     {
//         var posType = (SpawnPositionType)property.FindPropertyRelative("spawnPosition").enumValueIndex;
//         return posType == SpawnPositionType.Specific;
//     }
//
//     private static void StopPicking()
//     {
//         _activeTarget = null;
//         _activePropertyPath = null;
//         _scenePickerActive = false;
//         _cachedSpawnPoints = null;
//         SceneView.duringSceneGui -= OnSceneGUI;
//         SceneView.RepaintAll();
//     }
//
//     private static void OnSceneGUI(SceneView sceneView)
//     {
//         int controlId = GUIUtility.GetControlID(FocusType.Passive);
//         HandleUtility.AddDefaultControl(controlId);
//
//         var spawnPoints = _cachedSpawnPoints;
//         var e = Event.current;
//
//         EnemySpawnPoint hovered = null;
//         float closestDist = float.MaxValue;
//
//         foreach (var sp in spawnPoints)
//         {
//             float dist = Vector2.Distance(HandleUtility.WorldToGUIPoint(sp.transform.position), e.mousePosition);
//             if (dist < closestDist)
//             {
//                 closestDist = dist;
//                 hovered = sp;
//             }
//         }
//
//         bool canSelect = hovered != null && closestDist < PickRadius;
//
//         foreach (var sp in spawnPoints)
//         {
//             bool isSelectable = sp == hovered && canSelect;
//             Handles.color = isSelectable ? Color.green : new Color(1f, 1f, 1f, 0.4f);
//             Handles.DrawWireDisc(sp.transform.position, Vector3.up, sp.SpawnPointRange);
//             Handles.Label(sp.transform.position + Vector3.up * 2f, sp.name);
//         }
//
//         if (e.type == EventType.MouseDown && e.button == 0 && canSelect)
//         {
//             Debug.Log($"[EnemySpawnParams] Picked spawn point: {hovered.name}");
//             _pendingAssignment = hovered;
//             e.Use();
//         }
//         else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
//         {
//             e.Use();
//             StopPicking();
//         }
//
//         HandleUtility.Repaint();
//     }
//
//     private static void ApplyAssignment(SerializedProperty property, EnemySpawnPoint spawnPoint)
//     {
//         var director = TimelineEditor.inspectedDirector;
//         if (!director)
//         {
//             Debug.LogWarning("[EnemySpawnParams] No PlayableDirector found. Open the Timeline window with a director selected.");
//             return;
//         }
//
//         Undo.RecordObject(property.serializedObject.targetObject, "Assign Spawn Point");
//         Undo.RecordObject(director, "Assign Spawn Point");
//
//         var internalItems = property.FindPropertyRelative("spawnPoints")
//             .FindPropertyRelative("internalItems");
//
//         for (int i = 0; i < internalItems.arraySize; i++)
//         {
//             var existingNameProp = internalItems.GetArrayElementAtIndex(i)
//                 .FindPropertyRelative("item")
//                 .FindPropertyRelative("exposedName");
//             var existingKey = new PropertyName(existingNameProp.stringValue);
//             if (director.GetReferenceValue(existingKey, out _) is EnemySpawnPoint existing && existing == spawnPoint)
//                 return;
//         }
//
//         int index = internalItems.arraySize;
//         internalItems.InsertArrayElementAtIndex(index);
//         BindExposedReference(internalItems.GetArrayElementAtIndex(index).FindPropertyRelative("item"), spawnPoint, director);
//
//         property.serializedObject.ApplyModifiedProperties();
//         EditorUtility.SetDirty(property.serializedObject.targetObject);
//         EditorUtility.SetDirty(director);
//     }
//
//     private static void BindExposedReference(SerializedProperty exposedRefProp, EnemySpawnPoint spawnPoint, PlayableDirector director)
//     {
//         var exposedNameProp = exposedRefProp.FindPropertyRelative("exposedName");
//         string nameStr = exposedNameProp.stringValue;
//         if (string.IsNullOrEmpty(nameStr))
//         {
//             nameStr = System.Guid.NewGuid().ToString();
//             exposedNameProp.stringValue = nameStr;
//         }
//         director.SetReferenceValue(new PropertyName(nameStr), spawnPoint);
//     }
// }