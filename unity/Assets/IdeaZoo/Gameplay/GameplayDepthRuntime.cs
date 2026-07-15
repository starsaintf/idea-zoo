using System;
using System.Collections;
using System.Collections.Generic;
using IdeaZoo.Core;
using IdeaZoo.Runtime;
using UnityEngine;
using UnityEngine.UI;

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
        private GameplayEncounterRun _encounter;
        private GameplayDisruptionDefinition _disruption;
        private int _disruptionOrdinal;
        private readonly HashSet<int> _triggeredAtTestCount = new HashSet<int>();
        private bool _bound;

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
                _game = FindFirstObjectByType<IdeaZooGame>();
                if (_game != null && _game.World != null && _game.Hud != null)
                {
                    Bind();
                    yield break;
                }
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (_game == null) return;
            _game.CaseStarted -= OnCaseStarted;
            _game.GameplayEncounterRequested -= OnEncounterRequested;
            _game.EvidenceRecorded -= OnEvidenceRecorded;
            _game.RulingIssued -= OnRulingIssued;
        }

        private void Bind()
        {
            if (_bound || _game == null) return;
            _game.CaseStarted += OnCaseStarted;
            _game.GameplayEncounterRequested += OnEncounterRequested;
            _game.EvidenceRecorded += OnEvidenceRecorded;
            _game.RulingIssued += OnRulingIssued;

            _memoryWorld = _game.World.GetComponent<GameplayMemoryWorldPass>() ?? _game.World.gameObject.AddComponent<GameplayMemoryWorldPass>();
            _memoryWorld.Build(_game.World);
            _memoryWorld.Refresh(_memory.State);
            _resources = GameplayResourceState.Fresh();
            _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
            _bound = true;
        }

        private void OnCaseStarted(IdeaProfile profile)
        {
            _resources = GameplayResourceState.Fresh();
            _encounter = null;
            _disruption = null;
            _disruptionOrdinal = 0;
            _triggeredAtTestCount.Clear();
            _memory.BeginCase(profile, _resources);
            _hud.SetStatus(_resources, _memory.OpeningReflection(), _performance.StatusLabel);
        }

        private void OnEncounterRequested(string testId)
        {
            if (_game == null || _game.Director == null || _game.Director.Profile == null) return;
            var definition = GameplayEncounterCatalog.For(testId, _game.Director.Profile);
            _encounter = new GameplayEncounterRun(definition);
            _disruption = null;
            _game.SetGameplayInteractionLock(true);
            _hud.ShowEncounter(_encounter, _resources, _memory.OpeningReflection());
        }

        private void OnChoiceSelected(int index)
        {
            if (_encounter != null)
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
                _hud.HideOverlay();
                _game.SubmitGameplayEvidence(testId, strength, note);
                _game.Hud.SetSpecimen(profile);
                _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
                return;
            }

            if (_disruption != null)
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
                _game.SetGameplayInteractionLock(false);
                _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
            }
        }

        private void OnCancelRequested()
        {
            _encounter = null;
            _disruption = null;
            _hud.HideOverlay();
            if (_game != null) _game.SetGameplayInteractionLock(false);
        }

        private void OnEvidenceRecorded(string testId, int strength, string note)
        {
            if (_game == null || _game.Director == null) return;
            var completed = _game.Director.CompletedTests.Count;
            if ((completed != 2 && completed != 3) || _triggeredAtTestCount.Contains(completed)) return;
            _triggeredAtTestCount.Add(completed);
            StartCoroutine(ShowDisruptionNextFrame());
        }

        private IEnumerator ShowDisruptionNextFrame()
        {
            yield return null;
            if (_game == null || _game.Director == null || _game.Director.Profile == null) yield break;
            if (_game.Director.Stage != CaseStage.Testing) yield break;
            _disruption = GameplayDisruptionCatalog.For(_game.Director.Profile, _disruptionOrdinal++);
            _encounter = null;
            _game.SetGameplayInteractionLock(true);
            _hud.ShowDisruption(_disruption, _resources);
        }

        private void OnRulingIssued(IdeaProfile profile, Ruling ruling)
        {
            _memory.CompleteCase(profile, _resources);
            if (_memoryWorld != null) _memoryWorld.Refresh(_memory.State);
            _hud.SetStatus(_resources, _memory.Summary(), _performance.StatusLabel);
        }

        private void OnQualityChanged(string label)
        {
            if (_hud != null && _resources != null) _hud.SetStatus(_resources, _memory != null ? _memory.Summary() : string.Empty, label);
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
            if (UnityEngine.Object.FindFirstObjectByType<GameplayDepthDirector>() != null) return;
            var root = new GameObject("IdeaZoo_GameplayDepth");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.AddComponent<GameplayDepthDirector>();
        }
    }

    [DisallowMultipleComponent]
    public sealed class GameplayDepthHud : MonoBehaviour
    {
        public event Action<int> ChoiceSelected;
        public event Action CancelRequested;

        private Font _font;
        private Canvas _canvas;
        private GameObject _statusPanel;
        private Text _statusText;
        private GameObject _overlay;
        private Text _title;
        private Text _body;
        private Text _context;
        private Text _feedback;
        private Text _resourceText;
        private readonly List<Button> _choiceButtons = new List<Button>(4);
        private Button _cancelButton;

        private static readonly Color Ink = new Color(0.015f, 0.035f, 0.055f, 0.98f);
        private static readonly Color Paper = new Color(0.92f, 0.88f, 0.77f, 1f);
        private static readonly Color Brass = new Color(0.91f, 0.64f, 0.28f, 1f);
        private static readonly Color Teal = new Color(0.24f, 0.75f, 0.70f, 1f);
        private static readonly Color Rust = new Color(0.78f, 0.27f, 0.22f, 1f);
        private static readonly Color Muted = new Color(0.20f, 0.27f, 0.30f, 1f);

        public bool OverlayOpen { get { return _overlay != null && _overlay.activeSelf; } }

        public void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasObject = new GameObject("GameplayDepthCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 125;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(896f, 414f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildStatus(canvasObject.transform);
            BuildOverlay(canvasObject.transform);
            _overlay.SetActive(false);
        }

        public void SetStatus(GameplayResourceState resources, string memory, string performance)
        {
            if (_statusText == null || resources == null) return;
            _statusText.text = resources.Compact() + "\n" + memory + "  ·  " + performance;
        }

        public void ShowEncounter(GameplayEncounterRun run, GameplayResourceState resources, string reflection)
        {
            if (run == null || run.CurrentRound == null) return;
            OpenOverlay();
            _title.text = run.Definition.Title + "  ·  " + (run.RoundIndex + 1) + "/" + run.Definition.Rounds.Length;
            _body.text = run.CurrentRound.Prompt;
            var previous = string.IsNullOrEmpty(run.LastConsequence) ? reflection : "LAST CONSEQUENCE: " + run.LastConsequence;
            _context.text = run.Definition.Mission + "\n\n" + run.CurrentRound.Context + "\n\n" + previous;
            _feedback.text = string.Empty;
            _resourceText.text = resources.Compact();
            RenderChoices(run.CurrentRound.Choices, resources);
            _cancelButton.gameObject.SetActive(true);
        }

        public void ShowDisruption(GameplayDisruptionDefinition disruption, GameplayResourceState resources)
        {
            if (disruption == null) return;
            OpenOverlay();
            _title.text = disruption.Title;
            _body.text = disruption.Situation;
            _context.text = "The plan changed. Choose what the creature sacrifices.";
            _feedback.text = string.Empty;
            _resourceText.text = resources.Compact();
            RenderChoices(disruption.Choices, resources);
            _cancelButton.gameObject.SetActive(false);
        }

        public void SetFeedback(string message, bool error)
        {
            if (_feedback == null) return;
            _feedback.text = message ?? string.Empty;
            _feedback.color = error ? Rust : Teal;
        }

        public void HideOverlay()
        {
            if (_overlay != null) _overlay.SetActive(false);
        }

        private void BuildStatus(Transform parent)
        {
            _statusPanel = Panel("GameplayResourceStatus", parent, new Color(0.015f, 0.035f, 0.055f, 0.92f));
            var rect = _statusPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 1f);
            rect.anchorMax = new Vector2(0.92f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -124f);
            rect.offsetMax = new Vector2(0f, -88f);
            _statusText = Label("GameplayStatusText", _statusPanel.transform, string.Empty, 11, Paper, TextAnchor.MiddleCenter);
            Stretch(_statusText.rectTransform, 8f);
        }

        private void BuildOverlay(Transform parent)
        {
            _overlay = Panel("GameplayEncounterOverlay", parent, new Color(0.005f, 0.012f, 0.020f, 0.94f));
            Stretch(_overlay.GetComponent<RectTransform>());

            var panel = Panel("EncounterPanel", _overlay.transform, Ink);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.06f, 0.06f);
            panelRect.anchorMax = new Vector2(0.94f, 0.94f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            _title = Label("EncounterTitle", panel.transform, string.Empty, 24, Brass, TextAnchor.MiddleLeft);
            SetRect(_title.rectTransform, new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.96f));
            _body = Label("EncounterPrompt", panel.transform, string.Empty, 19, Paper, TextAnchor.UpperLeft);
            SetRect(_body.rectTransform, new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.82f));
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Truncate;

            _context = Label("EncounterContext", panel.transform, string.Empty, 13, new Color(0.72f, 0.76f, 0.72f), TextAnchor.UpperLeft);
            SetRect(_context.rectTransform, new Vector2(0.04f, 0.34f), new Vector2(0.45f, 0.62f));
            _context.horizontalOverflow = HorizontalWrapMode.Wrap;
            _context.verticalOverflow = VerticalWrapMode.Truncate;

            var choiceRoot = new GameObject("ChoicePool", typeof(RectTransform), typeof(VerticalLayoutGroup));
            choiceRoot.transform.SetParent(panel.transform, false);
            var choiceRect = choiceRoot.GetComponent<RectTransform>();
            SetRect(choiceRect, new Vector2(0.48f, 0.20f), new Vector2(0.96f, 0.72f));
            var layout = choiceRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            for (var i = 0; i < 4; i++)
            {
                var captured = i;
                var button = ButtonObject("Choice_" + i, choiceRoot.transform, Teal);
                button.onClick.AddListener(delegate { ChoiceSelected?.Invoke(captured); });
                _choiceButtons.Add(button);
            }

            _resourceText = Label("EncounterResources", panel.transform, string.Empty, 13, Teal, TextAnchor.MiddleLeft);
            SetRect(_resourceText.rectTransform, new Vector2(0.04f, 0.17f), new Vector2(0.72f, 0.28f));
            _feedback = Label("EncounterFeedback", panel.transform, string.Empty, 12, Rust, TextAnchor.MiddleLeft);
            SetRect(_feedback.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.72f, 0.17f));

            _cancelButton = ButtonObject("ReturnToZoo", panel.transform, Muted);
            var cancelRect = _cancelButton.GetComponent<RectTransform>();
            SetRect(cancelRect, new Vector2(0.75f, 0.04f), new Vector2(0.96f, 0.17f));
            SetButtonText(_cancelButton, "RETURN TO ZOO");
            _cancelButton.onClick.AddListener(delegate { CancelRequested?.Invoke(); });
        }

        private void RenderChoices(GameplayChoice[] choices, GameplayResourceState resources)
        {
            for (var i = 0; i < _choiceButtons.Count; i++)
            {
                var active = choices != null && i < choices.Length;
                var button = _choiceButtons[i];
                button.gameObject.SetActive(active);
                if (!active) continue;
                var choice = choices[i];
                var affordable = resources.CanApply(choice.Impact);
                button.interactable = affordable;
                SetButtonText(button, choice.Label + "\n" + Cost(choice.Impact));
                var colors = button.colors;
                colors.normalColor = affordable ? Teal : Muted;
                colors.highlightedColor = Brass;
                colors.pressedColor = new Color(0.15f, 0.55f, 0.52f, 1f);
                colors.disabledColor = new Color(0.11f, 0.14f, 0.16f, 0.72f);
                button.colors = colors;
            }
        }

        private void OpenOverlay()
        {
            _overlay.SetActive(true);
            _overlay.transform.SetAsLastSibling();
        }

        private Button ButtonObject(string name, Transform parent, Color normal)
        {
            var objectValue = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            objectValue.transform.SetParent(parent, false);
            var image = objectValue.GetComponent<Image>();
            image.color = normal;
            var button = objectValue.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Brass;
            colors.pressedColor = new Color(0.15f, 0.55f, 0.52f, 1f);
            button.colors = colors;
            var element = objectValue.GetComponent<LayoutElement>();
            element.minHeight = 52f;
            var text = Label("Label", objectValue.transform, string.Empty, 13, new Color(0.02f, 0.05f, 0.06f), TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 8f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return button;
        }

        private static string Cost(GameplayImpact impact)
        {
            var parts = new List<string>(4);
            if (impact.Time != 0) parts.Add("time " + Signed(impact.Time));
            if (impact.Trust != 0) parts.Add("trust " + Signed(impact.Trust));
            if (impact.Momentum != 0) parts.Add("momentum " + Signed(impact.Momentum));
            if (impact.Evidence != 0) parts.Add("evidence " + Signed(impact.Evidence));
            return parts.Count == 0 ? "no immediate cost" : string.Join(" · ", parts.ToArray());
        }

        private static string Signed(int value) { return value > 0 ? "+" + value : value.ToString(); }

        private static void SetButtonText(Button button, string value)
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = value;
        }

        private GameObject Panel(string name, Transform parent, Color color)
        {
            var value = new GameObject(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            value.GetComponent<Image>().color = color;
            return value;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color, TextAnchor anchor)
        {
            var objectValue = new GameObject(name, typeof(RectTransform), typeof(Text));
            objectValue.transform.SetParent(parent, false);
            var text = objectValue.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = Vector2.one * -padding;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    [DisallowMultipleComponent]
    public sealed class GameplayPerformanceGovernor : MonoBehaviour
    {
        public const int MobileTargetFps = 30;
        public const int DesktopTargetFps = 60;
        public const int MaximumEncounterButtons = 4;
        public const int MaximumVisibleMemoryCards = 12;
        public const int MaximumSavedCases = 20;

        public event Action<string> QualityChanged;

        private readonly Dictionary<ParticleSystem, int> _particleBudgets = new Dictionary<ParticleSystem, int>();
        private float _sampleStarted;
        private int _frames;
        private int _badSamples;
        private int _goodSamples;
        private bool _reduced;
        private float _originalShadowDistance;
        private float _originalLodBias;
        private int _target;

        public string StatusLabel { get; private set; } = "QUALITY · FULL";
        public float SmoothedFps { get; private set; } = 60f;

        public void Begin()
        {
            _target = Application.isMobilePlatform ? MobileTargetFps : DesktopTargetFps;
            Application.targetFrameRate = _target;
            QualitySettings.vSyncCount = 0;
            Time.maximumDeltaTime = 0.10f;
            _originalShadowDistance = QualitySettings.shadowDistance;
            _originalLodBias = QualitySettings.lodBias;
            _sampleStarted = Time.unscaledTime;
            _frames = 0;
        }

        private void Update()
        {
            _frames++;
            var elapsed = Time.unscaledTime - _sampleStarted;
            if (elapsed < 1f) return;

            var sample = _frames / Mathf.Max(0.01f, elapsed);
            SmoothedFps = Mathf.Lerp(SmoothedFps, sample, 0.35f);
            _frames = 0;
            _sampleStarted = Time.unscaledTime;

            if (SmoothedFps < _target * 0.82f)
            {
                _badSamples++;
                _goodSamples = 0;
            }
            else if (SmoothedFps > _target * 0.94f)
            {
                _goodSamples++;
                _badSamples = 0;
            }
            else
            {
                _badSamples = Mathf.Max(0, _badSamples - 1);
                _goodSamples = Mathf.Max(0, _goodSamples - 1);
            }

            if (!_reduced && _badSamples >= 3) ReduceNonessentialLoad();
            else if (_reduced && _goodSamples >= 8) RestoreQuality();
        }

        private void ReduceNonessentialLoad()
        {
            _reduced = true;
            _badSamples = 0;
            QualitySettings.shadowDistance = Mathf.Max(12f, _originalShadowDistance * 0.65f);
            QualitySettings.lodBias = Mathf.Max(0.72f, _originalLodBias * 0.82f);
            var particles = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < particles.Length; i++)
            {
                var system = particles[i];
                if (system == null) continue;
                var main = system.main;
                if (!_particleBudgets.ContainsKey(system)) _particleBudgets[system] = main.maxParticles;
                main.maxParticles = Mathf.Max(8, Mathf.RoundToInt(main.maxParticles * 0.65f));
            }
            StatusLabel = "QUALITY · ADAPTIVE " + SmoothedFps.ToString("0") + " FPS";
            QualityChanged?.Invoke(StatusLabel);
        }

        private void RestoreQuality()
        {
            _reduced = false;
            _goodSamples = 0;
            QualitySettings.shadowDistance = _originalShadowDistance;
            QualitySettings.lodBias = _originalLodBias;
            foreach (var pair in _particleBudgets)
            {
                if (pair.Key == null) continue;
                var main = pair.Key.main;
                main.maxParticles = pair.Value;
            }
            _particleBudgets.Clear();
            StatusLabel = "QUALITY · FULL " + SmoothedFps.ToString("0") + " FPS";
            QualityChanged?.Invoke(StatusLabel);
        }
    }
}
