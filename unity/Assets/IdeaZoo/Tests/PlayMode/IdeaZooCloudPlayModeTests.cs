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

            Component game = null;
            Component characterDirector = null;
            Component campaignDirector = null;
            Component intelligenceDirector = null;
            Component mobileDirector = null;
            var gameType = Type.GetType("IdeaZoo.Runtime.IdeaZooGame, Assembly-CSharp");
            var characterType = Type.GetType("IdeaZoo.Characters.CharacterProductionDirector, Assembly-CSharp");
            var campaignType = Type.GetType("IdeaZoo.Story.CampaignDirector, Assembly-CSharp");
            var intelligenceType = Type.GetType("IdeaZoo.Intelligence.IdeaIntelligenceRuntimeDirector, Assembly-CSharp");
            var mobileType = Type.GetType("IdeaZoo.Mobile.MobileProductionDirector, Assembly-CSharp");

            Assert.NotNull(gameType, "IdeaZooGame type was not imported.");
            Assert.NotNull(characterType, "Character production type was not imported.");
            Assert.NotNull(campaignType, "Campaign production type was not imported.");
            Assert.NotNull(intelligenceType, "Idea Lab intelligence type was not imported.");
            Assert.NotNull(mobileType, "Mobile production type was not imported.");

            for (var frame = 0; frame < 480; frame++)
            {
                game = FindSceneComponent(gameType);
                characterDirector = FindAnyComponent(characterType);
                campaignDirector = FindAnyComponent(campaignType);
                intelligenceDirector = FindAnyComponent(intelligenceType);
                mobileDirector = FindAnyComponent(mobileType);
                if (game != null && characterDirector != null && campaignDirector != null && intelligenceDirector != null && mobileDirector != null) break;
                yield return null;
            }

            Assert.NotNull(game, "Whisper Gate did not auto-boot IdeaZooGame.");
            Assert.NotNull(characterDirector, "Character production director did not boot.");
            Assert.NotNull(campaignDirector, "Campaign director did not boot.");
            Assert.NotNull(intelligenceDirector, "Idea Lab intelligence director did not boot.");
            Assert.NotNull(mobileDirector, "Mobile production director did not boot.");

            var worldProperty = gameType.GetProperty("World");
            var keeperProperty = gameType.GetProperty("Keeper");
            var creatureProperty = gameType.GetProperty("Creature");
            var hudProperty = gameType.GetProperty("Hud");
            Assert.NotNull(worldProperty, "World property is missing.");
            Assert.NotNull(keeperProperty, "Keeper property is missing.");
            Assert.NotNull(creatureProperty, "Creature property is missing.");
            Assert.NotNull(hudProperty, "HUD property is missing.");

            var world = worldProperty.GetValue(game) as Component;
            var keeper = keeperProperty.GetValue(game) as Component;
            var creature = creatureProperty.GetValue(game) as Component;
            Assert.NotNull(world, "Runtime world was not constructed.");
            Assert.NotNull(keeper, "Keeper was not constructed.");
            Assert.NotNull(creature, "Creature assembler was not constructed.");
            Assert.NotNull(hudProperty.GetValue(game), "HUD was not constructed.");

            for (var frame = 0; frame < 480; frame++)
            {
                var specialists = CountByTypeName(world.transform, "IdeaZoo.Presentation.ProceduralSpecialist");
                var jury = FindChild(world.transform, "CHILDRENS_JURY");
                var authored = FindChild(world.transform, "AUTHORED_ENVIRONMENT_KIT");
                var productionKeeper = FindChild(keeper.transform, "PRODUCTION_KEEPER_VISUAL");
                var creatureRig = creature.GetComponent(Type.GetType("IdeaZoo.Creatures.CreatureProductionRig, Assembly-CSharp"));
                if (specialists >= 6 && jury != null && authored != null && productionKeeper != null && creatureRig != null) break;
                yield return null;
            }

            var stationCount = CountByTypeName(world.transform, "IdeaZoo.Runtime.IdeaStation");
            var specialistCount = CountByTypeName(world.transform, "IdeaZoo.Presentation.ProceduralSpecialist");
            Assert.GreaterOrEqual(stationCount, 11, "Runtime world is missing evidence or ruling stations.");
            Assert.GreaterOrEqual(specialistCount, 6, "Runtime world is missing specialist characters.");
            Assert.NotNull(FindChild(world.transform, "01_WHISPER_GATE"), "Whisper Gate department is missing.");
            Assert.NotNull(FindChild(world.transform, "10_DECISION_GARDEN"), "Decision Garden department is missing.");
            Assert.NotNull(FindChild(world.transform, "AUTHORED_ENVIRONMENT_KIT"), "Authored environment kit did not boot.");
            Assert.NotNull(FindChild(world.transform, "CHILDRENS_JURY"), "Children's Jury did not enter the institution.");
            Assert.NotNull(FindChild(keeper.transform, "PRODUCTION_KEEPER_VISUAL"), "Production Keeper customization did not boot.");

            var creatureRigType = Type.GetType("IdeaZoo.Creatures.CreatureProductionRig, Assembly-CSharp");
            var lifecycleType = Type.GetType("IdeaZoo.Creatures.CreatureProductionLifecycle, Assembly-CSharp");
            Assert.NotNull(creatureRigType, "Creature production rig type was not imported.");
            Assert.NotNull(lifecycleType, "Creature lifecycle type was not imported.");
            Assert.NotNull(creature.GetComponent(creatureRigType), "Living idea has no production creature rig.");
            Assert.NotNull(FindAnyComponent(lifecycleType), "Creature lifecycle protection did not boot.");

            var stateProperty = campaignType.GetProperty("State");
            Assert.NotNull(stateProperty, "Campaign state property is missing.");
            Assert.NotNull(stateProperty.GetValue(campaignDirector), "Campaign state did not load.");

            var vaultProperty = intelligenceType.GetProperty("Vault");
            Assert.NotNull(vaultProperty, "Evidence vault property is missing.");
            Assert.NotNull(vaultProperty.GetValue(intelligenceDirector), "Private evidence vault did not initialize.");

            var snapshotMethod = mobileType.GetMethod("Snapshot");
            Assert.NotNull(snapshotMethod, "Mobile telemetry snapshot method is missing.");
            Assert.NotNull(snapshotMethod.Invoke(mobileDirector, null), "Mobile telemetry did not produce a report.");
        }

        private static Component FindSceneComponent(Type type)
        {
            if (type == null) return null;
            return Resources.FindObjectsOfTypeAll(type).OfType<Component>().FirstOrDefault(component => component != null && component.gameObject.scene.IsValid());
        }

        private static Component FindAnyComponent(Type type)
        {
            if (type == null) return null;
            return Resources.FindObjectsOfTypeAll(type).OfType<Component>().FirstOrDefault(component => component != null);
        }

        private static int CountByTypeName(Transform root, string fullName)
        {
            return root.GetComponentsInChildren<Component>(true).Count(component => component != null && component.GetType().FullName == fullName);
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }
    }
}
