using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.CustomFields;
using PrimeTween;
using UnityEngine;
using UnityEngine.VFX;

namespace ProjectWallE.GameLoop
{
    public enum EffectType
    {
        Trigger,
        Spawn
    }
    
    [Serializable]
    public class ParticleEffectAction
    {
        [SerializeField] private EffectType effectType = EffectType.Spawn;
        [SerializeField, ShowIf(nameof(effectType), EffectType.Spawn)] private PoolableParticleSystem particle;
        [SerializeField, ShowIf(nameof(effectType), EffectType.Trigger)] private ParticleSystem triggerParticle;
        [SerializeField, AudioLibraryID] private string soundId;

        public void Play(Vector3 position, Quaternion rotation, AudioSource audioSource = null)
        {
            switch (effectType)
            {
                case EffectType.Trigger:
                    triggerParticle?.Play();
                    
                    break;
                case EffectType.Spawn:
                    
                    if (particle)
                    {
                        var effect = ObjectPooler.GetObjectFromPool(particle, position, rotation);
                        effect?.Play();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            
            if (audioSource)
            {
                AudioLibrary.PlayOnSource(soundId, audioSource);
            }
            else
            {
                AudioLibrary.PlayAtPosition(soundId, position);
            }
        }

        public void Play(Vector3 position, AudioSource audioSource = null)
        {
            Play(position, Quaternion.identity, audioSource);
        }
    }
    
    [Serializable]
    public class VisualEffectAction
    {
        [SerializeField] private VisualEffect[] visualEffects;
        [SerializeField, AudioLibraryID] private string soundId;

        public void Play(Vector3 position, AudioSource audioSource = null)
        {
            if (visualEffects != null && visualEffects.Length != 0)
            {
                foreach (var effect in visualEffects)
                {
                    effect?.Play();
                }
            }
            
            if (audioSource)
            {
                AudioLibrary.PlayOnSource(soundId, audioSource);
            }
            else
            {
                AudioLibrary.PlayAtPosition(soundId, position);
            }
        }

        public void Stop()
        {
            if (visualEffects == null || visualEffects.Length == 0) return;
            
            foreach (var effect in visualEffects)
            {
                effect?.Stop();
            }
        }

        public void Stop(AudioSource audioSource)
        {
            audioSource?.Stop();
            Stop();
        }
    }

    [Serializable]
    public class DamageEffects
    {
        [SerializeField] private float punchStrength = 1f;
        [SerializeField] private float punchDuration = 0.15f;
        [SerializeField] private Ease punchEase = Ease.Linear;
        [SerializeField] private SOColorHDR emissionColor;
        [SerializeField, AudioLibraryID] private string hitSoundId;

        public static readonly int EmissionStrength = Shader.PropertyToID("_Emission_Strength");
        public static readonly int EmissionColorID = Shader.PropertyToID("_Emission_Color");
        

        private void PunchEmission(Material material)
        {
            if (!material || !emissionColor) return;

            material.SetColor(EmissionColorID, emissionColor.Value);
            var seq = Sequence.Create();
            seq.Group(Tween.MaterialProperty(material, EmissionStrength, punchStrength, punchDuration * 0.5f, punchEase));
            seq.Chain(Tween.MaterialProperty(material, EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
        }

        private void PunchEmission(Material[] materials)
        {
            if (materials == null || materials.Length == 0 || !emissionColor) return;

            foreach (var mat in materials)
                mat.SetColor(EmissionColorID, emissionColor.Value);

            var seq = Sequence.Create();
            foreach (var mat in materials)
                seq.Group(Tween.MaterialProperty(mat, EmissionStrength, punchStrength, punchDuration * 0.5f, punchEase));

            seq.Chain(Tween.MaterialProperty(materials[0], EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
            for (var i = 1; i < materials.Length; i++)
                seq.Group(Tween.MaterialProperty(materials[i], EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
        }
        
        public void Play(Vector3 position, Material[] materials)
        {
            PunchEmission(materials);
            AudioLibrary.PlayAtPosition(hitSoundId, position);
        }

        public void Play(Vector3 position, Material material)
        {
            PunchEmission(material);
            AudioLibrary.PlayAtPosition(hitSoundId, position);
        }
        
        public void PunchOnly(Material[] materials)
        {
            PunchEmission(materials);
        }
        
        public void PlayHitSound(Vector3 position)
        {
            AudioLibrary.PlayAtPosition(hitSoundId, position);
        }
    }

    [Serializable]
    public class ColorPunchEffect
    {
        [SerializeField] private float punchDuration = 0.15f;
        [SerializeField] private Ease punchEase = Ease.Linear;
        [SerializeField, ColorUsage(true,true)] private Color punchColor = Color.white;
        
        private Color _baseColor =  Color.white;
        public static readonly int ColorID = Shader.PropertyToID("_Color");


        public void Play(Material material)
        {
            if (!material) return;

            if (_baseColor == Color.white) _baseColor = material.GetColor(ColorID);
            
            var seq = Sequence.Create();
            seq.Group(Tween.MaterialProperty(material, ColorID, punchColor, punchDuration * 0.5f, punchEase));
            seq.Chain(Tween.MaterialProperty(material, ColorID, _baseColor, punchDuration * 0.5f, punchEase));
        }
    }
    
    [Serializable]
    public class ColorSetEffect
    {
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private Ease ease = Ease.Linear;
        [SerializeField, ColorUsage(true,true)] private Color color = Color.white;
        
        public static readonly int ColorID = Shader.PropertyToID("_Color");


        public void Play(Material material)
        {
            if (!material) return;
            
            var seq = Sequence.Create();
            seq.Group(Tween.MaterialProperty(material,ColorID, color, duration, ease));
        }
    }
    
    [Serializable]
    public class StructureBreakEffect
    {
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField, ColorUsage(true, true)] private Color brokenColor = Color.red;
        [SerializeField] private string materialProperty = "_Emission_Color";
        [SerializeField] private Renderer[] emissiveRenderers;
        [SerializeField, AudioLibraryID] private string brokenSoundId; 

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
            
            AudioLibrary.PlayAtPosition(brokenSoundId, position);
        }

        public void SetNormal()
        {
            if (_materials == null) return;
            for (int i = 0; i < _materials.Length; i++)
            {
                Tween.MaterialProperty(_materials[i], _propertyId, _baseColors[i], duration, ease);
            }
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

        public void Play()
        {
            if (!gfx) return;
            
            Sequence.Create().Group(Tween.Scale(gfx, Vector3.zero, Vector3.one, duration, ease));
            AudioLibrary.PlayAtPosition(buildSoundId, gfx);

            if (playAnimation.IsSetAndHasValue())
            {
                playAnimation.Value.PlayOnce();
            }
        }
    }
}