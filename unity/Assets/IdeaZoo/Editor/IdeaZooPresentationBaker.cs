#if UNITY_EDITOR
using IdeaZoo.Presentation;
using IdeaZoo.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdeaZoo.EditorTools
{
    public static class IdeaZooPresentationBaker
    {
        private const string ArtRoot = "Assets/IdeaZoo/Art";
        private const string PrefabRoot = ArtRoot + "/Prefabs";
        private const string ReviewScenePath = "Assets/IdeaZoo/Scenes/WhisperGatePresentationReview.unity";

        [MenuItem("Idea Zoo/Presentation/Bake District Prefab")]
        public static void BakeDistrictPrefab()
        {
            EnsureFolders();
            var root = BuildPresentationRoot();
            var prefabPath = PrefabRoot + "/WhisperGateDistrict.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Idea Zoo district prefab baked to " + prefabPath);
        }

        [MenuItem("Idea Zoo/Presentation/Bake Review Scene")]
        public static void BakeReviewScene()
        {
            EnsureFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = BuildPresentationRoot();
            root.name = "WhisperGate_Presentation_Review";

            var cameraObject = new GameObject("ReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 26f, 42f);
            camera.transform.rotation = Quaternion.Euler(24f, 180f, 0f);
            camera.fieldOfView = 48f;
            cameraObject.AddComponent<AudioListener>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ReviewScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;
            Debug.Log("Idea Zoo presentation review scene baked to " + ReviewScenePath);
        }

        [MenuItem("Idea Zoo/Presentation/Validate Baked Assets")]
        public static void ValidateBakedAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/WhisperGateDistrict.prefab");
            var reviewScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewScenePath);
            if (prefab == null || reviewScene == null)
                throw new UnityException("Bake the district prefab and review scene before validation.");

            var stationCount = prefab.GetComponentsInChildren<IdeaStation>(true).Length;
            var specialistCount = prefab.GetComponentsInChildren<ProceduralSpecialist>(true).Length;
            var publicLanguage = prefab.GetComponentsInChildren<TextMesh>(true).Length;
            if (stationCount < 11) throw new UnityException("Baked district is missing ruling or evidence stations.");
            if (specialistCount < 6) throw new UnityException("Baked district is missing specialist rigs.");
            if (publicLanguage < 8) throw new UnityException("Baked district is missing environmental story language.");
            Debug.Log("Idea Zoo baked presentation validated: " + stationCount + " stations, " + specialistCount + " specialists, " + publicLanguage + " public-language elements.");
        }

        private static GameObject BuildPresentationRoot()
        {
            var root = new GameObject("WhisperGateDistrict_Baked");
            var world = root.AddComponent<WhisperGateWorld>();
            world.Build();
            var art = root.AddComponent<CivicWorldArtPass>();
            art.Build(root.transform);
            var staff = root.AddComponent<StaffEnsemble>();
            staff.Build(root.transform, null, null);
            DisableGreyboxStaff(root.transform);
            return root;
        }

        private static void DisableGreyboxStaff(Transform root)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "STAFF_AND_AMBIENT_LIFE") continue;
                foreach (Transform member in child) member.gameObject.SetActive(false);
                return;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/IdeaZoo", "Art");
            EnsureFolder(ArtRoot, "Prefabs");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
