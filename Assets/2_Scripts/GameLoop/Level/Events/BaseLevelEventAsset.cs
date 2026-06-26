using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public abstract class BaseLevelEventAsset : PlayableAsset
{
#if UNITY_EDITOR
    [Header("Editor")]
    [SerializeField, Tooltip("If set to true, this event will not execute when playing in the editor")] private bool skipInEditor;
#endif

    public bool SkipInEditor
    {
        get
        {
#if UNITY_EDITOR
            return skipInEditor;
#else
            return false;
#endif
        }
    }

    public void Execute(IExposedPropertyTable resolver = null)
    {
#if UNITY_EDITOR
        if (skipInEditor && Application.isEditor) return;
#endif
        OnExecute(resolver);
    }

    protected virtual void OnExecute(IExposedPropertyTable resolver = null) { }
}