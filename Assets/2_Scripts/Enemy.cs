using System;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using UnityEngine;
using UnityEngine.AI;




[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
[SelectionBase]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Settings")]
    [SerializeField] private float maxHealth = 100f;

    [SerializeField] private AudioClip damagedSfx;
    [SerializeField] private AudioClip deathSfx;
    [SerializeField, ReadOnly] private State currentState = State.Moving;
    
    [Header("Movement Settings")]
    [SerializeField] private float targetRange = 5f;
    
    [Header("Combat Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Projectile projectilePrefab;

    private float _attackTimer;
    private float _currentHealth;
    private IDamageable _mainTarget;
    private IDamageable _secondaryTarget;
    private IDamageable CurrentTarget => _secondaryTarget ?? _mainTarget;
    private enum State { Attacking, Moving}
    
    
    private void Awake()
    {
        _currentHealth = maxHealth;
        currentState = State.Moving;
        
        FindMainTarget();
        UpdateSecondaryTarget();
        GoToTarget();
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Attacking:
                
                if (CurrentTarget != null)
                {
                    float distance = Vector3.Distance(transform.position, CurrentTarget.Transform().position);
                    if (distance > targetRange)
                    {
                        currentState = State.Moving;
                        GoToTarget();
                    }
                    
                    _attackTimer += Time.deltaTime;
                    if (_attackTimer >= attackCooldown)
                    {
                        CurrentTarget.TakeDamage(attackDamage, this);
                        _attackTimer = 0f;
                    }
                }
                
                break;
            case State.Moving:

                if (CurrentTarget != null)
                {
                    float distance = Vector3.Distance(transform.position, CurrentTarget.Transform().position);
                    if (distance <= targetRange)
                    {
                        currentState = State.Attacking;
                        navMeshAgent.ResetPath();
                    }
                }
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Die()
    {
        if (deathSfx)
        {
            audioSource?.PlayOneShot(deathSfx);
        }
        
        Destroy(gameObject, deathSfx ? deathSfx.length : 0f);
    }
    
    
    private void FindMainTarget()
    {
        var bases = FindObjectsByType<Base>(FindObjectsSortMode.None).ToList();
        
        float closestDistance = float.MaxValue;
        Base closestBase = null;
        
        foreach (Base b in bases)
        {
            float distance = Vector3.Distance(transform.position, b.Transform().position);
            if (!(distance < closestDistance)) continue;
            
            
            closestDistance = distance;
            closestBase = b;
        }
        
        if (closestBase != null)
        {
            _mainTarget = closestBase;
        }
    }

    private void UpdateSecondaryTarget(IDamageable newTarget = null)
    {
        if (newTarget == null) return;
        _secondaryTarget = newTarget;
    }
    
    
    private void GoToTarget()
    {
        if (_secondaryTarget != null)
        {
            navMeshAgent.SetDestination(_secondaryTarget.Transform().position);
        }
        else if (_mainTarget != null)
        {
            navMeshAgent.SetDestination(_mainTarget.Transform().position);
            
        }
    }
    
    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        _currentHealth -= damage;
        UpdateSecondaryTarget(attacker);
        
        if (_currentHealth <= 0)
        {
            Die();
            return;
        }
        
        if (damagedSfx)
        {
            audioSource?.PlayOneShot(damagedSfx);
        }
    }

    public Transform Transform()
    {
        return transform;
    }

    private void OnDrawGizmos()
    {

        if (CurrentTarget != null && currentState == State.Moving)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentTarget.Transform().position);
        }
        

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Health: {_currentHealth}/{maxHealth} \nState: {currentState} \nCurrent Target: {CurrentTarget?.Transform().name}",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.red },
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            });
        #endif
    }
}
