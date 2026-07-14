#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace IdeaZoo.Tests.EditMode
{
    public sealed class HeroSliceEditorTests
    {
        private const string PrefabPath = "Assets/IdeaZoo/HeroSlice/Prefabs/CinematicHeroSlice.prefab";

        [Test]
        public void HeroSliceRuntimeAssembliesImport()
        {
            Assert.NotNull(Type.GetType("IdeaZoo.HeroSlice.CinematicHeroSliceDirector, Assembly-CSharp"));
            Assert.NotNull(Type.GetType("IdeaZoo.HeroSlice.CinematicHeroSlicePrefabAnchor, Assembly-CSharp"));
            Assert.NotNull(Type.GetType("IdeaZoo.HeroSlice.HeroSliceReviewSceneAnchor, Assembly-CSharp"));
            Assert.NotNull(Type.GetType("IdeaZoo.HeroSlice.HeroWorldProductionPass, Assembly-CSharp"));
            Assert.NotNull(Type.GetType("IdeaZoo.HeroSlice.HeroCreatureTransformationDirector, Assembly-CSharp"));
            Assert.NotNull(Type.GetType("IdeaZoo.HeroSlice.HeroCharacterPerformanceDirector, Assembly-CSharp"));
            Assert.NotNull(Type.GetType("IdeaZoo.EditorTools.HeroSliceSceneBaker, Assembly-CSharp-Editor"));
        }

        [Test]
        public void HeroSliceBakerCreatesReviewableAssets()
        {
            Assert.IsTrue(EditorApplication.ExecuteMenuItem("Idea Zoo/Hero Slice/Bake Complete Production Pass"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.NotNull(prefab, "Hero-slice runtime prefab was not baked.");
            var anchorType = Type.GetType("IdeaZoo.HeroSlice.CinematicHeroSlicePrefabAnchor, Assembly-CSharp");
            Assert.NotNull(anchorType, "Serialization-safe prefab anchor type was not imported.");
            Assert.NotNull(prefab.GetComponent(anchorType), "Baked prefab lost its serialization-safe anchor.");

            foreach (var district in new[] { "ZooEntrance", "LanternFields", "SilentStacks", "EvidenceForge" })
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/IdeaZoo/Scenes/Hero_" + district + ".unity");
                Assert.NotNull(scene, "Hero review scene was not baked for " + district + ".");
            }

            Assert.IsTrue(EditorApplication.ExecuteMenuItem("Idea Zoo/Hero Slice/Validate Production Pass"));
        }

        [Test]
        public void HeroSliceContainsAllNarrativeTransformationStages()
        {
            var enumType = Type.GetType("IdeaZoo.HeroSlice.HeroCreatureStage, Assembly-CSharp");
            Assert.NotNull(enumType);
            var names = Enum.GetNames(enumType);
            CollectionAssert.AreEquivalent(
                new[] { "Unproven", "Observed", "Tested", "Trusted", "Burdened", "Transformed" },
                names);
        }

        [Test]
        public void EqualHeroSurfacesShareMaterialInstances()
        {
            var utilityType = Type.GetType("IdeaZoo.HeroSlice.HeroSliceUtility, Assembly-CSharp");
            Assert.NotNull(utilityType, "HeroSliceUtility type was not imported.");
            var primitive = utilityType.GetMethod("Primitive", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(primitive, "Hero primitive factory is missing.");

            var root = new GameObject("HeroMaterialSharingTest");
            try
            {
                var color = new Color(0.16f, 0.27f, 0.38f, 1f);
                var first = primitive.Invoke(null, new object[]
                {
                    root.transform, "UniqueObjectA", PrimitiveType.Cube, Vector3.zero, Vector3.one,
                    color, 0.4f, 0.7f, true
                }) as GameObject;
                var second = primitive.Invoke(null, new object[]
                {
                    root.transform, "UniqueObjectB", PrimitiveType.Sphere, Vector3.right, Vector3.one,
                    color, 0.4f, 0.7f, true
                }) as GameObject;
                Assert.NotNull(first);
                Assert.NotNull(second);
                Assert.AreSame(
                    first.GetComponent<Renderer>().sharedMaterial,
                    second.GetComponent<Renderer>().sharedMaterial,
                    "Object names must not split otherwise identical hero materials into separate instances.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
