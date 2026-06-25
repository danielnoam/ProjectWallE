using System;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class ToggleUIMarker : BaseLevelEventMarker
{
    [Header("UI")]
    [SerializeField] private bool uiVisible = true;

    public bool UIVisible => uiVisible;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        UIManager.Instance?.SetHudVisible(uiVisible);
    }
}
