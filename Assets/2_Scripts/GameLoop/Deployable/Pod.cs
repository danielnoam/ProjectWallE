using System;
using System.Collections;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.SerializableSelector;
using Unity.Cinemachine;
using UnityEngine;

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
        [SerializeField] private VisualEffectAction thrusterEffect;
        [SerializeReference, SerializableSelector(Foldout = false)] private DeployBehavior[] onLand;
        [SerializeReference, SerializableSelector(Foldout = false)] private DeployBehavior[] onDeploy;
        [SerializeField, AutoGetSelf, HideInInspector] private CinemachineImpulseSource impulseSource;
        [SerializeField, AutoGetSelf, HideInInspector] private AudioSource audioSource;

        
        private const float ArcHeight = 125f;
        
        private Coroutine _moveCoroutine;
        private IDeployable _deployable;
        private DeploymentRequest _deploymentRequest;
        private BehaviorRequest _behaviourRequest;
        private Vector3 _startPosition;

        public event Action OnLand;
        public event Action OnDeploy;

        
        
        private IEnumerator MoveInArc()
        {
            thrusterEffect?.Play(transform.position, audioSource);
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
                Vector3 position = Vector3.Lerp(_startPosition, _deploymentRequest.TargetPosition, progress);
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
            thrusterEffect?.Play(transform.position, audioSource);
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
                Vector3 position = Vector3.Lerp(_startPosition, _deploymentRequest.TargetPosition, progress);

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
            thrusterEffect?.Stop(audioSource);
            foreach (var effect in onLand)
            {
                effect?.Execute(_behaviourRequest);
            }
            OnLand?.Invoke();
        }

        private void Deploy()
        {
            foreach (var effect in onDeploy)
            {
                effect?.Execute(_behaviourRequest);
            }
            if (_deploymentRequest.InstantiateOnLand && _deployable != null)
            {
                MonoBehaviour instance = _deploymentRequest.DeployableParent
                    ? Instantiate(_deployable as MonoBehaviour, _deploymentRequest.TargetPosition, Quaternion.LookRotation(_deploymentRequest.TargetForward), _deploymentRequest.DeployableParent)
                    : Instantiate(_deployable as MonoBehaviour, _deploymentRequest.TargetPosition, Quaternion.LookRotation(_deploymentRequest.TargetForward));
                ((IDeployable)instance).Deploy(_deploymentRequest, _behaviourRequest);
            }
            else
            {
                _deployable?.Deploy(_deploymentRequest, _behaviourRequest);
            }
            
            OnDeploy?.Invoke();
            Destroy(gameObject);
        }
        
        public void Initialize<T>(T deployable, DeploymentRequest request, bool moveInArc) where T : MonoBehaviour, IDeployable
        {
            _deployable = deployable;
            _deploymentRequest = request;
            _startPosition = transform.position;

            _behaviourRequest = new BehaviorRequest()
            {
                Position = _deploymentRequest.TargetPosition,
                Forward = _deploymentRequest.TargetForward,
                SurfaceNormal = _deploymentRequest.TargetSurfaceNormal,
                Team = _deploymentRequest.Team,
                AudioSource = null,
                ImpulseSource = impulseSource,
            };
            
            _moveCoroutine = StartCoroutine(moveInArc ? MoveInArc() : MoveLinearly());
        }
        
        public void Initialize(ScriptableObject deployable, DeploymentRequest request, bool moveInArc)
        {
            _deployable = deployable as IDeployable;
            _deploymentRequest = request;
            _startPosition = transform.position;
            
            _behaviourRequest = new BehaviorRequest()
            {
                Position = _deploymentRequest.TargetPosition,
                Forward = _deploymentRequest.TargetForward,
                SurfaceNormal = _deploymentRequest.TargetSurfaceNormal,
                Team = _deploymentRequest.Team,
                AudioSource = null,
                ImpulseSource = impulseSource,
            };
            
            _moveCoroutine = StartCoroutine(moveInArc ? MoveInArc() : MoveLinearly());
        }
    }
}