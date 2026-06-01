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

        [Header("Structures")]
        [SerializeField] private RectTransform structuresHolder;
        [SerializeField, Min(0f)] private float structuresShowDuration = 0.25f;
        [SerializeField, Min(0f)] private float structuresHideDuration = 0.15f;
        [SerializeField] private Ease structuresAnimationEase = Ease.InOutSine;
        [SerializeField] private CountIcon basesIcon;
        [SerializeField] private CountIcon turretsIcon;
        [SerializeField] private CountIcon generatorsIcon;
        [SerializeField] private CountIcon rampsIcon;
        
        [Header("References")]
        [SerializeField, AutoGetScene] private PlayerManager player;

        private Tween _structuresTween;
        private float _structuresContentHeight;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            ResetIcons();

            if (structuresHolder)
            {
                var csf = structuresHolder.GetComponent<ContentSizeFitter>();
                if (csf) csf.enabled = false;
                structuresHolder.sizeDelta = new Vector2(structuresHolder.sizeDelta.x, 0f);

                LayoutRebuilder.ForceRebuildLayoutImmediate(structuresHolder);
                _structuresContentHeight = LayoutUtility.GetPreferredHeight(structuresHolder);
                structuresHolder.sizeDelta = new Vector2(structuresHolder.sizeDelta.x, 0f);
            }
        }

        private void OnEnable()
        {
            ResourceManager.OnResourcesChanged += UpdateResourcesDisplay;
            StructureManager.OnStructureCountChanged += UpdateStructuresText;
            LevelManager.OnLevelInitializing += ResetIcons;

            if (player)
            {
                player.SupportCaller.SupportMenuRequested += OnSupportMenuRequested;
                player.SupportCaller.MenuCloseRequested += OnSupportMenuCloseRequested;
                player.StructureBuilder.ActionsMenuRequested += OnActionsMenuRequested;
                player.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
            }
        }

        private void OnDisable()
        {
            ResourceManager.OnResourcesChanged -= UpdateResourcesDisplay;
            StructureManager.OnStructureCountChanged -= UpdateStructuresText;
            LevelManager.OnLevelInitializing -= ResetIcons;

            if (player)
            {
                player.SupportCaller.SupportMenuRequested -= OnSupportMenuRequested;
                player.SupportCaller.MenuCloseRequested -= OnSupportMenuCloseRequested;
                player.StructureBuilder.ActionsMenuRequested -= OnActionsMenuRequested;
                player.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested -= OnMenuCloseRequested;
            }
        }

        private void OnDestroy()
        {
            if (_structuresTween.isAlive) _structuresTween.Stop();
        }

        private void OnActionsMenuRequested(Structure structure) => ShowStructuresHolder();

        private void OnBuildMenuRequested(Structure[] structures, bool canBuild) => ShowStructuresHolder();

        private void OnSupportMenuRequested(SOSupportActionData[] obj) => ShowStructuresHolder();

        private void OnSupportMenuCloseRequested() => HideStructuresHolder();

        private void OnMenuCloseRequested() => HideStructuresHolder();

        private void ShowStructuresHolder()
        {
            if (!structuresHolder) return;
            if (_structuresTween.isAlive) _structuresTween.Stop();
            _structuresTween = AnimateStructuresHeight(_structuresContentHeight, structuresShowDuration);
        }

        private void HideStructuresHolder()
        {
            if (!structuresHolder) return;
            if (_structuresTween.isAlive) _structuresTween.Stop();
            _structuresTween = AnimateStructuresHeight(0f, structuresHideDuration);
        }

        private Tween AnimateStructuresHeight(float target, float duration)
        {
            float start = structuresHolder.sizeDelta.y;
            if (Mathf.Approximately(start, target)) return default;

            if (duration > 0)
                return Tween.Custom(start, target, duration,
                    v => structuresHolder.sizeDelta = new Vector2(structuresHolder.sizeDelta.x, v),
                    ease: structuresAnimationEase, useUnscaledTime: true);

            structuresHolder.sizeDelta = new Vector2(structuresHolder.sizeDelta.x, target);
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

        private void ResetIcons()
        {
            resourcesIcon?.SetCountImmediate(0);
            basesIcon?.SetCountImmediate(0);
            turretsIcon?.SetCountImmediate(0);
            generatorsIcon?.SetCountImmediate(0);
            rampsIcon?.SetCountImmediate(0);
        }
    }
}