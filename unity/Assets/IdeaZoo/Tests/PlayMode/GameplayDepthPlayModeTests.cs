#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdeaZoo.Tests.PlayMode
{
    public sealed class GameplayDepthPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameplayDepthBootsOnceAndReusesTheExistingWorld()
        {
            SceneManager.LoadScene("WhisperGate", LoadSceneMode.Single);

            var gameType = Type.GetType("IdeaZoo.Runtime.IdeaZooGame, Assembly-CSharp");
            var worldType = Type.GetType("IdeaZoo.Runtime.WhisperGateWorld, Assembly-CSharp");
            var depthType = Type.GetType("IdeaZoo.Gameplay.GameplayDepthDirector, Assembly-CSharp");
            var performanceType = Type.GetType("IdeaZoo.Gameplay.GameplayPerformanceGovernor, Assembly-CSharp");
            Assert.NotNull(gameType, "IdeaZooGame type was not imported.");
            Assert.NotNull(worldType, "WhisperGateWorld type was not imported.");
            Assert.NotNull(depthType, "GameplayDepthDirector type was not imported.");
            Assert.NotNull(performanceType, "GameplayPerformanceGovernor type was not imported.");

            Component game = null;
            Component depth = null;
            var boundProperty = depthType.GetProperty("Bound");
            Assert.NotNull(boundProperty);

            for (var frame = 0; frame < 600; frame++)
            {
                game = FindSceneComponent(gameType);
                depth = FindAnyComponent(depthType);
                if (game != null && depth != null && (bool)boundProperty.GetValue(depth)) break;
                yield return null;
            }

            Assert.NotNull(game, "Whisper Gate did not boot its existing IdeaZooGame.");
            Assert.NotNull(depth, "Gameplay depth did not auto-boot.");
            Assert.IsTrue((bool)boundProperty.GetValue(depth));
            Assert.NotNull(depthType.GetProperty("Resources").GetValue(depth), "Gameplay resources were not initialized.");
            Assert.NotNull(depthType.GetProperty("MemoryState").GetValue(depth), "Persistent Zoo memory was not initialized.");
            Assert.NotNull(depthType.GetProperty("DepthHud").GetValue(depth), "Touch-first gameplay HUD was not initialized.");

            Assert.AreEqual(1, CountAnyComponents(depthType), "Gameplay depth must have one runtime owner.");
            Assert.AreEqual(1, CountAnyComponents(worldType), "The gameplay pass must reuse the existing Zoo world.");

            var maximumCardsField = performanceType.GetField("MaximumVisibleMemoryCards");
            Assert.NotNull(maximumCardsField);
            var maximumCards = (int)maximumCardsField.GetRawConstantValue();
            var transforms = Resources.FindObjectsOfTypeAll<Transform>();
            var memoryCardCount = transforms.Count(item => item != null
                && item.gameObject.scene.IsValid()
                && item.name.StartsWith("GameplayMemoryCard_", StringComparison.Ordinal));
            Assert.AreEqual(maximumCards, memoryCardCount, "Persistent archive cards were not pooled to the fixed budget.");
            Assert.IsTrue(transforms.Any(item => item != null
                && item.gameObject.scene.IsValid()
                && item.name == "GameplayDepthSafeArea"), "Gameplay HUD is not protected by the shared mobile safe area.");
        }

        private static Component FindSceneComponent(Type type)
        {
            return Resources.FindObjectsOfTypeAll(type).OfType<Component>()
                .FirstOrDefault(component => component != null && component.gameObject.scene.IsValid());
        }

        private static Component FindAnyComponent(Type type)
        {
            return Resources.FindObjectsOfTypeAll(type).OfType<Component>()
                .FirstOrDefault(component => component != null);
        }

        private static int CountAnyComponents(Type type)
        {
            return Resources.FindObjectsOfTypeAll(type).OfType<Component>()
                .Count(component => component != null && component.gameObject.scene.IsValid());
        }
    }
}
#endif
