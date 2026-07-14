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
        private const string ManifestPath = "Assets/IdeaZoo/HeroSlice/HERO_SLICE_MANIFEST.json";

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

            var manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
            Assert.NotNull(manifest, "Hero-slice manifest was not baked.");
            StringAssert.Contains("\"heroCreature\": \"AllCreatureFamilies\"", manifest.text,
                "Manifest incorrectly narrows the transformation pass to one creature family.");

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
        public void FinalRulingsKeepDistinctNarrativeMeanings()
        {
            var semanticsType = Type.GetType("IdeaZoo.HeroSlice.HeroRulingSemantics, Assembly-CSharp");
            var rulingType = Type.GetType("IdeaZoo.Core.Ruling, Assembly-CSharp");
            Assert.NotNull(semanticsType);
            Assert.NotNull(rulingType);

            var isHopeful = semanticsType.GetMethod("IsHopeful", BindingFlags.Public | BindingFlags.Static);
            var isBreak = semanticsType.GetMethod("IsBreak", BindingFlags.Public | BindingFlags.Static);
            var isHibernate = semanticsType.GetMethod("IsHibernate", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(isHopeful);
            Assert.NotNull(isBreak);
            Assert.NotNull(isHibernate);

            var build = Enum.Parse(rulingType, "Build");
            var hibernate = Enum.Parse(rulingType, "Hibernate");
            var destroy = Enum.Parse(rulingType, "Break");
            Assert.IsTrue((bool)isHopeful.Invoke(null, new[] { build }));
            Assert.IsFalse((bool)isBreak.Invoke(null, new[] { hibernate }));
            Assert.IsTrue((bool)isHibernate.Invoke(null, new[] { hibernate }));
            Assert.IsTrue((bool)isBreak.Invoke(null, new[] { destroy }));
            Assert.IsFalse((bool)isHibernate.Invoke(null, new[] { destroy }));
        }

        [Test]
        public void EqualHeroSurfacesShareMaterialInstances()
        {
            var utilityType = Type.GetType("IdeaZoo.HeroSlice.HeroSliceUtility, Assembly-CSharp");
            Assert.NotNull(utilityType, "HeroSliceUtility type was not imported.");
            var materialFor = utilityType.GetMethod("MaterialFor", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(materialFor, "Hero material cache entry point is missing.");

            var color = new Color(0.16f, 0.27f, 0.38f, 1f);
            var first = materialFor.Invoke(null, new object[] { color, 0.4f, 0.7f }) as Material;
            var second = materialFor.Invoke(null, new object[] { color, 0.4f, 0.7f }) as Material;
            Assert.NotNull(first);
            Assert.AreSame(first, second, "Equal surface properties must resolve to one shared material instance.");
        }

        [Test]
        public void TransparentHeroSurfacesUseTransparentRendering()
        {
            var utilityType = Type.GetType("IdeaZoo.HeroSlice.HeroSliceUtility, Assembly-CSharp");
            Assert.NotNull(utilityType, "HeroSliceUtility type was not imported.");
            var materialFor = utilityType.GetMethod("MaterialFor", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(materialFor, "Hero material cache entry point is missing.");

            var glass = materialFor.Invoke(null, new object[] { new Color(0.10f, 0.42f, 0.62f, 0.35f), 0.05f, 0.92f }) as Material;
            Assert.NotNull(glass);
            Assert.GreaterOrEqual(glass.renderQueue, 3000, "Glass material remained in the opaque render queue.");
            Assert.AreEqual("Transparent", glass.GetTag("RenderType", false), "Glass material is not tagged as transparent.");
            if (glass.HasProperty("_Surface")) Assert.AreEqual(1f, glass.GetFloat("_Surface"), 0.001f);
            if (glass.HasProperty("_ZWrite")) Assert.AreEqual(0f, glass.GetFloat("_ZWrite"), 0.001f);
        }

        [Test]
        public void ImportedCreatureEmissionEnablesShaderKeywordWithoutRendererMaterialLeak()
        {
            var utilityType = Type.GetType("IdeaZoo.HeroSlice.HeroSliceUtility, Assembly-CSharp");
            Assert.NotNull(utilityType);
            var setEmissionOnly = utilityType.GetMethod("SetEmissionOnly", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(setEmissionOnly);

            var node = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var renderer = node.GetComponent<Renderer>();
                Assert.NotNull(renderer);
                var original = renderer.sharedMaterial;
                setEmissionOnly.Invoke(null, new object[] { renderer, Color.cyan, 1.2f });
                Assert.NotNull(renderer.sharedMaterial);
                Assert.AreNotSame(original, renderer.sharedMaterial, "Imported source material was mutated globally.");
                Assert.IsTrue(renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"),
                    "Imported creature emission color was set without enabling the shader emission variant.");
                StringAssert.Contains("_HeroEmission_", renderer.sharedMaterial.name,
                    "Emission variant was not produced by the explicit cached material path.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(node);
            }
        }
    }
}
#endif
