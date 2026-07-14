using System.Collections;
using System.Collections.Generic;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MobilePresentationBudget : MonoBehaviour
    {
        private readonly List<Renderer> _smallDetails = new List<Renderer>();
        private readonly List<Renderer> _mediumDetails = new List<Renderer>();
        private readonly List<Renderer> _landmarks = new List<Renderer>();
        private Camera _camera;
        private float _nextCull;
        private float _frameAccumulator;
        private int _frameSamples;
        private float _bufferScale = 1f;
        private bool _lowTier;

        public void Build(Transform world, Camera camera)
        {
            _camera = camera;
            _lowTier = Application.isMobilePlatform && (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 5000);
            QualitySettings.vSyncCount = 0;
            QualitySettings.lodBias = _lowTier ? 0.65f : Application.isMobilePlatform ? 0.85f : 1.2f;
            QualitySettings.shadowDistance = _lowTier ? 18f : Application.isMobilePlatform ? 28f : 48f;
            QualitySettings.antiAliasing = _lowTier ? 0 : Application.isMobilePlatform ? 2 : 4;

            foreach (var renderer in world.GetComponentsInChildren<Renderer>(true))
            {
                var authored = renderer.GetComponentInParent<AuthoredEnvironmentDetail>();
                if (authored != null)
                {
                    if (authored.Tier == AuthoredDetailTier.Decorative) _smallDetails.Add(renderer);
                    else if (authored.Tier == AuthoredDetailTier.Department) _mediumDetails.Add(renderer);
                    else _landmarks.Add(renderer);
                }
                else if (IsSmall(renderer.name)) _smallDetails.Add(renderer);
                else if (IsMedium(renderer.name)) _mediumDetails.Add(renderer);

                if (renderer.name.Contains("Glass") || renderer.name.Contains("Glow") || renderer.name.Contains("Light") || renderer.sharedMaterial != null && renderer.sharedMaterial.name.Contains("TealGlow"))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            StartCoroutine(StaggerInitialBudget());
        }

        private void Update()
        {
            _frameAccumulator += Time.unscaledDeltaTime;
            _frameSamples++;
            if (Time.unscaledTime >= _nextCull)
            {
                _nextCull = Time.unscaledTime + 0.35f;
                CullByDistance();
                AdaptResolution();
            }
        }

        private IEnumerator StaggerInitialBudget()
        {
            if (!_lowTier) yield break;
            for (var i = 0; i < _smallDetails.Count; i++)
            {
                if (_smallDetails[i] != null && i % 3 == 2) _smallDetails[i].enabled = false;
                if (i % 40 == 0) yield return null;
            }
        }

        private void CullByDistance()
        {
            if (_camera == null) return;
            var cameraPosition = _camera.transform.position;
            var smallDistance = _lowTier ? 17f : Application.isMobilePlatform ? 24f : 34f;
            var mediumDistance = _lowTier ? 28f : Application.isMobilePlatform ? 38f : 55f;
            var landmarkDistance = _lowTier ? 54f : Application.isMobilePlatform ? 72f : 110f;
            CullList(_smallDetails, cameraPosition, smallDistance * smallDistance, _lowTier);
            CullList(_mediumDetails, cameraPosition, mediumDistance * mediumDistance, false);
            CullList(_landmarks, cameraPosition, landmarkDistance * landmarkDistance, false);
        }

        private static void CullList(List<Renderer> renderers, Vector3 cameraPosition, float distanceSquared, bool stagger)
        {
            for (var i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                if (stagger && i % 3 == 2) { renderer.enabled = false; continue; }
                renderer.enabled = (renderer.bounds.center - cameraPosition).sqrMagnitude <= distanceSquared;
            }
        }

        private void AdaptResolution()
        {
            if (!Application.isMobilePlatform || _frameSamples < 12) return;
            var average = _frameAccumulator / _frameSamples;
            _frameAccumulator = 0f;
            _frameSamples = 0;
            var target = 1f / 30f;
            if (average > target * 1.25f) _bufferScale = Mathf.Max(0.68f, _bufferScale - 0.05f);
            else if (average < target * 0.88f) _bufferScale = Mathf.Min(1f, _bufferScale + 0.025f);
            ScalableBufferManager.ResizeBuffers(_bufferScale, _bufferScale);
        }

        private static bool IsSmall(string name)
        {
            return name.Contains("Marker_") || name.Contains("Case_Slip") || name.Contains("Maintenance_Tag") ||
                   name.Contains("Pending_Idea_Card") || name.Contains("Commitment_Token") || name.Contains("Path_Light") ||
                   name.Contains("Idea_Card") || name.Contains("Fleck_") || name.Contains("ValueToken") || name.Contains("AudienceEye");
        }

        private static bool IsMedium(string name)
        {
            return name.Contains("Banner") || name.Contains("Vitrine") || name.Contains("Listening_Stone") ||
                   name.Contains("Pledge_Post") || name.Contains("ArchivePlate") || name.Contains("ReflectionShard") ||
                   name.Contains("Suspended_Instrument") || name.Contains("Public_Language");
        }
    }

    [DisallowMultipleComponent]
    public sealed class MobilePresentationBudgetAutoLoad : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (var attempt = 0; attempt < 180; attempt++)
            {
                var world = FindFirstObjectByType<WhisperGateWorld>();
                var camera = FindFirstObjectByType<Camera>();
                var art = FindFirstObjectByType<CivicWorldArtPass>();
                var authored = FindFirstObjectByType<AuthoredEnvironmentPass>();
                if (world != null && camera != null && art != null && authored != null)
                {
                    yield return null;
                    var budget = world.GetComponent<MobilePresentationBudget>();
                    if (budget == null) budget = world.gameObject.AddComponent<MobilePresentationBudget>();
                    budget.Build(world.transform, camera);
                    yield break;
                }
                yield return null;
            }
        }
    }

    public static class MobilePresentationBudgetBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartBudget()
        {
            if (Object.FindFirstObjectByType<MobilePresentationBudgetAutoLoad>() != null) return;
            var root = new GameObject("IdeaZoo_Mobile_Presentation_Budget");
            root.AddComponent<MobilePresentationBudgetAutoLoad>();
            Object.DontDestroyOnLoad(root);
        }
    }
}
