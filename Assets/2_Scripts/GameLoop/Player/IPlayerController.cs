
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
        bool canBuild {get;}
        bool canShoot {get;}
        public void ApplyFixedUpdate();
        public void ApplyUpdate();
        public void Initialize(PlayerReferences playerReferences);

        public GameObject gameObject { get; }
        public Vector3 CenterOfMassOffset { get; }

        public void OnEnter();
        public void OnExit();
    }
}
