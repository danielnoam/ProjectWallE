using System;
using UnityEngine;

namespace _2_Scripts
{
    
    
    [Serializable]
    public class TireAndVisualTransform
    {
        public Transform tireTransform;
        public Transform visualTransform;
        private Vector3 _visualStartPosition;
        public bool isGrounded;
        public bool isMoving;
        public Vector3 VisualStartPosition => _visualStartPosition;
        

        public TireAndVisualTransform(TireAndVisualTransform tireAndVisualTransform)
        {
            tireTransform = tireAndVisualTransform.tireTransform;
            visualTransform = tireAndVisualTransform.visualTransform;
            _visualStartPosition = visualTransform.localPosition;
            isGrounded = false;
            isMoving = false;
        }
    }
}