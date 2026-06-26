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
        
        Rect topBar1 = new Rect(markerRect.x - 2, markerRect.y, markerRect.width + 4, 2);
        Rect topBar2 = new Rect(markerRect.x - 2, markerRect.y + 3, markerRect.width + 4, 2);
        
        EditorGUI.DrawRect(topBar1, new Color(1f, 0.3f, 0.3f, 0.9f));
        EditorGUI.DrawRect(topBar2, new Color(1f, 0.3f, 0.3f, 0.9f));
    }
    
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        return new MarkerDrawOptions
        {
            tooltip = $"Spawn enemies",
        };
    }
}

[CustomTimelineEditor(typeof(SpawnStructureMarker))]
public class SpawnStructureMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Rect markerRect = region.markerRegion;
        
        Rect bottomBar = new Rect(markerRect.x - 2, markerRect.yMax - 5, markerRect.width + 4, 5);
        EditorGUI.DrawRect(bottomBar, new Color(0.3f, 1f, 1f, 0.9f));
    }
    
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var structure = ((SpawnStructureMarker)marker).StructureToSpawn;
        return new MarkerDrawOptions
        {
            tooltip = $"Spawn {(!structure ? "Structure" : structure.StructureUIData.Label)}",
        };
    }
}

[CustomTimelineEditor(typeof(ToggleEnemySpawnPointMarker))]
public class ToggleEnemySpawnPointMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        var toggle = marker as ToggleEnemySpawnPointMarker;
        Color color = toggle && toggle.SpawnPointState ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(1f, 0.3f, 0.3f, 0.2f);
        
        Rect markerRect = region.markerRegion;
        
        float centerX = markerRect.x - 4;
        float centerY = markerRect.y + 5;
        
        EditorGUI.DrawRect(new Rect(centerX, centerY, 6, 6), color);
        EditorGUI.DrawRect(new Rect(centerX + 1, centerY - 1, 4, 8), color);
        EditorGUI.DrawRect(new Rect(centerX - 1, centerY + 1, 8, 4), color);
    }
    
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var toggle = marker as ToggleEnemySpawnPointMarker;
        return new MarkerDrawOptions
        {
            tooltip = toggle && toggle.SpawnPointState ? "Enable Spawn Point" : "Disable Spawn Point"
        };
    }
}

[CustomTimelineEditor(typeof(StartObjectivesMarker))]
internal class StartObjectivesMarkerEditor : MarkerEditor
{
    private static readonly Color ObjectiveColor = new Color(1f, 0.85f, 0.2f, 0.9f);

    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Rect markerRect = region.markerRegion;

        Rect topBar = new Rect(markerRect.x - 2, markerRect.y, markerRect.width + 4, 3);
        Rect bottomBar = new Rect(markerRect.x - 2, markerRect.yMax - 3, markerRect.width + 4, 3);

        EditorGUI.DrawRect(topBar, ObjectiveColor);
        EditorGUI.DrawRect(bottomBar, ObjectiveColor);
    }

    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var objective = marker as StartObjectivesMarker;
        int count = objective?.Objectives?.Count ?? 0;

        return new MarkerDrawOptions
        {
            tooltip = $"Objectives ({count})"
        };
    }
}

[CustomTimelineEditor(typeof(TeleportPlayerToSpawnPointMarker))]
public class TeleportPlayerToSpawnPointMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Rect markerRect = region.markerRegion;
        
        Rect bottomBar = new Rect(markerRect.x - 2, markerRect.yMax - 5, markerRect.width + 4, 5);
        EditorGUI.DrawRect(bottomBar, new Color(0.3f, 1f, 0.3f, 0.9f));
    }
    
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        return new MarkerDrawOptions
        {
            tooltip = "Teleport Player"
        };
    }
}

[CustomTimelineEditor(typeof(SetActivePlayerSpawnPointMarker))]
public class SetActivePlayerSpawnPointMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Color color = new Color(0.3f, 1f, 0.3f, 0.9f);
        
        Rect markerRect = region.markerRegion;
        
        float centerX = markerRect.x - 4;
        float centerY = markerRect.y + 5;
        
        EditorGUI.DrawRect(new Rect(centerX, centerY, 6, 6), color);
        EditorGUI.DrawRect(new Rect(centerX + 1, centerY - 1, 4, 8), color);
        EditorGUI.DrawRect(new Rect(centerX - 1, centerY + 1, 8, 4), color);
    }
    
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        return new MarkerDrawOptions
        {
            tooltip = "Set Active Spawn Point"
        };
    }
}

[CustomTimelineEditor(typeof(SetShipSplineMarker))]
public class SetShipSplineMarkerEditor : MarkerEditor
{
    private static readonly Color ShipColor = new Color(0.6f, 0.4f, 1f, 0.9f);

    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Rect markerRect = region.markerRegion;

        Rect topBar = new Rect(markerRect.x - 2, markerRect.y, markerRect.width + 4, 3);
        EditorGUI.DrawRect(topBar, ShipColor);

        var ship = marker as SetShipSplineMarker;
        if (ship != null && ship.Transition == SetShipSplineMarker.TransitionMode.Teleport)
        {
            Rect bottomBar = new Rect(markerRect.x - 2, markerRect.yMax - 3, markerRect.width + 4, 3);
            EditorGUI.DrawRect(bottomBar, ShipColor);
        }
    }

    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var ship = marker as SetShipSplineMarker;
        if (ship == null) return new MarkerDrawOptions { tooltip = "Set Ship Spline" };

        string transition = ship.Transition == SetShipSplineMarker.TransitionMode.Teleport ? "Teleport" : "Over Time";

        return new MarkerDrawOptions
        {
            tooltip = $"Set Ship Spline\n{ship.FollowMode} · {transition}"
        };
    }
}

[CustomTimelineEditor(typeof(PlayCinematicMarker))]
public class PlayCinematicMarkerEditor : MarkerEditor
{
    private static readonly Color CinematicColor = new Color(0.95f, 0.15f, 0.55f, 0.9f);

    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Rect markerRect = region.markerRegion;

        Rect topBar = new Rect(markerRect.x - 2, markerRect.y, markerRect.width + 4, 3);
        Rect bottomBar = new Rect(markerRect.x - 2, markerRect.yMax - 3, markerRect.width + 4, 3);

        EditorGUI.DrawRect(topBar, CinematicColor);
        EditorGUI.DrawRect(bottomBar, CinematicColor);
    }

    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var cinematic = marker as PlayCinematicMarker;
        string tooltip = "Play Cinematic";

        if (cinematic != null && cinematic.PauseLevelTimeline)
            tooltip += "\nPauses Timeline";

        return new MarkerDrawOptions { tooltip = tooltip };
    }
}

[CustomTimelineEditor(typeof(PresentationMarker))]
public class PresentationMarkerEditor : MarkerEditor
{
    private static readonly Color PresentationColor = new Color(0.5f, 0.5f, 0.65f, 0.9f);

    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        Rect markerRect = region.markerRegion;

        Rect leftBar = new Rect(markerRect.x - 2, markerRect.y, 3, markerRect.height);
        Rect rightBar = new Rect(markerRect.xMax - 1, markerRect.y, 3, markerRect.height);

        EditorGUI.DrawRect(leftBar, PresentationColor);
        EditorGUI.DrawRect(rightBar, PresentationColor);
    }

    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var presentation = marker as PresentationMarker;
        return new MarkerDrawOptions
        {
            tooltip = presentation != null ? $"Presentation\n{presentation.UI}" : "Presentation"
        };
    }
}

[CustomTimelineEditor(typeof(SetPlayerFeaturesMarker))]
public class SetPlayerFeaturesMarkerEditor : MarkerEditor
{
    private static readonly Color EnableColor  = new Color(0.4f, 0.7f, 1f, 0.9f);
    private static readonly Color DisableColor = new Color(1f, 0.5f, 0.2f, 0.9f);

    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        var featuresMarker = marker as SetPlayerFeaturesMarker;
        Rect markerRect = region.markerRegion;

        if (featuresMarker != null && featuresMarker.FeaturesToEnable != 0)
        {
            Rect leftBar = new Rect(markerRect.x - 2, markerRect.y, 3, markerRect.height);
            EditorGUI.DrawRect(leftBar, EnableColor);
        }

        if (featuresMarker != null && featuresMarker.FeaturesToDisable != 0)
        {
            Rect rightBar = new Rect(markerRect.xMax - 1, markerRect.y, 3, markerRect.height);
            EditorGUI.DrawRect(rightBar, DisableColor);
        }

        var color = new Color(0.3f, 1f, 0.3f, 0.9f);
        float cx = markerRect.x + markerRect.width * 0.5f - 3f;
        float cy = markerRect.y + markerRect.height * 0.5f - 3f;
        EditorGUI.DrawRect(new Rect(cx + 1, cy,     4, 6), color);
        EditorGUI.DrawRect(new Rect(cx,     cy + 1, 6, 4), color);
    }

    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var featuresMarker = marker as SetPlayerFeaturesMarker;
        if (featuresMarker == null) return new MarkerDrawOptions { tooltip = "Set Player Features" };

        var tooltip = "Player Features";

        if (featuresMarker.FeaturesToEnable != 0)
            tooltip += $"\n+ {featuresMarker.FeaturesToEnable}";

        if (featuresMarker.FeaturesToDisable != 0)
            tooltip += $"\n- {featuresMarker.FeaturesToDisable}";

        return new MarkerDrawOptions { tooltip = tooltip };
    }
}