using System;
using System.Collections;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CinemachineExtensions;
using DNExtensions.Utilities.SerializableSelector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.VFX;

namespace ProjectWallE.GameLoop
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class Pod : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, MinMaxRange(-5, 5)] private RangedFloat rotationSpeedRange = new RangedFloat(1, 1);
        [SerializeField, MinMaxRange(0, 10)] private RangedFloat travelDurationRange = new RangedFloat(3, 5);
        [SerializeField, MinMaxRange(0, 10)] private RangedFloat delayBeforeDeployRange = new RangedFloat(0, 1.5f);


        [Header("Effects")]
        [SerializeField] private ImpulseSettings collisionImpulseSettings;
        [SerializeField] private VisualEffect fireVisualEffect;
        [SerializeReference, SerializableSelector(Foldout = false)] private DeployBehavior[] onLand;
        [SerializeReference, SerializableSelector(Foldout = false)] private DeployBehavior[] onDeploy;
        [SerializeField, AutoGetSelf, HideInInspector] private CinemachineImpulseSource impulseSource;

        
        private const float ArcHeight = 125f;
        
        private Coroutine _moveCoroutine;
        private IDeployable _deployable;
        private DeploymentRequest _request;
        private Vector3 _startPosition;

        public event Action OnLand;
        public event Action OnDeploy;

        
        private IEnumerator MoveInArc()
        {
            float elapsed = 0f;
            Vector3 previousPosition = _startPosition;
            float randomT = UnityEngine.Random.value;
            float duration = travelDurationRange.Lerp(randomT);
            float rotationSpeed = rotationSpeedRange.Lerp(1f - randomT);
            float delayBeforeDeploy = delayBeforeDeployRange.RandomValue;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                Vector3 position = Vector3.Lerp(_startPosition, _request.TargetPosition, progress);
                position.y += ArcHeight * Mathf.Sin(progress * Mathf.PI);

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
            if (delayBeforeDeploy > 0f) yield return new  WaitForSeconds(delayBeforeDeploy);
            Deploy();
        }

        private IEnumerator MoveLinearly()
        {
            float elapsed = 0f;
            Vector3 previousPosition = _startPosition;
            float randomT = UnityEngine.Random.value;
            float duration = travelDurationRange.Lerp(randomT);
            float rotationSpeed = rotationSpeedRange.Lerp(1f - randomT);
            float delayBeforeDeploy = delayBeforeDeployRange.RandomValue;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                Vector3 position = Vector3.Lerp(_startPosition, _request.TargetPosition, progress);

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
            if (delayBeforeDeploy > 0f) yield return new  WaitForSeconds(delayBeforeDeploy);
            Deploy();
        }

        private void Land()
        {
            fireVisualEffect?.Stop();
            foreach (var effect in onLand)
            {
                effect?.Execute(_request);
            }
            impulseSource?.GenerateImpulse(collisionImpulseSettings);
            OnLand?.Invoke();
        }

        private void Deploy()
        {
            foreach (var effect in onDeploy)
            {
                effect?.Execute(_request);
            }
            if (_request.InstantiateOnLand && _deployable != null)
            {
                MonoBehaviour instance = _request.Parent
                    ? Instantiate(_deployable as MonoBehaviour, _request.TargetPosition, Quaternion.LookRotation(_request.TargetForward), _request.Parent)
                    : Instantiate(_deployable as MonoBehaviour, _request.TargetPosition, Quaternion.LookRotation(_request.TargetForward));
                ((IDeployable)instance).Deploy(_request);
            }
            else
            {
                _deployable?.Deploy(_request);
            }
            
            OnDeploy?.Invoke();
            Destroy(gameObject);
        }
        
        public void Initialize<T>(T deployable, DeploymentRequest request, bool moveInArc) where T : MonoBehaviour, IDeployable
        {
            _deployable = deployable;
            _request = request;
            _startPosition = transform.position;

            _moveCoroutine = StartCoroutine(moveInArc ? MoveInArc() : MoveLinearly());
        }
        
        public void Initialize(ScriptableObject deployable, DeploymentRequest request, bool moveInArc)
        {
            _deployable = deployable as IDeployable;
            _request = request;
            _startPosition = transform.position;
            _moveCoroutine = StartCoroutine(moveInArc ? MoveInArc() : MoveLinearly());
        }
    }
}