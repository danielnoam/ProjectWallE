using DNExtensions;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("SpawnPoint Settings")]
    [SerializeField] private ChanceList<Enemy> availableEnemies;
    [SerializeField] private float spawnPointRange = 10f;
    [SerializeField] private Color gizmoColor = Color.darkRed;
    
    public ChanceList<Enemy> AvailableEnemies => availableEnemies;
    public float SpawnPointRange => spawnPointRange;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, spawnPointRange);
        
        
        
#if UNITY_EDITOR
        var  enemyString = availableEnemies is { Count: > 0 } ? $"Spawn Point: {availableEnemies.Count} enemies" : $"Spawn Point: No enemies assigned";
        
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (spawnPointRange + 0.5f),
            enemyString,
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = gizmoColor },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            }
        );

#endif
    }
}