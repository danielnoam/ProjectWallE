using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.UI
{
    
    [RequireComponent(typeof(CanvasGroup))]
    public class BuildPrompt : MonoBehaviour
    {
        public static BuildPrompt Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private Vector3 offset;
        [SerializeField, AutoGetSelf, HideInInspector] private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }

            canvasGroup.alpha = 0;
        }
        
        
        
        
        public void Show(Vector3 position)
        {
            transform.position = position.Add(offset);
            canvasGroup.alpha = 1;
        }
        
        public void Hide()
        {
            canvasGroup.alpha = 0;
        }
    }
}
