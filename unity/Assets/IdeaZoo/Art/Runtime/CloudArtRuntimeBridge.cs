using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Characters;
using IdeaZoo.Core;
using IdeaZoo.Creatures;
using IdeaZoo.Presentation;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.CloudArt
{
    [DisallowMultipleComponent]
    public sealed class CloudArtRuntimeBridge : MonoBehaviour
    {
        private static readonly Color[] SkinTones =
        {
            new Color(0.22f, 0.12f, 0.08f), new Color(0.31f, 0.17f, 0.11f), new Color(0.42f, 0.25f, 0.17f),
            new Color(0.56f, 0.36f, 0.25f), new Color(0.70f, 0.49f, 0.34f), new Color(0.82f, 0.63f, 0.47f)
        };

        private static readonly Color[] CoatColors =
        {
            new Color(0.045f, 0.065f, 0.075f), new Color(0.27f, 0.09f, 0.12f),
            new Color(0.18f, 0.15f, 0.28f), new Color(0.23f, 0.24f, 0.16f)
        };

        private IdeaZooGame _game;
        private Transform _cloudCreature;
        private string _creatureSignature;
        private float _phase;

        private IEnumerator Start()
        {
            for (var frame = 0; frame < 600; frame++)
            {
                _game = FindFirstObjectByType<IdeaZooGame>();
                if (_game != null && _game.Keeper != null && _game.World != null && _game.Creature != null)
                {
                    BindCharacters();
                    RefreshCreature(true);
                    yield break;
                }
                yield return null;
            }
        }

        private void Update()
        {
            if (_game == null || _game.Director == null) return;
            RefreshCreature(false);
            AnimateCreature();
        }

        private void BindCharacters()
        {
            BindKeeper();
            foreach (var specialist in FindObjectsByType<ProceduralSpecialist>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                BindSpecialist(specialist);
            BindJury("Lio_Jury", "Lio", "Children's Jury");
            BindJury("Amara_Jury", "Amara", "Children's Jury");
            BindJury("Kweku_Jury", "Kweku", "Children's Jury");
        }

        private void BindKeeper()
        {
            var appearance = KeeperAppearance.Load();
            var prefab = Resources.Load<GameObject>("IdeaZooArt/Characters/Keeper_" + Mathf.Clamp(appearance.BodyFrame, 0, 2));
            if (prefab == null) return;
            var parent = _game.Keeper.transform;
            if (parent.Find("CLOUD_KEEPER_VISUAL") != null) return;
            var instance = Instantiate(prefab, parent, false);
            instance.name = "CLOUD_KEEPER_VISUAL";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            HideRenderers(parent.Find("PRODUCTION_KEEPER_VISUAL"));
            HideRenderers(parent.Find("KeeperVisual"));
            SelectVariant(instance.transform, "Hair_", Mathf.Clamp(appearance.HairStyle, 0, 4));
            SelectVariant(instance.transform, "Lens_", Mathf.Clamp(appearance.LensStyle, 0, 2));
            ApplyNamedColor(instance.transform, new[] { "HeadMesh", "LeftHandMesh", "RightHandMesh" }, SkinTones[Mathf.Clamp(appearance.SkinTone, 0, 5)]);
            ApplyNamedColor(instance.transform, new[] { "FieldCoat" }, CoatColors[Mathf.Clamp(appearance.CoatPattern, 0, 3)]);
            var rig = parent.GetComponent<CharacterPerformanceRig>() ?? parent.gameObject.AddComponent<CharacterPerformanceRig>();
            rig.Configure("Keeper", "Keeper of Unfinished Things", CharacterEmotion.Curious);
        }

        private static void BindSpecialist(ProceduralSpecialist specialist)
        {
            if (specialist == null || specialist.transform.Find("CLOUD_SPECIALIST_VISUAL") != null) return;
            var key = Sanitize(specialist.SpecialistName);
            var prefab = Resources.Load<GameObject>("IdeaZooArt/Characters/" + key);
            if (prefab == null) return;
            HideRenderers(specialist.transform.Find("Rig"));
            var instance = Instantiate(prefab, specialist.transform, false);
            instance.name = "CLOUD_SPECIALIST_VISUAL";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            var rig = specialist.GetComponent<CharacterPerformanceRig>() ?? specialist.gameObject.AddComponent<CharacterPerformanceRig>();
            rig.Configure(specialist.SpecialistName, specialist.Role, StartingEmotion(specialist.SpecialistName));
            rig.LookTarget = specialist.Creature;
        }

        private void BindJury(string objectName, string displayName, string role)
        {
            var target = _game.World.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == objectName);
            if (target == null || target.Find("CLOUD_JURY_VISUAL") != null) return;
            var prefab = Resources.Load<GameObject>("IdeaZooArt/Characters/" + objectName);
            if (prefab == null) return;
            foreach (var child in target.Cast<Transform>().ToArray()) HideRenderers(child);
            var instance = Instantiate(prefab, target, false);
            instance.name = "CLOUD_JURY_VISUAL";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            var rig = target.GetComponent<CharacterPerformanceRig>() ?? target.gameObject.AddComponent<CharacterPerformanceRig>();
            rig.Configure(displayName, role, CharacterEmotion.Curious, true);
            rig.LookTarget = _game.Creature.transform;
        }

        private void RefreshCreature(bool force)
        {
            var profile = _game.Director.Profile;
            if (profile == null) return;
            var genome = CreatureGenome.From(profile);
            var signature = genome.Signature + "|" + profile.Class + "|" + profile.Appetite + "|" + profile.Guardrails.Count + "|" + profile.Assumptions.Count;
            if (!force && signature == _creatureSignature) return;
            _creatureSignature = signature;
            if (_cloudCreature != null) Destroy(_cloudCreature.gameObject);
            var prefab = Resources.Load<GameObject>("IdeaZooArt/Creatures/" + genome.BodyFamily);
            if (prefab == null) return;
            var instance = Instantiate(prefab, _game.Creature.transform, false);
            instance.name = "CLOUD_CREATURE_VISUAL_" + genome.Signature;
            _cloudCreature = instance.transform;
            _cloudCreature.localPosition = Vector3.zero;
            _cloudCreature.localRotation = Quaternion.identity;
            _cloudCreature.localScale = Vector3.one * (0.86f + Mathf.Clamp01((float)profile.Metrics.Evidence) * 0.18f);
            foreach (var child in _game.Creature.transform.Cast<Transform>())
                if (child != _cloudCreature && child.name.StartsWith("PRODUCTION_CREATURE_LAYER_", StringComparison.Ordinal)) HideRenderers(child);
            ApplyCreatureState(profile);
            _phase = Mathf.Abs(genome.StableSeed % 1000) * 0.01f;
        }

        private void ApplyCreatureState(IdeaProfile profile)
        {
            if (_cloudCreature == null) return;
            var guardrails = Mathf.Clamp(profile.Guardrails.Count, 0, 2);
            var burdens = Mathf.Clamp(1 + profile.Assumptions.Count / 2, 0, 2);
            foreach (var item in _cloudCreature.GetComponentsInChildren<Transform>(true))
            {
                if (item.name.StartsWith("GuardrailRing_", StringComparison.Ordinal)) item.gameObject.SetActive(ParseSuffix(item.name) < guardrails);
                if (item.name.StartsWith("BurdenCrate_", StringComparison.Ordinal)) item.gameObject.SetActive(ParseSuffix(item.name) < burdens);
            }
            ApplyPrefixColor(_cloudCreature, new[] { "IntentEye", "InnerIdea", "MemoryTail" }, ClassColor(profile.Class));
            ApplyPrefixColor(_cloudCreature, new[] { "CivicRing", "LoadBack" }, AppetiteColor(profile.Appetite));
        }

        private void AnimateCreature()
        {
            if (_cloudCreature == null || _game == null || _game.Director.Profile == null) return;
            var profile = _game.Director.Profile;
            var trust = Mathf.Clamp01((float)profile.Metrics.Evidence * 0.62f + profile.Guardrails.Count * 0.09f);
            var hunger = Mathf.Clamp01(0.78f - (float)profile.Metrics.Viability * 0.36f + (profile.Evidence.Count == 0 ? 0.18f : 0f));
            var fear = Mathf.Clamp01(1f - (float)profile.Metrics.Safety + (profile.FinalRuling == Ruling.Break ? 0.28f : 0f));
            var agitation = Mathf.Clamp01(fear * 0.62f + hunger * 0.44f - trust * 0.38f);
            var speed = 0.75f + agitation * 1.7f;
            var wave = Mathf.Sin(Time.time * speed + _phase);
            _cloudCreature.localPosition = Vector3.up * wave * (0.025f + hunger * 0.025f);
            AnimateNamedBones(_cloudCreature, "LeftWing", wave * (18f + agitation * 26f));
            AnimateNamedBones(_cloudCreature, "RightWing", -wave * (18f + agitation * 26f));
            AnimateNamedBones(_cloudCreature, "LeftAppendage", wave * 18f);
            AnimateNamedBones(_cloudCreature, "RightAppendage", -wave * 18f);
            for (var i = 0; i < 7; i++) AnimateNamedBones(_cloudCreature, "Spine_" + i, Mathf.Sin(Time.time * speed + i * 0.55f) * (4f + agitation * 8f));
            for (var i = 0; i < 5; i++) AnimateNamedBones(_cloudCreature, "Voice_" + i, Mathf.Sin(Time.time * speed + i * 1.1f) * 8f);
            var head = Find(_cloudCreature, "Head");
            if (head != null && _game.Keeper != null)
            {
                var direction = _game.Keeper.transform.position + Vector3.up - head.position;
                if (direction.sqrMagnitude > 0.04f)
                {
                    var local = _cloudCreature.InverseTransformDirection(direction.normalized);
                    var yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -42f, 42f);
                    head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(fear * -10f, yaw, 0f), Time.deltaTime * (2f + trust * 3f));
                }
            }
        }

        private static void AnimateNamedBones(Transform root, string name, float amount)
        {
            var bone = Find(root, name);
            if (bone != null) bone.localRotation = Quaternion.Euler(0f, amount, amount * 0.35f);
        }

        private static CharacterEmotion StartingEmotion(string name)
        {
            if (name.Contains("Mara")) return CharacterEmotion.Protective;
            if (name.Contains("Nara")) return CharacterEmotion.Concerned;
            if (name.Contains("Toma")) return CharacterEmotion.Hopeful;
            return CharacterEmotion.Curious;
        }

        private static void SelectVariant(Transform root, string prefix, int selected)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                if (!item.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                item.gameObject.SetActive(ParseSuffix(item.name) == selected);
            }
        }

        private static int ParseSuffix(string value)
        {
            var digits = new string(value.SkipWhile(character => !char.IsDigit(character)).TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var result) ? result : 0;
        }

        private static void ApplyNamedColor(Transform root, IEnumerable<string> names, Color color)
        {
            var set = new HashSet<string>(names);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (set.Contains(renderer.name)) ApplyColor(renderer, color);
        }

        private static void ApplyPrefixColor(Transform root, IEnumerable<string> prefixes, Color color)
        {
            var list = prefixes.ToArray();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (list.Any(prefix => renderer.name.StartsWith(prefix, StringComparison.Ordinal))) ApplyColor(renderer, color);
        }

        private static void ApplyColor(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private static Color ClassColor(IdeaClass ideaClass)
        {
            switch (ideaClass)
            {
                case IdeaClass.Fleck: return new Color(0.72f, 0.84f, 0.76f);
                case IdeaClass.Hand: return new Color(0.58f, 0.39f, 0.12f);
                case IdeaClass.Mirror: return new Color(0.18f, 0.48f, 0.50f);
                case IdeaClass.Teeth: return new Color(0.48f, 0.13f, 0.10f);
                case IdeaClass.Swarm: return new Color(0.42f, 0.34f, 0.58f);
                case IdeaClass.Weather: return new Color(0.12f, 0.84f, 0.76f);
                default: return new Color(0.20f, 0.31f, 0.16f);
            }
        }

        private static Color AppetiteColor(Appetite appetite)
        {
            switch (appetite)
            {
                case Appetite.Money: return new Color(0.58f, 0.39f, 0.12f);
                case Appetite.Obedience: return new Color(0.48f, 0.13f, 0.10f);
                case Appetite.Care: return new Color(0.20f, 0.31f, 0.16f);
                case Appetite.Data: return new Color(0.18f, 0.48f, 0.50f);
                case Appetite.Time: return new Color(0.42f, 0.34f, 0.58f);
                default: return new Color(0.12f, 0.84f, 0.76f);
            }
        }

        private static string Sanitize(string value)
        {
            return string.Concat((value ?? string.Empty).Select(character => char.IsLetterOrDigit(character) ? character : '_')).Trim('_');
        }

        private static Transform Find(Transform root, string name)
        {
            return root == null ? null : root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
        }

        private static void HideRenderers(Transform root)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
    }

    public static class CloudArtRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<CloudArtRuntimeBridge>() != null) return;
            new GameObject("IdeaZoo_Cloud_Art_Bridge").AddComponent<CloudArtRuntimeBridge>();
        }
    }
}
