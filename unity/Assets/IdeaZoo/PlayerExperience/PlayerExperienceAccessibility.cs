using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IdeaZoo.PlayerExperience
{
    [DefaultExecutionOrder(-760)]
    [DisallowMultipleComponent]
    public sealed class PlayerExperienceAccessibilityController : MonoBehaviour
    {
        private static PlayerExperienceAccessibilityController _current;
        private readonly Dictionary<Text, int> _fontSizes = new Dictionary<Text, int>();
        private readonly Dictionary<Text, Color> _textColors = new Dictionary<Text, Color>();
        private readonly Dictionary<LayoutElement, float> _minimumHeights = new Dictionary<LayoutElement, float>();
        private readonly Dictionary<Animator, float> _animatorSpeeds = new Dictionary<Animator, float>();
        private readonly Dictionary<ParticleSystem, bool> _emissionStates = new Dictionary<ParticleSystem, bool>();
        private PlayerAccessibilitySettings _settings = new PlayerAccessibilitySettings();
        private bool _decisionFocus;

        public static PlayerAccessibilitySettings Settings
        {
            get { return _current != null ? _current._settings : new PlayerAccessibilitySettings(); }
        }

        public void Configure(PlayerAccessibilitySettings settings)
        {
            _current = this;
            _settings = settings ?? new PlayerAccessibilitySettings();
            RefreshCache();
            Apply();
        }

        public void ApplySettings(PlayerAccessibilitySettings settings)
        {
            _settings = settings ?? new PlayerAccessibilitySettings();
            RefreshCache();
            Apply();
        }

        public static void SetDecisionFocus(bool active)
        {
            if (_current == null) return;
            _current._decisionFocus = active;
            _current.ApplyMotionAndParticles();
        }

        public static void Pulse()
        {
            if (_current == null || !_current._settings.Haptics || !Application.isMobilePlatform) return;
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }

        private void RefreshCache()
        {
            foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (text == null || !text.gameObject.scene.IsValid()) continue;
                if (!_fontSizes.ContainsKey(text)) _fontSizes[text] = Mathf.Max(8, text.fontSize);
                if (!_textColors.ContainsKey(text)) _textColors[text] = text.color;
            }

            foreach (var layout in Resources.FindObjectsOfTypeAll<LayoutElement>())
            {
                if (layout == null || !layout.gameObject.scene.IsValid()) continue;
                if (!_minimumHeights.ContainsKey(layout)) _minimumHeights[layout] = layout.minHeight;
            }

            foreach (var animator in Resources.FindObjectsOfTypeAll<Animator>())
            {
                if (animator == null || !animator.gameObject.scene.IsValid()) continue;
                if (!_animatorSpeeds.ContainsKey(animator)) _animatorSpeeds[animator] = animator.speed;
            }

            foreach (var particle in Resources.FindObjectsOfTypeAll<ParticleSystem>())
            {
                if (particle == null || !particle.gameObject.scene.IsValid()) continue;
                if (!_emissionStates.ContainsKey(particle)) _emissionStates[particle] = particle.emission.enabled;
            }

            RemoveDestroyed();
        }

        private void RemoveDestroyed()
        {
            foreach (var key in _fontSizes.Keys.Where(item => item == null).ToArray()) _fontSizes.Remove(key);
            foreach (var key in _textColors.Keys.Where(item => item == null).ToArray()) _textColors.Remove(key);
            foreach (var key in _minimumHeights.Keys.Where(item => item == null).ToArray()) _minimumHeights.Remove(key);
            foreach (var key in _animatorSpeeds.Keys.Where(item => item == null).ToArray()) _animatorSpeeds.Remove(key);
            foreach (var key in _emissionStates.Keys.Where(item => item == null).ToArray()) _emissionStates.Remove(key);
        }

        private void Apply()
        {
            foreach (var pair in _fontSizes)
            {
                if (pair.Key == null) continue;
                pair.Key.fontSize = Mathf.Clamp(Mathf.RoundToInt(pair.Value * _settings.TextScale), 9, 42);
                var original = _textColors.ContainsKey(pair.Key) ? _textColors[pair.Key] : pair.Key.color;
                if (_settings.HighContrast)
                {
                    var luminance = original.r * .299f + original.g * .587f + original.b * .114f;
                    pair.Key.color = luminance > .45f ? Color.white : new Color(.02f, .02f, .02f, 1f);
                }
                else pair.Key.color = original;
            }

            foreach (var pair in _minimumHeights)
            {
                if (pair.Key == null) continue;
                var baseHeight = Mathf.Max(44f, pair.Value);
                pair.Key.minHeight = _settings.LargeTouchTargets ? Mathf.Max(64f, baseHeight * 1.18f) : pair.Value;
            }

            ApplyMotionAndParticles();
        }

        private void ApplyMotionAndParticles()
        {
            foreach (var pair in _animatorSpeeds)
            {
                if (pair.Key == null) continue;
                pair.Key.speed = _settings.ReducedMotion ? Mathf.Min(.55f, pair.Value) : pair.Value;
            }

            var reduceParticles = _settings.ReducedMotion || (_settings.FocusMode && _decisionFocus);
            foreach (var pair in _emissionStates)
            {
                if (pair.Key == null) continue;
                var emission = pair.Key.emission;
                var protectedEffect = pair.Key.transform.IsChildOf(transform) || pair.Key.name.IndexOf("Creature", System.StringComparison.OrdinalIgnoreCase) >= 0;
                emission.enabled = protectedEffect ? pair.Value : pair.Value && !reduceParticles;
            }
        }
    }
}
