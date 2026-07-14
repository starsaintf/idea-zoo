using System.Collections;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    public enum AuthoredDetailTier { Landmark, Department, Decorative }

    [DisallowMultipleComponent]
    public sealed class AuthoredEnvironmentDetail : MonoBehaviour
    {
        public AuthoredDetailTier Tier;
    }

    [DisallowMultipleComponent]
    public sealed class AuthoredEnvironmentPass : MonoBehaviour
    {
        private Transform _world;
        private Transform _root;

        public void Build(Transform world)
        {
            if (world == null || _root != null) return;
            _world = world;
            _root = new GameObject("AUTHORED_ENVIRONMENT_KIT").transform;
            _root.SetParent(world, false);

            DressWhisperGate(Find("01_WHISPER_GATE"));
            DressHatchery(Find("02_HATCHERY_ROTUNDA"));
            DressArchive(Find("03_CENTRAL_ARCHIVE_WALK"));
            DressDesire(Find("04_DESIRE_YARD"));
            DressCommitment(Find("05_COMMITMENT_PADDOCK"));
            DressBurrower(Find("06_BURROWER_TUNNEL"));
            DressRefusal(Find("07_REFUSAL_GATE"));
            DressMolt(Find("08_MOLT_HOUSE"));
            DressBoard(Find("09_SEALED_BOARD_WING"));
            DressDecision(Find("10_DECISION_GARDEN"));
        }

        private void DressWhisperGate(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "WhisperGate_AuthoredShell");
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 3; i++)
                {
                    var panel = LodMesh(root, "Folded_Intake_Wall", CivicAuthoredMeshFactory.FoldedPanel(3.8f, 4.4f, 0.35f, 12), CivicAuthoredMeshFactory.FoldedPanel(3.8f, 4.4f, 0.35f, 4), CivicSurface.Paper, new Vector3(side * (5.8f + i * 1.7f), 0.2f, -0.4f + i * 0.4f), Quaternion.Euler(0f, side * (-12f - i * 5f), 0f), AuthoredDetailTier.Department);
                    panel.transform.localScale = new Vector3(1f, 1f - i * 0.08f, 1f);
                }
            }
            LodMesh(root, "Whisper_Arch_Ring", CivicAuthoredMeshFactory.ArchRing(2.65f, 3.35f, 0.55f, 28), CivicAuthoredMeshFactory.ArchRing(2.65f, 3.35f, 0.55f, 10), CivicSurface.Brass, new Vector3(0f, 0.15f, -0.72f), Quaternion.identity, AuthoredDetailTier.Landmark);
            MeshObject(root, "Public_Language_Ribbon", CivicAuthoredMeshFactory.Ribbon(8.4f, 0.62f, 0.34f, 20), CivicSurface.TealGlow, new Vector3(0f, 5.5f, -1.25f), Quaternion.Euler(0f, 0f, -4f), AuthoredDetailTier.Landmark, false);
        }

        private void DressHatchery(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Hatchery_AuthoredInstruments");
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                LodMesh(root, "Incubation_Arch_" + i, CivicAuthoredMeshFactory.ArchRing(1.15f, 1.48f, 0.22f, 20), CivicAuthoredMeshFactory.ArchRing(1.15f, 1.48f, 0.22f, 8), CivicSurface.Brass, direction * 3.8f + Vector3.up * 0.25f, Quaternion.Euler(0f, angle, 0f), AuthoredDetailTier.Department);
                MeshObject(root, "Warmth_Pipe_" + i, CivicAuthoredMeshFactory.TubeArc(1.05f, 0.11f, 90f, 12, 8), CivicSurface.Brass, direction * 2.65f + Vector3.up * 1.4f, Quaternion.Euler(90f, angle, 0f), AuthoredDetailTier.Decorative, false);
            }
            MeshObject(root, "Hatching_Pressure_Ribbon", CivicAuthoredMeshFactory.Ribbon(5.4f, 0.36f, 0.52f, 24), CivicSurface.TealGlow, new Vector3(0f, 3.2f, 0f), Quaternion.Euler(0f, 90f, 90f), AuthoredDetailTier.Landmark, false);
        }

        private void DressArchive(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Archive_AuthoredRecords");
            for (var i = -4; i <= 4; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                MeshObject(root, "Classification_Seal_" + i, CivicAuthoredMeshFactory.ClassificationSeal(0.62f, 0.12f, 12), i % 3 == 0 ? CivicSurface.Rust : CivicSurface.Brass, new Vector3(side * 2.7f, 1.35f, i * 1.35f), Quaternion.Euler(0f, side < 0f ? 90f : -90f, 0f), AuthoredDetailTier.Decorative, false);
            }
        }

        private void DressDesire(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Desire_AuthoredListeningScreens");
            for (var i = 0; i < 3; i++)
            {
                var x = -4.3f + i * 4.3f;
                LodMesh(root, "Listening_Fold_" + i, CivicAuthoredMeshFactory.FoldedPanel(2.6f, 2.2f, 0.24f, 10), CivicAuthoredMeshFactory.FoldedPanel(2.6f, 2.2f, 0.24f, 4), CivicSurface.Paper, new Vector3(x, 0.3f, 2.4f), Quaternion.Euler(0f, 180f + (i - 1) * 9f, 0f), AuthoredDetailTier.Department);
                MeshObject(root, "Unmet_Need_Ribbon_" + i, CivicAuthoredMeshFactory.Ribbon(2.4f, 0.22f, 0.22f, 12), CivicSurface.TealGlow, new Vector3(x, 2.85f, 2.25f), Quaternion.Euler(0f, 0f, i % 2 == 0 ? 6f : -6f), AuthoredDetailTier.Decorative, false);
            }
        }

        private void DressCommitment(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Commitment_AuthoredPledges");
            for (var i = 0; i < 7; i++)
            {
                var x = -4.8f + i * 1.6f;
                MeshObject(root, "Pledge_Seal_" + i, CivicAuthoredMeshFactory.ClassificationSeal(0.48f, 0.16f, 10), i < 3 ? CivicSurface.Paper : CivicSurface.Brass, new Vector3(x, 1.2f + (i % 2) * 0.25f, 2.95f), Quaternion.Euler(0f, 0f, i * 13f), AuthoredDetailTier.Decorative, false);
            }
            LodMesh(root, "Commitment_Threshold", CivicAuthoredMeshFactory.ArchRing(2.0f, 2.45f, 0.38f, 24), CivicAuthoredMeshFactory.ArchRing(2.0f, 2.45f, 0.38f, 8), CivicSurface.Brass, new Vector3(0f, 0.25f, -4.65f), Quaternion.identity, AuthoredDetailTier.Landmark);
        }

        private void DressBurrower(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Burrower_AuthoredPipeNetwork");
            for (var i = 0; i < 8; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var z = -1f - (i / 2) * 2.4f;
                var pipe = LodMesh(root, "Maintenance_Elbow_" + i, CivicAuthoredMeshFactory.TubeArc(1.2f, 0.16f, 90f, 14, 10), CivicAuthoredMeshFactory.TubeArc(1.2f, 0.16f, 90f, 5, 6), CivicSurface.Brass, new Vector3(side * 3.25f, 1.25f + (i % 3) * 0.3f, z), Quaternion.Euler(0f, side < 0 ? 90f : -90f, side < 0 ? 0f : 180f), AuthoredDetailTier.Department);
                pipe.transform.localScale = new Vector3(1f, 1f, 1f + (i % 2) * 0.2f);
            }
        }

        private void DressRefusal(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Refusal_AuthoredExitFrames");
            for (var i = 0; i < 5; i++)
            {
                var x = -5f + i * 2.5f;
                var surface = i == 2 ? CivicSurface.TealGlow : CivicSurface.Rust;
                LodMesh(root, "Appeal_Frame_" + i, CivicAuthoredMeshFactory.StitchedFrame(1.65f, 3.0f, 0.18f, 9), CivicAuthoredMeshFactory.StitchedFrame(1.65f, 3.0f, 0.18f, 4), surface, new Vector3(x, 1.55f, -2.15f), Quaternion.identity, AuthoredDetailTier.Department);
            }
        }

        private void DressMolt(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Molt_AuthoredSurgicalFrames");
            for (var i = 0; i < 6; i++)
            {
                var angle = i * 60f;
                var radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 4.2f;
                LodMesh(root, "Molt_Frame_" + i, CivicAuthoredMeshFactory.StitchedFrame(1.8f, 2.8f, 0.16f, 12), CivicAuthoredMeshFactory.StitchedFrame(1.8f, 2.8f, 0.16f, 5), CivicSurface.Brass, radial + Vector3.up * 1.5f, Quaternion.Euler(0f, angle + 180f, i % 2 == 0 ? 5f : -5f), AuthoredDetailTier.Department);
            }
            MeshObject(root, "Molt_Seam_Ribbon", CivicAuthoredMeshFactory.Ribbon(8f, 0.45f, 0.65f, 32), CivicSurface.TealGlow, new Vector3(0f, 4.4f, 0f), Quaternion.Euler(0f, 90f, 0f), AuthoredDetailTier.Landmark, false);
        }

        private void DressBoard(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Board_AuthoredAuthorityShell");
            for (var i = 0; i < 3; i++)
            {
                LodMesh(root, "Preclassified_Panel_" + i, CivicAuthoredMeshFactory.FoldedPanel(3.2f, 4.2f, 0.32f, 8), CivicAuthoredMeshFactory.FoldedPanel(3.2f, 4.2f, 0.32f, 3), CivicSurface.Rust, new Vector3(-3.5f + i * 3.5f, 0.5f, -4.25f), Quaternion.identity, AuthoredDetailTier.Department);
            }
            MeshObject(root, "Premature_Classification_Seal", CivicAuthoredMeshFactory.ClassificationSeal(2.25f, 0.24f, 18), CivicSurface.Brass, new Vector3(0f, 3.2f, -4.65f), Quaternion.identity, AuthoredDetailTier.Landmark, false);
        }

        private void DressDecision(Transform department)
        {
            if (department == null) return;
            var root = DepartmentRoot(department, "Decision_AuthoredStandards");
            for (var i = 0; i < 5; i++)
            {
                var angle = Mathf.Lerp(-1.15f, 1.15f, i / 4f);
                var p = new Vector3(Mathf.Sin(angle) * 12f, 2.1f, Mathf.Cos(angle) * 7f - 0.55f);
                var surface = i == 0 ? CivicSurface.TealGlow : i == 1 ? CivicSurface.Brass : i == 2 ? CivicSurface.Glass : i == 3 ? CivicSurface.Moss : CivicSurface.Rust;
                MeshObject(root, "Ruling_Seal_" + i, CivicAuthoredMeshFactory.ClassificationSeal(0.9f, 0.16f, 12 + i * 2), surface, p, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, i * 7f), AuthoredDetailTier.Landmark, false);
            }
            MeshObject(root, "Future_Bet_Ribbon", CivicAuthoredMeshFactory.Ribbon(13f, 0.5f, 0.52f, 36), CivicSurface.Paper, new Vector3(0f, 6.1f, -0.7f), Quaternion.identity, AuthoredDetailTier.Landmark, false);
        }

        private Transform DepartmentRoot(Transform department, string name)
        {
            var node = new GameObject(name).transform;
            node.SetParent(department, false);
            node.gameObject.AddComponent<AuthoredEnvironmentDetail>().Tier = AuthoredDetailTier.Department;
            return node;
        }

        private GameObject LodMesh(Transform parent, string name, Mesh high, Mesh low, CivicSurface surface, Vector3 position, Quaternion rotation, AuthoredDetailTier tier)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = rotation;
            root.AddComponent<AuthoredEnvironmentDetail>().Tier = tier;

            var highObject = MeshChild(root.transform, "LOD0_" + name, high, surface);
            var lowObject = MeshChild(root.transform, "LOD1_" + name, low, surface);
            var lod = root.AddComponent<LODGroup>();
            lod.SetLODs(new[]
            {
                new LOD(0.48f, new[] { highObject.GetComponent<Renderer>() }),
                new LOD(0.16f, new[] { lowObject.GetComponent<Renderer>() }),
                new LOD(0.025f, new Renderer[0])
            });
            lod.RecalculateBounds();

            var bounds = high.bounds;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            return root;
        }

        private GameObject MeshObject(Transform parent, string name, Mesh mesh, CivicSurface surface, Vector3 position, Quaternion rotation, AuthoredDetailTier tier, bool collider)
        {
            var node = MeshChild(parent, name, mesh, surface);
            node.transform.localPosition = position;
            node.transform.localRotation = rotation;
            node.AddComponent<AuthoredEnvironmentDetail>().Tier = tier;
            if (collider)
            {
                var box = node.AddComponent<BoxCollider>();
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
            }
            return node;
        }

        private static GameObject MeshChild(Transform parent, string name, Mesh mesh, CivicSurface surface)
        {
            var node = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            node.transform.SetParent(parent, false);
            node.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CivicMaterialLibrary.Get(surface);
            renderer.shadowCastingMode = surface == CivicSurface.Glass || surface == CivicSurface.TealGlow ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = surface != CivicSurface.TealGlow;
            return node;
        }

        private Transform Find(string name)
        {
            foreach (var child in _world.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }

    public static class AuthoredEnvironmentAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var runner = new GameObject("AuthoredEnvironmentBootstrap");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<AuthoredEnvironmentBootstrapRunner>();
        }
    }

    [DefaultExecutionOrder(980)]
    public sealed class AuthoredEnvironmentBootstrapRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (var frame = 0; frame < 180; frame++)
            {
                var world = GameObject.Find("TheIdeaZooWorld");
                if (world != null)
                {
                    var pass = world.GetComponent<AuthoredEnvironmentPass>();
                    if (pass == null) pass = world.AddComponent<AuthoredEnvironmentPass>();
                    pass.Build(world.transform);
                    Destroy(gameObject);
                    yield break;
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
