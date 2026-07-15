using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using IdeaZoo.Characters;
using IdeaZoo.Core;
using IdeaZoo.Gameplay;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.PlayerExperience
{
    [DefaultExecutionOrder(-780)]
    [DisallowMultipleComponent]
    public sealed class PlayerExperienceDirector : MonoBehaviour
    {
        private static PlayerExperienceDirector _current;

        private IdeaZooGame _game;
        private GameplayDepthDirector _depth;
        private PlayerExperienceHud _hud;
        private PlayerExperienceAccessibilityController _accessibility;
        private PlayerExperienceWorldPass _worldPass;
        private PlayerExperienceService _service;
        private MethodInfo _beginCase;
        private string _activeRecordId = string.Empty;
        private string _completedRecordId = string.Empty;
        private int _lastCompletedTests = -1;
        private CaseStage _lastStage;
        private bool _bound;
        private bool _onboardingShown;

        public static PlayerExperienceDirector Current { get { return _current; } }
        public bool Bound { get { return _bound; } }
        public PlayerExperienceState State { get { return _service != null ? _service.State : null; } }
        public PlayerExperienceHud ExperienceHud { get { return _hud; } }
        public PlayerExperienceWorldPass WorldPass { get { return _worldPass; } }
        public IdeaArchetype CurrentArchetype
        {
            get
            {
                var active = _service != null ? _service.Active : null;
                return active != null ? active.Archetype : IdeaArchetype.TechnologyWithoutProblem;
            }
        }

        private IEnumerator Start()
        {
            _current = this;
            _service = new PlayerExperienceService();
            _hud = gameObject.AddComponent<PlayerExperienceHud>();
            _hud.Build(_service.State.Accessibility);
            _hud.BeginTutorialRequested += BeginTutorial;
            _hud.ReplayTutorialRequested += BeginTutorial;
            _hud.ContinueWithoutTutorialRequested += ContinueWithoutTutorial;
            _hud.AccessibilityChanged += AccessibilityChanged;

            _accessibility = gameObject.AddComponent<PlayerExperienceAccessibilityController>();
            _accessibility.Configure(_service.State.Accessibility);

            for (var frame = 0; frame < 600; frame++)
            {
                _game = FindAnyObjectByType<IdeaZooGame>();
                _depth = FindAnyObjectByType<GameplayDepthDirector>();
                if (_game != null && _game.World != null && _game.Hud != null && _depth != null && _depth.Bound)
                {
                    Bind();
                    yield break;
                }
                yield return null;
            }
        }

        private void Bind()
        {
            if (_bound || _game == null) return;
            _beginCase = typeof(IdeaZooGame).GetMethod("BeginCase", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_beginCase == null)
            {
                Debug.LogError("Player Experience V1 could not bind to the real Whisper Gate intake.");
                enabled = false;
                return;
            }

            _worldPass = _game.World.GetComponent<PlayerExperienceWorldPass>() ?? _game.World.gameObject.AddComponent<PlayerExperienceWorldPass>();
            _worldPass.Build(_game.World);
            _worldPass.Refresh(_service.State);
            _accessibility.ApplySettings(_service.State.Accessibility);
            _bound = true;

            if (_game.Director.Profile == null && !_service.State.OnboardingDismissed)
            {
                _onboardingShown = true;
                _hud.ShowOnboarding(_service.State.TutorialCompleted);
            }
        }

        private void Update()
        {
            if (!_bound || _game == null || _game.Director == null) return;
            DetectCaseChange();
            DetectProgress();
            DetectCompletion();
        }

        public static GameplayEncounterDefinition DecorateEncounter(IdeaProfile profile, GameplayEncounterDefinition definition)
        {
            var decorated = PlayerExperienceArchetypeCatalog.Decorate(profile, definition);
            var rank = _current != null && _current._service != null ? _current._service.State.Rank : KeeperRank.Apprentice;
            return PlayerExperienceRankGuidance.Apply(rank, decorated);
        }

        public static void RecordTactileOutcome(PlayerExperienceTactileOutcome outcome)
        {
            if (_current == null || _current._service == null || outcome == null) return;
            _current._service.RecordTactile(outcome);
            PlayerExperienceAccessibilityController.Pulse();
        }

        private void DetectCaseChange()
        {
            var profile = _game.Director.Profile;
            var recordId = profile != null ? profile.RecordId : string.Empty;
            if (string.IsNullOrEmpty(recordId) || string.Equals(recordId, _activeRecordId, StringComparison.Ordinal)) return;

            _activeRecordId = recordId;
            _completedRecordId = string.Empty;
            _lastCompletedTests = _game.Director.CompletedTests.Count;
            _lastStage = _game.Director.Stage;
            var record = _service.BeginCase(profile);
            _accessibility.ApplySettings(_service.State.Accessibility);
            React(CharacterEmotion.Curious, CharacterGesture.Inspect);

            if (record != null && record.Tutorial)
                _hud.ShowTutorialStep("GUIDED CASE · THE CREATURE HAS A BODY", PlayerExperienceTutorial.Step(0));
            else
                _hud.ShowReaction("The Keeper: The Zoo will not tell you the case pattern until you have made a ruling.");
        }

        private void DetectProgress()
        {
            var profile = _game.Director.Profile;
            if (profile == null) return;
            var completed = _game.Director.CompletedTests.Count;
            var stage = _game.Director.Stage;

            if (completed != _lastCompletedTests)
            {
                _lastCompletedTests = completed;
                var active = _service.Active;
                var tendency = _depth.MemoryState != null ? _depth.MemoryState.DominantTendency() : GameplayTendency.Skeptic;
                if (active != null && active.Tutorial)
                    _hud.ShowTutorialStep("GUIDED CASE · " + completed + "/4 HABITATS", PlayerExperienceTutorial.Step(completed));
                else if (active != null)
                    _hud.ShowReaction(PlayerExperienceReactionCatalog.AfterTest(active.Archetype, completed, tendency));
                React(completed >= 3 ? CharacterEmotion.Concerned : CharacterEmotion.Curious, CharacterGesture.Explain);
            }

            if (stage != _lastStage)
            {
                if (stage == CaseStage.Molt)
                    _hud.ShowReaction("The Keeper: Do not improve every part. Change the part the evidence has made indefensible.");
                else if (stage == CaseStage.Decision)
                    _hud.ShowReaction("The Keeper: The ruling changes what remains in this world. It is not a score screen.");
                _lastStage = stage;
            }
        }

        private void DetectCompletion()
        {
            var profile = _game.Director.Profile;
            if (profile == null || _game.Director.Stage != CaseStage.Complete) return;
            if (string.IsNullOrEmpty(profile.RecordId) || string.Equals(profile.RecordId, _completedRecordId, StringComparison.Ordinal)) return;
            _completedRecordId = profile.RecordId;
            var record = _service.CompleteCase(profile);
            _worldPass.Refresh(_service.State);
            React(profile.FinalRuling == Ruling.Break ? CharacterEmotion.Grieving : CharacterEmotion.Hopeful,
                profile.FinalRuling == Ruling.Break ? CharacterGesture.Mourn : CharacterGesture.Celebrate);
            StartCoroutine(ShowReveal(record));
        }

        private IEnumerator ShowReveal(PlayerExperienceCaseRecord record)
        {
            yield return new WaitForSecondsRealtime(.75f);
            if (record != null) _hud.ShowArchetypeReveal(record, _service.State.Rank);
        }

        private void BeginTutorial()
        {
            if (!_bound || _beginCase == null || _game == null) return;
            if (_game.Director.Profile != null && _game.Director.Stage != CaseStage.Complete)
            {
                _hud.ShowReaction("The Keeper: Finish or deliberately restart the active case before opening the guided case.");
                return;
            }
            try
            {
                _onboardingShown = false;
                _beginCase.Invoke(_game, new object[] { PlayerExperienceTutorial.Intake() });
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
                _game.Hud.ShowIntake();
            }
        }

        private void ContinueWithoutTutorial()
        {
            _service.DismissOnboarding();
            _onboardingShown = false;
            if (_game != null && _game.Hud != null) _game.Hud.ShowIntake();
        }

        private void AccessibilityChanged(PlayerAccessibilitySettings settings)
        {
            _service.SaveAccessibility(settings);
            _accessibility.ApplySettings(settings);
        }

        private static void React(CharacterEmotion emotion, CharacterGesture gesture)
        {
            var rigs = Resources.FindObjectsOfTypeAll<CharacterPerformanceRig>();
            for (var i = 0; i < rigs.Length; i++)
            {
                var rig = rigs[i];
                if (rig == null || !rig.gameObject.scene.IsValid()) continue;
                rig.SetEmotion(emotion);
                rig.Perform(gesture, 1.4f);
            }
        }
    }

    public static class PlayerExperienceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindAnyObjectByType<PlayerExperienceDirector>() != null) return;
            var root = new GameObject("IdeaZoo_PlayerExperienceV1");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.AddComponent<PlayerExperienceDirector>();
        }
    }
}
