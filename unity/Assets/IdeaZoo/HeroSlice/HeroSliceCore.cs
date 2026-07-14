using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IdeaZoo.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdeaZoo.HeroSlice
{
    public enum HeroDistrictId
    {
        ZooEntrance,
        LanternFields,
        SilentStacks,
        EvidenceForge
    }

    public enum HeroCreatureStage
    {
        Unproven,
        Observed,
        Tested,
        Trusted,
        Burdened,
        Transformed
    }

    public static class HeroSliceAutoInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindAnyObjectByType<CinematicHeroSliceDirector>() != null) return;
            var root = new GameObject("CINEMATIC_HERO_SLICE");
            root.AddComponent<CinematicHeroSliceDirector>();
        }
    }

    [DefaultExecutionOrder(975)]
    [DisallowMultipleComponent]
    public sealed class CinematicHeroSliceDirector : MonoBehaviour
    {
        private IdeaZooGame _game;
        private HeroWorldProductionPass _world;
        private HeroCreatureTransformationDirector _creature;
        private HeroCharacterPerformanceDirector _characters;
        private HeroStorySequenceDirector _story;
        private HeroMobileBudgetMonitor _budget;
        private bool _installed;

        public bool Installed { get { return _installed; } }
        public HeroWorldProductionPass WorldPass { get { return _world; } }
        public HeroCreatureTransformationDirector CreaturePass { get { return _creature; } }

        private System.Collections.IEnumerator Start()
        {
            for (var frame = 0; frame < 600; frame++)
            {
                _game = FindAnyObjectByType<IdeaZooGame>();
                if (_game != null && _game.World != null && _game.Keeper != null && _game.Creature != null) break;
                yield return null;
            }

            if (_game == null || _game.World == null) yield break;

            _world = _game.World.GetComponent<HeroWorldProductionPass>() ?? _game.World.gameObject.AddComponent<HeroWorldProductionPass>();
            _world.Build(_game.World);

            _creature = _game.Creature.GetComponent<HeroCreatureTransformationDirector>() ?? _game.Creature.gameObject.AddComponent<HeroCreatureTransformationDirector>();
            _creature.Bind(_game);

            _characters = gameObject.GetComponent<HeroCharacterPerformanceDirector>() ?? gameObject.AddComponent<HeroCharacterPerformanceDirector>();
            _characters.Bind(_game);

            _story = gameObject.GetComponent<HeroStorySequenceDirector>() ?? gameObject.AddComponent<HeroStorySequenceDirector>();
            _story.Bind(_game, _world, _creature, _characters);

            _budget = gameObject.GetComponent<HeroMobileBudgetMonitor>() ?? gameObject.AddComponent<HeroMobileBudgetMonitor>();
            _budget.Bind(_game, _world);

            ApplyGlobalMood();
            _installed = true;
        }

        private static void ApplyGlobalMood()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.24f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.10f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.018f, 0.025f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.025f, 0.055f, 0.095f);
            RenderSettings.fogDensity = Application.isMobilePlatform ? 0.0045f : 0.0065f;

            var camera = FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.008f, 0.018f, 0.035f);
                camera.allowHDR = true;
                camera.fieldOfView = Mathf.Clamp(camera.fieldOfView, 50f, 58f);
            }
        }
    }

    public static class HeroSliceUtility
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Material> EmissionMaterials = new Dictionary<string, Material>(StringComparer.Ordinal);

        public static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => string.Equals(item.name, name, StringComparison.Ordinal));
        }

        public static Transform NewRoot(Transform parent, string name, Vector3 localPosition)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = localPosition;
            return node;
        }

        public static GameObject Primitive(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            float metallic = 0f,
            float smoothness = 0.45f,
            bool castShadows = true)
        {
            var node = GameObject.CreatePrimitive(type);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = localPosition;
            node.transform.localScale = localScale;
            var collider = node.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);

            var renderer = node.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MaterialFor(color, metallic, smoothness);
                renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = castShadows;
            }
            return node;
        }

        public static Material MaterialFor(Color color, float metallic, float smoothness)
        {
            var cacheKey = ColorUtility.ToHtmlStringRGBA(color)
                           + "|" + metallic.ToString("0.000", CultureInfo.InvariantCulture)
                           + "|" + smoothness.ToString("0.000", CultureInfo.InvariantCulture);
            Material material;
            if (Materials.TryGetValue(cacheKey, out material) && material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Idea Zoo could not locate a supported lit shader.");
            material = new Material(shader) { name = "IZ_Hero_Surface_" + cacheKey };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            ConfigureSurface(material, color.a < 0.99f);
            Materials[cacheKey] = material;
            return material;
        }

        public static void SetEmission(Renderer renderer, Color color, float intensity)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;
            renderer.sharedMaterial = EmissionMaterialFor(renderer.sharedMaterial, color, intensity, true);
        }

        public static void SetEmissionOnly(Renderer renderer, Color color, float intensity)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;
            renderer.sharedMaterial = EmissionMaterialFor(renderer.sharedMaterial, color, intensity, false);
        }

        private static Material EmissionMaterialFor(Material source, Color color, float intensity, bool tintBase)
        {
            if (source == null) return null;
            var clampedIntensity = Mathf.Max(0f, intensity);
            var cacheKey = source.GetEntityId().ToString()
                           + "|" + ColorUtility.ToHtmlStringRGBA(color)
                           + "|" + clampedIntensity.ToString("0.000", CultureInfo.InvariantCulture)
                           + "|" + (tintBase ? "tint" : "emission-only");
            Material material;
            if (EmissionMaterials.TryGetValue(cacheKey, out material) && material != null) return material;

            material = new Material(source) { name = source.name + "_HeroEmission_" + cacheKey };
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * clampedIntensity);
            }
            if (tintBase)
            {
                var tint = Color.Lerp(color * 0.22f, color, 0.45f);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
                if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            }
            EmissionMaterials[cacheKey] = material;
            return material;
        }

        private static void ConfigureSurface(Material material, bool transparent)
        {
            if (material == null) return;
            if (transparent)
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry;
            }
        }

        public static TextMesh Label(Transform parent, string name, string text, Vector3 localPosition, int fontSize, Color color, float characterSize = 0.08f)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            node.transform.localPosition = localPosition;
            node.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var label = node.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = fontSize;
            label.characterSize = characterSize;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            return label;
        }

        public static Light PracticalLight(Transform parent, string name, Vector3 localPosition, Color color, float intensity, float range)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            node.transform.localPosition = localPosition;
            var light = node.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            var practical = node.AddComponent<HeroPracticalLight>();
            practical.PeakIntensity = intensity;
            practical.ActivationDistance = Mathf.Max(12f, range * 2.6f);
            practical.MaximumActivationDistance = practical.ActivationDistance;
            return light;
        }

        public static ParticleSystem Motes(Transform parent, string name, Color color, float radius, int maxParticles)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            var particles = node.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(color * 0.6f, color);
            main.maxParticles = maxParticles;

            var emission = particles.emission;
            emission.rateOverTime = Mathf.Max(2f, maxParticles / 10f);

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.2f;
            noise.frequency = 0.15f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                var material = new Material(shader) { name = "IZ_Hero_Motes" };
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                renderer.sharedMaterial = material;
            }
            return particles;
        }

        public static int TriangleCount(GameObject root)
        {
            if (root == null) return 0;
            var count = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                count += MeshTriangleCount(filter.sharedMesh);
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                count += MeshTriangleCount(skin.sharedMesh);
            return count;
        }

        private static int MeshTriangleCount(Mesh mesh)
        {
            if (mesh == null) return 0;
            var count = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                count += (int)(mesh.GetIndexCount(subMesh) / 3);
            return count;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HeroPracticalLight : MonoBehaviour
    {
        public float PeakIntensity = 2f;
        public float ActivationDistance = 24f;
        public float MaximumActivationDistance = 24f;
        public float FlickerAmount = 0.08f;
        public float FlickerSpeed = 2.3f;

        private Light _light;
        private Camera _camera;
        private float _phase;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _phase = Mathf.Abs(name.GetHashCode() % 1000) * 0.01f;
            MaximumActivationDistance = Mathf.Max(ActivationDistance, MaximumActivationDistance);
        }

        public void ReduceForPressure(float amount)
        {
            ActivationDistance = Mathf.Max(12f, ActivationDistance - Mathf.Max(0f, amount));
        }

        public void RecoverFromPressure(float amount)
        {
            ActivationDistance = Mathf.Min(MaximumActivationDistance, ActivationDistance + Mathf.Max(0f, amount));
        }

        private void Update()
        {
            if (_light == null) return;
            if (_camera == null) _camera = Camera.main ?? FindAnyObjectByType<Camera>();
            if (_camera == null) return;
            var distance = Vector3.Distance(_camera.transform.position, transform.position);
            _light.enabled = distance <= ActivationDistance;
            if (_light.enabled)
                _light.intensity = PeakIntensity * (1f + Mathf.Sin(Time.time * FlickerSpeed + _phase) * FlickerAmount);
        }
    }
}
