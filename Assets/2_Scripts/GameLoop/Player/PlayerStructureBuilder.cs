using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.InputSystem;

[SelectionBase]
public class PlayerStructureBuilder : MonoBehaviour
{
    [Header("Build Settings")]
    [SerializeField] private float buildRange = 100f;
    [SerializeField] private LayerMask buildableLayerMask;
    [SerializeField] private LayerMask blockBuildLayerMask;
    [SerializeField] private LayerMask structureLayerMask;

    [Header("References")]
    [SerializeField, AutoGetSelf] private LineRenderer lineRenderer;
    [SerializeField, AutoGetScene] private Camera mainCamera;
    [SerializeField, AutoGetScene] private StructureBuildMenu buildMenu;
    [SerializeField, AutoGetScene] private StructureActionsMenu actionsMenu;
    [SerializeField, AutoGetSelf] private FreeFormCameraController cameraController;
    [SerializeField, PrefabSelector("Assets/Prefabs/Structures")] private Structure[] structuresArray;

    private Ray _buildRay;
    private bool _canBuild;
    private bool _menuOpen;
    private Structure _targetedStructure;

    private void Awake()
    {
        buildMenu.SetupMenu(structuresArray, ConfigureBuildElement);
        buildMenu.OnItemSelected += TryBuildStructure;
        actionsMenu.OnItemSelected += OnStructureActionSelected;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDestroy()
    {
        buildMenu.OnItemSelected -= TryBuildStructure;
        actionsMenu.OnItemSelected -= OnStructureActionSelected;
    }

    private void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            OpenContextMenu();
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            CloseMenus();
        }

        CastBuildRay();
    }

    private void OpenContextMenu()
    {
        cameraController.enabled = false;
        _menuOpen = true;

        if (_targetedStructure)
        {
            actionsMenu.SetupMenu(BuildStructureActions(_targetedStructure), ConfigureActionElement);
            actionsMenu.OpenMenu();
        }
        else
        {
            buildMenu.SetupMenu(structuresArray, ConfigureBuildElement);
            buildMenu.OpenMenu();
        }
    }

    private void CloseMenus()
    {
        cameraController.enabled = true;
        buildMenu.CloseMenu();
        actionsMenu.CloseMenu();
        BuildPrompt.Instance?.Hide();
        _menuOpen = false;
    }

    private void CastBuildRay()
    {
        lineRenderer.SetPosition(0, transform.position.RemoveY(0.5f));
        _buildRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(_buildRay, out RaycastHit structureHit, buildRange, structureLayerMask))
        {
            if (structureHit.collider.TryGetComponent(out Structure structure))
            {
                _targetedStructure = structure;
                _canBuild = false;
                lineRenderer.SetPosition(1, structureHit.point);
                if (_menuOpen && _targetedStructure) BuildPrompt.Instance?.Show(_targetedStructure.TopPoint);
            }

        }
        else if (Physics.Raycast(_buildRay, buildRange, blockBuildLayerMask))
        {
            _targetedStructure = null;
            _canBuild = false;
            lineRenderer.SetPosition(1, transform.position + _buildRay.direction * buildRange);
            BuildPrompt.Instance?.Hide();
        }
        else if (Physics.Raycast(_buildRay, out RaycastHit groundHit, buildRange, buildableLayerMask))
        {
            _targetedStructure = null;
            _canBuild = true;
            lineRenderer.SetPosition(1, groundHit.point);
            if (_menuOpen) BuildPrompt.Instance?.Show(groundHit.point);
        }
        else
        {
            _targetedStructure = null;
            _canBuild = false;
            lineRenderer.SetPosition(1, transform.position + _buildRay.direction * buildRange);
            BuildPrompt.Instance?.Hide();
        }
    }

    private void ConfigureBuildElement(RadialMenuElement element, Structure structure)
    {
        bool canAfford = ResourceManager.Instance.CanAfford(structure.BuildCost);
        element.elementInfo = $"{structure.Label}\nCost: {structure.BuildCost}";
        element.SetDisabled(!canAfford);

        if (structure.Icon)
        {
            element.iconImage.sprite = structure.Icon;
        }
        else
        {
            element.iconImage.gameObject.SetActive(false);
        }
    }

    private void ConfigureActionElement(RadialMenuElement element, StructureAction action)
    {
        element.elementInfo = action.label;
        element.SetDisabled(!action.isAvailable);

        if (action.icon)
        {
            element.iconImage.sprite = action.icon;
        }
        else
        {
            element.iconImage.gameObject.SetActive(false);
        }
    }

    private void TryBuildStructure(Structure structure)
    {
        if (!_canBuild) return;
        if (!Physics.Raycast(_buildRay, out RaycastHit hit, buildRange, buildableLayerMask)) return;
        if (!ResourceManager.Instance.TrySpendResources(structure.BuildCost)) return;

        StructureManager.Instance?.DeployPod(structure, hit.point, transform.forward);
    }

    private void OnStructureActionSelected(StructureAction action)
    {
        if (action.isAvailable)
        {
            action.onSelected?.Invoke();
        }
    }

    private List<StructureAction> BuildStructureActions(Structure structure)
    {
        bool canUpgrade = structure.CanUpgrade();
        bool canFix = structure.CurrentHealth < structure.MaxHealth;

        string upgradeLabel = canUpgrade 
            ? $"Upgrade {structure.currentUpgradeLevel} -> {structure.currentUpgradeLevel + 1}\n{structure.UpgradeCost}" 
            : $"At Max Level\n {structure.currentUpgradeLevel}/{structure.currentUpgradeLevel}";
    
        string fixLabel = canFix 
            ? $"Fix\n{(int)structure.FixCost}\n{structure.CurrentHealth}/{structure.MaxHealth}" 
            : $"At Full Health\n{structure.CurrentHealth}/{structure.MaxHealth}";

        return new List<StructureAction>
        {
            new StructureAction
            {
                label = upgradeLabel,
                isAvailable = canUpgrade && ResourceManager.Instance.CanAfford(structure.UpgradeCost),
                onSelected = structure.Upgrade
            },
            new StructureAction
            {
                label = fixLabel,
                isAvailable = canFix && ResourceManager.Instance.CanAfford((int)structure.FixCost),
                onSelected = structure.Fix
            }
        };
    }
}