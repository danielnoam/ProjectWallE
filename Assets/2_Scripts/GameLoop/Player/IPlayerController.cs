
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
        public void ApplyMovement();
        public void Initialize(PlayerReferences playerReferences);
    }
}
