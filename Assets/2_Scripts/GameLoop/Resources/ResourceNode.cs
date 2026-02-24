using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using UnityEngine;

[DisallowMultipleComponent]
public class ResourceNode : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 50;
    [SerializeField] private int resourceAmount = 100;
    
    [SerializeField, ReadOnly] private float currentHealth;

    public event Action<IDamageable> OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }


    [Button]
    private void DestroySelf()
    {
        OnDeath?.Invoke(this);
        ResourceManager.Instance?.AddResources(resourceAmount);
        Destroy(gameObject);
    }

    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (currentHealth <= 0) return;
        
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            DestroySelf();
        }
    }

    public void Push(Vector3 direction, float force)
    {
        
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Health: {currentHealth}/{maxHealth}",
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