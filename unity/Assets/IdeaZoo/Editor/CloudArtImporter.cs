#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdeaZoo.EditorTools
{
    public sealed class CloudArtModelPostprocessor : AssetPostprocessor
    {
        private const string Root = "Assets/IdeaZoo/Art/CloudGenerated/Models/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.preserveHierarchy = true;
            importer.globalScale = 1f;
            importer.useFileScale = true;
        }
    }

    public static class CloudArtPrefabBaker
    {
        private const string ModelRoot = "Assets/IdeaZoo/Art/CloudGenerated/Models";
        private const string ResourceRoot = "Assets/IdeaZoo/Resources/IdeaZooArt";

        public static readonly string[] CharacterIds =
        {
            "Keeper_0", "Keeper_1", "Keeper_2",
            "Mara_Rook", "Toma_Reed", "Sefu_Anik", "Elian_Thread", "Sen_Osei", "Nara_Voss",
            "Lio_Jury", "Amara_Jury", "Kweku_Jury"
        };

        public static readonly string[] CreatureIds = { "Avian", "BurdenBeast", "Lantern", "Serpentine", "Choir" };

        [MenuItem("Idea Zoo/Cloud Art/Bake All Prefabs")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(ResourceRoot + "/Characters");
            Directory.CreateDirectory(ResourceRoot + "/Creatures");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var id in CharacterIds) Bake(id, "Characters");
            foreach (var id in CreatureIds) Bake(id, "Creatures");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void Bake(string id, string category)
        {
            var modelPath = ModelRoot + "/" + category + "/" + id + ".fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("Cloud-generated model is missing: " + modelPath);
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = id;
            var metadata = instance.GetComponent<CloudArtAssetMetadata>() ?? instance.AddComponent<CloudArtAssetMetadata>();
            metadata.AssetId = id;
            metadata.Category = category;
            ConfigureRenderers(instance);
            ValidateHierarchy(instance.transform, id, category);
            var prefabPath = ResourceRoot + "/" + category + "/" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void ConfigureRenderers(GameObject instance)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
            }
            foreach (var skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinned.updateWhenOffscreen = false;
                skinned.quality = SkinQuality.Bone4;
            }
        }

        private static void ValidateHierarchy(Transform root, string id, string category)
        {
            var names = root.GetComponentsInChildren<Transform>(true).Select(item => item.name).ToHashSet();
            if (category == "Characters")
            {
                foreach (var bone in new[] { "Hips", "Spine", "Chest", "Neck", "Head", "LeftUpperArm", "RightUpperArm", "LeftUpperLeg", "RightUpperLeg" })
                    if (!names.Contains(bone)) throw new InvalidOperationException(id + " is missing humanoid rig node " + bone);
                if (!names.Contains("HeadSocket") || !names.Contains("RightHandSocket") || !names.Contains("LeftHandSocket"))
                    throw new InvalidOperationException(id + " is missing required character sockets.");
            }
            else
            {
                foreach (var socket in new[] { "HeadSocket", "AppetiteSocket", "BurdenSocket", "GuardrailRoot", "TailSocket", "EffectRoot" })
                    if (!names.Contains(socket)) throw new InvalidOperationException(id + " is missing creature socket " + socket);
            }
            if (root.GetComponentsInChildren<Renderer>(true).Length < 3)
                throw new InvalidOperationException(id + " does not contain enough rendered parts.");
        }
    }
}
#endif
