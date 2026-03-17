using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Utilities.CustomFields;

public enum RadarBlipColor
{
    White,
    Red,
    Green,
    Blue,
    Yellow,
    Cyan
}

public class RadarSystem : MonoBehaviour
{
    public static RadarSystem Instance { get; private set; }

    [Header("Settings")]
    public float radarRange = 55f;
    public PositionField worldCenter;
    
    [Header("UI")]
    public float radiusMultiplier = 1;
    public bool showOutOfRange;
    public OptionalField<Transform> rotationTarget;
    public RectTransform radarPanel;
    public Transform blipHolder;
    public Graphic blipPrefab;

    private readonly HashSet<RadarTarget> _targets = new();
    private readonly Dictionary<RadarTarget, Graphic> _blips = new();

    private float _radarRadius;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        _radarRadius = radarPanel.rect.width * 0.5f * radiusMultiplier;
        UpdateBlips();
    }

    public void Register(RadarTarget target)
    {
        if (!_targets.Add(target)) return;

        var blip = Instantiate(blipPrefab, blipHolder ? blipHolder : transform);
        blip.color = GetColor(target.BlipColor);
        _blips[target] = blip;
    }

    public void Unregister(RadarTarget target)
    {
        if (!_targets.Remove(target)) return;

        if (_blips.Remove(target, out Graphic blip)) Destroy(blip.gameObject);
    }

    private void UpdateBlips()
    {
        Vector3 center = worldCenter.Position;
        float angle = rotationTarget && rotationTarget.Value ? rotationTarget.Value.eulerAngles.y : 0f;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        foreach (RadarTarget target in _targets)
        {
            if (!_blips.TryGetValue(target, out Graphic blip)) continue;

            Vector3 offset = target.transform.position - center;
            Vector2 radarOffset = new Vector2(offset.x, offset.z) / radarRange * _radarRadius;
            radarOffset = rotation * radarOffset;
            float distance = radarOffset.magnitude;

            if (distance > _radarRadius)
            {
                if (showOutOfRange)
                {
                    radarOffset = radarOffset.normalized * _radarRadius;
                    blip.enabled = true;
                }
                else
                {
                    blip.enabled = false;
                    continue;
                }
            }
            else
            {
                blip.enabled = true;
            }

            blip.rectTransform.anchoredPosition = radarOffset;
        }
    }

    private static Color GetColor(RadarBlipColor blipColor) => blipColor switch
    {
        RadarBlipColor.White => Color.white,
        RadarBlipColor.Red => Color.red,
        RadarBlipColor.Green => Color.green,
        RadarBlipColor.Blue => Color.blue,
        RadarBlipColor.Yellow => Color.yellow,
        RadarBlipColor.Cyan => Color.cyan,
        _ => Color.white
    };
}