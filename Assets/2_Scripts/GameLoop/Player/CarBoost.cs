using UnityEngine;

namespace ProjectWallE
{
    public class CarBoost : MonoBehaviour
    {
        private float _currentFuel = FuelCapacity;

        [Header("Boost Settings")]
        [SerializeField] private float boostAccel = 1.5f;
        [SerializeField] private float boostSpeedFactor = 1.25f;

        [Header("Fuel Settings")]
        [SerializeField] private float fuelConsumption = 20f;
        [SerializeField] private float rechargeCooldown = 1.5f;
        [SerializeField] private float rechargeRate = 25f;
        [SerializeField] private float minFuelToStartBoost = 10f;

        private const float FuelCapacity = 100f;

        private float _lastBoostTime;
        private float _disableTime;
        private bool _isBoosting;

        private CarInput _carInput;

        private void Awake()
        {
            _carInput = GetComponent<CarInput>();
            _currentFuel = Mathf.Clamp(_currentFuel, 0f, FuelCapacity);
        }

        private void OnDisable()
        {
            _disableTime = Time.time;
            _isBoosting = false;
        }

        private void OnEnable()
        {
            float enableTime = Time.time;

            float rechargeStartTime = _lastBoostTime + rechargeCooldown;

            float effectiveRechargeStart = Mathf.Max(_disableTime, rechargeStartTime);
            float rechargeDuration = enableTime - effectiveRechargeStart;

            if (rechargeDuration > 0f)
            {
                _currentFuel += rechargeDuration * rechargeRate;
                _currentFuel = Mathf.Clamp(_currentFuel, 0f, FuelCapacity);
            }
        }

        private void Update()
        {
            HandleFuel();
            _currentFuel = Mathf.Clamp(_currentFuel, 0f, FuelCapacity);
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
            if (!_carInput.BoostHeld)
            {
                _isBoosting = false;
                return;
            }

            if (_isBoosting)
            {
                if (_currentFuel <= 0f)
                    _isBoosting = false;

                return;
            }

            if (_currentFuel >= minFuelToStartBoost)
            {
                _isBoosting = true;
            }
        }

        private void UseFuel()
        {
            _currentFuel -= fuelConsumption * Time.deltaTime;
            _lastBoostTime = Time.time;
        }

        private void RechargeFuel()
        {
            if (_currentFuel >= FuelCapacity)
                return;

            _currentFuel += rechargeRate * Time.deltaTime;
        }
    }
}