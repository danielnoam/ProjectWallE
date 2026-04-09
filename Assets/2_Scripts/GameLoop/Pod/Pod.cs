using System.Collections;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CinemachineExtensions;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class Pod : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float arcHeight = 125f;
        [SerializeField] private float rotationSpeed = 300f;
        [SerializeField, MinMaxRange(0, 10)] private RangedFloat travelDuration = new RangedFloat(3,5);

        [Header("Impact")]
        [SerializeField] private float pushRange = 10f;
        [SerializeField] private float impactDamage = 100f;
        [SerializeField, MinMaxRange(0, 100)] private RangedFloat pushStrength = new RangedFloat(3, 15);

        [Header("Effects")]
        [SerializeField] private ParticleEffectAction collisionEffect;
        [SerializeField] private ImpulseSettings collisionImpulseSettings;
        [SerializeField, AutoGetSelf, HideInInspector] private CinemachineImpulseSource impulseSource;


        private StructureNode _structureNode;
        private IDeployable _deployable;
        private Transform _parent;
        private Vector3 _startPosition;
        private Vector3 _targetPoint;
        private Vector3 _forward;
        private Vector3 _surfaceNormal;
        private bool _damageOnImpact;


        private IEnumerator MoveInArc()
        {
            float elapsed = 0f;
            Vector3 previousPosition = _startPosition;
            float duration = travelDuration.RandomValue;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Vector3 position = Vector3.Lerp(_startPosition, _targetPoint, t);

                position.y += arcHeight * Mathf.Sin(t * Mathf.PI);

                Vector3 movementDirection = position - previousPosition;
                if (movementDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion directionRotation = Quaternion.LookRotation(movementDirection);
                    Quaternion offset = Quaternion.Euler(new Vector3(90, 0, 0));
                    float yRotation = elapsed * rotationSpeed * 360f;
                    Quaternion spinRotation = Quaternion.Euler(0, yRotation, 0);

                    transform.rotation = directionRotation * offset * spinRotation;
                }

                transform.position = position;
                previousPosition = position;
                yield return null;
            }

            Land();
        }

        private void Land()
        {
            impulseSource?.GenerateImpulse(collisionImpulseSettings);
            collisionEffect?.Play(_targetPoint);

            if (_damageOnImpact)
            {
                var colliders = Physics.OverlapSphere(_targetPoint, pushRange);
                foreach (Collider col in colliders)
                {
                    Enemy enemy = col.GetComponentInParent<IDamageable>() as Enemy;
                    enemy?.TakeDamage(impactDamage);

                    IPushable pushable = col.GetComponentInParent<IPushable>();
                    if (pushable != null)
                    {
                        float distance = Vector3.Distance(_targetPoint, col.transform.position);
                        float push = pushStrength.Lerp(1 - distance / pushRange);

                        Vector3 direction = (col.transform.position - _targetPoint).normalized;
                        pushable.Push(direction, push);
                    }
                }
            }
            

            MonoBehaviour instance = _parent
                ? Instantiate(_deployable as MonoBehaviour, _targetPoint, Quaternion.LookRotation(_forward), _parent)
                : Instantiate(_deployable as MonoBehaviour, _targetPoint, Quaternion.LookRotation(_forward));
            ((IDeployable)instance).Deploy(_targetPoint, _surfaceNormal, _forward);

            if (_structureNode && instance is Structure structure)
            {
                _structureNode.Occupy(structure);
            }

            Destroy(gameObject);
        }

        public void Initialize<T>(T deployable, Vector3 targetPoint, Vector3 forward, Transform parent, bool damageOnImpact = true, Vector3 surfaceNormal = default) where T : MonoBehaviour, IDeployable
        {
            _startPosition = transform.position;
            _deployable = deployable;
            _targetPoint = targetPoint;
            _forward = forward;
            _parent = parent;
            _damageOnImpact = damageOnImpact;
            _surfaceNormal = surfaceNormal == default ? Vector3.up : surfaceNormal;

            StartCoroutine(MoveInArc());
        }

        public void Initialize<T>(T deployable, Transform parent, StructureNode structureNode) where T : MonoBehaviour, IDeployable
        {
            _structureNode = structureNode;
            Initialize(deployable, _structureNode.SnapPoint, _structureNode.transform.forward, parent, surfaceNormal: _structureNode.transform.up);
        }
    }
}