using System.Collections;
using System.Collections.Generic;
using IdeaZoo.Core;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SpecimenPresentationEnhancer : MonoBehaviour
    {
        private CreatureAssembler _creature;
        private Transform _parts;
        private readonly List<Transform> _orbiters = new List<Transform>();
        private readonly List<Transform> _definitionParts = new List<Transform>();
        private string _recordId = string.Empty;
        private int _revisionCount = -1;
        private int _guardrailCount = -1;
        private float _evidence = -1f;
        private float _time;

        public void Build(CreatureAssembler creature)
        {
            _creature = creature;
        }

        private void Update()
        {
            if (_creature == null || _creature.Profile == null || !_creature.gameObject.activeInHierarchy) return;
            var profile = _creature.Profile;
            if (profile.RecordId != _recordId || profile.Revisions.Count != _revisionCount)
            {
                _recordId = profile.RecordId;
                _revisionCount = profile.Revisions.Count;
                Rebuild(profile);
            }
            if (_guardrailCount != profile.Guardrails.Count || Mathf.Abs(_evidence - (float)profile.Metrics.Evidence) > 0.01f)
            {
                _guardrailCount = profile.Guardrails.Count;
                _evidence = (float)profile.Metrics.Evidence;
                UpdateDefinition(profile);
            }
            Animate(profile);
        }

        private void Rebuild(IdeaProfile profile)
        {
            if (_parts != null) Destroy(_parts.gameObject);
            _parts = new GameObject("Authored_Specimen_Parts").transform;
            _parts.SetParent(_creature.transform, false);
            _orbiters.Clear();
            _definitionParts.Clear();
            RethemeBody(profile.Class);
            BuildClassSilhouette(profile.Class);
            BuildAppetiteMark(profile.Appetite);
            BuildBurden(profile);
            BuildGuardrails(profile.Guardrails.Count);
            UpdateDefinition(profile);
        }

        private void RethemeBody(IdeaClass ideaClass)
        {
            var surface = CivicSurface.Paper;
            if (ideaClass == IdeaClass.Hand) surface = CivicSurface.Brass;
            else if (ideaClass == IdeaClass.Mirror) surface = CivicSurface.Glass;
            else if (ideaClass == IdeaClass.Teeth) surface = CivicSurface.Rust;
            else if (ideaClass == IdeaClass.Swarm) surface = CivicSurface.TealGlow;
            else if (ideaClass == IdeaClass.Weather) surface = CivicSurface.Ink;
            else if (ideaClass == IdeaClass.Burrower) surface = CivicSurface.Moss;

            foreach (var renderer in _creature.GetComponentsInChildren<Renderer>(true))
            {
                if (_parts != null && renderer.transform.IsChildOf(_parts)) continue;
                if (renderer.name.ToLowerInvariant().Contains("burden")) renderer.sharedMaterial = CivicMaterialLibrary.Get(CivicSurface.Rust);
                else renderer.sharedMaterial = CivicMaterialLibrary.Get(surface);
            }
        }

        private void BuildClassSilhouette(IdeaClass ideaClass)
        {
            if (ideaClass == IdeaClass.Fleck)
            {
                Wing("PaperWing_L", new Vector3(-0.55f, 1.05f, 0.05f), -28f);
                Wing("PaperWing_R", new Vector3(0.55f, 1.05f, 0.05f), 28f);
                CivicKit.Sphere(_parts, "GentleLight", new Vector3(0f, 1.45f, -0.28f), Vector3.one * 0.24f, CivicSurface.TealGlow);
            }
            else if (ideaClass == IdeaClass.Hand)
            {
                CivicKit.Rail(_parts, "WorkingHarness", new Vector3(-0.72f, 0.62f, 0.34f), new Vector3(0.72f, 0.62f, 0.34f), 3);
                CivicKit.Box(_parts, "LoadPlate", new Vector3(0f, 1.0f, 0.38f), new Vector3(1.25f, 0.12f, 0.72f), CivicSurface.Brass);
                for (var i = 0; i < 3; i++) _definitionParts.Add(CivicKit.Box(_parts, "TaskPlate_" + i, new Vector3(-0.42f + i * 0.42f, 1.16f, 0.40f), new Vector3(0.28f, 0.08f, 0.40f), CivicSurface.Paper).transform);
            }
            else if (ideaClass == IdeaClass.Mirror)
            {
                Antler("GlassAntler_L", -1f);
                Antler("GlassAntler_R", 1f);
                for (var i = 0; i < 4; i++)
                {
                    var shard = CivicKit.Box(_parts, "ReflectionShard_" + i, new Vector3((i - 1.5f) * 0.28f, 1.05f + (i % 2) * 0.18f, -0.52f), new Vector3(0.20f, 0.42f, 0.04f), CivicSurface.Glass);
                    shard.transform.localRotation = Quaternion.Euler(0f, i * 12f - 18f, i * 9f - 13f);
                    _definitionParts.Add(shard.transform);
                }
            }
            else if (ideaClass == IdeaClass.Teeth)
            {
                for (var i = 0; i < 7; i++)
                {
                    var angle = Mathf.Lerp(-62f, 62f, i / 6f) * Mathf.Deg2Rad;
                    var tooth = CivicKit.Box(_parts, "CivicTooth_" + i, new Vector3(Mathf.Sin(angle) * 0.62f, 1.20f + Mathf.Cos(angle) * 0.20f, -0.54f), new Vector3(0.12f, 0.40f, 0.10f), CivicSurface.Paper);
                    tooth.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                    _definitionParts.Add(tooth.transform);
                }
                CivicKit.Rail(_parts, "CrownFrame", new Vector3(-0.70f, 1.58f, 0f), new Vector3(0.70f, 1.58f, 0f), 4);
            }
            else if (ideaClass == IdeaClass.Swarm)
            {
                for (var i = 0; i < 16; i++)
                {
                    var angle = i * Mathf.PI * 2f / 16f;
                    var body = CivicKit.Sphere(_parts, "ChoirBody_" + i, new Vector3(Mathf.Cos(angle) * (0.72f + i % 3 * 0.14f), 0.95f + Mathf.Sin(angle * 2f) * 0.34f, Mathf.Sin(angle) * 0.72f), Vector3.one * (0.10f + i % 2 * 0.05f), i % 4 == 0 ? CivicSurface.Rust : CivicSurface.TealGlow);
                    _orbiters.Add(body.transform);
                }
            }
            else if (ideaClass == IdeaClass.Weather)
            {
                for (var i = 0; i < 6; i++)
                {
                    var ribbon = CivicKit.Banner(_parts, "PressureRibbon_" + i, new Vector3(0f, 0.8f + i * 0.22f, 0f), new Vector2(1.6f + i * 0.35f, 0.24f), i % 3 == 0 ? CivicSurface.Rust : CivicSurface.Ink, 0.18f);
                    ribbon.localRotation = Quaternion.Euler(8f * i, i * 31f, 12f * Mathf.Sin(i));
                    _orbiters.Add(ribbon);
                }
            }
            else
            {
                for (var i = 0; i < 5; i++)
                {
                    var plate = CivicKit.Box(_parts, "ArchivePlate_" + i, new Vector3(0f, 0.55f + i * 0.22f, 0.38f + i * 0.05f), new Vector3(1.05f - i * 0.10f, 0.12f, 0.52f), i == 0 ? CivicSurface.Brass : CivicSurface.Paper);
                    plate.transform.localRotation = Quaternion.Euler(0f, i * 3f, i % 2 == 0 ? 3f : -3f);
                    _definitionParts.Add(plate.transform);
                }
                CivicKit.PipeRun(_parts, "MaintenanceSpine", new[] { new Vector3(-0.55f, 0.45f, 0.20f), new Vector3(-0.55f, 1.35f, 0.20f), new Vector3(0.55f, 1.35f, 0.20f), new Vector3(0.55f, 0.45f, 0.20f) }, 0.055f, CivicSurface.Brass);
            }
        }

        private void BuildAppetiteMark(Appetite appetite)
        {
            var root = new GameObject("Appetite_" + appetite).transform;
            root.SetParent(_parts, false);
            if (appetite == Appetite.Attention)
            {
                for (var i = 0; i < 5; i++)
                {
                    var eye = CivicKit.Sphere(root, "AudienceEye_" + i, new Vector3((i - 2) * 0.25f, 1.30f + Mathf.Abs(i - 2) * 0.08f, -0.62f), new Vector3(0.12f, 0.08f, 0.06f), CivicSurface.TealGlow);
                    _orbiters.Add(eye.transform);
                }
            }
            else if (appetite == Appetite.Data)
            {
                for (var i = 0; i < 4; i++)
                {
                    var prism = CivicKit.Box(root, "DataPrism_" + i, new Vector3(-0.42f + i * 0.28f, 0.70f + i * 0.18f, -0.56f), new Vector3(0.15f, 0.35f, 0.08f), CivicSurface.Glass);
                    prism.transform.localRotation = Quaternion.Euler(0f, i * 11f, i * 8f - 12f);
                    _definitionParts.Add(prism.transform);
                }
            }
            else if (appetite == Appetite.Money)
            {
                for (var i = 0; i < 5; i++) CivicKit.Cylinder(root, "ValueToken_" + i, new Vector3(-0.40f + i * 0.20f, 0.62f + (i % 2) * 0.17f, -0.58f), new Vector3(0.16f, 0.035f, 0.16f), CivicSurface.Brass).transform.localRotation = Quaternion.Euler(90f, i * 19f, 0f);
            }
            else if (appetite == Appetite.Trust)
            {
                CivicKit.Sphere(root, "TrustNode_L", new Vector3(-0.34f, 1.20f, -0.60f), Vector3.one * 0.18f, CivicSurface.TealGlow);
                CivicKit.Sphere(root, "TrustNode_R", new Vector3(0.34f, 1.20f, -0.60f), Vector3.one * 0.18f, CivicSurface.TealGlow);
                CivicKit.Beam(root, "TrustBridge", new Vector3(-0.34f, 1.20f, -0.60f), new Vector3(0.34f, 1.20f, -0.60f), 0.035f, CivicSurface.Paper);
            }
            else if (appetite == Appetite.Obedience)
            {
                CivicKit.Cylinder(root, "CommandCollar", new Vector3(0f, 1.10f, 0f), new Vector3(0.72f, 0.10f, 0.72f), CivicSurface.Rust);
            }
            else if (appetite == Appetite.Labour)
            {
                CivicKit.Rail(root, "LabourRack", new Vector3(-0.70f, 0.80f, 0.46f), new Vector3(0.70f, 0.80f, 0.46f), 4);
            }
            else if (appetite == Appetite.Care)
            {
                for (var i = 0; i < 5; i++) Wing("CareLeaf_" + i, new Vector3((i - 2) * 0.22f, 0.80f + i % 2 * 0.18f, -0.50f), i * 14f - 28f);
            }
            else
            {
                for (var i = 0; i < 6; i++)
                {
                    var spine = CivicKit.Box(root, "ClockSpine_" + i, new Vector3(0f, 0.50f + i * 0.20f, 0.48f), new Vector3(0.08f, 0.28f, 0.08f), i % 2 == 0 ? CivicSurface.Brass : CivicSurface.TealGlow);
                    spine.transform.localRotation = Quaternion.Euler(i * 8f, 0f, i % 2 == 0 ? 14f : -14f);
                }
            }
        }

        private void BuildBurden(IdeaProfile profile)
        {
            var burden = new GameObject("Authored_Hidden_Burden").transform;
            burden.SetParent(_parts, false);
            var weight = 0.65f + (1f - (float)profile.Metrics.Feasibility) * 0.65f;
            CivicKit.Box(burden, "BurdenCrate", new Vector3(0f, 0.70f, 0.58f), new Vector3(1.15f * weight, 0.58f, 0.62f), CivicSurface.Rust);
            CivicKit.Beam(burden, "BurdenTether_L", new Vector3(-0.42f, 0.86f, 0.26f), new Vector3(-0.42f, 0.86f, 0.82f), 0.04f, CivicSurface.Brass);
            CivicKit.Beam(burden, "BurdenTether_R", new Vector3(0.42f, 0.86f, 0.26f), new Vector3(0.42f, 0.86f, 0.82f), 0.04f, CivicSurface.Brass);
        }

        private void BuildGuardrails(int count)
        {
            var root = new GameObject("Guardrail_Rings").transform;
            root.SetParent(_parts, false);
            for (var i = 0; i < count; i++)
            {
                var ring = CivicKit.Cylinder(root, "RuleRing_" + i, new Vector3(0f, 0.42f + i * 0.18f, 0f), new Vector3(0.72f + i * 0.035f, 0.035f, 0.72f + i * 0.035f), i % 2 == 0 ? CivicSurface.Brass : CivicSurface.TealGlow);
                ring.transform.localRotation = Quaternion.Euler(0f, i * 13f, 0f);
            }
        }

        private void UpdateDefinition(IdeaProfile profile)
        {
            var evidence = Mathf.Clamp01((float)profile.Metrics.Evidence);
            for (var i = 0; i < _definitionParts.Count; i++)
            {
                var scale = Mathf.Lerp(0.62f, 1f, evidence);
                _definitionParts[i].localScale = Vector3.one * scale;
            }
            var guardrail = _parts != null ? _parts.Find("Guardrail_Rings") : null;
            if (guardrail != null) guardrail.localScale = Vector3.one * Mathf.Lerp(0.80f, 1.08f, Mathf.Clamp01(profile.Guardrails.Count / 6f));
        }

        private void Animate(IdeaProfile profile)
        {
            _time += Time.deltaTime;
            for (var i = 0; i < _orbiters.Count; i++)
            {
                if (_orbiters[i] == null) continue;
                var angle = _time * (0.55f + i % 3 * 0.18f) + i * 1.3f;
                var basePosition = _orbiters[i].localPosition;
                basePosition.y += Mathf.Sin(angle * 1.7f) * 0.0025f;
                _orbiters[i].localRotation = Quaternion.Euler(Mathf.Sin(angle) * 8f, angle * Mathf.Rad2Deg, Mathf.Cos(angle) * 6f);
            }
            if (_parts != null)
            {
                var confidence = Mathf.Lerp(1.8f, 0.55f, Mathf.Clamp01((float)profile.Metrics.Evidence));
                _parts.localRotation = Quaternion.Euler(0f, Mathf.Sin(_time * confidence) * 2.5f, Mathf.Sin(_time * 0.8f) * 0.8f);
            }
        }

        private void Wing(string name, Vector3 position, float angle)
        {
            var wing = CivicKit.Banner(_parts, name, position, new Vector2(0.75f, 1.15f), CivicSurface.Paper, 0.18f);
            wing.localRotation = Quaternion.Euler(0f, angle, angle * 0.45f);
            _definitionParts.Add(wing);
        }

        private void Antler(string name, float side)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_parts, false);
            var start = new Vector3(side * 0.32f, 1.42f, 0f);
            var middle = new Vector3(side * 0.65f, 1.90f, 0f);
            var end = new Vector3(side * 0.95f, 2.24f, side * 0.08f);
            CivicKit.Beam(root, "Main", start, middle, 0.055f, CivicSurface.Glass);
            CivicKit.Beam(root, "Crown", middle, end, 0.045f, CivicSurface.Glass);
            CivicKit.Beam(root, "Branch", middle, middle + new Vector3(side * 0.40f, -0.08f, -0.28f), 0.04f, CivicSurface.Glass);
        }
    }

    [DisallowMultipleComponent]
    public sealed class SpecimenPresentationAutoLoad : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (var attempt = 0; attempt < 120; attempt++)
            {
                var creature = FindFirstObjectByType<CreatureAssembler>();
                if (creature != null)
                {
                    var enhancer = creature.GetComponent<SpecimenPresentationEnhancer>();
                    if (enhancer == null) enhancer = creature.gameObject.AddComponent<SpecimenPresentationEnhancer>();
                    enhancer.Build(creature);
                    yield break;
                }
                yield return null;
            }
        }
    }

    public static class SpecimenPresentationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartEnhancer()
        {
            if (Object.FindFirstObjectByType<SpecimenPresentationAutoLoad>() != null) return;
            var root = new GameObject("IdeaZoo_Specimen_Presentation");
            root.AddComponent<SpecimenPresentationAutoLoad>();
            Object.DontDestroyOnLoad(root);
        }
    }
}
