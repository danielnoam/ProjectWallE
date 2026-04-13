using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectWallE
{
    public class PlayerManagerInput : MonoBehaviour, InputSystem_Actions.IPlayerManagerControlsActions
    {
        public InputSystem_Actions Input { get; private set; }

        public bool SwitchPressed { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsGamepadLook { get; private set; }

        public bool Attack1Held { get; private set; }
        public bool Attack1Released { get; private set; }
        public bool Attack2Held { get; private set; }
        public bool Attack2Released { get; private set; }
        public bool ActionMenuPressed { get; private set; }
        public bool ActionMenuReleased { get; private set; }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        private void EnableInput()
        {
            Input = new InputSystem_Actions();
            Input.Enable();

            Input.PlayerManagerControls.Enable();
            Input.PlayerManagerControls.SetCallbacks(this);
        }

        private void DisableInput()
        {
            if (Input == null) return;

            Input.PlayerManagerControls.Disable();
            Input.PlayerManagerControls.RemoveCallbacks(this);

            Input.Dispose();
            Input = null;
        }

        private void LateUpdate()
        {
            SwitchPressed = false;
            ActionMenuPressed = false;
            ActionMenuReleased = false;
            Attack1Released = false;
            Attack2Released = false;
        }

        public void OnSwitchController(InputAction.CallbackContext context)
        {
            if (context.started) SwitchPressed = true;
        }

        public void OnAttack1(InputAction.CallbackContext context)
        {
            Attack1Held = context.ReadValueAsButton();
            if (context.canceled) Attack1Released = true;
        }

        public void OnAttack2(InputAction.CallbackContext context)
        {
            Attack2Held = context.ReadValueAsButton();
            if (context.canceled) Attack2Released = true;
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();

            if (context.control?.device is Gamepad)
                IsGamepadLook = true;
            else if (context.control?.device is Mouse)
                IsGamepadLook = false;
        }

        public void OnActionMenu(InputAction.CallbackContext context)
        {
            if (context.started) ActionMenuPressed = true;
            if (context.canceled) ActionMenuReleased = true;
        }
    }
}