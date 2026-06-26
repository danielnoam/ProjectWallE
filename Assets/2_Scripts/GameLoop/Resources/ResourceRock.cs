using System;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.GameLoop;
using UnityEngine;

[DisallowMultipleComponent]
public class ResourceRock : MonoBehaviour, IDamageable
{
    public static event Action<ResourceRock> OnDestroyed;
    
    [Header("Settings")]
    [SerializeField] private float maxHealth = 150;
    [SerializeField] private int resourceAmount = 100;
    [SerializeField] private DamageEffects damageEffects;
    [SerializeField] private ParticleEffectAction destroyEffect;
    [SerializeField, AutoGetChildren, HideInInspector] private Renderer rend;
    
    private float _currentHealth;
    private Material _material;

    public bool IsAlive => _currentHealth > 0;
    public Team Team => Team.Neutral;
    
    public event Action<IDamageable> OnDeath;
    public event Action<DamageInfo> OnDamaged;
    
    
    private void Awake()
    {
        _currentHealth = maxHealth;
        _material = rend.material;
    }
    private void DestroySelf()
    {
        destroyEffect?.Play(transform.position);
        ResourceManager.Instance?.AddResources(resourceAmount);
        OnDeath?.Invoke(this);
        OnDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (_currentHealth <= 0) return;

        _currentHealth -= damage;
        damageEffects?.Play(transform.position, _material);
        OnDamaged?.Invoke(new DamageInfo(damage, transform.position));

        if (_currentHealth <= 0)
            DestroySelf();
    }
    


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Health: {_currentHealth}/{maxHealth}",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.white },
                fontSize = 8,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            });
    }
#endif
}