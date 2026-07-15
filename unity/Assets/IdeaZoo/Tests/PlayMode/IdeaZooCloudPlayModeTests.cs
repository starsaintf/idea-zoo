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
            Component heroDirector = null;
            Component heroWorldPass = null;
            Component heroCreaturePass = null;
            Component gameplayDepth = null;
            Component gameplayMemoryWorld = null;
            Component gameplayGovernor = null;
            Component playerExperience = null;
            Component playerExperienceWorld = null;
            Component playerAccessibility = null;

            var gameType = Type.GetType("IdeaZoo.Runtime.IdeaZooGame, Assembly-CSharp");
            var characterType = Type.GetType("IdeaZoo.Characters.CharacterProductionDirector, Assembly-CSharp");
            var campaignType = Type.GetType("IdeaZoo.Story.CampaignDirector, Assembly-CSharp");
            var intelligenceType = Type.GetType("IdeaZoo.Intelligence.IdeaIntelligenceRuntimeDirector, Assembly-CSharp");
            var mobileType = Type.GetType("IdeaZoo.Mobile.MobileProductionDirector, Assembly-CSharp");
            var heroDirectorType = Type.GetType("IdeaZoo.HeroSlice.CinematicHeroSliceDirector, Assembly-CSharp");
            var heroWorldType = Type.GetType("IdeaZoo.HeroSlice.HeroWorldProductionPass, Assembly-CSharp");
            var heroCreatureType = Type.GetType("IdeaZoo.HeroSlice.HeroCreatureTransformationDirector, Assembly-CSharp");
            var cameraRigType = Type.GetType("IdeaZoo.Presentation.PresentationCameraRig, Assembly-CSharp");
            var gameplayDepthType = Type.GetType("IdeaZoo.Gameplay.GameplayDepthDirector, Assembly-CSharp");
            var gameplayMemoryWorldType = Type.GetType("IdeaZoo.Gameplay.GameplayMemoryWorldPass, Assembly-CSharp");
            var gameplayGovernorType = Type.GetType("IdeaZoo.Gameplay.GameplayPerformanceGovernor, Assembly-CSharp");
            var gameplayHudType = Type.GetType("IdeaZoo.Gameplay.GameplayDepthHud, Assembly-CSharp");
            var playerExperienceType = Type.GetType("IdeaZoo.PlayerExperience.PlayerExperienceDirector, Assembly-CSharp");
            var playerExperienceWorldType = Type.GetType("IdeaZoo.PlayerExperience.PlayerExperienceWorldPass, Assembly-CSharp");
            var playerAccessibilityType = Type.GetType("IdeaZoo.PlayerExperience.PlayerExperienceAccessibilityController, Assembly-CSharp");
            var playerHudType = Type.GetType("IdeaZoo.PlayerExperience.PlayerExperienceHud, Assembly-CSharp");

            Assert.NotNull(gameType, "IdeaZooGame type was not imported.");
            Assert.NotNull(characterType, "Character production type was not imported.");
            Assert.NotNull(campaignType, "Campaign production type was not imported.");
            Assert.NotNull(intelligenceType, "Idea Lab intelligence type was not imported.");
            Assert.NotNull(mobileType, "Mobile production type was not imported.");
            Assert.NotNull(heroDirectorType, "Cinematic hero-slice director type was not imported.");
            Assert.NotNull(heroWorldType, "Hero world pass type was not imported.");
            Assert.NotNull(heroCreatureType, "Hero creature transformation type was not imported.");
            Assert.NotNull(cameraRigType, "Presentation camera rig type was not imported.");
            Assert.NotNull(gameplayDepthType, "Gameplay depth director type was not imported.");
            Assert.NotNull(gameplayMemoryWorldType, "Gameplay memory world type was not imported.");
            Assert.NotNull(gameplayGovernorType, "Gameplay performance governor type was not imported.");
            Assert.NotNull(gameplayHudType, "Gameplay depth HUD type was not imported.");
            Assert.NotNull(playerExperienceType, "Player Experience V1 director type was not imported.");
            Assert.NotNull(playerExperienceWorldType, "Player Experience consequence world type was not imported.");
            Assert.NotNull(playerAccessibilityType, "Player Experience accessibility type was not imported.");
            Assert.NotNull(playerHudType, "Player Experience HUD type was not imported.");

            for (var frame = 0; frame < 600; frame++)
            {
                game = FindSceneComponent(gameType);
                characterDirector = FindAnyComponent(characterType);
                campaignDirector = FindAnyComponent(campaignType);
                intelligenceDirector = FindAnyComponent(intelligenceType);
                mobileDirector = FindAnyComponent(mobileType);
                heroDirector = FindAnyComponent(heroDirectorType);
                heroWorldPass = FindAnyComponent(heroWorldType);
                heroCreaturePass = FindAnyComponent(heroCreatureType);
                gameplayDepth = FindAnyComponent(gameplayDepthType);
                gameplayMemoryWorld = FindAnyComponent(gameplayMemoryWorldType);
                gameplayGovernor = FindAnyComponent(gameplayGovernorType);
                playerExperience = FindAnyComponent(playerExperienceType);
                playerExperienceWorld = FindAnyComponent(playerExperienceWorldType);
                playerAccessibility = FindAnyComponent(playerAccessibilityType);
                if (game != null && characterDirector != null && campaignDirector != null && intelligenceDirector != null
                    && mobileDirector != null && heroDirector != null && heroWorldPass != null && heroCreaturePass != null
                    && gameplayDepth != null && gameplayMemoryWorld != null && gameplayGovernor != null
                    && playerExperience != null && playerExperienceWorld != null && playerAccessibility != null) break;
                yield return null;
            }

            Assert.NotNull(game, "Whisper Gate did not auto-boot IdeaZooGame.");
            Assert.NotNull(characterDirector, "Character production director did not boot.");
            Assert.NotNull(campaignDirector, "Campaign director did not boot.");
            Assert.NotNull(intelligenceDirector, "Idea Lab intelligence director did not boot.");
            Assert.NotNull(mobileDirector, "Mobile production director did not boot.");
            Assert.NotNull(heroDirector, "Cinematic hero-slice director did not boot.");
            Assert.NotNull(heroWorldPass, "Hero world production pass did not boot.");
            Assert.NotNull(heroCreaturePass, "Hero creature transformation pass did not boot.");
            Assert.NotNull(gameplayDepth, "Gameplay depth director did not boot.");
            Assert.NotNull(gameplayMemoryWorld, "Persistent gameplay memory did not attach to the Zoo.");
            Assert.NotNull(gameplayGovernor, "Gameplay performance governor did not boot.");
            Assert.NotNull(playerExperience, "Player Experience V1 did not boot.");
            Assert.NotNull(playerExperienceWorld, "Visible consequence world pass did not attach.");
            Assert.NotNull(playerAccessibility, "Accessibility controller did not boot.");

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
                var heroRoot = FindChild(world.transform, "HERO_SLICE_WORLD");
                var memoryRoot = FindChild(world.transform, "GAMEPLAY_MEMORY_ARCHIVE");
                var consequenceRoot = FindChild(world.transform, "PLAYER_EXPERIENCE_CONSEQUENCES");
                if (specialists >= 6 && jury != null && authored != null && productionKeeper != null
                    && creatureRig != null && heroRoot != null && memoryRoot != null && consequenceRoot != null) break;
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

            var heroRootTransform = FindChild(world.transform, "HERO_SLICE_WORLD");
            Assert.NotNull(heroRootTransform, "Hero-slice world root did not boot.");
            Assert.NotNull(FindChild(heroRootTransform, "HERO_ZOOENTRANCE"), "Hero Zoo Entrance is missing.");
            Assert.NotNull(FindChild(heroRootTransform, "HERO_LANTERNFIELDS"), "Hero Lantern Fields is missing.");
            Assert.NotNull(FindChild(heroRootTransform, "HERO_SILENTSTACKS"), "Hero Silent Stacks is missing.");
            Assert.NotNull(FindChild(heroRootTransform, "HERO_EVIDENCEFORGE"), "Hero Evidence Forge is missing.");
            Assert.AreEqual(1, CountAnyComponents(cameraRigType), "More than one presentation camera rig is active; case cinematics could fight each other.");

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

            var boundProperty = gameplayDepthType.GetProperty("Bound");
            var resourcesProperty = gameplayDepthType.GetProperty("Resources");
            var memoryStateProperty = gameplayDepthType.GetProperty("MemoryState");
            var depthHudProperty = gameplayDepthType.GetProperty("DepthHud");
            Assert.NotNull(boundProperty, "Gameplay depth Bound property is missing.");
            Assert.IsTrue((bool)boundProperty.GetValue(gameplayDepth), "Gameplay depth did not bind to the live case loop.");
            Assert.NotNull(resourcesProperty.GetValue(gameplayDepth), "Gameplay resources were not initialized.");
            Assert.NotNull(memoryStateProperty.GetValue(gameplayDepth), "Persistent Zoo memory did not initialize.");
            Assert.NotNull(depthHudProperty.GetValue(gameplayDepth), "Touch-first gameplay HUD did not initialize.");
            Assert.AreEqual(1, CountAnyComponents(gameplayDepthType), "More than one gameplay-depth owner is active.");
            Assert.AreEqual(world.gameObject, gameplayMemoryWorld.gameObject, "Gameplay memory created a second world instead of attaching to the existing Zoo.");
            Assert.NotNull(FindChild(world.transform, "GAMEPLAY_MEMORY_ARCHIVE"), "Silent Stacks memory archive did not boot.");

            var experienceBound = playerExperienceType.GetProperty("Bound");
            var experienceState = playerExperienceType.GetProperty("State");
            var experienceHud = playerExperienceType.GetProperty("ExperienceHud");
            Assert.NotNull(experienceBound);
            Assert.IsTrue((bool)experienceBound.GetValue(playerExperience), "Player Experience V1 did not bind to the live game.");
            Assert.NotNull(experienceState.GetValue(playerExperience), "Player Experience persistence did not initialize.");
            Assert.NotNull(experienceHud.GetValue(playerExperience), "Player Experience HUD did not initialize.");
            Assert.AreEqual(1, CountAnyComponents(playerExperienceType), "More than one Player Experience owner is active.");
            Assert.AreEqual(world.gameObject, playerExperienceWorld.gameObject, "Player Experience created a second Zoo world.");
            Assert.NotNull(FindChild(world.transform, "PLAYER_EXPERIENCE_CONSEQUENCES"), "Visible consequence monuments did not boot.");

            var maximumCards = (int)gameplayGovernorType.GetField("MaximumVisibleMemoryCards").GetRawConstantValue();
            var maximumConsequences = (int)playerExperienceWorldType.GetField("MaximumVisibleConsequences").GetRawConstantValue();
            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            var memoryCardCount = allTransforms.Count(item => item != null
                && item.gameObject.scene.IsValid()
                && item.name.StartsWith("GameplayMemoryCard_", StringComparison.Ordinal));
            var consequenceCount = allTransforms.Count(item => item != null
                && item.gameObject.scene.IsValid()
                && item.name.StartsWith("PlayerConsequence_", StringComparison.Ordinal));
            Assert.AreEqual(maximumCards, memoryCardCount, "Gameplay memory cards were not pooled to the fixed budget.");
            Assert.AreEqual(maximumConsequences, consequenceCount, "Visible consequences were not pooled to the fixed budget.");
            Assert.IsTrue(allTransforms.Any(item => item != null && item.gameObject.scene.IsValid() && item.name == "GameplayDepthSafeArea"),
                "Gameplay HUD is not protected by the shared mobile safe area.");
            Assert.IsTrue(allTransforms.Any(item => item != null && item.gameObject.scene.IsValid() && item.name == "PlayerExperienceSafeArea"),
                "Player Experience HUD is not protected by the shared mobile safe area.");
            Assert.IsTrue(allTransforms.Any(item => item != null && item.gameObject.scene.IsValid() && item.name == "TactileTokenPool"),
                "Tactile encounter preparation did not boot.");
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

        private static int CountAnyComponents(Type type)
        {
            if (type == null) return 0;
            return Resources.FindObjectsOfTypeAll(type).OfType<Component>().Count(component => component != null && component.gameObject.scene.IsValid());
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
