using System;
using ProjectWallE.GameLoop;
using UnityEngine;


[Serializable]
public class SetActivePlayerSpawnPointMarker : BaseLevelEventMarker
{
    [Header("Spawn Point")]
    [SerializeField] private ExposedReference<PlayerSpawnPoint> playerSpawnPoint;
    
    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        playerSpawnPoint.Resolve(resolver)?.SetAsActiveSpawnPoint();
    }
}