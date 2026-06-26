using UnityEngine;

public class DamageNumberSource : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset;

    private IDamageable _damageable;

    private void Awake()
    {
        _damageable = GetComponent<IDamageable>();
    }

    private void OnEnable()
    {
        if (_damageable != null) _damageable.OnDamaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (_damageable != null) _damageable.OnDamaged -= OnDamaged;
    }

    private void OnDamaged(DamageInfo info)
    {
        DamageNumberManager.Instance?.Spawn(new DamageInfo(info.Amount, info.Position + positionOffset, info.IsCritical, info.Attacker));
    }
}
