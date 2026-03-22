using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Utilities.CustomFields;

public class RadarSystem : MonoBehaviour
{
    public static RadarSystem Instance { get; private set; }

    [Header("Settings")]
    public float radarRange = 55f;
    public PositionField worldCenter;
    
    [Header("UI")]
    public float radiusMultiplier = 1;
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
        _radarRadius = radarPanel.rect.width * 0.5f * radiusMultiplier;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        UpdateBlips();
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
                if (target.ShowOutOfRange)
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
    
    public void Register(RadarTarget target)
    {
        if (!_targets.Add(target)) return;

        var blip = Instantiate(blipPrefab, blipHolder ? blipHolder : transform);
        blip.transform.localScale *= target.BlipSizeMultiplier;
        blip.color = target.BlipColor;
        if (blip is Image image && target.BlipSprite) image.sprite = target.BlipSprite;
        _blips[target] = blip;
    }

    public void Unregister(RadarTarget target)
    {
        if (!_targets.Remove(target)) return;

        if (_blips.Remove(target, out Graphic blip)) Destroy(blip.gameObject);
    }
}