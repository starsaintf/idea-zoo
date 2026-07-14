using System;
using System.Collections.Generic;
using System.Linq;
using IdeaZoo.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdeaZoo.Runtime
{
    [DisallowMultipleComponent]
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action<bool> Changed;
        public void OnPointerDown(PointerEventData eventData) { Changed?.Invoke(true); }
        public void OnPointerUp(PointerEventData eventData) { Changed?.Invoke(false); }
        public void OnPointerExit(PointerEventData eventData) { Changed?.Invoke(false); }
    }

    [DisallowMultipleComponent]
    public sealed class IdeaZooHud : MonoBehaviour
    {
        public event Action<IdeaIntake> IntakeSubmitted;
        public event Action<string, int, string> EvidenceSubmitted;
        public event Action<string, string, List<string>> MoltSubmitted;
        public event Action Continued;
        public event Action Cancelled;
        public event Action RestartRequested;
        public event Action InteractRequested;
        public event Action<bool> LensChanged;

        public MobileJoystick Joystick { get; private set; }
        public bool OverlayOpen { get { return _overlay != null && _overlay.activeSelf; } }

        private Font _font;
        private GameObject _safeRoot;
        private GameObject _touchRoot;
        private GameObject _overlay;
        private RectTransform _overlayContent;
        private Text _objective;
        private Text _specimen;
        private Text _metrics;
        private Text _progress;
        private Text _prompt;
        private Text _inlineError;
        private string _currentTest = string.Empty;
        private int _selectedStrength = -1;
        private readonly Dictionary<string, InputField> _fields = new Dictionary<string, InputField>();
        private readonly List<Toggle> _guardrails = new List<Toggle>();
        private bool _submitLocked;

        private static readonly Color Ink = new Color(0.02f, 0.05f, 0.06f, 0.96f);
        private static readonly Color Paper = new Color(0.90f, 0.86f, 0.76f, 1f);
        private static readonly Color Brass = new Color(0.79f, 0.59f, 0.31f, 1f);
        private static readonly Color Teal = new Color(0.29f, 0.76f, 0.69f, 1f);
        private static readonly Color Rust = new Color(0.76f, 0.30f, 0.25f, 1f);
        private static readonly Color Violet = new Color(0.54f, 0.43f, 0.74f, 1f);

        public void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasObject = new GameObject("IdeaZooCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(896f, 414f);
            scaler.matchWidthOrHeight = 0.5f;

            _safeRoot = UiObject("SafeArea", canvasObject.transform, typeof(SafeAreaFitter));
            Stretch(_safeRoot.GetComponent<RectTransform>());
            BuildStatus();
            BuildTouchControls();
            BuildOverlay();
        }

        public void ShowIntake()
        {
            OpenOverlay();
            ClearOverlay();
            ResetStatus();
            AddTitle("THE WHISPER GATE");
            AddBody("Bring an idea you may actually spend years building. The Zoo will give it a body, test what it consumes and help you decide whether it deserves more of your life.");
            AddSection("THE REAL IDEA");
            AddField("title", "Name the idea", false);
            AddField("idea", "Describe it plainly. No pitch language.", true);
            AddField("problem", "What painful problem exists without it?", true);
            AddField("promise", "What measurable outcome does it promise?", true);
            AddField("audience", "Who is the first specific user?", false);
            AddField("payer", "Who pays or commits resources?", false);
            AddField("evidence", "What have you already tested?", true);
            AddField("dependency", "What must exist for it to work?", true);
            AddField("maintenance", "Who keeps it alive after launch?", true);
            AddField("harm", "What is its cruelest plausible use?", true);
            AddError();
            AddButton("HATCH THIS IDEA", Brass, SubmitIntake);
        }

        public void ShowStory(string title, string body, string buttonText)
        {
            OpenOverlay();
            ClearOverlay();
            AddTitle(title);
            AddBody(body);
            AddButton(buttonText, Brass, delegate
            {
                if (LockSubmit()) return;
                CloseOverlay();
                Continued?.Invoke();
            });
        }

        public void ShowEvidence(string testId, string title, string question, string mission)
        {
            OpenOverlay();
            ClearOverlay();
            _currentTest = testId;
            _selectedStrength = -1;
            AddTitle(title);
            AddBody(question);
            AddSection("REAL-WORLD MISSION");
            AddBody(mission);
            var selected = AddBody("No evidence strength selected.", Brass);
            AddButton("0 · NO EVIDENCE YET", new Color(0.25f, 0.30f, 0.31f), delegate { _selectedStrength = 0; selected.text = "Selected: no evidence yet."; });
            AddButton("1 · ANECDOTE OR WEAK SIGNAL", Brass, delegate { _selectedStrength = 1; selected.text = "Selected: anecdote or weak signal."; });
            AddButton("2 · REPEATED BEHAVIOUR", Teal, delegate { _selectedStrength = 2; selected.text = "Selected: repeated behaviour."; });
            AddButton("3 · MONEY, PILOT OR COSTLY COMMITMENT", Teal, delegate { _selectedStrength = 3; selected.text = "Selected: costly commitment."; });
            AddField("test_note", "What actually happened? Record names, numbers or the strongest contradiction.", true);
            AddError();
            AddButton("RECORD THIS EVIDENCE", Teal, SubmitEvidence);
            AddButton("RETURN WITHOUT RECORDING", new Color(0.28f, 0.33f, 0.34f), delegate
            {
                if (LockSubmit()) return;
                CloseOverlay();
                Cancelled?.Invoke();
            });
        }

        public void ShowMolt(IdeaProfile profile)
        {
            OpenOverlay();
            ClearOverlay();
            AddTitle("THE MOLT HOUSE");
            AddBody("Do not defend the original shape. Preserve the useful core and change what reality has made indefensible.");
            AddField("revised_promise", "Revised measurable promise", true, profile.Promise);
            AddField("revised_audience", "Narrower first audience", false, profile.Audience);
            AddSection("RULES TO ADD TO THE CREATURE");
            _guardrails.Clear();
            foreach (var rule in new[]
            {
                "People can refuse without penalty",
                "A named keeper owns maintenance",
                "The idea has a clear boundary",
                "Uncertainty remains visible",
                "Users can appeal, delete or recall",
                "The idea expires unless deliberately renewed"
            }) AddGuardrail(rule);
            AddError();
            AddButton("LET IT MOLT", Violet, SubmitMolt);
            AddButton("RETURN TO THE ZOO", new Color(0.28f, 0.33f, 0.34f), delegate
            {
                if (LockSubmit()) return;
                CloseOverlay();
                Cancelled?.Invoke();
            });
        }

        public void ShowResult(IdeaProfile profile, bool saveSucceeded, string saveMessage)
        {
            OpenOverlay();
            ClearOverlay();
            AddTitle("RULING · " + profile.FinalRuling.ToString().ToUpperInvariant());
            AddBody(profile.VerdictReason);
            AddSection("THE IDEA THAT LEAVES THE ZOO");
            AddBody(profile.Title + "\n\nPromise: " + profile.Promise + "\nFirst user: " + profile.Audience + "\nClass: " + profile.Class + " · Appetite: " + profile.Appetite);
            AddSection("NEXT REAL-WORLD ACTIONS");
            for (var i = 0; i < profile.NextActions.Count; i++) AddBody((i + 1) + ". " + profile.NextActions[i]);
            AddBody(saveSucceeded ? "The private specimen record was saved on this device." : "The ruling completed, but the archive could not be saved: " + saveMessage, saveSucceeded ? Teal : Rust);
            AddButton("BRING ANOTHER IDEA", Brass, delegate
            {
                if (LockSubmit()) return;
                RestartRequested?.Invoke();
            });
        }

        public void SetObjective(string title, string detail)
        {
            if (_objective != null) _objective.text = title + "\n" + detail;
        }

        public void SetSpecimen(IdeaProfile profile)
        {
            if (_specimen != null) _specimen.text = profile.CreatureName + "\n" + profile.Class + " · feeds on " + profile.Appetite;
            SetMetrics(profile);
        }

        public void SetMetrics(IdeaProfile profile)
        {
            if (_metrics == null) return;
            _metrics.text = string.Format("E {0:0} · D {1:0} · V {2:0} · S {3:0}", profile.Metrics.Evidence * 100.0, profile.Metrics.Desirability * 100.0, profile.Metrics.Viability * 100.0, profile.Metrics.Safety * 100.0);
        }

        public void SetProgress(int current, int total)
        {
            if (_progress != null) _progress.text = current + " / " + total;
        }

        public void SetPrompt(string value)
        {
            if (_prompt == null) return;
            _prompt.text = value;
            _prompt.gameObject.SetActive(!string.IsNullOrWhiteSpace(value) && !OverlayOpen);
        }

        public void CloseOverlay()
        {
            _overlay.SetActive(false);
            _touchRoot.SetActive(Application.isMobilePlatform || Input.touchSupported);
            _submitLocked = false;
            Joystick.ResetInput();
            LensChanged?.Invoke(false);
        }

        public void ResetTransientInput()
        {
            Joystick.ResetInput();
            LensChanged?.Invoke(false);
        }

        private void BuildStatus()
        {
            var panel = Panel("StatusPanel", _safeRoot.transform, Ink, Teal);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(12f, -84f);
            rect.offsetMax = new Vector2(-12f, -10f);
            var layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.spacing = 12f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            _objective = Label("Objective", panel.transform, "WHISPER GATE", 15, Paper, TextAnchor.MiddleLeft);
            _specimen = Label("Specimen", panel.transform, "NO SPECIMEN", 14, Brass, TextAnchor.MiddleLeft);
            _metrics = Label("Metrics", panel.transform, "E 0 · D 0 · V 0 · S 0", 13, Teal, TextAnchor.MiddleRight);
            _progress = Label("Progress", panel.transform, "0 / 4", 14, Paper, TextAnchor.MiddleRight);
            _progress.gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;

            var promptPanel = Panel("Prompt", _safeRoot.transform, new Color(0.02f, 0.05f, 0.06f, 0.90f), Brass);
            var promptRect = promptPanel.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.23f, 0f);
            promptRect.anchorMax = new Vector2(0.77f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.offsetMin = new Vector2(0f, 18f);
            promptRect.offsetMax = new Vector2(0f, 66f);
            _prompt = Label("PromptText", promptPanel.transform, string.Empty, 15, Paper, TextAnchor.MiddleCenter);
            Stretch(_prompt.rectTransform);
            promptPanel.SetActive(false);
        }

        private void BuildTouchControls()
        {
            _touchRoot = UiObject("TouchControls", _safeRoot.transform);
            Stretch(_touchRoot.GetComponent<RectTransform>());
            _touchRoot.SetActive(Application.isMobilePlatform || Input.touchSupported);

            var joystickObject = UiObject("Joystick", _touchRoot.transform);
            Joystick = joystickObject.AddComponent<MobileJoystick>();
            Joystick.Build(new Color(0.02f, 0.10f, 0.12f, 0.70f), new Color(0.30f, 0.80f, 0.72f, 0.86f));
            var joystickRect = joystickObject.GetComponent<RectTransform>();
            joystickRect.anchorMin = new Vector2(0f, 0f);
            joystickRect.anchorMax = new Vector2(0f, 0f);
            joystickRect.pivot = new Vector2(0f, 0f);
            joystickRect.anchoredPosition = new Vector2(34f, 24f);

            var interact = AddTouchButton("TOUCH", new Vector2(1f, 0f), new Vector2(-34f, 24f), new Vector2(108f, 108f), Teal);
            interact.onClick.AddListener(delegate { InteractRequested?.Invoke(); });

            var lens = AddTouchButton("LENS", new Vector2(1f, 0f), new Vector2(-158f, 24f), new Vector2(82f, 82f), Brass);
            var hold = lens.gameObject.AddComponent<HoldButton>();
            hold.Changed += active => LensChanged?.Invoke(active);
        }

        private void BuildOverlay()
        {
            _overlay = Panel("Overlay", _safeRoot.transform, new Color(0.012f, 0.035f, 0.045f, 0.992f), Brass);
            Stretch(_overlay.GetComponent<RectTransform>());
            var scrollObject = UiObject("Scroll", _overlay.transform, typeof(ScrollRect));
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(22f, 18f);
            scrollRectTransform.offsetMax = new Vector2(-22f, -18f);
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = UiObject("Viewport", scrollObject.transform, typeof(Image), typeof(Mask));
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = Color.clear;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewport.GetComponent<RectTransform>();

            var content = UiObject("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _overlayContent = content.GetComponent<RectTransform>();
            _overlayContent.anchorMin = new Vector2(0f, 1f);
            _overlayContent.anchorMax = new Vector2(1f, 1f);
            _overlayContent.pivot = new Vector2(0.5f, 1f);
            _overlayContent.offsetMin = Vector2.zero;
            _overlayContent.offsetMax = Vector2.zero;
            var vertical = content.GetComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 10, 18);
            vertical.spacing = 10f;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _overlayContent;
        }

        private void SubmitIntake()
        {
            if (LockSubmit()) return;
            var intake = new IdeaIntake
            {
                Title = Field("title"), Idea = Field("idea"), Problem = Field("problem"), Promise = Field("promise"),
                Audience = Field("audience"), Payer = Field("payer"), Evidence = Field("evidence"), Dependency = Field("dependency"),
                Maintenance = Field("maintenance"), Harm = Field("harm")
            };
            if (string.IsNullOrWhiteSpace(intake.Title) || string.IsNullOrWhiteSpace(intake.Idea) || string.IsNullOrWhiteSpace(intake.Promise))
            {
                UnlockWithError("The Gate needs a name, a plain description and a measurable promise.");
                return;
            }
            IntakeSubmitted?.Invoke(intake);
        }

        private void SubmitEvidence()
        {
            if (LockSubmit()) return;
            var note = Field("test_note");
            if (_selectedStrength < 0)
            {
                UnlockWithError("Choose the strength of the evidence before recording it.");
                return;
            }
            if (string.IsNullOrWhiteSpace(note))
            {
                UnlockWithError("Record what happened, even when the result was 'nothing happened'.");
                return;
            }
            EvidenceSubmitted?.Invoke(_currentTest, _selectedStrength, note);
        }

        private void SubmitMolt()
        {
            if (LockSubmit()) return;
            var promise = Field("revised_promise");
            var audience = Field("revised_audience");
            if (string.IsNullOrWhiteSpace(promise) || string.IsNullOrWhiteSpace(audience))
            {
                UnlockWithError("The Molt House needs a measurable promise and a specific first audience.");
                return;
            }
            MoltSubmitted?.Invoke(promise, audience, _guardrails.Where(toggle => toggle.isOn).Select(toggle => toggle.GetComponentInChildren<Text>().text).ToList());
        }

        private bool LockSubmit()
        {
            if (_submitLocked) return true;
            _submitLocked = true;
            return false;
        }

        private void UnlockWithError(string message)
        {
            _submitLocked = false;
            if (_inlineError != null)
            {
                _inlineError.text = message;
                _inlineError.gameObject.SetActive(true);
            }
        }

        private void OpenOverlay()
        {
            _overlay.SetActive(true);
            _touchRoot.SetActive(false);
            _submitLocked = false;
            ResetTransientInput();
            SetPrompt(string.Empty);
        }

        private void ClearOverlay()
        {
            _fields.Clear();
            _guardrails.Clear();
            _inlineError = null;
            _currentTest = string.Empty;
            _selectedStrength = -1;
            foreach (Transform child in _overlayContent) Destroy(child.gameObject);
            _overlayContent.anchoredPosition = Vector2.zero;
        }

        private void ResetStatus()
        {
            _objective.text = "WHISPER GATE";
            _specimen.text = "NO SPECIMEN";
            _metrics.text = "E 0 · D 0 · V 0 · S 0";
            _progress.text = "0 / 4";
            SetPrompt(string.Empty);
        }

        private void AddTitle(string value)
        {
            var title = Label("Title", _overlayContent, value, 34, Brass, TextAnchor.MiddleCenter);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
        }

        private Text AddBody(string value, Color? color = null)
        {
            var text = Label("Body", _overlayContent, value, 16, color ?? Paper, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return text;
        }

        private void AddSection(string value)
        {
            var panel = Panel("Section", _overlayContent, new Color(0.02f, 0.09f, 0.10f, 0.94f), Teal);
            panel.AddComponent<LayoutElement>().preferredHeight = 42f;
            var text = Label("SectionText", panel.transform, value, 17, Teal, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, 12f, 8f, -12f, -8f);
        }

        private void AddField(string key, string placeholder, bool multiline, string value = "")
        {
            var root = Panel("Field_" + key, _overlayContent, new Color(0.02f, 0.065f, 0.075f, 0.96f), new Color(0.18f, 0.34f, 0.35f));
            root.AddComponent<LayoutElement>().preferredHeight = multiline ? 92f : 50f;
            var text = Label("Text", root.transform, value, 16, Paper, TextAnchor.UpperLeft);
            Stretch(text.rectTransform, 12f, 8f, -12f, -8f);
            var placeholderText = Label("Placeholder", root.transform, placeholder, 15, new Color(0.52f, 0.58f, 0.58f), TextAnchor.UpperLeft);
            Stretch(placeholderText.rectTransform, 12f, 8f, -12f, -8f);
            var input = root.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.text = value;
            input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            input.shouldHideMobileInput = false;
            _fields[key] = input;
        }

        private void AddGuardrail(string value)
        {
            var root = Panel("Guardrail", _overlayContent, new Color(0.03f, 0.075f, 0.085f, 0.94f), new Color(0.25f, 0.34f, 0.35f));
            root.AddComponent<LayoutElement>().preferredHeight = 46f;
            var toggle = root.AddComponent<Toggle>();
            var check = Panel("Check", root.transform, new Color(0.10f, 0.16f, 0.17f, 1f), Teal);
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            checkRect.anchoredPosition = new Vector2(10f, 0f);
            checkRect.sizeDelta = new Vector2(28f, 28f);
            var mark = Panel("Mark", check.transform, Teal, Teal);
            Stretch(mark.GetComponent<RectTransform>(), 6f, 6f, -6f, -6f);
            toggle.targetGraphic = check.GetComponent<Image>();
            toggle.graphic = mark.GetComponent<Image>();
            toggle.isOn = false;
            var label = Label("Label", root.transform, value, 15, Paper, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, 52f, 4f, -10f, -4f);
            _guardrails.Add(toggle);
        }

        private void AddError()
        {
            _inlineError = AddBody(string.Empty, Rust);
            _inlineError.gameObject.SetActive(false);
        }

        private Button AddButton(string value, Color color, UnityEngine.Events.UnityAction action)
        {
            var root = Panel("Button_" + value.Replace(' ', '_'), _overlayContent, color, color);
            root.AddComponent<LayoutElement>().preferredHeight = 52f;
            var button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            button.onClick.AddListener(action);
            var label = Label("Label", root.transform, value, 17, Ink, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return button;
        }

        private Button AddTouchButton(string value, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var root = Panel("Touch_" + value, _touchRoot.transform, color, color);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            var label = Label("Label", root.transform, value, 16, Ink, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return button;
        }

        private string Field(string key)
        {
            InputField field;
            return _fields.TryGetValue(key, out field) ? field.text.Trim() : string.Empty;
        }

        private GameObject Panel(string objectName, Transform parent, Color background, Color border)
        {
            var root = UiObject(objectName, parent, typeof(Image));
            var image = root.GetComponent<Image>();
            image.color = background;
            image.raycastTarget = true;
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(border.r, border.g, border.b, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return root;
        }

        private Text Label(string objectName, Transform parent, string value, int size, Color color, TextAnchor anchor)
        {
            var root = UiObject(objectName, parent, typeof(Text));
            var label = root.GetComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.text = value;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static GameObject UiObject(string objectName, Transform parent, params Type[] extra)
        {
            var types = new List<Type> { typeof(RectTransform) };
            types.AddRange(extra);
            var root = new GameObject(objectName, types.ToArray());
            root.transform.SetParent(parent, false);
            return root;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }
    }
}
