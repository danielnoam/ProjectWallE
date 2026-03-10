using System.Collections.Generic;

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

    private void HandleOpen(List<StructureAction> actions)
    {
        SetupMenu(actions, ConfigureElement);
        OpenMenu();
    }

    private void ConfigureElement(RadialMenuElement element, StructureAction action)
    {
        element.elementInfo = action.label;
        element.SetDisabled(!action.isAvailable);

        if (action.icon)
            element.iconImage.sprite = action.icon;
        else
            element.iconImage.gameObject.SetActive(false);
    }
}