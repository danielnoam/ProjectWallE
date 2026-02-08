using System;
using DNExtensions;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("SpawnPoint Settings")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private float spawnPointRange = 10f;
    
    public float SpawnPointRange => spawnPointRange;
    
    private void OnValidate()
    {
        if (Application.isPlaying || gameObject.scene.name == null) return;
        
        gameObject.name = $"EnemySpawnPoint({(isActive ? "Active" : "NotActive")})";
    }

    private void Start()
    {
        LevelManager.OnLevelStarted += OnLevelStarted;
        LevelManager.OnLevelCompleted += OnLevelFinished;
        LevelManager.OnLevelFailed += OnLevelFinished;
    }

    private void OnDestroy()
    {
        LevelManager.OnLevelStarted -= OnLevelStarted;
        LevelManager.OnLevelCompleted -= OnLevelFinished;
        LevelManager.OnLevelFailed -= OnLevelFinished;
    }

    private void OnLevelStarted()
    {
        if (isActive)
        {
            EnemyManager.Instance.RegisterSpawnPoint(this);
        }
    }
    
    private void OnLevelFinished()
    {
        EnemyManager.Instance.UnregisterSpawnPoint(this);
    }
    
    public void SetActiveState(bool state)
    {
        isActive = state;
        
        if (isActive)
        {
            EnemyManager.Instance.RegisterSpawnPoint(this);
        }
        else
        {
            EnemyManager.Instance.UnregisterSpawnPoint(this);
        }
    }


    private void OnDrawGizmos()
    {
        if (isActive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnPointRange);

        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, spawnPointRange);
        }
        
        
#if UNITY_EDITOR
        var  enemyString = $"Enemy Spawn Point: {(isActive ? "Active" : "Not Active")}";
        
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (spawnPointRange + 1f),
            enemyString,
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.red },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            }
        );

#endif
    }
}