using System;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Characters;
using IdeaZoo.Core;
using IdeaZoo.Presentation;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.HeroSlice
{
    [DefaultExecutionOrder(980)]
    [DisallowMultipleComponent]
    public sealed class HeroCharacterPerformanceDirector : MonoBehaviour
    {
        private IdeaZooGame _game;
        private CharacterPerformanceRig _keeper;
        private CharacterPerformanceRig _mara;
        private CaseStage _lastStage = (CaseStage)(-1);
        private int _lastEvidence = -1;
        private float _nextAmbientBeat;

        public CharacterPerformanceRig KeeperRig { get { return _keeper; } }
        public CharacterPerformanceRig MaraRig { get { return _mara; } }

        public void Bind(IdeaZooGame game)
        {
            _game = game;
            ResolveCast();
        }

        private void Update()
        {
            if (_game == null || _game.Director == null) return;
            if (_keeper == null || _mara == null) ResolveCast();
            var director = _game.Director;
            var evidence = director.Profile != null ? director.Profile.Evidence.Count : 0;

            if (_lastStage != director.Stage)
            {
                _lastStage = director.Stage;
                ApplyStagePerformance(director.Stage);
            }

            if (_lastEvidence != evidence)
            {
                _lastEvidence = evidence;
                if (evidence > 0)
                {
                    if (_keeper != null) _keeper.Perform(CharacterGesture.Inspect, 1.2f);
                    if (_mara != null) _mara.Perform(CharacterGesture.Explain, 1.2f);
                }
            }

            if (Time.time >= _nextAmbientBeat)
            {
                _nextAmbientBeat = Time.time + 7f + Mathf.Abs(Mathf.Sin(Time.time)) * 5f;
                AmbientBeat();
            }
        }

        public void SignalDiscovery()
        {
            if (_keeper != null)
            {
                _keeper.SetEmotion(CharacterEmotion.Curious);
                _keeper.Perform(CharacterGesture.Inspect, 1.7f);
            }
            if (_mara != null)
            {
                _mara.SetEmotion(CharacterEmotion.Protective);
                _mara.Perform(CharacterGesture.Explain, 1.4f);
            }
        }

        public void SignalTransformation(bool hopeful)
        {
            if (_keeper != null)
            {
                _keeper.SetEmotion(hopeful ? CharacterEmotion.Hopeful : CharacterEmotion.Grieving);
                _keeper.Perform(hopeful ? CharacterGesture.Celebrate : CharacterGesture.Mourn, 1.8f);
            }
            if (_mara != null)
            {
                _mara.SetEmotion(hopeful ? CharacterEmotion.Hopeful : CharacterEmotion.Concerned);
                _mara.Perform(hopeful ? CharacterGesture.Invite : CharacterGesture.Refuse, 1.8f);
            }
        }

        private void ResolveCast()
        {
            if (_game == null) return;
            _keeper = _game.Keeper != null ? _game.Keeper.GetComponent<CharacterPerformanceRig>() : null;
            var specialists = FindObjectsByType<ProceduralSpecialist>(FindObjectsInactive.Include);
            var mara = specialists.FirstOrDefault(item => item != null && item.SpecialistName.IndexOf("Mara", StringComparison.OrdinalIgnoreCase) >= 0);
            _mara = mara != null ? mara.GetComponent<CharacterPerformanceRig>() : null;

            if (_keeper != null && _game.Creature != null) _keeper.LookTarget = _game.Creature.transform;
            if (_mara != null && _game.Creature != null) _mara.LookTarget = _game.Creature.transform;
        }

        private void ApplyStagePerformance(CaseStage stage)
        {
            var ruling = _game != null && _game.Director != null && _game.Director.Profile != null
                ? _game.Director.Profile.FinalRuling
                : null;
            var hopeful = HeroRulingSemantics.IsHopeful(ruling);

            if (_keeper != null)
            {
                var emotion = CharacterEmotion.Curious;
                if (stage == CaseStage.Molt) emotion = CharacterEmotion.Concerned;
                if (stage == CaseStage.Decision) emotion = CharacterEmotion.Defiant;
                if (stage == CaseStage.Complete) emotion = hopeful ? CharacterEmotion.Hopeful : CharacterEmotion.Grieving;
                _keeper.SetEmotion(emotion);
            }

            if (_mara != null)
            {
                var emotion = CharacterEmotion.Protective;
                if (stage == CaseStage.Decision) emotion = CharacterEmotion.Concerned;
                if (stage == CaseStage.Complete) emotion = hopeful ? CharacterEmotion.Hopeful : CharacterEmotion.Concerned;
                _mara.SetEmotion(emotion);
            }
        }

        private void AmbientBeat()
        {
            if (_game == null || _game.Director == null || _game.Director.Profile == null) return;
            var profile = _game.Director.Profile;
            var risk = 1f - Mathf.Clamp01((float)profile.Metrics.Safety);
            if (_mara != null && risk > 0.55f) _mara.Perform(CharacterGesture.Refuse, 0.8f);
            else if (_keeper != null) _keeper.Perform(CharacterGesture.Inspect, 0.75f);
        }
    }

    internal static class HeroRulingSemantics
    {
        public static bool IsHopeful(Ruling? ruling)
        {
            return ruling == Ruling.Build || ruling == Ruling.Molt || ruling == Ruling.Sanctuary;
        }

        public static bool IsBreak(Ruling? ruling)
        {
            return ruling == Ruling.Break;
        }

        public static bool IsHibernate(Ruling? ruling)
        {
            return ruling == Ruling.Hibernate;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HeroCreatureTransformationDirector : MonoBehaviour
    {
        private IdeaZooGame _game;
        private Transform _visual;
        private Transform _layer;
        private Light _coreLight;
        private readonly List<Renderer> _stageRenderers = new List<Renderer>();
        private HeroCreatureStage _stage = (HeroCreatureStage)(-1);
        private string _recordId = string.Empty;
        private float _phase;
        private float _stageScale = 1f;
        private Vector3 _visualBaseScale = Vector3.one;

        public HeroCreatureStage Stage { get { return _stage; } }

        public void Bind(IdeaZooGame game)
        {
            _game = game;
            _phase = UnityEngine.Random.value * 6f;
            RefreshVisual(true);
        }

        private void Update()
        {
            if (_game == null || _game.Director == null) return;
            RefreshVisual(false);
            if (_game.Director.Profile == null || _layer == null) return;
            var next = Evaluate(_game.Director.Profile, _game.Director.Stage);
            if (next != _stage) ApplyStage(next, _game.Director.Profile);
            AnimateStage(_game.Director.Profile);
        }

        public HeroCreatureStage Evaluate(IdeaProfile profile, CaseStage caseStage)
        {
            if (profile == null) return HeroCreatureStage.Unproven;
            var evidenceCount = profile.Evidence.Count;
            var evidence = Mathf.Clamp01((float)profile.Metrics.Evidence);
            var safety = Mathf.Clamp01((float)profile.Metrics.Safety);
            var risk = 1f - safety;

            if (profile.FinalRuling.HasValue || caseStage == CaseStage.Complete) return HeroCreatureStage.Transformed;
            if (risk > 0.62f || profile.Assumptions.Count >= 5) return HeroCreatureStage.Burdened;
            if (profile.Guardrails.Count >= 2 && evidence >= 0.58f) return HeroCreatureStage.Trusted;
            if (evidenceCount >= 3 || evidence >= 0.48f) return HeroCreatureStage.Tested;
            if (evidenceCount >= 1 || evidence >= 0.22f) return HeroCreatureStage.Observed;
            return HeroCreatureStage.Unproven;
        }

        private void RefreshVisual(bool force)
        {
            if (_game == null || _game.Creature == null) return;
            var profile = _game.Director != null ? _game.Director.Profile : null;
            var id = profile != null ? profile.RecordId : string.Empty;
            var candidate = _game.Creature.transform.Cast<Transform>()
                .FirstOrDefault(item => item.name.StartsWith("CLOUD_CREATURE_VISUAL_", StringComparison.Ordinal))
                ?? _game.Creature.transform.Cast<Transform>().FirstOrDefault(item => item.name.StartsWith("PRODUCTION_CREATURE_LAYER_", StringComparison.Ordinal));

            if (!force && candidate == _visual && id == _recordId) return;
            _visual = candidate;
            _visualBaseScale = _visual != null ? _visual.localScale : Vector3.one;
            _recordId = id;
            if (_layer != null) Destroy(_layer.gameObject);
            _stageRenderers.Clear();

            _layer = HeroSliceUtility.NewRoot(_game.Creature.transform, "HERO_CREATURE_TRANSFORMATION_LAYER", Vector3.zero);
            BuildLayer();
            ApplyStage(profile == null ? HeroCreatureStage.Unproven : Evaluate(profile, _game.Director.Stage), profile);
        }

        private void BuildLayer()
        {
            if (_layer == null) return;
            var core = HeroSliceUtility.Primitive(_layer, "HeroInnerIdea", PrimitiveType.Sphere, new Vector3(0f, 1.0f, 0f),
                Vector3.one * 0.34f, new Color(1f, 0.55f, 0.12f), 0.02f, 0.96f, false);
            _stageRenderers.Add(core.GetComponent<Renderer>());

            for (var i = 0; i < 3; i++)
            {
                var ring = HeroSliceUtility.Primitive(_layer, "EvidenceHalo_" + i, PrimitiveType.Cylinder,
                    new Vector3(0f, 0.55f + i * 0.32f, 0f), new Vector3(0.75f + i * 0.22f, 0.035f, 0.75f + i * 0.22f),
                    new Color(0.16f, 0.52f, 1f), 0.72f, 0.86f, false);
                ring.transform.localRotation = Quaternion.Euler(i * 18f, 0f, i * 27f);
                _stageRenderers.Add(ring.GetComponent<Renderer>());
            }

            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI * 2f / 4f;
                var fin = HeroSliceUtility.Primitive(_layer, "TrustFin_" + i, PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(angle) * 0.72f, 1.05f, Mathf.Sin(angle) * 0.72f),
                    new Vector3(0.10f, 0.48f, 0.42f), new Color(1f, 0.66f, 0.18f), 0.14f, 0.84f, false);
                fin.transform.localRotation = Quaternion.Euler(20f, -angle * Mathf.Rad2Deg, 35f);
                _stageRenderers.Add(fin.GetComponent<Renderer>());
            }

            for (var i = 0; i < 3; i++)
            {
                var burden = HeroSliceUtility.Primitive(_layer, "BurdenShard_" + i, PrimitiveType.Cube,
                    new Vector3(-0.45f + i * 0.45f, 1.45f, 0.05f), new Vector3(0.22f, 0.55f, 0.18f),
                    new Color(0.34f, 0.06f, 0.07f), 0.48f, 0.54f);
                burden.transform.localRotation = Quaternion.Euler(18f + i * 16f, i * 35f, 22f);
                _stageRenderers.Add(burden.GetComponent<Renderer>());
            }

            var lightNode = new GameObject("IdeaCoreLight");
            lightNode.transform.SetParent(_layer, false);
            lightNode.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            _coreLight = lightNode.AddComponent<Light>();
            _coreLight.type = LightType.Point;
            _coreLight.range = 7f;
            _coreLight.shadows = LightShadows.None;
            HeroSliceUtility.Motes(_layer, "CreatureIdeaMotes", new Color(1f, 0.66f, 0.25f), 1.6f, 55);
        }

        private void ApplyStage(HeroCreatureStage stage, IdeaProfile profile)
        {
            _stage = stage;
            if (_layer == null) return;

            var evidence = profile != null ? Mathf.Clamp01((float)profile.Metrics.Evidence) : 0f;
            var safety = profile != null ? Mathf.Clamp01((float)profile.Metrics.Safety) : 0.5f;
            var ruling = profile != null ? profile.FinalRuling : null;
            var isBreak = stage == HeroCreatureStage.Transformed && HeroRulingSemantics.IsBreak(ruling);
            var isHibernate = stage == HeroCreatureStage.Transformed && HeroRulingSemantics.IsHibernate(ruling);
            var isHopeful = stage == HeroCreatureStage.Transformed && HeroRulingSemantics.IsHopeful(ruling);

            var gold = new Color(1f, 0.56f, 0.12f);
            var blue = new Color(0.18f, 0.52f, 1f);
            var burden = new Color(0.62f, 0.08f, 0.10f);
            var muted = new Color(0.24f, 0.32f, 0.45f);
            var color = Color.Lerp(gold, blue, evidence);
            if (stage == HeroCreatureStage.Burdened || isBreak) color = burden;
            else if (isHibernate) color = muted;
            else if (isHopeful) color = Color.Lerp(gold, blue, 0.52f);

            foreach (var child in _layer.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith("EvidenceHalo_", StringComparison.Ordinal))
                    child.gameObject.SetActive(stage >= HeroCreatureStage.Observed && stage != HeroCreatureStage.Burdened && !isBreak);
                if (child.name.StartsWith("TrustFin_", StringComparison.Ordinal))
                    child.gameObject.SetActive(stage == HeroCreatureStage.Trusted || isHopeful);
                if (child.name.StartsWith("BurdenShard_", StringComparison.Ordinal))
                    child.gameObject.SetActive(stage == HeroCreatureStage.Burdened || isBreak);
            }

            var intensity = 0.8f + evidence * 2.2f;
            if (stage == HeroCreatureStage.Unproven) intensity = 0.55f;
            if (isHopeful) intensity = 3.4f;
            if (isHibernate) intensity = 0.75f;
            if (isBreak) intensity = 0.42f;
            foreach (var renderer in _stageRenderers) HeroSliceUtility.SetEmission(renderer, color, intensity);

            if (_coreLight != null)
            {
                _coreLight.color = color;
                _coreLight.intensity = Application.isMobilePlatform ? Mathf.Min(2.2f, intensity) : intensity;
                _coreLight.range = isBreak ? 3.2f : 4.5f + evidence * 4f;
            }

            if (_visual != null)
            {
                foreach (var renderer in _visual.GetComponentsInChildren<Renderer>(true))
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetColor("_EmissionColor", color * (0.28f + evidence * 0.72f));
                    renderer.SetPropertyBlock(block);
                }
            }

            _stageScale = StageScale(stage, safety, ruling);
            if (_visual != null) _visual.localScale = _visualBaseScale * _stageScale;
        }

        private void AnimateStage(IdeaProfile profile)
        {
            var evidence = Mathf.Clamp01((float)profile.Metrics.Evidence);
            var safety = Mathf.Clamp01((float)profile.Metrics.Safety);
            var speed = 1.2f + evidence * 1.4f + (1f - safety) * 1.2f;
            var pulse = 1f + Mathf.Sin(Time.time * speed + _phase) * (0.025f + (1f - safety) * 0.03f);
            if (_layer != null) _layer.localScale = Vector3.one * (_stageScale * pulse);

            var halos = _layer.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name.StartsWith("EvidenceHalo_", StringComparison.Ordinal)).ToArray();
            for (var i = 0; i < halos.Length; i++)
                halos[i].localRotation *= Quaternion.Euler(0f, (10f + i * 6f) * Time.deltaTime, (i % 2 == 0 ? 8f : -8f) * Time.deltaTime);
        }

        private static float StageScale(HeroCreatureStage stage, float safety, Ruling? ruling)
        {
            if (stage == HeroCreatureStage.Transformed)
            {
                if (HeroRulingSemantics.IsBreak(ruling)) return 0.78f;
                if (HeroRulingSemantics.IsHibernate(ruling)) return 0.90f;
                return 1.14f;
            }

            switch (stage)
            {
                case HeroCreatureStage.Unproven: return 0.92f;
                case HeroCreatureStage.Observed: return 0.98f;
                case HeroCreatureStage.Tested: return 1.04f;
                case HeroCreatureStage.Trusted: return 1.08f;
                case HeroCreatureStage.Burdened: return 0.98f - (1f - safety) * 0.05f;
                default: return 1f;
            }
        }
    }

    [DefaultExecutionOrder(990)]
    [DisallowMultipleComponent]
    public sealed class HeroStorySequenceDirector : MonoBehaviour
    {
        private IdeaZooGame _game;
        private HeroWorldProductionPass _world;
        private HeroCreatureTransformationDirector _creature;
        private HeroCharacterPerformanceDirector _characters;
        private PresentationCameraRig _camera;
        private bool _standardPresentationOwnsCaseShots;
        private string _lastRecord = string.Empty;
        private int _lastEvidence = -1;
        private CaseStage _lastStage = (CaseStage)(-1);
        private HeroCreatureStage _lastCreatureStage = (HeroCreatureStage)(-1);
        private float _nextAllowedShot;

        public void Bind(
            IdeaZooGame game,
            HeroWorldProductionPass world,
            HeroCreatureTransformationDirector creature,
            HeroCharacterPerformanceDirector characters)
        {
            _game = game;
            _world = world;
            _creature = creature;
            _characters = characters;
            _camera = FindAnyObjectByType<PresentationCameraRig>();
            _standardPresentationOwnsCaseShots = FindAnyObjectByType<IdeaZooPresentationDirector>() != null;
        }

        private void Update()
        {
            if (_game == null || _game.Director == null) return;
            if (_camera == null) _camera = FindAnyObjectByType<PresentationCameraRig>();
            if (!_standardPresentationOwnsCaseShots)
                _standardPresentationOwnsCaseShots = FindAnyObjectByType<IdeaZooPresentationDirector>() != null;

            var director = _game.Director;
            var profile = director.Profile;

            if (profile != null && profile.RecordId != _lastRecord)
            {
                _lastRecord = profile.RecordId;
                _lastEvidence = profile.Evidence.Count;
                _lastCreatureStage = _creature != null ? _creature.Stage : HeroCreatureStage.Unproven;
                if (!_standardPresentationOwnsCaseShots)
                    QueueShot(PresentationShot.Hatch, _game.Creature.transform, 1.35d);
            }

            if (profile != null && profile.Evidence.Count != _lastEvidence)
            {
                _lastEvidence = profile.Evidence.Count;
                _characters?.SignalDiscovery();
                if (!_standardPresentationOwnsCaseShots)
                    QueueShot(PresentationShot.Inspection, _game.Creature.transform, 0.95d);
            }

            if (_creature != null && _creature.Stage != _lastCreatureStage)
            {
                _lastCreatureStage = _creature.Stage;
                if (_lastCreatureStage == HeroCreatureStage.Burdened)
                {
                    _characters?.SignalTransformation(false);
                    QueueShot(PresentationShot.Molt, _game.Creature.transform, 1.25d);
                }
                else if (_lastCreatureStage == HeroCreatureStage.Transformed)
                {
                    var hopeful = profile != null && HeroRulingSemantics.IsHopeful(profile.FinalRuling);
                    _characters?.SignalTransformation(hopeful);
                    if (!_standardPresentationOwnsCaseShots)
                        QueueShot(PresentationShot.Ruling, _game.Creature.transform, 1.8d);
                }
            }

            if (_lastStage != director.Stage)
            {
                _lastStage = director.Stage;
                if (_standardPresentationOwnsCaseShots) return;

                if (director.Stage == CaseStage.Molt)
                {
                    var district = _world != null ? _world.District(HeroDistrictId.EvidenceForge) : null;
                    QueueShot(PresentationShot.Molt, district ?? _game.Creature.transform, 1.5d);
                }
                else if (director.Stage == CaseStage.Decision)
                {
                    QueueShot(PresentationShot.Decision, _game.World.DecisionRoot ?? _game.Creature.transform, 1.7d);
                }
                else if (director.Stage == CaseStage.Complete)
                {
                    QueueShot(PresentationShot.Ruling, _game.Creature.transform, 1.8d);
                }
            }
        }

        private void QueueShot(PresentationShot shot, Transform target, double duration)
        {
            if (_camera == null || target == null || Time.unscaledTime < _nextAllowedShot) return;
            _nextAllowedShot = Time.unscaledTime + (float)duration + 0.35f;
            _camera.Play(shot, target, duration);
        }
    }

    [DisallowMultipleComponent]
    public sealed class HeroMobileBudgetMonitor : MonoBehaviour
    {
        public const int MaxDynamicLights = 10;
        public const int MaxHeroParticles = 520;
        public const int MaxVisibleTrianglesMobile = 950000;

        private IdeaZooGame _game;
        private HeroWorldProductionPass _world;
        private float _sampleStart;
        private int _sampleFrames;
        private float _smoothedFps = 60f;
        private float _nextAudit;

        public float SmoothedFps { get { return _smoothedFps; } }
        public string LastAudit { get; private set; } = string.Empty;

        public void Bind(IdeaZooGame game, HeroWorldProductionPass world)
        {
            _game = game;
            _world = world;
            _sampleStart = Time.unscaledTime;
            _sampleFrames = 0;
        }

        private void Update()
        {
            _sampleFrames++;
            var elapsed = Time.unscaledTime - _sampleStart;
            if (elapsed >= 1f)
            {
                var fps = _sampleFrames / Mathf.Max(0.01f, elapsed);
                _smoothedFps = Mathf.Lerp(_smoothedFps, fps, 0.35f);
                _sampleFrames = 0;
                _sampleStart = Time.unscaledTime;
                AdaptiveHeroQuality();
            }

            if (Time.unscaledTime >= _nextAudit)
            {
                _nextAudit = Time.unscaledTime + 8f;
                Audit();
            }
        }

        private void AdaptiveHeroQuality()
        {
            if (!Application.isMobilePlatform) return;
            var target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 30;
            var lights = FindObjectsByType<HeroPracticalLight>(FindObjectsInactive.Exclude);
            if (_smoothedFps < target * 0.86f)
            {
                foreach (var light in lights) light.ReduceForPressure(1f);
            }
            else if (_smoothedFps > target * 0.96f)
            {
                foreach (var light in lights) light.RecoverFromPressure(0.5f);
            }
        }

        public void Audit()
        {
            var heroRoot = _world != null ? _world.HeroRoot : null;
            var creatureRoot = _game != null && _game.Creature != null ? _game.Creature.transform : null;
            var lights = CountActiveLights(heroRoot) + CountActiveLights(creatureRoot);
            var particles = ParticleCapacity(heroRoot) + ParticleCapacity(creatureRoot);
            var triangles = TriangleCount(heroRoot) + TriangleCount(creatureRoot);
            LastAudit = "hero-lights=" + lights + "; hero-particles=" + particles + "; hero-triangles=" + triangles + "; fps=" + _smoothedFps.ToString("0.0");

            if (lights > MaxDynamicLights) Debug.LogWarning("Hero slice dynamic-light budget exceeded: " + LastAudit);
            if (particles > MaxHeroParticles) Debug.LogWarning("Hero slice particle budget exceeded: " + LastAudit);
            if (Application.isMobilePlatform && triangles > MaxVisibleTrianglesMobile)
                Debug.LogWarning("Hero slice mobile triangle budget exceeded: " + LastAudit);
        }

        private static int CountActiveLights(Transform root)
        {
            return root == null ? 0 : root.GetComponentsInChildren<Light>(true).Count(item => item.enabled && item.gameObject.activeInHierarchy);
        }

        private static int ParticleCapacity(Transform root)
        {
            return root == null ? 0 : root.GetComponentsInChildren<ParticleSystem>(true).Sum(item => item.main.maxParticles);
        }

        private static int TriangleCount(Transform root)
        {
            return root == null ? 0 : HeroSliceUtility.TriangleCount(root.gameObject);
        }
    }
}
