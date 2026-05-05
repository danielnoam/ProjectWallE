using System;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class ToggleEnemySpawnPointMarker : BaseLevelEventMarker
{
    [Header("Spawn Point")]
    [SerializeField] private ExposedReference<EnemySpawnPoint> enemySpawnPoint;
    [SerializeField] private bool spawnPointState = true;
    
    public bool SpawnPointState => spawnPointState;
    
    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var point = enemySpawnPoint.Resolve(resolver);
        point?.SetActiveState(spawnPointState);
    }
}