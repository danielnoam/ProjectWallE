
public class StructureBuildMenu : RadialMenu<Structure>
{
    private void OnEnable()
    {
        PlayerStructureBuilder.BuildMenuRequested += HandleOpen;
        PlayerStructureBuilder.MenuCloseRequested += CloseMenu;
    }

    private void OnDisable()
    {
        PlayerStructureBuilder.BuildMenuRequested -= HandleOpen;
        PlayerStructureBuilder.MenuCloseRequested -= CloseMenu;
    }

    private void HandleOpen(Structure[] structures)
    {
        SetupMenu(structures, ConfigureElement);
        OpenMenu();
    }

    private void ConfigureElement(RadialMenuElement element, Structure structure)
    {
        bool canAfford = ResourceManager.Instance.CanAfford(structure.BuildCost);
        element.elementInfo = $"{structure.Label}\nCost: {structure.BuildCost}";
        element.SetDisabled(!canAfford);

        if (structure.Icon)
        {
            element.iconImage.sprite = structure.Icon;
        }
        else
        {
            element.iconImage.gameObject.SetActive(false);
        }
    }
}