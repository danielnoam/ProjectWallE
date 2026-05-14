using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class RadarDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, AutoGetChildren] private RadarSystem radarSystem;
        [SerializeField, AutoGetScene] private PlayerManager player;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
            if (!player || !radarSystem) return;

            radarSystem.worldCenter.SetTransform(player.transform);
            if (Camera.main) radarSystem.rotationTarget.Value = Camera.main.transform;
        }
    }
}