using System;
using UnityEngine;

public class StructureSpawnPoint : MonoBehaviour
{
    [Header("SpawnPoint Settings")]
    [SerializeField] private bool spawnAtStart = true;
    [SerializeField] private bool spawnOnlyOnce = true;
    [SerializeField] private Structure structurePrefab;
    
    private bool _hasSpawned;
    
    public Structure StructurePrefab => structurePrefab;


    private void Start()
    {
        LevelManager.OnLevelInitializing += OnLevelInitializing;
    }

    private void OnDestroy()
    {
        LevelManager.OnLevelStarted -= OnLevelInitializing;
    }

    private void OnLevelInitializing()
    {
        if (spawnAtStart)
        {
            SpawnStructure();
        }
    }

    public void SpawnStructure()
    {
        if (!structurePrefab || (spawnOnlyOnce && _hasSpawned)) return;
        
        _hasSpawned = true;
        StructureDispatcher.Instance?.DeployPod(structurePrefab, transform.position, transform.up);
    }
    
    

    private void OnDrawGizmos()
    {
        if (spawnAtStart)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 2);

        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 2);
        }

        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (2 + 0.5f),
            structurePrefab ? $"Spawn Point: {structurePrefab.Label} \nHas Spawned: {_hasSpawned}" :  $"Spawn Point: No Structure Assigned",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.cyan },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            }
        );

#endif
    }
}