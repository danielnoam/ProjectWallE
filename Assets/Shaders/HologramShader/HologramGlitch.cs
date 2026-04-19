using System.Collections;
using UnityEngine;
using DNExtensions.Utilities.Button;

namespace ProjectWallE
{
    public class HologramGlitch : MonoBehaviour
    {
        [System.Serializable]
        private struct GlitchFrame
        {
            public float strength;
            public float scanlineOffset;
            public float duration;
        }
        

        [Header("Glitch Sequence")]
        [SerializeField] private GlitchFrame[] sequence = new GlitchFrame[]
        {
            new GlitchFrame { strength = 0.00f, scanlineOffset = 0.0f, duration = 0.25f },
            new GlitchFrame { strength = 0.15f, scanlineOffset = 0.5f, duration = 0.25f },
            new GlitchFrame { strength = 0.00f, scanlineOffset = 0.0f, duration = 0.50f },
            new GlitchFrame { strength = 0.10f, scanlineOffset = 0.5f, duration = 0.10f },
            new GlitchFrame { strength = 0.00f, scanlineOffset = 0.0f, duration = 0.10f },
            new GlitchFrame { strength = 0.05f, scanlineOffset = 0.5f, duration = 0.10f },
            new GlitchFrame { strength = 0.00f, scanlineOffset = 0.0f, duration = 0.40f },
            new GlitchFrame { strength = 0.10f, scanlineOffset = 0.5f, duration = 0.30f },
        };

        private static readonly int GlitchStrength = Shader.PropertyToID("_Glitch_Strength");
        private static readonly int ScanlineOffset = Shader.PropertyToID("_Scanlines_Offset");
        private Renderer[] _renderers;
        private Material[] _materials;
        
        private void Awake()
        {
            CacheRenderers();
            StartCoroutine(GlitchRoutine());
        }
        
        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _materials = new Material[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
                _materials[i] = Application.isPlaying ? _renderers[i].material : _renderers[i].sharedMaterial;
        }

        private void SetGlitch(float strength, float scanline)
        {
            foreach (var mat in _materials)
            {
                mat.SetFloat(GlitchStrength, strength);
                mat.SetFloat(ScanlineOffset, scanline);
            }
        }

        private IEnumerator GlitchRoutine()
        {
            while (gameObject.activeInHierarchy)
            {
                foreach (var frame in sequence)
                {
                    SetGlitch(frame.strength, frame.scanlineOffset);
                    yield return new WaitForSeconds(frame.duration);
                }
            }
        }
    }
}