using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdeaZoo.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayPerformanceGovernor : MonoBehaviour
    {
        public const int MobileTargetFps = 30;
        public const int DesktopTargetFps = 60;
        public const int MaximumEncounterButtons = 4;
        public const int MaximumVisibleMemoryCards = 12;
        public const int MaximumSavedCases = 20;

        public event Action<string> QualityChanged;

        private readonly Dictionary<ParticleSystem, int> _particleBudgets = new Dictionary<ParticleSystem, int>();
        private float _sampleStarted;
        private int _frames;
        private int _badSamples;
        private int _goodSamples;
        private bool _reduced;
        private float _desktopLodBeforeReduction;
        private int _target;

        public string StatusLabel { get; private set; } = "QUALITY · FULL";
        public float SmoothedFps { get; private set; } = 60f;

        public void Begin()
        {
            Time.maximumDeltaTime = 0.10f;
            _target = CurrentTarget();
            _sampleStarted = Time.unscaledTime;
            _frames = 0;
        }

        private void Update()
        {
            _frames++;
            var elapsed = Time.unscaledTime - _sampleStarted;
            if (elapsed < 1f) return;

            _target = CurrentTarget();
            var sample = _frames / Mathf.Max(0.01f, elapsed);
            SmoothedFps = Mathf.Lerp(SmoothedFps, sample, 0.35f);
            _frames = 0;
            _sampleStarted = Time.unscaledTime;

            if (SmoothedFps < _target * 0.82f)
            {
                _badSamples++;
                _goodSamples = 0;
            }
            else if (SmoothedFps > _target * 0.94f)
            {
                _goodSamples++;
                _badSamples = 0;
            }
            else
            {
                _badSamples = Mathf.Max(0, _badSamples - 1);
                _goodSamples = Mathf.Max(0, _goodSamples - 1);
            }

            if (!_reduced && _badSamples >= 3) ReduceNonessentialLoad();
            else if (_reduced && _goodSamples >= 8) RestoreQuality();
        }

        private void ReduceNonessentialLoad()
        {
            _reduced = true;
            _badSamples = 0;

            // MobileQualityController owns render scale, tier, shadows and mobile LOD.
            // Only desktop/WebGL LOD is adjusted here to avoid competing governors.
            if (!Application.isMobilePlatform)
            {
                _desktopLodBeforeReduction = QualitySettings.lodBias;
                QualitySettings.lodBias = Mathf.Max(0.72f, _desktopLodBeforeReduction * 0.82f);
            }

            var particles = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < particles.Length; i++)
            {
                var system = particles[i];
                if (system == null) continue;
                var main = system.main;
                if (!_particleBudgets.ContainsKey(system)) _particleBudgets[system] = main.maxParticles;
                main.maxParticles = Mathf.Max(8, Mathf.RoundToInt(main.maxParticles * 0.65f));
            }

            StatusLabel = "QUALITY · ADAPTIVE " + SmoothedFps.ToString("0") + "/" + _target + " FPS";
            QualityChanged?.Invoke(StatusLabel);
        }

        private void RestoreQuality()
        {
            _reduced = false;
            _goodSamples = 0;
            if (!Application.isMobilePlatform && _desktopLodBeforeReduction > 0f)
                QualitySettings.lodBias = _desktopLodBeforeReduction;

            foreach (var pair in _particleBudgets)
            {
                if (pair.Key == null) continue;
                var main = pair.Key.main;
                main.maxParticles = pair.Value;
            }
            _particleBudgets.Clear();

            StatusLabel = "QUALITY · FULL " + SmoothedFps.ToString("0") + "/" + _target + " FPS";
            QualityChanged?.Invoke(StatusLabel);
        }

        private static int CurrentTarget()
        {
            if (Application.targetFrameRate > 0) return Application.targetFrameRate;
            return Application.isMobilePlatform ? MobileTargetFps : DesktopTargetFps;
        }
    }
}
