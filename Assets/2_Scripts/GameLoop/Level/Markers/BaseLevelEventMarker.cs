using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public abstract class BaseLevelEventMarker : Marker, INotification
{
    public PropertyName id => new PropertyName(GetType().Name);
    public abstract void Execute(IExposedPropertyTable resolver = null);
}