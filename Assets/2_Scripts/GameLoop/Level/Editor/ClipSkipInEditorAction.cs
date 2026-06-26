using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline.Actions;
using UnityEngine.Timeline;

[MenuEntry("Skip In Editor/Enable")]
public class EnableSkipInEditorClipAction : ClipAction
{
    public override ActionValidity Validate(IEnumerable<TimelineClip> clips)
    {
        return clips.Any(c => c.asset is BaseLevelEventAsset) ? ActionValidity.Valid : ActionValidity.NotApplicable;
    }

    public override bool Execute(IEnumerable<TimelineClip> clips)
    {
        return ClipSkipInEditorAction.SetSkip(clips, true);
    }
}

[MenuEntry("Skip In Editor/Disable")]
public class DisableSkipInEditorClipAction : ClipAction
{
    public override ActionValidity Validate(IEnumerable<TimelineClip> clips)
    {
        return clips.Any(c => c.asset is BaseLevelEventAsset) ? ActionValidity.Valid : ActionValidity.NotApplicable;
    }

    public override bool Execute(IEnumerable<TimelineClip> clips)
    {
        return ClipSkipInEditorAction.SetSkip(clips, false);
    }
}

internal static class ClipSkipInEditorAction
{
    public static bool SetSkip(IEnumerable<TimelineClip> clips, bool value)
    {
        bool any = false;

        foreach (var clip in clips)
        {
            if (!(clip.asset is BaseLevelEventAsset eventAsset)) continue;

            var serializedAsset = new SerializedObject(eventAsset);
            var skipInEditor = serializedAsset.FindProperty("skipInEditor");
            if (skipInEditor == null) continue;

            skipInEditor.boolValue = value;
            serializedAsset.ApplyModifiedProperties();
            any = true;
        }

        return any;
    }
}
