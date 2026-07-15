using UnityEngine;

namespace IdeaZoo.Gameplay
{
    public static class GameplayResourceSafety
    {
        public static bool EnsurePlayable(GameplayChoice[] choices, GameplayResourceState resources)
        {
            if (choices == null || choices.Length == 0 || resources == null) return false;
            for (var i = 0; i < choices.Length; i++)
                if (resources.CanApply(choices[i].Impact)) return false;

            var best = 0;
            var smallestDeficit = int.MaxValue;
            for (var i = 0; i < choices.Length; i++)
            {
                var impact = choices[i].Impact;
                var deficit = Mathf.Max(0, -impact.Time - resources.Time)
                              + Mathf.Max(0, -impact.Trust - resources.Trust)
                              + Mathf.Max(0, -impact.Momentum - resources.Momentum);
                if (deficit >= smallestDeficit) continue;
                smallestDeficit = deficit;
                best = i;
            }

            var fallback = choices[best].Impact;
            resources.Time = Mathf.Max(resources.Time, Mathf.Max(0, -fallback.Time));
            resources.Trust = Mathf.Max(resources.Trust, Mathf.Max(0, -fallback.Trust));
            resources.Momentum = Mathf.Max(resources.Momentum, Mathf.Max(0, -fallback.Momentum));
            return true;
        }
    }
}
