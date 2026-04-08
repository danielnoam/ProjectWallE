using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(PlayerManagerInput))]
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera robotCamera;
        [SerializeField] private CinemachineCamera carCamera;
        [SerializeField] private Transform cameraTarget;

        [Header("Look Settings")]
        [SerializeField] private float mouseLookSensitivity = 0.08f;
        [SerializeField] private float gamepadLookSensitivity = 180f;
        [SerializeField, Range(1, 89)] private float cameraVerticalClamp = 80f;

        private PlayerManager _playerManager;
        private PlayerManagerInput _input;

        private bool _cameraLocked;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            _playerManager = FindFirstObjectByType<PlayerManager>();
            _input = GetComponent<PlayerManagerInput>();

            Vector3 euler = cameraTarget.rotation.eulerAngles;
            _yaw = euler.y;

            _pitch = euler.x;
            if (_pitch > 180f)
                _pitch -= 360f;
        }

        private void OnEnable()
        {
            if (_playerManager == null) return;

            _playerManager.OnControllerChanged += OnControllerSwitch;
            _playerManager.StructureBuilder.ActionsMenuRequested += OnMenuActionRequested;
            _playerManager.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
            _playerManager.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
        }

        private void OnDisable()
        {
            if (_playerManager == null) return;

            _playerManager.OnControllerChanged -= OnControllerSwitch;
            _playerManager.StructureBuilder.ActionsMenuRequested -= OnMenuActionRequested;
            _playerManager.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
            _playerManager.StructureBuilder.MenuCloseRequested -= OnMenuCloseRequested;
        }

        private void LateUpdate()
        {
            UpdateCameraMovement();
        }

        private void OnControllerSwitch(PlayerControllerType controllerType)
        {
            bool isRobot = controllerType == PlayerControllerType.Robot;

            robotCamera.Priority.Value = isRobot ? 1 : 0;
            carCamera.Priority.Value = isRobot ? 0 : 1;
        }

        private void OnMenuActionRequested(Structure obj)
        {
            _cameraLocked = true;
        }

        private void OnBuildMenuRequested(Structure[] obj)
        {
            _cameraLocked = true;
        }

        private void OnMenuCloseRequested()
        {
            _cameraLocked = false;
        }

        private void UpdateCameraMovement()
        {
            cameraTarget.position = _playerManager.transform.position;

            if (_cameraLocked) return;

            Vector2 lookDelta = GetScaledLookDelta();

            _yaw += lookDelta.x;
            _pitch -= lookDelta.y;
            _pitch = Mathf.Clamp(_pitch, -cameraVerticalClamp, cameraVerticalClamp);

            cameraTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private Vector2 GetScaledLookDelta()
        {
            Vector2 rawLook = _input.LookInput;

            if (_input.IsGamepadLook)
                return rawLook * gamepadLookSensitivity * Time.deltaTime;

            return rawLook * mouseLookSensitivity;
        }
    }
}