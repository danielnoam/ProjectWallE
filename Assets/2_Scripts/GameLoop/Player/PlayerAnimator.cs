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

        [Header("Arm IK")]
        [SerializeField] private float shoulderOffset = 0.7f;
        [SerializeField] private float targetDistance = 1.5f;
        [SerializeField] private float transitionSpeed = 3f;

        [Header("References")]
        [SerializeField] private Rig rig;
        [SerializeField] private MultiAimConstraint headAim;
        [SerializeField] private TwoBoneIKConstraint armIK;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform shoulder;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;

        private Transform _ikTarget;
        private Camera _cam;
        private float _targetWeight = 1f;

        private void OnValidate() => AutoGetSystem.Process(this);

        private void Awake()
        {
            _cam = Camera.main;
            _ikTarget = new GameObject("ArmIK_Target").transform;
            headAim.data.sourceObjects = new WeightedTransformArray { new WeightedTransform(_ikTarget, 1f) };
            armIK.data.target = _ikTarget;
        }

        private void OnEnable()
        {
            if (player)
            {
                player.Shooter.OnAttack1 += ShooterOnOnAttack1;
                player.Shooter.OnAttack2 += ShooterOnOnAttack1;
                player.OnControllerChanged += OnControllerChanged;
            }
        }

        private void OnDisable()
        {
            if (player)
            {
                player.Shooter.OnAttack1 -= ShooterOnOnAttack1;
                player.Shooter.OnAttack2 -= ShooterOnOnAttack1;
                player.OnControllerChanged -= OnControllerChanged;
            }
        }

        private void LateUpdate()
        {
            UpdateIKTarget();
        }

        private void UpdateIKTarget()
        {
            if (!_cam) return;
            
            rig.weight = Mathf.MoveTowards(rig.weight, _targetWeight, transitionSpeed * Time.deltaTime);
            
            if (rig.weight > 0.01f)
            {
                float dot = Vector3.Dot(torso.forward, _cam.transform.forward);
                float dynamicOffset = Mathf.Lerp(shoulderOffset * 3f, shoulderOffset, (dot + 1f) * 0.5f);
                _ikTarget.position = shoulder.position + _cam.transform.forward * targetDistance + torso.right * dynamicOffset;
                _ikTarget.rotation = _cam.transform.rotation * Quaternion.Euler(0f, -90f, 0f);
            }
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
        
        private void ShooterOnOnAttack1()
        {
            shootGun.Play();
        }
        
        public void ToggleArmIK(bool enable)
        {
            _targetWeight = enable ? 1f : 0f;
        }
    }
}