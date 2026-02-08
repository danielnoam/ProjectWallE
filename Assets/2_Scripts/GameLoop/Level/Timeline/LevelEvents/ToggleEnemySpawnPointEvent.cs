using System;
using UnityEngine;

[Serializable]
public class ToggleEnemySpawnPointEvent : BaseLevelEventAsset
{
    public ExposedReference<EnemySpawnPoint> enemySpawnPoint;
    public bool state = true;
    
    public override void Execute(IExposedPropertyTable resolver)
    {
        enemySpawnPoint.Resolve(resolver)?.SetActiveState(state);
    }
}