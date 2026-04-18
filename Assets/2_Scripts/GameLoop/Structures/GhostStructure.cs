using DNExtensions.Systems.Scriptables;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class GhostStructure : MonoBehaviour
    {
        [SerializeField] private SOColorHDR canBuildColor;
        [SerializeField] private SOColorHDR blockBuildColor;
        [SerializeField] private Material material;

        
        
        
        public void SetCanBuild(bool canBuild)
        {
            if (!material) return;
            material.color = canBuild ? canBuildColor : blockBuildColor;
        }
    }
}