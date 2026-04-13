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
                player.StructureBuilder.BuildMenuRequested += OnOpen;
                player.StructureBuilder.MenuCloseRequested += CloseMenu;
            }
        }

        private void OnDisable()
        {
            if (player)
            {
                player.StructureBuilder.BuildMenuRequested -= OnOpen;
                player.StructureBuilder.MenuCloseRequested -= CloseMenu;
            }
        }

        private void OnOpen(Structure[] structures)
        {
            SetupMenu(structures, ConfigureElement);
            OpenMenu();
        }

        private static void ConfigureElement(RadialMenuElement element, Structure structure)
        {
            bool canAfford = !ResourceManager.Instance || ResourceManager.Instance.CanAfford(structure.BuildCost);
            
            element.Configure(
                $"{structure.StructureUIData.Label}\nCost: {structure.BuildCost}",
                structure.StructureUIData.Icon,
                canAfford
            );
        }
    }
}