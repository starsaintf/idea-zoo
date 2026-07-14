#if UNITY_EDITOR
using System.Linq;
using IdeaZoo.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace IdeaZoo.Tests
{
    public sealed class CloudArtEditorTests
    {
        [Test]
        public void FullCloudArtPackageImportsAndBakes()
        {
            CloudArtPrefabBaker.BakeAll();
            foreach (var id in CloudArtPrefabBaker.CharacterIds)
                Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IdeaZoo/Resources/IdeaZooArt/Characters/" + id + ".prefab"), id);
            foreach (var id in CloudArtPrefabBaker.CreatureIds)
                Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IdeaZoo/Resources/IdeaZooArt/Creatures/" + id + ".prefab"), id);
        }

        [Test]
        public void GeneratedArtStaysInsideMobileRendererBudget()
        {
            CloudArtPrefabBaker.BakeAll();
            var paths = CloudArtPrefabBaker.CharacterIds.Select(id => "Assets/IdeaZoo/Resources/IdeaZooArt/Characters/" + id + ".prefab")
                .Concat(CloudArtPrefabBaker.CreatureIds.Select(id => "Assets/IdeaZoo/Resources/IdeaZooArt/Creatures/" + id + ".prefab"));
            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.NotNull(prefab, path);
                Assert.LessOrEqual(prefab.GetComponentsInChildren<Renderer>(true).Length, 64, path + " renderer count");
                var materialCount = prefab.GetComponentsInChildren<Renderer>(true).SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().Count();
                Assert.LessOrEqual(materialCount, 32, path + " material count");
            }
        }
    }
}
#endif
