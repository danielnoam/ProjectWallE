using System;
using ProjectWallE;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class TeleportPlayerToSpawnPointMarker : BaseLevelEventMarker
{
    [Header("Spawn Point")]
    [SerializeField] private bool usePod;
    [SerializeField, ScenePicker] private ExposedReference<PlayerSpawnPoint> playerSpawnPoint;


    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var spawnPoint = playerSpawnPoint.Resolve(resolver);
        if (!spawnPoint) return;

        var player = PlayerManager.Instance;
        if (!player) return;

        if (usePod) player.CallPodTo(spawnPoint);
        else player.Teleport(spawnPoint);
    }
}