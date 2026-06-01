using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace ProjectWallE.GameLoop.UI
{
    public class ObjectiveGameMarker : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showDistance;
        [SerializeField] private float tweenDuration = 0.3f;

        [Header("References")]
        [SerializeField, AutoGetSelf] private Note note;
        [SerializeField, AutoGetSelf] private RadarTarget radarTarget;
        [SerializeField] private GameObject inGameMarker;
        [SerializeField] private TextMeshProUGUI distanceLabel;

        private Transform _player;
        private bool _active;
        private Vector3 _markerBaseScale;
        private Tween _markerTween;

        private void Awake()
        {
            if (inGameMarker)
            {
                _markerBaseScale = inGameMarker.transform.localScale;
                inGameMarker.transform.localScale = Vector3.zero;
                inGameMarker.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_active || !showDistance || !distanceLabel || !_player) return;

            float dist = Vector3.Distance(_player.position, transform.position);
            distanceLabel.text = $"{dist:F0}m";
        }

        public void OnObjectiveStarted(bool showInGame, bool showOnRadar)
        {
            _active = true;

            if (inGameMarker && showInGame)
            {
                inGameMarker.SetActive(true);
                _markerTween.Stop();
                _markerTween = Tween.Scale(inGameMarker.transform, _markerBaseScale, tweenDuration, Ease.OutBack);
            }

            if (distanceLabel) distanceLabel.gameObject.SetActive(showInGame && showDistance);

            if (showOnRadar && radarTarget)
            {
                radarTarget.EnableBlip();
                radarTarget.PingBlip();
            }

            if (!_player)
                _player = PlayerManager.Instance ? PlayerManager.Instance.transform : null;
        }

        public void OnObjectiveCompleted(bool showInGame, bool showOnRadar)
        {
            _active = false;

            if (inGameMarker && showInGame)
            {
                _markerTween.Stop();
                _markerTween = Tween.Scale(inGameMarker.transform, Vector3.zero, tweenDuration, Ease.InBack)
                    .OnComplete(inGameMarker, static target => target.SetActive(false));
            }

            if (showOnRadar && radarTarget) radarTarget.DisableBlip();
        }
    }
}