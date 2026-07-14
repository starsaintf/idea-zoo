using System.Collections;
using UnityEngine;

namespace IdeaZoo.HeroSlice
{
    [DisallowMultipleComponent]
    public sealed class HeroSliceReviewSceneAnchor : MonoBehaviour
    {
        public HeroDistrictId District = HeroDistrictId.ZooEntrance;
        public bool AutoFrame = true;

        private IEnumerator Start()
        {
            for (var frame = 0; frame < 600; frame++)
            {
                var director = FindFirstObjectByType<CinematicHeroSliceDirector>();
                if (director != null && director.Installed && director.WorldPass != null)
                {
                    if (AutoFrame) director.WorldPass.FrameDistrict(District);
                    yield break;
                }
                yield return null;
            }
        }
    }
}
