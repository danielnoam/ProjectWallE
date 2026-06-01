

using System;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectWallE.GameLoop.Player
{
    [SelectionBase]
    public class PlayerSupportCaller : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float targetRange = 100f;
        [SerializeField] private float scatterRaycastHeight = 200f;
        [SerializeField] private LayerMask targetLayerMask;
        [SerializeField] private SOSupportActionData[] supportArray;
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManager playerManager;
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManagerInput input;
        [SerializeField, AutoGetScene, HideInInspector] private SupportActionsMenu supportMenu;

        private Vector3 _targetPoint;
        private Vector3 _targetNormal;
        private bool _menuOpen;
        private bool _hasTarget;

        public event Action<SOSupportActionData[]> SupportMenuRequested;
        public event Action MenuCloseRequested;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
            if (supportMenu) supportMenu.OnElementSelected += OnSupportSelected;
            if (playerManager)
            {
                playerManager.OnDeath += OnDeath;
                playerManager.OnControllerChanged += OnControllerChanged;
            }
        }

        private void OnDisable()
        {
            if (supportMenu) supportMenu.OnElementSelected -= OnSupportSelected;
            if (playerManager)
            {
                playerManager.OnDeath -= OnDeath;
                playerManager.OnControllerChanged -= OnControllerChanged;
            }
        }

        private void Update()
        {
            if (!playerManager || !playerManager.CanSupport) return;

            CastTargetRay();

            if (!_menuOpen && input.SupportMenuPressed)
            {
                OpenMenu();
            }
            else if (_menuOpen)
            {
                if (input.SupportMenuReleased)
                {
                    SelectHoveredAndClose();
                }
                else if (input.Attack2Released)
                {
                    CloseMenu();
                }
            }
        }

        private void CastTargetRay()
        {
            Ray ray = playerManager.Aimer.CameraRay;
            if (Physics.Raycast(ray, out RaycastHit hit, targetRange, targetLayerMask))
            {
                _targetPoint = hit.point;
                _targetNormal = hit.normal;
                _hasTarget = true;
                if (_menuOpen)
                {
                    AirSupportPrompt.Instance?.Show(_targetPoint, _targetNormal);
                }
            }
            else
            {
                _hasTarget = false;
                if (_menuOpen) AirSupportPrompt.Instance?.Hide();
            }
        }

        private void OpenMenu()
        {
            _menuOpen = true;
            SupportMenuRequested?.Invoke(supportArray);
        }

        private void CloseMenu()
        {
            AirSupportPrompt.Instance?.Hide();
            _menuOpen = false;
            MenuCloseRequested?.Invoke();
        }

        private void SelectHoveredAndClose()
        {
            supportMenu?.TrySelectHovered();
            CloseMenu();
        }

        private void OnSupportSelected(RadialMenuElement _, SOSupportActionData data)
        {
            if (!_hasTarget) return;
            if (ResourceManager.Instance && !ResourceManager.Instance.TrySpendResources(data.Cost)) return;
            FireSupport(data, _targetPoint, _targetNormal);
            AirSupportConfirmation.Instance?.Show(_targetPoint, _targetNormal);
        }

        private void OnDeath(IDamageable damageable) => CloseMenu();

        private void OnControllerChanged(ControllerType controllerType) => CloseMenu();

        private void FireSupport(SOSupportActionData data, Vector3 targetPosition, Vector3 surfaceNormal)
        {
            if (!DeploymentManager.Instance) return;

            for (int i = 0; i < data.PodCount; i++)
            {
                Vector2 scatter = Random.insideUnitCircle * data.ScatterRadius;
                Vector3 scatteredPosition = targetPosition + new Vector3(scatter.x, 0f, scatter.y);
                Vector3 resolvedNormal = surfaceNormal;

                Vector3 rayOrigin = scatteredPosition + Vector3.up * scatterRaycastHeight;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, scatterRaycastHeight * 2f, targetLayerMask, QueryTriggerInteraction.Ignore))
                {
                    scatteredPosition.y = hit.point.y;
                    resolvedNormal = hit.normal;
                }

                var request = new DeploymentRequest(scatteredPosition, transform.forward, null)
                {
                    TargetSurfaceNormal = resolvedNormal,
                    Team = Team.Player,
                    InstantiateOnLand = false
                };

                DeploymentManager.Instance.LaunchFromShip(data, request);
            }
        }
    }
}