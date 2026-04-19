using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
    [DefaultExecutionOrder(200)]
    public class PlayerAimer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float minAimDistance = 3f;
        [SerializeField] private float maxAimDistance = 200f;
        [SerializeField] private LayerMask aimMask = ~0;

        private Camera _cam;

        public Ray CameraRay { get; private set; }
        public Vector3 AimPoint { get; private set; }
        public bool HasHit { get; private set; }
        public RaycastHit LastHit { get; private set; }

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (!_cam)
            {
                _cam = Camera.main;
                if (!_cam) return;
            }

            CameraRay = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            bool rawHit = Physics.Raycast(CameraRay, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore);

            if (rawHit && hit.distance >= minAimDistance)
            {
                HasHit = true;
                LastHit = hit;
                AimPoint = hit.point;
            }
            else
            {
                HasHit = false;
                AimPoint = CameraRay.GetPoint(maxAimDistance);
            }
        }

        public Vector3 GetDirectionFrom(Vector3 position)
        {
            return (AimPoint - position).normalized;
        }
    }
}