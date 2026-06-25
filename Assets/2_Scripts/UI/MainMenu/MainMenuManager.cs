using DNExtensions.Systems.MenuSystem;
using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CustomFields;
using UnityEngine;
using UnityEngine.UI;
using Screen = DNExtensions.Systems.MenuSystem.Screen;

namespace ProjectWallE.UI
{
    [DisallowMultipleComponent]
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool animated = true;
        [AutoGetSelf, SerializeField] private MenuManager menuManager;
        
        [Header("Main")]
        [SerializeField] private Screen mainScreen;
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SceneField gameScene;
        [SerializeField] private EffectSequence fadeOutSequence;

        [Header("Options")]
        [SerializeField] private Screen optionsScreen;
        [SerializeField] private Button optionsBackButton;

        [Header("Credits")]
        [SerializeField] private Screen creditsScreen;
        [SerializeField] private Button creditsBackButton;

        private void OnEnable()
        {
            playButton.onClick.AddListener(Play);
            optionsButton.onClick.AddListener(ShowOptions);
            creditsButton.onClick.AddListener(ShowCredits);
            optionsBackButton.onClick.AddListener(ShowMain);
            creditsBackButton.onClick.AddListener(ShowMain);
            quitButton.onClick.AddListener(Quit);
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(Play);
            optionsButton.onClick.RemoveListener(ShowOptions);
            creditsButton.onClick.RemoveListener(ShowCredits);
            optionsBackButton.onClick.RemoveListener(ShowMain);
            creditsBackButton.onClick.RemoveListener(ShowMain);
            quitButton.onClick.RemoveListener(Quit);
        }

        private void ShowMain() => menuManager.ShowScreen(mainScreen, animated);
        private void ShowOptions() => menuManager.ShowScreen(optionsScreen, animated);
        private void ShowCredits() => menuManager.ShowScreen(creditsScreen, animated);

        private void Play()
        {
            TransitionManager.TransitionToScene(gameScene, fadeOutSequence);
        }
        
        private void Quit()
        {
            TransitionManager.TransitionQuit(fadeOutSequence);
        }
    }
}