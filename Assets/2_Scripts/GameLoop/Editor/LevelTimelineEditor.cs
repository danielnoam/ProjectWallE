using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

public class LevelTimelineEditor : EditorWindow
{
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 5f;
    private const float ZoomIncrement = 0.25f;
    
    private const float TimelineHeight = 50f;
    private const float TimelineMargin = 20;
    private const float TimeRulerHeight = 20f;
    
    private const float MarkerWidth = 10f;
    private const float MarkerPadding = 20;
    
    private const int EventFontSize = 10;
    private const float EventLabelWidth = 100f;
    private const float EventLabelHeight = 15f;
    
    private LevelManager _targetLevel;
    private SerializedObject _serializedLevel;
    private Vector2 _timelineScrollPos;
    private float _zoom = 1f;
    private Rect _timelineRect;
    private Rect _timeRulerRect;
    private int _selectedEventIndex = -1;
    
    [MenuItem("Tools/Level Timeline Editor")]
    private static void OpenWindowMenu()
    {
        var window = GetWindow<LevelTimelineEditor>("Level Timeline");
        window.Show();
    }
    
    public static void OpenWindow(LevelManager level)
    {
        var window = GetWindow<LevelTimelineEditor>("Level Timeline");
        window._targetLevel = level;
        window._serializedLevel = new SerializedObject(level);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        _targetLevel = (LevelManager)EditorGUILayout.ObjectField("Level Manager", _targetLevel, typeof(LevelManager), true);
        if (EditorGUI.EndChangeCheck() && _targetLevel)
        {
            _serializedLevel = new SerializedObject(_targetLevel);
        }
    
        if (!_targetLevel)
        {
            EditorGUILayout.HelpBox("Select a LevelManager to edit its timeline", MessageType.Info);
            return;
        }
    
        _serializedLevel ??= new SerializedObject(_targetLevel);
    
        _serializedLevel.Update();
        
        if (_selectedEventIndex >= _targetLevel.GetEvents().Count)
        {
            _selectedEventIndex = -1;
        }

        HandleKeyboardInput();

        EditorGUILayout.Space();
        
        DrawLevelDetails();
    
        EditorGUILayout.Space();
    
        DrawTimeline();
    
        EditorGUILayout.Space();
    
        DrawEventDetails();
    
        _serializedLevel.ApplyModifiedProperties();
    }

    private void HandleKeyboardInput()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
        {
            if (_selectedEventIndex >= 0 && _selectedEventIndex < _targetLevel.GetEvents().Count)
            {
                DeleteEvent(_selectedEventIndex);
                Event.current.Use();
            }
        }
    }
    
    private void DrawLevelDetails()
    {
        EditorGUILayout.LabelField("Level Details", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Duration: {_targetLevel.Duration}s");
        EditorGUILayout.LabelField($"Events: {_targetLevel.GetEvents().Count}");
    }

    private void DrawZoomControls()
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField("Zoom:", GUILayout.Width(45));
        
        if (GUILayout.Button("-", GUILayout.Width(30)))
        {
            _zoom = Mathf.Max(MinZoom, _zoom - ZoomIncrement);
            Repaint();
        }
        
        _zoom = EditorGUILayout.Slider(_zoom, MinZoom, MaxZoom);
        
        if (GUILayout.Button("+", GUILayout.Width(30)))
        {
            _zoom = Mathf.Min(MaxZoom, _zoom + ZoomIncrement);
            Repaint();
        }
        
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            _zoom = 1f;
            _timelineScrollPos = Vector2.zero;
            Repaint();
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTimeline()
    {
        float duration = _targetLevel.Duration;
    
        EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(TimelineMargin * 0.5f);
        
        float timelineWidth = (position.width - TimelineMargin) * _zoom;
    
        _timelineScrollPos = EditorGUILayout.BeginScrollView(
            _timelineScrollPos, 
            GUILayout.Height(TimelineHeight + TimeRulerHeight + 10)
        );
        
        // Time ruler rect
        _timeRulerRect = GUILayoutUtility.GetRect(timelineWidth, TimeRulerHeight);
        DrawTimeRuler(_timeRulerRect, duration);
        
        // Timeline rect
        _timelineRect = GUILayoutUtility.GetRect(timelineWidth, TimelineHeight);
        EditorGUI.DrawRect(_timelineRect, new Color(0.3f, 0.3f, 0.3f));
        
        DrawTimelineGrid(_timelineRect, duration);
        DrawEvents(_timelineRect, duration);
        
        if (_timelineRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
        {
            float zoomDelta = -Event.current.delta.y * 0.05f;
            _zoom = Mathf.Clamp(_zoom + zoomDelta, MinZoom, MaxZoom);
            Event.current.Use();
            Repaint();
        }
    
        EditorGUILayout.EndScrollView();
    
        GUILayout.Space(TimelineMargin * 0.5f);
        EditorGUILayout.EndHorizontal();
    
        EditorGUILayout.Space();

        DrawZoomControls();
    
        EditorGUILayout.Space();
    
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Event"))
        {
            AddEventAtTime(duration * 0.5f);
        }
        if (GUILayout.Button("Clear Selection"))
        {
            _selectedEventIndex = -1;
            Repaint();
        }
        if (GUILayout.Button("Delete Event"))
        {
            DeleteEvent(_selectedEventIndex);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTimeRuler(Rect rect, float duration)
    {
        // Dark background for ruler
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
        
        float markerInterval = GetMarkerInterval();
        int markerCount = Mathf.CeilToInt(duration / markerInterval);
        
        GUIStyle timeStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };
        
        // Draw time labels
        for (int i = 0; i <= markerCount; i++)
        {
            float time = i * markerInterval;
            if (time > duration) break;
            
            float normalizedPos = time / duration;
            float xPos = rect.x + rect.width * normalizedPos;
            
            // Format time as minutes:seconds
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            string timeLabel = $"{minutes}:{seconds:D2}";
            
            GUI.Label(new Rect(xPos + 2, rect.y, 50, rect.height), timeLabel, timeStyle);
        }
    }

    private void DrawTimelineGrid(Rect rect, float duration)
    {
        float markerInterval = GetMarkerInterval();
        int markerCount = Mathf.CeilToInt(duration / markerInterval);
        
        // Draw vertical grid lines
        for (int i = 0; i <= markerCount; i++)
        {
            float time = i * markerInterval;
            if (time > duration) break;
            
            float normalizedPos = time / duration;
            float xPos = rect.x + rect.width * normalizedPos;
            
            // Major lines (every 10 seconds) are brighter
            bool isMajor = Mathf.Abs(time % 10f) < 0.01f;
            Color lineColor = isMajor ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.38f, 0.38f, 0.38f);
            
            Handles.color = lineColor;
            Handles.DrawLine(
                new Vector3(xPos, rect.y),
                new Vector3(xPos, rect.yMax)
            );
        }
    }

    private float GetMarkerInterval()
    {
        if (_zoom > 3.5f) return 1f;
        if (_zoom > 2f) return 5f;
        if (_zoom < 0.75f) return 30f;
        return 10f;
    }

    private void DrawEvents(Rect rect, float duration)
    {
        var events = _targetLevel.GetEvents();
        
        for (int i = 0; i < events.Count; i++)
        {
            var evt = events[i];
            float normalizedPos = evt.triggerTime / duration;
            float xPos = rect.x + rect.width * normalizedPos;
            
            // Event marker
            float markerHalfWidth = MarkerWidth * 0.5f;
            Rect markerRect = new Rect(xPos - markerHalfWidth, rect.y + MarkerPadding, MarkerWidth, rect.height - MarkerPadding * 2);
            Color markerColor = i == _selectedEventIndex ? Color.cyan : Color.yellow;
            EditorGUI.DrawRect(markerRect, markerColor);
            
            // Event label
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = EventFontSize,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            
            string label = string.IsNullOrEmpty(evt.description) ? $"Event {i}" : evt.description;
            float labelHalfWidth = EventLabelWidth * 0.5f;
            GUI.Label(new Rect(xPos - labelHalfWidth, rect.y + 2, EventLabelWidth, EventLabelHeight), label, labelStyle);
            
            // Handle interaction
            if (markerRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(markerRect, MouseCursor.SlideArrow);
                
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    _selectedEventIndex = i;
                    Event.current.Use();
                    Repaint();
                }
                
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    int index = i;
                    menu.AddItem(new GUIContent("Delete Event"), false, () => DeleteEvent(index));
                    menu.ShowAsContext();
                    Event.current.Use();
                }
            }
            
            if (i == _selectedEventIndex && Event.current.type == EventType.MouseDrag && rect.Contains(Event.current.mousePosition))
            {
                float newNormalizedPos = Mathf.Clamp01((Event.current.mousePosition.x - rect.x) / rect.width);
                evt.triggerTime = Mathf.Clamp(newNormalizedPos * duration, 0, duration);
                EditorUtility.SetDirty(_targetLevel);
                Event.current.Use();
                Repaint();
            }
        }
        
        // Click on timeline to add event
        if (_timelineRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            bool clickedOnMarker = false;
            var events2 = _targetLevel.GetEvents();
            for (int i = 0; i < events2.Count; i++)
            {
                float normalizedPos = events2[i].triggerTime / duration;
                float xPos = rect.x + rect.width * normalizedPos;
                float markerHalfWidth = MarkerWidth * 0.5f;
                Rect markerRect = new Rect(xPos - markerHalfWidth, rect.y + MarkerPadding, MarkerWidth, rect.height - MarkerPadding * 2);
                if (markerRect.Contains(Event.current.mousePosition))
                {
                    clickedOnMarker = true;
                    break;
                }
            }
            
            if (!clickedOnMarker)
            {
                float clickNormalized = Mathf.Clamp01((Event.current.mousePosition.x - rect.x) / rect.width);
                float clickTime = Mathf.Clamp(clickNormalized * duration, 0, duration);
                AddEventAtTime(clickTime);
                Event.current.Use();
            }
        }
    }

    private void DrawEventDetails()
    {
        if (_serializedLevel == null) return;

        var events = _targetLevel.GetEvents();
        
        if (_selectedEventIndex < 0 || _selectedEventIndex >= events.Count)
        {
            return;
        }
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("Current Event", EditorStyles.boldLabel);

        var eventsProperty = _serializedLevel.FindProperty("events");
        
        if (eventsProperty == null || _selectedEventIndex >= eventsProperty.arraySize)
        {
            _selectedEventIndex = -1;
            return;
        }

        var eventProperty = eventsProperty.GetArrayElementAtIndex(_selectedEventIndex);

        EditorGUILayout.PropertyField(eventProperty.FindPropertyRelative("triggerTime"));
        EditorGUILayout.PropertyField(eventProperty.FindPropertyRelative("description"));
        EditorGUILayout.PropertyField(eventProperty.FindPropertyRelative("eventType"));
        
        var eventTypeProperty = eventProperty.FindPropertyRelative("eventType");
        LevelEvent.EventType eventType = (LevelEvent.EventType)eventTypeProperty.enumValueIndex;
        
        switch (eventType)
        {
            case LevelEvent.EventType.SpawnEnemyWave:
                EditorGUILayout.PropertyField(eventProperty.FindPropertyRelative("enemyCount"), new GUIContent("Enemy Count"));
                break;
            
            case LevelEvent.EventType.SpawnStructure:
                EditorGUILayout.PropertyField(eventProperty.FindPropertyRelative("structurePrefab"), new GUIContent("Structure Prefab"));
                EditorGUILayout.PropertyField(eventProperty.FindPropertyRelative("spawnPosition"), new GUIContent("Spawn Position"));
                break;
            
            case LevelEvent.EventType.Custom:
                EditorGUILayout.PropertyField(eventProperty.FindPropertyRelative("onTrigger"), new GUIContent("On Trigger"));
                break;
        }
    }

    private void AddEventAtTime(float time)
    {
        Undo.RecordObject(_targetLevel, "Add Timeline Event");
    
        var newEvent = new LevelEvent
        {
            triggerTime = Mathf.Clamp(time, 0, _targetLevel.Duration),
            description = "New Event",
            eventType = LevelEvent.EventType.Custom,
            enemyCount = 5,
            onTrigger = new UnityEvent()
        };
    
        _targetLevel.AddEvent(newEvent);
        _selectedEventIndex = _targetLevel.GetEvents().Count - 1;
        EditorUtility.SetDirty(_targetLevel);
        Repaint();
    }

    private void DeleteEvent(int index)
    {
        if (index < 0 || index >= _targetLevel.GetEvents().Count) return;
        
        Undo.RecordObject(_targetLevel, "Delete Timeline Event");
        
        var eventsProperty = _serializedLevel.FindProperty("events");
        eventsProperty.DeleteArrayElementAtIndex(index);
        _serializedLevel.ApplyModifiedProperties();
        
        if (_selectedEventIndex == index)
        {
            _selectedEventIndex = -1;
        }
        else if (_selectedEventIndex > index)
        {
            _selectedEventIndex--;
        }
        
        EditorUtility.SetDirty(_targetLevel);
        Repaint();
        
        if (_selectedEventIndex == -1 && _targetLevel.GetEvents().Count > 0)
        {
            _selectedEventIndex = Mathf.Clamp(index - 1, 0, _targetLevel.GetEvents().Count - 1);
        }
    }
}