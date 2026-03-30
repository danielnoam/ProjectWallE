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

        void Awake()
        {
            Input = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }
        
        void EnableInput()
        {
            Input.Enable();

            Input.RobotControls.Enable();
            Input.RobotControls.SetCallbacks(this);
        }
    
        void DisableInput()
        {
            Input?.RobotControls.Disable();
            Input?.RobotControls.RemoveCallbacks(this);
        
            Input?.Disable();
        }

        void LateUpdate()
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