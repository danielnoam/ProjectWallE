using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

[CustomTimelineEditor(typeof(SpawnEnemyWaveMarker))]
public class SpawnEnemyWaveMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Rect markerRect = region.markerRegion;
        
        // Double red bars at top
        Rect topBar1 = new Rect(markerRect.x - 2, markerRect.y, markerRect.width + 4, 2);
        Rect topBar2 = new Rect(markerRect.x - 2, markerRect.y + 3, markerRect.width + 4, 2);
        
        EditorGUI.DrawRect(topBar1, new Color(1f, 0.3f, 0.3f, 0.9f));
        EditorGUI.DrawRect(topBar2, new Color(1f, 0.3f, 0.3f, 0.9f));
    }
    
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        return new MarkerDrawOptions
        {
            tooltip = $"Spawn {((SpawnEnemyWaveMarker)marker).enemyCount} enemies",
        };
    }
}

[CustomTimelineEditor(typeof(SpawnStructureMarker))]
public class SpawnStructureMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Rect markerRect = region.markerRegion;
        
        // Thick cyan bar at bottom
        Rect bottomBar = new Rect(markerRect.x - 2, markerRect.yMax - 5, markerRect.width + 4, 5);
        EditorGUI.DrawRect(bottomBar, new Color(0.3f, 1f, 1f, 0.9f));
    }
    
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var structure = ((SpawnStructureMarker)marker).structureToSpawn;
        return new MarkerDrawOptions
        {
            tooltip = $"Spawn {(!structure ? "Structure" : structure.StructureUIData.Label)}",
        };
    }
}

[CustomTimelineEditor(typeof(ToggleEnemySpawnPointMarker))]
public class ToggleSpawnPointMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        var toggle = marker as ToggleEnemySpawnPointMarker;
        Color color = toggle && toggle.spawnPointState ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(1f, 0.3f, 0.3f, 0.2f);
        
        Rect markerRect = region.markerRegion;
        
        float centerX = markerRect.x - 4;
        float centerY = markerRect.y + 5;
        
        // Approximate circle with overlapping rects
        EditorGUI.DrawRect(new Rect(centerX, centerY, 6, 6), color);
        EditorGUI.DrawRect(new Rect(centerX + 1, centerY - 1, 4, 8), color);
        EditorGUI.DrawRect(new Rect(centerX - 1, centerY + 1, 8, 4), color);
    }
    
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var toggle = marker as ToggleEnemySpawnPointMarker;
        return new MarkerDrawOptions
        {
            tooltip = toggle && toggle.spawnPointState ? "Enable Spawn Point" : "Disable Spawn Point"
        };
    }
}