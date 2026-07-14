using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdeaZoo.Presentation
{
    /// <summary>
    /// Keeps authored environment installation tied to every loaded gameplay scene.
    /// This prevents a persistent bootstrap from attaching to a temporary test scene
    /// and disappearing before Whisper Gate is loaded.
    /// </summary>
    [DefaultExecutionOrder(975)]
    public sealed class AuthoredEnvironmentSceneInstaller : MonoBehaviour
    {
        private GameObject _installedWorld;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            StartCoroutine(InstallWhenReady());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _installedWorld = null;
            StartCoroutine(InstallWhenReady());
        }

        private IEnumerator InstallWhenReady()
        {
            for (var frame = 0; frame < 480; frame++)
            {
                var world = GameObject.Find("TheIdeaZooWorld");
                if (world != null && world != _installedWorld)
                {
                    var hasDepartments = world.transform.Find("01_WHISPER_GATE") != null && world.transform.Find("10_DECISION_GARDEN") != null;
                    if (hasDepartments)
                    {
                        var pass = world.GetComponent<AuthoredEnvironmentPass>();
                        if (pass == null) pass = world.AddComponent<AuthoredEnvironmentPass>();
                        pass.Build(world.transform);
                        _installedWorld = world;
                        yield break;
                    }
                }
                yield return null;
            }
        }
    }

    public static class AuthoredEnvironmentSceneInstallerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<AuthoredEnvironmentSceneInstaller>() != null) return;
            var root = new GameObject("IdeaZoo_Authored_Environment_Installer");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<AuthoredEnvironmentSceneInstaller>();
        }
    }
}
