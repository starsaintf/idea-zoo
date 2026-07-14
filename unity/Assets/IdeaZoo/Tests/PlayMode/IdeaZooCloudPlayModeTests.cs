using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdeaZoo.Tests.PlayMode
{
    public sealed class IdeaZooCloudPlayModeTests
    {
        [UnityTest]
        public IEnumerator WhisperGateBootsCompleteRuntime()
        {
            SceneManager.LoadScene("WhisperGate", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return null;

            var gameType = Type.GetType("IdeaZoo.Runtime.IdeaZooGame, Assembly-CSharp");
            Assert.NotNull(gameType, "IdeaZooGame type was not imported.");
            var game = Resources.FindObjectsOfTypeAll(gameType)
                .OfType<Component>()
                .FirstOrDefault(component => component != null && component.gameObject.scene.IsValid());
            Assert.NotNull(game, "Whisper Gate did not auto-boot IdeaZooGame.");

            var worldProperty = gameType.GetProperty("World");
            var keeperProperty = gameType.GetProperty("Keeper");
            var creatureProperty = gameType.GetProperty("Creature");
            var hudProperty = gameType.GetProperty("Hud");
            Assert.NotNull(worldProperty, "World property is missing.");
            Assert.NotNull(keeperProperty, "Keeper property is missing.");
            Assert.NotNull(creatureProperty, "Creature property is missing.");
            Assert.NotNull(hudProperty, "HUD property is missing.");

            var world = worldProperty.GetValue(game) as Component;
            Assert.NotNull(world, "Runtime world was not constructed.");
            Assert.NotNull(keeperProperty.GetValue(game), "Keeper was not constructed.");
            Assert.NotNull(creatureProperty.GetValue(game), "Creature assembler was not constructed.");
            Assert.NotNull(hudProperty.GetValue(game), "HUD was not constructed.");

            var stationCount = world.GetComponentsInChildren<Component>(true)
                .Count(component => component != null && component.GetType().FullName == "IdeaZoo.Runtime.IdeaStation");
            var specialistCount = world.GetComponentsInChildren<Component>(true)
                .Count(component => component != null && component.GetType().FullName == "IdeaZoo.Presentation.ProceduralSpecialist");
            Assert.GreaterOrEqual(stationCount, 11, "Runtime world is missing evidence or ruling stations.");
            Assert.GreaterOrEqual(specialistCount, 6, "Runtime world is missing specialist characters.");

            var whisperGate = FindChild(world.transform, "01_WHISPER_GATE");
            var decisionGarden = FindChild(world.transform, "10_DECISION_GARDEN");
            Assert.NotNull(whisperGate, "Whisper Gate department is missing.");
            Assert.NotNull(decisionGarden, "Decision Garden department is missing.");
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }
    }
}
