using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectWallE.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AirSupportPrompt : MonoBehaviour
    {
        public static AirSupportPrompt Instance { get; private set; }
        
        [Header("Arrow Billboard")]
        [SerializeField] private Vector3 offset;
        [SerializeField] private float rotationSpeed = 25f;
        [SerializeField, MinMaxRange(0, 10)] private RangedFloat minMaxScaleMultiplier = new RangedFloat(1, 1.5f);
        [SerializeField, MinMaxRange(0, 1000)] private RangedFloat minMaxDistance = new RangedFloat(5, 15);
        
        [Header("References")]
        [SerializeField, AutoGetSelf] private DecalProjector decalProjector;
        [SerializeField, AutoGetSelf] private CanvasGroup canvasGroup;
        [SerializeField] private Transform arrowTransform;

        private Camera _cam;
        private Vector3 _baseScale;
        private Vector3 _surfaceNormal = Vector3.up;
        private bool _visible;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _cam = Camera.main;
            if (arrowTransform) _baseScale = arrowTransform.localScale;
            canvasGroup.alpha = 0;
        }

        private void Update()
        {
            if (!_visible || !arrowTransform || !_cam) return;

            UpdateArrowRotation();
            UpdateArrowScale();
        }

        private void UpdateArrowRotation()
        {
            Vector3 directionToCamera = arrowTransform.position - _cam.transform.position;
            Vector3 projected = Vector3.ProjectOnPlane(directionToCamera, _surfaceNormal).normalized;
            if (projected == Vector3.zero) return;

            Quaternion target = Quaternion.LookRotation(projected, _surfaceNormal);
            arrowTransform.rotation = Quaternion.Slerp(arrowTransform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        private void UpdateArrowScale()
        {
            float distance = Vector3.Distance(arrowTransform.position, _cam.transform.position);
            float t = Mathf.InverseLerp(minMaxDistance.minValue, minMaxDistance.maxValue, distance);
            float multiplier = Mathf.Lerp(minMaxScaleMultiplier.minValue, minMaxScaleMultiplier.maxValue, t);
            arrowTransform.localScale = _baseScale * multiplier;
        }

        public void Show(Vector3 position, Vector3 normal)
        {
            if (!canvasGroup) return;

            transform.position = position;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            if (arrowTransform) arrowTransform.position = position + transform.rotation * offset;
            _surfaceNormal = normal;
            _visible = true;
            canvasGroup.alpha = 1;
            decalProjector.enabled = true;
        }

        public void Hide()
        {
            if (!canvasGroup) return;

            _visible = false;
            canvasGroup.alpha = 0;
            decalProjector.enabled = false;
        }
    }
}