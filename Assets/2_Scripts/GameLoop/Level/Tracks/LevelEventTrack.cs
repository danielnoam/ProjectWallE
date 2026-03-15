using UnityEngine;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor.Timeline;
#endif

[TrackColor(0.3f, 0.5f, 0.8f)]
[TrackClipType(typeof(BaseLevelEventAsset))]
public class LevelEventTrack : TrackAsset
{

}