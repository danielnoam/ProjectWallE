using System;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class TeleportPlayerToSpawnPointMarker : BaseLevelEventMarker
{
    [Header("Spawn Point")]
    [SerializeField] private bool usePod;
    [SerializeField] private ExposedReference<PlayerSpawnPoint> playerSpawnPoint;


    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var spawnPoint = playerSpawnPoint.Resolve(resolver);
        if (!spawnPoint) return;

        var player = LevelManager.Instance?.Player;
        if (!player) return;

        if (usePod) player.CallPodTo(spawnPoint);
        else player.Teleport(spawnPoint);
    }
}