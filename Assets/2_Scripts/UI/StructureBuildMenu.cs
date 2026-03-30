using DNExtensions.Utilities.AutoGet;
using ProjectWallE;
using RadialMenu;
using UnityEngine;

public class StructureBuildMenu : RadialMenu<Structure>
{
    [SerializeField, AutoGetScene, HideInInspector] private PlayerManager player;
    
    private void OnEnable()
    {
        if (player)
        {
            player.StructureBuilder.BuildMenuRequested += HandleOpen;
            player.StructureBuilder.MenuCloseRequested += CloseMenu;
        }
    }

    private void OnDisable()
    {
        if (player)
        {
            player.StructureBuilder.BuildMenuRequested -= HandleOpen;
            player.StructureBuilder.MenuCloseRequested -= CloseMenu;
        }
    }

    private void HandleOpen(Structure[] structures)
    {
        SetupMenu(structures, ConfigureElement);
        OpenMenu();
    }

    private void ConfigureElement(RadialMenuElement element, Structure structure)
    {
        bool canAfford = ResourceManager.Instance.CanAfford(structure.BuildCost);
        element.elementInfo = $"{structure.StructureUIData.Label}\nCost: {structure.BuildCost}";
        element.SetDisabled(!canAfford);

        if (structure.StructureUIData.Icon)
        {
            element.iconImage.sprite = structure.StructureUIData.Icon;
        }
        else
        {
            element.iconImage.gameObject.SetActive(false);
        }
    }
}