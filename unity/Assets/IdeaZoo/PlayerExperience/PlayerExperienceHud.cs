using System;
using IdeaZoo.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace IdeaZoo.PlayerExperience
{
    [DisallowMultipleComponent]
    public sealed class PlayerExperienceHud : MonoBehaviour
    {
        public event Action BeginTutorialRequested;
        public event Action ContinueWithoutTutorialRequested;
        public event Action ReplayTutorialRequested;
        public event Action<PlayerAccessibilitySettings> AccessibilityChanged;

        private Font _font;
        private GameObject _overlay;
        private GameObject _banner;
        private Text _bannerText;
        private GameObject _accessButton;
        private PlayerAccessibilitySettings _settings;

        private static readonly Color Ink = new Color(.008f, .018f, .025f, .97f);
        private static readonly Color Paper = new Color(.94f, .90f, .80f, 1f);
        private static readonly Color Brass = new Color(.92f, .65f, .27f, 1f);
        private static readonly Color Teal = new Color(.25f, .78f, .71f, 1f);
        private static readonly Color Rust = new Color(.80f, .29f, .22f, 1f);
        private static readonly Color Muted = new Color(.18f, .23f, .25f, 1f);

        public bool OverlayOpen { get { return _overlay != null && _overlay.activeSelf; } }

        public void Build(PlayerAccessibilitySettings settings)
        {
            _settings = settings ?? new PlayerAccessibilitySettings();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasObject = new GameObject("PlayerExperienceCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 145;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(896f, 414f);
            scaler.matchWidthOrHeight = .5f;

            var safe = new GameObject("PlayerExperienceSafeArea", typeof(RectTransform));
            safe.transform.SetParent(canvasObject.transform, false);
            Stretch(safe.GetComponent<RectTransform>());
            safe.AddComponent<SafeAreaFitter>();

            BuildBanner(safe.transform);
            BuildAccessButton(safe.transform);
            _overlay = Panel("PlayerExperienceOverlay", safe.transform, new Color(.003f, .008f, .012f, .95f));
            Stretch(_overlay.GetComponent<RectTransform>());
            _overlay.SetActive(false);
        }

        public void ShowOnboarding(bool tutorialCompleted)
        {
            ClearOverlay();
            OpenOverlay();
            var panel = ContentPanel();
            AddTitle(panel, tutorialCompleted ? "RETURN TO THE FIRST CASE" : "YOUR FIRST DAY AS KEEPER");
            AddBody(panel, "The Zoo is easiest to understand when one small idea is allowed to fail honestly. The guided case teaches the real loop using the same habitats, creature, resources and rulings as every idea you bring later.");
            AddSection(panel, "THE NEIGHBOURHOOD UMBRELLA LIBRARY");
            AddBody(panel, "A seven-minute case about shared umbrellas, sudden rain, maintenance and privacy. No tutorial room. No separate simulation. The case enters through the real Whisper Gate.");
            AddButton(panel, tutorialCompleted ? "REPLAY GUIDED CASE" : "BEGIN THE GUIDED CASE", Brass, delegate
            {
                CloseOverlay();
                if (tutorialCompleted) ReplayTutorialRequested?.Invoke();
                else BeginTutorialRequested?.Invoke();
            });
            AddButton(panel, "BRING MY OWN IDEA", Muted, delegate
            {
                CloseOverlay();
                ContinueWithoutTutorialRequested?.Invoke();
            });
        }

        public void ShowTutorialStep(string title, string detail)
        {
            ShowBanner(title + "\n" + detail, Brass, 7f);
        }

        public void ShowReaction(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            ShowBanner(line, Teal, 6f);
        }

        public void ShowArchetypeReveal(PlayerExperienceCaseRecord record, KeeperRank rank)
        {
            if (record == null) return;
            ClearOverlay();
            OpenOverlay();
            var panel = ContentPanel();
            AddTitle(panel, "THE HIDDEN PATTERN");
            AddSection(panel, PlayerExperienceArchetypeCatalog.Reveal(record.Archetype));
            AddBody(panel, record.Consequence);
            if (record.TactileFindings != null && record.TactileFindings.Count > 0)
            {
                AddSection(panel, "WHAT YOUR HANDS FOUND");
                for (var i = 0; i < record.TactileFindings.Count && i < 4; i++) AddBody(panel, "• " + record.TactileFindings[i]);
            }
            AddBody(panel, PlayerExperienceReactionCatalog.AtRuling(record));
            AddBody(panel, "Keeper rank: " + rank.ToString().ToUpperInvariant(), Brass);
            AddButton(panel, "RETURN TO THE ZOO", Brass, CloseOverlay);
        }

        public void ShowAccessibility()
        {
            ClearOverlay();
            OpenOverlay();
            var panel = ContentPanel();
            AddTitle(panel, "ACCESSIBILITY & COMFORT");
            AddBody(panel, "These settings apply immediately and are saved on this device.");
            AddToggle(panel, "TEXT SIZE", delegate { return _settings.TextScaleLabel; }, delegate
            {
                _settings.TextScaleIndex = (_settings.TextScaleIndex + 1) % 3;
            });
            AddToggle(panel, "REDUCED MOTION", delegate { return OnOff(_settings.ReducedMotion); }, delegate
            {
                _settings.ReducedMotion = !_settings.ReducedMotion;
            });
            AddToggle(panel, "HIGH CONTRAST", delegate { return OnOff(_settings.HighContrast); }, delegate
            {
                _settings.HighContrast = !_settings.HighContrast;
            });
            AddToggle(panel, "LARGE TOUCH TARGETS", delegate { return OnOff(_settings.LargeTouchTargets); }, delegate
            {
                _settings.LargeTouchTargets = !_settings.LargeTouchTargets;
            });
            AddToggle(panel, "DECISION FOCUS MODE", delegate { return OnOff(_settings.FocusMode); }, delegate
            {
                _settings.FocusMode = !_settings.FocusMode;
            });
            AddToggle(panel, "HAPTIC FEEDBACK", delegate { return OnOff(_settings.Haptics); }, delegate
            {
                _settings.Haptics = !_settings.Haptics;
            });
            AddButton(panel, "SAVE AND RETURN", Brass, delegate
            {
                AccessibilityChanged?.Invoke(_settings);
                CloseOverlay();
            });
            AddButton(panel, "REPLAY GUIDED CASE", Muted, delegate
            {
                CloseOverlay();
                ReplayTutorialRequested?.Invoke();
            });
        }

        public void HideOverlay()
        {
            CloseOverlay();
        }

        private void BuildBanner(Transform parent)
        {
            _banner = Panel("PlayerExperienceBanner", parent, new Color(.02f, .05f, .06f, .96f));
            var rect = _banner.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.16f, 1f);
            rect.anchorMax = new Vector2(.84f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.offsetMin = new Vector2(0f, -176f);
            rect.offsetMax = new Vector2(0f, -126f);
            _bannerText = Label("PlayerExperienceBannerText", _banner.transform, string.Empty, 14, Paper, TextAnchor.MiddleCenter);
            Stretch(_bannerText.rectTransform, 10f);
            _banner.SetActive(false);
        }

        private void BuildAccessButton(Transform parent)
        {
            _accessButton = ButtonObject("AccessibilityButton", parent, Muted).gameObject;
            var rect = _accessButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(110f, 42f);
            rect.anchoredPosition = new Vector2(-18f, -92f);
            var button = _accessButton.GetComponent<Button>();
            SetButtonText(button, "ACCESS");
            button.onClick.AddListener(ShowAccessibility);
        }

        private void ShowBanner(string value, Color accent, float seconds)
        {
            if (_banner == null || OverlayOpen) return;
            StopAllCoroutines();
            _banner.GetComponent<Image>().color = new Color(accent.r * .12f, accent.g * .12f, accent.b * .12f, .96f);
            _bannerText.text = value;
            _banner.SetActive(true);
            StartCoroutine(HideBanner(seconds));
        }

        private System.Collections.IEnumerator HideBanner(float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(2f, seconds));
            if (_banner != null) _banner.SetActive(false);
        }

        private GameObject ContentPanel()
        {
            var panel = Panel("PlayerExperienceContent", _overlay.transform, Ink);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.08f, .06f);
            rect.anchorMax = new Vector2(.92f, .94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.spacing = 10f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            return panel;
        }

        private void AddTitle(GameObject panel, string value)
        {
            var text = Label("Title", panel.transform, value, 25, Brass, TextAnchor.MiddleLeft);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        }

        private void AddSection(GameObject panel, string value)
        {
            var text = Label("Section", panel.transform, value, 16, Teal, TextAnchor.MiddleLeft);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        }

        private void AddBody(GameObject panel, string value, Color? color = null)
        {
            var text = Label("Body", panel.transform, value, 14, color ?? Paper, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = Mathf.Clamp(34f + value.Length * .18f, 42f, 96f);
        }

        private void AddButton(GameObject panel, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            var button = ButtonObject("Action", panel.transform, color);
            SetButtonText(button, label);
            button.onClick.AddListener(action);
        }

        private void AddToggle(GameObject panel, string label, Func<string> value, Action mutate)
        {
            var button = ButtonObject("Setting_" + label.Replace(" ", string.Empty), panel.transform, Muted);
            Action refresh = delegate { SetButtonText(button, label + " · " + value()); };
            refresh();
            button.onClick.AddListener(delegate
            {
                mutate();
                refresh();
                AccessibilityChanged?.Invoke(_settings);
            });
        }

        private Button ButtonObject(string name, Transform parent, Color color)
        {
            var value = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            value.transform.SetParent(parent, false);
            value.GetComponent<Image>().color = color;
            value.GetComponent<LayoutElement>().minHeight = 52f;
            var button = value.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Brass;
            colors.pressedColor = Teal;
            button.colors = colors;
            var text = Label("Label", value.transform, string.Empty, 14, new Color(.02f, .04f, .05f), TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color, TextAnchor anchor)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Text));
            node.transform.SetParent(parent, false);
            var text = node.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = false;
            return text;
        }

        private GameObject Panel(string name, Transform parent, Color color)
        {
            var value = new GameObject(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            value.GetComponent<Image>().color = color;
            return value;
        }

        private void ClearOverlay()
        {
            for (var i = _overlay.transform.childCount - 1; i >= 0; i--) Destroy(_overlay.transform.GetChild(i).gameObject);
        }

        private void OpenOverlay()
        {
            _overlay.SetActive(true);
            _overlay.transform.SetAsLastSibling();
            if (_accessButton != null) _accessButton.SetActive(false);
        }

        private void CloseOverlay()
        {
            if (_overlay != null) _overlay.SetActive(false);
            if (_accessButton != null) _accessButton.SetActive(true);
        }

        private static string OnOff(bool value) { return value ? "ON" : "OFF"; }

        private static void SetButtonText(Button button, string value)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = value;
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = Vector2.one * -padding;
        }
    }
}
