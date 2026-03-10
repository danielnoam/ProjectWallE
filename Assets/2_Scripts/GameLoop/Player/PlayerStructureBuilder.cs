using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE;
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
    [SerializeField, PrefabSelector("Assets/Prefabs/Structures")] private Structure[] structuresArray;
    
    
    [SerializeField, AutoGetSelf, HideInInspector] private PlayerManager playerManager;
    [SerializeField, AutoGetScene, HideInInspector] private StructureBuildMenu buildMenu;
    [SerializeField, AutoGetScene, HideInInspector] private StructureActionsMenu actionsMenu;

    private Camera _mainCamera;
    private Ray _buildRay;
    private bool _canBuild;
    private bool _menuOpen;
    private Structure _targetedStructure;

    public static event Action<Structure[]> BuildMenuRequested;
    public static event Action<List<StructureAction>> ActionsMenuRequested;
    public static event Action MenuCloseRequested;
    

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
        buildMenu.OnItemSelected += TryBuildStructure;
        actionsMenu.OnItemSelected += OnStructureActionSelected;
        playerManager.OnDeath += OnDeath;
        PlayerManager.OnControllerChanged += OnControllerChanged;
    }
    

    private void OnDisable()
    {
        buildMenu.OnItemSelected -= TryBuildStructure;
        actionsMenu.OnItemSelected -= OnStructureActionSelected;
        playerManager.OnDeath -= OnDeath;
        PlayerManager.OnControllerChanged -= OnControllerChanged;
    }

    private void Update()
    {
        if (!playerManager.canBuild) return;

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
    
    private void OnDeath(IDamageable damageable)
    {
        CloseMenus();
    }
    
    private void OnControllerChanged(PlayerControllerType controllerType)
    {
        CloseMenus();
    }
    
    private void OnStructureActionSelected(StructureAction action)
    {
        if (!action.isAvailable) return;
        action.onSelected?.Invoke();
    }

    private void OpenContextMenu()
    {
        _menuOpen = true;

        if (_targetedStructure)
        {
            ActionsMenuRequested?.Invoke(BuildStructureActions(_targetedStructure));
        }
        else
        {
            BuildMenuRequested?.Invoke(structuresArray);
        }
    }

    private void CloseMenus()
    {
        MenuCloseRequested?.Invoke();
        BuildPrompt.Instance?.Hide();
        _menuOpen = false;
    }

    private void CastBuildRay()
    {
        _buildRay = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(_buildRay, out RaycastHit structureHit, buildRange, structureLayerMask) && structureHit.collider.TryGetComponent(out Structure structure))
        {
            _targetedStructure = structure;
            _canBuild = false;
            UpdateBuildPrompt(_menuOpen ? structure.TopPoint : null);
        }
        else if (Physics.Raycast(_buildRay, buildRange, blockBuildLayerMask))
        {
            _targetedStructure = null;
            _canBuild = false;
            UpdateBuildPrompt(null);
        }
        else if (Physics.Raycast(_buildRay, out RaycastHit groundHit, buildRange, buildableLayerMask))
        {
            _targetedStructure = null;
            _canBuild = true;
            UpdateBuildPrompt(_menuOpen ? groundHit.point : null);
        }
        else
        {
            _targetedStructure = null;
            _canBuild = false;
            UpdateBuildPrompt(null);
        }
    }

    private void UpdateBuildPrompt(Vector3? position)
    {
        if (position.HasValue)
        {
            BuildPrompt.Instance?.Show(position.Value);
        }
        else
        {
            BuildPrompt.Instance?.Hide();
        }
    }

    private void TryBuildStructure(Structure structure)
    {
        if (!_canBuild) return;
        if (!Physics.Raycast(_buildRay, out RaycastHit hit, buildRange, buildableLayerMask)) return;
        if (!ResourceManager.Instance.TrySpendResources(structure.BuildCost)) return;

        StructureManager.Instance?.DeployPod(structure, hit.point, transform.forward);
    }
    

    private List<StructureAction> BuildStructureActions(Structure structure)
    {
        bool canUpgrade = structure.CanUpgrade();
        bool canFix = structure.CurrentHealth < structure.MaxHealth;

        string upgradeLabel = canUpgrade
            ? $"Upgrade {structure.currentUpgradeLevel} -> {structure.currentUpgradeLevel + 1}\n{structure.UpgradeCost}"
            : $"At Max Level\n {structure.currentUpgradeLevel}/{structure.currentUpgradeLevel}";

        string fixLabel = canFix
            ? $"Fix {(int)structure.FixCost}\n{(int)structure.CurrentHealth}/{(int)structure.MaxHealth}"
            : $"At Full Health\n{(int)structure.CurrentHealth}/{(int)structure.MaxHealth}";

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