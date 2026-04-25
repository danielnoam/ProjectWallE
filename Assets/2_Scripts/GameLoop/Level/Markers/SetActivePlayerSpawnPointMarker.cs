using System;
using ProjectWallE.GameLoop;
using UnityEngine;


[Serializable]
public class SetActivePlayerSpawnPointMarker : BaseLevelEventMarker
{
    public ExposedReference<PlayerSpawnPoint> playerSpawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        playerSpawnPoint.Resolve(resolver)?.SetAsActiveSpawnPoint();
    }
}