using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProjectWallE.GameLoop
{
    public class CheatsManager : MonoBehaviour
    {
        private static CheatsManager _instance;
        
        [Header("Settings")]
        [SerializeField] private Key restartSceneKey = Key.F6;
        [SerializeField] private Key completeLevelKey = Key.F5;
        [SerializeField] private Key completeObjectivesKey = Key.F4;
        [SerializeField] private Key addResourcesKey = Key.F3;
        [SerializeField] private Key healPlayerKey = Key.F2;
        [SerializeField] private Key damagePlayerKey = Key.F1;

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

            if (Keyboard.current[completeLevelKey].wasPressedThisFrame)
            {
                LevelManager.Instance?.CompleteLevel();
                Debug.Log("Level complete");
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
            
            if (Keyboard.current[healPlayerKey].wasPressedThisFrame)
            {
                PlayerManager.Instance?.Heal(25f);
                Debug.Log("Healed 25 Player");
            }
            
            if (Keyboard.current[damagePlayerKey].wasPressedThisFrame)
            {
                PlayerManager.Instance?.TakeDamage(25f, null);
                Debug.Log("Damaged Player for 25");
            }
        }
    }
}