using System.Collections;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GreyboxStaffCleanup : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (var attempt = 0; attempt < 120; attempt++)
            {
                var world = FindFirstObjectByType<WhisperGateWorld>();
                if (world != null)
                {
                    DisablePrototypeStaff(world.transform);
                    yield break;
                }
                yield return null;
            }
        }

        private static void DisablePrototypeStaff(Transform world)
        {
            Transform prototypeRoot = null;
            foreach (var child in world.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "STAFF_AND_AMBIENT_LIFE")
                {
                    prototypeRoot = child;
                    break;
                }
            }
            if (prototypeRoot == null) return;

            foreach (Transform child in prototypeRoot)
            {
                if (child.name == "AMBIENT_ORGANISMS") continue;
                child.gameObject.SetActive(false);
            }
        }
    }

    public static class GreyboxStaffCleanupAutoLoad
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartCleanup()
        {
            if (Object.FindFirstObjectByType<GreyboxStaffCleanup>() != null) return;
            var root = new GameObject("IdeaZoo_Greybox_Staff_Cleanup");
            root.AddComponent<GreyboxStaffCleanup>();
            Object.DontDestroyOnLoad(root);
        }
    }
}
