using System;
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
        [SerializeField, MinMaxRange(-5, 5)] private RangedFloat rotationSpeedRange = new RangedFloat(1, 1);
        [SerializeField, MinMaxRange(0, 10)] private RangedFloat travelDurationRange = new RangedFloat(3, 5);

        [Header("Impact")]
        [SerializeField] private float pushRange = 10f;
        [SerializeField] private float impactDamage = 100f;
        [SerializeField, MinMaxRange(0, 100)] private RangedFloat pushStrength = new RangedFloat(3, 15);

        [Header("Effects")]
        [SerializeField] private ParticleEffectAction collisionEffect;
        [SerializeField] private VisualEffectAction explodeEffect;
        [SerializeField] private ImpulseSettings collisionImpulseSettings;
        [SerializeField, AutoGetSelf, HideInInspector] private CinemachineImpulseSource impulseSource;

        
        private Coroutine _moveCoroutine;
        private IDeployable _deployable;
        private DeploymentRequest _request;
        private Vector3 _startPosition;

        public event Action OnLand;

        
        private IEnumerator MoveInArc()
        {
            float elapsed = 0f;
            Vector3 previousPosition = _startPosition;
            float duration = travelDurationRange.RandomValue;
            float rotationSpeed = rotationSpeedRange.RandomValue;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Vector3 position = Vector3.Lerp(_startPosition, _request.TargetPosition, t);

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

        private IEnumerator MoveLinearly()
        {
            float elapsed = 0f;
            Vector3 previousPosition = _startPosition;
            float duration = travelDurationRange.RandomValue;
            float rotationSpeed = rotationSpeedRange.RandomValue;

            while (elapsed < duration)
            {

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Vector3 position = Vector3.Lerp(_startPosition, _request.TargetPosition, t);
                Vector3 movementDirection = position - previousPosition;
                if (movementDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion directionRotation = Quaternion.LookRotation(movementDirection);
                    Quaternion offset = Quaternion.Euler(new Vector3(90, 0, 0));
                    float yRotation = elapsed * rotationSpeed * 360f;
                    Quaternion spinRotation = Quaternion.Euler(0, yRotation, 0);
                    transform.rotation = directionRotation * offset * spinRotation;
                }
                
                previousPosition = position;
                transform.position = position;
                yield return null;
            }

            Land();
        }

        private void Land()
        {
            impulseSource?.GenerateImpulse(collisionImpulseSettings);
            collisionEffect?.Play(_request.TargetPosition, Quaternion.LookRotation(_request.TargetSurfaceNormal));
            explodeEffect?.Play(_request.TargetPosition.AddY(1));

            if (_request.DamageOnImpact)
            {
                var colliders = Physics.OverlapSphere(_request.TargetPosition, pushRange);
                foreach (Collider col in colliders)
                {
                    Enemy enemy = col.GetComponentInParent<IDamageable>() as Enemy;
                    enemy?.TakeDamage(impactDamage);

                    IPushable pushable = col.GetComponentInParent<IPushable>();
                    if (pushable != null)
                    {
                        float distance = Vector3.Distance(_request.TargetPosition, col.transform.position);
                        float push = pushStrength.Lerp(1 - distance / pushRange);

                        Vector3 direction = (col.transform.position - _request.TargetPosition).normalized;
                        pushable.Push(direction, push);
                    }
                }
            }

            if (_request.InstantiateOnLand)
            {
                MonoBehaviour instance = _request.Parent
                    ? Instantiate(_deployable as MonoBehaviour, _request.TargetPosition, Quaternion.LookRotation(_request.TargetForward), _request.Parent)
                    : Instantiate(_deployable as MonoBehaviour, _request.TargetPosition, Quaternion.LookRotation(_request.TargetForward));

                ((IDeployable)instance).Deploy(_request);
            }
            else
            {
                _deployable.Deploy(_request);
            }
            
            OnLand?.Invoke();

            Destroy(gameObject);
        }
        
        public void Initialize<T>(T deployable, DeploymentRequest request, bool moveInArc) where T : MonoBehaviour, IDeployable
        {
            _deployable = deployable;
            _request = request;
            _startPosition = transform.position;

            _moveCoroutine = StartCoroutine(moveInArc ? MoveInArc() : MoveLinearly());
        }
    }
}