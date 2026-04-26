using System;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE
{
    public class CameraAutoAlign : CinemachineExtension
    {
        [SerializeField] private float alignCooldown = 1f;
        [SerializeField] private float alignStrength = 2f;
        [SerializeField] private float xOffset = 20f;

        private float _lastInputTime;

        public void NotifyInput() => _lastInputTime = Time.time;

        public bool CanAlign(out float offset)
        {
            offset = xOffset;
            return Time.time - _lastInputTime >= alignCooldown;
        }

        public float Strength => alignStrength;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime) { }
    }
}
