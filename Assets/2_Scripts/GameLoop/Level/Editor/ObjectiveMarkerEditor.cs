using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

[CustomTimelineEditor(typeof(ObjectiveMarker))]
internal class ObjectiveMarkerEditor : MarkerEditor
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
        var objective = marker as ObjectiveMarker;
        int count = objective?.objectives?.Count ?? 0;

        return new MarkerDrawOptions
        {
            tooltip = $"Objectives ({count})"
        };
    }
}