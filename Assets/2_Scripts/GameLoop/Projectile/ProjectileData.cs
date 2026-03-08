
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Utilities;
using UnityEngine;

public enum ProjectileMovementType
{

    [Tooltip("Moves in a straight line")] 
    Linear = 0,
    [Tooltip("Moves in an arc")]
    Arc = 1,
}

public enum ProjectileDamageType
{
    [Tooltip("Damage is applied to whatever it hits")]
    Single = 0,
    [Tooltip("Damage decreases with distance from center")]
    AreaOfEffect = 1,
}

[CreateAssetMenu(fileName = "New ProjectileData", menuName = "Projectile Data")]
public class ProjectileData : ScriptableObject
{

    [Header("Settings")]
    public float maxLifetime = 10f;
    public float pushStrength = 25f;
    public ProjectileDamageType damageType = ProjectileDamageType.Single;
    [ShowIf("damageType", ProjectileDamageType.Single)] public float damage = 20f;
    [ShowIf("damageType", ProjectileDamageType.AreaOfEffect)] public float aoeRadius = 5f;
    [ShowIf("damageType", ProjectileDamageType.AreaOfEffect), MinMaxRange(0, 100)] public RangedFloat damageRange = 5f;

    
    [Header("Movement")]
    public ProjectileMovementType movementType = ProjectileMovementType.Linear;
    public float speed = 55f;
    
    [Header("Visuals")]
    [PrefabSelector("Assets/Prefabs")] public Projectile prefab;
    [PrefabSelector("Assets/Prefabs")] public PoolableParticleSystem hitParticle;

    public Projectile Spawn(LayerMask hitLayers, Vector3 position, Vector3 direction, Vector3 targetPosition = default)
    {
        var projectile = ObjectPooler.GetObjectFromPool(prefab, position, Quaternion.LookRotation(direction));
        projectile.Initialize(this, hitLayers, direction, targetPosition);
        
        return projectile;
    }
}