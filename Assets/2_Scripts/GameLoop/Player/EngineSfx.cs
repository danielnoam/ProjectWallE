using System.Collections.Generic;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE
{
    public class EngineSfx : MonoBehaviour
    {
        [SerializeField] private SOCarControllerSettings carControllerSettings;
        [SerializeField] private List<AudioSource> audioSources;

        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;
        
        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }
        void Awake()
        {
            foreach (var source in audioSources)
            {
                source.volume = 0;
            }
        }
        
        
    }
}
