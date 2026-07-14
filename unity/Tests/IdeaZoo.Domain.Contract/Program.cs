using IdeaZoo.Core;

static class Contract
{
    private static readonly List<string> Failures = new();

    public static int Main()
    {
        TestStrictAnalysis();
        TestCompleteCase();
        TestInvalidTransitions();
        TestIdeaIntelligence();

        if (Failures.Count == 0)
        {
            Console.WriteLine("IDEA_ZOO_DOMAIN_CONTRACT_PASS");
            return 0;
        }

        Console.Error.WriteLine("IDEA_ZOO_DOMAIN_CONTRACT_FAIL");
        foreach (var failure in Failures) Console.Error.WriteLine("- " + failure);
        return 1;
    }

    private static void TestStrictAnalysis()
    {
        var maintenance = new IdeaIntake
        {
            Title = "Maintenance Ledger",
            Idea = "A maintenance workflow for repair crews.",
            Problem = "Repairs disappear between shifts.",
            Promise = "Every unresolved repair remains visible.",
            Audience = "municipal repair crews",
            Payer = "city operations office",
            Evidence = "unpaid conversations with repair crews",
            Dependency = "existing work orders",
            Maintenance = "a maintenance coordinator",
            Harm = "managers could use it to blame workers"
        };

        var profile = IdeaAnalyzer.Analyze(maintenance);
        Check(profile.Class == IdeaClass.Burrower, "maintenance workflow was not classified as a Burrower");
        Check(profile.Appetite != Appetite.Data, "the letters 'ai' inside maintenance triggered a Data appetite");
        Check(profile.Metrics.Viability < 0.60, "the word unpaid was treated as paid evidence");

        var translation = IdeaAnalyzer.Analyze(MeetingBridge());
        Check(translation.Class == IdeaClass.Hand, "translation hardware was not classified as a Hand");
        Check(translation.Appetite == Appetite.Data, "AI translation did not expose its Data appetite");
    }

    private static void TestCompleteCase()
    {
        var director = new IdeaZooCaseDirector();
        var profile = director.Begin(MeetingBridge());
        Check(director.Stage == CaseStage.Hatching, "case did not enter Hatching after intake");
        director.EnterZoo();
        Check(director.Stage == CaseStage.Testing, "case did not enter Testing after hatching");

        var evidenceBeforeNoSignal = profile.Metrics.Evidence;
        Check(director.RecordEvidence("desire", 0, "No users described the problem yet."), "valid zero-strength evidence was rejected");
        Check(Math.Abs(profile.Metrics.Evidence - evidenceBeforeNoSignal) < 0.0001, "zero-strength evidence increased the evidence score");
        Check(!director.RecordEvidence("desire", 3, "duplicate"), "duplicate evidence was accepted");
        Check(!director.RecordEvidence("unknown", 3, "invalid habitat"), "unknown evidence habitat was accepted");

        Check(director.RecordEvidence("commitment", 3, "Two companies signed paid pilots."), "commitment evidence was rejected");
        Check(director.RecordEvidence("burden", 2, "Support, language QA and repair were priced."), "burden evidence was rejected");
        Check(director.RecordEvidence("refusal", 2, "Participants can pause translation and delete sessions."), "refusal evidence was rejected");
        Check(director.CompletedTests.Count == 4, "four distinct habitats did not complete");

        var evidenceBeforeMolt = profile.Metrics.Evidence;
        director.EnterMolt();
        director.CancelMolt();
        Check(director.Stage == CaseStage.Testing, "cancelling a Molt did not return to Testing");
        director.EnterMolt();
        Expect<InvalidOperationException>(() => director.ApplyMolt(profile.Promise, profile.Audience, Array.Empty<string>()), "unchanged idea was accepted as a Molt");

        director.ApplyMolt(
            "Participants hear translation that preserves uncertainty and can be paused at any time.",
            "cross-border sales teams in high-stakes meetings",
            new[] { "People can refuse without penalty", "A named keeper owns maintenance", "Uncertainty remains visible" });

        Check(director.Stage == CaseStage.Decision, "valid Molt did not unlock Decision");
        Check(Math.Abs(profile.Metrics.Evidence - evidenceBeforeMolt) < 0.0001, "Molt reset accumulated evidence");
        Check(profile.Guardrails.Count == 3, "Molt guardrails were not preserved");

        director.IssueRuling(Ruling.Build);
        Check(director.Stage == CaseStage.Complete, "ruling did not complete the case");
        Check(profile.FinalRuling == Ruling.Build, "Build ruling was not recorded");
        Check(profile.NextActions.Count == 3, "ruling did not produce three real-world actions");
        Check(!string.IsNullOrWhiteSpace(profile.VerdictReason), "ruling did not produce a reason");
        Expect<InvalidOperationException>(() => director.IssueRuling(Ruling.Break), "a second ruling changed a completed case");
    }

    private static void TestInvalidTransitions()
    {
        var director = new IdeaZooCaseDirector();
        Expect<InvalidOperationException>(() => director.EnterZoo(), "testing began without an intake");
        Expect<ArgumentException>(() => director.Begin(new IdeaIntake()), "empty intake was accepted");

        director.Begin(new IdeaIntake
        {
            Title = "Small Tool",
            Idea = "A tool that helps a team record handovers.",
            Promise = "Every shift receives one accurate handover note.",
            Audience = "night shift supervisors"
        });
        director.EnterZoo();
        Expect<InvalidOperationException>(() => director.EnterMolt(), "Molt opened before all habitats completed");
        Expect<ArgumentOutOfRangeException>(() => director.RecordEvidence("desire", 4, "too strong"), "out-of-range evidence strength was accepted");
    }

    private static void TestIdeaIntelligence()
    {
        var profile = IdeaAnalyzer.Analyze(MeetingBridge());
        profile.RecordId = "contract-intelligence-record";
        var artifacts = new List<EvidenceArtifact>
        {
            new()
            {
                ArtifactId = "interview-1",
                Kind = EvidenceArtifactKind.Interview,
                Title = "Founder interviews",
                Summary = "Six exporters described losing detail and trust across languages.",
                ContentHash = IdeaIntelligenceHash.Sha256("six interviews"),
                IndependentlyVerified = false,
                RecordedAtUtc = DateTime.UtcNow
            },
            new()
            {
                ArtifactId = "commitment-1",
                Kind = EvidenceArtifactKind.Commitment,
                Title = "Paid pilot",
                Summary = "A logistics company signed a paid pilot.",
                ContentHash = IdeaIntelligenceHash.Sha256("paid pilot"),
                IndependentlyVerified = true,
                RecordedAtUtc = DateTime.UtcNow
            }
        };
        var versions = new List<IdeaVersionSnapshot>
        {
            new() { VersionId = "v1", Promise = profile.Promise, Audience = profile.Audience, CreatedAtUtc = DateTime.UtcNow },
            new() { VersionId = "v2", Promise = "Preserve uncertainty while translating meetings.", Audience = "cross-border sales teams", Guardrails = new List<string> { "Visible uncertainty", "Session deletion" }, CreatedAtUtc = DateTime.UtcNow }
        };

        IIdeaIntelligenceProvider provider = new LocalIdeaIntelligenceProvider();
        var report = provider.Analyze(profile, artifacts, versions);
        Check(provider.ProviderName == "Private Local Reasoner", "local intelligence provider identity changed");
        Check(!string.IsNullOrWhiteSpace(report.PlainThesis), "intelligence report produced no thesis");
        Check(report.Challenges.Count >= 6, "intelligence report produced too few assumption challenges");
        Check(report.Stakeholders.Count >= 6, "intelligence report produced too few stakeholder simulations");
        Check(report.Experiments.Count >= 5, "intelligence report produced too few experiments");
        Check(report.Evidence.Count == artifacts.Count, "intelligence report did not interpret every artifact");
        Check(report.Experiments.First().InformationValue >= report.Experiments.Last().InformationValue, "experiments are not ordered by information value");
        Check(report.Confidence >= 0.18 && report.Confidence <= 0.92, "intelligence confidence escaped its honest bounds");
        Check(report.Recommendation.Contains("player", StringComparison.OrdinalIgnoreCase), "intelligence layer did not preserve the player's authority");
        Check(IdeaIntelligenceHash.Sha256("same") == IdeaIntelligenceHash.Sha256("same"), "evidence hashing is unstable");
        Check(IdeaIntelligenceHash.Sha256("same") != IdeaIntelligenceHash.Sha256("different"), "evidence hashing does not distinguish content");
    }

    private static IdeaIntake MeetingBridge()
    {
        return new IdeaIntake
        {
            Title = "Meeting Bridge",
            Idea = "A headset using AI models to translate business meetings in real time.",
            Problem = "People lose trust and detail across languages.",
            Promise = "Two people complete a ten minute meeting without managing a phone.",
            Audience = "cross-border sales teams",
            Payer = "export companies",
            Evidence = "interviewed six founders and tested a prototype",
            Dependency = "speech models and microphones",
            Maintenance = "language quality and hardware support",
            Harm = "translation could hide uncertainty or change consent"
        };
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) Failures.Add(message);
    }

    private static void Expect<T>(Action action, string message) where T : Exception
    {
        try
        {
            action();
            Failures.Add(message);
        }
        catch (T)
        {
        }
        catch (Exception exception)
        {
            Failures.Add(message + " (wrong exception: " + exception.GetType().Name + ")");
        }
    }
}
