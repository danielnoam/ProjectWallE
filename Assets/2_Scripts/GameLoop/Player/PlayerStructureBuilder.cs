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
        [SerializeField] private float buildRange = 50f;
        [SerializeField] private float downBuildRange = 10f;
        [SerializeField] private float maxBuildAngle = 45f;
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
        private Vector3 _buildPoint;
        private Vector3 _buildNormal;
        private bool _canBuild;
        private bool _structureStatusVisible;
        private bool _menuOpen;
        private bool _lastMenuWasBuildMenu;
        private Structure _targetedStructure;
        private Structure _lastMenuStructure;
        private Sequence _timeSequence;

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
            SetGameTimeScale(0.05f);
            RefreshOpenMenu();
        }

        private void CloseMenus()
        {
            SetGameTimeScale(1f);
            UpdateStructureStatusVisibility(null);
            StructureManager.Instance?.HideGhost();
            BuildPrompt.Instance?.Hide();
            _menuOpen = false;
            _lastMenuStructure = null;
            _lastMenuWasBuildMenu = false;
            MenuCloseRequested?.Invoke();
        }

        private void CastBuildRay()
        {
            _buildRay = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
 

            if (Physics.Raycast(_buildRay, out RaycastHit structureHit, buildRange, structureLayerMask) && structureHit.collider.TryGetComponent(out Structure structure)) // structure
            {
                _targetedStructure = structure;
                _targetedNode = null;
                _canBuild = false;
                UpdateBuildPrompt(_menuOpen ? structure.TopPoint : null);
                UpdateStructureStatusVisibility(structure);
                StructureManager.Instance?.HideGhost();
            }
            else if (Physics.Raycast(_buildRay, buildRange, blockBuildLayerMask)) // blocked
            {
                _targetedStructure = null;
                _targetedNode = null;
                _canBuild = false;
                UpdateBuildPrompt(null);
                UpdateStructureStatusVisibility(null);
            }
            else if (Physics.Raycast(_buildRay, out RaycastHit groundHit, buildRange, buildableLayerMask)) // build
            {
                _targetedStructure = null;
                UpdateStructureStatusVisibility(null);
                
                if (groundHit.collider.TryGetComponent(out StructureNode node))
                {
                    _buildPoint = node.SnapPoint;
                    _buildNormal = Vector3.up;
                    _targetedNode = node;
                    _canBuild = !node.IsOccupied;
                    UpdateBuildPrompt(_menuOpen ? node.SnapPoint : null);
                    Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized, Vector3.up);
                    StructureManager.Instance?.ShowGhost(node.transform.position,rotation, _canBuild);
                }
                else
                {
                    _buildPoint = groundHit.point;
                    _buildNormal = groundHit.normal;
                    _targetedNode = null;
                    _canBuild = Vector3.Angle(groundHit.normal, Vector3.up) <= maxBuildAngle;
                    UpdateBuildPrompt(_menuOpen ? groundHit.point : null);
                    Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, groundHit.normal).normalized;
                    Quaternion rotation = projectedForward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(projectedForward, groundHit.normal) : Quaternion.identity;
                    StructureManager.Instance?.ShowGhost(groundHit.point, rotation, _canBuild);
                }
            }
            else // nothing
            {
                _targetedStructure = null;
                _targetedNode = null;

                Vector3 fallbackOrigin = _buildRay.GetPoint(buildRange);
                if (Physics.Raycast(fallbackOrigin, Vector3.down, out RaycastHit downHit, downBuildRange, buildableLayerMask))
                {
                    _canBuild = Vector3.Angle(downHit.normal, Vector3.up) <= maxBuildAngle;
                    UpdateBuildPrompt(_menuOpen ? downHit.point : null);
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
                    UpdateBuildPrompt(null);
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

            if (ResourceManager.Instance)
            {
                if (!ResourceManager.Instance.TrySpendResources(structure.BuildCost))
                {
                    return;
                }
            }

            if (_targetedNode)
            {
                StructureManager.Instance?.DeployStructureOnNode(structure, _targetedNode);
            }
            else
            {
                StructureManager.Instance?.DeployStructureOnGround(structure, _buildPoint, transform.forward, _buildNormal);
            }
        }

        private void SetGameTimeScale(float timeScale)
        {
            if (Mathf.Approximately(Time.timeScale, timeScale)) return;
            if (_timeSequence.isAlive) _timeSequence.Stop();

            var easeToUse = timeScale > 0.05f ? Ease.OutBack : Ease.Linear;
            
            _timeSequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.GlobalTimeScale(timeScale, 0.5f, easeToUse));
        }
    }
}