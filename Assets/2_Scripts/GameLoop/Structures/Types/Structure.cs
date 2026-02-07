using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEditor;
using UnityEngine;




[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
[SelectionBase]
public abstract class Structure : MonoBehaviour, IDamageable
{
    [Header("Structure Settings")]
    [SerializeField] private int buildCost = 100;
    [SerializeField] private string label = "Structure";
    [SerializeField] private string description = "A basic structure.";
    [SerializeField] private Sprite icon;
    [SerializeField, Range(1,3)] protected int maxUpgradeLevel = 1;
    [SerializeField] protected float startHealth = 100f;
    [SerializeField] protected Vector3 bottomPoint = Vector3.down;
    [SerializeField] protected AudioClip buildSfx;
    [SerializeField] protected Transform gfx;
    [SerializeField, ReadOnly] protected float currentHealth;
    [SerializeField, ReadOnly] protected int currentUpgradeLevel;
    

    protected string StateInfo;
    private AudioSource _audioSource;
    public Vector3 BottomPoint => bottomPoint;
    public int BuildCost => buildCost;
    public  string Label => label;
    public string Description => description;
    public Sprite Icon => icon;

    
    public event Action OnBuilt;
    public event Action OnUpgraded;
    public event Action OnBroken;
    public event Action<IDamageable> OnDeath;
    
    

    protected abstract void OnBuild();
    protected abstract void OnUpgrade();
    protected abstract void OnBreak();


    private void OnValidate()
    {
        if (!_audioSource) _audioSource = this.GetOrAddComponent<AudioSource>();
    }


    private void Awake()
    {
        ResetStats();
    }
    
    private void OnDestroy()
    {
        StructureManager.Instance?.RegisterStructure(this);
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
        spawnSequence.OnComplete(() =>
        {
            if (buildSfx) _audioSource?.PlayOneShot(buildSfx);
        });

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
        if (buildSfx) _audioSource?.PlayOneShot(buildSfx);
        OnUpgrade();
        OnUpgraded?.Invoke();
    }
    
    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (currentHealth <= 0 || damage <= 0) return;
        
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Break();
        }
    }

    public float CurrentHealth => currentHealth;
    public float MaxHealth => startHealth;

    public Transform Transform()
    {
        return transform;
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