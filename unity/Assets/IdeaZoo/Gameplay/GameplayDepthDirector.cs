using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IdeaZoo.Core;
using IdeaZoo.Runtime;
using UnityEngine;

namespace IdeaZoo.Gameplay
{
    [DefaultExecutionOrder(-820)]
    [DisallowMultipleComponent]
    public sealed class GameplayDepthDirector : MonoBehaviour
    {
        private IdeaZooGame _game;
        private GameplayDepthHud _hud;
        private GameplayMemoryService _memory;
        private GameplayMemoryWorldPass _memoryWorld;
        private GameplayPerformanceGovernor _performance;
        private GameplayResourceState _resources;
        private GameplayResourceState _encounterResourceCheckpoint;
        private GameplayEncounterRun _encounter;
        private GameplayDisruptionDefinition _disruption;
        private readonly HashSet<int> _triggeredAtTestCount = new HashSet<int>();
        private MethodInfo _recordEvidence;
        private FieldInfo _currentTestField;
        private string _activeRecordId = string.Empty;
        private string _completedRecordId = string.Empty;
        private int _lastCompletedTests;
        private int _disruptionOrdinal;
        private bool _bound;
        private bool _disruptionQueued;

        public bool Bound { get { return _bound; } }
        public GameplayResourceState Resources { get { return _resources; } }
        public GameplayMemoryState MemoryState { get { return _memory != null ? _memory.State : null; } }
        public GameplayDepthHud DepthHud { get { return _hud; } }

        private IEnumerator Start()
        {
            _memory = new GameplayMemoryService();
            _hud = gameObject.AddComponent<GameplayDepthHud>();
            _hud.Build();
            _hud.ChoiceSelected += OnChoiceSelected;
            _hud.CancelRequested += OnCancelRequested;

            _performance = gameObject.AddComponent<GameplayPerformanceGovernor>();
            _performance.QualityChanged += OnQualityChanged;
            _performance.Begin();

            for (var frame = 0; frame < 600; frame++)
            {
                _game = FindAnyObjectByType<IdeaZooGame>();
                if (_game != null && _game.World != null && _game.Hud != null)
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
            _recordEvidence = typeof(IdeaZooGame).GetMethod("RecordEvidence", BindingFlags.Instance | BindingFlags.NonPublic);
            _currentTestField = typeof(IdeaZooHud).GetField("_currentTest", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_recordEvidence == null || _currentTestField == null)
            {
                Debug.LogError("Gameplay depth could not bind to the existing evidence loop.");
                enabled = false;
                return;
            }

            _memoryWorld = _game.World.GetComponent<GameplayMemoryWorldPass>() ?? _game.World.gameObject.AddComponent<GameplayMemoryWorldPass>();
            _memoryWorld.Build(_game.World);
            _memoryWorld.Refresh(_memory.State);
            _resources = GameplayResourceState.Fresh();
            _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
            _bound = true;
        }

        private void Update()
        {
            if (!_bound || _game == null || _game.Director == null) return;
            DetectCaseChange();
            DetectEvidenceOverlay();
            DetectEvidenceCompletion();
            DetectRulingCompletion();
        }

        private void DetectCaseChange()
        {
            var profile = _game.Director.Profile;
            var recordId = profile != null ? profile.RecordId : string.Empty;
            if (string.IsNullOrEmpty(recordId) || string.Equals(recordId, _activeRecordId, StringComparison.Ordinal)) return;

            _activeRecordId = recordId;
            _completedRecordId = string.Empty;
            _resources = GameplayResourceState.Fresh();
            _encounterResourceCheckpoint = null;
            _encounter = null;
            _disruption = null;
            _disruptionOrdinal = 0;
            _lastCompletedTests = 0;
            _disruptionQueued = false;
            _triggeredAtTestCount.Clear();
            _memory.BeginCase(profile, _resources);
            _hud.SetStatus(_resources, _memory.OpeningReflection(), _performance.StatusLabel);
        }

        private void DetectEvidenceOverlay()
        {
            if (_encounter != null || _disruption != null || _hud.OverlayOpen) return;
            if (_game.Director.Stage != CaseStage.Testing || !_game.Hud.OverlayOpen) return;

            var testId = _currentTestField.GetValue(_game.Hud) as string;
            if (string.IsNullOrWhiteSpace(testId)) return;
            if (_game.Director.CompletedTests.Contains(testId)) return;

            GameplayEncounterDefinition definition;
            try { definition = GameplayEncounterCatalog.For(testId, _game.Director.Profile); }
            catch { return; }

            _game.Hud.CloseOverlay();
            _game.Keeper.SetLocked(true);
            _game.Keeper.ResetTransientInput();
            _encounterResourceCheckpoint = _resources.Clone();
            _encounter = new GameplayEncounterRun(definition);
            _hud.ShowEncounter(_encounter, _resources, _memory.OpeningReflection());
        }

        private void DetectEvidenceCompletion()
        {
            var completed = _game.Director.CompletedTests.Count;
            if (completed == _lastCompletedTests) return;
            _lastCompletedTests = completed;
            _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);

            if ((completed == 2 || completed == 3) && !_triggeredAtTestCount.Contains(completed) && !_disruptionQueued)
            {
                _triggeredAtTestCount.Add(completed);
                _disruptionQueued = true;
                StartCoroutine(ShowDisruptionAfterCurrentFrame());
            }
        }

        private void DetectRulingCompletion()
        {
            if (_game.Director.Stage != CaseStage.Complete || _game.Director.Profile == null) return;
            var recordId = _game.Director.Profile.RecordId;
            if (string.IsNullOrEmpty(recordId) || string.Equals(recordId, _completedRecordId, StringComparison.Ordinal)) return;
            _completedRecordId = recordId;
            _memory.CompleteCase(_game.Director.Profile, _resources);
            if (_memoryWorld != null) _memoryWorld.Refresh(_memory.State);
            _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
        }

        private IEnumerator ShowDisruptionAfterCurrentFrame()
        {
            yield return null;
            _disruptionQueued = false;
            if (_game == null || _game.Director == null || _game.Director.Profile == null) yield break;
            if (_game.Director.Stage != CaseStage.Testing || _encounter != null || _hud.OverlayOpen) yield break;

            _disruption = GameplayDisruptionCatalog.For(_game.Director.Profile, _disruptionOrdinal++);
            _game.Keeper.SetLocked(true);
            _game.Keeper.ResetTransientInput();
            _hud.ShowDisruption(_disruption, _resources);
        }

        private void OnChoiceSelected(int index)
        {
            if (_encounter != null)
            {
                ResolveEncounterChoice(index);
                return;
            }
            if (_disruption != null) ResolveDisruptionChoice(index);
        }

        private void ResolveEncounterChoice(int index)
        {
            string error;
            if (!_encounter.Choose(index, _resources, out error))
            {
                _hud.SetFeedback(error, true);
                return;
            }

            _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
            if (!_encounter.Complete)
            {
                _hud.ShowEncounter(_encounter, _resources, _memory.OpeningReflection());
                return;
            }

            var profile = _game.Director.Profile;
            _encounter.ApplyMetricConsequences(profile);
            _memory.RecordEncounter(_encounter, _resources);
            var testId = _encounter.Definition.TestId;
            var strength = _encounter.Strength();
            var note = _encounter.EvidenceNote();
            _encounter = null;
            _encounterResourceCheckpoint = null;
            _hud.HideOverlay();

            try
            {
                _recordEvidence.Invoke(_game, new object[] { testId, strength, note });
                _game.Hud.SetSpecimen(profile);
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
                _game.Keeper.SetLocked(false);
            }
        }

        private void ResolveDisruptionChoice(int index)
        {
            if (index < 0 || index >= _disruption.Choices.Length) return;
            var choice = _disruption.Choices[index];
            if (!_resources.CanApply(choice.Impact))
            {
                _hud.SetFeedback("You do not have enough time, trust or momentum for that response.", true);
                return;
            }

            _resources.Apply(choice.Impact);
            ApplyImpact(_game.Director.Profile, choice.Impact);
            _memory.RecordDisruption(_disruption, choice, _resources);
            _game.Hud.SetSpecimen(_game.Director.Profile);
            _disruption = null;
            _hud.HideOverlay();
            _game.Keeper.SetLocked(false);
            _game.Keeper.ResetTransientInput();
            _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
        }

        private void OnCancelRequested()
        {
            if (_encounter != null && _encounterResourceCheckpoint != null)
                _resources = _encounterResourceCheckpoint.Clone();

            _encounterResourceCheckpoint = null;
            _encounter = null;
            _disruption = null;
            _hud.HideOverlay();
            _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
            if (_game == null || _game.Keeper == null) return;
            _game.Keeper.SetLocked(false);
            _game.Keeper.ResetTransientInput();
        }

        private void OnQualityChanged(string label)
        {
            if (_hud != null && _resources != null)
                _hud.SetStatus(_resources, _memory != null ? _memory.Summary() : string.Empty, label);
        }

        private static void ApplyImpact(IdeaProfile profile, GameplayImpact impact)
        {
            if (profile == null || impact == null) return;
            profile.Metrics.Desirability = Clamp(profile.Metrics.Desirability + impact.Desirability);
            profile.Metrics.Feasibility = Clamp(profile.Metrics.Feasibility + impact.Feasibility);
            profile.Metrics.Viability = Clamp(profile.Metrics.Viability + impact.Viability);
            profile.Metrics.Safety = Clamp(profile.Metrics.Safety + impact.Safety);
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        private static double Clamp(double value) { return Math.Max(0.0, Math.Min(1.0, value)); }
    }

    public static class GameplayDepthBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindAnyObjectByType<GameplayDepthDirector>() != null) return;
            var root = new GameObject("IdeaZoo_GameplayDepth");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.AddComponent<GameplayDepthDirector>();
        }
    }
}
