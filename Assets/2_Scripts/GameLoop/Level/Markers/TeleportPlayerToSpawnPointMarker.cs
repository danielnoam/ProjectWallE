using System;
using ProjectWallE.GameLoop;
using UnityEngine;


[Serializable]
public class TeleportPlayerToSpawnPointMarker : BaseLevelEventMarker
{
    public ExposedReference<PlayerSpawnPoint> playerSpawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        var spawnPoint = playerSpawnPoint.Resolve(resolver);
        LevelManager.Instance?.Player?.Teleport(spawnPoint);
    }
}