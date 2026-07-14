using System;
using System.Collections.Generic;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.HeroSlice
{
    [DisallowMultipleComponent]
    public sealed class HeroWorldProductionPass : MonoBehaviour
    {
        private readonly Dictionary<HeroDistrictId, Transform> _districts = new Dictionary<HeroDistrictId, Transform>();
        private WhisperGateWorld _world;
        private Transform _root;
        private bool _built;

        private static readonly Color Ink = new Color(0.025f, 0.035f, 0.065f);
        private static readonly Color Brass = new Color(0.55f, 0.34f, 0.10f);
        private static readonly Color BrassLight = new Color(0.95f, 0.68f, 0.27f);
        private static readonly Color GlassBlue = new Color(0.10f, 0.42f, 0.62f);
        private static readonly Color LanternGold = new Color(1f, 0.64f, 0.18f);
        private static readonly Color ArchivePaper = new Color(0.50f, 0.42f, 0.30f);
        private static readonly Color EvidenceBlue = new Color(0.18f, 0.52f, 1f);

        public bool Built { get { return _built; } }
        public Transform HeroRoot { get { return _root; } }

        public void Build(WhisperGateWorld world)
        {
            if (_built || world == null) return;
            _world = world;
            var existing = world.transform.Find("HERO_SLICE_WORLD");
            if (existing != null)
            {
                _root = existing;
                IndexDistricts();
                _built = true;
                return;
            }

            _root = HeroSliceUtility.NewRoot(world.transform, "HERO_SLICE_WORLD", Vector3.zero);
            BuildZooEntrance();
            BuildLanternFields();
            BuildSilentStacks();
            BuildEvidenceForge();
            BuildConnectorLanguage();
            IndexDistricts();
            _built = true;
        }

        public Transform District(HeroDistrictId district)
        {
            Transform value;
            return _districts.TryGetValue(district, out value) ? value : null;
        }

        public void FrameDistrict(HeroDistrictId district)
        {
            var target = District(district);
            var camera = FindAnyObjectByType<Camera>();
            if (target == null || camera == null) return;

            var offset = DistrictCameraOffset(district);
            camera.transform.position = target.position + offset;
            camera.transform.rotation = Quaternion.LookRotation((target.position + Vector3.up * 2.2f - camera.transform.position).normalized, Vector3.up);
        }

        private static Vector3 DistrictCameraOffset(HeroDistrictId district)
        {
            switch (district)
            {
                case HeroDistrictId.ZooEntrance: return new Vector3(0f, 7.5f, 18f);
                case HeroDistrictId.LanternFields: return new Vector3(9f, 5.2f, 10f);
                case HeroDistrictId.SilentStacks: return new Vector3(-10f, 5.5f, 9f);
                case HeroDistrictId.EvidenceForge: return new Vector3(12f, 8f, 13f);
                default: return new Vector3(0f, 6f, 12f);
            }
        }

        private void IndexDistricts()
        {
            _districts.Clear();
            foreach (HeroDistrictId value in Enum.GetValues(typeof(HeroDistrictId)))
            {
                var node = HeroSliceUtility.FindDeep(_root, "HERO_" + value.ToString().ToUpperInvariant());
                if (node != null) _districts[value] = node;
            }
        }

        private Transform Anchor(string existingName, HeroDistrictId id, Vector3 fallback)
        {
            var existing = HeroSliceUtility.FindDeep(_world.transform, existingName);
            var local = existing != null ? _root.InverseTransformPoint(existing.position) : fallback;
            return HeroSliceUtility.NewRoot(_root, "HERO_" + id.ToString().ToUpperInvariant(), local);
        }

        private void BuildZooEntrance()
        {
            var root = Anchor("01_WHISPER_GATE", HeroDistrictId.ZooEntrance, new Vector3(0f, 0f, 25f));
            var facade = HeroSliceUtility.NewRoot(root, "Cinematic_Entrance_Facade", new Vector3(0f, 0f, -2.2f));

            for (var side = -1; side <= 1; side += 2)
            {
                HeroSliceUtility.Primitive(facade, "GatePier_" + side, PrimitiveType.Cube,
                    new Vector3(side * 5.8f, 3.2f, 0f), new Vector3(1.15f, 6.4f, 1.2f), Brass, 0.78f, 0.70f);
                HeroSliceUtility.Primitive(facade, "GlassWing_" + side, PrimitiveType.Sphere,
                    new Vector3(side * 8.2f, 4.1f, 1.6f), new Vector3(4.2f, 3.7f, 4.2f), GlassBlue * 0.55f, 0.05f, 0.88f, false);
                BuildLampColumn(facade, new Vector3(side * 4.6f, 1.8f, -1.5f), "EntranceLamp_" + side);
            }

            HeroSliceUtility.Primitive(facade, "GreatLintel", PrimitiveType.Cube,
                new Vector3(0f, 7.0f, 0f), new Vector3(12.8f, 1.2f, 1.3f), Brass, 0.84f, 0.75f);
            HeroSliceUtility.Primitive(facade, "ClockworkMedallion", PrimitiveType.Cylinder,
                new Vector3(0f, 8.35f, -0.2f), new Vector3(1.6f, 0.20f, 1.6f), BrassLight, 0.9f, 0.84f)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            HeroSliceUtility.Label(facade, "IdeaZooTitle", "THE IDEA ZOO", new Vector3(0f, 7.3f, -1.0f), 68, new Color(1f, 0.78f, 0.38f), 0.09f);
            HeroSliceUtility.Label(facade, "IdeaZooMotto", "NURTURE  ·  QUESTION  ·  PROTECT", new Vector3(0f, 6.15f, -1.02f), 28, ArchivePaper, 0.065f);
            BuildSignpost(root, new Vector3(11.5f, 0f, -1f));
            BuildObservationBoard(root, new Vector3(-11.5f, 0f, -1f));
            BuildWetPlaza(root, 13f, 7);
            HeroSliceUtility.PracticalLight(root, "EntranceKey", new Vector3(0f, 6.2f, -3f), LanternGold, 4.2f, 18f);
            HeroSliceUtility.Motes(root, "EntranceRainMotes", new Color(0.52f, 0.72f, 1f), 10f, 90);
        }

        private void BuildLanternFields()
        {
            var root = Anchor("02_HATCHERY_ROTUNDA", HeroDistrictId.LanternFields, new Vector3(0f, 0f, 15f));
            var habitat = HeroSliceUtility.NewRoot(root, "Lantern_Field_Habitat", new Vector3(-9f, 0f, -2f));

            for (var i = 0; i < 7; i++)
            {
                var angle = i * Mathf.PI * 2f / 7f;
                var radius = 4.4f + (i % 2) * 1.2f;
                var position = new Vector3(Mathf.Cos(angle) * radius, 1.3f + (i % 3) * 0.45f, Mathf.Sin(angle) * radius);
                var pod = HeroSliceUtility.Primitive(habitat, "LuminousHabitatPod_" + i, PrimitiveType.Sphere,
                    position, new Vector3(1.2f, 1.8f, 1.2f), new Color(0.08f, 0.22f, 0.28f), 0.05f, 0.92f, false);
                var renderer = pod.GetComponent<Renderer>();
                HeroSliceUtility.SetEmission(renderer, i % 2 == 0 ? LanternGold : EvidenceBlue, 1.25f);
                HeroSliceUtility.Primitive(habitat, "PodBase_" + i, PrimitiveType.Cylinder,
                    position + Vector3.down * 1.45f, new Vector3(1.35f, 0.22f, 1.35f), Brass, 0.85f, 0.7f);
            }

            var board = HeroSliceUtility.Primitive(root, "LanternEvidenceBoard", PrimitiveType.Cube,
                new Vector3(-14f, 3.0f, -5.5f), new Vector3(4.4f, 3.0f, 0.22f), Ink, 0.25f, 0.48f);
            HeroSliceUtility.Label(board.transform, "LanternFieldsTitle", "LANTERN FIELDS", new Vector3(0f, 0.55f, -0.58f), 42, new Color(1f, 0.72f, 0.30f), 0.06f);
            HeroSliceUtility.Label(board.transform, "LanternFieldsStatus", "TRUST BEFORE TOUCH\nEVIDENCE GAUGE · TRANSITIONAL", new Vector3(0f, -0.25f, -0.58f), 22, ArchivePaper, 0.046f);

            BuildCareTable(root, new Vector3(-9f, 0f, 4.3f));
            HeroSliceUtility.PracticalLight(root, "LanternHabitatKey", new Vector3(-9f, 4.5f, -1f), LanternGold, 4.8f, 16f);
            HeroSliceUtility.PracticalLight(root, "LanternHabitatFill", new Vector3(-4f, 3.5f, 2f), EvidenceBlue, 2.2f, 13f);
            HeroSliceUtility.Motes(root, "LanternSpores", LanternGold, 7f, 110);
        }

        private void BuildSilentStacks()
        {
            var root = Anchor("03_CENTRAL_ARCHIVE_WALK", HeroDistrictId.SilentStacks, new Vector3(0f, 0f, 2f));
            var stacks = HeroSliceUtility.NewRoot(root, "Silent_Stacks_Archive", new Vector3(9.5f, 0f, -1.5f));

            for (var side = -1; side <= 1; side += 2)
            {
                for (var bay = 0; bay < 5; bay++)
                {
                    var x = side * 4.6f;
                    var z = -5.2f + bay * 2.55f;
                    HeroSliceUtility.Primitive(stacks, "ArchiveBay_" + side + "_" + bay, PrimitiveType.Cube,
                        new Vector3(x, 2.6f, z), new Vector3(1.1f, 5.2f, 2.1f), new Color(0.075f, 0.065f, 0.065f), 0.35f, 0.45f);
                    for (var drawer = 0; drawer < 4; drawer++)
                        HeroSliceUtility.Primitive(stacks, "SpecimenDrawer_" + side + "_" + bay + "_" + drawer, PrimitiveType.Cube,
                            new Vector3(x - side * 0.62f, 0.7f + drawer * 1.15f, z), new Vector3(0.22f, 0.82f, 1.75f), ArchivePaper * 0.58f, 0.25f, 0.4f);
                }
            }

            BuildArchiveDesk(stacks, new Vector3(0f, 0f, 1.4f));
            var titleBoard = HeroSliceUtility.Primitive(stacks, "SilentStacksBoard", PrimitiveType.Cube,
                new Vector3(0f, 5.6f, -5.8f), new Vector3(4.8f, 1.4f, 0.20f), Ink, 0.28f, 0.5f);
            HeroSliceUtility.Label(titleBoard.transform, "SilentStacksTitle", "SILENT STACKS", new Vector3(0f, 0.18f, -0.58f), 44, ArchivePaper, 0.07f);
            HeroSliceUtility.Label(titleBoard.transform, "SilentStacksSubtitle", "ARCHIVES & RECORDS", new Vector3(0f, -0.45f, -0.58f), 22, new Color(0.75f, 0.58f, 0.30f), 0.05f);
            HeroSliceUtility.PracticalLight(root, "ArchiveWarmKey", new Vector3(9.5f, 5.5f, -1f), LanternGold, 3.6f, 15f);
            HeroSliceUtility.Motes(root, "ArchiveDust", new Color(0.84f, 0.68f, 0.40f), 8f, 80);
        }

        private void BuildEvidenceForge()
        {
            var root = Anchor("08_MOLT_HOUSE", HeroDistrictId.EvidenceForge, new Vector3(-8f, 0f, -24f));
            var forge = HeroSliceUtility.NewRoot(root, "Evidence_Forge_Expansion", new Vector3(0f, 0f, -2f));

            for (var ring = 0; ring < 3; ring++)
            {
                var radius = 5.4f + ring * 2.7f;
                for (var i = 0; i < 12; i++)
                {
                    var a = i * Mathf.PI * 2f / 12f;
                    var p = new Vector3(Mathf.Cos(a) * radius, 1.2f + ring * 0.65f, Mathf.Sin(a) * radius);
                    HeroSliceUtility.Primitive(forge, "ForgeRail_" + ring + "_" + i, PrimitiveType.Cube,
                        p, new Vector3(0.18f, 2.2f, 0.18f), Brass, 0.84f, 0.72f);
                }
            }

            var dais = HeroSliceUtility.Primitive(forge, "JudgmentDais", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(5.4f, 0.55f, 5.4f), new Color(0.08f, 0.12f, 0.18f), 0.55f, 0.75f);
            HeroSliceUtility.Primitive(dais.transform, "MoltGlassCore", PrimitiveType.Sphere,
                new Vector3(0f, 2.7f, 0f), new Vector3(3.0f, 3.0f, 3.0f), new Color(0.08f, 0.22f, 0.40f), 0.02f, 0.96f, false);
            HeroSliceUtility.Primitive(forge, "TransformationHalo", PrimitiveType.Cylinder,
                new Vector3(0f, 5.2f, 0f), new Vector3(3.6f, 0.15f, 3.6f), EvidenceBlue, 0.75f, 0.82f, false);

            var sign = HeroSliceUtility.Primitive(forge, "ForgeTitleBoard", PrimitiveType.Cube,
                new Vector3(0f, 8.3f, -6.4f), new Vector3(6.4f, 1.6f, 0.28f), Ink, 0.35f, 0.55f);
            HeroSliceUtility.Label(sign.transform, "ForgeTitle", "EVIDENCE FORGE", new Vector3(0f, 0.32f, -0.62f), 48, new Color(0.95f, 0.65f, 0.28f), 0.065f);
            HeroSliceUtility.Label(sign.transform, "ForgeSubtitle", "TEST  ·  PROVE  ·  REFINE\nMOLT CHAMBER", new Vector3(0f, -0.46f, -0.62f), 24, EvidenceBlue, 0.045f);

            BuildJudgmentBoards(forge);
            HeroSliceUtility.PracticalLight(root, "ForgeGoldKey", new Vector3(-2.8f, 6.0f, -2f), LanternGold, 4.6f, 18f);
            HeroSliceUtility.PracticalLight(root, "ForgeBlueRim", new Vector3(3.0f, 5.0f, 1.5f), EvidenceBlue, 3.4f, 18f);
            HeroSliceUtility.Motes(root, "ForgeSparks", new Color(1f, 0.50f, 0.16f), 9f, 130);
        }

        private void BuildConnectorLanguage()
        {
            var connectors = new[]
            {
                Tuple.Create(new Vector3(0f, 0.1f, 21f), new Vector3(0f, 0.1f, 17f)),
                Tuple.Create(new Vector3(-2f, 0.1f, 12f), new Vector3(5f, 0.1f, 4f)),
                Tuple.Create(new Vector3(4f, 0.1f, -3f), new Vector3(-6f, 0.1f, -20f)),
            };

            var root = HeroSliceUtility.NewRoot(_root, "HERO_CONNECTOR_PATHS", Vector3.zero);
            for (var i = 0; i < connectors.Length; i++)
            {
                var a = connectors[i].Item1;
                var b = connectors[i].Item2;
                var distance = Vector3.Distance(a, b);
                var midpoint = (a + b) * 0.5f;
                var path = HeroSliceUtility.Primitive(root, "WetStoryPath_" + i, PrimitiveType.Cube,
                    midpoint, new Vector3(2.4f, 0.08f, distance), new Color(0.035f, 0.075f, 0.11f), 0.65f, 0.92f, false);
                path.transform.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
            }
        }

        private static void BuildWetPlaza(Transform root, float radius, int rings)
        {
            for (var ring = 0; ring < rings; ring++)
            {
                var count = 10 + ring * 4;
                var r = 2.4f + ring * (radius / rings);
                for (var i = 0; i < count; i++)
                {
                    var a = i * Mathf.PI * 2f / count;
                    HeroSliceUtility.Primitive(root, "WetPlazaStone_" + ring + "_" + i, PrimitiveType.Cube,
                        new Vector3(Mathf.Cos(a) * r, 0.06f, Mathf.Sin(a) * r),
                        new Vector3(1.5f, 0.10f, 0.85f), new Color(0.03f, 0.055f, 0.075f), 0.65f, 0.96f, false)
                        .transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                }
            }
        }

        private static void BuildLampColumn(Transform parent, Vector3 position, string name)
        {
            var root = HeroSliceUtility.NewRoot(parent, name, position);
            HeroSliceUtility.Primitive(root, "Post", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.16f, 1.8f, 0.16f), Brass, 0.85f, 0.72f);
            var bulb = HeroSliceUtility.Primitive(root, "LampGlass", PrimitiveType.Sphere, new Vector3(0f, 3.8f, 0f), Vector3.one * 0.46f, LanternGold * 0.4f, 0.05f, 0.95f, false);
            HeroSliceUtility.SetEmission(bulb.GetComponent<Renderer>(), LanternGold, 1.8f);
            HeroSliceUtility.PracticalLight(root, "LampLight", new Vector3(0f, 3.8f, 0f), LanternGold, 2.1f, 9f);
        }

        private static void BuildSignpost(Transform parent, Vector3 position)
        {
            var root = HeroSliceUtility.NewRoot(parent, "HabitatSignpost", position);
            HeroSliceUtility.Primitive(root, "Signpost", PrimitiveType.Cylinder, new Vector3(0f, 2.7f, 0f), new Vector3(0.16f, 2.7f, 0.16f), Brass, 0.85f, 0.72f);
            var labels = new[] { "LANTERN FIELDS", "MECHANISM WARD", "WILD IMAGINATION", "ECHO VAULTS", "SILENT STACKS" };
            for (var i = 0; i < labels.Length; i++)
            {
                var board = HeroSliceUtility.Primitive(root, "HabitatBoard_" + i, PrimitiveType.Cube,
                    new Vector3(0f, 4.6f - i * 0.72f, 0f), new Vector3(3.5f, 0.52f, 0.16f), Ink, 0.35f, 0.52f);
                HeroSliceUtility.Label(board.transform, "Label", labels[i] + "  →", new Vector3(0f, 0f, -0.58f), 22, ArchivePaper, 0.045f);
            }
        }

        private static void BuildObservationBoard(Transform parent, Vector3 position)
        {
            var board = HeroSliceUtility.Primitive(parent, "TodayObservationBoard", PrimitiveType.Cube,
                position + Vector3.up * 2.6f, new Vector3(4.0f, 2.7f, 0.18f), Ink, 0.32f, 0.5f);
            HeroSliceUtility.Label(board.transform, "ObservationTitle", "TODAY'S OBSERVATIONS", new Vector3(0f, 0.75f, -0.58f), 28, ArchivePaper, 0.05f);
            HeroSliceUtility.Label(board.transform, "ObservationText", "CURIOSITY · HIGH\nCLARITY · FLUCTUATING\nCONTROVERSY · ELEVATED", new Vector3(0f, -0.25f, -0.58f), 20, new Color(0.70f, 0.62f, 0.42f), 0.043f);
        }

        private static void BuildCareTable(Transform parent, Vector3 position)
        {
            var root = HeroSliceUtility.NewRoot(parent, "CreatureCareStation", position);
            HeroSliceUtility.Primitive(root, "CareTable", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), new Vector3(4.2f, 0.32f, 2.0f), ArchivePaper * 0.68f, 0.25f, 0.48f);
            for (var i = 0; i < 5; i++)
            {
                var jar = HeroSliceUtility.Primitive(root, "EvidenceJar_" + i, PrimitiveType.Sphere,
                    new Vector3(-1.5f + i * 0.75f, 1.55f, 0f), Vector3.one * (0.28f + i * 0.03f),
                    i % 2 == 0 ? EvidenceBlue * 0.55f : LanternGold * 0.55f, 0.02f, 0.95f, false);
                HeroSliceUtility.SetEmission(jar.GetComponent<Renderer>(), i % 2 == 0 ? EvidenceBlue : LanternGold, 0.8f);
            }
        }

        private static void BuildArchiveDesk(Transform parent, Vector3 position)
        {
            var root = HeroSliceUtility.NewRoot(parent, "KeeperArchiveDesk", position);
            HeroSliceUtility.Primitive(root, "Desk", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), new Vector3(4.2f, 0.35f, 2.4f), ArchivePaper * 0.52f, 0.28f, 0.48f);
            var record = HeroSliceUtility.Primitive(root, "SuspendedFieldRecord", PrimitiveType.Cube, new Vector3(0f, 3.1f, 0f),
                new Vector3(2.4f, 1.6f, 0.08f), new Color(0.45f, 0.27f, 0.08f), 0.05f, 0.92f, false);
            HeroSliceUtility.SetEmission(record.GetComponent<Renderer>(), LanternGold, 1.5f);
            HeroSliceUtility.Label(record.transform, "RecordTitle", "FIELD RECORD · S-7\nFRAGILE IDEA SPECIMEN", new Vector3(0f, 0f, -0.58f), 22, new Color(1f, 0.80f, 0.42f), 0.045f);
        }

        private static void BuildJudgmentBoards(Transform parent)
        {
            var phrases = new[]
            {
                Tuple.Create("JUDGMENT DAIS", "IDEAS ARE NOT BORN TRUE.\nTHEY EARN IT."),
                Tuple.Create("EVIDENCE PROTOCOLS", "OBSERVE · TEST · CHALLENGE\nDOCUMENT · REFINE · DECIDE"),
                Tuple.Create("MOLT CYCLE", "SHED · SOFTEN · SHIFT\nEMERGE · ADAPT")
            };
            for (var i = 0; i < phrases.Length; i++)
            {
                var angle = Mathf.Lerp(-0.9f, 0.9f, i / 2f);
                var p = new Vector3(Mathf.Sin(angle) * 9.8f, 4.5f, Mathf.Cos(angle) * -7.4f);
                var board = HeroSliceUtility.Primitive(parent, "JudgmentBoard_" + i, PrimitiveType.Cube, p,
                    new Vector3(3.4f, 2.0f, 0.20f), Ink, 0.35f, 0.52f);
                HeroSliceUtility.Label(board.transform, "Title", phrases[i].Item1, new Vector3(0f, 0.45f, -0.58f), 26, ArchivePaper, 0.05f);
                HeroSliceUtility.Label(board.transform, "Body", phrases[i].Item2, new Vector3(0f, -0.35f, -0.58f), 18, new Color(0.80f, 0.60f, 0.30f), 0.04f);
            }
        }
    }
}
