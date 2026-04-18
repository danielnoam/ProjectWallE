using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProjectWallE.GameLoop
{
    public class CheatsManager : MonoBehaviour
    {
        private static CheatsManager _instance;
        
        [Header("Settings")]
        [SerializeField] private Key restartSceneKey = Key.F5;
        [SerializeField] private Key completeObjectivesKey = Key.F4;
        [SerializeField] private Key addResourcesKey = Key.F3;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        

        private void Update()
        {
            if (Keyboard.current[restartSceneKey].wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                Debug.Log("Scene reloaded");
            }

            if (Keyboard.current[completeObjectivesKey].wasPressedThisFrame)
            {
                var objectives = LevelManager.Instance?.ActiveObjectives;
                if (objectives == null) return;

                foreach (var objective in objectives)
                {
                    objective?.ForceComplete();
                }
                
                Debug.Log("Objectives force complete");
            }
            
            if (Keyboard.current[addResourcesKey].wasPressedThisFrame)
            {
                ResourceManager.Instance?.AddResources(500);
                Debug.Log("Added 500 Resources");
            }
        }
    }
}