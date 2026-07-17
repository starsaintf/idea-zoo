using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Core;
using IdeaZoo.Presentation;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Characters
{
    public enum CharacterEmotion { Neutral, Curious, Protective, Concerned, Defiant, Grieving, Hopeful }
    public enum CharacterGesture { None, Invite, Explain, Refuse, Inspect, Celebrate, Mourn }

    [Serializable]
    public sealed class KeeperAppearance
    {
        public int BodyFrame;
        public int SkinTone = 2;
        public int HairStyle = 1;
        public int CoatPattern;
        public int LensStyle;

        public static KeeperAppearance Load()
        {
            return new KeeperAppearance
            {
                BodyFrame = PlayerPrefs.GetInt("iz_keeper_body", 1),
                SkinTone = PlayerPrefs.GetInt("iz_keeper_skin", 2),
                HairStyle = PlayerPrefs.GetInt("iz_keeper_hair", 1),
                CoatPattern = PlayerPrefs.GetInt("iz_keeper_coat", 0),
                LensStyle = PlayerPrefs.GetInt("iz_keeper_lens", 0)
            };
        }

        public void Save()
        {
            PlayerPrefs.SetInt("iz_keeper_body", Mathf.Clamp(BodyFrame, 0, 2));
            PlayerPrefs.SetInt("iz_keeper_skin", Mathf.Clamp(SkinTone, 0, 5));
            PlayerPrefs.SetInt("iz_keeper_hair", Mathf.Clamp(HairStyle, 0, 4));
            PlayerPrefs.SetInt("iz_keeper_coat", Mathf.Clamp(CoatPattern, 0, 3));
            PlayerPrefs.SetInt("iz_keeper_lens", Mathf.Clamp(LensStyle, 0, 2));
            PlayerPrefs.Save();
        }
    }

    [DisallowMultipleComponent]
    public sealed class CharacterPerformanceRig : MonoBehaviour
    {
        public string CharacterName;
        public string Role;
        public CharacterEmotion Emotion;
        public Transform LookTarget;
        public bool IsChild;

        private Animator _animator;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Vector3 _baseScale;
        private Quaternion _headBase;
        private CharacterGesture _gesture;
        private float _gestureUntil;
        private float _phase;
        private Vector3 _lastPosition;
        private float _speed;

        public void Configure(string characterName, string role, CharacterEmotion emotion, bool isChild = false)
        {
            CharacterName = characterName;
            Role = role;
            Emotion = emotion;
            IsChild = isChild;
            _phase = Mathf.Abs(characterName.GetHashCode() % 1000) * 0.013f;
            _animator = GetComponentInChildren<Animator>();
            _head = FindPart("Head") ?? FindPart("head");
            _leftArm = FindPart("LeftArm") ?? FindPart("UpperArm_L");
            _rightArm = FindPart("RightArm") ?? FindPart("UpperArm_R");
            _baseScale = transform.localScale;
            _headBase = _head != null ? _head.localRotation : Quaternion.identity;
            _lastPosition = transform.position;
            ResetPerformance();
            ApplyAnimatorEmotion();
        }

        public void ResetPerformance()
        {
            _gesture = CharacterGesture.None;
            _gestureUntil = 0f;
            _speed = 0f;
            _lastPosition = transform.position;
            transform.localScale = _baseScale;
            if (_head != null) _head.localRotation = _headBase;
        }

        private void Reset()
        {
            _baseScale = transform.localScale;
            _lastPosition = transform.position;
            _gesture = CharacterGesture.None;
        }

        public void SetEmotion(CharacterEmotion emotion)
        {
            if (Emotion == emotion) return;
            Emotion = emotion;
            ApplyAnimatorEmotion();
        }

        public void Perform(CharacterGesture gesture, float seconds = 1.45f)
        {
            _gesture = gesture;
            _gestureUntil = Time.time + Mathf.Max(0.25f, seconds);
            if (_animator != null)
            {
                SetTriggerIfPresent("Gesture");
                SetIntegerIfPresent("GestureIndex", (int)gesture);
            }
        }

        private void Update()
        {
            var delta = transform.position - _lastPosition;
            _lastPosition = transform.position;
            _speed = Mathf.Lerp(_speed, delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f), Time.deltaTime * 8f);
            if (_animator != null)
            {
                SetFloatIfPresent("Speed", _speed);
                SetBoolIfPresent("Grounded", true);
            }

            AnimateGaze();
            if (_animator == null) AnimateFallback();
            if (_gestureUntil <= Time.time) _gesture = CharacterGesture.None;
        }

        private void AnimateGaze()
        {
            if (_head == null) return;
            var target = LookTarget;
            if (target == null)
            {
                _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase, Time.deltaTime * 3f);
                return;
            }

            var direction = target.position + Vector3.up * 0.8f - _head.position;
            if (direction.sqrMagnitude < 0.02f) return;
            var local = transform.InverseTransformDirection(direction.normalized);
            var yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -45f, 45f);
            var pitch = Mathf.Clamp(-Mathf.Asin(local.y) * Mathf.Rad2Deg, -22f, 22f);
            _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(pitch, yaw, 0f), Time.deltaTime * 4.5f);
        }

        private void AnimateFallback()
        {
            var breath = Mathf.Sin(Time.time * 1.7f + _phase) * 0.008f;
            transform.localScale = _baseScale + Vector3.up * breath;
            var amount = GestureAmount(_gesture);
            var pulse = Mathf.Sin(Mathf.Clamp01(1f - (_gestureUntil - Time.time)) * Mathf.PI);
            if (_leftArm != null) _leftArm.localRotation = Quaternion.Slerp(_leftArm.localRotation, Quaternion.Euler(amount.x * pulse, 0f, amount.z), Time.deltaTime * 8f);
            if (_rightArm != null) _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, Quaternion.Euler(amount.y * pulse, 0f, -amount.z), Time.deltaTime * 8f);
        }

        private static Vector3 GestureAmount(CharacterGesture gesture)
        {
            switch (gesture)
            {
                case CharacterGesture.Invite: return new Vector3(-32f, -48f, 18f);
                case CharacterGesture.Explain: return new Vector3(-20f, -58f, 24f);
                case CharacterGesture.Refuse: return new Vector3(25f, 25f, 32f);
                case CharacterGesture.Inspect: return new Vector3(-45f, -18f, 8f);
                case CharacterGesture.Celebrate: return new Vector3(-105f, -105f, 12f);
                case CharacterGesture.Mourn: return new Vector3(18f, 18f, 5f);
                default: return Vector3.zero;
            }
        }

        private void ApplyAnimatorEmotion()
        {
            if (_animator == null) return;
            SetIntegerIfPresent("Emotion", (int)Emotion);
            SetFloatIfPresent("Concern", Emotion == CharacterEmotion.Concerned || Emotion == CharacterEmotion.Grieving ? 1f : 0f);
            SetFloatIfPresent("Hope", Emotion == CharacterEmotion.Hopeful ? 1f : 0f);
        }

        private Transform FindPart(string part)
        {
            return GetComponentsInChildren<Transform>(true).FirstOrDefault(item => string.Equals(item.name, part, StringComparison.OrdinalIgnoreCase));
        }

        private bool HasParameter(string name, AnimatorControllerParameterType type)
        {
            return _animator != null && _animator.parameters.Any(parameter => parameter.name == name && parameter.type == type);
        }

        private void SetFloatIfPresent(string name, float value) { if (HasParameter(name, AnimatorControllerParameterType.Float)) _animator.SetFloat(name, value); }
        private void SetIntegerIfPresent(string name, int value) { if (HasParameter(name, AnimatorControllerParameterType.Int)) _animator.SetInteger(name, value); }
        private void SetBoolIfPresent(string name, bool value) { if (HasParameter(name, AnimatorControllerParameterType.Bool)) _animator.SetBool(name, value); }
        private void SetTriggerIfPresent(string name) { if (HasParameter(name, AnimatorControllerParameterType.Trigger)) _animator.SetTrigger(name); }
    }

    public static class KeeperVisualBuilder
    {
        private static readonly Color[] SkinTones =
        {
            new Color(0.22f, 0.12f, 0.08f), new Color(0.31f, 0.17f, 0.11f), new Color(0.42f, 0.25f, 0.17f),
            new Color(0.56f, 0.36f, 0.25f), new Color(0.70f, 0.49f, 0.34f), new Color(0.82f, 0.63f, 0.47f)
        };

        public static void Apply(Transform keeper, KeeperAppearance appearance)
        {
            if (keeper == null || keeper.Find("PRODUCTION_KEEPER_VISUAL") != null) return;
            var root = new GameObject("PRODUCTION_KEEPER_VISUAL").transform;
            root.SetParent(keeper, false);
            var width = new[] { 0.82f, 1f, 1.16f }[Mathf.Clamp(appearance.BodyFrame, 0, 2)];
            var skin = SkinTones[Mathf.Clamp(appearance.SkinTone, 0, SkinTones.Length - 1)];
            Part(root, "FieldCoat", PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(0.62f * width, 1.0f, 0.48f * width), CoatColor(appearance.CoatPattern));
            Part(root, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.12f, 0f), Vector3.one * 0.46f, skin);
            BuildHair(root, appearance.HairStyle, width);
            Part(root, "LeftGlove", PrimitiveType.Sphere, new Vector3(-0.58f * width, 0.98f, -0.08f), Vector3.one * 0.18f, new Color(0.08f, 0.10f, 0.11f));
            Part(root, "RightGlove", PrimitiveType.Sphere, new Vector3(0.58f * width, 0.98f, -0.08f), Vector3.one * 0.18f, new Color(0.08f, 0.10f, 0.11f));
            BuildLens(root, appearance.LensStyle);
            var rig = keeper.gameObject.GetComponent<CharacterPerformanceRig>() ?? keeper.gameObject.AddComponent<CharacterPerformanceRig>();
            rig.Configure("Keeper", "Keeper of Unfinished Things", CharacterEmotion.Curious);
        }

        private static void BuildHair(Transform root, int style, float width)
        {
            var count = Mathf.Clamp(style + 2, 2, 6);
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / count;
                Part(root, "Hair_" + i, PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * 0.27f * width, 2.39f, Mathf.Sin(angle) * 0.23f), new Vector3(0.22f, 0.16f + style * 0.035f, 0.22f), new Color(0.035f, 0.025f, 0.02f));
            }
        }

        private static void BuildLens(Transform root, int style)
        {
            var anchor = new GameObject("ResonanceLens_" + style).transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0.48f, 1.58f, -0.34f);
            Part(anchor, "BrassFrame", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.20f + style * 0.04f, 0.035f, 0.20f + style * 0.04f), CivicMaterialLibrary.SurfaceColor(CivicSurface.Brass)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(anchor, "LensGlass", PrimitiveType.Sphere, Vector3.zero, Vector3.one * (0.16f + style * 0.025f), CivicMaterialLibrary.SurfaceColor(CivicSurface.Glass));
        }

        private static Color CoatColor(int pattern)
        {
            var colors = new[] { new Color(0.10f, 0.19f, 0.20f), new Color(0.27f, 0.09f, 0.12f), new Color(0.18f, 0.15f, 0.28f), new Color(0.23f, 0.24f, 0.16f) };
            return colors[Mathf.Clamp(pattern, 0, colors.Length - 1)];
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var node = GameObject.CreatePrimitive(type);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            node.transform.localScale = scale;
            var collider = node.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);
            var renderer = node.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "IZ_Character_" + name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            renderer.sharedMaterial = material;
            return node;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CharacterProductionDirector : MonoBehaviour
    {
        private IdeaZooGame _game;
        private readonly List<CharacterPerformanceRig> _cast = new List<CharacterPerformanceRig>();
        private CaseStage _lastStage;
        private int _lastEvidence;

        private IEnumerator Start()
        {
            for (var frame = 0; frame < 300; frame++)
            {
                _game = FindFirstObjectByType<IdeaZooGame>();
                if (_game != null && _game.World != null && _game.Keeper != null)
                {
                    BuildCast();
                    yield break;
                }
                yield return null;
            }
        }

        private void BuildCast()
        {
            KeeperVisualBuilder.Apply(_game.Keeper.transform, KeeperAppearance.Load());
            var creature = _game.Creature != null ? _game.Creature.transform : null;
            foreach (var specialist in FindObjectsByType<ProceduralSpecialist>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var rig = specialist.GetComponent<CharacterPerformanceRig>() ?? specialist.gameObject.AddComponent<CharacterPerformanceRig>();
                rig.Configure(specialist.SpecialistName, specialist.Role, StartingEmotion(specialist.SpecialistName));
                rig.LookTarget = creature;
                _cast.Add(rig);
            }
            BuildChildrensJury(creature);
            _lastStage = _game.Director.Stage;
        }

        private void BuildChildrensJury(Transform creature)
        {
            var archive = _game.World.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "03_CENTRAL_ARCHIVE_WALK");
            if (archive == null || archive.Find("CHILDRENS_JURY") != null) return;
            var root = new GameObject("CHILDRENS_JURY").transform;
            root.SetParent(archive, false);
            root.localPosition = new Vector3(-1.8f, 0f, 3.4f);
            var names = new[] { "Lio", "Amara", "Kweku" };
            for (var i = 0; i < names.Length; i++)
            {
                var child = new GameObject(names[i] + "_Jury").transform;
                child.SetParent(root, false);
                child.localPosition = new Vector3(i * 1.25f, 0f, (i % 2) * 0.35f);
                Part(child, "Coat", PrimitiveType.Capsule, new Vector3(0f, 0.70f, 0f), new Vector3(0.38f, 0.62f, 0.34f), i == 0 ? CivicSurface.Rust : i == 1 ? CivicSurface.TealGlow : CivicSurface.Paper);
                Part(child, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.42f, 0f), Vector3.one * 0.34f, CivicSurface.Clay);
                Part(child, "QuestionPlate", PrimitiveType.Cube, new Vector3(0f, 0.72f, -0.30f), new Vector3(0.42f, 0.36f, 0.04f), CivicSurface.Paper);
                var rig = child.gameObject.AddComponent<CharacterPerformanceRig>();
                rig.Configure(names[i], "Children's Jury", CharacterEmotion.Curious, true);
                rig.LookTarget = creature;
                _cast.Add(rig);
            }
        }

        private void Update()
        {
            if (_game == null || _game.Director == null) return;
            var stage = _game.Director.Stage;
            var evidence = _game.Director.Profile != null ? _game.Director.Profile.Evidence.Count : 0;
            if (stage != _lastStage)
            {
                RespondToStage(stage);
                _lastStage = stage;
            }
            if (evidence != _lastEvidence)
            {
                foreach (var rig in _cast) rig.Perform(CharacterGesture.Inspect, 1.2f);
                _lastEvidence = evidence;
            }
        }

        private void RespondToStage(CaseStage stage)
        {
            foreach (var rig in _cast)
            {
                if (stage == CaseStage.Hatching) { rig.SetEmotion(CharacterEmotion.Curious); rig.Perform(CharacterGesture.Invite); }
                else if (stage == CaseStage.Molt) { rig.SetEmotion(CharacterEmotion.Concerned); rig.Perform(CharacterGesture.Explain); }
                else if (stage == CaseStage.Decision) { rig.SetEmotion(CharacterEmotion.Defiant); rig.Perform(CharacterGesture.Refuse); }
                else if (stage == CaseStage.Complete)
                {
                    var ruling = _game.Director.Profile != null ? _game.Director.Profile.FinalRuling : null;
                    var hopeful = ruling == Ruling.Build || ruling == Ruling.Molt || ruling == Ruling.Sanctuary;
                    rig.SetEmotion(hopeful ? CharacterEmotion.Hopeful : CharacterEmotion.Grieving);
                    rig.Perform(hopeful ? CharacterGesture.Celebrate : CharacterGesture.Mourn, 2f);
                }
            }
        }

        private static CharacterEmotion StartingEmotion(string name)
        {
            if (name.Contains("Mara")) return CharacterEmotion.Protective;
            if (name.Contains("Nara")) return CharacterEmotion.Concerned;
            if (name.Contains("Toma")) return CharacterEmotion.Hopeful;
            return CharacterEmotion.Curious;
        }

        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, CivicSurface surface)
        {
            var node = GameObject.CreatePrimitive(type);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            node.transform.localScale = scale;
            var collider = node.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            node.GetComponent<Renderer>().sharedMaterial = CivicMaterialLibrary.Get(surface);
        }
    }

    public static class CharacterProductionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<CharacterProductionDirector>() != null) return;
            new GameObject("IdeaZoo_Character_Production").AddComponent<CharacterProductionDirector>();
        }
    }
}
