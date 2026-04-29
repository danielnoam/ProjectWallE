using DNExtensions.Utilities.AutoGet;
using ProjectWallE.GameLoop;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class SupportActionsMenu : RadialMenu<SOSupportActionData>
    {
        [SerializeField, AutoGetScene, HideInInspector] private PlayerManager player;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
            if (!player) return;
            player.SupportCaller.SupportMenuRequested += OnSupportCallerMenuRequested;
            player.SupportCaller.MenuCloseRequested += CloseMenu;
        }

        private void OnDisable()
        {
            if (!player) return;
            player.SupportCaller.SupportMenuRequested -= OnSupportCallerMenuRequested;
            player.SupportCaller.MenuCloseRequested -= CloseMenu;
        }

        private void OnSupportCallerMenuRequested(SOSupportActionData[] supports)
        {
            SetupMenu(supports, ConfigureElement);
            OpenMenu();
        }

        private void ConfigureElement(RadialMenuElement element, SOSupportActionData actionData)
        {
            element.Configure(actionData.Label, actionData.Icon, true);
        }
    }
}