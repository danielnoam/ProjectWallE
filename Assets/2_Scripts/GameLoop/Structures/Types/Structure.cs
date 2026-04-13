using System;
using System.Collections.Generic;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.Button;
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

[Serializable]
public struct StructureUIData
{
    [SerializeField] private string label;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite upgradeIcon;
    [SerializeField] private Sprite fixIcon;
    [SerializeField] private Sprite demolishIcon;
    [SerializeField] private GameObject ghostPrefab;
    
    public string Label => label;
    public Sprite Icon => icon;
    public Sprite UpgradeIcon => upgradeIcon;
    public Sprite FixIcon => fixIcon;
    public Sprite DemolishIcon => demolishIcon;
    public GameObject GhostPrefab => ghostPrefab;
}

[DisallowMultipleComponent]
[SelectionBase]
public abstract class Structure : MonoBehaviour, IDamageable, IDeployable
{
    public static event Action<Structure> OnStructureBuilt;
    public static event Action<Structure> OnStructureUpgraded;
    public static event Action<Structure> OnStructureDemolished;
    
    [Header("Structure")]
    [SerializeField] private bool canDemolish;
    [SerializeField] protected Vector3 topPoint = Vector3.up;
    [SerializeField] protected Vector3 bottomPoint = Vector3.down;
    [SerializeField] private StructureUIData structureUIData;
    [SerializeField] private DamageEffects damageEffects;
    [SerializeField] private StructureBuildEffect buildEffect;
    [SerializeField] private StructureUpgradeEffect upgradeEffect;
    [SerializeField] private StructureUpgradeEffect fixEffect;
    [SerializeField] private StructureBreakEffect brokenEffect;

    [SerializeField, AutoGetSelf, HideInInspector] private RadarTarget radarTarget;
    
    private Material[] _materials;
    private int UpgradeCost => CanUpgrade() ? Levels[CurrentUpgradeLevel].cost : 0;
    private float FixCost => (MaxHealth - CurrentHealth) * CurrentLevelData.fixCostPerHealthPoint;
    private StructureLevelData CurrentLevelData => Levels[CurrentUpgradeLevel - 1];
    
    protected string StateInfo;
    
    public abstract StructureLevelData[] Levels { get; }
    public int CurrentUpgradeLevel { get; private set; }
    public float CurrentHealth { get; private set; }
    public StructureUIData StructureUIData => structureUIData;
    public Vector3 TopPoint => transform.position + transform.TransformVector(topPoint);
    public float MaxHealth => CurrentLevelData.maxHealth;
    public int BuildCost => Levels[0].cost;
    public bool IsAlive => CurrentHealth > 0;
    
    public event Action<IDamageable> OnDeath;
    public event Action<float> OnDamaged;

    protected virtual void OnBuild() {}
    protected virtual void OnFix() {}
    protected virtual void OnUpgrade() {}
    protected virtual void OnBreak() {}
    
    private void OnValidate()
    {
        if (Levels == null || Levels.Length == 0)
        {
            Debug.LogWarning($"{name}: Levels array is empty, must have at least 1 level.", this);
        }
        
        AutoGetSystem.Process(this);
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
        
        brokenEffect?.Initialize();
        
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
        buildEffect?.Play(OnBuild);
        radarTarget?.PingBlip();
        OnStructureBuilt?.Invoke(this);
    }
    

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Break()
    {
        CurrentHealth = 0f;
        brokenEffect?.SetBroken(transform.position);
        StructureManager.Instance?.UnregisterStructure(this);
        OnDeath?.Invoke(this);
        radarTarget?.PingBlip(Color.red);
        OnBreak();
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Demolish()
    {
        OnStructureDemolished?.Invoke(this);
        Destroy(gameObject);
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Fix()
    {
        if (ResourceManager.Instance && !ResourceManager.Instance.TrySpendResources((int)FixCost)) return;

        if (CurrentHealth <= 0) brokenEffect?.SetNormal();
        CurrentHealth = MaxHealth;
        fixEffect?.Play();
        OnFix();
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Upgrade()
    {
        if (!CanUpgrade())
        {
            Debug.Log("At max upgrade level");
            return;
        }

        if (ResourceManager.Instance && !ResourceManager.Instance.TrySpendResources(UpgradeCost)) return;

        if (CurrentHealth <= 0) brokenEffect?.SetNormal();
        CurrentUpgradeLevel++;
        CurrentHealth = CurrentLevelData.maxHealth;
        upgradeEffect?.Play();
        OnUpgrade();
        OnStructureUpgraded?.Invoke(this);
    }
    
    private bool CanUpgrade() 
    {
        return CurrentUpgradeLevel < Levels.Length;
    }


    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (CurrentHealth <= 0 || damage <= 0) return;

        radarTarget?.PunchBlip(Color.darkOrange);
        damageEffects?.Play(transform.position, _materials);
        CurrentHealth -= damage;
        OnDamaged?.Invoke(damage);
        
        if (CurrentHealth <= 0)
        {
            Break();
        }
    }

    public void Deploy(DeploymentRequest request)
    {
        Vector3 projectedForward = Vector3.ProjectOnPlane(request.TargetForward, request.TargetSurfaceNormal).normalized;
        Quaternion yawRotation = projectedForward.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(projectedForward, request.TargetSurfaceNormal)
            : Quaternion.identity;

        transform.position = request.TargetPosition - yawRotation * bottomPoint;
        transform.rotation = yawRotation;
        
        if (request.Node) request.Node.Occupy(this);
        
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
                Label = $"{structureUIData.Label}\n{upgradeLabel}",
                Icon = structureUIData.UpgradeIcon,
                IsAvailable = canUpgrade && ResourceManager.Instance.CanAfford(UpgradeCost),
                OnSelected = Upgrade
            },
            new StructureAction
            {
                Label = $"{structureUIData.Label}\n{fixLabel}",
                Icon = structureUIData.FixIcon,
                IsAvailable = canFix && ResourceManager.Instance.CanAfford((int)FixCost),
                OnSelected = Fix
            },
            new StructureAction
            {
            Label = $"{structureUIData.Label}\n{demolishLabel}",
            Icon = structureUIData.DemolishIcon,
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