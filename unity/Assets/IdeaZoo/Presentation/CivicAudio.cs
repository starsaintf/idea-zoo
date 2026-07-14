using System;
using IdeaZoo.Core;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CivicAudioBed : MonoBehaviour
    {
        private AudioSource _ambient;
        private AudioSource _cue;
        private AudioClip _hatch;
        private AudioClip _evidence;
        private AudioClip _molt;
        private AudioClip _decision;
        private AudioClip _complete;

        public void Build()
        {
            if (_ambient != null) return;
            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.loop = true;
            _ambient.playOnAwake = false;
            _ambient.volume = 0.16f;
            _ambient.spatialBlend = 0f;
            _ambient.clip = Generate("Civic_Ambience", 8f, delegate(float time)
            {
                var low = Mathf.Sin(time * Mathf.PI * 2f * 47f) * 0.18f;
                var middle = Mathf.Sin(time * Mathf.PI * 2f * 71f + Mathf.Sin(time * 0.5f)) * 0.06f;
                var paper = Mathf.PerlinNoise(time * 2.1f, 0.7f) * 0.035f - 0.0175f;
                var bell = Mathf.Sin(time * Mathf.PI * 2f * 141f) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * Mathf.PI * 0.25f)), 18f) * 0.04f;
                return (low + middle + paper + bell) * 0.55f;
            });

            _cue = gameObject.AddComponent<AudioSource>();
            _cue.playOnAwake = false;
            _cue.volume = 0.48f;
            _cue.spatialBlend = 0f;
            _hatch = Chord("Hatch_Cue", new[] { 110f, 164.81f, 220f, 329.63f }, 1.8f, 0.16f);
            _evidence = Chord("Evidence_Cue", new[] { 196f, 246.94f, 293.66f }, 0.65f, 0.11f);
            _molt = Chord("Molt_Cue", new[] { 130.81f, 174.61f, 261.63f, 349.23f }, 1.6f, 0.14f);
            _decision = Chord("Decision_Cue", new[] { 98f, 146.83f, 196f }, 1.2f, 0.16f);
            _complete = Chord("Ruling_Cue", new[] { 123.47f, 185f, 246.94f, 369.99f }, 2.2f, 0.15f);
            _ambient.Play();
        }

        public void Cue(CaseStage stage)
        {
            if (_cue == null) return;
            if (stage == CaseStage.Hatching) Play(_hatch);
            else if (stage == CaseStage.Testing) Play(_evidence);
            else if (stage == CaseStage.Molt) Play(_molt);
            else if (stage == CaseStage.Decision) Play(_decision);
            else if (stage == CaseStage.Complete) Play(_complete);
        }

        public void EvidencePulse()
        {
            Play(_evidence);
        }

        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            _cue.Stop();
            _cue.clip = clip;
            _cue.Play();
        }

        private static AudioClip Chord(string name, float[] frequencies, float duration, float amplitude)
        {
            return Generate(name, duration, delegate(float time)
            {
                var envelope = Mathf.Clamp01(time * 9f) * Mathf.Pow(Mathf.Clamp01(1f - time / duration), 1.6f);
                var signal = 0f;
                for (var i = 0; i < frequencies.Length; i++)
                {
                    var harmonic = Mathf.Sin(time * Mathf.PI * 2f * frequencies[i]);
                    harmonic += Mathf.Sin(time * Mathf.PI * 2f * frequencies[i] * 2.01f) * 0.16f;
                    signal += harmonic / frequencies.Length;
                }
                var strike = Mathf.Sin(time * Mathf.PI * 2f * 820f) * Mathf.Exp(-time * 22f) * 0.10f;
                return (signal * amplitude + strike) * envelope;
            });
        }

        private static AudioClip Generate(string name, float duration, Func<float, float> sample)
        {
            const int rate = 22050;
            var count = Mathf.CeilToInt(duration * rate);
            var data = new float[count];
            for (var i = 0; i < count; i++) data[i] = Mathf.Clamp(sample(i / (float)rate), -0.95f, 0.95f);
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
