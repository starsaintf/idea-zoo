using System.Collections;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    public sealed class SpecimenProportionGuard : MonoBehaviour
    {
        private CreatureAssembler _creature;

        public void Build(CreatureAssembler creature)
        {
            _creature = creature;
        }

        private void LateUpdate()
        {
            if (_creature == null || _creature.Profile == null || !_creature.gameObject.activeInHierarchy) return;
            var root = _creature.transform.Find("Authored_Specimen_Parts");
            if (root == null) return;
            var definition = Mathf.Lerp(0.62f, 1f, Mathf.Clamp01((float)_creature.Profile.Metrics.Evidence));
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                Vector3 baseScale;
                if (!TryBaseScale(child.name, out baseScale)) continue;
                child.localScale = baseScale * definition;
            }
        }

        private static bool TryBaseScale(string name, out Vector3 scale)
        {
            scale = Vector3.one;
            int index;
            if (TryIndex(name, "TaskPlate_", out index))
            {
                scale = new Vector3(0.28f, 0.08f, 0.40f);
                return true;
            }
            if (TryIndex(name, "ReflectionShard_", out index))
            {
                scale = new Vector3(0.20f, 0.42f, 0.04f);
                return true;
            }
            if (TryIndex(name, "CivicTooth_", out index))
            {
                scale = new Vector3(0.12f, 0.40f, 0.10f);
                return true;
            }
            if (TryIndex(name, "ArchivePlate_", out index))
            {
                scale = new Vector3(1.05f - index * 0.10f, 0.12f, 0.52f);
                return true;
            }
            if (TryIndex(name, "DataPrism_", out index))
            {
                scale = new Vector3(0.15f, 0.35f, 0.08f);
                return true;
            }
            return false;
        }

        private static bool TryIndex(string name, string prefix, out int index)
        {
            index = 0;
            return name.StartsWith(prefix) && int.TryParse(name.Substring(prefix.Length), out index);
        }
    }

    [DisallowMultipleComponent]
    public sealed class SpecimenProportionGuardAutoLoad : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (var attempt = 0; attempt < 120; attempt++)
            {
                var creature = FindFirstObjectByType<CreatureAssembler>();
                if (creature != null)
                {
                    var guard = creature.GetComponent<SpecimenProportionGuard>();
                    if (guard == null) guard = creature.gameObject.AddComponent<SpecimenProportionGuard>();
                    guard.Build(creature);
                    yield break;
                }
                yield return null;
            }
        }
    }

    public static class SpecimenProportionGuardBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGuard()
        {
            if (Object.FindFirstObjectByType<SpecimenProportionGuardAutoLoad>() != null) return;
            var root = new GameObject("IdeaZoo_Specimen_Proportion_Guard");
            root.AddComponent<SpecimenProportionGuardAutoLoad>();
            Object.DontDestroyOnLoad(root);
        }
    }
}
