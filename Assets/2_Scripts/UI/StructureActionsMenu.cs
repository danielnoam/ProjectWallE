using System;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE;
using ProjectWallE.GameLoop.Player;
using RadialMenu;
using UnityEngine;

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
            player.StructureBuilder.ActionsMenuRequested += HandleOpen;
            player.StructureBuilder.MenuCloseRequested += CloseMenu;
        }
    }

    private void OnDisable()
    {
        if (player)
        {
            player.StructureBuilder.ActionsMenuRequested -= HandleOpen;
            player.StructureBuilder.MenuCloseRequested -= CloseMenu;
        }
    }

    private void HandleOpen(Structure structure)
    {
        if (!structure) return;
        SetupMenu(structure.GetActions(), ConfigureElement);
        OpenMenu();
    }

    private void ConfigureElement(RadialMenuElement element, StructureAction action)
    {
        element.elementInfo = action.Label;
        element.iconImage.sprite = action.Icon;
        element.SetDisabled(!action.IsAvailable);

        if (action.Icon)
        {
            element.iconImage.sprite = action.Icon;
        }
        else
        {
            element.iconImage.gameObject.SetActive(false);
        }
    }
}