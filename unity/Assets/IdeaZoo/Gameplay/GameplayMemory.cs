using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdeaZoo.Core;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Gameplay
{
    [Serializable]
    public sealed class GameplayTendencyRecord
    {
        public GameplayTendency Tendency;
        public int Count;
    }

    [Serializable]
    public sealed class GameplayTestMemory
    {
        public string TestId = string.Empty;
        public int Strength;
        public GameplayTendency Tendency;
        public List<string> Choices = new List<string>();
        public string Summary = string.Empty;
    }

    [Serializable]
    public sealed class GameplayDisruptionMemory
    {
        public GameplayDisruptionKind Kind;
        public string ChoiceId = string.Empty;
        public string Consequence = string.Empty;
    }

    [Serializable]
    public sealed class GameplayCaseMemory
    {
        public string RecordId = string.Empty;
        public string Title = string.Empty;
        public string CreatureName = string.Empty;
        public IdeaClass Class;
        public bool HasRuling;
        public Ruling Ruling;
        public int StartedAtUnix;
        public int CompletedAtUnix;
        public int TimeRemaining;
        public int TrustRemaining;
        public int MomentumRemaining;
        public int EvidenceCollected;
        public List<GameplayTestMemory> Tests = new List<GameplayTestMemory>();
        public List<GameplayDisruptionMemory> Disruptions = new List<GameplayDisruptionMemory>();
        public List<string> Scars = new List<string>();
        public string KeeperReflection = string.Empty;
    }

    [Serializable]
    public sealed class GameplayMemoryState
    {
        public int Version = 1;
        public int TotalTests;
        public int TotalDisruptions;
        public int CompletedCases;
        public List<GameplayTendencyRecord> Tendencies = new List<GameplayTendencyRecord>();
        public List<GameplayCaseMemory> Cases = new List<GameplayCaseMemory>();
        public long UpdatedAtUnix;

        public GameplayTendency DominantTendency()
        {
            if (Tendencies == null || Tendencies.Count == 0) return GameplayTendency.Skeptic;
            return Tendencies.OrderByDescending(item => item.Count).ThenBy(item => item.Tendency).First().Tendency;
        }

        public void AddTendency(GameplayTendency tendency)
        {
            if (Tendencies == null) Tendencies = new List<GameplayTendencyRecord>();
            var record = Tendencies.FirstOrDefault(item => item.Tendency == tendency);
            if (record == null)
            {
                record = new GameplayTendencyRecord { Tendency = tendency };
                Tendencies.Add(record);
            }
            record.Count++;
        }

        public void Trim()
        {
            if (Cases == null) Cases = new List<GameplayCaseMemory>();
            if (Cases.Count <= GameplayPerformanceGovernor.MaximumSavedCases) return;
            Cases.RemoveRange(0, Cases.Count - GameplayPerformanceGovernor.MaximumSavedCases);
        }
    }

    public sealed class GameplayMemoryService
    {
        private readonly string _path;
        private readonly string _backupPath;
        private GameplayMemoryState _state;
        private GameplayCaseMemory _active;

        public GameplayMemoryState State { get { return _state; } }

        public GameplayMemoryService()
        {
            _path = Path.Combine(Application.persistentDataPath, "idea-zoo-gameplay-memory.json");
            _backupPath = _path + ".backup";
            _state = Load();
        }

        public void BeginCase(IdeaProfile profile, GameplayResourceState resources)
        {
            if (profile == null) return;
            _active = _state.Cases.FirstOrDefault(item => item.RecordId == profile.RecordId);
            if (_active == null)
            {
                _active = new GameplayCaseMemory
                {
                    RecordId = profile.RecordId,
                    Title = profile.Title,
                    CreatureName = profile.CreatureName,
                    Class = profile.Class,
                    StartedAtUnix = Now(),
                    KeeperReflection = Reflection(_state.DominantTendency())
                };
                _state.Cases.Add(_active);
                _state.Trim();
            }
            UpdateResources(resources);
            Save();
        }

        public void RecordEncounter(GameplayEncounterRun run, GameplayResourceState resources)
        {
            if (_active == null || run == null) return;
            var memory = new GameplayTestMemory
            {
                TestId = run.Definition.TestId,
                Strength = run.Strength(),
                Tendency = run.DominantTendency(),
                Choices = new List<string>(run.ChoiceIds),
                Summary = run.EvidenceNote()
            };
            _active.Tests.RemoveAll(item => item.TestId == memory.TestId);
            _active.Tests.Add(memory);
            _state.TotalTests++;
            _state.AddTendency(memory.Tendency);
            UpdateResources(resources);
            Save();
        }

        public void RecordDisruption(GameplayDisruptionDefinition disruption, GameplayChoice choice, GameplayResourceState resources)
        {
            if (_active == null || disruption == null || choice == null) return;
            _active.Disruptions.Add(new GameplayDisruptionMemory
            {
                Kind = disruption.Kind,
                ChoiceId = choice.Id,
                Consequence = choice.Consequence
            });
            _state.TotalDisruptions++;
            _state.AddTendency(choice.Impact.Tendency);
            UpdateResources(resources);
            Save();
        }

        public void CompleteCase(IdeaProfile profile, GameplayResourceState resources)
        {
            if (_active == null || profile == null) return;
            _active.HasRuling = profile.FinalRuling.HasValue;
            _active.Ruling = profile.FinalRuling ?? Core.Ruling.Hibernate;
            _active.CompletedAtUnix = Now();
            UpdateResources(resources);
            BuildScars(_active, profile);
            _active.KeeperReflection = Reflection(_state.DominantTendency()) + " This time you chose " + _active.Ruling + ".";
            _state.CompletedCases++;
            Save();
            _active = null;
        }

        public string OpeningReflection()
        {
            return _state.CompletedCases == 0 ? "The Keeper has no history with you yet." : Reflection(_state.DominantTendency());
        }

        public string Summary()
        {
            return _state.CompletedCases + " ruled ideas · " + _state.TotalTests + " tests · tendency: " + _state.DominantTendency();
        }

        private GameplayMemoryState Load()
        {
            try
            {
                if (!File.Exists(_path)) return NewState();
                var loaded = JsonUtility.FromJson<GameplayMemoryState>(File.ReadAllText(_path));
                if (loaded == null) return NewState();
                loaded.Trim();
                return loaded;
            }
            catch
            {
                try
                {
                    if (File.Exists(_backupPath))
                    {
                        var backup = JsonUtility.FromJson<GameplayMemoryState>(File.ReadAllText(_backupPath));
                        if (backup != null) return backup;
                    }
                }
                catch { }
                return NewState();
            }
        }

        private void Save()
        {
            _state.Trim();
            _state.UpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(_state, true));
            if (File.Exists(_path)) File.Copy(_path, _backupPath, true);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temporary, _path);
        }

        private void UpdateResources(GameplayResourceState resources)
        {
            if (_active == null || resources == null) return;
            _active.TimeRemaining = resources.Time;
            _active.TrustRemaining = resources.Trust;
            _active.MomentumRemaining = resources.Momentum;
            _active.EvidenceCollected = resources.Evidence;
        }

        private static void BuildScars(GameplayCaseMemory memory, IdeaProfile profile)
        {
            memory.Scars.Clear();
            if (memory.TimeRemaining <= 2) memory.Scars.Add("Rushed under a nearly exhausted clock");
            if (memory.TrustRemaining <= 2) memory.Scars.Add("Lost trust during testing");
            if (memory.MomentumRemaining <= 1) memory.Scars.Add("Survived a collapse in momentum");
            if (profile.Metrics.Safety < 0.40) memory.Scars.Add("Left the Zoo carrying unresolved risk");
            if (profile.FinalRuling == Core.Ruling.Break) memory.Scars.Add("The body was broken; the lesson remains in the archive");
            if (profile.FinalRuling == Core.Ruling.Molt) memory.Scars.Add("The first form was surrendered to preserve the useful core");
            if (memory.Scars.Count == 0) memory.Scars.Add("Changed by evidence without losing its centre");
        }

        private static string Reflection(GameplayTendency tendency)
        {
            if (tendency == GameplayTendency.Experimenter) return "The Keeper remembers that you prefer evidence to certainty.";
            if (tendency == GameplayTendency.Protector) return "The Keeper remembers that you protect living ideas, sometimes longer than the evidence does.";
            if (tendency == GameplayTendency.Builder) return "The Keeper remembers that you move quickly and accept unfinished risk.";
            if (tendency == GameplayTendency.Simplifier) return "The Keeper remembers that you cut away form to preserve the useful core.";
            return "The Keeper remembers that you challenge claims before granting them a body.";
        }

        private static GameplayMemoryState NewState()
        {
            var state = new GameplayMemoryState();
            foreach (GameplayTendency tendency in Enum.GetValues(typeof(GameplayTendency)))
                state.Tendencies.Add(new GameplayTendencyRecord { Tendency = tendency });
            return state;
        }

        private static int Now()
        {
            var value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (int)Math.Min(int.MaxValue, Math.Max(0L, value));
        }
    }

    [DisallowMultipleComponent]
    public sealed class GameplayMemoryWorldPass : MonoBehaviour
    {
        private readonly List<GameObject> _cards = new List<GameObject>(GameplayPerformanceGovernor.MaximumVisibleMemoryCards);
        private readonly List<TextMesh> _labels = new List<TextMesh>(GameplayPerformanceGovernor.MaximumVisibleMemoryCards);
        private Transform _root;
        private Transform _archive;

        public void Build(WhisperGateWorld world)
        {
            if (world == null || _root != null) return;
            _root = new GameObject("GAMEPLAY_MEMORY_ARCHIVE").transform;
            _root.SetParent(world.transform, false);
            _archive = Find(world.transform, "03_CENTRAL_ARCHIVE_WALK") ?? world.transform;
            for (var i = 0; i < GameplayPerformanceGovernor.MaximumVisibleMemoryCards; i++) CreateCard(i);
        }

        public void Refresh(GameplayMemoryState state)
        {
            if (_root == null || state == null) return;
            var count = Mathf.Min(GameplayPerformanceGovernor.MaximumVisibleMemoryCards, state.Cases.Count);
            var start = Mathf.Max(0, state.Cases.Count - count);
            for (var i = 0; i < _cards.Count; i++)
            {
                var active = i < count;
                _cards[i].SetActive(active);
                if (!active) continue;
                var memory = state.Cases[start + i];
                var card = _cards[i].transform;
                card.position = _archive.TransformPoint(new Vector3(i % 2 == 0 ? -3.6f : 3.6f, 1.0f + (i % 4) * 0.58f, -7.2f + (i / 2) * 1.15f));
                var ruling = memory.HasRuling ? memory.Ruling.ToString().ToUpperInvariant() : "ACTIVE";
                _labels[i].text = memory.CreatureName + "\n" + ruling + " · " + memory.Tests.Count + " TESTS";
                _cards[i].GetComponent<Renderer>().sharedMaterial = Presentation.CivicMaterialLibrary.Get(
                    memory.HasRuling && memory.Ruling == Core.Ruling.Build ? Presentation.CivicSurface.TealGlow :
                    memory.HasRuling && memory.Ruling == Core.Ruling.Break ? Presentation.CivicSurface.Rust :
                    Presentation.CivicSurface.Paper);
            }
        }

        private void CreateCard(int index)
        {
            var card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = "GameplayMemoryCard_" + index;
            card.transform.SetParent(_root, false);
            card.transform.localScale = new Vector3(1.65f, 0.52f, 0.08f);
            var collider = card.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var labelObject = new GameObject("MemoryLabel");
            labelObject.transform.SetParent(card.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.58f);
            labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.fontSize = 22;
            label.characterSize = 0.055f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.08f, 0.08f, 0.07f);
            _cards.Add(card);
            _labels.Add(label);
        }

        private static Transform Find(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < all.Length; i++) if (all[i].name == name) return all[i];
            return null;
        }
    }
}
