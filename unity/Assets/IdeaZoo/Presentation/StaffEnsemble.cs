using System.Collections.Generic;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    public enum SpecialistTool
    {
        HatchFork,
        ReleaseStaff,
        AppetiteLens,
        MoltSpool,
        CounterfactualFrames,
        MercyBell
    }

    [DisallowMultipleComponent]
    public sealed class StaffEnsemble : MonoBehaviour
    {
        private readonly List<ProceduralSpecialist> _staff = new List<ProceduralSpecialist>();

        public void Build(Transform world, Transform keeper, Transform creature)
        {
            if (world == null || _staff.Count > 0) return;
            Add(world, "04_DESIRE_YARD", "Mara Rook", "Hatchkeeper", SpecialistTool.HatchFork, new Vector3(-5.0f, 0f, -2.7f), new Color(0.29f, 0.10f, 0.16f), 1.72f, 0.58f, keeper, creature);
            Add(world, "05_COMMITMENT_PADDOCK", "Toma Reed", "Release Shepherd", SpecialistTool.ReleaseStaff, new Vector3(5.2f, 0f, -3.0f), new Color(0.17f, 0.34f, 0.31f), 1.86f, 0.55f, keeper, creature);
            Add(world, "06_BURROWER_TUNNEL", "Sefu Anik", "Appetite Reader", SpecialistTool.AppetiteLens, new Vector3(-2.4f, 0f, -3.8f), new Color(0.32f, 0.28f, 0.12f), 1.78f, 0.62f, keeper, creature);
            Add(world, "08_MOLT_HOUSE", "Elian Thread", "Molt Surgeon", SpecialistTool.MoltSpool, new Vector3(4.6f, 0f, -2.5f), new Color(0.24f, 0.16f, 0.32f), 1.82f, 0.56f, keeper, creature);
            Add(world, "03_CENTRAL_ARCHIVE_WALK", "Sen Osei", "Counterfactual Veterinarian", SpecialistTool.CounterfactualFrames, new Vector3(3.8f, 0f, -1.8f), new Color(0.10f, 0.18f, 0.25f), 1.88f, 0.57f, keeper, creature);
            Add(world, "07_REFUSAL_GATE", "Nara Voss", "Mercy Butcher", SpecialistTool.MercyBell, new Vector3(-4.8f, 0f, -2.8f), new Color(0.42f, 0.40f, 0.36f), 1.76f, 0.54f, keeper, creature);
        }

        public void SignalAll()
        {
            foreach (var specialist in _staff) specialist.Gesture(1.8f);
        }

        private void Add(Transform world, string department, string person, string role, SpecialistTool tool, Vector3 position, Color coat, float height, float width, Transform keeper, Transform creature)
        {
            var root = Find(world, department) ?? world;
            var node = new GameObject(person.Replace(' ', '_'));
            node.transform.SetParent(root, false);
            node.transform.localPosition = position;
            node.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var specialist = node.AddComponent<ProceduralSpecialist>();
            specialist.Phase = _staff.Count * 1.21f;
            specialist.Build(person, role, tool, coat, height, width);
            specialist.SetTargets(keeper, creature);
            _staff.Add(specialist);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
