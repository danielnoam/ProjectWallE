using UnityEngine;

public class DamageNumberSource : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset;

    [Header("On Death")]
    [SerializeField] private bool showOnDeath;
    [SerializeField] private bool useCustomText;
    [SerializeField] private string customText = "Dead";
    [SerializeField] private float deathNumber;
    [SerializeField] private bool deathIsCritical;

    private IDamageable _damageable;

    private void Awake()
    {
        _damageable = GetComponent<IDamageable>();
    }

    private void OnEnable()
    {
        if (_damageable == null) return;
        _damageable.OnDamaged += OnDamaged;
        _damageable.OnDeath += OnDeath;
    }

    private void OnDisable()
    {
        if (_damageable == null) return;
        _damageable.OnDamaged -= OnDamaged;
        _damageable.OnDeath -= OnDeath;
    }

    private void OnDamaged(DamageInfo info)
    {
        DamageNumberManager.Instance?.Spawn(new DamageInfo(info.Amount, info.Position + positionOffset, info.IsCritical, info.Attacker));
    }

    private void OnDeath(IDamageable damageable)
    {
        if (!showOnDeath) return;

        string text = useCustomText ? customText : null;
        DamageNumberManager.Instance?.Spawn(new DamageInfo(deathNumber, transform.position + positionOffset, deathIsCritical, null, text));
    }
}
