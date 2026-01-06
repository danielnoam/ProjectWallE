using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public abstract class LevelEvent
{
    public float triggerTime;
    public string description;
    [HideInInspector] public bool hasTriggered;
    
    public abstract void Execute();
}

[Serializable]
public class SpawnEnemyWaveEvent : LevelEvent
{
    [Min(1)] public int enemyCount = 5;
    
    public override void Execute()
    {
        EnemyManager.Instance?.SpawnEnemyWave(enemyCount);
    }
}

[Serializable]
public class SpawnStructureEvent : LevelEvent
{
    public Structure structurePrefab;
    public Vector3 spawnPosition;
    
    public override void Execute()
    {
        if (structurePrefab)
        {
            StructureDispatcher.Instance?.DeployPod(structurePrefab, spawnPosition, Vector3.up);
        }
    }
}

[Serializable]
public class ToggleEnemySpawnPointEvent : LevelEvent
{
    public EnemySpawnPoint enemySpawnPoint;
    public bool spawnPointState = true;
    
    public override void Execute()
    {
        enemySpawnPoint?.SetActiveState(spawnPointState);
    }
}

[Serializable]
public class UseStructureSpawnPointEvent : LevelEvent
{
    public StructureSpawnPoint structureSpawnPoint;
    
    public override void Execute()
    {
        structureSpawnPoint?.SpawnStructure();
    }
}

[Serializable]
public class CustomEvent : LevelEvent
{
    public UnityEvent onTrigger;
    
    public override void Execute()
    {
        onTrigger?.Invoke();
    }
}