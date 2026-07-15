#if UNITY_EDITOR
using System.Collections;
using IdeaZoo.Gameplay;
using IdeaZoo.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IdeaZoo.Tests.PlayMode
{
    public sealed class GameplayDepthPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameplayDepthBootsOnceAndReusesTheExistingWorld()
        {
            var game = Object.FindFirstObjectByType<IdeaZooGame>();
            if (game == null)
            {
                var root = new GameObject("GameplayDepthTestGame");
                game = root.AddComponent<IdeaZooGame>();
            }

            GameplayDepthDirector depth = null;
            for (var frame = 0; frame < 300; frame++)
            {
                depth = Object.FindFirstObjectByType<GameplayDepthDirector>();
                if (depth != null && depth.Bound) break;
                yield return null;
            }

            Assert.NotNull(game);
            Assert.NotNull(depth);
            Assert.IsTrue(depth.Bound);
            Assert.NotNull(depth.Resources);
            Assert.NotNull(depth.MemoryState);
            Assert.NotNull(depth.DepthHud);

            var directors = Object.FindObjectsByType<GameplayDepthDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, directors.Length);

            var worlds = Object.FindObjectsByType<WhisperGateWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, worlds.Length);

            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var memoryCardCount = 0;
            for (var i = 0; i < transforms.Length; i++)
                if (transforms[i].name.StartsWith("GameplayMemoryCard_")) memoryCardCount++;
            Assert.AreEqual(GameplayPerformanceGovernor.MaximumVisibleMemoryCards, memoryCardCount);
        }
    }
}
#endif
