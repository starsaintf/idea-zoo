using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Core;
using IdeaZoo.Presentation;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Creatures
{
    public enum CreatureEmotion { Dormant, Curious, Trusting, Hungry, Afraid, Agitated, Resolute }
    public enum CreatureBodyFamily { Avian, BurdenBeast, Lantern, Serpentine, Choir }
    public enum CreatureMotionFamily { Hop, Trot, Glide, Coil, Orbit }

    [Serializable]
    public sealed class CreatureGenome
    {
        public CreatureBodyFamily BodyFamily;
        public CreatureMotionFamily MotionFamily;
        public int HeadVariant;
        public int LimbVariant;
        public int TailVariant;
        public int MarkingVariant;
        public float Height;
        public float Width;
        public float Temperament;
        public int StableSeed;

        public static CreatureGenome From(IdeaProfile profile)
        {
            var seed = StableHash((profile.Title ?? string.Empty) + "|" + (profile.PlainIdea ?? string.Empty) + "|" + profile.Class + "|" + profile.Appetite);
            var family = ClassFamily(profile.Class, seed);
            return new CreatureGenome
            {
                StableSeed = seed,
                BodyFamily = family,
                MotionFamily = MotionFor(family, profile.Class),
                HeadVariant = Positive(seed / 7) % 4,
                LimbVariant = Positive(seed / 17) % 4,
                TailVariant = Positive(seed / 31) % 4,
                MarkingVariant = Positive(seed / 47) % 5,
                Height = 0.82f + Positive(seed / 61) % 45 / 100f,
                Width = 0.78f + Positive(seed / 83) % 38 / 100f,
                Temperament = Positive(seed / 101) % 100 / 100f
            };
        }

        public string Signature
        {
            get { return BodyFamily + "-H" + HeadVariant + "-L" + LimbVariant + "-T" + TailVariant + "-M" + MarkingVariant; }
        }

        private static CreatureBodyFamily ClassFamily(IdeaClass ideaClass, int seed)
        {
            var offset = Positive(seed) % 3;
            if (ideaClass == IdeaClass.Fleck) return offset == 0 ? CreatureBodyFamily.Lantern : CreatureBodyFamily.Avian;
            if (ideaClass == IdeaClass.Hand) return offset == 0 ? CreatureBodyFamily.BurdenBeast : CreatureBodyFamily.Avian;
            if (ideaClass == IdeaClass.Mirror) return offset == 0 ? CreatureBodyFamily.Lantern : CreatureBodyFamily.Serpentine;
            if (ideaClass == IdeaClass.Teeth) return offset == 0 ? CreatureBodyFamily.Serpentine : CreatureBodyFamily.BurdenBeast;
            if (ideaClass == IdeaClass.Swarm) return offset == 0 ? CreatureBodyFamily.Choir : CreatureBodyFamily.Avian;
            if (ideaClass == IdeaClass.Weather) return offset == 0 ? CreatureBodyFamily.Choir : CreatureBodyFamily.Lantern;
            return offset == 0 ? CreatureBodyFamily.BurdenBeast : CreatureBodyFamily.Serpentine;
        }

        private static CreatureMotionFamily MotionFor(CreatureBodyFamily family, IdeaClass ideaClass)
        {
            if (ideaClass == IdeaClass.Swarm || family == CreatureBodyFamily.Choir) return CreatureMotionFamily.Orbit;
            if (family == CreatureBodyFamily.Avian) return CreatureMotionFamily.Glide;
            if (family == CreatureBodyFamily.Serpentine) return CreatureMotionFamily.Coil;
            if (family == CreatureBodyFamily.Lantern) return CreatureMotionFamily.Hop;
            return CreatureMotionFamily.Trot;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                foreach (var character in value) hash = hash * 31 + character;
                return hash;
            }
        }

        private static int Positive(int value) { return value == int.MinValue ? int.MaxValue : Math.Abs(value); }
    }

    [DisallowMultipleComponent]
    public sealed class CreatureProductionRig : MonoBehaviour
    {
        public CreatureGenome Genome { get; private set; }
        public CreatureEmotion Emotion { get; private set; }
        public float Trust { get; private set; }
        public float Hunger { get; private set; }
        public float Fear { get; private set; }
        public float Agitation { get; private set; }

        private CreatureAssembler _assembler;
        private Transform _keeper;
        private Transform _root;
        private Transform _head;
        private Transform _burden;
        private readonly List<Transform> _animated = new List<Transform>();
        private float _phase;
        private int _lastEvidence = -1;
        private int _lastGuardrails = -1;
        private IdeaClass _lastClass;
        private Appetite _lastAppetite;

        public void Build(CreatureAssembler assembler, Transform keeper)
        {
            _assembler = assembler;
            _keeper = keeper;
            if (_assembler != null && _assembler.Profile != null) Rebuild(_assembler.Profile);
        }

        public void Rebuild(IdeaProfile profile)
        {
            if (profile == null) return;
            Genome = CreatureGenome.From(profile);
            if (_root != null) Destroy(_root.gameObject);
            _animated.Clear();
            _root = new GameObject("PRODUCTION_CREATURE_LAYER_" + Genome.Signature).transform;
            _root.SetParent(transform, false);
            _phase = Mathf.Abs(Genome.StableSeed % 1000) * 0.01f;
            DisablePrototypeBody();
            BuildFamily(profile);
            BuildHead(profile);
            BuildLimbs(profile);
            BuildTail(profile);
            BuildAppetiteOrgan(profile);
            BuildClassAnatomy(profile);
            BuildBurden(profile);
            BuildGuardrails(profile);
            _lastEvidence = profile.Evidence.Count;
            _lastGuardrails = profile.Guardrails.Count;
            _lastClass = profile.Class;
            _lastAppetite = profile.Appetite;
            UpdateEmotionalModel(profile);
        }

        private void Update()
        {
            if (_assembler == null || _assembler.Profile == null || _root == null) return;
            var profile = _assembler.Profile;
            if (profile.Class != _lastClass || profile.Appetite != _lastAppetite || profile.Guardrails.Count != _lastGuardrails)
            {
                Rebuild(profile);
                return;
            }
            if (profile.Evidence.Count != _lastEvidence)
            {
                _lastEvidence = profile.Evidence.Count;
                UpdateDefinition(profile);
            }
            UpdateEmotionalModel(profile);
            Animate(profile);
        }

        private void DisablePrototypeBody()
        {
            var prototype = transform.Find("SpecimenBody");
            if (prototype == null) return;
            foreach (var renderer in prototype.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }

        private void BuildFamily(IdeaProfile profile)
        {
            if (Genome.BodyFamily == CreatureBodyFamily.Avian)
            {
                Part(_root, "AvianBody", PrimitiveType.Capsule, new Vector3(0f, 0.82f, 0f), new Vector3(0.72f * Genome.Width, 0.88f * Genome.Height, 0.58f), CivicSurface.Clay);
                Wing("LeftWing", -1f); Wing("RightWing", 1f);
            }
            else if (Genome.BodyFamily == CreatureBodyFamily.BurdenBeast)
            {
                Part(_root, "BurdenBody", PrimitiveType.Capsule, new Vector3(0f, 0.76f, 0f), new Vector3(1.02f * Genome.Width, 0.70f * Genome.Height, 0.72f), CivicSurface.Clay).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Part(_root, "LoadBack", PrimitiveType.Cube, new Vector3(0f, 1.30f, 0.18f), new Vector3(0.92f, 0.22f, 0.66f), CivicSurface.Brass);
            }
            else if (Genome.BodyFamily == CreatureBodyFamily.Lantern)
            {
                Part(_root, "LanternCore", PrimitiveType.Sphere, new Vector3(0f, 0.98f, 0f), new Vector3(0.90f * Genome.Width, 1.05f * Genome.Height, 0.82f), CivicSurface.Glass);
                Part(_root, "InnerIdea", PrimitiveType.Sphere, new Vector3(0f, 0.98f, 0f), Vector3.one * 0.42f, CivicSurface.TealGlow);
            }
            else if (Genome.BodyFamily == CreatureBodyFamily.Serpentine)
            {
                for (var i = 0; i < 7; i++)
                {
                    var segment = Part(_root, "CoilSegment_" + i, PrimitiveType.Sphere, new Vector3(0f, 0.44f + i * 0.18f, i * 0.24f - 0.72f), Vector3.one * (0.62f - i * 0.035f), i % 3 == 0 ? CivicSurface.Rust : CivicSurface.Clay).transform;
                    _animated.Add(segment);
                }
            }
            else
            {
                for (var i = 0; i < 5; i++)
                {
                    var angle = i * Mathf.PI * 2f / 5f;
                    var choir = Part(_root, "ChoirBody_" + i, PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * 0.72f, 0.95f + Mathf.Sin(angle * 2f) * 0.18f, Mathf.Sin(angle) * 0.72f), Vector3.one * 0.38f, i % 2 == 0 ? CivicSurface.Paper : CivicSurface.Glass).transform;
                    _animated.Add(choir);
                }
            }
        }

        private void BuildHead(IdeaProfile profile)
        {
            var position = Genome.BodyFamily == CreatureBodyFamily.Serpentine ? new Vector3(0f, 1.82f, 0.84f) : new Vector3(0f, 1.72f, -0.38f);
            var type = Genome.HeadVariant == 0 ? PrimitiveType.Sphere : Genome.HeadVariant == 1 ? PrimitiveType.Cube : Genome.HeadVariant == 2 ? PrimitiveType.Capsule : PrimitiveType.Cylinder;
            _head = Part(_root, "AuthoredHead_" + Genome.HeadVariant, type, position, new Vector3(0.54f, 0.48f + Genome.HeadVariant * 0.04f, 0.52f), profile.Class == IdeaClass.Mirror ? CivicSurface.Glass : CivicSurface.Paper).transform;
            for (var i = 0; i < 2 + Genome.MarkingVariant % 3; i++)
            {
                var eye = Part(_head, "IntentEye_" + i, PrimitiveType.Sphere, new Vector3((i - 0.5f) * 0.24f, 0.05f + (i / 2) * 0.16f, -0.46f), Vector3.one * 0.09f, CivicSurface.TealGlow).transform;
                _animated.Add(eye);
            }
            if (profile.Class == IdeaClass.Teeth)
                for (var i = 0; i < 4; i++) Part(_head, "CivicFang_" + i, PrimitiveType.Cube, new Vector3(-0.24f + i * 0.16f, -0.24f, -0.46f), new Vector3(0.07f, 0.20f, 0.07f), CivicSurface.Rust);
        }

        private void BuildLimbs(IdeaProfile profile)
        {
            var count = Genome.BodyFamily == CreatureBodyFamily.Choir ? 0 : 2 + Genome.LimbVariant;
            for (var i = 0; i < count; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var row = i / 2;
                var limb = Part(_root, "WorkingLimb_" + i, PrimitiveType.Capsule, new Vector3(side * (0.48f + row * 0.10f), 0.50f + row * 0.20f, 0.10f + row * 0.18f), new Vector3(0.16f, 0.45f - row * 0.04f, 0.16f), profile.Class == IdeaClass.Hand ? CivicSurface.Brass : CivicSurface.Clay).transform;
                limb.localRotation = Quaternion.Euler(0f, 0f, side * (22f + row * 8f));
                _animated.Add(limb);
            }
        }

        private void BuildTail(IdeaProfile profile)
        {
            if (Genome.BodyFamily == CreatureBodyFamily.Choir) return;
            var segments = 2 + Genome.TailVariant;
            for (var i = 0; i < segments; i++)
            {
                var tail = Part(_root, "MemoryTail_" + i, PrimitiveType.Capsule, new Vector3(0f, 0.72f - i * 0.06f, 0.62f + i * 0.32f), new Vector3(0.13f, 0.34f, 0.13f), i == segments - 1 ? CivicSurface.TealGlow : CivicSurface.Paper).transform;
                tail.localRotation = Quaternion.Euler(78f, i * 8f, 0f);
                _animated.Add(tail);
            }
        }

        private void BuildAppetiteOrgan(IdeaProfile profile)
        {
            var root = new GameObject("AppetiteOrgan_" + profile.Appetite).transform;
            root.SetParent(_root, false);
            root.localPosition = new Vector3(0f, 1.12f, -0.48f);
            var count = 1 + Genome.MarkingVariant % 4;
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / count;
                var surface = profile.Appetite == Appetite.Money ? CivicSurface.Brass : profile.Appetite == Appetite.Obedience ? CivicSurface.Rust : profile.Appetite == Appetite.Care ? CivicSurface.Moss : CivicSurface.TealGlow;
                var mark = Part(root, profile.Appetite + "Mark_" + i, profile.Appetite == Appetite.Data ? PrimitiveType.Cube : PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * 0.24f, Mathf.Sin(angle) * 0.18f, 0f), Vector3.one * 0.14f, surface).transform;
                _animated.Add(mark);
            }
        }

        private void BuildClassAnatomy(IdeaProfile profile)
        {
            if (profile.Class == IdeaClass.Mirror)
                for (var i = 0; i < 4; i++) Part(_root, "ReflectionShard_" + i, PrimitiveType.Cube, new Vector3((i - 1.5f) * 0.30f, 1.45f + (i % 2) * 0.25f, 0.12f), new Vector3(0.12f, 0.45f, 0.05f), CivicSurface.Glass).transform.localRotation = Quaternion.Euler(0f, i * 28f, i * 12f);
            else if (profile.Class == IdeaClass.Weather)
                for (var i = 0; i < 3; i++)
                {
                    var ribbon = CivicAuthoredMeshFactory.Create("PressureRibbon_" + i, CivicAuthoredMeshFactory.Ribbon(2.2f + i * 0.35f, 0.16f, 9, i * 0.7f), CivicMaterialLibrary.Get(i == 1 ? CivicSurface.Rust : CivicSurface.TealGlow), _root).transform;
                    ribbon.localPosition = new Vector3(0f, 1.0f + i * 0.22f, 0f);
                    ribbon.localRotation = Quaternion.Euler(0f, i * 120f, 0f);
                    _animated.Add(ribbon);
                }
            else if (profile.Class == IdeaClass.Burrower)
                for (var i = 0; i < 4; i++) Part(_root, "ArchivePlate_" + i, PrimitiveType.Cube, new Vector3(-0.48f + i * 0.32f, 1.30f, 0.38f), new Vector3(0.22f, 0.34f, 0.05f), CivicSurface.Paper);
        }

        private void BuildBurden(IdeaProfile profile)
        {
            _burden = new GameObject("LivingHiddenBurden").transform;
            _burden.SetParent(_root, false);
            _burden.localPosition = new Vector3(0f, 1.46f, 0.48f);
            var count = Mathf.Clamp(1 + profile.Assumptions.Count / 2, 1, 4);
            for (var i = 0; i < count; i++) Part(_burden, "BurdenCrate_" + i, PrimitiveType.Cube, new Vector3((i - (count - 1) * 0.5f) * 0.28f, i * 0.12f, 0f), new Vector3(0.24f, 0.22f, 0.26f), i % 2 == 0 ? CivicSurface.Rust : CivicSurface.Brass);
        }

        private void BuildGuardrails(IdeaProfile profile)
        {
            var root = new GameObject("LivingGuardrails").transform;
            root.SetParent(_root, false);
            for (var i = 0; i < profile.Guardrails.Count; i++)
            {
                var ring = CivicAuthoredMeshFactory.Create("GuardrailRing_" + i, CivicAuthoredMeshFactory.ArchRing(0.86f + i * 0.08f, 0.05f, 24, 360f), CivicMaterialLibrary.Get(CivicSurface.TealGlow), root).transform;
                ring.localPosition = new Vector3(0f, 0.76f + i * 0.20f, 0f);
                ring.localRotation = Quaternion.Euler(90f, i * 21f, 0f);
            }
        }

        private void UpdateDefinition(IdeaProfile profile)
        {
            var definition = 0.82f + Mathf.Clamp01((float)profile.Metrics.Evidence) * 0.22f;
            _root.localScale = new Vector3(definition, definition, definition);
            foreach (var renderer in _root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = profile.Evidence.Count >= 2 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void UpdateEmotionalModel(IdeaProfile profile)
        {
            Trust = Mathf.Clamp01((float)profile.Metrics.Evidence * 0.62f + profile.Guardrails.Count * 0.09f);
            Hunger = Mathf.Clamp01(0.78f - (float)profile.Metrics.Viability * 0.36f + (profile.Evidence.Count == 0 ? 0.18f : 0f));
            Fear = Mathf.Clamp01(1f - (float)profile.Metrics.Safety + (profile.FinalRuling == Ruling.Break ? 0.28f : 0f));
            Agitation = Mathf.Clamp01(Fear * 0.62f + Hunger * 0.44f - Trust * 0.38f);
            if (profile.FinalRuling.HasValue) Emotion = profile.FinalRuling == Ruling.Build || profile.FinalRuling == Ruling.Sanctuary ? CreatureEmotion.Resolute : profile.FinalRuling == Ruling.Break ? CreatureEmotion.Afraid : CreatureEmotion.Trusting;
            else if (Agitation > 0.68f) Emotion = CreatureEmotion.Agitated;
            else if (Fear > 0.58f) Emotion = CreatureEmotion.Afraid;
            else if (Hunger > 0.62f) Emotion = CreatureEmotion.Hungry;
            else if (Trust > 0.58f) Emotion = CreatureEmotion.Trusting;
            else Emotion = CreatureEmotion.Curious;
        }

        private void Animate(IdeaProfile profile)
        {
            var time = Time.time;
            var speed = 0.7f + Agitation * 1.8f;
            var breath = Mathf.Sin(time * speed + _phase) * (0.025f + Hunger * 0.025f);
            _root.localPosition = Vector3.up * breath;
            if (_head != null && _keeper != null)
            {
                var direction = _keeper.position + Vector3.up * 1.0f - _head.position;
                if (direction.sqrMagnitude > 0.05f)
                {
                    var local = transform.InverseTransformDirection(direction.normalized);
                    var yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -48f, 48f);
                    _head.localRotation = Quaternion.Slerp(_head.localRotation, Quaternion.Euler(Fear * -12f, yaw, 0f), Time.deltaTime * (2f + Trust * 3f));
                }
            }
            for (var i = 0; i < _animated.Count; i++)
            {
                var part = _animated[i];
                if (part == null) continue;
                var wave = Mathf.Sin(time * speed + _phase + i * 0.72f);
                part.localRotation *= Quaternion.Euler(wave * 0.08f, wave * 0.12f, wave * 0.06f);
            }
            if (_burden != null) _burden.localScale = Vector3.one * (0.72f + Fear * 0.40f);
        }

        private void Wing(string name, float side)
        {
            var wing = Part(_root, name, PrimitiveType.Cube, new Vector3(side * 0.68f, 1.02f, 0.08f), new Vector3(0.62f, 0.10f, 0.48f), CivicSurface.Paper).transform;
            wing.localRotation = Quaternion.Euler(0f, side * 8f, side * 22f);
            _animated.Add(wing);
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, CivicSurface surface)
        {
            var node = GameObject.CreatePrimitive(type);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            node.transform.localScale = scale;
            var collider = node.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            node.GetComponent<Renderer>().sharedMaterial = CivicMaterialLibrary.Get(surface);
            return node;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CreatureProductionDirector : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (var frame = 0; frame < 360; frame++)
            {
                var game = FindFirstObjectByType<IdeaZooGame>();
                if (game != null && game.Creature != null && game.Keeper != null)
                {
                    var rig = game.Creature.GetComponent<CreatureProductionRig>() ?? game.Creature.gameObject.AddComponent<CreatureProductionRig>();
                    rig.Build(game.Creature, game.Keeper.transform);
                    yield break;
                }
                yield return null;
            }
        }
    }

    public static class CreatureProductionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<CreatureProductionDirector>() != null) return;
            new GameObject("IdeaZoo_Creature_Production").AddComponent<CreatureProductionDirector>();
        }
    }
}
