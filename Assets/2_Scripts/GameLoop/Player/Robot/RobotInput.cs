using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectWallE
{
    public class RobotInput : MonoBehaviour, InputSystem_Actions.IRobotControlsActions
    {
        public InputSystem_Actions Input { get; private set; }

        public Vector2 Movement { get; private set; }

        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpReleased { get; private set; }

        private void Awake()
        {
            Input = new InputSystem_Actions();
            Input.RobotControls.SetCallbacks(this);
        }

        private void OnEnable()
        {
            Input.RobotControls.Enable();
        }

        private void OnDisable()
        {
            if (Input == null) return;
            Input.RobotControls.Disable();
        }

        private void OnDestroy()
        {
            if (Input == null) return;

            Input.RobotControls.RemoveCallbacks(this);
            Input.Dispose();
            Input = null;
        }

        private void LateUpdate()
        {
            JumpPressed = false;
            JumpReleased = false;
        }

        public void OnMovement(InputAction.CallbackContext context)
        {
            Movement = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                JumpPressed = true;
                JumpHeld = true;
            }
            else if (context.canceled)
            {
                JumpReleased = true;
                JumpHeld = false;
            }
        }
    }
}