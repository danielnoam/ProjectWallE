using System;
using UnityEngine;

public class PlayerSpawnPoint : BaseSpawnPoint
{
    public static PlayerSpawnPoint Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Debug.LogWarning("Multiple PlayerSpawnPoints in scene!", gameObject);
            return;
        }
        Instance = this;
    }

    private void OnValidate()
    {
        if (Application.isPlaying || gameObject.scene.name == null) return;
        
        gameObject.name = $"PlayerSpawnPoint";
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2);
        
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (2 + 0.5f),
            "Player Spawn Point",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.yellow },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            }
        );
    }
    
#endif
}