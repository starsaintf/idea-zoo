using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdeaZoo.Runtime
{
    public enum StationKind
    {
        Desire, Commitment, Burden, Refusal, Board, Molt,
        Build, Rework, Hibernate, Sanctuary, Break
    }

    [DisallowMultipleComponent]
    public sealed class IdeaStation : MonoBehaviour
    {
        public StationKind Kind;
        public string DisplayName = string.Empty;
        public bool Available;
        public bool Completed;
        public Renderer Marker;

        private MaterialPropertyBlock _block;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public void Configure(StationKind kind, string displayName, bool available, Renderer marker)
        {
            Kind = kind;
            DisplayName = displayName;
            Available = available;
            Completed = false;
            Marker = marker;
            Refresh();
        }

        public void SetAvailable(bool value)
        {
            Available = value;
            gameObject.SetActive(value || Completed);
            Refresh();
        }

        public void Complete()
        {
            Completed = true;
            Available = false;
            gameObject.SetActive(true);
            Refresh();
        }

        public void ResetStation(bool available)
        {
            Completed = false;
            Available = available;
            gameObject.SetActive(available);
            Refresh();
        }

        private void Refresh()
        {
            if (Marker == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            Marker.GetPropertyBlock(_block);
            var color = Completed ? new Color(0.25f, 0.40f, 0.34f) : Available ? new Color(0.35f, 0.80f, 0.72f) : new Color(0.22f, 0.24f, 0.25f);
            _block.SetColor(BaseColor, color);
            _block.SetColor(ColorId, color);
            Marker.SetPropertyBlock(_block);
        }
    }

    [DisallowMultipleComponent]
    public sealed class WhisperGateWorld : MonoBehaviour
    {
        public readonly List<IdeaStation> Stations = new List<IdeaStation>();
        public Transform KeeperSpawn { get; private set; }
        public Transform HatchPoint { get; private set; }
        public Transform DecisionRoot { get; private set; }

        private readonly Dictionary<StationKind, IdeaStation> _byKind = new Dictionary<StationKind, IdeaStation>();
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        private Transform _ambientRoot;

        public void Build()
        {
            name = "Glassmarket_WhisperGate_District";
            BuildLighting();
            BuildTerrarium();
            BuildWhisperGate();
            BuildHatcheryRotunda();
            BuildCentralArchiveWalk();
            BuildDesireYard();
            BuildCommitmentPaddock();
            BuildBurrowerTunnel();
            BuildRefusalGate();
            BuildMoltHouse();
            BuildBoardWing();
            BuildDecisionGarden();
            BuildStaffAndAmbientLife();
        }

        public IdeaStation NearestAvailable(Vector3 position, float radius = 3.5f)
        {
            IdeaStation best = null;
            var distance = radius;
            foreach (var station in Stations)
            {
                if (!station.Available || station.Completed || !station.gameObject.activeInHierarchy) continue;
                var current = Vector3.Distance(position, station.transform.position);
                if (current < distance)
                {
                    distance = current;
                    best = station;
                }
            }
            return best;
        }

        public void Complete(StationKind kind)
        {
            IdeaStation station;
            if (_byKind.TryGetValue(kind, out station)) station.Complete();
        }

        public void SetAvailable(StationKind kind, bool available)
        {
            IdeaStation station;
            if (_byKind.TryGetValue(kind, out station)) station.SetAvailable(available);
        }

        public void ResetCase()
        {
            foreach (var pair in _byKind)
            {
                var initial = pair.Key == StationKind.Desire || pair.Key == StationKind.Commitment || pair.Key == StationKind.Burden || pair.Key == StationKind.Refusal;
                pair.Value.ResetStation(initial);
            }
            if (DecisionRoot != null) DecisionRoot.gameObject.SetActive(false);
        }

        public void RevealDecisionGarden()
        {
            if (DecisionRoot != null) DecisionRoot.gameObject.SetActive(true);
            SetAvailable(StationKind.Build, true);
            SetAvailable(StationKind.Rework, true);
            SetAvailable(StationKind.Hibernate, true);
            SetAvailable(StationKind.Sanctuary, true);
            SetAvailable(StationKind.Break, true);
        }

        private void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.42f, 0.43f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.20f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.055f, 0.065f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.045f, 0.10f, 0.12f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.005f;

            var sun = new GameObject("Civic_Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.84f, 0.64f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
            sun.transform.SetParent(transform, false);

            var moon = new GameObject("Archive_Fill").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.34f, 0.68f, 0.72f);
            moon.intensity = 0.35f;
            moon.transform.rotation = Quaternion.Euler(18f, 145f, 0f);
            moon.transform.SetParent(transform, false);
        }

        private void BuildTerrarium()
        {
            Primitive("Terrarium_Ground", PrimitiveType.Cylinder, Vector3.zero, new Vector3(42f, 0.35f, 42f), "ground", new Color(0.045f, 0.12f, 0.13f), true);
            Primitive("Inner_Paper_Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.20f, -7f), new Vector3(31f, 0.16f, 31f), "paper", new Color(0.50f, 0.44f, 0.34f), true);
            Primitive("Central_Ink_Garden", PrimitiveType.Cylinder, new Vector3(0f, 0.30f, -7f), new Vector3(25f, 0.18f, 25f), "ground2", new Color(0.055f, 0.16f, 0.17f), true);

            for (var i = 0; i < 16; i++)
            {
                var angle = i * Mathf.PI * 2f / 16f;
                var position = new Vector3(Mathf.Cos(angle) * 38f, 5.8f, -7f + Mathf.Sin(angle) * 38f);
                var rib = Primitive("Brass_Rib_" + i, PrimitiveType.Cube, position, new Vector3(0.26f, 12f, 0.26f), "brass", Brass(), false);
                rib.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 8f * Mathf.Sin(angle));
            }

            for (var ring = 0; ring < 3; ring++)
            {
                var radius = 11f + ring * 10f;
                for (var i = 0; i < 20; i++)
                {
                    var angle = i * Mathf.PI * 2f / 20f + ring * 0.14f;
                    var p = new Vector3(Mathf.Cos(angle) * radius, 0.55f, -7f + Mathf.Sin(angle) * radius);
                    Primitive("Path_Light_" + ring + "_" + i, PrimitiveType.Sphere, p, Vector3.one * 0.22f, "glow", new Color(0.30f, 0.85f, 0.78f), false);
                }
            }
        }

        private void BuildWhisperGate()
        {
            var root = NewRoot("01_WHISPER_GATE", new Vector3(0f, 0f, 25f));
            KeeperSpawn = Point(root, "KeeperSpawn", new Vector3(0f, 0.2f, 5.5f));
            Arch(root, "Whisper_Arch", Vector3.zero, new Vector3(8f, 6.5f, 1.0f), Brass());
            PrimitiveUnder(root, "Gate_Glass", PrimitiveType.Cube, new Vector3(0f, 2.6f, 0.4f), new Vector3(5.8f, 4.6f, 0.16f), "glass", new Color(0.20f, 0.72f, 0.70f, 0.42f), false);
            Sign(root, "THE WHISPER GATE", new Vector3(0f, 6.1f, -0.75f), 42, new Color(0.95f, 0.82f, 0.55f));

            for (var side = -1; side <= 1; side += 2)
            {
                for (var shelf = 0; shelf < 4; shelf++)
                {
                    PrimitiveUnder(root, "Intake_Drawer_" + side + "_" + shelf, PrimitiveType.Cube,
                        new Vector3(side * 5.1f, 0.8f + shelf * 1.0f, 0.6f), new Vector3(2.0f, 0.68f, 1.3f), "paper", Paper(), true);
                    PrimitiveUnder(root, "Drawer_Handle", PrimitiveType.Cube,
                        new Vector3(side * 5.1f, 0.8f + shelf * 1.0f, -0.08f), new Vector3(0.62f, 0.12f, 0.10f), "brass", Brass(), false);
                }
            }
            BuildPaperTrees(root, new Vector3(-8f, 0f, 1f), 4, 1);
            BuildPaperTrees(root, new Vector3(8f, 0f, 1f), 4, 2);
        }

        private void BuildHatcheryRotunda()
        {
            var root = NewRoot("02_HATCHERY_ROTUNDA", new Vector3(0f, 0f, 15f));
            HatchPoint = Point(root, "HatchPoint", new Vector3(0f, 0.65f, 0f));
            PrimitiveUnder(root, "Rotunda_Dais", PrimitiveType.Cylinder, Vector3.zero, new Vector3(7.5f, 0.5f, 7.5f), "brass", Brass(), true);
            PrimitiveUnder(root, "Incubation_Bowl", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), new Vector3(2.8f, 1.0f, 2.8f), "glass", new Color(0.26f, 0.82f, 0.76f, 0.50f), false);
            Sign(root, "HATCHERY · TWENTY-FOUR HOURS", new Vector3(0f, 4.5f, 0f), 34, Paper());

            for (var i = 0; i < 8; i++)
            {
                var a = i * Mathf.PI * 2f / 8f;
                var p = new Vector3(Mathf.Cos(a) * 5.3f, 1.1f, Mathf.Sin(a) * 5.3f);
                var pod = PrimitiveUnder(root, "Embryo_Pod_" + i, PrimitiveType.Capsule, p, new Vector3(0.65f, 1.35f, 0.65f), "glass", new Color(0.26f, 0.70f, 0.68f, 0.38f), false);
                pod.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 8f * Mathf.Sin(a));
                PrimitiveUnder(root, "Pod_Base_" + i, PrimitiveType.Cylinder, p + Vector3.down * 0.55f, new Vector3(0.9f, 0.18f, 0.9f), "brass", Brass(), true);
            }
        }

        private void BuildCentralArchiveWalk()
        {
            var root = NewRoot("03_CENTRAL_ARCHIVE_WALK", new Vector3(0f, 0f, 2f));
            for (var z = -5; z <= 5; z++)
            {
                PrimitiveUnder(root, "Record_Slab_" + z, PrimitiveType.Cube, new Vector3(0f, 0.18f, z * 1.55f), new Vector3(4.4f, 0.18f, 1.15f), "paper", new Color(0.26f, 0.38f, 0.36f), true);
                if (z % 2 == 0)
                {
                    PrimitiveUnder(root, "Archive_Post_L_" + z, PrimitiveType.Cube, new Vector3(-3f, 1.2f, z * 1.55f), new Vector3(0.25f, 2.4f, 0.25f), "brass", Brass(), true);
                    PrimitiveUnder(root, "Archive_Post_R_" + z, PrimitiveType.Cube, new Vector3(3f, 1.2f, z * 1.55f), new Vector3(0.25f, 2.4f, 0.25f), "brass", Brass(), true);
                }
            }
        }

        private void BuildDesireYard()
        {
            var root = NewRoot("04_DESIRE_YARD", new Vector3(-15f, 0f, 5f));
            BuildCourtyard(root, new Color(0.26f, 0.67f, 0.63f), "DESIRE YARD", "MARA ROOK · HATCHKEEPER");
            for (var i = 0; i < 9; i++)
            {
                var p = new Vector3(-4f + (i % 3) * 4f, 0.45f, -3.8f + (i / 3) * 3.8f);
                PrimitiveUnder(root, "Listening_Stone_" + i, PrimitiveType.Sphere, p, new Vector3(1.0f, 0.5f, 1.0f), "teal", new Color(0.18f, 0.48f, 0.46f), true);
            }
            AddStation(root, StationKind.Desire, "DESIRE YARD", true, new Vector3(0f, 0.4f, 0f), new Color(0.35f, 0.82f, 0.74f));
        }

        private void BuildCommitmentPaddock()
        {
            var root = NewRoot("05_COMMITMENT_PADDOCK", new Vector3(15f, 0f, 5f));
            BuildCourtyard(root, new Color(0.72f, 0.52f, 0.28f), "COMMITMENT PADDOCK", "TOMA REED · RELEASE SHEPHERD");
            for (var i = 0; i < 8; i++)
            {
                PrimitiveUnder(root, "Pledge_Post_" + i, PrimitiveType.Cube,
                    new Vector3(-5.2f + i * 1.5f, 1.4f, 2.8f), new Vector3(0.20f, 2.8f, 0.20f), "brass", Brass(), true);
            }
            for (var i = 0; i < 4; i++)
                PrimitiveUnder(root, "Contract_Table_" + i, PrimitiveType.Cube, new Vector3(-4.5f + i * 3f, 0.65f, -2.7f), new Vector3(2.1f, 0.18f, 1.2f), "paper", Paper(), true);
            AddStation(root, StationKind.Commitment, "COMMITMENT PADDOCK", true, new Vector3(0f, 0.4f, 0f), new Color(0.86f, 0.64f, 0.32f));
        }

        private void BuildBurrowerTunnel()
        {
            var root = NewRoot("06_BURROWER_TUNNEL", new Vector3(-15f, -0.5f, -10f));
            Arch(root, "Tunnel_Mouth", Vector3.zero, new Vector3(10f, 5f, 2f), new Color(0.42f, 0.54f, 0.36f));
            Sign(root, "BURROWER TUNNEL", new Vector3(0f, 4.8f, -1.2f), 34, Paper());
            Sign(root, "SEFU ANIK · APPETITE READER", new Vector3(0f, 3.8f, -1.2f), 22, new Color(0.70f, 0.82f, 0.58f));
            for (var z = 0; z < 8; z++)
            {
                PrimitiveUnder(root, "Pipe_L_" + z, PrimitiveType.Cylinder, new Vector3(-3.4f, 1f, -z * 1.6f), new Vector3(0.22f, 1.6f, 0.22f), "brass", Brass(), false);
                PrimitiveUnder(root, "Pipe_R_" + z, PrimitiveType.Cylinder, new Vector3(3.4f, 1f, -z * 1.6f), new Vector3(0.22f, 1.6f, 0.22f), "brass", Brass(), false);
                PrimitiveUnder(root, "Ledger_" + z, PrimitiveType.Cube, new Vector3((z % 2 == 0 ? -1.5f : 1.5f), 0.7f, -z * 1.6f), new Vector3(1.7f, 1.2f, 0.5f), "paper", Paper(), true);
            }
            AddStation(root, StationKind.Burden, "BURROWER TUNNEL", true, new Vector3(0f, 0.9f, -5.5f), new Color(0.52f, 0.66f, 0.42f));
        }

        private void BuildRefusalGate()
        {
            var root = NewRoot("07_REFUSAL_GATE", new Vector3(15f, 0f, -10f));
            BuildCourtyard(root, new Color(0.68f, 0.28f, 0.24f), "REFUSAL GATE", "NARA VOSS · MERCY BUTCHER");
            for (var i = 0; i < 5; i++)
            {
                var x = -5f + i * 2.5f;
                Arch(root, "Exit_Arch_" + i, new Vector3(x, 0f, -2f), new Vector3(1.8f, 3.2f, 0.5f), i == 2 ? new Color(0.32f, 0.75f, 0.68f) : new Color(0.50f, 0.22f, 0.20f));
            }
            AddStation(root, StationKind.Refusal, "REFUSAL GATE", true, new Vector3(0f, 0.4f, 1.7f), new Color(0.82f, 0.35f, 0.28f));
        }

        private void BuildMoltHouse()
        {
            var root = NewRoot("08_MOLT_HOUSE", new Vector3(-8f, 0f, -24f));
            PrimitiveUnder(root, "Molt_House", PrimitiveType.Cylinder, Vector3.zero, new Vector3(10f, 3.5f, 10f), "violet", new Color(0.34f, 0.26f, 0.48f), true);
            PrimitiveUnder(root, "Molt_Chamber", PrimitiveType.Sphere, new Vector3(0f, 3.3f, 0f), new Vector3(4.6f, 2.4f, 4.6f), "glass", new Color(0.48f, 0.38f, 0.70f, 0.42f), false);
            Sign(root, "THE MOLT HOUSE", new Vector3(0f, 6.5f, -5f), 36, Paper());
            Sign(root, "ELIAN THREAD · MOLT SURGEON", new Vector3(0f, 5.4f, -5f), 22, new Color(0.72f, 0.62f, 0.90f));
            for (var i = 0; i < 10; i++)
            {
                var angle = i * Mathf.PI * 2f / 10f;
                PrimitiveUnder(root, "Rule_Spool_" + i, PrimitiveType.Cylinder,
                    new Vector3(Mathf.Cos(angle) * 5.7f, 1.2f, Mathf.Sin(angle) * 5.7f), new Vector3(0.7f, 1.3f, 0.7f), "brass", Brass(), true);
            }
            AddStation(root, StationKind.Molt, "MOLT HOUSE", false, new Vector3(0f, 0.4f, -4.7f), new Color(0.55f, 0.43f, 0.72f));
        }

        private void BuildBoardWing()
        {
            var root = NewRoot("09_SEALED_BOARD_WING", new Vector3(8f, 0f, -24f));
            PrimitiveUnder(root, "Board_Block", PrimitiveType.Cube, new Vector3(0f, 3f, 0f), new Vector3(11f, 6f, 8f), "rust", new Color(0.31f, 0.12f, 0.12f), true);
            for (var i = 0; i < 9; i++)
                PrimitiveUnder(root, "Sealed_Window_" + i, PrimitiveType.Cube, new Vector3(-4f + (i % 3) * 4f, 2f + (i / 3) * 1.4f, -4.05f), new Vector3(1.6f, 0.7f, 0.12f), "brass", Brass(), false);
            Sign(root, "SEALED BOARD WING", new Vector3(0f, 6.7f, -4.2f), 34, new Color(0.95f, 0.58f, 0.45f));
            Sign(root, "SEN OSEI · COUNTERFACTUAL VETERINARIAN", new Vector3(0f, 5.6f, -4.2f), 18, Paper());
            AddStation(root, StationKind.Board, "SEALED BOARD WING", false, new Vector3(0f, 0.4f, -4.5f), new Color(0.78f, 0.25f, 0.22f));
        }

        private void BuildDecisionGarden()
        {
            DecisionRoot = NewRoot("10_DECISION_GARDEN", new Vector3(0f, 0f, -37f));
            Sign(DecisionRoot, "THE DECISION GARDEN", new Vector3(0f, 7f, 0f), 42, Paper());
            PrimitiveUnder(DecisionRoot, "Garden_Dais", PrimitiveType.Cylinder, Vector3.zero, new Vector3(18f, 0.45f, 18f), "ground2", new Color(0.06f, 0.18f, 0.18f), true);

            var gates = new[]
            {
                Tuple.Create(StationKind.Build, "BUILD", new Color(0.28f, 0.75f, 0.67f)),
                Tuple.Create(StationKind.Rework, "MOLT", new Color(0.52f, 0.42f, 0.72f)),
                Tuple.Create(StationKind.Hibernate, "HIBERNATE", new Color(0.35f, 0.51f, 0.72f)),
                Tuple.Create(StationKind.Sanctuary, "SANCTUARY", new Color(0.48f, 0.62f, 0.38f)),
                Tuple.Create(StationKind.Break, "BREAK", new Color(0.72f, 0.28f, 0.23f))
            };
            for (var i = 0; i < gates.Length; i++)
            {
                var angle = Mathf.Lerp(-1.15f, 1.15f, i / 4f);
                var p = new Vector3(Mathf.Sin(angle) * 12f, 0f, Mathf.Cos(angle) * 7f);
                Arch(DecisionRoot, gates[i].Item2 + "_Gate", p, new Vector3(3.1f, 4.7f, 0.8f), gates[i].Item3);
                Sign(DecisionRoot, gates[i].Item2, p + new Vector3(0f, 4.9f, -0.8f), 26, Paper());
                AddStation(DecisionRoot, gates[i].Item1, gates[i].Item2, false, p, gates[i].Item3);
            }
            DecisionRoot.gameObject.SetActive(false);
        }

        private void BuildStaffAndAmbientLife()
        {
            var staff = NewRoot("STAFF_AND_AMBIENT_LIFE", Vector3.zero);
            Staff(staff, "Mara_Rook", new Vector3(-15f, 0f, 9f), new Color(0.18f, 0.55f, 0.52f));
            Staff(staff, "Toma_Reed", new Vector3(15f, 0f, 9f), new Color(0.64f, 0.45f, 0.24f));
            Staff(staff, "Sefu_Anik", new Vector3(-15f, -0.2f, -5f), new Color(0.36f, 0.50f, 0.30f));
            Staff(staff, "Nara_Voss", new Vector3(15f, 0f, -5f), new Color(0.57f, 0.24f, 0.22f));
            Staff(staff, "Elian_Thread", new Vector3(-8f, 0f, -18f), new Color(0.43f, 0.34f, 0.60f));
            Staff(staff, "Sen_Osei", new Vector3(8f, 0f, -18f), new Color(0.45f, 0.20f, 0.20f));

            _ambientRoot = NewRoot("AMBIENT_ORGANISMS", Vector3.zero);
            for (var i = 0; i < 26; i++)
            {
                var angle = i * 2.399963f;
                var radius = 7f + (i % 7) * 3.2f;
                var p = new Vector3(Mathf.Cos(angle) * radius, 0.8f + (i % 3) * 0.25f, -7f + Mathf.Sin(angle) * radius);
                var fleck = PrimitiveUnder(_ambientRoot, "Fleck_" + i, PrimitiveType.Sphere, p, Vector3.one * (0.20f + (i % 3) * 0.06f), "glow", new Color(0.85f, 0.66f, 0.30f), false);
                var orbit = fleck.AddComponent<AmbientOrbit>();
                orbit.Center = p;
                orbit.Radius = 0.5f + (i % 4) * 0.22f;
                orbit.Speed = 0.25f + (i % 5) * 0.08f;
                orbit.Phase = angle;
            }
        }

        private void BuildCourtyard(Transform root, Color color, string title, string staff)
        {
            PrimitiveUnder(root, "Courtyard", PrimitiveType.Cylinder, Vector3.zero, new Vector3(12f, 0.28f, 12f), "court" + title, color * 0.42f, true);
            for (var i = 0; i < 10; i++)
            {
                var a = i * Mathf.PI * 2f / 10f;
                PrimitiveUnder(root, "Pillar_" + i, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 6.4f, 1.6f, Mathf.Sin(a) * 6.4f), new Vector3(0.32f, 3.2f, 0.32f), "brass", Brass(), true);
            }
            Sign(root, title, new Vector3(0f, 5.1f, -6.5f), 34, Paper());
            Sign(root, staff, new Vector3(0f, 4.1f, -6.5f), 20, color + new Color(0.18f, 0.18f, 0.18f));
        }

        private IdeaStation AddStation(Transform parent, StationKind kind, string title, bool available, Vector3 localPosition, Color color)
        {
            var root = NewRoot("Station_" + kind, Vector3.zero, parent);
            root.localPosition = localPosition;
            var marker = PrimitiveUnder(root, "Marker", PrimitiveType.Cylinder, Vector3.zero, new Vector3(2.8f, 0.25f, 2.8f), "station" + kind, color, true).GetComponent<Renderer>();
            var station = root.gameObject.AddComponent<IdeaStation>();
            station.Configure(kind, title, available, marker);
            Stations.Add(station);
            _byKind[kind] = station;
            root.gameObject.SetActive(available);
            return station;
        }

        private void Staff(Transform parent, string staffName, Vector3 position, Color coat)
        {
            var root = NewRoot(staffName, position, parent);
            PrimitiveUnder(root, "Body", PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.75f, 1.7f, 0.65f), "coat" + staffName, coat, true);
            PrimitiveUnder(root, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.25f, 0f), Vector3.one * 0.52f, "skin", new Color(0.43f, 0.30f, 0.22f), false);
            PrimitiveUnder(root, "Lens", PrimitiveType.Sphere, new Vector3(0.28f, 2.28f, -0.40f), Vector3.one * 0.14f, "glow", new Color(0.32f, 0.90f, 0.82f), false);
            Sign(root, staffName.Replace('_', ' ').ToUpperInvariant(), new Vector3(0f, 3.05f, 0f), 20, Paper());
        }

        private void BuildPaperTrees(Transform parent, Vector3 origin, int count, int seed)
        {
            for (var i = 0; i < count; i++)
            {
                var x = origin.x + Mathf.Sin(seed * 7.1f + i * 3.4f) * 2.2f;
                var z = origin.z + Mathf.Cos(seed * 4.7f + i * 2.2f) * 2.2f;
                PrimitiveUnder(parent, "Paper_Tree_Trunk_" + seed + "_" + i, PrimitiveType.Cylinder, new Vector3(x, 1.25f, z), new Vector3(0.18f, 1.25f, 0.18f), "brass", Brass(), true);
                for (var leaf = 0; leaf < 4; leaf++)
                {
                    var leafObject = PrimitiveUnder(parent, "Paper_Leaf", PrimitiveType.Cube,
                        new Vector3(x + Mathf.Sin(leaf * 1.57f) * 0.8f, 2.2f + leaf * 0.28f, z + Mathf.Cos(leaf * 1.57f) * 0.8f),
                        new Vector3(1.4f, 0.08f, 0.75f), "paperLeaf", new Color(0.36f, 0.56f, 0.45f), false);
                    leafObject.transform.rotation = Quaternion.Euler(0f, leaf * 47f, leaf % 2 == 0 ? 18f : -18f);
                }
            }
        }

        private void Arch(Transform parent, string objectName, Vector3 localPosition, Vector3 size, Color color)
        {
            var root = NewRoot(objectName, Vector3.zero, parent);
            root.localPosition = localPosition;
            PrimitiveUnder(root, "Left", PrimitiveType.Cube, new Vector3(-size.x * 0.42f, size.y * 0.5f, 0f), new Vector3(size.x * 0.16f, size.y, size.z), objectName + "Mat", color, true);
            PrimitiveUnder(root, "Right", PrimitiveType.Cube, new Vector3(size.x * 0.42f, size.y * 0.5f, 0f), new Vector3(size.x * 0.16f, size.y, size.z), objectName + "Mat", color, true);
            PrimitiveUnder(root, "Lintel", PrimitiveType.Cube, new Vector3(0f, size.y, 0f), new Vector3(size.x, size.y * 0.16f, size.z), objectName + "Mat", color, true);
            for (var i = 0; i < 5; i++)
            {
                var t = i / 4f;
                PrimitiveUnder(root, "Civic_Notch_" + i, PrimitiveType.Cube,
                    new Vector3(Mathf.Lerp(-size.x * 0.32f, size.x * 0.32f, t), size.y * 1.08f + Mathf.Sin(t * Mathf.PI) * 0.25f, -size.z * 0.58f),
                    new Vector3(size.x * 0.08f, size.y * 0.10f, size.z * 0.12f), "paper", Paper(), false);
            }
        }

        private Transform NewRoot(string objectName, Vector3 position, Transform parent = null)
        {
            var root = new GameObject(objectName).transform;
            root.SetParent(parent == null ? transform : parent, false);
            root.localPosition = position;
            return root;
        }

        private Transform Point(Transform parent, string objectName, Vector3 localPosition)
        {
            var point = NewRoot(objectName, Vector3.zero, parent);
            point.localPosition = localPosition;
            return point;
        }

        private GameObject Primitive(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, string materialKey, Color color, bool collider)
        {
            return PrimitiveUnder(transform, objectName, type, position, scale, materialKey, color, collider);
        }

        private GameObject PrimitiveUnder(Transform parent, string objectName, PrimitiveType type, Vector3 localPosition, Vector3 scale, string materialKey, Color color, bool collider)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = objectName;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = scale;
            var renderer = item.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = Material(materialKey, color);
            var itemCollider = item.GetComponent<Collider>();
            if (itemCollider != null) itemCollider.enabled = collider;
            return item;
        }

        private void Sign(Transform parent, string text, Vector3 localPosition, int fontSize, Color color)
        {
            var sign = NewRoot("Sign_" + text.Replace(' ', '_'), Vector3.zero, parent);
            sign.localPosition = localPosition;
            sign.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var mesh = sign.gameObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = fontSize;
            mesh.characterSize = 0.075f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
        }

        private Material Material(string key, Color color)
        {
            Material material;
            if (_materials.TryGetValue(key, out material)) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = "IdeaZoo_" + key, color = color };
            if (color.a < 0.99f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.renderQueue = 3000;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            _materials[key] = material;
            return material;
        }

        private static Color Brass() { return new Color(0.76f, 0.55f, 0.28f); }
        private static Color Paper() { return new Color(0.84f, 0.80f, 0.70f); }
    }

    public sealed class AmbientOrbit : MonoBehaviour
    {
        public Vector3 Center;
        public float Radius = 1f;
        public float Speed = 0.4f;
        public float Phase;

        private void Update()
        {
            var angle = Phase + Time.time * Speed;
            transform.localPosition = Center + new Vector3(Mathf.Cos(angle) * Radius, Mathf.Sin(angle * 1.7f) * 0.25f, Mathf.Sin(angle) * Radius);
        }
    }
}
