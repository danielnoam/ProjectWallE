using System;
using DNExtensions;
using DNExtensions.Button;
using PrimeTween;
using UnityEditor;
using UnityEngine;




[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
[SelectionBase]
public abstract class Structure : MonoBehaviour
{

    [Header("Structure Settings")]
    [SerializeField, Range(1f,100f)] protected float startHealth = 100f;
    [SerializeField, Range(1,3)] protected int maxUpgradeLevel = 1;
    [SerializeField] protected Vector3 bottomPoint = Vector3.down;
    [SerializeField] protected Transform gfx;
    [SerializeField] protected AudioClip buildSfx;
    [SerializeField, ReadOnly] protected float currentHealth;
    [SerializeField, ReadOnly] protected int currentUpgradeLevel;
    

    

    
    private AudioSource _audioSource;
    public Vector3 BottomPoint => bottomPoint;

    
    public event Action OnBuilt;
    public event Action OnUpgraded;
    public event Action OnBroken;
    
    

    protected abstract void OnBuild();
    protected abstract void OnUpgrade();
    protected abstract void OnBreak();


    private void OnValidate()
    {
        if (!_audioSource) _audioSource = this.GetOrAddComponent<AudioSource>();
    }


    protected void Start()
    {
        ResetStats();
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
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Break();
        }
    }
    
    
    public void Build()
    {
        ResetStats();
        PlaySpawnEffect();
        OnBuild();
        OnBuilt?.Invoke();
    }
    

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void Break()
    {
        OnBroken?.Invoke();
        OnBreak();
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void Upgrade()
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
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + bottomPoint, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + bottomPoint);

#if UNITY_EDITOR
        Handles.Label(transform.position + bottomPoint, "Bottom Point");
#endif
    }

}
