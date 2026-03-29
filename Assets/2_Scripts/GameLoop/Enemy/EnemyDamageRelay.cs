using System;
using System.Collections.Generic;
using ProjectWallE.GameLoop;
using UnityEngine;

public class EnemyDamageRelay : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyHitZone hitZone = EnemyHitZone.Normal;
    
    public EnemyHitZone HitZone => hitZone;
    public Enemy Parent { get; private set; }
    public Material[] Materials { get; private set; }

    // disabled warning for this event because its never used but is part of the interface
    #pragma warning disable 0067
    public event Action<IDamageable> OnDeath;
    public event Action<float> OnDamaged;
    #pragma warning restore 0067

    private void Awake()
    {
        Parent = GetComponentInParent<Enemy>();

        if (Parent)
        {
            Parent.RegisterRelay(this);

            var renderers = GetComponentsInChildren<Renderer>();
            var mats = new List<Material>();
            foreach (var rend in renderers)
            {
                if (rend is UnityEngine.VFX.VFXRenderer) continue;
                foreach (var mat in rend.materials)
                {
                    if (mat.HasProperty(DamageEffects.EmissionStrength)) mats.Add(mat);
                }
            }
            Materials = mats.ToArray();
        }
        else
        {
            enabled = false;
        }

    }

    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (!Parent || !Parent.IsAlive) return;
        Parent.TakeHitZoneDamage(damage, attacker, this);
    }
}