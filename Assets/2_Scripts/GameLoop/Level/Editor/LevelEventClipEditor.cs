using System.Linq;
using ProjectWallE.GameLoop;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

[CustomTimelineEditor(typeof(SpawnEnemyEvent))]
public class SpawnEnemyEventClipEditor : ClipEditor
{
    public override ClipDrawOptions GetClipOptions(TimelineClip clip)
    {
        var options = base.GetClipOptions(clip);
        options.highlightColor = new Color(1f, 0.3f, 0.3f, 0.9f);
        options.tooltip = "Spawn Enemies";
        return options;
    }
}

[CustomTimelineEditor(typeof(StartObjectivesEvent))]
public class StartObjectivesEventClipEditor : ClipEditor
{
    public override ClipDrawOptions GetClipOptions(TimelineClip clip)
    {
        var options = base.GetClipOptions(clip);
        options.highlightColor = new Color(1f, 0.85f, 0.2f, 0.9f);

        var startObjectives = clip.asset as StartObjectivesEvent;
        int count = startObjectives != null && startObjectives.Objectives != null ? startObjectives.Objectives.Count : 0;
        options.tooltip = $"Objectives ({count})";

        return options;
    }

    public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
    {
        base.OnCreate(clip, track, clonedFrom);
        UpdateDisplayName(clip);
    }

    public override void OnClipChanged(TimelineClip clip)
    {
        base.OnClipChanged(clip);
        UpdateDisplayName(clip);
    }

    private static void UpdateDisplayName(TimelineClip clip)
    {
        var startObjectives = clip.asset as StartObjectivesEvent;
        if (startObjectives == null) return;

        var objectives = startObjectives.Objectives;
        string names = objectives != null && objectives.Count > 0
            ? string.Join(", ", objectives.Where(o => o != null).Select(o => o.Description))
            : "None";

        clip.displayName = $"Start Objectives: {names}";
    }
}
