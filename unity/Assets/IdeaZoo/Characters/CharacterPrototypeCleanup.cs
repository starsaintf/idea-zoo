using System.Collections;
using System.Linq;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Characters
{
    [DisallowMultipleComponent]
    public sealed class CharacterPrototypeCleanup : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (var frame = 0; frame < 480; frame++)
            {
                var game = FindFirstObjectByType<IdeaZooGame>();
                if (game != null && game.Keeper != null)
                {
                    var production = game.Keeper.transform.Find("PRODUCTION_KEEPER_VISUAL");
                    if (production != null)
                    {
                        var prototype = game.Keeper.transform.Find("KeeperVisual");
                        if (prototype != null)
                        {
                            foreach (var renderer in prototype.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                        }
                        yield break;
                    }
                }
                yield return null;
            }
        }
    }

    public static class CharacterPrototypeCleanupBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<CharacterPrototypeCleanup>() != null) return;
            new GameObject("IdeaZoo_Character_Prototype_Cleanup").AddComponent<CharacterPrototypeCleanup>();
        }
    }
}
