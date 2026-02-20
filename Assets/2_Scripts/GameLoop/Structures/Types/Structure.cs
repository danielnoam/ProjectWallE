using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEditor;
using UnityEngine;


[DisallowMultipleComponent]
[SelectionBase]
public abstract class Structure : MonoBehaviour, IDamageable, IDeployable
{
    [Header("Structure Settings")]
    [SerializeField] private int buildCost = 100;
    [SerializeField] private string label = "Structure";
    [SerializeField] private string description = "A basic structure.";
    [SerializeField] private Sprite icon;
    [SerializeField, Range(1,3)] protected int maxUpgradeLevel = 1;
    [SerializeField] protected float startHealth = 100f;
    [SerializeField] protected Vector3 bottomPoint = Vector3.down;
    [SerializeField] protected Transform gfx;
    [SerializeField, ReadOnly] protected float currentHealth;
    [SerializeField, ReadOnly] protected int currentUpgradeLevel;
    

    protected string StateInfo;
    
    public int BuildCost => buildCost;
    public  string Label => label;
    public string Description => description;
    public Sprite Icon => icon;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => startHealth;
    
    public event Action OnBuilt;
    public event Action OnUpgraded;
    public event Action OnBroken;
    public event Action<IDamageable> OnDeath;
    
    

    protected abstract void OnBuild();
    protected abstract void OnUpgrade();
    protected abstract void OnBreak();

    


    private void Awake()
    {
        ResetStats();
    }
    
    private void OnDestroy()
    {
        StructureManager.Instance?.UnregisterStructure(this);
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    protected virtual void ResetStats()
    {
        currentHealth = startHealth;
        currentUpgradeLevel = 1;
    }
    
    protected virtual bool CanUpgrade()
    {
        return currentUpgradeLevel < maxUpgradeLevel;
    }
    
    protected virtual void PlaySpawnEffect()
    {

        var spawnSequence = Sequence.Create();
        
        spawnSequence.Group(Tween.Scale(gfx,Vector3.zero, Vector3.one , 0.3f, Ease.OutBack));
    }
    

    public void Build()
    {
        StructureManager.Instance?.RegisterStructure(this);
        ResetStats();
        PlaySpawnEffect();
        OnBuild();
        OnBuilt?.Invoke();
    }
    
    

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Break()
    {
        StructureManager.Instance?.UnregisterStructure(this);
        OnDeath?.Invoke(this);
        OnBroken?.Invoke();
        OnBreak();
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Upgrade()
    {
        if (!CanUpgrade())
        {
            Debug.Log("Can't upgrade");
            return;
        }

        currentUpgradeLevel++;
        OnUpgrade();
        OnUpgraded?.Invoke();
    }
    
    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (currentHealth <= 0 || damage <= 0) return;
        
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0f;
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
    

#if UNITY_EDITOR
    private void OnDrawGizmos()
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + bottomPoint, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + bottomPoint);
        Handles.Label(transform.position + bottomPoint, "Bottom Point");
    }
    
#endif


}