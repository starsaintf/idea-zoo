using IdeaZoo.Gameplay;

namespace IdeaZoo.PlayerExperience
{
    public static class PlayerExperienceRankGuidance
    {
        public static GameplayEncounterDefinition Apply(KeeperRank rank, GameplayEncounterDefinition definition)
        {
            if (definition == null) return null;
            var rounds = new GameplayRound[definition.Rounds.Length];
            for (var i = 0; i < definition.Rounds.Length; i++)
            {
                var source = definition.Rounds[i];
                rounds[i] = new GameplayRound(source.Prompt, source.Context + "\n\n" + Guidance(rank, definition.Kind, i), source.Choices);
            }
            return new GameplayEncounterDefinition(definition.TestId, definition.Kind, definition.Title, definition.Mission, rounds);
        }

        private static string Guidance(KeeperRank rank, GameplayEncounterKind kind, int round)
        {
            if (rank == KeeperRank.Apprentice)
            {
                if (kind == GameplayEncounterKind.CustomerInterview) return "APPRENTICE LENS: Behaviour, repeated urgency and costly commitment are stronger than praise.";
                if (kind == GameplayEncounterKind.PrototypeTrial) return "APPRENTICE LENS: Test one promise before building the surrounding product.";
                if (kind == GameplayEncounterKind.FeasibilityAudit) return "APPRENTICE LENS: Recurring labour and single-owner dependencies are part of the design.";
                return "APPRENTICE LENS: A real exit is visible, penalty-free and usable under pressure.";
            }
            if (rank == KeeperRank.Keeper)
                return "KEEPER LENS: One comfortable answer is probably protecting the current shape.";
            if (rank == KeeperRank.Curator)
                return "CURATOR LENS: Two pieces of evidence may be true and still point to different ideas.";
            if (rank == KeeperRank.Warden)
                return "WARDEN LENS: Judge the decision under public pressure, scarce time and ethical obligation at once.";
            return round == 0 ? "FOUNDER MODE: The Zoo will show consequences, not recommend a move." : string.Empty;
        }
    }
}
