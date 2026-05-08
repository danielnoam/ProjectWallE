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
        [SerializeField] private EffectType effectType = EffectType.Trigger;
        [SerializeField, ShowIf(nameof(effectType), EffectType.Spawn)] private PoolableVisualEffect visualEffect;
        [SerializeField, ShowIf(nameof(effectType), EffectType.Trigger)] private VisualEffect[] visualEffects;
        [SerializeField, AudioLibraryID] private string soundId;

        public void Play(Vector3 position, AudioSource audioSource = null)
        {
            
            switch (effectType)
            {
                case EffectType.Trigger:
                    if (visualEffects != null && visualEffects.Length != 0)
                    {
                        foreach (var effect in visualEffects)
                        {
                            effect?.Play();
                        }
                    }
                    
                    break;
                case EffectType.Spawn:
                    
                    if (visualEffect)
                    {
                        var effect = ObjectPooler.GetObjectFromPool(visualEffect, position);
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
        [SerializeField] private SOColor materialColor;
        [SerializeField, AudioLibraryID] private string hitSoundId;

        public static readonly int EmissionStrength = Shader.PropertyToID("_Emission_Strength");
        public static readonly int EmissionColorID = Shader.PropertyToID("_Emission_Color");
        

        private void PunchEmission(Material material)
        {
            if (!material || !materialColor) return;

            material.SetColor(EmissionColorID, materialColor.Value);
            var seq = Sequence.Create();
            seq.Group(Tween.MaterialProperty(material, EmissionStrength, punchStrength, punchDuration * 0.5f, punchEase));
            seq.Chain(Tween.MaterialProperty(material, EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
        }

        private void PunchEmission(Material[] materials)
        {
            if (materials == null || materials.Length == 0 || !materialColor) return;

            foreach (var mat in materials)
                mat.SetColor(EmissionColorID, materialColor.Value);

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
}