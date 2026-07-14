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

            var gameType = Type.GetType("IdeaZoo.Runtime.IdeaZooGame, Assembly-CSharp");
            Assert.NotNull(gameType, "IdeaZooGame type was not imported.");

            Component game = null;
            Component world = null;
            var stationCount = 0;
            var specialistCount = 0;
            var authoredDetailCount = 0;
            var worldProperty = gameType.GetProperty("World");
            var keeperProperty = gameType.GetProperty("Keeper");
            var creatureProperty = gameType.GetProperty("Creature");
            var hudProperty = gameType.GetProperty("Hud");
            Assert.NotNull(worldProperty, "World property is missing.");
            Assert.NotNull(keeperProperty, "Keeper property is missing.");
            Assert.NotNull(creatureProperty, "Creature property is missing.");
            Assert.NotNull(hudProperty, "HUD property is missing.");

            for (var frame = 0; frame < 240; frame++)
            {
                game = Resources.FindObjectsOfTypeAll(gameType)
                    .OfType<Component>()
                    .FirstOrDefault(component => component != null && component.gameObject.scene.IsValid());
                if (game != null)
                {
                    world = worldProperty.GetValue(game) as Component;
                    if (world != null)
                    {
                        var components = world.GetComponentsInChildren<Component>(true);
                        stationCount = components.Count(component => component != null && component.GetType().FullName == "IdeaZoo.Runtime.IdeaStation");
                        specialistCount = components.Count(component => component != null && component.GetType().FullName == "IdeaZoo.Presentation.ProceduralSpecialist");
                        authoredDetailCount = components.Count(component => component != null && component.GetType().FullName == "IdeaZoo.Presentation.AuthoredEnvironmentDetail");
                        if (stationCount >= 11 && specialistCount >= 6 && authoredDetailCount >= 24) break;
                    }
                }
                yield return null;
            }

            Assert.NotNull(game, "Whisper Gate did not auto-boot IdeaZooGame within 240 frames.");
            Assert.NotNull(world, "Runtime world was not constructed.");
            Assert.NotNull(keeperProperty.GetValue(game), "Keeper was not constructed.");
            Assert.NotNull(creatureProperty.GetValue(game), "Creature assembler was not constructed.");
            Assert.NotNull(hudProperty.GetValue(game), "HUD was not constructed.");
            Assert.GreaterOrEqual(stationCount, 11, "Runtime world is missing evidence or ruling stations.");
            Assert.GreaterOrEqual(specialistCount, 6, "Runtime world is missing specialist characters.");
            Assert.GreaterOrEqual(authoredDetailCount, 24, "Runtime world did not install the authored environment kit.");

            var whisperGate = FindChild(world.transform, "01_WHISPER_GATE");
            var decisionGarden = FindChild(world.transform, "10_DECISION_GARDEN");
            var authoredRoot = FindChild(world.transform, "AUTHORED_ENVIRONMENT_KIT");
            Assert.NotNull(whisperGate, "Whisper Gate department is missing.");
            Assert.NotNull(decisionGarden, "Decision Garden department is missing.");
            Assert.NotNull(authoredRoot, "Authored environment bootstrap did not complete.");
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }
    }
}
