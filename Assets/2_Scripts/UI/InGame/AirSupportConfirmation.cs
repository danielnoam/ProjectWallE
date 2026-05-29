using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectWallE.UI
{
    public class AirSupportConfirmation : MonoBehaviour
    {
        public static AirSupportConfirmation Instance { get; private set; }

        [Header("Animation")]
        [SerializeField] private float duration = 0.6f;
        [SerializeField] private float maxScale = 2f;
        [SerializeField] private Ease scaleEase = Ease.OutCubic;
        [SerializeField] private Ease fadeEase = Ease.InCubic;

        [Header("References")]
        [SerializeField, AutoGetSelf] private DecalProjector decalProjector;

        private Vector3 _baseScale;
        private Sequence _sequence;

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
            _baseScale = transform.localScale;
            if (decalProjector) decalProjector.enabled = false;
        }

        public void Show(Vector3 position, Vector3 normal)
        {
            transform.position = position;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            transform.localScale = Vector3.zero;

            _sequence.Stop();

            if (decalProjector)
            {
                decalProjector.enabled = true;
                decalProjector.fadeFactor = 1f;
            }

            _sequence = Sequence.Create()
                .Group(Tween.Scale(transform, _baseScale * maxScale, duration, scaleEase))
                .Group(Tween.Custom(this, 1f, 0f, duration, (target, v) =>
                {
                    if (target.decalProjector) target.decalProjector.fadeFactor = v;
                }, fadeEase));
        }
    }
}