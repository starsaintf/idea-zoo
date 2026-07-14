using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Core;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Creatures
{
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class CreatureProductionLifecycle : MonoBehaviour
    {
        private CreatureAssembler _assembler;
        private CreatureProductionRig _rig;
        private IdeaProfile _profile;
        private Transform _productionRoot;
        private readonly Dictionary<Transform, Quaternion> _baseRotations = new Dictionary<Transform, Quaternion>();
        private float _phase;

        private IEnumerator Start()
        {
            for (var frame = 0; frame < 480; frame++)
            {
                var game = FindFirstObjectByType<IdeaZooGame>();
                if (game != null && game.Creature != null)
                {
                    _assembler = game.Creature;
                    _rig = _assembler.GetComponent<CreatureProductionRig>() ?? _assembler.gameObject.AddComponent<CreatureProductionRig>();
                    _rig.Build(_assembler, game.Keeper != null ? game.Keeper.transform : null);
                    yield break;
                }
                yield return null;
            }
        }

        private void Update()
        {
            if (_assembler == null || _rig == null || _assembler.Profile == null) return;
            var currentRoot = FindProductionRoot();
            if (!ReferenceEquals(_profile, _assembler.Profile) || currentRoot == null)
            {
                _profile = _assembler.Profile;
                _rig.Rebuild(_profile);
                _productionRoot = FindProductionRoot();
                _phase = Mathf.Abs(_profile.RecordId.GetHashCode() % 1000) * 0.011f;
                CaptureBaseRotations();
            }
            DisablePrototypeRenderers();
        }

        private void LateUpdate()
        {
            if (_productionRoot == null || _baseRotations.Count == 0) return;
            var speed = _rig != null ? 0.75f + _rig.Agitation * 1.6f : 1f;
            var time = Time.time * speed + _phase;
            var index = 0;
            foreach (var pair in _baseRotations.ToArray())
            {
                if (pair.Key == null) continue;
                var wave = Mathf.Sin(time + index * 0.67f);
                pair.Key.localRotation = pair.Value * Quaternion.Euler(wave * 2.4f, wave * 3.2f, wave * 1.8f);
                index++;
            }
        }

        private Transform FindProductionRoot()
        {
            if (_assembler == null) return null;
            return _assembler.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name.StartsWith("PRODUCTION_CREATURE_LAYER_"));
        }

        private void CaptureBaseRotations()
        {
            _baseRotations.Clear();
            if (_productionRoot == null) return;
            foreach (var child in _productionRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == _productionRoot) continue;
                var name = child.name;
                if (name.Contains("Wing") || name.Contains("WorkingLimb") || name.Contains("MemoryTail") ||
                    name.Contains("CoilSegment") || name.Contains("ChoirBody") || name.Contains("IntentEye") ||
                    name.Contains("Mark_") || name.Contains("PressureRibbon"))
                    _baseRotations[child] = child.localRotation;
            }
        }

        private void DisablePrototypeRenderers()
        {
            var prototype = _assembler.transform.Find("SpecimenBody");
            if (prototype == null) return;
            foreach (var renderer in prototype.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
    }

    public static class CreatureProductionLifecycleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<CreatureProductionLifecycle>() != null) return;
            new GameObject("IdeaZoo_Creature_Production_Lifecycle").AddComponent<CreatureProductionLifecycle>();
        }
    }
}
