#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace IdeaZoo.Tests.EditMode
{
    public sealed class GameplayDepthEditorTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        [Test]
        public void GameplayDepthRuntimeAssembliesImport()
        {
            foreach (var name in new[]
            {
                "IdeaZoo.Gameplay.GameplayEncounterCatalog",
                "IdeaZoo.Gameplay.GameplayEncounterRun",
                "IdeaZoo.Gameplay.GameplayResourceState",
                "IdeaZoo.Gameplay.GameplayResourceSafety",
                "IdeaZoo.Gameplay.GameplayDisruptionCatalog",
                "IdeaZoo.Gameplay.GameplayMemoryState",
                "IdeaZoo.Gameplay.GameplayPerformanceGovernor",
                "IdeaZoo.Gameplay.GameplayDepthDirector",
                "IdeaZoo.Gameplay.GameplayDepthHud"
            })
                Assert.NotNull(Runtime(name), name + " was not imported into Assembly-CSharp.");
        }

        [Test]
        public void EveryEvidenceHabitatIsPlayableAndBounded()
        {
            var catalogType = Runtime("IdeaZoo.Gameplay.GameplayEncounterCatalog");
            var resourcesType = Runtime("IdeaZoo.Gameplay.GameplayResourceState");
            var runType = Runtime("IdeaZoo.Gameplay.GameplayEncounterRun");
            var safetyType = Runtime("IdeaZoo.Gameplay.GameplayResourceSafety");
            var performanceType = Runtime("IdeaZoo.Gameplay.GameplayPerformanceGovernor");
            Assert.NotNull(catalogType);
            Assert.NotNull(resourcesType);
            Assert.NotNull(runType);
            Assert.NotNull(safetyType);
            Assert.NotNull(performanceType);

            var testIds = (string[])catalogType.GetProperty("TestIds", PublicStatic).GetValue(null);
            var createDefinition = catalogType.GetMethod("For", PublicStatic);
            var fresh = resourcesType.GetMethod("Fresh", PublicStatic);
            var canApply = resourcesType.GetMethod("CanApply", PublicInstance);
            var ensurePlayable = safetyType.GetMethod("EnsurePlayable", PublicStatic);
            var choose = runType.GetMethod("Choose", PublicInstance);
            var strength = runType.GetMethod("Strength", PublicInstance);
            var evidenceNote = runType.GetMethod("EvidenceNote", PublicInstance);
            var complete = runType.GetProperty("Complete", PublicInstance);
            var currentRound = runType.GetProperty("CurrentRound", PublicInstance);
            var maxButtons = ConstantInt(performanceType, "MaximumEncounterButtons");

            CollectionAssert.AreEquivalent(new[] { "desire", "commitment", "burden", "refusal" }, testIds);

            foreach (var testId in testIds)
            {
                var definition = createDefinition.Invoke(null, new object[] { testId, null });
                var rounds = (Array)Field(definition, "Rounds").GetValue(definition);
                Assert.AreEqual(3, rounds.Length, testId + " should remain a fast three-decision encounter.");
                for (var roundIndex = 0; roundIndex < rounds.Length; roundIndex++)
                {
                    var choices = (Array)Field(rounds.GetValue(roundIndex), "Choices").GetValue(rounds.GetValue(roundIndex));
                    Assert.That(choices.Length, Is.InRange(3, maxButtons));
                }

                var resources = fresh.Invoke(null, null);
                var run = Activator.CreateInstance(runType, new[] { definition });
                var guard = 0;
                while (!(bool)complete.GetValue(run))
                {
                    Assert.Less(guard++, 8, testId + " did not terminate within its bounded pacing contract.");
                    var round = currentRound.GetValue(run);
                    var choices = (Array)Field(round, "Choices").GetValue(round);
                    ensurePlayable.Invoke(null, new object[] { choices, resources });

                    var bestIndex = -1;
                    var bestScore = int.MinValue;
                    for (var index = 0; index < choices.Length; index++)
                    {
                        var choice = choices.GetValue(index);
                        var impact = Field(choice, "Impact").GetValue(choice);
                        if (!(bool)canApply.Invoke(resources, new[] { impact })) continue;
                        var score = (int)Field(impact, "TestScore").GetValue(impact);
                        if (score <= bestScore) continue;
                        bestScore = score;
                        bestIndex = index;
                    }

                    Assert.GreaterOrEqual(bestIndex, 0, testId + " produced a resource dead-end.");
                    var arguments = new object[] { bestIndex, resources, null };
                    Assert.IsTrue((bool)choose.Invoke(run, arguments), arguments[2] as string);
                }

                Assert.That((int)strength.Invoke(run, null), Is.InRange(0, 3));
                Assert.IsFalse(string.IsNullOrWhiteSpace((string)evidenceNote.Invoke(run, null)));
                Assert.GreaterOrEqual(IntField(resources, "Time"), 0);
                Assert.GreaterOrEqual(IntField(resources, "Trust"), 0);
                Assert.GreaterOrEqual(IntField(resources, "Momentum"), 0);
            }
        }

        [Test]
        public void DisruptionsAreDeterministicAndAlwaysOfferAnAffordableResponse()
        {
            var catalogType = Runtime("IdeaZoo.Gameplay.GameplayDisruptionCatalog");
            var resourcesType = Runtime("IdeaZoo.Gameplay.GameplayResourceState");
            var first = catalogType.GetMethod("For", PublicStatic).Invoke(null, new object[] { null, 1 });
            var repeated = catalogType.GetMethod("For", PublicStatic).Invoke(null, new object[] { null, 1 });
            Assert.AreEqual(Field(first, "Kind").GetValue(first), Field(repeated, "Kind").GetValue(repeated));
            Assert.AreEqual(Field(first, "Title").GetValue(first), Field(repeated, "Title").GetValue(repeated));

            var resources = resourcesType.GetMethod("Fresh", PublicStatic).Invoke(null, null);
            var canApply = resourcesType.GetMethod("CanApply", PublicInstance);
            var choices = (Array)Field(first, "Choices").GetValue(first);
            var affordable = false;
            for (var index = 0; index < choices.Length; index++)
            {
                var impact = Field(choices.GetValue(index), "Impact").GetValue(choices.GetValue(index));
                if ((bool)canApply.Invoke(resources, new[] { impact })) affordable = true;
            }
            Assert.IsTrue(affordable);
        }

        [Test]
        public void ResourceStateCannotBecomeNegative()
        {
            var resourcesType = Runtime("IdeaZoo.Gameplay.GameplayResourceState");
            var impactType = Runtime("IdeaZoo.Gameplay.GameplayImpact");
            var resources = Activator.CreateInstance(resourcesType);
            SetInt(resources, "Time", 1);
            SetInt(resources, "Trust", 1);
            SetInt(resources, "Momentum", 1);
            SetInt(resources, "Evidence", 0);
            var impact = Impact(impactType, -5, -5, -5, 50);

            Assert.IsFalse((bool)resourcesType.GetMethod("CanApply", PublicInstance).Invoke(resources, new[] { impact }));
            resourcesType.GetMethod("Apply", PublicInstance).Invoke(resources, new[] { impact });
            Assert.AreEqual(0, IntField(resources, "Time"));
            Assert.AreEqual(0, IntField(resources, "Trust"));
            Assert.AreEqual(0, IntField(resources, "Momentum"));
            Assert.AreEqual(30, IntField(resources, "Evidence"));
        }

        [Test]
        public void ReserveProtocolPreventsAResourceSoftLockWithoutGrantingExcessCapacity()
        {
            var resourcesType = Runtime("IdeaZoo.Gameplay.GameplayResourceState");
            var impactType = Runtime("IdeaZoo.Gameplay.GameplayImpact");
            var choiceType = Runtime("IdeaZoo.Gameplay.GameplayChoice");
            var safetyType = Runtime("IdeaZoo.Gameplay.GameplayResourceSafety");
            var resources = Activator.CreateInstance(resourcesType);
            SetInt(resources, "Time", 0);
            SetInt(resources, "Trust", 0);
            SetInt(resources, "Momentum", 0);
            SetInt(resources, "Evidence", 12);

            var choices = Array.CreateInstance(choiceType, 3);
            choices.SetValue(Choice(choiceType, "expensive", Impact(impactType, -4, -2, -2, 0)), 0);
            choices.SetValue(Choice(choiceType, "smallest", Impact(impactType, -1, 0, -1, 0)), 1);
            choices.SetValue(Choice(choiceType, "middle", Impact(impactType, -2, -1, -1, 0)), 2);

            Assert.IsTrue((bool)safetyType.GetMethod("EnsurePlayable", PublicStatic).Invoke(null, new object[] { choices, resources }));
            Assert.AreEqual(1, IntField(resources, "Time"));
            Assert.AreEqual(0, IntField(resources, "Trust"));
            Assert.AreEqual(1, IntField(resources, "Momentum"));
            Assert.AreEqual(12, IntField(resources, "Evidence"));

            var canApply = resourcesType.GetMethod("CanApply", PublicInstance);
            var affordable = false;
            for (var index = 0; index < choices.Length; index++)
            {
                var impact = Field(choices.GetValue(index), "Impact").GetValue(choices.GetValue(index));
                if ((bool)canApply.Invoke(resources, new[] { impact })) affordable = true;
            }
            Assert.IsTrue(affordable);
            var expensiveImpact = Field(choices.GetValue(0), "Impact").GetValue(choices.GetValue(0));
            Assert.IsFalse((bool)canApply.Invoke(resources, new[] { expensiveImpact }));
        }

        [Test]
        public void PersistentMemoryIsCappedToAvoidUnboundedSaveAndWorldGrowth()
        {
            var stateType = Runtime("IdeaZoo.Gameplay.GameplayMemoryState");
            var caseType = Runtime("IdeaZoo.Gameplay.GameplayCaseMemory");
            var performanceType = Runtime("IdeaZoo.Gameplay.GameplayPerformanceGovernor");
            var state = Activator.CreateInstance(stateType);
            var cases = (IList)Field(state, "Cases").GetValue(state);
            for (var index = 0; index < 35; index++)
            {
                var memory = Activator.CreateInstance(caseType);
                Field(memory, "RecordId").SetValue(memory, "case-" + index);
                cases.Add(memory);
            }

            stateType.GetMethod("Trim", PublicInstance).Invoke(state, null);
            Assert.AreEqual(ConstantInt(performanceType, "MaximumSavedCases"), cases.Count);
            Assert.AreEqual("case-15", Field(cases[0], "RecordId").GetValue(cases[0]));
        }

        [Test]
        public void PerformanceBudgetsRemainMobileSafe()
        {
            var performanceType = Runtime("IdeaZoo.Gameplay.GameplayPerformanceGovernor");
            Assert.AreEqual(30, ConstantInt(performanceType, "MobileTargetFps"));
            Assert.LessOrEqual(ConstantInt(performanceType, "MaximumEncounterButtons"), 4);
            Assert.LessOrEqual(ConstantInt(performanceType, "MaximumVisibleMemoryCards"), 12);
            Assert.LessOrEqual(ConstantInt(performanceType, "MaximumSavedCases"), 20);
        }

        private static Type Runtime(string fullName)
        {
            return Type.GetType(fullName + ", Assembly-CSharp");
        }

        private static FieldInfo Field(object value, string name)
        {
            Assert.NotNull(value);
            var field = value.GetType().GetField(name, PublicInstance);
            Assert.NotNull(field, value.GetType().FullName + "." + name + " is missing.");
            return field;
        }

        private static int IntField(object value, string name)
        {
            return (int)Field(value, name).GetValue(value);
        }

        private static void SetInt(object value, string name, int number)
        {
            Field(value, name).SetValue(value, number);
        }

        private static int ConstantInt(Type type, string name)
        {
            var field = type.GetField(name, PublicStatic);
            Assert.NotNull(field, type.FullName + "." + name + " is missing.");
            return (int)field.GetRawConstantValue();
        }

        private static object Impact(Type impactType, int time, int trust, int momentum, int evidence)
        {
            var impact = Activator.CreateInstance(impactType);
            SetInt(impact, "Time", time);
            SetInt(impact, "Trust", trust);
            SetInt(impact, "Momentum", momentum);
            SetInt(impact, "Evidence", evidence);
            return impact;
        }

        private static object Choice(Type choiceType, string id, object impact)
        {
            return Activator.CreateInstance(choiceType, new[] { id, id, string.Empty, impact });
        }
    }
}
#endif
