using System;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Gameplay;

namespace IdeaZoo.PlayerExperience
{
    public enum PlayerExperienceInteractionKind
    {
        SignalSort,
        ScopeBudget,
        DependencyMap,
        PlainLanguage
    }

    public sealed class PlayerExperienceTactileToken
    {
        public readonly string Id;
        public readonly string Label;
        public readonly bool StrongSignal;

        public PlayerExperienceTactileToken(string id, string label, bool strongSignal)
        {
            Id = id;
            Label = label;
            StrongSignal = strongSignal;
        }
    }

    public sealed class PlayerExperienceTactileSpec
    {
        public readonly PlayerExperienceInteractionKind Kind;
        public readonly string Prompt;
        public readonly int RequiredSelections;
        public readonly PlayerExperienceTactileToken[] Tokens;

        public PlayerExperienceTactileSpec(PlayerExperienceInteractionKind kind, string prompt, int requiredSelections, params PlayerExperienceTactileToken[] tokens)
        {
            Kind = kind;
            Prompt = prompt;
            RequiredSelections = Math.Max(1, requiredSelections);
            Tokens = tokens ?? Array.Empty<PlayerExperienceTactileToken>();
        }
    }

    public sealed class PlayerExperienceTactileOutcome
    {
        public string Summary = string.Empty;
        public GameplayImpact Impact = new GameplayImpact();
        public int StrongSelections;
        public int TotalSelections;
    }

    public static class PlayerExperienceTactileCatalog
    {
        public static PlayerExperienceTactileSpec For(string testId, int roundIndex)
        {
            if (string.Equals(testId, "desire", StringComparison.OrdinalIgnoreCase)) return Interview(roundIndex);
            if (string.Equals(testId, "commitment", StringComparison.OrdinalIgnoreCase)) return Prototype(roundIndex);
            if (string.Equals(testId, "burden", StringComparison.OrdinalIgnoreCase)) return Feasibility(roundIndex);
            if (string.Equals(testId, "refusal", StringComparison.OrdinalIgnoreCase)) return Jury(roundIndex);
            return null;
        }

        public static PlayerExperienceTactileOutcome Resolve(PlayerExperienceTactileSpec spec, IEnumerable<string> selectedIds)
        {
            if (spec == null) return null;
            var ids = new HashSet<string>(selectedIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var selected = spec.Tokens.Where(token => ids.Contains(token.Id)).ToArray();
            var strong = selected.Count(token => token.StrongSignal);
            var complete = selected.Length >= spec.RequiredSelections;
            var outcome = new PlayerExperienceTactileOutcome
            {
                TotalSelections = selected.Length,
                StrongSelections = strong,
                Summary = Summary(spec.Kind, selected, strong, complete),
                Impact = new GameplayImpact
                {
                    Evidence = complete && strong >= Math.Max(1, spec.RequiredSelections - 1) ? 1 : 0,
                    TestScore = complete && strong >= spec.RequiredSelections ? 1 : 0,
                    Desirability = spec.Kind == PlayerExperienceInteractionKind.SignalSort ? strong * .006 : 0,
                    Feasibility = spec.Kind == PlayerExperienceInteractionKind.DependencyMap ? strong * .006 : 0,
                    Viability = spec.Kind == PlayerExperienceInteractionKind.ScopeBudget ? strong * .006 : 0,
                    Safety = spec.Kind == PlayerExperienceInteractionKind.PlainLanguage ? strong * .008 : 0,
                    Tendency = Tendency(spec.Kind)
                }
            };
            return outcome;
        }

        private static PlayerExperienceTactileSpec Interview(int round)
        {
            if (round == 0)
                return Spec(PlayerExperienceInteractionKind.SignalSort, "Select the two signals worth carrying into the interview.", 2,
                    T("compliment", "They say it sounds useful", false),
                    T("workaround", "They show today’s workaround", true),
                    T("urgency", "They describe the last urgent moment", true),
                    T("feature", "They request a feature", false),
                    T("payment", "They already spend money or time", true));
            if (round == 1)
                return Spec(PlayerExperienceInteractionKind.SignalSort, "Select the contradictions that deserve another question.", 2,
                    T("different-user", "A different user has the sharper need", true),
                    T("polite-no", "Praise disappears when commitment is requested", true),
                    T("one-negative", "One person simply dislikes it", false),
                    T("old-habit", "The existing habit is easier than expected", true),
                    T("founder-story", "The result does not fit your pitch", false));
            return Spec(PlayerExperienceInteractionKind.SignalSort, "Select the two demand signals that cost the user something real.", 2,
                T("return", "They return voluntarily", true),
                T("share", "They share the post", false),
                T("preorder", "They preorder or sign a pilot", true),
                T("survey", "They rate it highly", false),
                T("workflow", "They change a real workflow", true));
        }

        private static PlayerExperienceTactileSpec Prototype(int round)
        {
            if (round == 0)
                return Spec(PlayerExperienceInteractionKind.ScopeBudget, "Your prototype budget fits two pieces. Select them.", 2,
                    T("core", "The core promise", true),
                    T("manual", "A manual service behind the screen", true),
                    T("profile", "Profiles and settings", false),
                    T("automation", "Full automation", false),
                    T("analytics", "Analytics dashboard", false));
            if (round == 1)
                return Spec(PlayerExperienceInteractionKind.ScopeBudget, "Select the two conditions that make the first test honest.", 2,
                    T("target", "The narrow first user", true),
                    T("stranger", "Someone who does not know the team", true),
                    T("friends", "Supportive friends", false),
                    T("crowd", "A broad public crowd", false),
                    T("real-setting", "The actual place the problem occurs", true));
            return Spec(PlayerExperienceInteractionKind.ScopeBudget, "Select the commitments strong enough to change the build decision.", 2,
                T("pilot", "Signed pilot", true),
                T("preorder", "Preorder", true),
                T("second-session", "Second voluntary session", true),
                T("likes", "Likes and reactions", false),
                T("survey", "High satisfaction score", false));
        }

        private static PlayerExperienceTactileSpec Feasibility(int round)
        {
            if (round == 0)
                return Spec(PlayerExperienceInteractionKind.DependencyMap, "Map three recurring burdens the launch plan must carry.", 3,
                    T("support", "Support and exceptions", true),
                    T("moderation", "Moderation or dispute handling", true),
                    T("compliance", "Compliance and review", true),
                    T("launch", "Launch campaign", false),
                    T("demo", "Demo polish", false),
                    T("repair", "Repair and replacement", true));
            if (round == 1)
                return Spec(PlayerExperienceInteractionKind.DependencyMap, "Select the dependencies that need a fallback before launch.", 2,
                    T("single-vendor", "One irreplaceable vendor", true),
                    T("manual-review", "A scarce specialist reviewer", true),
                    T("commodity", "A replaceable commodity service", false),
                    T("regulation", "One untested regulatory assumption", true),
                    T("branding", "Brand artwork", false));
            return Spec(PlayerExperienceInteractionKind.DependencyMap, "Select the pieces that make ownership real.", 3,
                T("named-owner", "Named accountable keeper", true),
                T("budget", "Recurring maintenance budget", true),
                T("authority", "Authority to stop or change it", true),
                T("everyone", "Everyone owns it", false),
                T("goodwill", "Goodwill and spare time", false));
        }

        private static PlayerExperienceTactileSpec Jury(int round)
        {
            if (round == 0)
                return Spec(PlayerExperienceInteractionKind.PlainLanguage, "Build the explanation from three human pieces.", 3,
                    T("person", "A specific person", true),
                    T("problem", "The problem they already face", true),
                    T("outcome", "The change they can feel", true),
                    T("model", "The model architecture", false),
                    T("platform", "The platform category", false));
            if (round == 1)
                return Spec(PlayerExperienceInteractionKind.PlainLanguage, "Select the three parts of a real exit.", 3,
                    T("visible", "Visible from the main path", true),
                    T("no-penalty", "No punishment or lost essentials", true),
                    T("delete", "Deletion or return of their data", true),
                    T("settings", "Hidden in settings", false),
                    T("support", "Requires contacting support", false));
            return Spec(PlayerExperienceInteractionKind.PlainLanguage, "Name the cruelest plausible use with three concrete pieces.", 3,
                T("actor", "Who has the power", true),
                T("action", "What they do", true),
                T("victim", "Who carries the harm", true),
                T("misuse", "People might misuse it", false),
                T("intent", "That was not our intent", false));
        }

        private static PlayerExperienceTactileSpec Spec(PlayerExperienceInteractionKind kind, string prompt, int required, params PlayerExperienceTactileToken[] tokens)
        {
            return new PlayerExperienceTactileSpec(kind, prompt, required, tokens);
        }

        private static PlayerExperienceTactileToken T(string id, string label, bool strong)
        {
            return new PlayerExperienceTactileToken(id, label, strong);
        }

        private static GameplayTendency Tendency(PlayerExperienceInteractionKind kind)
        {
            if (kind == PlayerExperienceInteractionKind.ScopeBudget) return GameplayTendency.Simplifier;
            if (kind == PlayerExperienceInteractionKind.DependencyMap) return GameplayTendency.Skeptic;
            if (kind == PlayerExperienceInteractionKind.PlainLanguage) return GameplayTendency.Protector;
            return GameplayTendency.Experimenter;
        }

        private static string Summary(PlayerExperienceInteractionKind kind, PlayerExperienceTactileToken[] selected, int strong, bool complete)
        {
            var labels = selected == null || selected.Length == 0 ? "nothing" : string.Join(", ", selected.Select(item => item.Label).ToArray());
            var quality = !complete ? "unfinished" : strong >= Math.Max(1, selected.Length - 1) ? "strong" : "mixed";
            return kind + " preparation was " + quality + ": " + labels + ".";
        }
    }
}
