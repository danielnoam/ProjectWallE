
using System;
using UnityEngine;

namespace ProjectWallE
{
    public struct PlayerReferences
    {
        public Rigidbody rigidBody;
        public Transform cameraTransform;
        public LayerMask groundLayer;
    }
    
    public interface IPlayerController
    {
        bool CanMove { get; set; }
        bool BuildEnabled {get;}
        bool ShootEnabled {get;}
        bool SupportEnabled { get; }
        public void ApplyFixedUpdate();
        public void ApplyUpdate();
        public void ApplyLateUpdate();
        public void ApplyLateFixedUpdate();
        public void Initialize(PlayerReferences playerReferences);

        public GameObject gameObject { get; }
        public Vector3 CenterOfMassOffset { get; }

        public void OnEnter();
        public void OnExit();
    }
}
