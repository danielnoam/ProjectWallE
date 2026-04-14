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
                RestartLevel();
            }

            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                ForceCompleteObjectives();
            }
        }

        private void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ForceCompleteObjectives()
        {
            var objectives = LevelManager.Instance?.ActiveObjectives;
            if (objectives == null) return;

            foreach (var objective in objectives)
            {
                objective?.ForceComplete();
            }
        }
    }
}