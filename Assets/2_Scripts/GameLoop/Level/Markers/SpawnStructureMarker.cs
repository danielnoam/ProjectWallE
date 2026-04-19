using System;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;


[Serializable]
public class SpawnStructureMarker : BaseLevelEventMarker
{
    [Header("Structure")]
    [Tooltip("When spawning the structure, should the camera switch to a pod camera focused on the structure")]
    public bool enablePodCamera;
    [PrefabSelector("Assets/5_Prefabs/Structures")] public Structure structureToSpawn;
    public ExposedReference<StructureSpawnPoint> structureSpawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        structureSpawnPoint.Resolve(resolver)?.SpawnStructure(structureToSpawn, enablePodCamera);
    }
}