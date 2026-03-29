using System;
using System.Collections.Generic;
using DNExtensions.Utilities.Button;
using PrimeTween;
using ProjectWallE.GameLoop;
using UnityEditor;
using UnityEngine;

[Serializable]
public class StructureLevelData
{
    [Tooltip("Max health for this level")]
    public float maxHealth = 100f;
    [Tooltip("Cost to upgrade to this level")]
    public int cost = 100;
    [Tooltip("Cost to fix per 1 health point")]
    public float fixCostPerHealthPoint = 1;
}

[DisallowMultipleComponent]
[SelectionBase]
public abstract class Structure : MonoBehaviour, IDamageable, IDeployable
{
    [Header("Structure")]
    [SerializeField] private string label = "Structure";
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite upgradeIcon;
    [SerializeField] private Sprite fixIcon;
    [SerializeField] private Sprite demolishIcon;
    [SerializeField] private bool canDemolish;
    [SerializeField] protected Vector3 topPoint = Vector3.up;
    [SerializeField] protected Vector3 bottomPoint = Vector3.down;
    [SerializeField] protected Transform gfx;
    [SerializeField] private DamageEffects damageEffects;
    
    
    private Material[] _materials;
    private int UpgradeCost => CanUpgrade() ? Levels[CurrentUpgradeLevel].cost : 0;
    private float FixCost => (MaxHealth - CurrentHealth) * CurrentLevelData.fixCostPerHealthPoint;
    private StructureLevelData CurrentLevelData => Levels[CurrentUpgradeLevel - 1];
    
    protected string StateInfo;
    protected abstract StructureLevelData[] Levels { get; }
    protected int CurrentUpgradeLevel { get; private set; }
    
    public float CurrentHealth { get; private set; }
    public string Label => label;
    public Sprite Icon => icon;
    public Vector3 TopPoint => transform.position + transform.TransformVector(topPoint);
    public float MaxHealth => CurrentLevelData.maxHealth;
    public int BuildCost => Levels[0].cost;
    
    public event Action<IDamageable> OnDeath;
    public event Action<float> OnDamaged;

    protected abstract void OnBuild();
    protected abstract void OnFix();
    protected abstract void OnUpgrade();
    protected abstract void OnBreak();
    
    private void OnValidate()
    {
        if (Levels == null || Levels.Length == 0)
        {
            Debug.LogWarning($"{name}: Levels array is empty, must have at least 1 level.", this);
        }

    }

    private void Awake()
    {
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
        _materials = mats.ToArray();
        
        // initialize if in test scene and not in game
        if (!StructureManager.Instance)
        {
            Build();
        }
    }

    protected virtual void OnDestroy()
    {
        StructureManager.Instance?.UnregisterStructure(this);
    }
    
    private void Build()
    {
        StructureManager.Instance?.RegisterStructure(this);
        CurrentUpgradeLevel = 1;
        CurrentHealth = CurrentLevelData.maxHealth;
        PlaySpawnEffect();
        OnBuild();
    }

    private void PlaySpawnEffect()
    {
        Sequence.Create().Group(Tween.Scale(gfx, Vector3.zero, Vector3.one, 0.3f, Ease.OutBack));
    }
    
    private void PlayUpgradeEffect()
    {
        Sequence.Create().Group(Tween.PunchScale(gfx, Vector3.one * 0.9f, 0.2f,1,true, Ease.OutBack));
    }
    

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Break()
    {
        CurrentHealth = 0f;
        StructureManager.Instance?.UnregisterStructure(this);
        OnDeath?.Invoke(this);
        OnBreak();
    }
    
    private bool CanUpgrade() 
    {
        return CurrentUpgradeLevel < Levels.Length;
    }
    
    private void Demolish()
    {
        Destroy(gameObject);
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Fix()
    {
        if (!ResourceManager.Instance.TrySpendResources((int)FixCost)) return;

        CurrentHealth = MaxHealth;
        OnFix();
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Upgrade()
    {
        if (!CanUpgrade())
        {
            Debug.Log("Can't upgrade");
            return;
        }

        if (!ResourceManager.Instance.TrySpendResources(UpgradeCost)) return;

        CurrentUpgradeLevel++;
        CurrentHealth = CurrentLevelData.maxHealth;
        PlayUpgradeEffect();
        OnUpgrade();
    }


    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (CurrentHealth <= 0 || damage <= 0) return;

        damageEffects?.Play(transform.position, _materials);
        CurrentHealth -= damage;
        OnDamaged?.Invoke(damage);
        
        if (CurrentHealth <= 0)
        {
            Break();
        }
    }
    
    public void Deploy(Vector3 impactPoint, Vector3 surfaceNormal, Vector3 forward)
    {
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, surfaceNormal).normalized;
        Quaternion yawRotation = projectedForward.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(projectedForward, surfaceNormal)
            : Quaternion.identity;

        transform.position = impactPoint - yawRotation * bottomPoint;
        transform.rotation = yawRotation;
        Build();
    }
    
    public List<StructureAction> GetActions()
    {
        bool canUpgrade = CanUpgrade();
        bool canFix = CurrentHealth < MaxHealth;

        string upgradeLabel = canUpgrade
            ? $"Upgrade {CurrentUpgradeLevel} -> {CurrentUpgradeLevel + 1}\n{UpgradeCost}"
            : $"At Max Level\n{CurrentUpgradeLevel}/{CurrentUpgradeLevel}";

        string fixLabel = canFix
            ? $"Fix {(int)FixCost}\n{(int)CurrentHealth}/{(int)MaxHealth}"
            : $"At Full Health\n{(int)CurrentHealth}/{(int)MaxHealth}";
        
        string demolishLabel = $"Demolish";

        return new List<StructureAction>
        {
            new StructureAction
            {
                Label = $"{label}\n{upgradeLabel}",
                Icon = upgradeIcon,
                IsAvailable = canUpgrade && ResourceManager.Instance.CanAfford(UpgradeCost),
                OnSelected = Upgrade
            },
            new StructureAction
            {
                Label = $"{label}\n{fixLabel}",
                Icon = fixIcon,
                IsAvailable = canFix && ResourceManager.Instance.CanAfford((int)FixCost),
                OnSelected = Fix
            },
            new StructureAction
            {
            Label = $"{label}\n{demolishLabel}",
            Icon = demolishIcon,
            IsAvailable = canDemolish,
            OnSelected = Demolish
        }
        };
    }
    

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        Handles.Label(
            transform.position + topPoint,
            $"{StateInfo}",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.white },
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            });
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + bottomPoint, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + bottomPoint);
        Handles.Label(transform.position + bottomPoint, "Bottom Point");
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + topPoint, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + topPoint);
        Handles.Label(transform.position + topPoint, "Top Point");
    }
#endif
}