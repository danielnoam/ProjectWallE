using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Utilities.CustomFields;
using PrimeTween;
using UnityEngine;
using UnityEngine.VFX;

namespace ProjectWallE.GameLoop
{
    
    [Serializable]
    public class StructureBreakEffect
    {
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField, ColorUsage(true, true)] private Color brokenColor = Color.red;
        [SerializeField] private string materialProperty = "_Emission_Color";
        [SerializeField] private Renderer[] emissiveRenderers;
        [SerializeField, AudioLibraryID] private string brokenSoundId; 
        [SerializeField] private VisualEffect smokeEffect;

        private int _propertyId;
        private Material[] _materials;
        private Color[] _baseColors;

        public void Initialize()
        {
            if (emissiveRenderers == null || emissiveRenderers.Length == 0) return;

            _propertyId = Shader.PropertyToID(materialProperty);
            
            var mats = new List<Material>();
            foreach (var rend in emissiveRenderers)
            {
                if (rend) mats.AddRange(rend.materials);
            }

            _materials = mats.ToArray();
            _baseColors = new Color[_materials.Length];
            for (int i = 0; i < _materials.Length; i++)
            {
                _baseColors[i] = _materials[i].GetColor(_propertyId);
            }
        }

        public void SetBroken(Vector3 position)
        {
            if (_materials == null) return;
            foreach (var mat in _materials)
            {
                Tween.MaterialProperty(mat, _propertyId, brokenColor, duration, ease);
            }
            
            if (smokeEffect) smokeEffect.Play();
            
            AudioLibrary.PlayAtPosition(brokenSoundId, position);
        }

        public void SetNormal()
        {
            if (_materials == null) return;
            for (int i = 0; i < _materials.Length; i++)
            {
                Tween.MaterialProperty(_materials[i], _propertyId, _baseColors[i], duration, ease);
            }
            
            if (smokeEffect) smokeEffect.Stop();
        }
    }
    
    [Serializable]
    public class StructureUpgradeEffect
    {
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private float punchStrength = 0.9f;
        [SerializeField] private Ease ease = Ease.OutBack;
        [SerializeField] private Transform gfx;
        [SerializeField, AudioLibraryID] private string upgradeSoundId; 

        public void Play()
        {
            if (!gfx) return;
            
            Sequence.Create().Group(Tween.PunchScale(gfx, Vector3.one * punchStrength, duration,1,true, ease));
            AudioLibrary.PlayAtPosition(upgradeSoundId, gfx);
        }
    }
    
    [Serializable]
    public class StructureBuildEffect
    {
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private Ease ease = Ease.OutBack;
        [SerializeField] private Transform gfx;
        [SerializeField, AudioLibraryID] private string buildSoundId; 
        [SerializeField] private OptionalField<SimpleAnimatorClipField> playAnimation;

        public void Play(Action onAnimationFinish = null)
        {
            if (!gfx) return;
            
            Sequence.Create().Group(Tween.Scale(gfx, Vector3.zero, Vector3.one, duration, ease));
            AudioLibrary.PlayAtPosition(buildSoundId, gfx);

            if (playAnimation.IsSetAndHasValue())
            {
                playAnimation.Value.PlayOnce(0, onAnimationFinish);
            }
            else
            {
                onAnimationFinish?.Invoke();
            }
        }
    }
}