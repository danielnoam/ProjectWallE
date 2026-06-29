using System;
using DNExtensions.Utilities;
using ProjectWallE;
using UnityEngine;
using UnityEngine.Playables;


[Serializable]
public class LookAtTargetMarker : BaseLevelEventMarker
{
    [Header("Look At")]
    [SerializeField, ScenePicker] private ExposedReference<Transform> target;
    [SerializeField, Min(0f)] private float duration = 3f;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var resolvedTarget = target.Resolve(resolver);
        if (!resolvedTarget) return;

        CameraManager.Instance?.LookAtTargetForDuration(resolvedTarget, duration);
    }
}
