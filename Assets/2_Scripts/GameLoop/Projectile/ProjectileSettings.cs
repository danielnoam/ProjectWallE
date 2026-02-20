using System;
using DNExtensions.Utilities;
using UnityEngine;

public enum ProjectileMovementType { Linear, Arc }

[Serializable]
public struct ProjectileSettings
{
    public float damage;
    public LayerMask hitLayers;
    public ProjectileMovementType movementType;
    public float speed;
    [ShowIf("movementType", ProjectileMovementType.Arc)]
    public float arcHeight;

    
}