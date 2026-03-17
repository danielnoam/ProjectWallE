using UnityEngine;

public class RadarTarget : MonoBehaviour
{
    [SerializeField] private RadarBlipColor blipColor = RadarBlipColor.White;

    public RadarBlipColor BlipColor => blipColor;

    private void OnEnable()
    {
        RadarSystem.Instance?.Register(this);
    }

    private void OnDisable()
    {
        RadarSystem.Instance?.Unregister(this);
    }
}