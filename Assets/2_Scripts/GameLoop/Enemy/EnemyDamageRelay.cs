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

    public event Action<IDamageable> OnDeath;
    public event Action<float> OnDamaged;
    
    private static readonly int EmissionStrength = Shader.PropertyToID("_Emission_Strength");

    private void Awake()
    {
        Parent = GetComponentInParent<Enemy>();

        if (Parent)
        {
            Parent.RegisterRelay(this);

            var renderers = GetComponentsInChildren<Renderer>();
            var mats = new List<Material>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty(EmissionStrength))
                        mats.Add(mat);
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