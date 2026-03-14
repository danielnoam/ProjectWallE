using System;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CustomFields;
using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animations")]
        [SerializeField] private AnimatorStateField switchToCar;
        [SerializeField] private AnimatorStateField switchToRobot;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;

        private void OnValidate() => AutoGetSystem.Process(this);

        private void OnEnable()
        {
            if (player) player.OnControllerChanged += OnControllerChanged;
        }

        private void OnDisable()
        {
            if (player) player.OnControllerChanged -= OnControllerChanged;
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            switch (type)
            {
                case PlayerControllerType.Car:
                    switchToCar.Animator?.Play(switchToCar.StateName);
                    break;
                case PlayerControllerType.Robot:
                    switchToRobot.Animator?.Play(switchToRobot.StateName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}