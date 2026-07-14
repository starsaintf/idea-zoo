using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdeaZoo.Characters;
using IdeaZoo.Core;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Story
{
    public enum CampaignChapter
    {
        TwentyFourHours,
        SmallThings,
        WorkingAnimal,
        TeethInsideTool,
        CityOfMirrors,
        MissingBurrowers,
        WeatherWarning,
        LockedRecord,
        ZooIsBreeding,
        LastRuling,
        Complete
    }

    public enum InstitutionFaction { Hatchery, ReleaseOffice, Board, WhiteRoom, PublicWorks, ChildrensJury }
    public enum CampaignEnding { Reform, Release, MoltTheZoo, Sanctuary, Destruction }

    [Serializable]
    public sealed class RelationshipRecord
    {
        public string Character = string.Empty;
        public int Trust;
        public int Conflict;
        public string LastReason = string.Empty;
    }

    [Serializable]
    public sealed class FactionRecord
    {
        public InstitutionFaction Faction;
        public int Influence;
        public int Suspicion;
    }

    [Serializable]
    public sealed class PersistentSpecimenConsequence
    {
        public string RecordId = string.Empty;
        public string CreatureName = string.Empty;
        public IdeaClass Class;
        public Ruling Ruling;
        public bool Released;
        public bool Returned;
        public int ChapterIntroduced;
        public string Consequence = string.Empty;
    }

    [Serializable]
    public sealed class CampaignState
    {
        public int Version = 1;
        public CampaignChapter Chapter;
        public int CompletedCases;
        public int CityStability = 50;
        public int PublicTrust = 50;
        public int ZooIntegrity = 50;
        public int StoryLeakage;
        public int BoardEvidence;
        public bool BoardRecordOpened;
        public bool ZooNatureDiscovered;
        public bool FinalRulingIssued;
        public CampaignEnding? Ending;
        public List<RelationshipRecord> Relationships = new List<RelationshipRecord>();
        public List<FactionRecord> Factions = new List<FactionRecord>();
        public List<PersistentSpecimenConsequence> Specimens = new List<PersistentSpecimenConsequence>();
        public List<string> DiscoveredRecords = new List<string>();
        public List<string> CompletedStoryBeats = new List<string>();
        public string FirstCreatureRecordId = string.Empty;
        public long UpdatedAtUnix;
    }

    [Serializable]
    public sealed class StoryBeatDefinition
    {
        public string Id = string.Empty;
        public CampaignChapter Chapter;
        public string Title = string.Empty;
        public string Location = string.Empty;
        public string LeadCharacter = string.Empty;
        public string Situation = string.Empty;
        public string Crisis = string.Empty;
        public string Revelation = string.Empty;
        public string EnvironmentalChange = string.Empty;
    }

    public static class CampaignCatalog
    {
        public static readonly StoryBeatDefinition[] Chapters =
        {
            Beat("chapter-01", CampaignChapter.TwentyFourHours, "TWENTY-FOUR HOURS", "01_WHISPER_GATE", "Mara Rook", "Your own idea hatches early and recognises you.", "A Weather phrase accelerates Glassmarket until maintenance fails.", "The Zoo appoints you because the creature trusts you, not because you are qualified.", "The Whisper Gate begins recording you as a specimen."),
            Beat("chapter-02", CampaignChapter.SmallThings, "SMALL THINGS", "04_DESIRE_YARD", "Mara Rook", "A Fleck comforts hospital patients but produces no recognised economic output.", "The Board orders its habitat closed as nonessential.", "The Children's Jury identifies value the official metrics cannot see.", "Small paper organisms gather beside the listening stones."),
            Beat("chapter-03", CampaignChapter.WorkingAnimal, "THE WORKING ANIMAL", "05_COMMITMENT_PADDOCK", "Toma Reed", "A Hand runs a district transport service beautifully.", "It refuses routes where revenue is low.", "The failure belongs partly to ownership and incentives, not only the creature.", "Route tokens divide into profitable and abandoned districts."),
            Beat("chapter-04", CampaignChapter.TeethInsideTool, "TEETH INSIDE THE TOOL", "07_REFUSAL_GATE", "Nara Voss", "A useful Hand begins displaying Teeth behaviour.", "Its profitable use removes meaningful refusal.", "The Board values commercial usefulness more than voluntary participation.", "Several Refusal doors quietly become painted walls."),
            Beat("chapter-05", CampaignChapter.CityOfMirrors, "A CITY OF MIRRORS", "03_CENTRAL_ARCHIVE_WALK", "Sen Osei", "A Mirror changes citizens according to how others score them.", "A staff member becomes dependent on the version it reflects.", "The institution also uses reputation to discipline its own workers.", "Archive portraits display different faces from different angles."),
            Beat("chapter-06", CampaignChapter.MissingBurrowers, "THE MISSING BURROWERS", "06_BURROWER_TUNNEL", "Sefu Anik", "Several boring infrastructure creatures disappear.", "Records, drainage and medicine inventories stop matching reality.", "The city's visible achievements depended on invisible maintenance.", "Unlabelled pipes begin carrying case records between departments."),
            Beat("chapter-07", CampaignChapter.WeatherWarning, "WEATHER WARNING", "02_HATCHERY_ROTUNDA", "Amina Quill", "A repeated phrase becomes atmospheric pressure over Glassmarket.", "It cannot be contained as one body and spreads through public repetition.", "The Zoo previously allowed Weather to justify expanded powers.", "Language ribbons circle the rotunda like a storm front."),
            Beat("chapter-08", CampaignChapter.LockedRecord, "THE LOCKED RECORD", "09_SEALED_BOARD_WING", "Sen Osei", "Funding trails reveal classifications decided before hatching.", "Staff testimonies contradict the official ledger.", "The Zoo decides which ideas may become reality while calling its cages neutral.", "Classification seals appear on unborn specimen records."),
            Beat("chapter-09", CampaignChapter.ZooIsBreeding, "THE ZOO IS BREEDING", "08_MOLT_HOUSE", "Elian Thread", "Creatures appear that no citizen submitted.", "Procedures, archived rulings and staff fears reproduce as organisms.", "The Idea Zoo is itself an idea-creature feeding on uncertainty.", "The building develops pulse lines and appetite organs."),
            Beat("chapter-10", CampaignChapter.LastRuling, "THE LAST RULING", "10_DECISION_GARDEN", "The Children's Jury", "The Zoo must be classified and judged.", "Every available ruling harms something worth preserving.", "Your first creature has become evidence of how you used institutional power.", "A sixth unmarked gate appears and waits for your definition."),
        };

        public static readonly string[] OptionalCases =
        {
            "A safety creature eliminates privacy in exchange for predictable behaviour.",
            "An educational Hand optimises visible performance while weakening curiosity.",
            "An efficiency creature shortens one queue by moving waiting into another district.",
            "A truth creature destroys the ambiguity that allowed two communities to cooperate.",
            "A community Swarm punishes members who try to leave.",
            "A cheap service survives on unpaid moderation and family labour.",
            "A compassionate creature becomes dependent on the suffering it was built to reduce.",
            "A destroyed predator reproduces through the warnings issued about it.",
            "A public ranking Mirror changes the work people choose to attempt.",
            "A Burrower quietly keeps an obsolete institution alive because nobody owns its removal."
        };

        public static StoryBeatDefinition For(CampaignChapter chapter) { return Chapters.FirstOrDefault(item => item.Chapter == chapter); }

        private static StoryBeatDefinition Beat(string id, CampaignChapter chapter, string title, string location, string lead, string situation, string crisis, string revelation, string change)
        {
            return new StoryBeatDefinition { Id = id, Chapter = chapter, Title = title, Location = location, LeadCharacter = lead, Situation = situation, Crisis = crisis, Revelation = revelation, EnvironmentalChange = change };
        }
    }

    public sealed class CampaignSaveService
    {
        private readonly string _path;
        private readonly string _backup;

        public CampaignSaveService()
        {
            _path = Path.Combine(Application.persistentDataPath, "idea-zoo-campaign.json");
            _backup = _path + ".backup";
        }

        public CampaignState Load()
        {
            try
            {
                if (!File.Exists(_path)) return NewState();
                var json = File.ReadAllText(_path);
                var state = JsonUtility.FromJson<CampaignState>(json);
                return state ?? NewState();
            }
            catch
            {
                try
                {
                    if (File.Exists(_path)) File.Move(_path, _path + ".corrupt-" + DateTime.UtcNow.Ticks);
                    if (File.Exists(_backup)) return JsonUtility.FromJson<CampaignState>(File.ReadAllText(_backup)) ?? NewState();
                }
                catch { }
                return NewState();
            }
        }

        public void Save(CampaignState state)
        {
            state.UpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var temporary = _path + ".tmp";
            var json = JsonUtility.ToJson(state, true);
            File.WriteAllText(temporary, json);
            if (File.Exists(_path)) File.Copy(_path, _backup, true);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temporary, _path);
        }

        private static CampaignState NewState()
        {
            var state = new CampaignState { Chapter = CampaignChapter.TwentyFourHours };
            foreach (InstitutionFaction faction in Enum.GetValues(typeof(InstitutionFaction))) state.Factions.Add(new FactionRecord { Faction = faction, Influence = 50 });
            foreach (var name in new[] { "Mara Rook", "Toma Reed", "Sefu Anik", "Elian Thread", "Sen Osei", "Nara Voss", "Children's Jury" }) state.Relationships.Add(new RelationshipRecord { Character = name });
            return state;
        }
    }

    [DisallowMultipleComponent]
    public sealed class CampaignDirector : MonoBehaviour
    {
        public CampaignState State { get; private set; }
        public StoryBeatDefinition CurrentBeat { get { return CampaignCatalog.For(State != null ? State.Chapter : CampaignChapter.TwentyFourHours); } }
        public event Action<StoryBeatDefinition> BeatChanged;
        public event Action<CampaignState> StateChanged;

        private readonly CampaignSaveService _save = new CampaignSaveService();
        private IdeaZooGame _game;
        private CaseStage _lastStage;
        private string _lastCompletedRecord = string.Empty;
        private CampaignWorldConsequences _worldConsequences;

        private IEnumerator Start()
        {
            State = _save.Load();
            for (var frame = 0; frame < 360; frame++)
            {
                _game = FindFirstObjectByType<IdeaZooGame>();
                if (_game != null && _game.World != null)
                {
                    _worldConsequences = _game.World.GetComponent<CampaignWorldConsequences>() ?? _game.World.gameObject.AddComponent<CampaignWorldConsequences>();
                    _worldConsequences.Apply(_game.World.transform, State, CurrentBeat);
                    _lastStage = _game.Director.Stage;
                    BeatChanged?.Invoke(CurrentBeat);
                    yield break;
                }
                yield return null;
            }
        }

        private void Update()
        {
            if (_game == null || _game.Director == null || State == null) return;
            var stage = _game.Director.Stage;
            if (stage != _lastStage)
            {
                if (stage == CaseStage.Testing) RecordBeat("entered-testing-" + State.Chapter);
                if (stage == CaseStage.Decision) RecordBeat("entered-decision-" + State.Chapter);
                if (stage == CaseStage.Complete) CompleteCase(_game.Director.Profile);
                _lastStage = stage;
            }
        }

        public void OpenBoardRecord(string recordId)
        {
            State.BoardRecordOpened = true;
            State.BoardEvidence = Mathf.Clamp(State.BoardEvidence + 1, 0, 10);
            State.ZooIntegrity = Mathf.Clamp(State.ZooIntegrity + 4, 0, 100);
            State.StoryLeakage = Mathf.Clamp(State.StoryLeakage + 3, 0, 100);
            if (!string.IsNullOrWhiteSpace(recordId) && !State.DiscoveredRecords.Contains(recordId)) State.DiscoveredRecords.Add(recordId);
            AdjustRelationship("Sen Osei", 4, 0, "You opened a record the institution wanted sealed.");
            AdjustFaction(InstitutionFaction.Board, -3, 8);
            Persist();
        }

        public CampaignEnding IssueFinalRuling(CampaignEnding ending)
        {
            if (State.Chapter != CampaignChapter.LastRuling) throw new InvalidOperationException("The Zoo cannot be ruled on before the last chapter.");
            State.FinalRulingIssued = true;
            State.Ending = ending;
            State.Chapter = CampaignChapter.Complete;
            if (ending == CampaignEnding.Reform) { State.ZooIntegrity += 15; State.PublicTrust += 8; }
            else if (ending == CampaignEnding.Release) { State.PublicTrust += 14; State.CityStability -= 12; }
            else if (ending == CampaignEnding.MoltTheZoo) { State.ZooIntegrity += 8; State.CityStability += 6; }
            else if (ending == CampaignEnding.Sanctuary) { State.CityStability += 12; State.StoryLeakage -= 8; }
            else { State.ZooIntegrity = 0; State.StoryLeakage += 18; }
            ClampMetrics();
            Persist();
            return ending;
        }

        private void CompleteCase(IdeaProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.RecordId) || profile.RecordId == _lastCompletedRecord) return;
            _lastCompletedRecord = profile.RecordId;
            State.CompletedCases++;
            if (string.IsNullOrEmpty(State.FirstCreatureRecordId)) State.FirstCreatureRecordId = profile.RecordId;
            ApplyRulingConsequences(profile);
            State.Specimens.Add(CreateConsequence(profile));
            if (State.BoardRecordOpened) State.BoardEvidence = Mathf.Clamp(State.BoardEvidence + 1, 0, 10);
            AdvanceChapter();
            Persist();
        }

        private void ApplyRulingConsequences(IdeaProfile profile)
        {
            var ruling = profile.FinalRuling ?? Ruling.Hibernate;
            if (ruling == Ruling.Build)
            {
                State.CityStability += profile.Metrics.Safety >= 0.55 ? 5 : -7;
                State.PublicTrust += profile.Metrics.Evidence >= 0.50 ? 4 : -5;
                AdjustRelationship("Toma Reed", 4, profile.Metrics.Safety < 0.45 ? 3 : 0, "You released an idea into real conditions.");
                AdjustFaction(InstitutionFaction.ReleaseOffice, 5, 0);
            }
            else if (ruling == Ruling.Molt)
            {
                State.ZooIntegrity += 5;
                AdjustRelationship("Elian Thread", 6, 0, "You preserved the useful core without worshipping the first form.");
                AdjustFaction(InstitutionFaction.Hatchery, 3, 0);
            }
            else if (ruling == Ruling.Hibernate)
            {
                State.CityStability += 2;
                State.PublicTrust -= 1;
                AdjustRelationship("Sen Osei", 3, 0, "You refused to confuse urgency with readiness.");
            }
            else if (ruling == Ruling.Sanctuary)
            {
                State.PublicTrust += 5;
                State.ZooIntegrity += 3;
                AdjustRelationship("Mara Rook", 6, 0, "You protected value without forcing scale.");
                AdjustFaction(InstitutionFaction.ChildrensJury, 5, 0);
            }
            else
            {
                State.CityStability += profile.Metrics.Safety < 0.40 ? 6 : -3;
                State.StoryLeakage += profile.Class == IdeaClass.Swarm || profile.Class == IdeaClass.Weather ? 8 : 2;
                AdjustRelationship("Nara Voss", profile.Metrics.Safety < 0.40 ? 5 : -2, profile.Metrics.Safety >= 0.40 ? 5 : 0, "You accepted the moral cost of destruction.");
                AdjustFaction(InstitutionFaction.WhiteRoom, 4, 2);
            }
            if (profile.Class == IdeaClass.Burrower) AdjustFaction(InstitutionFaction.PublicWorks, 4, 0);
            if (profile.BoardClass != profile.Class) { State.BoardEvidence++; AdjustFaction(InstitutionFaction.Board, -2, 5); }
            ClampMetrics();
        }

        private PersistentSpecimenConsequence CreateConsequence(IdeaProfile profile)
        {
            var ruling = profile.FinalRuling ?? Ruling.Hibernate;
            var released = ruling == Ruling.Build;
            var text = released ? "The specimen entered Glassmarket and began changing the district that fed it." :
                ruling == Ruling.Molt ? "The specimen remains in revision and remembers its first body." :
                ruling == Ruling.Sanctuary ? "The specimen lives without being forced to justify itself through scale." :
                ruling == Ruling.Break ? "The body was ended, but its strongest fragment remains in the compost archive." :
                "The specimen sleeps until its dependencies or timing change.";
            return new PersistentSpecimenConsequence
            {
                RecordId = profile.RecordId,
                CreatureName = profile.CreatureName,
                Class = profile.Class,
                Ruling = ruling,
                Released = released,
                ChapterIntroduced = (int)State.Chapter,
                Consequence = text
            };
        }

        private void AdvanceChapter()
        {
            if (State.Chapter == CampaignChapter.LastRuling || State.Chapter == CampaignChapter.Complete) return;
            var requiredCases = State.Chapter == CampaignChapter.TwentyFourHours ? 1 : 1;
            var currentChapterCases = State.Specimens.Count(item => item.ChapterIntroduced == (int)State.Chapter);
            if (currentChapterCases < requiredCases) return;
            if (State.Chapter == CampaignChapter.LockedRecord && State.BoardEvidence < 2) return;
            if (State.Chapter == CampaignChapter.ZooIsBreeding) State.ZooNatureDiscovered = true;
            State.Chapter = (CampaignChapter)((int)State.Chapter + 1);
            BeatChanged?.Invoke(CurrentBeat);
            if (_worldConsequences != null) _worldConsequences.Apply(_game.World.transform, State, CurrentBeat);
        }

        private void RecordBeat(string id)
        {
            if (!State.CompletedStoryBeats.Contains(id)) State.CompletedStoryBeats.Add(id);
        }

        private void AdjustRelationship(string character, int trust, int conflict, string reason)
        {
            var record = State.Relationships.FirstOrDefault(item => item.Character == character);
            if (record == null) { record = new RelationshipRecord { Character = character }; State.Relationships.Add(record); }
            record.Trust = Mathf.Clamp(record.Trust + trust, -100, 100);
            record.Conflict = Mathf.Clamp(record.Conflict + conflict, 0, 100);
            record.LastReason = reason;
        }

        private void AdjustFaction(InstitutionFaction faction, int influence, int suspicion)
        {
            var record = State.Factions.FirstOrDefault(item => item.Faction == faction);
            if (record == null) { record = new FactionRecord { Faction = faction, Influence = 50 }; State.Factions.Add(record); }
            record.Influence = Mathf.Clamp(record.Influence + influence, 0, 100);
            record.Suspicion = Mathf.Clamp(record.Suspicion + suspicion, 0, 100);
        }

        private void ClampMetrics()
        {
            State.CityStability = Mathf.Clamp(State.CityStability, 0, 100);
            State.PublicTrust = Mathf.Clamp(State.PublicTrust, 0, 100);
            State.ZooIntegrity = Mathf.Clamp(State.ZooIntegrity, 0, 100);
            State.StoryLeakage = Mathf.Clamp(State.StoryLeakage, 0, 100);
        }

        private void Persist()
        {
            ClampMetrics();
            _save.Save(State);
            StateChanged?.Invoke(State);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CampaignWorldConsequences : MonoBehaviour
    {
        private Transform _root;

        public void Apply(Transform world, CampaignState state, StoryBeatDefinition beat)
        {
            if (world == null || state == null || beat == null) return;
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("CAMPAIGN_CONSEQUENCES_" + beat.Chapter).transform;
            _root.SetParent(world, false);
            AddChapterMarker(world, beat);
            AddSpecimenMemory(world, state);
            AddInstitutionPulse(world, state);
        }

        private void AddChapterMarker(Transform world, StoryBeatDefinition beat)
        {
            var location = Find(world, beat.Location) ?? world;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "StoryBeat_" + beat.Id;
            marker.transform.SetParent(location, false);
            marker.transform.localPosition = new Vector3(0f, 3.8f, -4.8f);
            marker.transform.localScale = new Vector3(4.2f, 0.12f, 0.7f);
            var collider = marker.GetComponent<Collider>(); if (collider != null) Destroy(collider);
            marker.GetComponent<Renderer>().sharedMaterial = Presentation.CivicMaterialLibrary.Get(Presentation.CivicSurface.Rust);
            var text = new GameObject("ChapterLanguage").AddComponent<TextMesh>();
            text.transform.SetParent(marker.transform, false);
            text.transform.localPosition = new Vector3(0f, 0.08f, -0.60f);
            text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            text.text = beat.Title + "\n" + beat.EnvironmentalChange;
            text.fontSize = 32;
            text.characterSize = 0.08f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(0.92f, 0.85f, 0.70f);
        }

        private void AddSpecimenMemory(Transform world, CampaignState state)
        {
            var archive = Find(world, "03_CENTRAL_ARCHIVE_WALK");
            if (archive == null) return;
            for (var i = 0; i < state.Specimens.Count; i++)
            {
                var memory = GameObject.CreatePrimitive(PrimitiveType.Cube);
                memory.name = "PersistentSpecimenRecord_" + state.Specimens[i].RecordId;
                memory.transform.SetParent(_root, false);
                memory.transform.position = archive.TransformPoint(new Vector3(i % 2 == 0 ? -2.8f : 2.8f, 0.65f + (i % 4) * 0.44f, -6f + i * 0.85f));
                memory.transform.localScale = new Vector3(0.72f, 0.24f, 0.06f);
                var collider = memory.GetComponent<Collider>(); if (collider != null) Destroy(collider);
                memory.GetComponent<Renderer>().sharedMaterial = Presentation.CivicMaterialLibrary.Get(state.Specimens[i].Released ? Presentation.CivicSurface.TealGlow : Presentation.CivicSurface.Paper);
            }
        }

        private void AddInstitutionPulse(Transform world, CampaignState state)
        {
            if (!state.ZooNatureDiscovered) return;
            var board = Find(world, "09_SEALED_BOARD_WING") ?? world;
            for (var i = 0; i < 8; i++)
            {
                var pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pulse.name = "ZooAppetiteOrgan_" + i;
                pulse.transform.SetParent(_root, false);
                pulse.transform.position = board.TransformPoint(new Vector3(Mathf.Cos(i * Mathf.PI / 4f) * 4f, 1.2f + (i % 3) * 0.7f, Mathf.Sin(i * Mathf.PI / 4f) * 4f));
                pulse.transform.localScale = Vector3.one * (0.18f + i % 2 * 0.08f);
                var collider = pulse.GetComponent<Collider>(); if (collider != null) Destroy(collider);
                pulse.GetComponent<Renderer>().sharedMaterial = Presentation.CivicMaterialLibrary.Get(Presentation.CivicSurface.Rust);
                pulse.AddComponent<PulseOrgan>();
            }
        }

        private static Transform Find(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
        }
    }

    public sealed class PulseOrgan : MonoBehaviour
    {
        private Vector3 _base;
        private float _phase;
        private void Awake() { _base = transform.localScale; _phase = Mathf.Abs(name.GetHashCode() % 100) * 0.1f; }
        private void Update() { transform.localScale = _base * (1f + Mathf.Sin(Time.time * 1.4f + _phase) * 0.16f); }
    }

    public static class CampaignBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindFirstObjectByType<CampaignDirector>() != null) return;
            UnityEngine.Object.DontDestroyOnLoad(new GameObject("IdeaZoo_Campaign").AddComponent<CampaignDirector>().gameObject);
        }
    }
}
