using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace IdeaZoo.Core
{
    public enum IdeaClass { Fleck, Hand, Mirror, Teeth, Swarm, Weather, Burrower }
    public enum Appetite { Attention, Data, Money, Trust, Obedience, Labour, Care, Time }
    public enum CaseStage { Intake, Hatching, Testing, Molt, Decision, Complete }
    public enum Ruling { Build, Molt, Hibernate, Sanctuary, Break }

    [Serializable]
    public sealed class IdeaIntake
    {
        public string Title = string.Empty;
        public string Idea = string.Empty;
        public string Problem = string.Empty;
        public string Promise = string.Empty;
        public string Audience = string.Empty;
        public string Payer = string.Empty;
        public string Evidence = string.Empty;
        public string Dependency = string.Empty;
        public string Maintenance = string.Empty;
        public string Harm = string.Empty;
    }

    [Serializable]
    public sealed class IdeaMetrics
    {
        public double Desirability;
        public double Feasibility;
        public double Viability;
        public double Safety;
        public double Evidence;

        public IdeaMetrics Clone()
        {
            return new IdeaMetrics
            {
                Desirability = Desirability,
                Feasibility = Feasibility,
                Viability = Viability,
                Safety = Safety,
                Evidence = Evidence
            };
        }
    }

    [Serializable]
    public sealed class EvidenceRecord
    {
        public string TestId = string.Empty;
        public int Strength;
        public string Note = string.Empty;
        public DateTime RecordedAtUtc;
    }

    [Serializable]
    public sealed class MoltRevision
    {
        public string PreviousPromise = string.Empty;
        public string RevisedPromise = string.Empty;
        public string PreviousAudience = string.Empty;
        public string RevisedAudience = string.Empty;
        public List<string> Guardrails = new List<string>();
        public DateTime RecordedAtUtc;
    }

    [Serializable]
    public sealed class IdeaProfile
    {
        public string Title = string.Empty;
        public string PlainIdea = string.Empty;
        public string Problem = string.Empty;
        public string Promise = string.Empty;
        public string Audience = string.Empty;
        public string Payer = string.Empty;
        public string Dependency = string.Empty;
        public string Maintenance = string.Empty;
        public string Harm = string.Empty;
        public string CreatureName = string.Empty;
        public string HiddenBurden = string.Empty;
        public IdeaClass Class;
        public IdeaClass BoardClass;
        public Appetite Appetite;
        public IdeaMetrics Metrics = new IdeaMetrics();
        public List<string> Assumptions = new List<string>();
        public List<EvidenceRecord> Evidence = new List<EvidenceRecord>();
        public List<MoltRevision> Revisions = new List<MoltRevision>();
        public List<string> Guardrails = new List<string>();
        public Ruling? FinalRuling;
        public List<string> NextActions = new List<string>();
        public string VerdictReason = string.Empty;
        public string RecordId = string.Empty;
        public DateTime CreatedAtUtc;
        public DateTime UpdatedAtUtc;
    }

    public sealed class IdeaZooCaseDirector
    {
        private readonly HashSet<string> _completedTests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public CaseStage Stage { get; private set; } = CaseStage.Intake;
        public IdeaProfile Profile { get; private set; }
        public IReadOnlyCollection<string> CompletedTests { get { return _completedTests; } }

        public IdeaProfile Begin(IdeaIntake intake)
        {
            if (Stage != CaseStage.Intake) throw new InvalidOperationException("A case is already active.");
            Profile = IdeaAnalyzer.Analyze(intake);
            Stage = CaseStage.Hatching;
            return Profile;
        }

        public void EnterZoo()
        {
            Require(CaseStage.Hatching);
            Stage = CaseStage.Testing;
        }

        public bool RecordEvidence(string testId, int strength, string note)
        {
            Require(CaseStage.Testing);
            if (!IdeaAnalyzer.ValidTests.Contains(testId)) return false;
            if (_completedTests.Contains(testId)) return false;
            if (strength < 0 || strength > 3) throw new ArgumentOutOfRangeException("strength");
            if (string.IsNullOrWhiteSpace(note)) throw new ArgumentException("Evidence needs a note.");

            IdeaAnalyzer.ApplyEvidence(Profile, testId, strength, note);
            _completedTests.Add(testId);
            return true;
        }

        public void EnterMolt()
        {
            Require(CaseStage.Testing);
            if (_completedTests.Count < IdeaAnalyzer.ValidTests.Count)
                throw new InvalidOperationException("All four habitats must be completed before a Molt.");
            Stage = CaseStage.Molt;
        }

        public void CancelMolt()
        {
            Require(CaseStage.Molt);
            Stage = CaseStage.Testing;
        }

        public void ApplyMolt(string promise, string audience, IEnumerable<string> guardrails)
        {
            Require(CaseStage.Molt);
            var cleanPromise = IdeaAnalyzer.Clean(promise);
            var cleanAudience = IdeaAnalyzer.Clean(audience);
            var rules = (guardrails ?? Enumerable.Empty<string>())
                .Select(IdeaAnalyzer.Clean)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleanPromise.Length == 0 || cleanAudience.Length == 0)
                throw new ArgumentException("A Molt needs a measurable promise and a specific audience.");

            var changed = !string.Equals(cleanPromise, Profile.Promise, StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(cleanAudience, Profile.Audience, StringComparison.OrdinalIgnoreCase)
                          || rules.Count > 0;
            if (!changed) throw new InvalidOperationException("The idea did not change.");

            IdeaAnalyzer.ApplyMolt(Profile, cleanPromise, cleanAudience, rules);
            Stage = CaseStage.Decision;
        }

        public void IssueRuling(Ruling ruling)
        {
            Require(CaseStage.Decision);
            if (Profile.FinalRuling.HasValue) throw new InvalidOperationException("The ruling is final.");
            IdeaAnalyzer.ApplyRuling(Profile, ruling);
            Stage = CaseStage.Complete;
        }

        public void Reset()
        {
            Stage = CaseStage.Intake;
            Profile = null;
            _completedTests.Clear();
        }

        private void Require(CaseStage expected)
        {
            if (Stage != expected) throw new InvalidOperationException("Expected " + expected + ", found " + Stage + ".");
        }
    }

    public static class IdeaAnalyzer
    {
        public static readonly IReadOnlyCollection<string> ValidTests = new[] { "desire", "commitment", "burden", "refusal" };

        private static readonly Dictionary<IdeaClass, string[]> ClassWords = new Dictionary<IdeaClass, string[]>
        {
            { IdeaClass.Fleck, new[] { "comfort", "delight", "journal", "poem", "memory", "gift", "personal", "small" } },
            { IdeaClass.Hand, new[] { "tool", "service", "help", "build", "translate", "work", "deliver", "device", "hardware", "assist" } },
            { IdeaClass.Mirror, new[] { "rank", "reputation", "identity", "profile", "score", "recommend", "personalize", "status" } },
            { IdeaClass.Teeth, new[] { "control", "enforce", "monitor", "surveil", "mandatory", "weapon", "punish", "ban" } },
            { IdeaClass.Swarm, new[] { "social", "share", "viral", "community", "network", "marketplace", "creator", "invite" } },
            { IdeaClass.Weather, new[] { "culture", "movement", "public", "everyone", "society", "narrative", "campaign", "belief" } },
            { IdeaClass.Burrower, new[] { "infrastructure", "records", "workflow", "compliance", "archive", "maintenance", "operations", "standard" } }
        };

        private static readonly Dictionary<Appetite, string[]> AppetiteWords = new Dictionary<Appetite, string[]>
        {
            { Appetite.Attention, new[] { "attention", "views", "audience", "engagement", "content" } },
            { Appetite.Data, new[] { "data", "tracking", "model", "models", "ai", "personalize", "recording" } },
            { Appetite.Money, new[] { "revenue", "paid", "price", "sale", "subscription", "profit" } },
            { Appetite.Trust, new[] { "trust", "private", "secure", "health", "finance", "legal" } },
            { Appetite.Obedience, new[] { "mandatory", "enforce", "control", "policy", "compliance" } },
            { Appetite.Labour, new[] { "worker", "manual", "operate", "moderate", "maintain", "support" } },
            { Appetite.Care, new[] { "care", "wellbeing", "comfort", "friend", "support" } },
            { Appetite.Time, new[] { "faster", "speed", "instant", "waiting", "time" } }
        };

        public static IdeaProfile Analyze(IdeaIntake intake)
        {
            if (intake == null) throw new ArgumentNullException("intake");
            if (string.IsNullOrWhiteSpace(intake.Title) || string.IsNullOrWhiteSpace(intake.Idea) || string.IsNullOrWhiteSpace(intake.Promise))
                throw new ArgumentException("The Whisper Gate needs a title, a plain idea and a measurable promise.");

            var raw = string.Join(" ", new[]
            {
                intake.Title, intake.Idea, intake.Problem, intake.Promise, intake.Audience,
                intake.Payer, intake.Evidence, intake.Dependency, intake.Maintenance, intake.Harm
            });
            var tokens = Tokens(raw);
            var ideaClass = PickClass(tokens, string.IsNullOrWhiteSpace(intake.Payer));
            var appetite = PickAppetite(tokens);
            var evidence = EvidenceScore(intake.Evidence);
            var risk = RiskScore(tokens, intake.Harm);
            var now = DateTime.UtcNow;

            var profile = new IdeaProfile
            {
                Title = Clean(intake.Title),
                PlainIdea = Clean(intake.Idea),
                Problem = Clean(intake.Problem),
                Promise = Clean(intake.Promise),
                Audience = Clean(intake.Audience),
                Payer = Clean(intake.Payer),
                Dependency = Clean(intake.Dependency),
                Maintenance = Clean(intake.Maintenance),
                Harm = Clean(intake.Harm),
                Class = ideaClass,
                BoardClass = BoardClass(tokens, intake.Payer),
                Appetite = appetite,
                CreatureName = CreatureName(intake.Title, ideaClass),
                HiddenBurden = string.IsNullOrWhiteSpace(intake.Maintenance)
                    ? "Unnamed recurring " + appetite.ToString().ToLowerInvariant() + ", support and maintenance"
                    : Clean(intake.Maintenance),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                RecordId = Slug(intake.Title) + "-" + now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)
            };

            profile.Metrics = new IdeaMetrics
            {
                Evidence = evidence,
                Desirability = Clamp(0.30 + evidence * 0.55 + (Clean(intake.Audience).Length > 7 ? 0.10 : 0.0)),
                Feasibility = Clamp(0.38 + (Clean(intake.Dependency).Length > 6 ? 0.09 : 0.0) + (Clean(intake.Maintenance).Length > 8 ? 0.09 : 0.0)),
                Viability = Clamp(0.20 + (Clean(intake.Payer).Length > 4 ? 0.18 : 0.0) + (ContainsExact(Tokens(intake.Evidence), "paid") ? 0.28 : 0.0)),
                Safety = Clamp(1.0 - risk)
            };

            BuildAssumptions(profile, intake);
            return profile;
        }

        public static void ApplyEvidence(IdeaProfile profile, string testId, int strength, string note)
        {
            var normalized = strength / 3.0;
            if (testId == "desire") profile.Metrics.Desirability = Clamp(profile.Metrics.Desirability * 0.55 + normalized * 0.45);
            if (testId == "commitment") profile.Metrics.Viability = Clamp(profile.Metrics.Viability * 0.45 + normalized * 0.55);
            if (testId == "burden")
            {
                profile.Metrics.Feasibility = Clamp(profile.Metrics.Feasibility + (normalized - 0.5) * 0.28);
                profile.Metrics.Safety = Clamp(profile.Metrics.Safety + (normalized - 0.5) * 0.18);
            }
            if (testId == "refusal") profile.Metrics.Safety = Clamp(profile.Metrics.Safety * 0.55 + normalized * 0.45);

            if (strength > 0) profile.Metrics.Evidence = Clamp(profile.Metrics.Evidence + 0.08 + normalized * 0.10);
            profile.Evidence.Add(new EvidenceRecord
            {
                TestId = testId,
                Strength = strength,
                Note = Clean(note),
                RecordedAtUtc = DateTime.UtcNow
            });
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        public static void ApplyMolt(IdeaProfile profile, string promise, string audience, List<string> guardrails)
        {
            profile.Revisions.Add(new MoltRevision
            {
                PreviousPromise = profile.Promise,
                RevisedPromise = promise,
                PreviousAudience = profile.Audience,
                RevisedAudience = audience,
                Guardrails = new List<string>(guardrails),
                RecordedAtUtc = DateTime.UtcNow
            });
            profile.Promise = promise;
            profile.Audience = audience;
            profile.Guardrails = new List<string>(guardrails);
            profile.Metrics.Safety = Clamp(profile.Metrics.Safety + guardrails.Count * 0.06);
            profile.Metrics.Feasibility = Clamp(profile.Metrics.Feasibility - guardrails.Count * 0.018);
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        public static void ApplyRuling(IdeaProfile profile, Ruling ruling)
        {
            profile.FinalRuling = ruling;
            profile.NextActions = NextActions(profile, ruling);
            profile.VerdictReason = VerdictReason(profile, ruling);
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        public static string Clean(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");
        }

        private static HashSet<string> Tokens(string value)
        {
            return new HashSet<string>(Regex.Matches((value ?? string.Empty).ToLowerInvariant(), @"[a-z0-9]+")
                .Cast<Match>().Select(match => match.Value), StringComparer.OrdinalIgnoreCase);
        }

        private static bool ContainsExact(HashSet<string> tokens, string value) { return tokens.Contains(value); }

        private static IdeaClass PickClass(HashSet<string> tokens, bool noPayer)
        {
            var scores = ClassWords.ToDictionary(pair => pair.Key, pair => pair.Value.Count(tokens.Contains));
            if (noPayer) scores[IdeaClass.Fleck] += 1;
            return scores.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First().Key;
        }

        private static Appetite PickAppetite(HashSet<string> tokens)
        {
            var scores = AppetiteWords.ToDictionary(pair => pair.Key, pair => pair.Value.Count(tokens.Contains));
            var winner = scores.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First();
            return winner.Value == 0 ? Appetite.Attention : winner.Key;
        }

        private static IdeaClass BoardClass(HashSet<string> tokens, string payer)
        {
            if (!string.IsNullOrWhiteSpace(payer) || tokens.Contains("revenue") || tokens.Contains("business")) return IdeaClass.Hand;
            if (tokens.Contains("social") || tokens.Contains("platform")) return IdeaClass.Swarm;
            return IdeaClass.Fleck;
        }

        private static double EvidenceScore(string evidence)
        {
            var tokens = Tokens(evidence);
            var score = 0.08;
            foreach (var word in new[] { "interviewed", "customer", "users", "paid", "pilot", "prototype", "tested", "revenue", "preorder" })
                if (tokens.Contains(word)) score += 0.11;
            return Clamp(score, 0.88);
        }

        private static double RiskScore(HashSet<string> tokens, string harm)
        {
            var score = 0.18 + (Clean(harm).Length > 18 ? 0.12 : 0.0);
            foreach (var word in new[] { "data", "children", "health", "money", "surveil", "mandatory", "control", "worker" })
                if (tokens.Contains(word)) score += 0.07;
            return Clamp(score, 0.92);
        }

        private static void BuildAssumptions(IdeaProfile profile, IdeaIntake intake)
        {
            if (Clean(intake.Audience).Length < 8) profile.Assumptions.Add("The first user is not yet specific enough.");
            if (Clean(intake.Payer).Length < 3) profile.Assumptions.Add("The payer and beneficiary may be different people.");
            if (Clean(intake.Evidence).Length < 12) profile.Assumptions.Add("The problem is supported more by belief than evidence.");
            if (Clean(intake.Maintenance).Length < 8) profile.Assumptions.Add("The recurring maintenance burden is unnamed.");
            if (Clean(intake.Harm).Length < 8) profile.Assumptions.Add("The cruelest plausible use is still unnamed.");
            if (profile.Assumptions.Count == 0) profile.Assumptions.Add("The strongest remaining assumptions are hidden in scale, ownership and timing.");
        }

        private static List<string> NextActions(IdeaProfile profile, Ruling ruling)
        {
            if (ruling == Ruling.Build) return new List<string> { "Run the highest-risk test before adding features.", "Ask three qualified users for a concrete commitment.", "Build the smallest version capable of disproving " + profile.Title + "." };
            if (ruling == Ruling.Molt) return new List<string> { "Rewrite the promise in one measurable sentence.", "Retest the narrower version with five people.", "Remove one expensive dependency before expanding scope." };
            if (ruling == Ruling.Hibernate) return new List<string> { "Write three conditions that would make the timing right.", "Choose a review date and stop spending until then.", "Preserve the research, contacts and prototype." };
            if (ruling == Ruling.Sanctuary) return new List<string> { "Define success without compulsory scale.", "Set a humane time and money boundary.", "Keep the idea alive for craft, learning or community value." };
            return new List<string> { "Write which assumption failed and what evidence changed your mind.", "Extract reusable assets, relationships and technical lessons.", "Tell collaborators the project is closed instead of leaving it undead." };
        }

        private static string VerdictReason(IdeaProfile profile, Ruling ruling)
        {
            if (ruling == Ruling.Build) return "The specimen has enough evidence to earn another controlled investment.";
            if (ruling == Ruling.Molt) return "The useful core may survive, but its current form carries avoidable weakness.";
            if (ruling == Ruling.Hibernate) return "The idea may work later, but current timing or dependencies are hostile.";
            if (ruling == Ruling.Sanctuary) return "The idea has value that should not be distorted by compulsory scale.";
            return string.Format(CultureInfo.InvariantCulture, "Ending it now protects future time. Evidence {0:0}%, safety {1:0}%.", profile.Metrics.Evidence * 100.0, profile.Metrics.Safety * 100.0);
        }

        private static string CreatureName(string title, IdeaClass ideaClass)
        {
            var suffix = new Dictionary<IdeaClass, string>
            {
                { IdeaClass.Fleck, "Moth" }, { IdeaClass.Hand, "Bearer" }, { IdeaClass.Mirror, "Glassling" },
                { IdeaClass.Teeth, "Crown" }, { IdeaClass.Swarm, "Choir" }, { IdeaClass.Weather, "Front" },
                { IdeaClass.Burrower, "Numberer" }
            }[ideaClass];
            var clean = Clean(title);
            if (clean.Length > 24) clean = clean.Substring(0, 24);
            return clean + " " + suffix;
        }

        private static string Slug(string value)
        {
            var clean = Regex.Replace(Clean(value).ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return clean.Length == 0 ? "idea" : clean;
        }

        private static double Clamp(double value, double maximum = 1.0)
        {
            return Math.Max(0.0, Math.Min(maximum, value));
        }
    }
}
