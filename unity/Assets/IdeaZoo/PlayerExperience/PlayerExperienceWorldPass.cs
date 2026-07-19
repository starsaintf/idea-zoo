using System;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Core;
using IdeaZoo.Presentation;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.PlayerExperience
{
    [DisallowMultipleComponent]
    public sealed class PlayerExperienceWorldPass : MonoBehaviour
    {
        public const int MaximumVisibleConsequences = 8;

        private readonly List<GameObject> _monuments = new List<GameObject>(MaximumVisibleConsequences);
        private readonly List<TextMesh> _labels = new List<TextMesh>(MaximumVisibleConsequences);
        private Transform _root;
        private Transform _garden;

        public void Build(WhisperGateWorld world)
        {
            if (world == null || _root != null) return;
            _root = new GameObject("PLAYER_EXPERIENCE_CONSEQUENCES").transform;
            _root.SetParent(world.transform, false);
            _garden = Find(world.transform, "10_DECISION_GARDEN") ?? Find(world.transform, "03_CENTRAL_ARCHIVE_WALK") ?? world.transform;
            for (var i = 0; i < MaximumVisibleConsequences; i++) CreateMonument(i);
        }

        public void Refresh(PlayerExperienceState state)
        {
            if (_root == null || state == null || state.Cases == null) return;
            var completed = state.Cases.Where(item => item != null && item.HasRuling && item.Revealed).ToArray();
            var count = Mathf.Min(MaximumVisibleConsequences, completed.Length);
            var start = Mathf.Max(0, completed.Length - count);
            for (var i = 0; i < _monuments.Count; i++)
            {
                var active = i < count;
                _monuments[i].SetActive(active);
                if (!active) continue;
                var record = completed[start + i];
                Configure(_monuments[i], _labels[i], record, i);
            }
        }

        private void CreateMonument(int index)
        {
            var root = new GameObject("PlayerConsequence_" + index);
            root.transform.SetParent(_root, false);

            var baseNode = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseNode.name = "Base";
            baseNode.transform.SetParent(root.transform, false);
            baseNode.transform.localScale = new Vector3(.8f, .12f, .8f);
            RemoveCollider(baseNode);

            var symbol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            symbol.name = "Symbol";
            symbol.transform.SetParent(root.transform, false);
            symbol.transform.localPosition = new Vector3(0f, .72f, 0f);
            symbol.transform.localScale = new Vector3(.6f, 1.1f, .22f);
            RemoveCollider(symbol);

            var labelObject = new GameObject("ConsequenceLabel");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.6f, -.1f);
            labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.fontSize = 26;
            label.characterSize = .045f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(.94f, .90f, .78f);

            _monuments.Add(root);
            _labels.Add(label);
        }

        private void Configure(GameObject monument, TextMesh label, PlayerExperienceCaseRecord record, int index)
        {
            var row = index / 4;
            var column = index % 4;
            monument.transform.position = _garden.TransformPoint(new Vector3(-4.5f + column * 3f, 0f, 4f + row * 2.7f));
            monument.transform.rotation = _garden.rotation;

            var symbol = monument.transform.Find("Symbol");
            var baseRenderer = monument.transform.Find("Base").GetComponent<Renderer>();
            var symbolRenderer = symbol.GetComponent<Renderer>();
            baseRenderer.sharedMaterial = CivicMaterialLibrary.Get(CivicSurface.Brass);

            if (record.Ruling == Ruling.Build)
            {
                symbol.localScale = new Vector3(.55f, 1.4f, .55f);
                symbol.localRotation = Quaternion.Euler(0f, 45f, 0f);
                symbolRenderer.sharedMaterial = CivicMaterialLibrary.Get(CivicSurface.TealGlow);
            }
            else if (record.Ruling == Ruling.Break)
            {
                symbol.localScale = new Vector3(.9f, .28f, .25f);
                symbol.localRotation = Quaternion.Euler(0f, 0f, 18f);
                symbolRenderer.sharedMaterial = CivicMaterialLibrary.Get(CivicSurface.Rust);
            }
            else if (record.Ruling == Ruling.Sanctuary)
            {
                symbol.localScale = new Vector3(.72f, .72f, .72f);
                symbol.localRotation = Quaternion.Euler(45f, 45f, 0f);
                symbolRenderer.sharedMaterial = CivicMaterialLibrary.Get(CivicSurface.Glass);
            }
            else if (record.Ruling == Ruling.Molt)
            {
                symbol.localScale = new Vector3(.95f, .12f, .95f);
                symbol.localRotation = Quaternion.Euler(0f, index * 31f, 0f);
                symbolRenderer.sharedMaterial = CivicMaterialLibrary.Get(CivicSurface.Paper);
            }
            else
            {
                symbol.localScale = new Vector3(.62f, 1f, .62f);
                symbol.localRotation = Quaternion.identity;
                symbolRenderer.sharedMaterial = CivicMaterialLibrary.Get(CivicSurface.Ink);
            }

            label.text = Short(record.Title, 22) + "\n" + record.Ruling.ToString().ToUpperInvariant() + " · " + Short(PlayerExperienceArchetypeCatalog.Reveal(record.Archetype), 28);
        }

        private static void RemoveCollider(GameObject node)
        {
            var collider = node.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);
        }

        private static string Short(string value, int length)
        {
            if (string.IsNullOrWhiteSpace(value)) return "UNTITLED";
            var text = value.Trim();
            return text.Length <= length ? text : text.Substring(0, length - 1) + "…";
        }

        private static Transform Find(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++) if (all[i].name == name) return all[i];
            return null;
        }
    }
}
