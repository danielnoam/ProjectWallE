using System;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CustomFields;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ProjectWallE.GameLoop.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animations")]
        [SerializeField] private AnimatorStateField switchToCar;
        [SerializeField] private AnimatorStateField switchToRobot;
        [SerializeField] private AnimatorStateField shootGun;

        [Header("IK")]
        [SerializeField] private float armTargetShoulderOffset = 0.7f;
        [SerializeField] private float armTargetDistance = 1.5f;
        [SerializeField] private float headTargetDistance = 1.5f;
        [SerializeField] private float transitionSpeed = 3f;
        
        [Header("Arm Shoot Shake")]
        [SerializeField] private float shakeAmount = 0.05f;
        [SerializeField] private float shakeDecay = 5f;
        [SerializeField] private float maxShake = 0.15f;
        [SerializeField] private Vector3 shakeDirection = Vector3.up;
        [SerializeField, Range(0f, 1f)] private float directionBias = 0.5f;

        [Header("References")]
        [SerializeField] private Rig rig;
        [SerializeField] private MultiAimConstraint headAim;
        [SerializeField] private TwoBoneIKConstraint armIK;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform shoulder;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;

        private Transform _armIkTarget;
        private Transform _headIkTarget;
        private Camera _cam;
        private float _targetWeight = 1f;
        private Vector3 _shakeOffset;

        private void OnValidate() => AutoGetSystem.Process(this);

        private void Awake()
        {
            _cam = Camera.main;
            
            _armIkTarget = new GameObject("PlayerArmIK_Target").transform;
            armIK.data.target = _armIkTarget;
            
            _headIkTarget = new GameObject("PlayerHeadIK_Target").transform;
            headAim.data.sourceObjects = new WeightedTransformArray { new WeightedTransform(_headIkTarget, 1f) };
        }

        private void OnEnable()
        {
            if (player)
            {
                player.Shooter.OnAttack1 += OnAttack1;
                player.Shooter.OnAttack2 += OnAttack1;
                player.OnControllerChanged += OnControllerChanged;
            }
        }

        private void OnDisable()
        {
            if (player)
            {
                player.Shooter.OnAttack1 -= OnAttack1;
                player.Shooter.OnAttack2 -= OnAttack1;
                player.OnControllerChanged -= OnControllerChanged;
            }
        }

        private void LateUpdate()
        {
            UpdateRigWeight();
            UpdateArmIK();
            UpdateHeadIK();
            ApplyShake();
        }

        private void UpdateRigWeight()
        {
            rig.weight = Mathf.MoveTowards(rig.weight, _targetWeight, transitionSpeed * Time.deltaTime);
        }

        private void UpdateArmIK()
        {
            if (!_cam || rig.weight <= 0.01f) return;

            float dot = Vector3.Dot(torso.forward, _cam.transform.forward);
            float dynamicOffset = Mathf.Lerp(armTargetShoulderOffset * 3f, armTargetShoulderOffset, (dot + 1f) * 0.5f);
            _armIkTarget.position = shoulder.position + _cam.transform.forward * armTargetDistance + torso.right * dynamicOffset;
            _armIkTarget.rotation = _cam.transform.rotation * Quaternion.Euler(0f, -90f, 0f);
        }

        private void UpdateHeadIK()
        {
            if (!_cam || rig.weight <= 0.01f) return;

            _headIkTarget.position = _cam.transform.position + _cam.transform.forward * headTargetDistance;
        }

        private void ApplyShake()
        {
            _shakeOffset = Vector3.Lerp(_shakeOffset, Vector3.zero, shakeDecay * Time.deltaTime);
            _armIkTarget.position += _shakeOffset;
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            switch (type)
            {
                case PlayerControllerType.Car:
                    switchToCar.Play();
                    break;
                case PlayerControllerType.Robot:
                    switchToRobot.Play();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        private void OnAttack1()
        {
            shootGun.Play();
    
            Vector3 biased = Vector3.Lerp(UnityEngine.Random.insideUnitSphere, shakeDirection.normalized, directionBias);
            _shakeOffset = Vector3.ClampMagnitude(_shakeOffset + biased * shakeAmount, maxShake);
        }
        
        public void ToggleArmIK(bool enable)
        {
            _targetWeight = enable ? 1f : 0f;
        }
    }
}