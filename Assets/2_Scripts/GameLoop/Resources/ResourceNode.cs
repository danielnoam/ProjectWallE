using System;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
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
    [SerializeField, AudioLibraryID] private string damagedSoundId;
    [SerializeField, AudioLibraryID] private string destroySoundId;

    [Header("Emission")]
    [SerializeField] private float punchStrength = 1f;
    [SerializeField] private float punchDuration = 0.15f;
    [SerializeField] private Ease punchEase = Ease.Linear;
    [SerializeField, ColorUsage(false, true)] private Color emissionColor = Color.red;
    [SerializeField, AutoGetChildren] private Renderer rend;

    private static readonly int EmissionStrength = Shader.PropertyToID("_Emission_Strength");
    private static readonly int EmissionColorID = Shader.PropertyToID("_Emission_Color");
    
    private float _currentHealth;
    private Material _material;

    public event Action<IDamageable> OnDeath;
    public event Action<float> OnDamaged;
    
    

    private void Awake()
    {
        _currentHealth = maxHealth;
        _material = rend.material;
    }
    
    private void PunchEmission()
    {
        if (!_material) return;
        
        _material.SetColor(EmissionColorID, emissionColor);

        var seq = Sequence.Create();
        seq.Group(Tween.MaterialProperty(_material, EmissionStrength, punchStrength, punchDuration * 0.5f, punchEase));
        seq.Chain(Tween.MaterialProperty(_material, EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
    }
    
    private void DestroySelf()
    {
        if (destroyEffect)
        {
            var effect = ObjectPooler.GetObjectFromPool(destroyEffect, destroyEffectPosition.position);
            effect?.Play();
        }
        AudioLibrary.PlayAtPosition(destroySoundId, transform.position);
        OnDeath?.Invoke(this);
        ResourceManager.Instance?.AddResources(resourceAmount);
        Destroy(gameObject);
    }

    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (_currentHealth <= 0) return;

        _currentHealth -= damage;
        OnDamaged?.Invoke(damage);
        PunchEmission();
        AudioLibrary.PlayAtPosition(damagedSoundId, transform.position);

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