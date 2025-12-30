using UnityEngine;

public class StructureSpawnPoint : MonoBehaviour
{
    [Header("SpawnPoint Settings")]
    [SerializeField] private Structure structurePrefab;
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private float gizmoRadius = 2f;

    public Structure StructurePrefab => structurePrefab;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (gizmoRadius + 0.5f),
            structurePrefab ? $"Spawn Point: {structurePrefab.Label}" :  $"Spawn Point: No Structure Assigned",
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