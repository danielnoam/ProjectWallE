
using DNExtensions.Utilities;
using UnityEngine;

public enum ProjectileMovementType
{
    Linear,
    Arc,
}
[CreateAssetMenu(fileName = "New ProjectileData", menuName = "Projectile Data")]
public class ProjectileData : ScriptableObject
{

    [Header("Settings")]
    public float damage = 20f;
    public float maxLifetime = 10f;
    
    [Header("Movement")]
    public ProjectileMovementType movementType = ProjectileMovementType.Linear;
    public float speed = 55f;
    
    [Header("Visuals")]
    [SerializeField, PrefabSelector("Assets/Prefabs")] public Projectile prefab;

    public Projectile Spawn(IDamageable owner, LayerMask hitLayers, Vector3 position, Vector3 direction, Vector3 targetPosition = default)
    {
        var p = Instantiate(prefab, position, Quaternion.LookRotation(direction));
        p.Initialize(owner, this, hitLayers, direction, targetPosition);
        return p;
    }
}