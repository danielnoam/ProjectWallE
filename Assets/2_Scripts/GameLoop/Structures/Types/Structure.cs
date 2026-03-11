using System;
using System.Collections.Generic;
using DNExtensions.Utilities.Button;
using PrimeTween;
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
    [SerializeField] protected Vector3 topPoint = Vector3.up;
    [SerializeField] protected Vector3 bottomPoint = Vector3.down;
    [SerializeField] protected Transform gfx;
    
    

    protected StructureLevelData CurrentLevelData => Levels[currentUpgradeLevel - 1];
    protected string StateInfo;
    protected abstract StructureLevelData[] Levels { get; }
    public float CurrentHealth { get; private set; }
    
    public int currentUpgradeLevel;
    public string Label => label;
    public Sprite Icon => icon;
    public Vector3 TopPoint => transform.position + transform.TransformVector(topPoint);
    public float MaxHealth => CurrentLevelData.maxHealth;
    public int BuildCost => Levels[0].cost;
    public int UpgradeCost => CanUpgrade() ? Levels[currentUpgradeLevel].cost : 0;
    public float FixCost => (MaxHealth - CurrentHealth) * CurrentLevelData.fixCostPerHealthPoint;
    
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
    
    private void OnDestroy()
    {
        StructureManager.Instance?.UnregisterStructure(this);
    }
    
    private void Build()
    {
        StructureManager.Instance?.RegisterStructure(this);
        currentUpgradeLevel = 1;
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

    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (CurrentHealth <= 0 || damage <= 0) return;

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
    
    public bool CanUpgrade() 
    {
        return currentUpgradeLevel < Levels.Length;
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void Fix()
    {
        if (!ResourceManager.Instance.TrySpendResources((int)FixCost)) return;

        CurrentHealth = MaxHealth;
        OnFix();
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void Upgrade()
    {
        if (!CanUpgrade())
        {
            Debug.Log("Can't upgrade");
            return;
        }

        if (!ResourceManager.Instance.TrySpendResources(UpgradeCost)) return;

        currentUpgradeLevel++;
        CurrentHealth = CurrentLevelData.maxHealth;
        PlayUpgradeEffect();
        OnUpgrade();
    }
    
    public List<StructureAction> GetActions()
    {
        bool canUpgrade = CanUpgrade();
        bool canFix = CurrentHealth < MaxHealth;

        string upgradeLabel = canUpgrade
            ? $"Upgrade {currentUpgradeLevel} -> {currentUpgradeLevel + 1}\n{UpgradeCost}"
            : $"At Max Level\n{currentUpgradeLevel}/{currentUpgradeLevel}";

        string fixLabel = canFix
            ? $"Fix {(int)FixCost}\n{(int)CurrentHealth}/{(int)MaxHealth}"
            : $"At Full Health\n{(int)CurrentHealth}/{(int)MaxHealth}";

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
            }
        };
    }
    

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        Handles.Label(
            transform.position + Vector3.up * 2.5f,
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