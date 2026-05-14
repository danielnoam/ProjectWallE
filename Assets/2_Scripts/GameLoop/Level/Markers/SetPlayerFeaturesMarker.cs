using System;
using ProjectWallE;
using UnityEngine;

[Serializable]
public class SetPlayerFeaturesMarker : BaseLevelEventMarker
{
    [Header("Features")]
    [SerializeField] private PlayerFeature featuresToEnable;
    [SerializeField] private PlayerFeature featuresToDisable;

    public PlayerFeature FeaturesToEnable => featuresToEnable;
    public PlayerFeature FeaturesToDisable => featuresToDisable;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        PlayerManager.Instance?.EnableFeatures(featuresToEnable);
        PlayerManager.Instance?.DisableFeatures(featuresToDisable);
    }
}