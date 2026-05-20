using System;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class SpawnEnemyWaveMarker : BaseLevelEventMarker
{
    [Header("Spawn")]
    [SerializeField] private EnemySpawnParams spawnParams;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        spawnParams.Initialize(resolver);
        spawnParams.Spawn();
    }
}