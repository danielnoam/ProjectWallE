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
        public bool MovementHeld => BoostHeld || Acceleration != 0;

        private void Awake()
        {
            Input = new InputSystem_Actions();
            Input.CarControls.SetCallbacks(this);
        }

        private void OnEnable()
        {
            Input.CarControls.Enable();
        }

        private void OnDisable()
        {
            if (Input == null) return;
            Input.CarControls.Disable();
        }

        private void OnDestroy()
        {
            if (Input == null) return;

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