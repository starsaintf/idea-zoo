using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdeaZoo.Core;
using IdeaZoo.Gameplay;
using UnityEngine;

namespace IdeaZoo.PlayerExperience
{
    public enum IdeaArchetype
    {
        GoodIdeaWrongAudience,
        UsefulWeakBusiness,
        ProfitableButHarmful,
        ImpossibleForNow,
        OrdinaryExceptionalExecution,
        TechnologyWithoutProblem,
        BetterAsFeature,
        HiddenAudience
    }

    public enum KeeperRank
    {
        Apprentice,
        Keeper,
        Curator,
        Warden,
        Founder
    }

    [Serializable]
    public sealed class PlayerAccessibilitySettings
    {
        public int TextScaleIndex;
        public bool ReducedMotion;
        public bool HighContrast;
        public bool LargeTouchTargets;
        public bool FocusMode = true;
        public bool Haptics = true;

        public float TextScale
        {
            get
            {
                if (TextScaleIndex <= 0) return 1f;
                if (TextScaleIndex == 1) return 1.18f;
                return 1.36f;
            }
        }

        public string TextScaleLabel
        {
            get
            {
                if (TextScaleIndex <= 0) return "STANDARD";
                if (TextScaleIndex == 1) return "LARGE";
                return "EXTRA LARGE";
            }
        }
    }

    [Serializable]
    public sealed class PlayerExperienceCaseRecord
    {
        public string RecordId = string.Empty;
        public string Title = string.Empty;
        public IdeaArchetype Archetype;
        public bool Revealed;
        public bool Tutorial;
        public bool HasRuling;
        public Ruling Ruling;
        public string Consequence = string.Empty;
        public int CompletedAtUnix;
        public List<string> TactileFindings = new List<string>();
    }

    [Serializable]
    public sealed class PlayerExperienceState
    {
        public int Version = 1;
        public bool TutorialCompleted;
        public bool OnboardingDismissed;
        public KeeperRank Rank;
        public PlayerAccessibilitySettings Accessibility = new PlayerAccessibilitySettings();
        public List<PlayerExperienceCaseRecord> Cases = new List<PlayerExperienceCaseRecord>();

        public void Trim()
        {
            if (Cases == null) Cases = new List<PlayerExperienceCaseRecord>();
            if (Cases.Count > 20) Cases.RemoveRange(0, Cases.Count - 20);
        }

        public void RecalculateRank()
        {
            var completed = Cases == null ? 0 : Cases.Count(item => item.HasRuling);
            if (completed >= 15) Rank = KeeperRank.Founder;
            else if (completed >= 10) Rank = KeeperRank.Warden;
            else if (completed >= 6) Rank = KeeperRank.Curator;
            else if (completed >= 2) Rank = KeeperRank.Keeper;
            else Rank = KeeperRank.Apprentice;
        }
    }

    public sealed class PlayerExperienceService
    {
        private readonly string _path;
        private readonly string _backupPath;
        private PlayerExperienceState _state;
        private PlayerExperienceCaseRecord _active;

        public PlayerExperienceState State { get { return _state; } }
        public PlayerExperienceCaseRecord Active { get { return _active; } }

        public PlayerExperienceService()
        {
            _path = Path.Combine(Application.persistentDataPath, "idea-zoo-player-experience.json");
            _backupPath = _path + ".backup";
            _state = Load();
            _state.RecalculateRank();
        }

        public PlayerExperienceCaseRecord BeginCase(IdeaProfile profile)
        {
            if (profile == null) return null;
            _active = _state.Cases.FirstOrDefault(item => item.RecordId == profile.RecordId);
            if (_active == null)
            {
                _active = new PlayerExperienceCaseRecord
                {
                    RecordId = profile.RecordId,
                    Title = profile.Title,
                    Archetype = PlayerExperienceArchetypeCatalog.Resolve(profile),
                    Tutorial = PlayerExperienceTutorial.IsTutorial(profile)
                };
                _state.Cases.Add(_active);
                _state.Trim();
                Save();
            }
            return _active;
        }

        public void RecordTactile(PlayerExperienceTactileOutcome outcome)
        {
            if (_active == null || outcome == null || string.IsNullOrWhiteSpace(outcome.Summary)) return;
            if (_active.TactileFindings == null) _active.TactileFindings = new List<string>();
            if (_active.TactileFindings.Count < 12) _active.TactileFindings.Add(outcome.Summary);
            Save();
        }

        public PlayerExperienceCaseRecord CompleteCase(IdeaProfile profile)
        {
            if (profile == null) return null;
            if (_active == null || _active.RecordId != profile.RecordId) BeginCase(profile);
            if (_active == null) return null;
            _active.Revealed = true;
            _active.HasRuling = profile.FinalRuling.HasValue;
            _active.Ruling = profile.FinalRuling ?? Ruling.Hibernate;
            _active.Consequence = PlayerExperienceArchetypeCatalog.Consequence(_active.Archetype, _active.Ruling);
            _active.CompletedAtUnix = Now();
            if (_active.Tutorial) _state.TutorialCompleted = true;
            _state.RecalculateRank();
            Save();
            var completed = _active;
            _active = null;
            return completed;
        }

        public void DismissOnboarding()
        {
            _state.OnboardingDismissed = true;
            Save();
        }

        public void SaveAccessibility(PlayerAccessibilitySettings settings)
        {
            _state.Accessibility = settings ?? new PlayerAccessibilitySettings();
            Save();
        }

        private PlayerExperienceState Load()
        {
            try
            {
                if (!File.Exists(_path)) return new PlayerExperienceState();
                var state = JsonUtility.FromJson<PlayerExperienceState>(File.ReadAllText(_path));
                if (state == null) return new PlayerExperienceState();
                if (state.Accessibility == null) state.Accessibility = new PlayerAccessibilitySettings();
                state.Trim();
                return state;
            }
            catch
            {
                try
                {
                    if (File.Exists(_backupPath))
                    {
                        var backup = JsonUtility.FromJson<PlayerExperienceState>(File.ReadAllText(_backupPath));
                        if (backup != null) return backup;
                    }
                }
                catch { }
                return new PlayerExperienceState();
            }
        }

        private void Save()
        {
            _state.Trim();
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(_state, true));
            if (File.Exists(_path)) File.Copy(_path, _backupPath, true);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temporary, _path);
        }

        private static int Now()
        {
            var value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (int)Math.Min(int.MaxValue, Math.Max(0L, value));
        }
    }

    public static class PlayerExperienceTutorial
    {
        public const string TutorialTitle = "The Neighbourhood Umbrella Library";

        public static IdeaIntake Intake()
        {
            return new IdeaIntake
            {
                Title = TutorialTitle,
                Idea = "A shared collection of umbrellas that neighbours can borrow from small stands during sudden rain.",
                Problem = "People are caught in sudden rain and buy cheap umbrellas they rarely use again.",
                Promise = "Help a neighbour borrow a dry umbrella in under two minutes and return it anywhere nearby.",
                Audience = "People walking through one rainy neighbourhood without an umbrella",
                Payer = "Local shops and the neighbourhood association",
                Evidence = "Three neighbours said sudden rain regularly changes their route or forces an unnecessary purchase.",
                Dependency = "Simple stands, durable umbrellas and a return system",
                Maintenance = "A named local keeper who inspects, redistributes and replaces umbrellas",
                Harm = "A tracking-heavy system could turn a simple public service into surveillance of people moving through the neighbourhood."
            };
        }

        public static bool IsTutorial(IdeaProfile profile)
        {
            return profile != null && string.Equals(profile.Title, TutorialTitle, StringComparison.OrdinalIgnoreCase);
        }

        public static string Step(int completedTests)
        {
            if (completedTests <= 0) return "Start in the Desire Yard. Ask about the rainy-day behaviour before praising the umbrella library.";
            if (completedTests == 1) return "The creature has heard a real problem. In the Commitment Paddock, remove features until one promise can be tested.";
            if (completedTests == 2) return "Demand is not enough. Enter the Burrower Tunnel and expose the weekly labour that keeps shared umbrellas usable.";
            if (completedTests == 3) return "One question remains. Take the idea to the Children’s Jury and make refusal, privacy and misuse understandable.";
            return "All four habitats have spoken. Let the idea Molt before giving it a final ruling.";
        }
    }

    public static class PlayerExperienceArchetypeCatalog
    {
        public static IdeaArchetype Resolve(IdeaProfile profile)
        {
            if (profile == null) return IdeaArchetype.TechnologyWithoutProblem;
            if (profile.Metrics.Safety < 0.36) return IdeaArchetype.ProfitableButHarmful;
            if (profile.Metrics.Feasibility < 0.34) return IdeaArchetype.ImpossibleForNow;
            if (profile.Metrics.Desirability < 0.30) return IdeaArchetype.TechnologyWithoutProblem;
            if (profile.Metrics.Viability < 0.32) return IdeaArchetype.UsefulWeakBusiness;

            var hash = GameplayDisruptionCatalog.StableHash((profile.RecordId ?? string.Empty) + "|" + (profile.Title ?? string.Empty));
            switch (hash % 4)
            {
                case 0: return IdeaArchetype.GoodIdeaWrongAudience;
                case 1: return IdeaArchetype.OrdinaryExceptionalExecution;
                case 2: return IdeaArchetype.BetterAsFeature;
                default: return IdeaArchetype.HiddenAudience;
            }
        }

        public static GameplayEncounterDefinition Decorate(IdeaProfile profile, GameplayEncounterDefinition definition)
        {
            if (definition == null) return null;
            var archetype = Resolve(profile);
            var rounds = new GameplayRound[definition.Rounds.Length];
            for (var i = 0; i < definition.Rounds.Length; i++)
            {
                var source = definition.Rounds[i];
                var choices = new GameplayChoice[source.Choices.Length];
                for (var c = 0; c < source.Choices.Length; c++)
                {
                    var original = source.Choices[c];
                    var impact = original.Impact.Clone();
                    Adjust(archetype, definition.Kind, impact);
                    choices[c] = new GameplayChoice(original.Id, original.Label, original.Consequence, impact);
                }
                rounds[i] = new GameplayRound(source.Prompt, source.Context + "\n\n" + HiddenClue(archetype, definition.Kind), choices);
            }
            return new GameplayEncounterDefinition(definition.TestId, definition.Kind, definition.Title, definition.Mission, rounds);
        }

        public static string Reveal(IdeaArchetype archetype)
        {
            switch (archetype)
            {
                case IdeaArchetype.GoodIdeaWrongAudience: return "GOOD IDEA · WRONG FIRST AUDIENCE";
                case IdeaArchetype.UsefulWeakBusiness: return "USEFUL · WEAK BUSINESS";
                case IdeaArchetype.ProfitableButHarmful: return "PROFITABLE · HARMFUL WITHOUT GUARDRAILS";
                case IdeaArchetype.ImpossibleForNow: return "VALID NEED · IMPOSSIBLE FOR NOW";
                case IdeaArchetype.OrdinaryExceptionalExecution: return "ORDINARY IDEA · EXECUTION IS THE ADVANTAGE";
                case IdeaArchetype.TechnologyWithoutProblem: return "IMPRESSIVE MECHANISM · NO URGENT PROBLEM";
                case IdeaArchetype.BetterAsFeature: return "USEFUL CORE · BETTER AS A FEATURE";
                default: return "THE HIDDEN AUDIENCE WAS STRONGER";
            }
        }

        public static string Consequence(IdeaArchetype archetype, Ruling ruling)
        {
            var action = ruling == Ruling.Build ? "entered the working Zoo" :
                ruling == Ruling.Break ? "became a memorial and a warning" :
                ruling == Ruling.Sanctuary ? "entered protected care" :
                ruling == Ruling.Molt ? "left its first skin in the Stacks" :
                "was sealed until conditions change";
            return Reveal(archetype) + " · It " + action + ".";
        }

        private static string HiddenClue(IdeaArchetype archetype, GameplayEncounterKind kind)
        {
            if (archetype == IdeaArchetype.GoodIdeaWrongAudience && kind == GameplayEncounterKind.CustomerInterview)
                return "A second group keeps appearing at the edge of the evidence.";
            if (archetype == IdeaArchetype.UsefulWeakBusiness && kind == GameplayEncounterKind.PrototypeTrial)
                return "Usefulness is visible. Willingness to fund it is not.";
            if (archetype == IdeaArchetype.ProfitableButHarmful && kind == GameplayEncounterKind.ChildrensJury)
                return "The easiest path to growth gives the creature too much power.";
            if (archetype == IdeaArchetype.ImpossibleForNow && kind == GameplayEncounterKind.FeasibilityAudit)
                return "The need is real, but one dependency belongs to a future that has not arrived.";
            if (archetype == IdeaArchetype.BetterAsFeature && kind == GameplayEncounterKind.PrototypeTrial)
                return "The useful part fits inside something people already use.";
            if (archetype == IdeaArchetype.TechnologyWithoutProblem && kind == GameplayEncounterKind.CustomerInterview)
                return "People admire the mechanism and return to their old behaviour.";
            if (archetype == IdeaArchetype.HiddenAudience && kind == GameplayEncounterKind.CustomerInterview)
                return "The intended audience is polite. Someone else is impatient.";
            return "The case contains a pattern the Board has not named yet.";
        }

        private static void Adjust(IdeaArchetype archetype, GameplayEncounterKind kind, GameplayImpact impact)
        {
            if (impact == null) return;
            if ((archetype == IdeaArchetype.GoodIdeaWrongAudience || archetype == IdeaArchetype.HiddenAudience)
                && kind == GameplayEncounterKind.CustomerInterview && impact.Tendency == GameplayTendency.Experimenter)
            {
                impact.Desirability += .025;
                impact.Evidence += 1;
            }
            else if (archetype == IdeaArchetype.UsefulWeakBusiness && kind == GameplayEncounterKind.PrototypeTrial)
            {
                impact.Viability -= .015;
            }
            else if (archetype == IdeaArchetype.ProfitableButHarmful && kind == GameplayEncounterKind.ChildrensJury)
            {
                impact.Safety += impact.Tendency == GameplayTendency.Protector || impact.Tendency == GameplayTendency.Skeptic ? .035 : -.025;
            }
            else if (archetype == IdeaArchetype.ImpossibleForNow && kind == GameplayEncounterKind.FeasibilityAudit)
            {
                impact.Feasibility -= .02;
            }
            else if (archetype == IdeaArchetype.OrdinaryExceptionalExecution && kind == GameplayEncounterKind.FeasibilityAudit)
            {
                impact.Feasibility += .02;
                impact.Viability += .015;
            }
            else if (archetype == IdeaArchetype.BetterAsFeature && kind == GameplayEncounterKind.PrototypeTrial && impact.Tendency == GameplayTendency.Simplifier)
            {
                impact.Feasibility += .035;
                impact.Viability += .025;
            }
            else if (archetype == IdeaArchetype.TechnologyWithoutProblem && kind == GameplayEncounterKind.CustomerInterview)
            {
                impact.Desirability -= .02;
            }
        }
    }

    public static class PlayerExperienceReactionCatalog
    {
        public static string AfterTest(IdeaArchetype archetype, int completedTests, GameplayTendency tendency)
        {
            if (completedTests <= 0) return string.Empty;
            if (archetype == IdeaArchetype.ProfitableButHarmful && completedTests >= 3)
                return "Mara: The numbers are improving faster than the creature’s right to exist.";
            if (archetype == IdeaArchetype.GoodIdeaWrongAudience || archetype == IdeaArchetype.HiddenAudience)
                return "The Keeper: Notice who keeps returning without being invited.";
            if (archetype == IdeaArchetype.ImpossibleForNow)
                return "Nara: A real need does not make the present capable of serving it.";
            if (tendency == GameplayTendency.Builder)
                return "The Keeper: You are moving quickly. Check what the creature has learned to hide behind momentum.";
            if (tendency == GameplayTendency.Protector)
                return "Mara: Care is not the same thing as refusing to change it.";
            if (tendency == GameplayTendency.Simplifier)
                return "The Keeper: The smaller form is beginning to tell the truth.";
            return "The Keeper: The contradiction is not an interruption. It is the case.";
        }

        public static string AtRuling(PlayerExperienceCaseRecord record)
        {
            if (record == null) return string.Empty;
            if (record.Ruling == Ruling.Break) return "The Keeper: We do not erase broken ideas. We keep the lesson where the next creature can see it.";
            if (record.Ruling == Ruling.Hibernate) return "The Keeper: Not now is a real ruling. Seal the conditions required for its return.";
            if (record.Ruling == Ruling.Sanctuary) return "Mara: Protection is work. Name who will continue doing it.";
            if (record.Ruling == Ruling.Molt) return "The Keeper: The first shape was not the idea. This one may be closer.";
            return "The Keeper: Building is not release from judgment. It is where judgment becomes maintenance.";
        }
    }
}
