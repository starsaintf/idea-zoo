#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace IdeaZoo.Tests.EditMode
{
    public sealed class PlayerExperienceEditorTests
    {
        private static Type T(string name) { return Type.GetType(name + ", Assembly-CSharp"); }

        [Test]
        public void PlayerExperienceRuntimeTypesImport()
        {
            Assert.NotNull(T("IdeaZoo.PlayerExperience.PlayerExperienceDirector"));
            Assert.NotNull(T("IdeaZoo.PlayerExperience.PlayerExperienceHud"));
            Assert.NotNull(T("IdeaZoo.PlayerExperience.PlayerExperienceWorldPass"));
            Assert.NotNull(T("IdeaZoo.PlayerExperience.PlayerExperienceAccessibilityController"));
            Assert.NotNull(T("IdeaZoo.PlayerExperience.PlayerExperienceTactileCatalog"));
            Assert.NotNull(T("IdeaZoo.PlayerExperience.PlayerExperienceArchetypeCatalog"));
        }

        [Test]
        public void GuidedCaseUsesACompleteRealIntake()
        {
            var tutorial = T("IdeaZoo.PlayerExperience.PlayerExperienceTutorial");
            Assert.NotNull(tutorial);
            var intakeMethod = tutorial.GetMethod("Intake", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(intakeMethod);
            var intake = intakeMethod.Invoke(null, null);
            Assert.NotNull(intake);
            var type = intake.GetType();
            Assert.AreEqual("The Neighbourhood Umbrella Library", type.GetField("Title").GetValue(intake));
            foreach (var field in new[] { "Idea", "Problem", "Promise", "Audience", "Payer", "Evidence", "Dependency", "Maintenance", "Harm" })
                Assert.IsFalse(string.IsNullOrWhiteSpace((string)type.GetField(field).GetValue(intake)), field + " is empty.");
        }

        [Test]
        public void AllEightHiddenArchetypesExist()
        {
            var archetype = T("IdeaZoo.PlayerExperience.IdeaArchetype");
            Assert.NotNull(archetype);
            Assert.AreEqual(8, Enum.GetNames(archetype).Length);
            CollectionAssert.Contains(Enum.GetNames(archetype), "GoodIdeaWrongAudience");
            CollectionAssert.Contains(Enum.GetNames(archetype), "ProfitableButHarmful");
            CollectionAssert.Contains(Enum.GetNames(archetype), "BetterAsFeature");
            CollectionAssert.Contains(Enum.GetNames(archetype), "HiddenAudience");
        }

        [Test]
        public void EveryEncounterRoundHasBoundedTactilePreparation()
        {
            var catalog = T("IdeaZoo.PlayerExperience.PlayerExperienceTactileCatalog");
            Assert.NotNull(catalog);
            var forMethod = catalog.GetMethod("For", BindingFlags.Public | BindingFlags.Static);
            var resolveMethod = catalog.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(forMethod);
            Assert.NotNull(resolveMethod);

            foreach (var testId in new[] { "desire", "commitment", "burden", "refusal" })
            {
                for (var round = 0; round < 3; round++)
                {
                    var spec = forMethod.Invoke(null, new object[] { testId, round });
                    Assert.NotNull(spec, testId + " round " + round + " has no tactile interaction.");
                    var specType = spec.GetType();
                    var required = (int)specType.GetField("RequiredSelections").GetValue(spec);
                    var tokens = (Array)specType.GetField("Tokens").GetValue(spec);
                    Assert.That(required, Is.InRange(1, 3));
                    Assert.That(tokens.Length, Is.InRange(required, 6));

                    var strongIds = tokens.Cast<object>()
                        .Where(token => (bool)token.GetType().GetField("StrongSignal").GetValue(token))
                        .Take(required)
                        .Select(token => (string)token.GetType().GetField("Id").GetValue(token))
                        .ToArray();
                    var outcome = resolveMethod.Invoke(null, new object[] { spec, strongIds });
                    Assert.NotNull(outcome);
                    Assert.IsFalse(string.IsNullOrWhiteSpace((string)outcome.GetType().GetField("Summary").GetValue(outcome)));
                }
            }
        }

        [Test]
        public void PlayerExperienceHistoryIsCappedAndRanksProgress()
        {
            var stateType = T("IdeaZoo.PlayerExperience.PlayerExperienceState");
            var recordType = T("IdeaZoo.PlayerExperience.PlayerExperienceCaseRecord");
            Assert.NotNull(stateType);
            Assert.NotNull(recordType);
            var state = Activator.CreateInstance(stateType);
            var cases = (IList)stateType.GetField("Cases").GetValue(state);
            for (var i = 0; i < 30; i++)
            {
                var record = Activator.CreateInstance(recordType);
                recordType.GetField("RecordId").SetValue(record, "case-" + i);
                recordType.GetField("HasRuling").SetValue(record, true);
                cases.Add(record);
            }
            stateType.GetMethod("Trim").Invoke(state, null);
            stateType.GetMethod("RecalculateRank").Invoke(state, null);
            Assert.AreEqual(20, cases.Count);
            Assert.AreEqual("Founder", stateType.GetField("Rank").GetValue(state).ToString());
        }

        [Test]
        public void AccessibilityTextScaleRemainsBounded()
        {
            var settingsType = T("IdeaZoo.PlayerExperience.PlayerAccessibilitySettings");
            var settings = Activator.CreateInstance(settingsType);
            var index = settingsType.GetField("TextScaleIndex");
            var scale = settingsType.GetProperty("TextScale");
            index.SetValue(settings, 0);
            Assert.AreEqual(1f, (float)scale.GetValue(settings), .001f);
            index.SetValue(settings, 2);
            Assert.That((float)scale.GetValue(settings), Is.InRange(1.3f, 1.4f));
        }
    }
}
#endif
