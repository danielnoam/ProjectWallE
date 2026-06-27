using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using ProjectWallE.GameLoop;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class PlayerStatusDisplay : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Bars")]
        [SerializeField] private FillBar fuelBar;
        [SerializeField] private FillBar healthBar;

        [Header("Actions")]
        [SerializeField] private ActionIcon switchIcon;
        [SerializeField] private ActionIcon buildIcon;
        [SerializeField] private ActionIcon supportIcon;
        [SerializeField] private ActionIcon basicAttackIcon;
        [SerializeField] private ActionIcon specialAttackIcon;
        [SerializeField] private ActionIcon jumpIcon;
        [SerializeField] private ActionIcon boostIcon;
        [SerializeField] private ActionIcon brakeIcon;

        [Header("References")]
        [SerializeField, AutoGetScene] private PlayerManager player;

        private ControllerType _currentControllerType;
        private Tween _fadeTween;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
            if (!player) return;

            player.OnDeath += OnPlayerDeath;
            player.OnSpawn += OnPlayerSpawn;
            player.OnControllerChanged += OnControllerChanged;
            player.OnFeaturesChanged += OnFeaturesChanged;
            player.OnSwitchCooldownUpdated += OnSwitchCooldownUpdated;
            player.OnHealthChanged += UpdateHealthBar;
            player.CarController.OnBrakeStarted += OnBrakeStarted;
            player.CarController.CarBoost.OnFuelChange += UpdateFuelBar;
            player.CarController.CarBoost.OnBoostStart += OnBoostStart;
            if (player.Upgrades)
            {
                player.Upgrades.OnMaxHealthMultiplierChanged += OnHealthUpgraded;
                player.Upgrades.OnMaxFuelMultiplierChanged += OnFuelUpgraded;
            }
            player.RobotController.OnJumped += OnJumped;
            player.Shooter.OnBasicCooldownUpdated += OnBasicCooldownUpdated;
            player.Shooter.OnSpecialCooldownUpdated += OnSpecialCooldownUpdated;
            player.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
            player.StructureBuilder.ActionsMenuRequested += OnActionsMenuRequested;
            player.SupportCaller.SupportMenuRequested += OnSupportMenuRequested;

            fuelBar.SetImmediate(100, 100);
            healthBar.SetImmediate(100, 100);
            OnControllerChanged(player.ControllerType);
        }

        private void OnDisable()
        {
            if (!player) return;

            player.OnDeath -= OnPlayerDeath;
            player.OnSpawn -= OnPlayerSpawn;
            player.OnControllerChanged -= OnControllerChanged;
            player.OnFeaturesChanged -= OnFeaturesChanged;
            player.OnSwitchCooldownUpdated -= OnSwitchCooldownUpdated;
            player.OnHealthChanged -= UpdateHealthBar;
            player.CarController.OnBrakeStarted -= OnBrakeStarted;
            player.CarController.CarBoost.OnFuelChange -= UpdateFuelBar;
            player.CarController.CarBoost.OnBoostStart -= OnBoostStart;
            if (player.Upgrades)
            {
                player.Upgrades.OnMaxHealthMultiplierChanged -= OnHealthUpgraded;
                player.Upgrades.OnMaxFuelMultiplierChanged -= OnFuelUpgraded;
            }
            player.RobotController.OnJumped -= OnJumped;
            player.Shooter.OnBasicCooldownUpdated -= OnBasicCooldownUpdated;
            player.Shooter.OnSpecialCooldownUpdated -= OnSpecialCooldownUpdated;
            player.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
            player.StructureBuilder.ActionsMenuRequested -= OnActionsMenuRequested;
            player.SupportCaller.SupportMenuRequested -= OnSupportMenuRequested;
        }

        private void OnPlayerSpawn()
        {
            if (!canvasGroup) return;
            if (Mathf.Approximately(canvasGroup.alpha, 1f)) return;
            _fadeTween.Stop();
            _fadeTween = Tween.Alpha(canvasGroup, 1f, fadeDuration);
        }

        private void OnPlayerDeath(IDamageable attacker)
        {
            if (!canvasGroup) return;
            if (Mathf.Approximately(canvasGroup.alpha, 0f)) return;
            _fadeTween.Stop();
            _fadeTween = Tween.Alpha(canvasGroup, 0f, fadeDuration);
        }

        private void RefreshActionIcons(ControllerType controllerType, PlayerFeature features)
        {
            bool isRobot = controllerType == ControllerType.Robot;

            jumpIcon.gameObject.SetActive(isRobot);
            boostIcon.gameObject.SetActive(!isRobot);
            brakeIcon.gameObject.SetActive(!isRobot);

            basicAttackIcon.gameObject.SetActive(isRobot && features.HasFlag(PlayerFeature.Shoot));
            specialAttackIcon.gameObject.SetActive(isRobot && features.HasFlag(PlayerFeature.Shoot));
            buildIcon.gameObject.SetActive(features.HasFlag(PlayerFeature.Build));
            supportIcon.gameObject.SetActive(features.HasFlag(PlayerFeature.AirSupport));
        }

        private void OnControllerChanged(ControllerType type)
        {
            _currentControllerType = type;
            RefreshActionIcons(_currentControllerType, player.EnabledFeatures);
        }

        private void OnFeaturesChanged(PlayerFeature features)
        {
            RefreshActionIcons(_currentControllerType, features);
        }

        private void OnSwitchCooldownUpdated(float currentCooldown, float maxCooldown)
        {
            if (switchIcon && switchIcon.isActiveAndEnabled) switchIcon.UpdateCooldown(currentCooldown, maxCooldown);
        }

        private void OnBrakeStarted()
        {
            if (brakeIcon && brakeIcon.isActiveAndEnabled) brakeIcon.PunchIcon();
        }

        private void OnBoostStart()
        {
            if (boostIcon && boostIcon.isActiveAndEnabled) boostIcon.PunchIcon();
        }

        private void OnJumped()
        {
            if (jumpIcon && jumpIcon.isActiveAndEnabled) jumpIcon.PunchIcon();
        }
        
        private void OnBuildMenuRequested(Structure[] _, bool __) 
        { 
            if (buildIcon && buildIcon.isActiveAndEnabled) buildIcon.PunchIcon(); 
        }

        private void OnActionsMenuRequested(Structure _) 
        { 
            if (buildIcon && buildIcon.isActiveAndEnabled) buildIcon.PunchIcon(); 
        }

        private void OnSupportMenuRequested(SOSupportActionData[] _) 
        { 
            if (supportIcon && supportIcon.isActiveAndEnabled) supportIcon.PunchIcon(); 
        }

        private void OnBasicCooldownUpdated(float currentCooldown, float maxCooldown)
        {
            if (basicAttackIcon && basicAttackIcon.isActiveAndEnabled) basicAttackIcon.UpdateCooldown(currentCooldown, maxCooldown);
        }

        private void OnSpecialCooldownUpdated(float currentCooldown, float maxCooldown)
        {
            if (specialAttackIcon && specialAttackIcon.isActiveAndEnabled) specialAttackIcon.UpdateCooldown(currentCooldown, maxCooldown);
        }

        private void OnHealthUpgraded(float amount)
        {
            if (healthBar) healthBar.Punch();
        }

        private void OnFuelUpgraded(float amount)
        {
            if (fuelBar) fuelBar.Punch();
        }

        private void UpdateFuelBar(float currentFuel, float maxFuel)
        {
            if (fuelBar) fuelBar.SetValue(currentFuel, maxFuel);
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthBar) healthBar.SetValue(currentHealth, maxHealth);
        }
        
        
    }
}