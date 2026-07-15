using UnityEngine;

namespace IdeaZoo.HeroSlice
{
    [DisallowMultipleComponent]
    public sealed class CinematicHeroSlicePrefabAnchor : MonoBehaviour
    {
        public bool InstallOnAwake = true;

        private void Awake()
        {
            if (!InstallOnAwake) return;
            if (FindAnyObjectByType<CinematicHeroSliceDirector>() != null) return;
            gameObject.AddComponent<CinematicHeroSliceDirector>();
        }
    }
}
