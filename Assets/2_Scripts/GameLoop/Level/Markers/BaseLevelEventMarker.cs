using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public abstract class BaseLevelEventMarker : Marker, INotification
{
    
#if UNITY_EDITOR
    [Header("Editor")]
    [SerializeField, Tooltip("If set to true, the marker will not start when playing in the editor")] private bool skipInEditor;
#endif
    
    public PropertyName id => new PropertyName(GetType().Name);


    public void Execute(IExposedPropertyTable resolver = null)
    {
        #if UNITY_EDITOR
        if (skipInEditor && Application.isEditor)
        {
            Debug.Log($"Skipping {GetType().Name}");
            return;
        }
        #endif
        
        OnExecute(resolver);
    }
    
    protected abstract void OnExecute(IExposedPropertyTable resolver = null);
}