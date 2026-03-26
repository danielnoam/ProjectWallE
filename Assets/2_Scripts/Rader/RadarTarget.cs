using DNExtensions.Utilities.Button;
using UnityEngine;

public class RadarTarget : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Sprite blipSprite;
    [SerializeField] private Color blipColor = Color.white;
    [SerializeField, Range(0.1f,2.0f)] private float blipSizeMultiplier = 1.0f;
    [SerializeField] private bool showOutOfRange;
    
    [Header("Ping")]
    [SerializeField] private bool pingOnRegister;
    [SerializeField] private bool pingOnUnregister;
    [SerializeField, Range(0.1f, 1f)] private float pingStartStrength = 1f;

    public Sprite BlipSprite => blipSprite;
    public Color BlipColor => blipColor;
    public float BlipSizeMultiplier => blipSizeMultiplier;
    public bool ShowOutOfRange => showOutOfRange;
    public bool PingOnRegister => pingOnRegister;
    public bool PingOnUnregister => pingOnUnregister;
    public float PingStartStrength => pingStartStrength;
    
    

    private void OnEnable()
    {
        RadarSystem.Instance?.Register(this);
    }

    private void OnDisable()
    {
        RadarSystem.Instance?.Unregister(this);
    }
    
    
    [Button]
    private void TestRegister()
    {
        RadarSystem.Instance?.Register(this);
    }

    [Button]
    private void TestUnregister()
    {
        RadarSystem.Instance?.Unregister(this);
    }
}