using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class StructureBuildMenu : RadialMenu<Structure>
    {
        [SerializeField, AutoGetScene, HideInInspector] private PlayerManager player;

        private void OnEnable()
        {
            if (player)
            {
                player.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested += CloseMenu;
            }
        }

        private void OnDisable()
        {
            if (player)
            {
                player.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested -= CloseMenu;
            }
        }

        private void OnBuildMenuRequested(Structure[] structures, bool canBuild)
        {
            menuTitleText.text = "Structures";
            SetupMenu(structures, (element, structure) => ConfigureElement(element, structure, canBuild));
            OpenMenu();
        }

        private void ConfigureElement(RadialMenuElement element, Structure structure, bool canBuild)
        {
            if (!canBuild) selectedItemText.text = "Can't build here";
            bool canAfford = !ResourceManager.Instance || ResourceManager.Instance.CanAfford(structure.BuildCost);
            
            element.Configure(
                canBuild ? $"{structure.StructureUIData.Label}\nCost: {structure.BuildCost}" : "Can't build here",
                structure.StructureUIData.Icon,
                canBuild && canAfford
            );
        }
    }
}
