using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IdeaZoo.Core
{
    public enum EvidenceArtifactKind { Interview, Commitment, Prototype, Metric, Document, Link, Cost, Observation }
    public enum IntelligenceSeverity { Question, Risk, Critical }
    public enum ExperimentCost { Minutes, Hours, Days, Weeks }

    [Serializable]
    public sealed class EvidenceArtifact
    {
        public string ArtifactId = string.Empty;
        public EvidenceArtifactKind Kind;
        public string Title = string.Empty;
        public string Summary = string.Empty;
        public string Source = string.Empty;
        public string ContentHash = string.Empty;
        public bool IndependentlyVerified;
        public DateTime RecordedAtUtc;
    }

    [Serializable]
    public sealed class AssumptionChallenge
    {
        public string Assumption = string.Empty;
        public string FailureSignal = string.Empty;
        public string CounterArgument = string.Empty;
        public IntelligenceSeverity Severity;
        public double Confidence;
    }

    [Serializable]
    public sealed class StakeholderSimulation
    {
        public string Stakeholder = string.Empty;
        public string DesiredOutcome = string.Empty;
        public string LikelyObjection = string.Empty;
        public string RefusalCost = string.Empty;
        public string HiddenWork = string.Empty;
    }

    [Serializable]
    public sealed class ExperimentRecommendation
    {
        public string ExperimentId = string.Empty;
        public string Title = string.Empty;
        public string Method = string.Empty;
        public string PassCondition = string.Empty;
        public string FailureMeaning = string.Empty;
        public ExperimentCost Cost;
        public double InformationValue;
        public string TestsAssumption = string.Empty;
    }

    [Serializable]
    public sealed class EvidenceInterpretation
    {
        public string ArtifactId = string.Empty;
        public string Supports = string.Empty;
        public string Contradicts = string.Empty;
        public string Missing = string.Empty;
        public double Reliability;
    }

    [Serializable]
    public sealed class IdeaVersionSnapshot
    {
        public string VersionId = string.Empty;
        public string Promise = string.Empty;
        public string Audience = string.Empty;
        public List<string> Guardrails = new List<string>();
        public string ChangeReason = string.Empty;
        public DateTime CreatedAtUtc;
    }

    [Serializable]
    public sealed class IdeaIntelligenceReport
    {
        public string ReportId = string.Empty;
        public string RecordId = string.Empty;
        public string PlainThesis = string.Empty;
        public string BuyerUserGap = string.Empty;
        public string StrongestCaseFor = string.Empty;
        public string StrongestCaseAgainst = string.Empty;
        public string CruelestPlausibleUse = string.Empty;
        public string Recommendation = string.Empty;
        public Ruling SuggestedRuling;
        public double Confidence;
        public List<AssumptionChallenge> Challenges = new List<AssumptionChallenge>();
        public List<StakeholderSimulation> Stakeholders = new List<StakeholderSimulation>();
        public List<ExperimentRecommendation> Experiments = new List<ExperimentRecommendation>();
        public List<EvidenceInterpretation> Evidence = new List<EvidenceInterpretation>();
        public List<string> Uncertainties = new List<string>();
        public List<string> Counterfactuals = new List<string>();
        public DateTime GeneratedAtUtc;
    }

    public interface IIdeaIntelligenceProvider
    {
        string ProviderName { get; }
        IdeaIntelligenceReport Analyze(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts, IReadOnlyList<IdeaVersionSnapshot> versions);
    }

    public static class IdeaIntelligenceHash
    {
        public static string Sha256(string content)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }
    }

    public sealed class LocalIdeaIntelligenceProvider : IIdeaIntelligenceProvider
    {
        public string ProviderName { get { return "Private Local Reasoner"; } }

        public IdeaIntelligenceReport Analyze(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts, IReadOnlyList<IdeaVersionSnapshot> versions)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            artifacts = artifacts ?? Array.Empty<EvidenceArtifact>();
            versions = versions ?? Array.Empty<IdeaVersionSnapshot>();
            var report = new IdeaIntelligenceReport
            {
                ReportId = "report-" + Guid.NewGuid().ToString("N"),
                RecordId = profile.RecordId,
                PlainThesis = BuildThesis(profile),
                BuyerUserGap = BuildBuyerUserGap(profile),
                StrongestCaseFor = BuildCaseFor(profile, artifacts),
                StrongestCaseAgainst = BuildCaseAgainst(profile, artifacts),
                CruelestPlausibleUse = string.IsNullOrWhiteSpace(profile.Harm) ? "The idea becomes difficult to refuse while its operator hides uncertainty and maintenance costs." : profile.Harm,
                GeneratedAtUtc = DateTime.UtcNow
            };
            report.Challenges.AddRange(BuildChallenges(profile, artifacts));
            report.Stakeholders.AddRange(BuildStakeholders(profile));
            report.Experiments.AddRange(BuildExperiments(profile, report.Challenges));
            report.Evidence.AddRange(InterpretEvidence(profile, artifacts));
            report.Uncertainties.AddRange(BuildUncertainties(profile, artifacts, versions));
            report.Counterfactuals.AddRange(BuildCounterfactuals(profile));
            report.SuggestedRuling = SuggestRuling(profile, artifacts, report.Challenges);
            report.Confidence = CalculateConfidence(profile, artifacts, report.Challenges);
            report.Recommendation = BuildRecommendation(report.SuggestedRuling, report.Experiments.FirstOrDefault(), report.Confidence);
            return report;
        }

        private static string BuildThesis(IdeaProfile profile)
        {
            return "For " + NonEmpty(profile.Audience, "a specific first user") + ", provide " + NonEmpty(profile.Promise, profile.PlainIdea) + " while depending on " + NonEmpty(profile.Dependency, "unproven operational assumptions") + ".";
        }

        private static string BuildBuyerUserGap(IdeaProfile profile)
        {
            var payer = NonEmpty(profile.Payer, "not yet named");
            var audience = NonEmpty(profile.Audience, "not yet named");
            if (string.Equals(payer, audience, StringComparison.OrdinalIgnoreCase)) return "The buyer and first user appear aligned, but willingness to pay still requires behavioural evidence.";
            return "The first user is " + audience + ", while the payer is " + payer + ". Their incentives may diverge, so adoption cannot be inferred from purchase interest.";
        }

        private static string BuildCaseFor(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts)
        {
            var verified = artifacts.Count(item => item.IndependentlyVerified);
            var commitments = artifacts.Count(item => item.Kind == EvidenceArtifactKind.Commitment || item.Kind == EvidenceArtifactKind.Cost);
            if (verified + commitments > 0) return "The idea has evidence outside the inventor's description: " + verified + " independently verified artifacts and " + commitments + " costly commitments.";
            return "The problem and promise form a testable proposition, and the idea can still earn confidence through small real-world experiments before major investment.";
        }

        private static string BuildCaseAgainst(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts)
        {
            if (artifacts.Count == 0) return "The idea currently survives mainly through explanation. No external artifact yet proves desire, commitment, feasibility or safe refusal.";
            var weak = artifacts.Count(item => !item.IndependentlyVerified && item.Kind != EvidenceArtifactKind.Commitment);
            return weak > artifacts.Count / 2 ? "Most current evidence is self-reported or reversible. Interest may disappear when money, access, reputation or operational work is required." : "The largest remaining risk is whether the strongest result survives scale, hostile ownership and long-term maintenance.";
        }

        private static IEnumerable<AssumptionChallenge> BuildChallenges(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts)
        {
            var challenges = new List<AssumptionChallenge>();
            Add(challenges, "The problem is frequent and painful enough to change behaviour.", "Likely users describe workarounds but refuse any costly commitment.", "People may agree that the problem exists while preferring the current inconvenience to adopting a new system.", artifacts.Any(item => item.Kind == EvidenceArtifactKind.Interview) ? IntelligenceSeverity.Risk : IntelligenceSeverity.Critical, artifacts.Any(item => item.Kind == EvidenceArtifactKind.Interview) ? 0.72 : 0.88);
            Add(challenges, "The promised outcome can be measured without relying on vanity metrics.", "Success is described through activity, engagement or model output rather than the user's outcome.", "The product may optimize what is easiest to count instead of what matters.", IntelligenceSeverity.Risk, 0.76);
            Add(challenges, "The named payer receives enough value to fund the recurring burden.", "Users praise the idea while procurement, budget owners or maintainers decline responsibility.", "A useful experience can still be an unviable business or an unfunded public service.", string.IsNullOrWhiteSpace(profile.Payer) ? IntelligenceSeverity.Critical : IntelligenceSeverity.Risk, 0.81);
            Add(challenges, "The system remains useful when its highest-cost dependency becomes slower or more expensive.", "Unit economics fail after realistic usage, support and exception handling are included.", "The prototype may hide labour, compute, compliance or trust costs that production cannot avoid.", string.IsNullOrWhiteSpace(profile.Dependency) ? IntelligenceSeverity.Critical : IntelligenceSeverity.Risk, 0.79);
            Add(challenges, "Affected people can refuse, appeal or leave without disproportionate loss.", "The opt-out exists in policy but causes exclusion, lost income, lower service quality or social punishment.", "Consent is not meaningful when participation is structurally required.", profile.Metrics.Safety < 0.55 ? IntelligenceSeverity.Critical : IntelligenceSeverity.Risk, 0.84);
            Add(challenges, "The idea's most profitable operator will preserve the inventor's intended boundaries.", "Revenue improves when uncertainty, privacy costs or coercive defaults are hidden.", "Later owners may discover a business model that converts a Hand into Teeth.", IntelligenceSeverity.Critical, 0.83);
            foreach (var assumption in profile.Assumptions.Where(value => !string.IsNullOrWhiteSpace(value)))
                Add(challenges, assumption, "A direct test contradicts the assumption under real constraints.", "This assumption is currently carried by the inventor rather than external evidence.", IntelligenceSeverity.Question, 0.62);
            return challenges.GroupBy(item => item.Assumption, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).Take(10);
        }

        private static IEnumerable<StakeholderSimulation> BuildStakeholders(IdeaProfile profile)
        {
            return new[]
            {
                Stakeholder("First user", NonEmpty(profile.Promise, "a better outcome"), "This adds another tool, habit or risk before proving it replaces current work.", "Time, switching cost or lost access.", NonEmpty(profile.Maintenance, "Training, support and exception handling.")),
                Stakeholder("Payer", "A measurable return or avoided loss.", "The value may be real but too diffuse to own in one budget.", "Contract lock-in or sunk implementation cost.", "Procurement, security review and internal adoption."),
                Stakeholder("Maintainer", "A system whose recurring burden is funded and observable.", "Edge cases will arrive faster than staffing or documentation.", "On-call responsibility and reputational blame.", NonEmpty(profile.Maintenance, "Monitoring, correction and user support.")),
                Stakeholder("Reluctant participant", "A meaningful way to refuse or appeal.", "The system may become mandatory through employment, market access or social expectation.", "Exclusion, lower service quality or punishment.", "Documenting harm and navigating appeals."),
                Stakeholder("Powerful later owner", "More control, revenue or data from the same infrastructure.", "Guardrails reduce monetization or enforcement value.", "Low; the owner controls defaults and terms.", "Policy, lobbying and narrative management."),
                Stakeholder("Regulator or community", "Benefits without hidden public costs.", "The idea may externalize maintenance, discrimination or failure recovery.", "Collective rather than individual.", "Auditing, enforcement and remediation.")
            };
        }

        private static IEnumerable<ExperimentRecommendation> BuildExperiments(IdeaProfile profile, IReadOnlyList<AssumptionChallenge> challenges)
        {
            var experiments = new List<ExperimentRecommendation>();
            AddExperiment(experiments, "problem-interviews", "Unprompted problem interviews", "Interview five qualified users without describing the solution first. Record current behaviour, frequency, cost and failed workarounds.", "At least three independently describe the same painful pattern and one recent attempt to solve it.", "The problem may be interesting but not behaviour-changing.", ExperimentCost.Hours, 0.92, challenges.ElementAtOrDefault(0));
            AddExperiment(experiments, "costly-commitment", "Ask for a costly commitment", "Request money, a signed pilot, data access, an introduction, a preorder or one hour of implementation time.", "Three qualified prospects accept a commitment proportional to the promised value.", "Praise is not converting into priority.", ExperimentCost.Days, 0.96, challenges.ElementAtOrDefault(2));
            AddExperiment(experiments, "concierge-core", "Run the smallest disconfirming prototype", "Deliver the core outcome manually or with a narrow prototype while measuring every hidden task and exception.", "The outcome is repeatable and the recurring burden stays below the value created.", "Automation may be hiding an uneconomic service operation.", ExperimentCost.Days, 0.90, challenges.ElementAtOrDefault(3));
            AddExperiment(experiments, "refusal-drill", "Run a refusal and appeal drill", "Ask an affected participant to leave, delete data, reverse a decision and appeal an error. Time every step and record penalties.", "The participant exits or appeals without losing unrelated rights, income or service quality.", "The idea relies on coerced participation.", ExperimentCost.Hours, 0.88, challenges.ElementAtOrDefault(4));
            AddExperiment(experiments, "hostile-owner", "Simulate the most profitable hostile owner", "Rewrite defaults, incentives and messaging as an operator maximizing revenue or control. Identify which guardrails fail first.", "The useful core remains viable after enforceable boundaries are added.", "The business model may depend on converting hidden harm into margin.", ExperimentCost.Hours, 0.86, challenges.ElementAtOrDefault(5));
            return experiments.OrderByDescending(item => item.InformationValue);
        }

        private static IEnumerable<EvidenceInterpretation> InterpretEvidence(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts)
        {
            foreach (var artifact in artifacts)
            {
                var reliable = artifact.IndependentlyVerified ? 0.85 : artifact.Kind == EvidenceArtifactKind.Commitment ? 0.78 : artifact.Kind == EvidenceArtifactKind.Metric ? 0.68 : 0.48;
                yield return new EvidenceInterpretation
                {
                    ArtifactId = artifact.ArtifactId,
                    Supports = artifact.Kind == EvidenceArtifactKind.Commitment ? "Priority and willingness to incur cost." : artifact.Kind == EvidenceArtifactKind.Prototype ? "Technical or operational feasibility of a narrow outcome." : "A claim about the problem or result.",
                    Contradicts = artifact.Summary.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 || artifact.Summary.IndexOf("declined", StringComparison.OrdinalIgnoreCase) >= 0 ? "The current promise or audience may be wrong." : "No direct contradiction was encoded; absence of contradiction is not confirmation.",
                    Missing = artifact.Kind == EvidenceArtifactKind.Interview ? "A costly behavioural commitment." : artifact.Kind == EvidenceArtifactKind.Metric ? "A causal explanation and comparison baseline." : "Independent replication under a different operator.",
                    Reliability = reliable
                };
            }
        }

        private static IEnumerable<string> BuildUncertainties(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts, IReadOnlyList<IdeaVersionSnapshot> versions)
        {
            if (artifacts.Count == 0) yield return "No imported real-world evidence has been interpreted.";
            if (!artifacts.Any(item => item.Kind == EvidenceArtifactKind.Commitment)) yield return "No one has made a costly commitment.";
            if (!artifacts.Any(item => item.IndependentlyVerified)) yield return "No artifact has independent verification.";
            if (string.IsNullOrWhiteSpace(profile.Payer)) yield return "The economic buyer is not named.";
            if (string.IsNullOrWhiteSpace(profile.Maintenance)) yield return "The recurring keeper and maintenance burden are not owned.";
            if (versions.Count < 2) yield return "The idea has not yet demonstrated learning through a meaningful revision.";
        }

        private static IEnumerable<string> BuildCounterfactuals(IdeaProfile profile)
        {
            yield return "If the most expensive dependency doubles in price, which part of the promise survives?";
            yield return "If a competitor copies the feature but already owns distribution, what remains defensible?";
            yield return "If the first user cannot be the payer, who funds the recurring burden and why?";
            yield return "If the operator profits from making refusal difficult, which boundary remains enforceable?";
            yield return "If the idea succeeds ten times faster than expected, which human or civic system breaks first?";
            if (profile.Class == IdeaClass.Swarm || profile.Class == IdeaClass.Weather) yield return "If public criticism repeats the idea more effectively than promotion, does containment reproduce it?";
        }

        private static Ruling SuggestRuling(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts, IReadOnlyList<AssumptionChallenge> challenges)
        {
            var commitment = artifacts.Any(item => item.Kind == EvidenceArtifactKind.Commitment);
            var verified = artifacts.Any(item => item.IndependentlyVerified);
            var critical = challenges.Count(item => item.Severity == IntelligenceSeverity.Critical);
            if (profile.Metrics.Safety < 0.30 && critical >= 3) return Ruling.Break;
            if (profile.Metrics.Evidence < 0.25 && artifacts.Count == 0) return Ruling.Hibernate;
            if (profile.Metrics.Safety < 0.52 || critical >= 3) return Ruling.Molt;
            if (commitment && verified && profile.Metrics.Viability >= 0.50) return Ruling.Build;
            if (profile.Class == IdeaClass.Fleck && profile.Metrics.Viability < 0.45) return Ruling.Sanctuary;
            return Ruling.Molt;
        }

        private static double CalculateConfidence(IdeaProfile profile, IReadOnlyList<EvidenceArtifact> artifacts, IReadOnlyList<AssumptionChallenge> challenges)
        {
            var artifactScore = Math.Min(0.35, artifacts.Count * 0.045 + artifacts.Count(item => item.IndependentlyVerified) * 0.08);
            var metricScore = (profile.Metrics.Evidence + profile.Metrics.Desirability + profile.Metrics.Feasibility + profile.Metrics.Viability + profile.Metrics.Safety) / 5.0 * 0.45;
            var uncertaintyPenalty = challenges.Count(item => item.Severity == IntelligenceSeverity.Critical) * 0.035;
            return Math.Max(0.18, Math.Min(0.92, 0.18 + artifactScore + metricScore - uncertaintyPenalty));
        }

        private static string BuildRecommendation(Ruling ruling, ExperimentRecommendation experiment, double confidence)
        {
            var action = ruling == Ruling.Build ? "Proceed only with a bounded build" : ruling == Ruling.Molt ? "Revise the idea before increasing investment" : ruling == Ruling.Hibernate ? "Preserve the idea but stop active investment" : ruling == Ruling.Sanctuary ? "Keep it valuable without forcing business scale" : "End the current form and preserve only validated fragments";
            var next = experiment != null ? " Next, run: " + experiment.Title + "." : string.Empty;
            return action + ". Confidence " + Math.Round(confidence * 100) + "%." + next + " The player, not the intelligence layer, issues the ruling.";
        }

        private static string NonEmpty(string value, string fallback) { return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim(); }
        private static StakeholderSimulation Stakeholder(string stakeholder, string desired, string objection, string refusal, string work) { return new StakeholderSimulation { Stakeholder = stakeholder, DesiredOutcome = desired, LikelyObjection = objection, RefusalCost = refusal, HiddenWork = work }; }
        private static void Add(List<AssumptionChallenge> list, string assumption, string failure, string counter, IntelligenceSeverity severity, double confidence) { list.Add(new AssumptionChallenge { Assumption = assumption, FailureSignal = failure, CounterArgument = counter, Severity = severity, Confidence = confidence }); }
        private static void AddExperiment(List<ExperimentRecommendation> list, string id, string title, string method, string pass, string failure, ExperimentCost cost, double value, AssumptionChallenge challenge) { list.Add(new ExperimentRecommendation { ExperimentId = id, Title = title, Method = method, PassCondition = pass, FailureMeaning = failure, Cost = cost, InformationValue = value, TestsAssumption = challenge != null ? challenge.Assumption : string.Empty }); }
    }
}
