using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.InputSystem;


namespace ProjectWallE.GameLoop.Player
{
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private Vector3 aimOffset = Vector3.zero;
        [SerializeField] private Transform firePoint;
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManager playerManager;
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManagerInput input;
        
        [Header("Basic Attack")]
        [SerializeField] private float basicFireRate = 0.1f;
        [SerializeField, SOSelector] private SOProjectileData basicProjectileData;
        
        [Header("Special Attack")]
        [SerializeField] private float aoeFireRate = 1.5f;
        [SerializeField, SOSelector] private SOProjectileData specialProjectileData;

        private bool _inMenu;
        private float _basicCooldown;
        private float _specialCooldown;
        private Camera _mainCamera;


        public event Action OnAttack1;
        public event Action OnAttack2;

        public event Action<float, float> OnBasicCooldownUpdated;
        public event Action<float, float> OnSpecialCooldownUpdated;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            playerManager.StructureBuilder.ActionsMenuRequested += OnActionsMenuRequested;
            playerManager.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
            playerManager.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
        }

        private void OnDisable()
        {
            playerManager.StructureBuilder.ActionsMenuRequested -= OnActionsMenuRequested;
            playerManager.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
            playerManager.StructureBuilder.MenuCloseRequested -= OnMenuCloseRequested;
        }
        
        private void Update()
        {
            UpdateCooldowns();
            CheckInput();
        }
        
        private void OnMenuCloseRequested()
        {
            _inMenu = false;
        }

        private void OnBuildMenuRequested(Structure[] obj)
        {
            _inMenu = true;
        }

        private void OnActionsMenuRequested(Structure obj)
        {
            _inMenu = true;
        }

        private void CheckInput()
        {
            if (playerManager)
            {
                if (playerManager.CanShoot && !_inMenu)
                {
                    if (input.Attack1Held && _basicCooldown <= 0)
                    {
                        ShootBasic();
                    }
                
                    if (input.Attack2Held && _specialCooldown <= 0)
                    {
                        ShootSpecial();
                    }
                }
            }
            else
            {
                if (Mouse.current.leftButton.isPressed && _basicCooldown <= 0)
                {
                    ShootBasic();
                }
                if (Mouse.current.rightButton.isPressed && _specialCooldown <= 0)
                {
                    ShootSpecial();
                }
            }
        }

        private void UpdateCooldowns()
        {
            if (_basicCooldown > 0)
            {
                _basicCooldown -= Time.deltaTime;
                if (_basicCooldown <= 0)
                {
                    _basicCooldown = 0;
                    OnBasicCooldownUpdated?.Invoke(0, basicFireRate);
                }
                else
                {
                    OnBasicCooldownUpdated?.Invoke(_basicCooldown, basicFireRate);
                }
            }

            if (_specialCooldown > 0)
            {
                _specialCooldown -= Time.deltaTime;
                if (_specialCooldown <= 0)
                {
                    _specialCooldown = 0;
                    OnSpecialCooldownUpdated?.Invoke(0, aoeFireRate);
                }
                else
                {
                    OnSpecialCooldownUpdated?.Invoke(_specialCooldown, aoeFireRate);
                }
            }
        }
        private void ShootBasic()
        {
            if (!basicProjectileData) return;
            
            _basicCooldown = basicFireRate;
            Vector3 position = firePoint ? firePoint.position : transform.position;
            Vector3 direction = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).direction;
            basicProjectileData?.Spawn(position, direction.Add(aimOffset), default, playerManager);
            OnAttack1?.Invoke();
            OnBasicCooldownUpdated?.Invoke(_basicCooldown, basicFireRate);
        }

        private void ShootSpecial()
        {
            if (!specialProjectileData) return;
            
            _specialCooldown = aoeFireRate;
            Vector3 position = firePoint ? firePoint.position : transform.position;
            Vector3 direction = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).direction;
            specialProjectileData?.Spawn(position, direction.Add(aimOffset), default, playerManager);
            OnAttack2?.Invoke();
            OnSpecialCooldownUpdated?.Invoke(_specialCooldown, aoeFireRate);
        }
    }
}