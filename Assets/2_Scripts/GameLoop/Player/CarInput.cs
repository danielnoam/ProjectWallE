using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectWallE
{
    public class CarInput : MonoBehaviour, InputSystem_Actions.ICarControlsActions
    {
        public InputSystem_Actions Input { get; private set; }   
        
        public float Acceleration { get; private set; }
        public float Steering { get; private set; }
        public bool HandBreakHeld { get; private set; }
        public bool BoostHeld { get; private set; }
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
            Input = new InputSystem_Actions();
            Input.Enable();

            Input.CarControls.Enable();
            Input.CarControls.SetCallbacks(this);
        }
    
        void DisableInput()
        {
            Input.CarControls.Disable();
            Input.CarControls.RemoveCallbacks(this);
        
            Input.Dispose();
            Input = null;
        }
        
        public void OnAccelDecel(InputAction.CallbackContext context)
        {
            Acceleration = context.ReadValue<float>();
        }

        public void OnSteering(InputAction.CallbackContext context)
        {
            Steering = context.ReadValue<float>();
        }

        public void OnHandbreak(InputAction.CallbackContext context)
        {
            HandBreakHeld = context.ReadValueAsButton();
        }

        public void OnBoost(InputAction.CallbackContext context)
        {
            BoostHeld = context.ReadValueAsButton();
        }
    }
}
