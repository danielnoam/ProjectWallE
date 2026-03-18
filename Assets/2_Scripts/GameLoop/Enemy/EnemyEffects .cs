using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class EnemyEffects : MonoBehaviour
    {
        [Header("SFX")]
        [SerializeField, AudioLibraryID] private string damagedSoundId;

        [Header("Emission Punch")]
        [SerializeField] private float punchStrength = 1f;
        [SerializeField] private float punchDuration = 0.15f;
        [SerializeField] private Ease punchEase = Ease.Linear;
        [SerializeField] private int punchCycles = 1;

        [Header("References")]
        [SerializeField] private Renderer[] excludedRenderers;
        [SerializeField, AutoGetSelf] private Enemy enemy;

        private static readonly int EmissionStrength = Shader.PropertyToID("_Emission_Strength");

        private Material[] _materials;
        private Sequence _punchSequence;

        private void OnValidate() => AutoGetSystem.Process(this);

        private void Start()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var mats = new List<Material>();
            foreach (var r in renderers)
            {
                if (Array.IndexOf(excludedRenderers, r) >= 0) continue;
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty(EmissionStrength))
                    {
                        mats.Add(mat);
                    }
                }
            }
            _materials = mats.ToArray();
        }

        private void OnEnable()
        {
            if (enemy) enemy.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (enemy) enemy.OnDamaged -= OnDamaged;
        }

        private void OnDamaged(float damage)
        {
            PunchEmission();
            AudioLibrary.PlayAtPosition(damagedSoundId, transform.position);
        }

        private void PunchEmission()
        {
            if (_materials == null || _materials.Length == 0) return;
            if (_punchSequence.isAlive) _punchSequence.Stop();

            _punchSequence = Sequence.Create(cycles: punchCycles);
            foreach (var mat in _materials)
            {
                _punchSequence.Group(Tween.MaterialProperty(mat, EmissionStrength, punchStrength, punchDuration * 0.5f, punchEase));
            }

            _punchSequence.Chain(Tween.MaterialProperty(_materials[0], EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
            for (var i = 1; i < _materials.Length; i++)
            {
                _punchSequence.Group(Tween.MaterialProperty(_materials[i], EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
            }
        }
    }
}