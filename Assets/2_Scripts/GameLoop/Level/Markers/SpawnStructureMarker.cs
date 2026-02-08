using System;
using DNExtensions.Utilities.PrefabSelector;
using UnityEngine;


[Serializable]
public class SpawnStructureMarker : BaseLevelEventMarker
{
    [PrefabSelector("Assets/Prefabs/Structures")] public Structure structureToSpawn;
    public ExposedReference<StructureSpawnPoint> structureSpawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver)
    {
        structureSpawnPoint.Resolve(resolver)?.SpawnStructure(structureToSpawn);
    }
}