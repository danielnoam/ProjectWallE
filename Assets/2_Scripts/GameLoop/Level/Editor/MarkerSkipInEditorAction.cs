using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline.Actions;
using UnityEngine.Timeline;

[MenuEntry("Skip In Editor/Enable")]
public class EnableSkipInEditorMarkerAction : MarkerAction
{
    public override ActionValidity Validate(IEnumerable<IMarker> markers)
    {
        return markers.Any(m => m is BaseLevelEventMarker) ? ActionValidity.Valid : ActionValidity.NotApplicable;
    }

    public override bool Execute(IEnumerable<IMarker> markers)
    {
        return MarkerSkipInEditorAction.SetSkip(markers, true);
    }
}

[MenuEntry("Skip In Editor/Disable")]
public class DisableSkipInEditorMarkerAction : MarkerAction
{
    public override ActionValidity Validate(IEnumerable<IMarker> markers)
    {
        return markers.Any(m => m is BaseLevelEventMarker) ? ActionValidity.Valid : ActionValidity.NotApplicable;
    }

    public override bool Execute(IEnumerable<IMarker> markers)
    {
        return MarkerSkipInEditorAction.SetSkip(markers, false);
    }
}

internal static class MarkerSkipInEditorAction
{
    public static bool SetSkip(IEnumerable<IMarker> markers, bool value)
    {
        bool any = false;

        foreach (var m in markers)
        {
            if (!(m is BaseLevelEventMarker marker)) continue;

            var serializedMarker = new SerializedObject(marker);
            var skipInEditor = serializedMarker.FindProperty("skipInEditor");
            if (skipInEditor == null) continue;

            skipInEditor.boolValue = value;
            serializedMarker.ApplyModifiedProperties();
            any = true;
        }

        return any;
    }
}
