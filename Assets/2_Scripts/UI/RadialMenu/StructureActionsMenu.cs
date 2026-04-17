using System;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class StructureAction
    {
        public string Label;
        public Sprite Icon;
        public bool IsAvailable;
        public Action OnSelected;
    }
    
    public class StructureActionsMenu : RadialMenu<StructureAction>
    {
        [SerializeField, AutoGetScene, HideInInspector] private PlayerManager player;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
            if (player)
            {
                player.StructureBuilder.ActionsMenuRequested += OnOpen;
                player.StructureBuilder.MenuCloseRequested += CloseMenu;
            }
        }

        private void OnDisable()
        {
            if (player)
            {
                player.StructureBuilder.ActionsMenuRequested -= OnOpen;
                player.StructureBuilder.MenuCloseRequested -= CloseMenu;
            }
        }

        private void OnOpen(Structure structure)
        {
            if (!structure) return;
            menuTitleText.text = structure.StructureUIData.Label;
            SetupMenu(structure.GetActions(), ConfigureElement);
            OpenMenu();
        }

        private static void ConfigureElement(RadialMenuElement element, StructureAction action)
        {
            element.Configure(action.Label, action.Icon, action.IsAvailable);
        }
    }
}