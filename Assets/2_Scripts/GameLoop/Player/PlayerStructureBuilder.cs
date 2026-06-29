using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.UI;
using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
    [SelectionBase]
    public class PlayerStructureBuilder : MonoBehaviour
    {
        [Header("Build Settings")] 
        [SerializeField] private float buildRange = 50f;
        [SerializeField] private float downBuildRange = 10f;
        [SerializeField] private float maxBuildAngle = 45f;
        [SerializeField] private LayerMask buildableLayerMask;
        [SerializeField] private LayerMask blockBuildLayerMask;
        [SerializeField] private LayerMask structureLayerMask;
        [SerializeField] private LayerMask nodeLayerMask;
        [SerializeField, PrefabSelector("Assets/5_Prefabs/Structures")] private Structure[] structuresArray;
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManager playerManager;
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManagerInput input;
        [SerializeField, AutoGetScene, HideInInspector] private StructureBuildMenu buildMenu;
        [SerializeField, AutoGetScene, HideInInspector] private StructureActionsMenu actionsMenu;

        private StructureNode _targetedNode;
        private Ray _buildRay;
        private Vector3 _buildPoint;
        private Vector3 _buildNormal;
        private bool _lastCanBuild;
        private bool _canBuild;
        private bool _structureStatusVisible;
        private bool _menuOpen;
        private bool _lastMenuWasBuildMenu;
        private Structure _targetedStructure;
        private Structure _lastMenuStructure;
        private StructureNode _lastBuildNode;

        public event Action<Structure[], bool> BuildMenuRequested;
        public event Action<Structure> ActionsMenuRequested;
        public event Action MenuCloseRequested;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
            if (buildMenu)
            {
                buildMenu.OnElementSelected += TryBuildStructure;
                buildMenu.OnElementHoverChanged += OnBuildElementHoverChanged;
            }
            if (actionsMenu) actionsMenu.OnElementSelected += OnStructureActionSelected;
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
                buildMenu.OnElementSelected -= TryBuildStructure;
                buildMenu.OnElementHoverChanged -= OnBuildElementHoverChanged;
            }
            if (actionsMenu) actionsMenu.OnElementSelected -= OnStructureActionSelected;
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

        private void OnControllerChanged(ControllerType controllerType)
        {
            CloseMenus();
        }

        private void OnStructureActionSelected(RadialMenuElement _, StructureAction action)
        {
            if (!action.IsAvailable) return;
            action.OnSelected?.Invoke();
        }

        private void OnBuildElementHoverChanged(RadialMenuElement _, Structure structure)
        {
            if (!StructureManager.Instance) return;

            if (structure)
            {
                StructureManager.Instance.SetGhost(structure);
            }
            else
            {
                StructureManager.Instance.HideGhost();
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
            RefreshOpenMenu();
        }

        private void CloseMenus()
        {
            UpdateStructureStatusVisibility(null);
            StructureManager.Instance?.HideGhost();
            _menuOpen = false;
            _lastCanBuild = false;
            _lastMenuStructure = null;
            _lastMenuWasBuildMenu = false;
            _lastBuildNode = null;
            MenuCloseRequested?.Invoke();
        }

        private void CastBuildRay()
        {
            _buildRay = playerManager.Aimer.CameraRay;

            if (Physics.Raycast(_buildRay, out RaycastHit structureHit, buildRange, structureLayerMask) && structureHit.collider.TryGetComponent(out Structure structure))
            {
                _targetedStructure = structure;
                _targetedNode = null;
                _canBuild = false;
                UpdateStructureStatusVisibility(structure);
                StructureManager.Instance?.HideGhost();
            }
            else if (Physics.Raycast(_buildRay, buildRange, blockBuildLayerMask))
            {
                _targetedStructure = null;
                _targetedNode = null;
                _canBuild = false;
                UpdateStructureStatusVisibility(null);
            }
            else if (Physics.Raycast(_buildRay, out RaycastHit nodeHit, buildRange, nodeLayerMask, QueryTriggerInteraction.Collide) && nodeHit.collider.TryGetComponent(out StructureNode node))
            {
                _targetedStructure = null;
                UpdateStructureStatusVisibility(null);
                _buildPoint = node.SnapPoint;
                _buildNormal = Vector3.up;
                _targetedNode = node;
                _canBuild = !node.IsOccupied;
                Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
                StructureManager.Instance?.ShowGhost(node.transform.position, rotation, _canBuild);
            }
            else if (Physics.Raycast(_buildRay, out RaycastHit groundHit, buildRange, buildableLayerMask, QueryTriggerInteraction.Ignore))
            {
                _targetedStructure = null;
                UpdateStructureStatusVisibility(null);
                _buildPoint = groundHit.point;
                _buildNormal = groundHit.normal;
                _targetedNode = null;
                _canBuild = Vector3.Angle(groundHit.normal, Vector3.up) <= maxBuildAngle;
                Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, groundHit.normal).normalized;
                Quaternion rotation = projectedForward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(projectedForward, groundHit.normal) : Quaternion.identity;
                StructureManager.Instance?.ShowGhost(groundHit.point, rotation, _canBuild);
            }
            else
            {
                _targetedStructure = null;
                _targetedNode = null;

                Vector3 fallbackOrigin = _buildRay.GetPoint(buildRange);
                if (Physics.Raycast(fallbackOrigin, Vector3.down, out RaycastHit downHit, downBuildRange, buildableLayerMask, QueryTriggerInteraction.Ignore))
                {
                    _canBuild = Vector3.Angle(downHit.normal, Vector3.up) <= maxBuildAngle;

                    _buildPoint = downHit.point;
                    _buildNormal = downHit.normal;

                    Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, downHit.normal).normalized;
                    Quaternion rotation = projectedForward.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(projectedForward, downHit.normal)
                        : Quaternion.identity;
                    StructureManager.Instance?.ShowGhost(downHit.point, rotation, _canBuild);
                }
                else
                {
                    UpdateStructureStatusVisibility(null);
                    StructureManager.Instance?.HideGhost();
                    _canBuild = false;
                }
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
                if (!_lastMenuWasBuildMenu || _lastCanBuild != _canBuild || _lastBuildNode != _targetedNode)
                {
                    if (!_lastMenuWasBuildMenu && _lastMenuStructure) MenuCloseRequested?.Invoke();
                    BuildMenuRequested?.Invoke(_targetedNode ? _targetedNode.AllowedStructures : structuresArray, _canBuild);
                    _lastMenuStructure = null;
                    _lastMenuWasBuildMenu = true;
                    _lastCanBuild = _canBuild;
                    _lastBuildNode = _targetedNode;
                }
            }
        }
        

        private void TryBuildStructure(RadialMenuElement _, Structure structure)
        {
            if (!_canBuild) return;

            if (_targetedNode && Array.IndexOf(_targetedNode.AllowedStructures, structure) < 0) return;

            if (ResourceManager.Instance && !ResourceManager.Instance.TrySpendResources(structure.BuildCost)) return;

            if (_targetedNode)
                StructureManager.Instance?.DeployStructureOnNode(structure, _targetedNode);
            else
                StructureManager.Instance?.DeployStructureOnGround(structure, _buildPoint, transform.forward, _buildNormal, PodCameraMode.None);

            StructureManager.Instance?.ConfirmAndHideGhost();
        }
    }
}