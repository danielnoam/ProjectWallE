using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    public class ResourcesDisplay : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private CountIcon resourcesIcon;
        [SerializeField] private RectTransform resourcesHolder;
        [SerializeField, Min(0f)] private float resourcesNormalWidth = 200f;
        [SerializeField, Min(0f)] private float resourcesGoalWidth = 300f;
        [SerializeField, Min(0f)] private float resourcesWidthDuration = 0.25f;
        [SerializeField] private Ease resourcesWidthEase = Ease.InOutSine;

        [Header("Structures")]
        [SerializeField] private RectTransform structuresHolder;
        [SerializeField, Min(0f)] private float structuresShowDuration = 0.25f;
        [SerializeField, Min(0f)] private float structuresHideDuration = 0.15f;
        [SerializeField] private Ease structuresAnimationEase = Ease.InOutSine;
        [SerializeField] private CountIcon basesIcon;
        [SerializeField] private CountIcon turretsIcon;
        [SerializeField] private CountIcon generatorsIcon;
        [SerializeField] private CountIcon rampsIcon;

        [Header("Upgrades")]
        [SerializeField] private RectTransform upgradesHolder;
        [SerializeField, Min(0f)] private float upgradesShowDuration = 0.25f;
        [SerializeField, Min(0f)] private float upgradesHideDuration = 0.15f;
        [SerializeField] private Ease upgradesAnimationEase = Ease.InOutSine;
        [SerializeField] private UpgradeIcon damageUpgradeIcon;
        [SerializeField] private UpgradeIcon healthUpgradeIcon;
        [SerializeField] private UpgradeIcon fuelUpgradeIcon;

        [Header("References")]
        [SerializeField, AutoGetScene] private PlayerManager player;

        private Tween _structuresTween;
        private float _structuresContentHeight;
        private Tween _upgradesTween;
        private float _upgradesContentHeight;
        private Tween _resourcesWidthTween;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            ResetIcons();

            if (resourcesHolder)
                resourcesHolder.sizeDelta = new Vector2(resourcesNormalWidth, resourcesHolder.sizeDelta.y);

            _structuresContentHeight = MeasureCollapsibleHolder(structuresHolder);
            _upgradesContentHeight = MeasureCollapsibleHolder(upgradesHolder);
        }

        private static float MeasureCollapsibleHolder(RectTransform holder)
        {
            if (!holder) return 0f;

            var csf = holder.GetComponent<ContentSizeFitter>();
            if (csf) csf.enabled = false;
            holder.sizeDelta = new Vector2(holder.sizeDelta.x, 0f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(holder);
            float height = LayoutUtility.GetPreferredHeight(holder);
            holder.sizeDelta = new Vector2(holder.sizeDelta.x, 0f);
            return height;
        }

        private void OnEnable()
        {
            ResourceManager.OnResourcesChanged += UpdateResourcesDisplay;
            StructureManager.OnStructureCountChanged += UpdateStructuresText;
            LevelManager.OnLevelInitializing += ResetIcons;
            LevelManager.OnResourceGoalSet += ShowResourceGoal;
            LevelManager.OnResourceGoalCleared += HideResourceGoal;

            if (player)
            {
                player.SupportCaller.SupportMenuRequested += OnSupportMenuRequested;
                player.SupportCaller.MenuCloseRequested += OnSupportMenuCloseRequested;
                player.StructureBuilder.ActionsMenuRequested += OnActionsMenuRequested;
                player.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;

                if (player.Upgrades)
                {
                    player.Upgrades.OnDamageMultiplierChanged += OnDamageUpgraded;
                    player.Upgrades.OnMaxHealthMultiplierChanged += OnHealthUpgraded;
                    player.Upgrades.OnMaxFuelMultiplierChanged += OnFuelUpgraded;
                }
            }
        }

        private void OnDisable()
        {
            ResourceManager.OnResourcesChanged -= UpdateResourcesDisplay;
            StructureManager.OnStructureCountChanged -= UpdateStructuresText;
            LevelManager.OnLevelInitializing -= ResetIcons;
            LevelManager.OnResourceGoalSet -= ShowResourceGoal;
            LevelManager.OnResourceGoalCleared -= HideResourceGoal;

            if (player)
            {
                player.SupportCaller.SupportMenuRequested -= OnSupportMenuRequested;
                player.SupportCaller.MenuCloseRequested -= OnSupportMenuCloseRequested;
                player.StructureBuilder.ActionsMenuRequested -= OnActionsMenuRequested;
                player.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested -= OnMenuCloseRequested;

                if (player.Upgrades)
                {
                    player.Upgrades.OnDamageMultiplierChanged -= OnDamageUpgraded;
                    player.Upgrades.OnMaxHealthMultiplierChanged -= OnHealthUpgraded;
                    player.Upgrades.OnMaxFuelMultiplierChanged -= OnFuelUpgraded;
                }
            }
        }

        private void OnDestroy()
        {
            if (_structuresTween.isAlive) _structuresTween.Stop();
            if (_upgradesTween.isAlive) _upgradesTween.Stop();
            if (_resourcesWidthTween.isAlive) _resourcesWidthTween.Stop();
        }

        private void ShowResourceGoal(int target)
        {
            resourcesIcon?.SetGoal(target);
            AnimateResourcesWidth(resourcesGoalWidth);
        }

        private void HideResourceGoal()
        {
            resourcesIcon?.ClearGoal();
            AnimateResourcesWidth(resourcesNormalWidth);
        }

        private void AnimateResourcesWidth(float target)
        {
            if (!resourcesHolder) return;
            if (_resourcesWidthTween.isAlive) _resourcesWidthTween.Stop();

            float start = resourcesHolder.sizeDelta.x;
            if (Mathf.Approximately(start, target))
            {
                resourcesHolder.sizeDelta = new Vector2(target, resourcesHolder.sizeDelta.y);
                return;
            }

            if (resourcesWidthDuration > 0)
            {
                _resourcesWidthTween = Tween.Custom(start, target, resourcesWidthDuration,
                    v => resourcesHolder.sizeDelta = new Vector2(v, resourcesHolder.sizeDelta.y),
                    ease: resourcesWidthEase, useUnscaledTime: true);
                return;
            }

            resourcesHolder.sizeDelta = new Vector2(target, resourcesHolder.sizeDelta.y);
        }

        private void OnActionsMenuRequested(Structure structure) => ShowMenuSections();

        private void OnBuildMenuRequested(Structure[] structures, bool canBuild) => ShowMenuSections();

        private void OnSupportMenuRequested(SOSupportActionData[] obj) => ShowMenuSections();

        private void OnSupportMenuCloseRequested() => HideMenuSections();

        private void OnMenuCloseRequested() => HideMenuSections();

        private void ShowMenuSections()
        {
            ShowStructuresHolder();
            ShowUpgradesHolder();
        }

        private void HideMenuSections()
        {
            HideStructuresHolder();
            HideUpgradesHolder();
        }

        private void ShowStructuresHolder()
        {
            if (!structuresHolder) return;
            if (_structuresTween.isAlive) _structuresTween.Stop();
            _structuresTween = AnimateHolderHeight(structuresHolder, _structuresContentHeight, structuresShowDuration, structuresAnimationEase);
        }

        private void HideStructuresHolder()
        {
            if (!structuresHolder) return;
            if (_structuresTween.isAlive) _structuresTween.Stop();
            _structuresTween = AnimateHolderHeight(structuresHolder, 0f, structuresHideDuration, structuresAnimationEase);
        }

        private void ShowUpgradesHolder()
        {
            if (!upgradesHolder) return;
            if (_upgradesTween.isAlive) _upgradesTween.Stop();
            _upgradesTween = AnimateHolderHeight(upgradesHolder, _upgradesContentHeight, upgradesShowDuration, upgradesAnimationEase);
        }

        private void HideUpgradesHolder()
        {
            if (!upgradesHolder) return;
            if (_upgradesTween.isAlive) _upgradesTween.Stop();
            _upgradesTween = AnimateHolderHeight(upgradesHolder, 0f, upgradesHideDuration, upgradesAnimationEase);
        }

        private static Tween AnimateHolderHeight(RectTransform holder, float target, float duration, Ease ease)
        {
            float start = holder.sizeDelta.y;
            if (Mathf.Approximately(start, target)) return default;

            if (duration > 0)
                return Tween.Custom(start, target, duration,
                    v => holder.sizeDelta = new Vector2(holder.sizeDelta.x, v),
                    ease: ease, useUnscaledTime: true);

            holder.sizeDelta = new Vector2(holder.sizeDelta.x, target);
            return default;
        }

        private void UpdateStructuresText(StructuresData data)
        {
            basesIcon?.SetCount(data.BasesCount);
            turretsIcon?.SetCount(data.TurretsCount);
            generatorsIcon?.SetCount(data.GeneratorsCount);
            rampsIcon?.SetCount(data.RampsCount);
        }

        private void UpdateResourcesDisplay(int currentResources)
        {
            resourcesIcon?.SetCount(currentResources);
        }

        private void OnDamageUpgraded(float amount)
        {
            if (player.Upgrades) damageUpgradeIcon?.SetValue(player.Upgrades.DamageMultiplier);
        }

        private void OnHealthUpgraded(float amount)
        {
            if (player.Upgrades) healthUpgradeIcon?.SetValue(player.Upgrades.MaxHealthMultiplier);
        }

        private void OnFuelUpgraded(float amount)
        {
            if (player.Upgrades) fuelUpgradeIcon?.SetValue(player.Upgrades.MaxFuelMultiplier);
        }

        private void ResetIcons()
        {
            HideResourceGoal();
            resourcesIcon?.SetCountImmediate(0);
            basesIcon?.SetCountImmediate(0);
            turretsIcon?.SetCountImmediate(0);
            generatorsIcon?.SetCountImmediate(0);
            rampsIcon?.SetCountImmediate(0);
            damageUpgradeIcon?.SetValueImmediate(1f);
            healthUpgradeIcon?.SetValueImmediate(1f);
            fuelUpgradeIcon?.SetValueImmediate(1f);
        }
    }
}