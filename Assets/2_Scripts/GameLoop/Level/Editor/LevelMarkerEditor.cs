using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

internal static class MarkerOverlayGUI
{
    public static readonly Color EnemyColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    public static readonly Color PlayerColor = new Color(1f, 0.55f, 0.15f, 0.9f);
    public static readonly Color ObjectiveColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    public static readonly Color ShipColor = new Color(0.6f, 0.4f, 1f, 0.9f);
    public static readonly Color CinematicColor = new Color(0.95f, 0.15f, 0.55f, 0.9f);
    public static readonly Color PresentationColor = new Color(0.2f, 0.6f, 1f, 0.9f);
    public static readonly Color StructureColor = new Color(0.3f, 1f, 1f, 0.9f);

    public static void DrawCenterCircle(MarkerOverlayRegion region, Color color, float radius = 4f, float yOffset = -3f)
    {
        Rect markerRect = region.markerRegion;
        Vector3 center = new Vector3(markerRect.center.x, markerRect.center.y + yOffset, 0f);

        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawSolidDisc(center, Vector3.forward, radius);
        Handles.EndGUI();
    }

    public static void DrawTopBar(MarkerOverlayRegion region, Color color)
    {
        Rect markerRect = region.markerRegion;
        Rect topBar = new Rect(markerRect.x - 2, markerRect.y, markerRect.width + 4, 3);
        EditorGUI.DrawRect(topBar, color);
    }

    public static void DrawBottomBar(MarkerOverlayRegion region, Color color)
    {
        Rect markerRect = region.markerRegion;
        Rect bottomBar = new Rect(markerRect.x - 2, markerRect.yMax - 3, markerRect.width + 4, 3);
        EditorGUI.DrawRect(bottomBar, color);
    }

    public static void DrawLeftBar(MarkerOverlayRegion region, Color color)
    {
        Rect markerRect = region.markerRegion;
        Rect leftBar = new Rect(markerRect.x - 2, markerRect.y, 3, markerRect.height);
        EditorGUI.DrawRect(leftBar, color);
    }

    public static void DrawRightBar(MarkerOverlayRegion region, Color color)
    {
        Rect markerRect = region.markerRegion;
        Rect rightBar = new Rect(markerRect.xMax - 1, markerRect.y, 3, markerRect.height);
        EditorGUI.DrawRect(rightBar, color);
    }
}

[CustomTimelineEditor(typeof(SpawnEnemyWaveMarker))]
public class SpawnEnemyWaveMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        MarkerOverlayGUI.DrawTopBar(region, MarkerOverlayGUI.EnemyColor);
        MarkerOverlayGUI.DrawCenterCircle(region, MarkerOverlayGUI.EnemyColor);
    }

    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        return new MarkerDrawOptions { tooltip = "Spawn Enemies" };
    }
}

[CustomTimelineEditor(typeof(SpawnStructureMarker))]
public class SpawnStructureMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        MarkerOverlayGUI.DrawCenterCircle(region, MarkerOverlayGUI.StructureColor);
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
        bool anyEnabled = toggle && toggle.SpawnPoints.Exists(entry => entry.spawnPointState);
        Color color = anyEnabled ? MarkerOverlayGUI.EnemyColor : new Color(MarkerOverlayGUI.EnemyColor.r, MarkerOverlayGUI.EnemyColor.g, MarkerOverlayGUI.EnemyColor.b, 0.2f);

        MarkerOverlayGUI.DrawBottomBar(region, color);
        MarkerOverlayGUI.DrawCenterCircle(region, color);
    }

    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var toggle = marker as ToggleEnemySpawnPointMarker;
        int enableCount = 0;
        int disableCount = 0;

        if (toggle != null)
        {
            foreach (var entry in toggle.SpawnPoints)
            {
                if (entry.spawnPointState) enableCount++;
                else disableCount++;
            }
        }

        return new MarkerDrawOptions
        {
            tooltip = $"Enable {enableCount} / Disable {disableCount} Spawn Point(s)"
        };
    }
}

[CustomTimelineEditor(typeof(StartObjectivesMarker))]
internal class StartObjectivesMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        MarkerOverlayGUI.DrawCenterCircle(region, MarkerOverlayGUI.ObjectiveColor);
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

[CustomTimelineEditor(typeof(SetActivePlayerSpawnPointMarker))]
public class SetActivePlayerSpawnPointMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        MarkerOverlayGUI.DrawBottomBar(region, MarkerOverlayGUI.PlayerColor);
        MarkerOverlayGUI.DrawCenterCircle(region, MarkerOverlayGUI.PlayerColor);
    }

    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        var spawnPointMarker = marker as SetActivePlayerSpawnPointMarker;
        string tooltip = "Set Active Spawn Point";

        if (spawnPointMarker != null && spawnPointMarker.Teleport)
            tooltip += "\nTeleport Player";

        return new MarkerDrawOptions { tooltip = tooltip };
    }
}

[CustomTimelineEditor(typeof(SetShipSplineMarker))]
public class SetShipSplineMarkerEditor : MarkerEditor
{
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        MarkerOverlayGUI.DrawCenterCircle(region, MarkerOverlayGUI.ShipColor);
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
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        MarkerOverlayGUI.DrawCenterCircle(region, MarkerOverlayGUI.CinematicColor);
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
    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        MarkerOverlayGUI.DrawCenterCircle(region, MarkerOverlayGUI.PresentationColor);
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
    private static readonly Color EnableColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);
    private static readonly Color DisableColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);

    public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
    {
        var featuresMarker = marker as SetPlayerFeaturesMarker;

        if (featuresMarker != null && featuresMarker.FeaturesToEnable != 0)
            MarkerOverlayGUI.DrawRightBar(region, EnableColor);

        if (featuresMarker != null && featuresMarker.FeaturesToDisable != 0)
            MarkerOverlayGUI.DrawLeftBar(region, DisableColor);

        MarkerOverlayGUI.DrawCenterCircle(region, MarkerOverlayGUI.PlayerColor);
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
