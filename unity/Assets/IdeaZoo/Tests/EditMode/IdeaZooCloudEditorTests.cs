#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace IdeaZoo.Tests.EditMode
{
    public sealed class IdeaZooCloudEditorTests
    {
        private const string PrefabPath = "Assets/IdeaZoo/Art/Prefabs/WhisperGateDistrict.prefab";
        private const string ReviewScenePath = "Assets/IdeaZoo/Scenes/WhisperGatePresentationReview.unity";

        [Test]
        public void ProductionAssembliesImport()
        {
            Assert.NotNull(Type.GetType("IdeaZoo.Core.IdeaZooCaseDirector, Assembly-CSharp"), "Idea domain did not compile into Assembly-CSharp.");
            Assert.NotNull(Type.GetType("IdeaZoo.Runtime.IdeaZooGame, Assembly-CSharp"), "Runtime did not compile into Assembly-CSharp.");
            Assert.NotNull(Type.GetType("IdeaZoo.Presentation.CivicWorldArtPass, Assembly-CSharp"), "Presentation layer did not compile into Assembly-CSharp.");
            Assert.NotNull(Type.GetType("IdeaZoo.EditorTools.IdeaZooPresentationBaker, Assembly-CSharp-Editor"), "Editor baker did not compile.");
        }

        [Test]
        public void PresentationBakerCreatesReviewableAssets()
        {
            Assert.IsTrue(EditorApplication.ExecuteMenuItem("Idea Zoo/Presentation/Bake District Prefab"), "District bake menu item was unavailable.");
            Assert.IsTrue(EditorApplication.ExecuteMenuItem("Idea Zoo/Presentation/Bake Review Scene"), "Review-scene bake menu item was unavailable.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var reviewScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewScenePath);
            Assert.NotNull(prefab, "Cloud bake did not produce the district prefab.");
            Assert.NotNull(reviewScene, "Cloud bake did not produce the presentation review scene.");

            var stations = prefab.GetComponentsInChildren<Component>(true);
            var stationCount = 0;
            var specialistCount = 0;
            foreach (var component in stations)
            {
                if (component == null) continue;
                var fullName = component.GetType().FullName;
                if (fullName == "IdeaZoo.Runtime.IdeaStation") stationCount++;
                if (fullName == "IdeaZoo.Presentation.ProceduralSpecialist") specialistCount++;
            }

            Assert.GreaterOrEqual(stationCount, 11, "Baked district is missing evidence or ruling stations.");
            Assert.GreaterOrEqual(specialistCount, 6, "Baked district is missing specialist characters.");
            Assert.IsTrue(EditorApplication.ExecuteMenuItem("Idea Zoo/Presentation/Validate Baked Assets"), "Baked-asset validation menu item was unavailable.");
        }
    }
}
#endif
