using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CivicWorldArtPass : MonoBehaviour
    {
        private readonly List<GameObject> _generated = new List<GameObject>();
        private Transform _world;

        public void Build(Transform world)
        {
            if (world == null || _world != null) return;
            _world = world;
            RethemeExistingWorld();
            DressWhisperGate(Find("01_WHISPER_GATE"));
            DressHatchery(Find("02_HATCHERY_ROTUNDA"));
            DressArchiveWalk(Find("03_CENTRAL_ARCHIVE_WALK"));
            DressDesireYard(Find("04_DESIRE_YARD"));
            DressCommitmentPaddock(Find("05_COMMITMENT_PADDOCK"));
            DressBurrowerTunnel(Find("06_BURROWER_TUNNEL"));
            DressRefusalGate(Find("07_REFUSAL_GATE"));
            DressMoltHouse(Find("08_MOLT_HOUSE"));
            DressBoardWing(Find("09_BOARD_WING"));
            DressDecisionGarden(Find("10_DECISION_GARDEN"));
            DressTerrariumPaths();
        }

        private Transform Find(string name)
        {
            var direct = _world.Find(name);
            if (direct != null) return direct;
            foreach (var child in _world.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private void RethemeExistingWorld()
        {
            foreach (var renderer in _world.GetComponentsInChildren<Renderer>(true))
            {
                var lower = renderer.name.ToLowerInvariant();
                var surface = CivicSurface.Ink;
                if (lower.Contains("brass") || lower.Contains("handle") || lower.Contains("post") || lower.Contains("dais")) surface = CivicSurface.Brass;
                else if (lower.Contains("paper") || lower.Contains("drawer") || lower.Contains("ledger") || lower.Contains("record") || lower.Contains("table")) surface = CivicSurface.Paper;
                else if (lower.Contains("glass") || lower.Contains("pod") || lower.Contains("bowl")) surface = CivicSurface.Glass;
                else if (lower.Contains("moss") || lower.Contains("tree") || lower.Contains("garden")) surface = CivicSurface.Moss;
                else if (lower.Contains("light") || lower.Contains("glow") || lower.Contains("marker")) surface = CivicSurface.TealGlow;
                else if (lower.Contains("rust") || lower.Contains("seal")) surface = CivicSurface.Rust;
                renderer.sharedMaterial = CivicMaterialLibrary.Get(surface);
                renderer.shadowCastingMode = lower.Contains("light") || lower.Contains("glass") ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        private void DressWhisperGate(Transform root)
        {
            if (root == null) return;
            Track(CivicKit.LayeredFacade(root, "Layered_Intake_Wall_L", new Vector3(-7.2f, 2.8f, 1.7f), new Vector2(4.8f, 5.2f), 6, CivicSurface.Paper));
            Track(CivicKit.LayeredFacade(root, "Layered_Intake_Wall_R", new Vector3(7.2f, 2.8f, 1.7f), new Vector2(4.8f, 5.2f), 6, CivicSurface.Paper));
            CivicKit.Rail(root, "Queue_Rail_L", new Vector3(-5.6f, 0f, 4.0f), new Vector3(-1.5f, 0f, 4.0f), 5);
            CivicKit.Rail(root, "Queue_Rail_R", new Vector3(1.5f, 0f, 4.0f), new Vector3(5.6f, 0f, 4.0f), 5);
            CivicKit.Banner(root, "Gate_Banner_L", new Vector3(-4.0f, 4.7f, -0.1f), new Vector2(2.4f, 4.2f), CivicSurface.Rust, 0.35f).localRotation = Quaternion.Euler(0f, 8f, 0f);
            CivicKit.Banner(root, "Gate_Banner_R", new Vector3(4.0f, 4.7f, -0.1f), new Vector2(2.4f, 4.2f), CivicSurface.TealGlow, 0.35f).localRotation = Quaternion.Euler(0f, -8f, 0f);
            AddWords(root, "PROMISES ENTER · APPETITES LEAVE", new Vector3(0f, 1.3f, -0.55f), 0.23f, CivicSurface.Brass);
            for (var i = 0; i < 7; i++)
            {
                var card = CivicKit.Box(root, "Pending_Idea_Card_" + i, new Vector3(-5.8f + i * 1.95f, 0.45f + (i % 2) * 0.12f, 2.9f), new Vector3(1.15f, 0.05f, 0.72f), CivicSurface.Paper);
                card.transform.localRotation = Quaternion.Euler(0f, -12f + i * 4f, (i % 2 == 0 ? 1f : -1f) * 5f);
            }
        }

        private void DressHatchery(Transform root)
        {
            if (root == null) return;
            for (var i = 0; i < 12; i++)
            {
                var angle = i * Mathf.PI * 2f / 12f;
                var p = new Vector3(Mathf.Cos(angle) * 6.5f, 2.6f + Mathf.Sin(angle * 3f) * 0.35f, Mathf.Sin(angle) * 6.5f);
                var instrument = CivicKit.Cylinder(root, "Suspended_Instrument_" + i, p, new Vector3(0.12f, 0.85f, 0.12f), i % 3 == 0 ? CivicSurface.TealGlow : CivicSurface.Brass);
                instrument.transform.localRotation = Quaternion.Euler(Mathf.Sin(angle) * 16f, -angle * Mathf.Rad2Deg, Mathf.Cos(angle) * 12f);
                var motion = instrument.AddComponent<CivicAmbientMotion>();
                motion.Axis = Vector3.forward;
                motion.Degrees = 4f;
                motion.Speed = 0.35f + i * 0.025f;
                motion.Phase = i * 0.7f;
            }
            CivicKit.PipeRun(root, "Warm_Incubation_Line", new[]
            {
                new Vector3(-7f, 0.45f, -3.2f), new Vector3(-7f, 2.8f, -3.2f),
                new Vector3(0f, 4.1f, -3.2f), new Vector3(7f, 2.8f, -3.2f), new Vector3(7f, 0.45f, -3.2f)
            }, 0.10f, CivicSurface.Brass);
            AddWords(root, "DO NOT NAME IT BEFORE IT MOVES", new Vector3(0f, 3.2f, 5.4f), 0.18f, CivicSurface.Paper);
            for (var i = 0; i < 4; i++) CivicKit.Vitrine(root, "Unfinished_Specimen_" + i, new Vector3(-5.2f + i * 3.5f, 0f, 5.5f), new Vector3(1.1f, 1.8f, 1.1f));
        }

        private void DressArchiveWalk(Transform root)
        {
            if (root == null) return;
            CivicKit.PipeRun(root, "Archive_Memory_Line_L", new[] { new Vector3(-3.4f, 0.2f, -8f), new Vector3(-3.4f, 2.4f, -8f), new Vector3(-3.4f, 2.4f, 8f) }, 0.08f, CivicSurface.Brass);
            CivicKit.PipeRun(root, "Archive_Memory_Line_R", new[] { new Vector3(3.4f, 0.2f, -8f), new Vector3(3.4f, 2.4f, -8f), new Vector3(3.4f, 2.4f, 8f) }, 0.08f, CivicSurface.Brass);
            for (var i = 0; i < 12; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var slip = CivicKit.Box(root, "Case_Slip_" + i, new Vector3(side * 2.55f, 1.1f + (i % 3) * 0.42f, -7f + i * 1.25f), new Vector3(1.0f, 0.28f, 0.05f), i % 4 == 0 ? CivicSurface.Rust : CivicSurface.Paper);
                slip.transform.localRotation = Quaternion.Euler(0f, side * 90f, side * (i % 3 - 1) * 4f);
            }
            AddWords(root, "THE RECORD IS PART OF THE CREATURE", new Vector3(0f, 2.8f, 0f), 0.16f, CivicSurface.TealGlow);
        }

        private void DressDesireYard(Transform root)
        {
            if (root == null) return;
            CivicKit.Workbench(root, "Mara_Listening_Desk", new Vector3(-5.2f, 0f, -3.5f), new Vector3(2.8f, 1f, 1.4f), CivicSurface.Paper);
            for (var i = 0; i < 8; i++)
            {
                var ribbon = CivicKit.Banner(root, "Unsaid_Need_" + i, new Vector3(-5.2f + (i % 4) * 3.4f, 2.0f + (i / 4) * 1.2f, 4.2f), new Vector2(1.7f, 0.65f), i % 3 == 0 ? CivicSurface.Rust : CivicSurface.Paper, 0.16f);
                ribbon.gameObject.AddComponent<CivicAmbientMotion>().Phase = i * 0.8f;
            }
            AddWords(root, "ASK BEFORE EXPLAINING", new Vector3(0f, 2.8f, -5.7f), 0.20f, CivicSurface.TealGlow);
        }

        private void DressCommitmentPaddock(Transform root)
        {
            if (root == null) return;
            CivicKit.Workbench(root, "Toma_Pledge_Desk", new Vector3(5.0f, 0f, -3.8f), new Vector3(3.0f, 1f, 1.4f), CivicSurface.Brass);
            for (var i = 0; i < 10; i++)
            {
                var token = CivicKit.Cylinder(root, "Commitment_Token_" + i, new Vector3(-5.2f + (i % 5) * 2.5f, 0.25f, -0.8f + (i / 5) * 1.8f), new Vector3(0.42f, 0.10f, 0.42f), i < 3 ? CivicSurface.TealGlow : CivicSurface.Clay);
                token.transform.localRotation = Quaternion.Euler(90f, i * 21f, 0f);
            }
            CivicKit.Rail(root, "Pledge_Boundary", new Vector3(-6f, 0f, 4.8f), new Vector3(6f, 0f, 4.8f), 10);
            AddWords(root, "INTEREST IS NOT A COMMITMENT", new Vector3(0f, 2.7f, -5.7f), 0.19f, CivicSurface.Brass);
        }

        private void DressBurrowerTunnel(Transform root)
        {
            if (root == null) return;
            CivicKit.PipeRun(root, "Burden_Main", new[] { new Vector3(-4.5f, 0.3f, 1f), new Vector3(-4.5f, 3.4f, 1f), new Vector3(-4.5f, 3.4f, -12f) }, 0.16f, CivicSurface.Brass);
            CivicKit.PipeRun(root, "Burden_Overflow", new[] { new Vector3(4.5f, 0.3f, 1f), new Vector3(4.5f, 2.6f, 1f), new Vector3(2.5f, 2.6f, -6f), new Vector3(4.5f, 1.3f, -12f) }, 0.12f, CivicSurface.Rust);
            for (var i = 0; i < 14; i++)
            {
                CivicKit.Box(root, "Maintenance_Tag_" + i, new Vector3((i % 2 == 0 ? -2.7f : 2.7f), 0.5f + (i % 4) * 0.34f, -0.4f - i * 0.78f), new Vector3(0.8f, 0.22f, 0.06f), CivicSurface.Paper);
            }
            AddWords(root, "WHO CLEANS UP WHEN IT WORKS?", new Vector3(0f, 3.3f, -7.5f), 0.17f, CivicSurface.TealGlow);
        }

        private void DressRefusalGate(Transform root)
        {
            if (root == null) return;
            for (var i = 0; i < 5; i++)
            {
                var door = CivicKit.LayeredFacade(root, "Refusal_Door_" + i, new Vector3(-5.6f + i * 2.8f, 2.0f, 2.2f), new Vector2(1.8f, 3.6f), 4, i == 2 ? CivicSurface.TealGlow : CivicSurface.Paper);
                door.localRotation = Quaternion.Euler(0f, (i - 2) * 5f, 0f);
            }
            CivicKit.Rail(root, "Appeal_Line", new Vector3(-6f, 0f, -4.5f), new Vector3(6f, 0f, -4.5f), 9);
            AddWords(root, "A DOOR THAT PUNISHES EXIT IS A WALL", new Vector3(0f, 3.2f, -5.4f), 0.17f, CivicSurface.Rust);
        }

        private void DressMoltHouse(Transform root)
        {
            if (root == null) return;
            for (var i = 0; i < 9; i++)
            {
                var spool = CivicKit.Cylinder(root, "Revision_Spool_" + i, new Vector3(-5f + (i % 3) * 5f, 0.55f, -4f + (i / 3) * 3.8f), new Vector3(0.62f, 0.18f, 0.62f), i % 2 == 0 ? CivicSurface.Brass : CivicSurface.TealGlow);
                spool.transform.localRotation = Quaternion.Euler(90f, i * 17f, 0f);
            }
            CivicKit.PipeRun(root, "Molt_Seam", new[] { new Vector3(-6f, 0.3f, 5f), new Vector3(-6f, 4.2f, 5f), new Vector3(6f, 4.2f, 5f), new Vector3(6f, 0.3f, 5f) }, 0.10f, CivicSurface.Brass);
            AddWords(root, "PRESERVE THE CORE · CHANGE THE SHAPE", new Vector3(0f, 3.6f, -5.4f), 0.18f, CivicSurface.TealGlow);
        }

        private void DressBoardWing(Transform root)
        {
            if (root == null) return;
            for (var i = 0; i < 6; i++)
            {
                var seal = CivicKit.Box(root, "Premature_Seal_" + i, new Vector3(-4.8f + (i % 3) * 4.8f, 1.1f + (i / 3) * 2f, -0.3f), new Vector3(2.4f, 0.55f, 0.12f), CivicSurface.Rust);
                seal.transform.localRotation = Quaternion.Euler(0f, 0f, -9f + i * 3f);
            }
            CivicKit.Rail(root, "Board_Barrier", new Vector3(-6f, 0f, 4.2f), new Vector3(6f, 0f, 4.2f), 8);
            AddWords(root, "CLASSIFIED BEFORE HATCHING", new Vector3(0f, 3.7f, -2.4f), 0.23f, CivicSurface.Rust);
        }

        private void DressDecisionGarden(Transform root)
        {
            if (root == null) return;
            var surfaces = new[] { CivicSurface.TealGlow, CivicSurface.Brass, CivicSurface.Paper, CivicSurface.Moss, CivicSurface.Rust };
            for (var i = 0; i < 5; i++)
            {
                var angle = Mathf.Lerp(-55f, 55f, i / 4f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(angle) * 11f, 0f, Mathf.Cos(angle) * 4f - 2f);
                CivicKit.Banner(root, "Ruling_Standard_" + i, p + Vector3.up * 3.2f, new Vector2(1.5f, 4.5f), surfaces[i], 0.28f).localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                CivicKit.Rail(root, "Ruling_Rail_" + i, p + new Vector3(-0.85f, 0f, 1f), p + new Vector3(0.85f, 0f, 1f), 2);
            }
            AddWords(root, "A RULING IS A BET ABOUT THE FUTURE", new Vector3(0f, 4.5f, 4.2f), 0.18f, CivicSurface.Paper);
        }

        private void DressTerrariumPaths()
        {
            var root = new GameObject("Authored_Path_Detail").transform;
            root.SetParent(_world, false);
            for (var ring = 0; ring < 3; ring++)
            {
                var radius = 12f + ring * 9.5f;
                for (var i = 0; i < 24; i++)
                {
                    var angle = i * Mathf.PI * 2f / 24f + ring * 0.21f;
                    var p = new Vector3(Mathf.Cos(angle) * radius, 0.48f, -7f + Mathf.Sin(angle) * radius);
                    var marker = CivicKit.Box(root, "Civic_Marker_" + ring + "_" + i, p, new Vector3(0.34f, 0.05f, 0.72f), i % 7 == 0 ? CivicSurface.Rust : CivicSurface.Paper);
                    marker.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                }
            }
        }

        private void AddWords(Transform parent, string text, Vector3 position, float size, CivicSurface surface)
        {
            var node = new GameObject("Public_Language_" + text.Replace(' ', '_'));
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            var mesh = node.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 48;
            mesh.color = CivicMaterialLibrary.SurfaceColor(surface);
            node.AddComponent<CivicAmbientMotion>().Bob = 0.015f;
        }

        private void Track(Transform transform)
        {
            if (transform != null) _generated.Add(transform.gameObject);
        }
    }
}
