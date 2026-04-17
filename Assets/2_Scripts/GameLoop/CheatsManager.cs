using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProjectWallE.GameLoop
{
    public class LevelCheats : MonoBehaviour
    {
        private static LevelCheats _instance;

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
            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (Keyboard.current.f4Key.wasPressedThisFrame)
            {
                var objectives = LevelManager.Instance?.ActiveObjectives;
                if (objectives == null) return;

                foreach (var objective in objectives)
                {
                    objective?.ForceComplete();
                }
            }
            
            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                ResourceManager.Instance?.AddResources(500);
            }
        }
    }
}