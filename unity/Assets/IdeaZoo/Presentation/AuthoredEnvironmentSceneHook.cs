using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdeaZoo.Presentation
{
    /// <summary>
    /// Installs authored geometry after each scene's Awake phase. The static hook is
    /// resilient to Play Mode tests that replace the initial scene after bootstraps run.
    /// </summary>
    public static class AuthoredEnvironmentSceneHook
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialLoad()
        {
            EnsureInstalled();
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstalled();
        }

        public static bool EnsureInstalled()
        {
            var world = GameObject.Find("TheIdeaZooWorld");
            if (world == null) return false;
            if (world.transform.Find("01_WHISPER_GATE") == null || world.transform.Find("10_DECISION_GARDEN") == null) return false;

            var pass = world.GetComponent<AuthoredEnvironmentPass>();
            if (pass == null) pass = world.AddComponent<AuthoredEnvironmentPass>();
            pass.Build(world.transform);
            return world.transform.Find("AUTHORED_ENVIRONMENT_KIT") != null;
        }
    }
}
