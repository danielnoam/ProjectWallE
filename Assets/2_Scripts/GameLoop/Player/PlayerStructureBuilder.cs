using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using ProjectWallE.UI;
using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
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
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManagerInput input;
        [SerializeField, AutoGetScene, HideInInspector] private StructureBuildMenu buildMenu;
        [SerializeField, AutoGetScene, HideInInspector] private StructureActionsMenu actionsMenu;

        private StructureNode _targetedNode;
        private Camera _mainCamera;
        private Ray _buildRay;
        private bool _canBuild;
        private bool _structureStatusVisible;
        private bool _menuOpen;
        private bool _lastMenuWasBuildMenu;
        private Structure _targetedStructure;
        private Structure _lastMenuStructure;
        private Sequence _timeSequence;
        private GameObject _activeGhost;

        public event Action<Structure[]> BuildMenuRequested;
        public event Action<Structure> ActionsMenuRequested;
        public event Action MenuCloseRequested;

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
            if (buildMenu)
            {
                buildMenu.OnItemSelected += TryBuildStructure;
                buildMenu.OnItemHoverChanged += OnBuildItemHoverChanged;
            }
            if (actionsMenu) actionsMenu.OnItemSelected += OnStructureActionSelected;
            if (playerManager)
            {
                playerManager.OnDeath += OnDeath;
                playerManager.OnControllerChanged += OnControllerChanged;
            }
        }

        private void OnDisable()
        {
            if (buildMenu)
            {
                buildMenu.OnItemSelected -= TryBuildStructure;
                buildMenu.OnItemHoverChanged -= OnBuildItemHoverChanged;
            }
            if (actionsMenu) actionsMenu.OnItemSelected -= OnStructureActionSelected;
            if (playerManager)
            {
                playerManager.OnDeath -= OnDeath;
                playerManager.OnControllerChanged -= OnControllerChanged;
            }
        }

        private void Update()
        {
            if (!playerManager || !playerManager.CanBuild) return;

            CastBuildRay();
            UpdateGhostPosition();

            if (!_menuOpen && input.ActionMenuPressed)
            {
                OpenContextMenu();
            }
            else if (_menuOpen)
            {
                if (input.ActionMenuReleased || input.Attack1Released)
                {
                    SelectHoveredAndClose();
                }
                else if (input.Attack2Released)
                {
                    CloseMenus();
                }
            }
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
            if (!action.IsAvailable) return;
            action.OnSelected?.Invoke();
        }

        private void OnBuildItemHoverChanged(Structure structure)
        {
            if (!StructureManager.Instance) return;

            if (structure)
            {
                _activeGhost = StructureManager.Instance.ShowGhost(structure);
            }
            else
            {
                StructureManager.Instance.HideGhost();
                _activeGhost = null;
            }
        }

        private void UpdateGhostPosition()
        {
            if (!_activeGhost) return;

            if (_targetedNode)
            {
                _activeGhost.transform.position = _targetedNode.transform.position;
                _activeGhost.transform.rotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
            }
            else if (Physics.Raycast(_buildRay, out RaycastHit hit, buildRange, buildableLayerMask))
            {
                Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                Quaternion rotation = projectedForward.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(projectedForward, hit.normal)
                    : Quaternion.identity;

                _activeGhost.transform.position = hit.point;
                _activeGhost.transform.rotation = rotation;
            }
        }

        private void SelectHoveredAndClose()
        {
            if (_lastMenuWasBuildMenu) buildMenu?.TrySelectHovered();
            else actionsMenu?.TrySelectHovered();

            CloseMenus();
        }

        private void OpenContextMenu()
        {
            _menuOpen = true;
            SetGameTimeScale(0f);
            RefreshOpenMenu();
        }

        private void CloseMenus()
        {
            SetGameTimeScale(1f);
            MenuCloseRequested?.Invoke();
            BuildPrompt.Instance?.Hide();
            UpdateStructureStatusVisibility(null);
            StructureManager.Instance?.HideGhost();
            _activeGhost = null;
            _menuOpen = false;
            _lastMenuStructure = null;
            _lastMenuWasBuildMenu = false;
        }

        private void CastBuildRay()
        {
            _buildRay = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
 
            if (Physics.Raycast(_buildRay, out RaycastHit structureHit, buildRange, structureLayerMask) && structureHit.collider.TryGetComponent(out Structure structure))
            {
                _targetedStructure = structure;
                _targetedNode = null;
                _canBuild = false;
                UpdateBuildPrompt(_menuOpen ? structure.TopPoint : null);
                UpdateStructureStatusVisibility(structure);
            }
            else if (Physics.Raycast(_buildRay, buildRange, blockBuildLayerMask))
            {
                _targetedStructure = null;
                _targetedNode = null;
                _canBuild = false;
                UpdateBuildPrompt(null);
                UpdateStructureStatusVisibility(null);
            }
            else if (Physics.Raycast(_buildRay, out RaycastHit groundHit, buildRange, buildableLayerMask))
            {
                _targetedStructure = null;
                UpdateStructureStatusVisibility(null);
                
                if (groundHit.collider.TryGetComponent(out StructureNode node))
                {
                    _targetedNode = node;
                    _canBuild = !node.IsOccupied;
                    UpdateBuildPrompt(_menuOpen ? node.SnapPoint : null);
                }
                else
                {
                    _targetedNode = null;
                    _canBuild = true;
                    UpdateBuildPrompt(_menuOpen ? groundHit.point : null);
                }
            }
            else
            {
                _targetedStructure = null;
                _targetedNode = null;
                _canBuild = false;
                UpdateBuildPrompt(null);
                UpdateStructureStatusVisibility(null);
            }
 
            if (_menuOpen) RefreshOpenMenu();
        }

        private static void UpdateStructureStatusVisibility(Structure structure)
        {
            if (structure)
            {
                StructureStatus.Instance?.Show(structure);
            }
            else
            {
                StructureStatus.Instance?.Hide();
            }
        }

        private void RefreshOpenMenu()
        {
            bool isBuildMenu = !_targetedStructure;

            if (!isBuildMenu)
            {
                if (_lastMenuWasBuildMenu || _lastMenuStructure != _targetedStructure)
                {
                    if (_lastMenuWasBuildMenu) MenuCloseRequested?.Invoke();
                    ActionsMenuRequested?.Invoke(_targetedStructure);
                    _lastMenuStructure = _targetedStructure;
                    _lastMenuWasBuildMenu = false;
                }
            }
            else
            {
                if (!_lastMenuWasBuildMenu)
                {
                    if (_lastMenuStructure) MenuCloseRequested?.Invoke();
                    BuildMenuRequested?.Invoke(_targetedNode ? _targetedNode.AllowedStructures : structuresArray);
                    _lastMenuStructure = null;
                    _lastMenuWasBuildMenu = true;
                }
            }
        }

        private void UpdateBuildPrompt(Vector3? position)
        {
            if (!BuildPrompt.Instance) return;
            
            if (position.HasValue)
            {
                BuildPrompt.Instance.Show(position.Value);
            }
            else
            {
                BuildPrompt.Instance.Hide();
            }
        }

        private void TryBuildStructure(Structure structure)
        {
            if (!_canBuild) return;
            
            if (ResourceManager.Instance.TrySpendResources(structure.BuildCost))
            {
                if (_targetedNode)
                {
                    StructureManager.Instance?.DeployStructureOnNode(structure, _targetedNode);
                }
                else if (Physics.Raycast(_buildRay, out RaycastHit hit, buildRange, buildableLayerMask))
                {
                    StructureManager.Instance?.DeployStructureOnGround(structure, hit.point, transform.forward, hit.normal);
                }
            }
        }

        private void SetGameTimeScale(float timeScale)
        {
            if (Mathf.Approximately(Time.timeScale, timeScale)) return;
            if (_timeSequence.isAlive) _timeSequence.Stop();

            var easeToUse = timeScale > 0f ? Ease.OutBack : Ease.Linear;
            
            _timeSequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.GlobalTimeScale(timeScale, 0.5f, easeToUse));
        }
    }
}