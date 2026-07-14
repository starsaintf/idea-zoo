using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Core;
using IdeaZoo.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IdeaZoo.Runtime
{
    [DisallowMultipleComponent]
    public sealed class IdeaZooGame : MonoBehaviour
    {
        private enum PendingStory { None, Hatch, Board, MoltRejected }

        private readonly Dictionary<string, TestDefinition> _tests = new Dictionary<string, TestDefinition>
        {
            { "desire", new TestDefinition("desire", "DESIRE YARD", "Did real people describe this problem before hearing your solution?", "Speak to five likely users. Ask how they solve it today before describing the idea.") },
            { "commitment", new TestDefinition("commitment", "COMMITMENT PADDOCK", "What has anyone risked to get this outcome?", "Ask three people for money, a preorder, a signed pilot, data access or one hour of their time.") },
            { "burden", new TestDefinition("burden", "BURROWER TUNNEL", "Who performs the invisible work when the idea succeeds?", "Map every recurring human, technical, legal and support task. Price the most expensive dependency.") },
            { "refusal", new TestDefinition("refusal", "REFUSAL GATE", "Can affected people leave without punishment?", "Write the easiest opt-out, deletion, appeal or shutdown path. Test it against the cruelest plausible use.") }
        };

        private IdeaZooCaseDirector _director;
        private WhisperGateWorld _world;
        private ThirdPersonKeeperController _keeper;
        private CreatureAssembler _creature;
        private IdeaZooHud _hud;
        private SpecimenArchive _archive;
        private Camera _camera;
        private PendingStory _pendingStory;
        private IdeaStation _nearest;
        private StationKind? _armedDecision;
        private float _armedDecisionUntil;
        private bool _boardExposed;
        private bool _transitionLocked;

        public IdeaZooCaseDirector Director { get { return _director; } }
        public WhisperGateWorld World { get { return _world; } }
        public ThirdPersonKeeperController Keeper { get { return _keeper; } }
        public CreatureAssembler Creature { get { return _creature; } }
        public IdeaZooHud Hud { get { return _hud; } }

        private void Awake()
        {
            Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            _director = new IdeaZooCaseDirector();
            _archive = new SpecimenArchive();
            EnsureEventSystem();
            BuildCamera();
            BuildWorld();
            BuildKeeper();
            BuildCreature();
            BuildHud();
            ResetCase();
        }

        private void Update()
        {
            if (_hud == null || _keeper == null || _world == null) return;
            if (_hud.OverlayOpen)
            {
                _hud.SetPrompt(string.Empty);
                return;
            }
            _keeper.MobileMove = _hud.Joystick.Value;
            if (_director.Profile == null) return;

            _nearest = _world.NearestAvailable(_keeper.transform.position);
            if (_director.Stage == CaseStage.Decision)
            {
                var decision = NearestDecision();
                if (decision.HasValue)
                {
                    var armed = _armedDecision.HasValue && _armedDecision.Value == decision.Value && Time.unscaledTime <= _armedDecisionUntil;
                    _hud.SetPrompt(armed ? "TOUCH AGAIN · CONFIRM " + DecisionName(decision.Value) : "TOUCH · ARM " + DecisionName(decision.Value) + " RULING");
                }
                else _hud.SetPrompt("ENTER A GATE · ISSUE A REAL-WORLD RULING");
                return;
            }

            if (_nearest == null) _hud.SetPrompt("HOLD LENS · SEE WHAT THE IDEA HIDES");
            else if (_nearest.Kind == StationKind.Board) _hud.SetPrompt("TOUCH · OPEN THE SEALED CLASSIFICATION");
            else if (_nearest.Kind == StationKind.Molt) _hud.SetPrompt("TOUCH · EDIT THE REAL IDEA");
            else _hud.SetPrompt("TOUCH · RUN " + _nearest.DisplayName);
        }

        private void BuildCamera()
        {
            var cameraObject = new GameObject("IdeaZooCamera");
            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.025f, 0.07f, 0.085f);
            _camera.fieldOfView = 58f;
            _camera.nearClipPlane = 0.08f;
            cameraObject.AddComponent<AudioListener>();
        }

        private void BuildWorld()
        {
            var worldObject = new GameObject("TheIdeaZooWorld");
            worldObject.transform.SetParent(transform, false);
            _world = worldObject.AddComponent<WhisperGateWorld>();
            _world.Build();
            var authored = worldObject.GetComponent<AuthoredEnvironmentPass>();
            if (authored == null) authored = worldObject.AddComponent<AuthoredEnvironmentPass>();
            authored.Build(worldObject.transform);
        }

        private void BuildKeeper()
        {
            var keeperObject = new GameObject("Keeper");
            keeperObject.transform.SetParent(transform, false);
            keeperObject.AddComponent<CharacterController>();
            _keeper = keeperObject.AddComponent<ThirdPersonKeeperController>();
            _keeper.Build(_camera);
            _keeper.InteractRequested += Interact;
            _keeper.LensChanged += SetLens;
        }

        private void BuildCreature()
        {
            var creatureObject = new GameObject("LivingIdea");
            creatureObject.transform.SetParent(transform, false);
            _creature = creatureObject.AddComponent<CreatureAssembler>();
            creatureObject.SetActive(false);
        }

        private void BuildHud()
        {
            var hudObject = new GameObject("IdeaZooHud");
            hudObject.transform.SetParent(transform, false);
            _hud = hudObject.AddComponent<IdeaZooHud>();
            _hud.Build();
            _hud.IntakeSubmitted += BeginCase;
            _hud.EvidenceSubmitted += RecordEvidence;
            _hud.MoltSubmitted += ApplyMolt;
            _hud.Continued += ContinueStory;
            _hud.Cancelled += CancelOverlay;
            _hud.RestartRequested += ResetCase;
            _hud.InteractRequested += Interact;
            _hud.LensChanged += SetLens;
        }

        private void BeginCase(IdeaIntake intake)
        {
            if (_transitionLocked) return;
            _transitionLocked = true;
            try
            {
                _director.Reset();
                _world.ResetCase();
                _boardExposed = false;
                _armedDecision = null;
                var profile = _director.Begin(intake);
                _keeper.transform.position = _world.KeeperSpawn.position;
                _keeper.SetLocked(true);
                _creature.gameObject.SetActive(true);
                _creature.transform.position = _world.HatchPoint.position;
                _creature.transform.localScale = Vector3.one * 0.06f;
                _creature.Configure(profile);
                _creature.SetFollowTarget(_keeper.transform);
                _hud.SetSpecimen(profile);
                _hud.SetProgress(0, 4);
                _hud.SetObjective("TWENTY-FOUR HOURS", "Your real idea has taken a body. Learn what it wants before deciding what it deserves.");
                StartCoroutine(Hatch(profile));
            }
            catch (Exception exception)
            {
                _hud.ShowStory("THE GATE REFUSED THE INTAKE", exception.Message, "RETURN TO THE FORM");
                _pendingStory = PendingStory.MoltRejected;
            }
            finally
            {
                _transitionLocked = false;
            }
        }

        private IEnumerator Hatch(IdeaProfile profile)
        {
            var duration = 1.25f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                _creature.transform.localScale = Vector3.one * Mathf.Lerp(0.06f, 1f, eased);
                yield return null;
            }

            var assumptions = profile.Assumptions.Count == 0 ? "No assumptions recorded." : "• " + string.Join("\n• ", profile.Assumptions.ToArray());
            _pendingStory = PendingStory.Hatch;
            _hud.ShowStory(
                profile.CreatureName.ToUpperInvariant() + " HAS HATCHED",
                "The Zoo reads it as " + profile.Class + "-class. It feeds on " + profile.Appetite + ". The Board pencilled in " + profile.BoardClass + " before seeing evidence.\n\nCurrent assumptions:\n" + assumptions,
                "ENTER THE LIVING ZOO");
        }

        private void ContinueStory()
        {
            if (_pendingStory == PendingStory.Hatch)
            {
                _director.EnterZoo();
                _keeper.SetLocked(false);
                _hud.SetObjective("MAKE OR BREAK THE IDEA", "Visit all four evidence habitats. The creature changes when reality contradicts you.");
            }
            else if (_pendingStory == PendingStory.Board)
            {
                _keeper.SetLocked(false);
            }
            else if (_pendingStory == PendingStory.MoltRejected)
            {
                if (_director.Stage == CaseStage.Molt) _hud.ShowMolt(_director.Profile);
                else _hud.ShowIntake();
            }
            _pendingStory = PendingStory.None;
        }

        private void Interact()
        {
            if (_transitionLocked || _hud.OverlayOpen || _keeper.ControlsLocked || _director.Profile == null) return;
            if (_director.Stage == CaseStage.Decision)
            {
                var decision = NearestDecision();
                if (decision.HasValue) ArmOrIssueDecision(decision.Value);
                return;
            }
            if (_nearest == null) return;

            if (_nearest.Kind == StationKind.Board)
            {
                OpenBoard();
                return;
            }
            if (_nearest.Kind == StationKind.Molt)
            {
                OpenMolt();
                return;
            }

            string testId = null;
            if (_nearest.Kind == StationKind.Desire) testId = "desire";
            if (_nearest.Kind == StationKind.Commitment) testId = "commitment";
            if (_nearest.Kind == StationKind.Burden) testId = "burden";
            if (_nearest.Kind == StationKind.Refusal) testId = "refusal";
            if (testId != null) OpenEvidence(testId);
        }

        private void OpenEvidence(string testId)
        {
            if (_director.Stage != CaseStage.Testing || _director.CompletedTests.Contains(testId)) return;
            TestDefinition test;
            if (!_tests.TryGetValue(testId, out test)) return;
            _keeper.SetLocked(true);
            _keeper.ResetTransientInput();
            _hud.ShowEvidence(test.Id, test.Title, test.Question, test.Mission);
        }

        private void RecordEvidence(string testId, int strength, string note)
        {
            if (_transitionLocked) return;
            _transitionLocked = true;
            try
            {
                if (!_director.RecordEvidence(testId, strength, note)) return;
                var stationKind = TestStation(testId);
                _world.Complete(stationKind);
                _creature.SetStage((float)_director.Profile.Metrics.Evidence, 1f - (float)_director.Profile.Metrics.Safety, _director.Profile.Guardrails.Count / 6f);
                _hud.SetSpecimen(_director.Profile);
                _hud.SetProgress(_director.CompletedTests.Count, 4);
                _hud.CloseOverlay();
                _keeper.SetLocked(false);

                if (_director.CompletedTests.Count == 2 && !_boardExposed)
                {
                    _world.SetAvailable(StationKind.Board, true);
                    _hud.SetObjective("THE OFFICIAL STORY MOVED FIRST", "A sealed Board record has opened. Inspect it, or keep testing the idea.");
                }
                else if (_director.CompletedTests.Count == 4)
                {
                    _world.SetAvailable(StationKind.Molt, true);
                    _hud.SetObjective("THE IDEA HAS EVIDENCE NOW", "Take it to the Molt House. The Decision Garden remains sealed until the idea changes.");
                }
                else _hud.SetObjective("EVIDENCE CHANGES THE BODY", (4 - _director.CompletedTests.Count) + " tests remain. Look for the next lit habitat.");
            }
            catch (Exception exception)
            {
                _hud.ShowStory("EVIDENCE REJECTED", exception.Message, "RETURN TO THE CASE");
                _pendingStory = PendingStory.Board;
            }
            finally
            {
                _transitionLocked = false;
            }
        }

        private void OpenBoard()
        {
            if (_boardExposed) return;
            _boardExposed = true;
            _world.Complete(StationKind.Board);
            _keeper.SetLocked(true);
            _pendingStory = PendingStory.Board;
            _hud.ShowStory(
                "FAST CITY MANDATE",
                "The Board marked this specimen " + _director.Profile.BoardClass + " before it hatched because that classification is easier to fund, sell and deploy. Your observed class is " + _director.Profile.Class + ". The institution is now part of the evidence.",
                "RETURN TO THE CASE");
        }

        private void OpenMolt()
        {
            if (_director.Stage != CaseStage.Testing || _director.CompletedTests.Count < 4) return;
            _director.EnterMolt();
            _keeper.SetLocked(true);
            _hud.ShowMolt(_director.Profile);
        }

        private void ApplyMolt(string promise, string audience, List<string> guardrails)
        {
            if (_transitionLocked) return;
            _transitionLocked = true;
            try
            {
                _director.ApplyMolt(promise, audience, guardrails);
                _creature.Molt(_director.Profile);
                _world.Complete(StationKind.Molt);
                _world.RevealDecisionGarden();
                _hud.SetSpecimen(_director.Profile);
                _hud.CloseOverlay();
                _keeper.SetLocked(false);
                _hud.SetObjective("THE IDEA CAN LEAVE IN FIVE WAYS", "Walk into a ruling gate: Build, Molt, Hibernate, Sanctuary or Break.");
            }
            catch (Exception exception)
            {
                _pendingStory = PendingStory.MoltRejected;
                _hud.ShowStory("THE MOLT DID NOT TAKE", exception.Message, "RETURN TO THE MOLT HOUSE");
            }
            finally
            {
                _transitionLocked = false;
            }
        }

        private void CancelOverlay()
        {
            if (_director.Stage == CaseStage.Molt) _director.CancelMolt();
            _keeper.SetLocked(false);
            _keeper.ResetTransientInput();
        }

        private void ArmOrIssueDecision(StationKind station)
        {
            if (!_armedDecision.HasValue || _armedDecision.Value != station || Time.unscaledTime > _armedDecisionUntil)
            {
                _armedDecision = station;
                _armedDecisionUntil = Time.unscaledTime + 4f;
                _hud.SetPrompt("TOUCH AGAIN · CONFIRM " + DecisionName(station));
                return;
            }

            var ruling = ToRuling(station);
            _director.IssueRuling(ruling);
            _keeper.SetLocked(true);
            _armedDecision = null;
            string saveError;
            var saved = _archive.Save(_director.Profile, out saveError);
            _hud.ShowResult(_director.Profile, saved, saveError);
        }

        private StationKind? NearestDecision()
        {
            if (_world.DecisionRoot == null || !_world.DecisionRoot.gameObject.activeInHierarchy) return null;
            IdeaStation closest = null;
            var distance = 3.0f;
            foreach (var station in _world.Stations)
            {
                if (!IsDecision(station.Kind) || !station.Available || !station.gameObject.activeInHierarchy) continue;
                var current = Vector3.Distance(_keeper.transform.position, station.transform.position);
                if (current < distance)
                {
                    distance = current;
                    closest = station;
                }
            }
            return closest == null ? (StationKind?)null : closest.Kind;
        }

        private void SetLens(bool active)
        {
            if (_creature != null && _creature.gameObject.activeSelf) _creature.SetRevealed(active);
            if (_keeper != null) _keeper.SetLens(active);
        }

        private void ResetCase()
        {
            StopAllCoroutines();
            _director.Reset();
            _world.ResetCase();
            _boardExposed = false;
            _pendingStory = PendingStory.None;
            _armedDecision = null;
            _nearest = null;
            _transitionLocked = false;
            _creature.gameObject.SetActive(false);
            _creature.SetFollowTarget(null);
            _keeper.transform.position = _world.KeeperSpawn.position;
            _keeper.SetLocked(true);
            _keeper.ResetTransientInput();
            _hud.ShowIntake();
        }

        private static StationKind TestStation(string testId)
        {
            if (testId == "desire") return StationKind.Desire;
            if (testId == "commitment") return StationKind.Commitment;
            if (testId == "burden") return StationKind.Burden;
            return StationKind.Refusal;
        }

        private static bool IsDecision(StationKind kind)
        {
            return kind == StationKind.Build || kind == StationKind.Rework || kind == StationKind.Hibernate || kind == StationKind.Sanctuary || kind == StationKind.Break;
        }

        private static Ruling ToRuling(StationKind kind)
        {
            if (kind == StationKind.Build) return Ruling.Build;
            if (kind == StationKind.Rework) return Ruling.Molt;
            if (kind == StationKind.Hibernate) return Ruling.Hibernate;
            if (kind == StationKind.Sanctuary) return Ruling.Sanctuary;
            return Ruling.Break;
        }

        private static string DecisionName(StationKind kind)
        {
            return kind == StationKind.Rework ? "MOLT" : kind.ToString().ToUpperInvariant();
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private sealed class TestDefinition
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string Question;
            public readonly string Mission;

            public TestDefinition(string id, string title, string question, string mission)
            {
                Id = id;
                Title = title;
                Question = question;
                Mission = mission;
            }
        }
    }

    public static class IdeaZooAutoLoad
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIdeaZoo()
        {
            if (UnityEngine.Object.FindFirstObjectByType<IdeaZooGame>() != null) return;
            var root = new GameObject("TheIdeaZoo");
            root.AddComponent<IdeaZooGame>();
            UnityEngine.Object.DontDestroyOnLoad(root);
        }
    }
}
