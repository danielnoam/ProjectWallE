using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class LevelEvent
{
    public enum EventType
    {
        SpawnEnemyWave,
        SpawnStructure,
        Custom,
    }

    public float triggerTime;
    public string description;
    public EventType eventType;
    public int enemyCount = 5;
    public Structure structurePrefab;
    public Vector3 spawnPosition;
    public UnityEvent onTrigger;
    
    [HideInInspector] public bool hasTriggered;

    public void Execute()
    {
        switch (eventType)
        {
            case EventType.SpawnEnemyWave:
                EnemyManager.Instance.SpawnEnemyWave(enemyCount);
                break;
            
            case EventType.SpawnStructure:
                if (structurePrefab)
                {
                    StructureDispatcher.Instance.DeployPod(
                        structurePrefab,
                        spawnPosition,
                        Vector3.up
                    );
                }
                break;
            
            case EventType.Custom:
                onTrigger?.Invoke();
                break;
        }
    }
}