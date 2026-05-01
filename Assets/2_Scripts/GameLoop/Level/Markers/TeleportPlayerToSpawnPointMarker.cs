using System;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class TeleportPlayerToSpawnPointMarker : BaseLevelEventMarker
{
    public bool usePod;
    public ExposedReference<PlayerSpawnPoint> playerSpawnPoint;


    public override void Execute(IExposedPropertyTable resolver = null)
    {
        var spawnPoint = playerSpawnPoint.Resolve(resolver);
        if (!spawnPoint) return;

        var player = LevelManager.Instance?.Player;
        if (!player) return;

        if (usePod) player.CallPodTo(spawnPoint);
        else player.Teleport(spawnPoint);
    }
}