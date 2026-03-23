using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using PrimeTween;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public class EnemyEffects
    {
        [Header("Emission")]
        [SerializeField] private float punchStrength = 1f;
        [SerializeField] private float punchDuration = 0.15f;
        [SerializeField] private Ease punchEase = Ease.Linear;
        [SerializeField, ColorUsage(false, true)] private Color normalEmissionColor = Color.red;
        [SerializeField, ColorUsage(false, true)] private Color criticalEmissionColor = Color.red;

        [Header("SFX")]
        [SerializeField, AudioLibraryID] private string normalDamagedSoundId;
        [SerializeField, AudioLibraryID] private string criticalDamageSoundId;

        private static readonly int EmissionStrength = Shader.PropertyToID("_Emission_Strength");
        private static readonly int EmissionColor = Shader.PropertyToID("_Emission_Color");
        
        private void PunchEmission(Material[] materials, Color color)
        {
            if (materials == null || materials.Length == 0) return;

            foreach (var mat in materials)
                mat.SetColor(EmissionColor, color);

            var seq = Sequence.Create();
            foreach (var mat in materials)
                seq.Group(Tween.MaterialProperty(mat, EmissionStrength, punchStrength, punchDuration * 0.5f, punchEase));

            seq.Chain(Tween.MaterialProperty(materials[0], EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
            for (var i = 1; i < materials.Length; i++)
                seq.Group(Tween.MaterialProperty(materials[i], EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
        }
        
        public void PlayHitAll(Vector3 position, List<EnemyDamageRelay> relays)
        {
            foreach (var relay in relays)
            {
                PunchEmission(relay.Materials, normalEmissionColor);
            }
            AudioLibrary.PlayAtPosition(normalDamagedSoundId, position);
        }
        
        public void PlayHit(Vector3 position, EnemyDamageRelay relay)
        {
            Color color = relay.HitZone == EnemyHitZone.Critical ? criticalEmissionColor : normalEmissionColor;
            string soundId = relay.HitZone == EnemyHitZone.Critical ? criticalDamageSoundId : normalDamagedSoundId;
            
            PunchEmission(relay.Materials, color);
            AudioLibrary.PlayAtPosition(soundId, position);
        }
    }
}