using System.Collections;
using UnityEngine;

namespace IdeaZoo.Story
{
    [DisallowMultipleComponent]
    public sealed class CampaignEndingPersistence : MonoBehaviour
    {
        private CampaignDirector _campaign;
        private bool _loaded;

        private IEnumerator Start()
        {
            for (var frame = 0; frame < 480; frame++)
            {
                _campaign = FindFirstObjectByType<CampaignDirector>();
                if (_campaign != null && _campaign.State != null)
                {
                    Restore();
                    _campaign.StateChanged += Save;
                    yield break;
                }
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (_campaign != null) _campaign.StateChanged -= Save;
        }

        private void Restore()
        {
            if (_loaded || _campaign == null || _campaign.State == null) return;
            _loaded = true;
            if (PlayerPrefs.GetInt("iz_campaign_has_ending", 0) != 1) return;
            var value = Mathf.Clamp(PlayerPrefs.GetInt("iz_campaign_ending", 0), 0, 4);
            _campaign.State.Ending = (CampaignEnding)value;
            _campaign.State.FinalRulingIssued = true;
            _campaign.State.Chapter = CampaignChapter.Complete;
        }

        private static void Save(CampaignState state)
        {
            if (state == null || !state.FinalRulingIssued || !state.Ending.HasValue) return;
            PlayerPrefs.SetInt("iz_campaign_has_ending", 1);
            PlayerPrefs.SetInt("iz_campaign_ending", (int)state.Ending.Value);
            PlayerPrefs.Save();
        }
    }

    public static class CampaignEndingPersistenceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<CampaignEndingPersistence>() != null) return;
            var root = new GameObject("IdeaZoo_Campaign_Ending_Persistence");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<CampaignEndingPersistence>();
        }
    }
}
