using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IdeaZoo.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayDepthHud : MonoBehaviour
    {
        public event Action<int> ChoiceSelected;
        public event Action CancelRequested;

        private Font _font;
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
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 125;
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
            objectValue.GetComponent<Image>().color = normal;
            var button = objectValue.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Brass;
            colors.pressedColor = new Color(0.15f, 0.55f, 0.52f, 1f);
            button.colors = colors;
            objectValue.GetComponent<LayoutElement>().minHeight = 52f;
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
}
