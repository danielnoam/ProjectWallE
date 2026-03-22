using UnityEngine;

public class RadarTarget : MonoBehaviour
{
    [SerializeField] private Sprite blipSprite;
    [SerializeField] private Color blipColor = Color.white;
    [SerializeField, Range(0.1f,2.0f)] private float blipSizeMultiplier = 1.0f;
    [SerializeField] private bool showOutOfRange;

    public Sprite BlipSprite => blipSprite;
    public Color BlipColor => blipColor;
    public float BlipSizeMultiplier => blipSizeMultiplier;
    public bool ShowOutOfRange => showOutOfRange;

    private void OnEnable()
    {
        RadarSystem.Instance?.Register(this);
    }

    private void OnDisable()
    {
        RadarSystem.Instance?.Unregister(this);
    }
}