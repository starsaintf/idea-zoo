#if UNITY_EDITOR
using System;
using System.IO;
using IdeaZoo.HeroSlice;
using IdeaZoo.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdeaZoo.EditorTools
{
    public static class HeroSliceSceneBaker
    {
        public const string PrefabPath = "Assets/IdeaZoo/HeroSlice/Prefabs/CinematicHeroSlice.prefab";
        public const string SceneRoot = "Assets/IdeaZoo/Scenes";

        [MenuItem("Idea Zoo/Hero Slice/Bake Runtime Prefab")]
        public static void BakeRuntimePrefab()
        {
            EnsureFolder("Assets/IdeaZoo/HeroSlice");
            EnsureFolder("Assets/IdeaZoo/HeroSlice/Prefabs");

            var root = new GameObject("CINEMATIC_HERO_SLICE");
            root.AddComponent<CinematicHeroSliceDirector>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Idea Zoo/Hero Slice/Bake Four Review Scenes")]
        public static void BakeReviewScenes()
        {
            EnsureFolder(SceneRoot);
            foreach (HeroDistrictId district in Enum.GetValues(typeof(HeroDistrictId)))
                BakeScene(district);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Idea Zoo/Hero Slice/Bake Complete Production Pass")]
        public static void BakeComplete()
        {
            BakeRuntimePrefab();
            BakeReviewScenes();
            WriteManifest();
            ValidateBakedAssets();
        }

        [MenuItem("Idea Zoo/Hero Slice/Validate Production Pass")]
        public static void ValidateBakedAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Hero-slice runtime prefab is missing.");
            if (prefab.GetComponent<CinematicHeroSliceDirector>() == null)
                throw new InvalidOperationException("Hero-slice runtime prefab is missing CinematicHeroSliceDirector.");

            foreach (HeroDistrictId district in Enum.GetValues(typeof(HeroDistrictId)))
            {
                var path = ScenePath(district);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new InvalidOperationException("Hero review scene is missing: " + path);
            }

            var runtimeType = Type.GetType("IdeaZoo.HeroSlice.HeroCreatureTransformationDirector, Assembly-CSharp");
            var worldType = Type.GetType("IdeaZoo.HeroSlice.HeroWorldProductionPass, Assembly-CSharp");
            var performanceType = Type.GetType("IdeaZoo.HeroSlice.HeroCharacterPerformanceDirector, Assembly-CSharp");
            if (runtimeType == null || worldType == null || performanceType == null)
                throw new InvalidOperationException("Hero-slice runtime types did not compile into Assembly-CSharp.");

            Debug.Log("HERO_SLICE_BAKED_ASSETS_VALID");
        }

        private static void BakeScene(HeroDistrictId district)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("HERO_REVIEW_" + district.ToString().ToUpperInvariant());
            root.AddComponent<IdeaZooGame>();
            var review = root.AddComponent<HeroSliceReviewBootstrap>();
            review.District = district;
            review.AutoFrame = true;

            var direction = new GameObject("Review_Directional_Key").AddComponent<Light>();
            direction.type = LightType.Directional;
            direction.color = new Color(1f, 0.76f, 0.46f);
            direction.intensity = 1.15f;
            direction.shadows = LightShadows.Soft;
            direction.transform.rotation = Quaternion.Euler(42f, -34f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath(district));
        }

        private static void WriteManifest()
        {
            EnsureFolder("Assets/IdeaZoo/HeroSlice");
            var path = Path.Combine(Application.dataPath, "IdeaZoo/HeroSlice/HERO_SLICE_MANIFEST.json");
            var json =
                "{\n" +
                "  \"schema\": 1,\n" +
                "  \"productionPass\": \"cinematic-hero-slice-v1\",\n" +
                "  \"districts\": [\"ZooEntrance\", \"LanternFields\", \"SilentStacks\", \"EvidenceForge\"],\n" +
                "  \"heroCharacters\": [\"Keeper\", \"Mara_Rook\"],\n" +
                "  \"heroCreature\": \"Lantern\",\n" +
                "  \"creatureStages\": [\"Unproven\", \"Observed\", \"Tested\", \"Trusted\", \"Burdened\", \"Transformed\"],\n" +
                "  \"mobileTargets\": {\"eco\": 30, \"balanced\": 45, \"quality\": 60}\n" +
                "}\n";
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset("Assets/IdeaZoo/HeroSlice/HERO_SLICE_MANIFEST.json", ImportAssetOptions.ForceSynchronousImport);
        }

        private static string ScenePath(HeroDistrictId district)
        {
            return SceneRoot + "/Hero_" + district + ".unity";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var normalized = path.Replace('\\', '/');
            var parent = normalized.Substring(0, normalized.LastIndexOf('/'));
            var name = normalized.Substring(normalized.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
