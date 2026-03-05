using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectWallE
{
    public class PlayerManagerInput : MonoBehaviour, InputSystem_Actions.IPlayerManagerControlsActions
    {
        public InputSystem_Actions Input { get; private set; }   
        
        public bool SwitchPressed { get; private set; }

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

            Input.PlayerManagerControls.Enable();
            Input.PlayerManagerControls.SetCallbacks(this);
        }
    
        void DisableInput()
        {
            Input.PlayerManagerControls.Disable();
            Input.PlayerManagerControls.RemoveCallbacks(this);
        
            Input.Dispose();
            Input = null;
        }

        void LateUpdate()
        {
            SwitchPressed = false;
        }

        public void OnSwitchController(InputAction.CallbackContext context)
        {
            if (context.started) SwitchPressed = true;
        }
    }
}
