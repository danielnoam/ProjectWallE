using System;
using DNExtensions.Utilities;
using ProjectWallE;
using ProjectWallE.GameLoop;
using UnityEngine;


[Serializable]
public class SetActivePlayerSpawnPointMarker : BaseLevelEventMarker
{
    [Header("Spawn Point")]
    [SerializeField, ScenePicker] private ExposedReference<PlayerSpawnPoint> playerSpawnPoint;

    [Header("Actions")]
    [SerializeField] private bool setActive = true;
    [SerializeField] private bool teleport;
    [SerializeField, ShowIf("teleport")] private bool usePod;

    public bool Teleport => teleport;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var spawnPoint = playerSpawnPoint.Resolve(resolver);
        if (!spawnPoint) return;

        if (setActive) spawnPoint.SetAsActiveSpawnPoint();

        if (teleport)
        {
            var player = PlayerManager.Instance;
            if (!player) return;

            if (usePod) player.CallPodTo(spawnPoint);
            else player.Teleport(spawnPoint);
        }
    }
}