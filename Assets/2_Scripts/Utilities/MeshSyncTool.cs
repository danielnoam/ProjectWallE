using UnityEngine;

public class MeshSyncTool : MonoBehaviour
{
    [Header("Source (Animated)")]
    [SerializeField] private Transform animatedRoot;

    [Header("Target (Code Mesh)")]
    [SerializeField] private Transform targetRoot;

    [ContextMenu("Sync Mesh From Animation")]
    public void Sync()
    {
        if (animatedRoot == null || targetRoot == null)
        {
            Debug.LogError("Assign both roots!");
            return;
        }

        TransformCopyUtility.CopyHierarchy(animatedRoot, targetRoot);

        Debug.Log("Mesh synced successfully!");
    }
}