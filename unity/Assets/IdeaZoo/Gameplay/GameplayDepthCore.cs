using System;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Core;
using UnityEngine;

namespace IdeaZoo.Gameplay
{
    public enum GameplayTendency
    {
        Experimenter,
        Protector,
        Builder,
        Skeptic,
        Simplifier
    }

    public enum GameplayEncounterKind
    {
        CustomerInterview,
        PrototypeTrial,
        FeasibilityAudit,
        ChildrensJury
    }

    public enum GameplayDisruptionKind
    {
        CompetitorLaunch,
        CostShock,
        AudiencePivot,
        PrivacyConcern,
        PrematureAttention
    }

    [Serializable]
    public sealed class GameplayResourceState
    {
        public int Time = 18;
        public int Trust = 8;
        public int Momentum = 7;
        public int Evidence;

        public static GameplayResourceState Fresh()
        {
            return new GameplayResourceState();
        }

        public GameplayResourceState Clone()
        {
            return new GameplayResourceState
            {
                Time = Time,
                Trust = Trust,
                Momentum = Momentum,
                Evidence = Evidence
            };
        }

        public bool CanApply(GameplayImpact impact)
        {
            if (impact == null) return false;
            return Time + impact.Time >= 0
                   && Trust + impact.Trust >= 0
                   && Momentum + impact.Momentum >= 0;
        }

        public void Apply(GameplayImpact impact)
        {
            if (impact == null) return;
            Time = Mathf.Clamp(Time + impact.Time, 0, 24);
            Trust = Mathf.Clamp(Trust + impact.Trust, 0, 12);
            Momentum = Mathf.Clamp(Momentum + impact.Momentum, 0, 12);
            Evidence = Mathf.Clamp(Evidence + impact.Evidence, 0, 30);
        }

        public string Compact()
        {
            return "TIME " + Time + "  ·  TRUST " + Trust + "  ·  MOMENTUM " + Momentum + "  ·  EVIDENCE " + Evidence;
        }
    }

    [Serializable]
    public sealed class GameplayImpact
    {
        public int Time;
        public int Trust;
        public int Momentum;
        public int Evidence;
        public int TestScore;
        public double Desirability;
        public double Feasibility;
        public double Viability;
        public double Safety;
        public GameplayTendency Tendency;

        public GameplayImpact Clone()
        {
            return (GameplayImpact)MemberwiseClone();
        }
    }

    public sealed class GameplayChoice
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string Consequence;
        public readonly GameplayImpact Impact;

        public GameplayChoice(string id, string label, string consequence, GameplayImpact impact)
        {
            Id = id;
            Label = label;
            Consequence = consequence;
            Impact = impact;
        }
    }

    public sealed class GameplayRound
    {
        public readonly string Prompt;
        public readonly string Context;
        public readonly GameplayChoice[] Choices;

        public GameplayRound(string prompt, string context, params GameplayChoice[] choices)
        {
            Prompt = prompt;
            Context = context;
            Choices = choices ?? Array.Empty<GameplayChoice>();
        }
    }

    public sealed class GameplayEncounterDefinition
    {
        public readonly string TestId;
        public readonly GameplayEncounterKind Kind;
        public readonly string Title;
        public readonly string Mission;
        public readonly GameplayRound[] Rounds;

        public GameplayEncounterDefinition(string testId, GameplayEncounterKind kind, string title, string mission, params GameplayRound[] rounds)
        {
            TestId = testId;
            Kind = kind;
            Title = title;
            Mission = mission;
            Rounds = rounds ?? Array.Empty<GameplayRound>();
        }
    }

    public sealed class GameplayDisruptionDefinition
    {
        public readonly GameplayDisruptionKind Kind;
        public readonly string Title;
        public readonly string Situation;
        public readonly GameplayChoice[] Choices;

        public GameplayDisruptionDefinition(GameplayDisruptionKind kind, string title, string situation, params GameplayChoice[] choices)
        {
            Kind = kind;
            Title = title;
            Situation = situation;
            Choices = choices ?? Array.Empty<GameplayChoice>();
        }
    }

    public sealed class GameplayEncounterRun
    {
        private readonly List<string> _choiceIds = new List<string>(4);
        private readonly List<string> _consequences = new List<string>(4);
        private readonly Dictionary<GameplayTendency, int> _tendencies = new Dictionary<GameplayTendency, int>();
        private int _roundIndex;
        private int _score;
        private int _evidenceGained;
        private double _desirability;
        private double _feasibility;
        private double _viability;
        private double _safety;

        public GameplayEncounterDefinition Definition { get; }
        public int RoundIndex { get { return _roundIndex; } }
        public bool Complete { get { return _roundIndex >= Definition.Rounds.Length; } }
        public GameplayRound CurrentRound { get { return Complete ? null : Definition.Rounds[_roundIndex]; } }
        public IReadOnlyList<string> ChoiceIds { get { return _choiceIds; } }
        public IReadOnlyList<string> Consequences { get { return _consequences; } }
        public string LastConsequence { get { return _consequences.Count == 0 ? string.Empty : _consequences[_consequences.Count - 1]; } }

        public GameplayEncounterRun(GameplayEncounterDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException("definition");
        }

        public bool Choose(int index, GameplayResourceState resources, out string error)
        {
            error = string.Empty;
            if (Complete)
            {
                error = "This encounter is already complete.";
                return false;
            }

            var round = CurrentRound;
            if (index < 0 || index >= round.Choices.Length)
            {
                error = "That response is unavailable.";
                return false;
            }

            var choice = round.Choices[index];
            if (!resources.CanApply(choice.Impact))
            {
                error = "You do not have enough time, trust or momentum for that move.";
                return false;
            }

            resources.Apply(choice.Impact);
            _choiceIds.Add(choice.Id);
            _consequences.Add(choice.Consequence);
            _score += choice.Impact.TestScore;
            _evidenceGained += Mathf.Max(0, choice.Impact.Evidence);
            _desirability += choice.Impact.Desirability;
            _feasibility += choice.Impact.Feasibility;
            _viability += choice.Impact.Viability;
            _safety += choice.Impact.Safety;
            if (!_tendencies.ContainsKey(choice.Impact.Tendency)) _tendencies[choice.Impact.Tendency] = 0;
            _tendencies[choice.Impact.Tendency]++;
            _roundIndex++;
            return true;
        }

        public int Strength()
        {
            if (Definition.Rounds.Length == 0) return 0;
            var average = (double)_score / Definition.Rounds.Length;
            if (_evidenceGained >= 6) average += 0.25;
            return Mathf.Clamp(Mathf.RoundToInt((float)average), 0, 3);
        }

        public GameplayTendency DominantTendency()
        {
            if (_tendencies.Count == 0) return GameplayTendency.Skeptic;
            return _tendencies.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First().Key;
        }

        public string EvidenceNote()
        {
            var prefix = Definition.Title + ": ";
            return prefix + string.Join(" ", _consequences.ToArray());
        }

        public void ApplyMetricConsequences(IdeaProfile profile)
        {
            if (profile == null) return;
            profile.Metrics.Desirability = Clamp(profile.Metrics.Desirability + _desirability);
            profile.Metrics.Feasibility = Clamp(profile.Metrics.Feasibility + _feasibility);
            profile.Metrics.Viability = Clamp(profile.Metrics.Viability + _viability);
            profile.Metrics.Safety = Clamp(profile.Metrics.Safety + _safety);
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        private static double Clamp(double value) { return Math.Max(0.0, Math.Min(1.0, value)); }
    }

    public static class GameplayEncounterCatalog
    {
        public static GameplayEncounterDefinition For(string testId, IdeaProfile profile)
        {
            if (string.Equals(testId, "desire", StringComparison.OrdinalIgnoreCase)) return CustomerInterview(profile);
            if (string.Equals(testId, "commitment", StringComparison.OrdinalIgnoreCase)) return PrototypeTrial(profile);
            if (string.Equals(testId, "burden", StringComparison.OrdinalIgnoreCase)) return FeasibilityAudit(profile);
            if (string.Equals(testId, "refusal", StringComparison.OrdinalIgnoreCase)) return ChildrensJury(profile);
            throw new ArgumentOutOfRangeException("testId", testId, "Unknown Idea Zoo test.");
        }

        public static string[] TestIds { get { return new[] { "desire", "commitment", "burden", "refusal" }; } }

        private static GameplayEncounterDefinition CustomerInterview(IdeaProfile profile)
        {
            var audience = Safe(profile != null ? profile.Audience : string.Empty, "the first user");
            return new GameplayEncounterDefinition(
                "desire",
                GameplayEncounterKind.CustomerInterview,
                "CUSTOMER INTERVIEW",
                "Discover whether the problem exists before teaching anyone to praise the solution.",
                new GameplayRound(
                    "You meet someone from " + audience + ". What do you ask first?",
                    "The creature listens for whether you want truth or reassurance.",
                    Choice("interview-pitch", "Pitch the solution immediately", "They react to your confidence, not their own problem.", I(-1, -1, 1, 0, 0, .00, .00, .00, -.02, GameplayTendency.Builder)),
                    Choice("interview-today", "Ask how they solve the problem today", "They describe an expensive workaround you had not considered.", I(-2, 1, 0, 2, 3, .05, .00, .01, .01, GameplayTendency.Experimenter)),
                    Choice("interview-like", "Ask whether they like the idea", "They say yes politely, but reveal no behaviour.", I(-1, 0, 1, 1, 1, .01, .00, .00, .00, GameplayTendency.Protector))
                ),
                new GameplayRound(
                    "One interview contradicts the others. What do you do?",
                    "Contradiction can be noise, or the first useful crack in your story.",
                    Choice("interview-dismiss", "Dismiss the person as an outlier", "The story stays clean and the evidence stays weak.", I(0, -1, 1, 0, 0, -.03, .00, .00, -.01, GameplayTendency.Builder)),
                    Choice("interview-probe", "Probe the contradiction", "The contradiction exposes a second user with a sharper need.", I(-2, 1, -1, 2, 3, .04, .00, .01, .02, GameplayTendency.Skeptic)),
                    Choice("interview-narrow", "Narrow the first audience", "The market becomes smaller and the promise becomes more believable.", I(-1, 0, 0, 2, 2, .02, .01, .03, .01, GameplayTendency.Simplifier))
                ),
                new GameplayRound(
                    "What signal will you accept as real demand?",
                    "Compliments are cheap. Behaviour costs something.",
                    Choice("interview-compliment", "Collect positive reactions", "You leave with enthusiasm but little proof.", I(-1, 0, 1, 1, 1, .01, .00, .00, .00, GameplayTendency.Protector)),
                    Choice("interview-observe", "Observe the current workaround", "You see when, where and why the problem becomes urgent.", I(-2, 1, 0, 2, 2, .04, .01, .01, .01, GameplayTendency.Experimenter)),
                    Choice("interview-return", "Ask them to return for a second session", "They spend more time because the outcome matters to them.", I(-2, 1, 1, 3, 3, .05, .00, .03, .01, GameplayTendency.Builder))
                ));
        }

        private static GameplayEncounterDefinition PrototypeTrial(IdeaProfile profile)
        {
            var promise = Safe(profile != null ? profile.Promise : string.Empty, "the central promise");
            return new GameplayEncounterDefinition(
                "commitment",
                GameplayEncounterKind.PrototypeTrial,
                "PROTOTYPE TRIAL",
                "Build the smallest thing that can expose whether the promise works.",
                new GameplayRound(
                    "You cannot build everything. What does the first prototype prove?",
                    "The current promise is: " + promise,
                    Choice("prototype-everything", "Build the full product", "Time disappears into features before the central promise is tested.", I(-4, 0, 1, 0, 0, .00, -.04, -.03, .00, GameplayTendency.Builder)),
                    Choice("prototype-core", "Build only the core promise", "A rough but honest prototype exposes the main mechanism.", I(-3, 0, 1, 3, 3, .01, .05, .03, .01, GameplayTendency.Simplifier)),
                    Choice("prototype-fake", "Use a manual concierge prototype", "The experience is tested before expensive automation exists.", I(-2, 1, 0, 2, 2, .02, .03, .02, .01, GameplayTendency.Experimenter))
                ),
                new GameplayRound(
                    "Who receives the prototype first?",
                    "A convenient tester can produce a convenient lie.",
                    Choice("prototype-friends", "Friends who want you to succeed", "They protect your feelings and miss the real friction.", I(-1, 1, 1, 1, 1, .01, .00, .00, .00, GameplayTendency.Protector)),
                    Choice("prototype-first-user", "The narrow first user", "They find a failure your team had learned to work around.", I(-2, 0, 0, 3, 3, .02, .03, .03, .01, GameplayTendency.Skeptic)),
                    Choice("prototype-crowd", "A broad public audience", "Attention rises, but the signal becomes difficult to interpret.", I(-1, -1, 2, 1, 1, .02, -.01, .01, -.02, GameplayTendency.Builder))
                ),
                new GameplayRound(
                    "What counts as commitment?",
                    "The creature grows differently when someone risks something real.",
                    Choice("prototype-survey", "A high satisfaction score", "The score looks good but asks almost nothing of the tester.", I(-1, 0, 1, 1, 1, .01, .00, .01, .00, GameplayTendency.Protector)),
                    Choice("prototype-pilot", "A signed pilot or preorder", "Someone accepts cost and accountability for the outcome.", I(-2, 1, 1, 3, 3, .02, .01, .06, .01, GameplayTendency.Builder)),
                    Choice("prototype-time", "A second voluntary session", "The user spends scarce time to continue the experiment.", I(-1, 1, 0, 2, 2, .02, .00, .03, .01, GameplayTendency.Experimenter))
                ));
        }

        private static GameplayEncounterDefinition FeasibilityAudit(IdeaProfile profile)
        {
            var burden = Safe(profile != null ? profile.HiddenBurden : string.Empty, "recurring maintenance");
            return new GameplayEncounterDefinition(
                "burden",
                GameplayEncounterKind.FeasibilityAudit,
                "FEASIBILITY AUDIT",
                "Make invisible labour, dependencies and operating cost visible.",
                new GameplayRound(
                    "The idea succeeds. What work appears every week?",
                    "The current hidden burden is: " + burden,
                    Choice("audit-launch", "Count only launch work", "The launch appears affordable because recurring work remains unnamed.", I(-1, 0, 1, 0, 0, .00, -.05, -.02, -.01, GameplayTendency.Builder)),
                    Choice("audit-map", "Map every recurring task", "Support, moderation, compliance and repair become part of the design.", I(-2, 0, -1, 3, 3, .00, .06, .02, .03, GameplayTendency.Skeptic)),
                    Choice("audit-automate", "Assume automation will absorb it", "The burden is moved into exceptions that still need people.", I(-1, -1, 1, 1, 1, .00, -.02, .00, -.02, GameplayTendency.Builder))
                ),
                new GameplayRound(
                    "A critical dependency doubles in price. What changes?",
                    "The first plan no longer fits the available resources.",
                    Choice("audit-ignore", "Keep the plan and hope prices fall", "The creature becomes dependent on a future you do not control.", I(0, 0, 1, 0, 0, .00, -.05, -.04, -.01, GameplayTendency.Protector)),
                    Choice("audit-fallback", "Design a smaller fallback", "The promise narrows, but the idea can survive a dependency failure.", I(-2, 0, -1, 2, 2, .00, .05, .03, .03, GameplayTendency.Simplifier)),
                    Choice("audit-price", "Price the dependency into the model", "The business becomes less attractive and more truthful.", I(-1, 0, 0, 3, 3, .00, .04, .05, .01, GameplayTendency.Builder))
                ),
                new GameplayRound(
                    "Who owns the idea after launch?",
                    "Shared responsibility can mean nobody is responsible.",
                    Choice("audit-everyone", "Everyone shares ownership", "The work is praised collectively and missed individually.", I(-1, 0, 1, 1, 1, .00, -.02, -.01, -.01, GameplayTendency.Protector)),
                    Choice("audit-named", "Name one accountable keeper", "Maintenance receives a budget, authority and an escalation path.", I(-1, 1, 0, 3, 3, .00, .05, .03, .04, GameplayTendency.Builder)),
                    Choice("audit-expiry", "Give the system an expiry date", "The idea must earn renewal instead of becoming permanent by neglect.", I(-1, 1, -1, 2, 3, .00, .04, .01, .05, GameplayTendency.Simplifier))
                ));
        }

        private static GameplayEncounterDefinition ChildrensJury(IdeaProfile profile)
        {
            var idea = Safe(profile != null ? profile.PlainIdea : string.Empty, "the idea");
            return new GameplayEncounterDefinition(
                "refusal",
                GameplayEncounterKind.ChildrensJury,
                "CHILDREN'S JURY",
                "Explain the idea without status, jargon or institutional protection.",
                new GameplayRound(
                    "The Jury asks: what does this do to a person?",
                    "Your current description is: " + idea,
                    Choice("jury-jargon", "Explain the technology", "The mechanism becomes clearer while the human consequence stays hidden.", I(-1, -1, 1, 0, 0, .00, .00, .00, -.04, GameplayTendency.Builder)),
                    Choice("jury-simple", "Answer in one plain sentence", "The promise becomes understandable enough to challenge.", I(-1, 1, 0, 2, 3, .03, .01, .01, .03, GameplayTendency.Simplifier)),
                    Choice("jury-demo", "Show the best-case demo", "The Jury sees what works but not who pays when it fails.", I(-1, 0, 1, 1, 1, .01, .00, .01, -.01, GameplayTendency.Protector))
                ),
                new GameplayRound(
                    "A person wants to leave. What happens?",
                    "Refusal is only real when it is easy under pressure.",
                    Choice("jury-lock", "They lose access to the service", "The idea turns dependency into obedience.", I(0, -2, 1, 0, 0, .00, .00, .01, -.08, GameplayTendency.Builder)),
                    Choice("jury-hidden", "An opt-out exists in settings", "Refusal is technically possible and practically discouraged.", I(-1, -1, 0, 1, 1, .00, .00, .00, -.02, GameplayTendency.Protector)),
                    Choice("jury-exit", "They leave without punishment", "The creature loses power and gains legitimacy.", I(-2, 2, -1, 3, 3, .01, .01, -.01, .08, GameplayTendency.Protector))
                ),
                new GameplayRound(
                    "What is the cruelest plausible use?",
                    "The Jury will not accept 'people might misuse it' as an answer.",
                    Choice("jury-best", "Return to the intended use", "Intent protects the story, not the affected person.", I(0, -1, 1, 0, 0, .00, .00, .00, -.05, GameplayTendency.Builder)),
                    Choice("jury-name", "Name the actor, action and victim", "The harm becomes specific enough to design against.", I(-2, 1, -1, 3, 3, .00, .01, -.01, .08, GameplayTendency.Skeptic)),
                    Choice("jury-board", "Ask the Board to decide later", "Responsibility leaves the room while the capability remains.", I(-1, -1, 1, 1, 1, .00, .00, .00, -.03, GameplayTendency.Protector))
                ));
        }

        private static GameplayChoice Choice(string id, string label, string consequence, GameplayImpact impact)
        {
            return new GameplayChoice(id, label, consequence, impact);
        }

        private static GameplayImpact I(int time, int trust, int momentum, int evidence, int score, double desire, double feasible, double viable, double safety, GameplayTendency tendency)
        {
            return new GameplayImpact
            {
                Time = time,
                Trust = trust,
                Momentum = momentum,
                Evidence = evidence,
                TestScore = score,
                Desirability = desire,
                Feasibility = feasible,
                Viability = viable,
                Safety = safety,
                Tendency = tendency
            };
        }

        private static string Safe(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public static class GameplayDisruptionCatalog
    {
        public static GameplayDisruptionDefinition For(IdeaProfile profile, int ordinal)
        {
            var index = Mathf.Abs(StableHash((profile != null ? profile.RecordId : "idea") + "|" + ordinal)) % 5;
            if (index == 0) return Competitor();
            if (index == 1) return CostShock();
            if (index == 2) return AudiencePivot();
            if (index == 3) return PrivacyConcern();
            return PrematureAttention();
        }

        public static int StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                var text = value ?? string.Empty;
                for (var i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619;
                }
                return (int)(hash & 0x7fffffff);
            }
        }

        private static GameplayDisruptionDefinition Competitor()
        {
            return new GameplayDisruptionDefinition(
                GameplayDisruptionKind.CompetitorLaunch,
                "A COMPETITOR LAUNCHED FIRST",
                "Their product looks more complete. The creature wants to sprint after it.",
                C("competitor-copy", "Copy their visible features", "Momentum rises, but your idea loses its own reason to exist.", X(-2, 0, 2, 0, .00, -.03, -.02, -.01, GameplayTendency.Builder)),
                C("competitor-difference", "Test the difference users still care about", "The idea becomes narrower and more defensible.", X(-2, 0, -1, 2, .03, .02, .04, .01, GameplayTendency.Skeptic)),
                C("competitor-pause", "Pause and reconsider the market", "You lose speed and avoid building a duplicate by reflex.", X(-1, 0, -2, 1, .01, .01, .01, .02, GameplayTendency.Protector)));
        }

        private static GameplayDisruptionDefinition CostShock()
        {
            return new GameplayDisruptionDefinition(
                GameplayDisruptionKind.CostShock,
                "THE PROTOTYPE COST DOUBLED",
                "A dependency changed price and the first plan no longer fits.",
                C("cost-raise", "Raise the price immediately", "Viability improves while demand becomes less certain.", X(-1, -1, 0, 1, -.02, .00, .04, .00, GameplayTendency.Builder)),
                C("cost-cut", "Remove everything outside the core promise", "The creature becomes smaller, faster and easier to judge.", X(-1, 0, -1, 2, .00, .04, .03, .02, GameplayTendency.Simplifier)),
                C("cost-subsidise", "Absorb the cost for now", "Momentum survives by creating a burden that returns later.", X(0, 0, 2, 0, .01, -.04, -.03, -.01, GameplayTendency.Protector)));
        }

        private static GameplayDisruptionDefinition AudiencePivot()
        {
            return new GameplayDisruptionDefinition(
                GameplayDisruptionKind.AudiencePivot,
                "THE WRONG PEOPLE WANT IT",
                "The intended audience is lukewarm. Another group is asking to use it now.",
                C("pivot-follow", "Follow the unexpected audience", "Demand strengthens, but the original problem may disappear.", X(-2, 0, 1, 2, .05, .00, .02, -.01, GameplayTendency.Experimenter)),
                C("pivot-original", "Protect the original audience", "The mission survives while urgency falls.", X(-1, 1, -1, 1, .00, .00, -.01, .02, GameplayTendency.Protector)),
                C("pivot-split", "Run one small comparison test", "The decision waits for behaviour instead of instinct.", X(-2, 0, -1, 3, .03, .01, .02, .02, GameplayTendency.Skeptic)));
        }

        private static GameplayDisruptionDefinition PrivacyConcern()
        {
            return new GameplayDisruptionDefinition(
                GameplayDisruptionKind.PrivacyConcern,
                "A TESTER FOUND A PRIVACY FAILURE",
                "The feature works by collecting more than the user understood.",
                C("privacy-hide", "Call it an edge case", "The schedule survives and trust collapses.", X(0, -3, 2, 0, .00, .00, .01, -.08, GameplayTendency.Builder)),
                C("privacy-stop", "Stop the test and redesign consent", "Momentum falls while legitimacy improves.", X(-2, 2, -2, 2, .00, -.01, -.01, .08, GameplayTendency.Protector)),
                C("privacy-minimise", "Remove the unnecessary data", "The system becomes less powerful and safer by design.", X(-2, 1, -1, 3, .00, .03, -.01, .07, GameplayTendency.Simplifier)));
        }

        private static GameplayDisruptionDefinition PrematureAttention()
        {
            return new GameplayDisruptionDefinition(
                GameplayDisruptionKind.PrematureAttention,
                "PUBLIC ATTENTION ARRIVED EARLY",
                "A clip of the prototype is spreading before the idea is ready.",
                C("attention-launch", "Launch while attention is high", "Momentum surges and unresolved risks leave the Zoo with it.", X(-2, -2, 3, 1, .03, -.01, .04, -.05, GameplayTendency.Builder)),
                C("attention-context", "Publish the limits and continue testing", "The story becomes less exciting and more trustworthy.", X(-1, 2, -1, 2, .01, .00, .01, .04, GameplayTendency.Skeptic)),
                C("attention-close", "Close access until the core is ready", "You sacrifice momentum to protect the experiment.", X(-1, 1, -3, 1, -.01, .02, -.01, .03, GameplayTendency.Protector)));
        }

        private static GameplayChoice C(string id, string label, string consequence, GameplayImpact impact)
        {
            return new GameplayChoice(id, label, consequence, impact);
        }

        private static GameplayImpact X(int time, int trust, int momentum, int evidence, double desire, double feasible, double viable, double safety, GameplayTendency tendency)
        {
            return new GameplayImpact
            {
                Time = time,
                Trust = trust,
                Momentum = momentum,
                Evidence = evidence,
                TestScore = 0,
                Desirability = desire,
                Feasibility = feasible,
                Viability = viable,
                Safety = safety,
                Tendency = tendency
            };
        }
    }
}
