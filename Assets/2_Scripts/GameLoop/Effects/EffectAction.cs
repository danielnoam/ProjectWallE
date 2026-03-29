using System;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using PrimeTween;
using UnityEngine;
using UnityEngine.VFX;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public class EffectAction
    {
        [SerializeField] private PoolableParticleSystem particle;
        [SerializeField, AudioLibraryID] private string soundId;

        public void Play(Vector3 position)
        {
            if (particle)
            {
                var effect = ObjectPooler.GetObjectFromPool(particle, position);
                effect?.Play();
            }
            AudioLibrary.PlayAtPosition(soundId, position);
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
                foreach (var effect in visualEffects)
                {
                    effect?.Play();
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
}