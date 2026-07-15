#if UNITY_EDITOR
using System;
using System.Linq;
using IdeaZoo.Core;
using IdeaZoo.Gameplay;
using NUnit.Framework;

namespace IdeaZoo.Tests.EditMode
{
    public sealed class GameplayDepthEditorTests
    {
        private static IdeaProfile Profile()
        {
            return IdeaAnalyzer.Analyze(new IdeaIntake
            {
                Title = "Pocket Clinic",
                Idea = "A small service that helps patients prepare questions before a clinic visit.",
                Problem = "Patients forget important details during short appointments.",
                Promise = "Help a patient prepare three useful questions in five minutes.",
                Audience = "Adults preparing for a first specialist appointment",
                Payer = "The clinic",
                Evidence = "Two patients described the problem.",
                Dependency = "A short intake workflow",
                Maintenance = "A named clinical content reviewer",
                Harm = "Sensitive health details could be exposed."
            });
        }

        [Test]
        public void EveryEvidenceHabitatIsPlayableAndBounded()
        {
            foreach (var testId in GameplayEncounterCatalog.TestIds)
            {
                var definition = GameplayEncounterCatalog.For(testId, Profile());
                Assert.AreEqual(3, definition.Rounds.Length, testId + " should remain a fast three-decision encounter.");
                Assert.IsTrue(definition.Rounds.All(round => round.Choices.Length >= 3 && round.Choices.Length <= GameplayPerformanceGovernor.MaximumEncounterButtons));

                var resources = GameplayResourceState.Fresh();
                var run = new GameplayEncounterRun(definition);
                while (!run.Complete)
                {
                    GameplayResourceSafety.EnsurePlayable(run.CurrentRound.Choices, resources);
                    var available = run.CurrentRound.Choices
                        .Select((choice, index) => new { choice, index })
                        .Where(item => resources.CanApply(item.choice.Impact))
                        .OrderByDescending(item => item.choice.Impact.TestScore)
                        .FirstOrDefault();
                    Assert.NotNull(available, testId + " produced a resource dead-end.");
                    string error;
                    Assert.IsTrue(run.Choose(available.index, resources, out error), error);
                }

                Assert.That(run.Strength(), Is.InRange(0, 3));
                Assert.IsFalse(string.IsNullOrWhiteSpace(run.EvidenceNote()));
                Assert.GreaterOrEqual(resources.Time, 0);
                Assert.GreaterOrEqual(resources.Trust, 0);
                Assert.GreaterOrEqual(resources.Momentum, 0);
            }
        }

        [Test]
        public void DisruptionsAreDeterministicAndAlwaysOfferAnAffordableResponse()
        {
            var profile = Profile();
            var first = GameplayDisruptionCatalog.For(profile, 1);
            var repeated = GameplayDisruptionCatalog.For(profile, 1);
            Assert.AreEqual(first.Kind, repeated.Kind);
            Assert.AreEqual(first.Title, repeated.Title);

            var resources = GameplayResourceState.Fresh();
            Assert.IsTrue(first.Choices.Any(choice => resources.CanApply(choice.Impact)));
        }

        [Test]
        public void ResourceStateCannotBecomeNegative()
        {
            var resources = new GameplayResourceState { Time = 1, Trust = 1, Momentum = 1, Evidence = 0 };
            var impact = new GameplayImpact { Time = -5, Trust = -5, Momentum = -5, Evidence = 50 };
            Assert.IsFalse(resources.CanApply(impact));
            resources.Apply(impact);
            Assert.AreEqual(0, resources.Time);
            Assert.AreEqual(0, resources.Trust);
            Assert.AreEqual(0, resources.Momentum);
            Assert.AreEqual(30, resources.Evidence);
        }

        [Test]
        public void ReserveProtocolPreventsAResourceSoftLockWithoutGrantingExcessCapacity()
        {
            var resources = new GameplayResourceState { Time = 0, Trust = 0, Momentum = 0, Evidence = 12 };
            var choices = new[]
            {
                new GameplayChoice("expensive", "Expensive", "", new GameplayImpact { Time = -4, Trust = -2, Momentum = -2 }),
                new GameplayChoice("smallest", "Smallest", "", new GameplayImpact { Time = -1, Trust = 0, Momentum = -1 }),
                new GameplayChoice("middle", "Middle", "", new GameplayImpact { Time = -2, Trust = -1, Momentum = -1 })
            };

            Assert.IsTrue(GameplayResourceSafety.EnsurePlayable(choices, resources));
            Assert.IsTrue(choices.Any(choice => resources.CanApply(choice.Impact)));
            Assert.AreEqual(1, resources.Time);
            Assert.AreEqual(0, resources.Trust);
            Assert.AreEqual(1, resources.Momentum);
            Assert.AreEqual(12, resources.Evidence);
            Assert.IsFalse(resources.CanApply(choices[0].Impact));
        }

        [Test]
        public void PersistentMemoryIsCappedToAvoidUnboundedSaveAndWorldGrowth()
        {
            var state = new GameplayMemoryState();
            for (var i = 0; i < 35; i++) state.Cases.Add(new GameplayCaseMemory { RecordId = "case-" + i });
            state.Trim();
            Assert.AreEqual(GameplayPerformanceGovernor.MaximumSavedCases, state.Cases.Count);
            Assert.AreEqual("case-15", state.Cases[0].RecordId);
        }

        [Test]
        public void PerformanceBudgetsRemainMobileSafe()
        {
            Assert.AreEqual(30, GameplayPerformanceGovernor.MobileTargetFps);
            Assert.LessOrEqual(GameplayPerformanceGovernor.MaximumEncounterButtons, 4);
            Assert.LessOrEqual(GameplayPerformanceGovernor.MaximumVisibleMemoryCards, 12);
            Assert.LessOrEqual(GameplayPerformanceGovernor.MaximumSavedCases, 20);
        }
    }
}
#endif
