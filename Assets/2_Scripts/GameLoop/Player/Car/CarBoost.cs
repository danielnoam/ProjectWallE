using System;
using UnityEngine;

namespace ProjectWallE
{
    public class CarBoost : MonoBehaviour
    {
        [Header("Boost Settings")]
        [SerializeField] private float boostAccel = 1.5f;
        [SerializeField] private float boostSpeedFactor = 1.25f;

        [Header("Fuel Settings")]
        [SerializeField] private float fuelConsumption = 20f;
        [SerializeField] private float rechargeCooldown = 1.5f;
        [SerializeField] private float rechargeRate = 25f;
        [SerializeField] private float minFuelToStartBoost = 10f;

        private const float FuelCapacity = 100f;
        
        private float _currentFuel = FuelCapacity;
        
        private float _lastBoostTime;
        private float _disableTime;
        private bool _isBoosting;

        private CarInput _carInput;
        
        public event Action OnBoostStart;
        public event Action OnBoostEnd; 
        public event Action<float, float> OnFuelChange;

        private void Awake()
        {
            _carInput = GetComponent<CarInput>();
            _currentFuel = Mathf.Clamp(_currentFuel, 0f, FuelCapacity);
        }

        private void OnDisable()
        {
            bool wasBoosting = _isBoosting;
            _disableTime = Time.time;
            _isBoosting = false;

            if (!wasBoosting) return;
            OnBoostEnd?.Invoke();
        }

        private void OnEnable()
        {
            float enableTime = Time.time;

            float rechargeStartTime = _lastBoostTime + rechargeCooldown;

            float effectiveRechargeStart = Mathf.Max(_disableTime, rechargeStartTime);
            float rechargeDuration = enableTime - effectiveRechargeStart;

            if (rechargeDuration > 0f)
            {
                AddFuel(rechargeDuration * rechargeRate);
            }
        }

        private void Update()
        {
            HandleFuel();
        }

        public bool CanBoost(out float boostAccel, out float boostSpeedFactor)
        {
            boostAccel = this.boostAccel;
            boostSpeedFactor = this.boostSpeedFactor;

            return _isBoosting;
        }

        private void HandleFuel()
        {
            HandleBoostState();

            if (_isBoosting)
            {
                UseFuel();
                return;
            }

            if (Time.time >= _lastBoostTime + rechargeCooldown)
            {
                RechargeFuel();
            }
        }

        private void HandleBoostState()
        {
            bool wasBoosting = _isBoosting;
            
            if (!_carInput.BoostHeld)
            {
                _isBoosting = false;
            }
            else if (_isBoosting && _currentFuel <= 0f)
            {
                _isBoosting = false;
            }
            else if (_currentFuel >= minFuelToStartBoost)
            {
                _isBoosting = true;
            }
            
            //events
            if (_isBoosting && !wasBoosting)
            {
                OnBoostStart?.Invoke();
            }
            else if (!_isBoosting && wasBoosting)
            {
                OnBoostEnd?.Invoke();
            }
        }

        private void UseFuel()
        {
            AddFuel(-fuelConsumption * Time.deltaTime);
            _lastBoostTime = Time.time;
        }

        private void RechargeFuel()
        {
            if (_currentFuel >= FuelCapacity)
                return;

            AddFuel(rechargeRate * Time.deltaTime);
        }

        private void AddFuel(float addedFuel)
        {
            _currentFuel = Mathf.Clamp(_currentFuel + addedFuel, 0, FuelCapacity);
            OnFuelChange?.Invoke(_currentFuel, FuelCapacity);
        }
    }
}