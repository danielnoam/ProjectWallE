using System;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Utilities.Button;
using UnityEngine;

[DisallowMultipleComponent]
public class ResourceNode : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 50;
    [SerializeField] private int resourceAmount = 100;
    
    [Header("Effects")]
    [SerializeField] private PoolableParticleSystem destroyEffect;
    [SerializeField] private Transform destroyEffectPosition;
    
    private float _currentHealth;

    public event Action<IDamageable> OnDeath;
    public event Action<float> OnDamaged;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }


    [Button]
    private void DestroySelf()
    {
        if (destroyEffect)
        {
            var effect = ObjectPooler.GetObjectFromPool(destroyEffect, destroyEffectPosition.position);
            effect?.Play();
        }
        OnDeath?.Invoke(this);
        ResourceManager.Instance?.AddResources(resourceAmount);
        Destroy(gameObject);
    }

    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (_currentHealth <= 0) return;
        
        _currentHealth -= damage;
        OnDamaged?.Invoke(damage);
        
        if (_currentHealth <= 0)
        {
            DestroySelf();
        }
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