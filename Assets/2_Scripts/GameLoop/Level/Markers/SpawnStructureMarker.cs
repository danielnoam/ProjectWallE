using System;
using DNExtensions.Utilities;
using UnityEngine;


[Serializable]
public class SpawnStructureMarker : BaseLevelEventMarker
{
    [Header("Structure")]
    [PrefabSelector("Assets/Prefabs/Structures")] public Structure structureToSpawn;
    public ExposedReference<StructureSpawnPoint> structureSpawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        structureSpawnPoint.Resolve(resolver)?.SpawnStructure(structureToSpawn);
    }
}