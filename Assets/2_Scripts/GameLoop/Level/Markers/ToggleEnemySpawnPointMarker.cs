using System;
using UnityEngine;


[Serializable]
public class ToggleEnemySpawnPointMarker : BaseLevelEventMarker
{
    [Header("Spawn Point")]
    public ExposedReference<EnemySpawnPoint> enemySpawnPoint;
    public bool spawnPointState = true;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        var point = enemySpawnPoint.Resolve(resolver);
        point?.SetActiveState(spawnPointState);
    }
}