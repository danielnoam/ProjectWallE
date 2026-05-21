using System;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;


[Serializable]
public class SpawnStructureMarker : BaseLevelEventMarker
{
    [Header("Structure")]
    [Tooltip("When spawning the structure, should the camera switch to a pod camera focused on the structure")]
    [SerializeField] private PodCameraMode podCameraMode = PodCameraMode.None;
    [PrefabSelector("Assets/5_Prefabs/Structures"), SerializeField] private Structure structureToSpawn;
    [SerializeField, ScenePicker] private ExposedReference<StructureSpawnPoint> structureSpawnPoint;
    
    public Structure StructureToSpawn => structureToSpawn;
    
    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        structureSpawnPoint.Resolve(resolver)?.SpawnStructure(structureToSpawn, podCameraMode);
    }
}