
public class StructureActionsMenu : RadialMenu<StructureAction>
{
    private void OnEnable()
    {
        PlayerStructureBuilder.ActionsMenuRequested += HandleOpen;
        PlayerStructureBuilder.MenuCloseRequested += CloseMenu;
    }

    private void OnDisable()
    {
        PlayerStructureBuilder.ActionsMenuRequested -= HandleOpen;
        PlayerStructureBuilder.MenuCloseRequested -= CloseMenu;
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