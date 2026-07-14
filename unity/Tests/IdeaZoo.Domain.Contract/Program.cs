using IdeaZoo.Core;

static class Contract
{
    private static readonly List<string> Failures = new();

    private static void Check(bool condition, string message)
    {
        if (!condition) Failures.Add(message);
    }

    public static int Main()
    {
        TestCompleteCase();
        TestBoundaryWords();
        TestStateProtection();

        if (Failures.Count == 0)
        {
            Console.WriteLine("IDEA_ZOO_DOMAIN_CONTRACT_PASS");
            return 0;
        }

        foreach (var failure in Failures) Console.Error.WriteLine("FAIL: " + failure);
        return 1;
    }

    private static void TestCompleteCase()
    {
        var director = new IdeaZooCaseDirector();
        var profile = director.Begin(MeetingBridge());
        Check(profile.Title == "Meeting Bridge", "intake title was not preserved");
        Check(profile.Class == IdeaClass.Hand, "translation hardware did not classify as Hand");
        Check(profile.Appetite == Appetite.Data || profile.Appetite == Appetite.Trust, "translation idea received an implausible appetite");
        var initialEvidence = profile.Metrics.Evidence;

        director.EnterZoo();
        Check(director.RecordEvidence("desire", 2, "Five founders described the problem."), "desire evidence was rejected");
        Check(!director.RecordEvidence("desire", 3, "duplicate"), "duplicate habitat evidence was accepted");
        Check(director.RecordEvidence("commitment", 3, "Two companies signed paid pilots."), "commitment evidence was rejected");
        Check(director.RecordEvidence("burden", 2, "Language QA and repair were priced."), "burden evidence was rejected");
        Check(director.RecordEvidence("refusal", 2, "Participants can pause and delete sessions."), "refusal evidence was rejected");
        Check(profile.Metrics.Evidence > initialEvidence, "evidence did not increase after positive tests");

        director.EnterMolt();
        var beforeMolt = profile.Metrics.Evidence;
        director.ApplyMolt(
            "Participants hear translation that preserves uncertainty and can be paused.",
            "cross-border sales teams running high-stakes meetings",
            new[] { "People can refuse without penalty", "A named keeper owns maintenance", "Uncertainty remains visible" });
        Check(Math.Abs(profile.Metrics.Evidence - beforeMolt) < 0.0001, "Molt reset accumulated evidence");
        Check(profile.Guardrails.Count == 3, "Molt guardrails were not attached");
        Check(director.Stage == CaseStage.Decision, "Molt did not unlock decision stage");

        director.IssueRuling(Ruling.Build);
        Check(profile.FinalRuling == Ruling.Build, "ruling was not recorded");
        Check(profile.NextActions.Count == 3, "real-world next actions were not generated");
        Check(director.Stage == CaseStage.Complete, "case did not complete");
    }

    private static void TestBoundaryWords()
    {
        var profile = IdeaAnalyzer.Analyze(new IdeaIntake
        {
            Title = "Maintenance Ledger",
            Idea = "A maintenance workflow for municipal repair crews.",
            Problem = "Repairs disappear between shifts.",
            Promise = "Every unresolved repair remains visible.",
            Audience = "municipal repair crews",
            Payer = "city operations office",
            Evidence = "unpaid conversations with repair crews",
            Dependency = "existing work orders",
            Maintenance = "a maintenance coordinator",
            Harm = "managers could use it to blame workers"
        });

        Check(profile.Appetite != Appetite.Data, "letters inside maintenance triggered a Data appetite");
        Check(profile.Metrics.Viability < 0.65, "unpaid was treated as paid evidence");
        var before = profile.Metrics.Evidence;
        IdeaAnalyzer.ApplyEvidence(profile, "desire", 0, "No evidence yet");
        Check(Math.Abs(profile.Metrics.Evidence - before) < 0.0001, "zero-strength evidence inflated the score");
    }

    private static void TestStateProtection()
    {
        var director = new IdeaZooCaseDirector();
        director.Begin(MeetingBridge());
        director.EnterZoo();

        var prematureMoltRejected = false;
        try { director.EnterMolt(); }
        catch (InvalidOperationException) { prematureMoltRejected = true; }
        Check(prematureMoltRejected, "premature Molt was accepted");

        foreach (var id in new[] { "desire", "commitment", "burden", "refusal" })
            director.RecordEvidence(id, 1, "A weak but recorded signal.");
        director.EnterMolt();

        var unchangedRejected = false;
        try { director.ApplyMolt(director.Profile.Promise, director.Profile.Audience, Array.Empty<string>()); }
        catch (InvalidOperationException) { unchangedRejected = true; }
        Check(unchangedRejected, "unchanged Molt was accepted");
        director.CancelMolt();
        Check(director.Stage == CaseStage.Testing, "cancelled Molt left the state machine stuck");
    }

    private static IdeaIntake MeetingBridge()
    {
        return new IdeaIntake
        {
            Title = "Meeting Bridge",
            Idea = "A headset that uses AI speech models to translate business meetings in real time.",
            Problem = "People lose trust and detail when meetings cross languages.",
            Promise = "Two people can hold a ten minute meeting without managing a phone.",
            Audience = "cross-border sales teams",
            Payer = "export companies",
            Evidence = "interviewed six founders and built a tested prototype",
            Dependency = "speech models, microphones and trusted translation",
            Maintenance = "language quality and hardware support teams",
            Harm = "translation could hide uncertainty or change the force of consent"
        };
    }
}
