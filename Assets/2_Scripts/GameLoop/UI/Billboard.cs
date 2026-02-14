using DNExtensions.Utilities;
using DNExtensions.Utilities.RangedValues;
using UnityEngine;

namespace ProjectWallE.UI
{
    [DisallowMultipleComponent]
    public class Billboard : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private bool rotateToCamera = true;
        [SerializeField, EnableIf("rotateToCamera")]
        private float rotationSpeed = 25f;

        [Header("Scale Settings")]
        [SerializeField] private bool distanceToCameraAffectsScale = true;
        [SerializeField, MinMaxRange(1, 2), EnableIf("distanceToCameraAffectsScale")]
        private RangedFloat minMaxScaleMultiplier = new RangedFloat(1, 1.5f);
        [SerializeField, MinMaxRange(0, 50), EnableIf("distanceToCameraAffectsScale")]
        private RangedFloat minMaxDistance = new RangedFloat(5, 15);

        private Camera _cam;
        private Vector3 _baseScale;

        private void Awake()
        {
            _cam = Camera.main;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            if (!_cam) return;

            if (rotateToCamera) RotateToCamera();
            if (distanceToCameraAffectsScale) UpdateScale();
        }

        private void RotateToCamera()
        {
            Vector3 directionToCamera = transform.position - _cam.transform.position;
            directionToCamera.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void UpdateScale()
        {
            float distance = Vector3.Distance(transform.position, _cam.transform.position);
            float t = Mathf.InverseLerp(minMaxDistance.minValue, minMaxDistance.maxValue, distance);
            float scaleMultiplier = Mathf.Lerp(minMaxScaleMultiplier.minValue, minMaxScaleMultiplier.maxValue, t);
            transform.localScale = _baseScale * scaleMultiplier;
        }
    }
}