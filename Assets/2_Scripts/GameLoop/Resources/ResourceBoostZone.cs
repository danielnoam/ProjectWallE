using UnityEngine;

[DisallowMultipleComponent]
public class ResourceBoostZone : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] private int boostMultiplier = 2;
    
    public int BoostMultiplier => boostMultiplier;
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.purple;
    
        if (TryGetComponent(out SphereCollider sphereCollider))
        {
            Gizmos.DrawWireSphere(transform.position, sphereCollider.radius * transform.lossyScale.x);
            
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (sphereCollider.radius + 0.5f) * transform.lossyScale.x,
                $"Resource Boost Zone *{boostMultiplier}",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.purple },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                });
        }
    }
#endif
}