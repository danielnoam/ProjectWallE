using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public abstract class BaseLevelEventAsset : PlayableAsset
{
    public virtual void Execute(IExposedPropertyTable resolver = null) { }
}