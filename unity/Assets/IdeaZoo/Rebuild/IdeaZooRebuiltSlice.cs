using System;
using System.Collections;
using System.Collections.Generic;
using IdeaZoo.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace IdeaZoo.Rebuild
{
    public static class RebuiltSliceInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindAnyObjectByType<RebuiltSliceDirector>() != null) return;
            new GameObject("IDEA_ZOO_REBUILT_SLICE").AddComponent<RebuiltSliceDirector>();
        }
    }

    [DefaultExecutionOrder(2000)]
    public sealed class RebuiltSliceDirector : MonoBehaviour
    {
        private enum Mission { Track, Investigate, Contain, Decide, Complete }

        private readonly List<Clue> _clues = new List<Clue>();
        private ThirdPersonKeeperController _keeper;
        private Transform _creature;
        private Transform _beacon;
        private Transform _releaseGate;
        private Transform _containGate;
        private Text _objective;
        private Text _detail;
        private Text _prompt;
        private Image _progressFill;
        private Mission _mission;
        private bool _lens;
        private float _capture;
        private float _creatureClock;
        private Vector3 _arenaOrigin;
        private Vector3 _creatureTarget;

        private IEnumerator Start()
        {
            yield return null;
            var game = FindAnyObjectByType<IdeaZooGame>();
            if (game == null || game.Keeper == null) yield break;

            _keeper = game.Keeper;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                canvas.gameObject.SetActive(false);
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (behaviour == this || behaviour == _keeper) continue;
                if (behaviour.transform.IsChildOf(_keeper.transform)) continue;
                behaviour.enabled = false;
            }
            if (game.World != null) game.World.gameObject.SetActive(false);
            if (game.Creature != null) game.Creature.gameObject.SetActive(false);

            _arenaOrigin = new Vector3(0f, 0f, 0f);
            var controller = _keeper.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            _keeper.transform.position = new Vector3(0f, 0.05f, -18f);
            _keeper.transform.rotation = Quaternion.identity;
            if (controller != null) controller.enabled = true;
            _keeper.SetLocked(false);
            _keeper.InteractRequested += Interact;
            _keeper.LensChanged += LensChanged;

            BuildArena();
            BuildHud();
            SetMission(Mission.Track);
        }

        private void OnDestroy()
        {
            if (_keeper == null) return;
            _keeper.InteractRequested -= Interact;
            _keeper.LensChanged -= LensChanged;
        }

        private void Update()
        {
            if (_keeper == null || _creature == null) return;
            AnimateCreature();
            UpdateGuidance();
            if (_mission == Mission.Track && Vector3.Distance(_keeper.transform.position, _creature.position) < 10f)
                SetMission(Mission.Investigate);
            if (_mission == Mission.Contain) UpdateCapture();
        }

        private void BuildArena()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.025f, 0.045f, 0.075f);
            RenderSettings.fogDensity = 0.009f;
            Primitive("Ground", PrimitiveType.Cube, _arenaOrigin + new Vector3(0f, -0.55f, 8f), new Vector3(42f, 1f, 62f), new Color(0.035f, 0.07f, 0.085f));
            Primitive("CentralPath", PrimitiveType.Cube, _arenaOrigin + new Vector3(0f, 0.02f, 8f), new Vector3(8f, 0.08f, 58f), new Color(0.16f, 0.19f, 0.20f));

            for (var i = 0; i < 18; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var z = -15f + (i / 2) * 6.2f;
                Primitive("LanternPost", PrimitiveType.Cylinder, new Vector3(side * 5.2f, 1.4f, z), new Vector3(.12f, 1.4f, .12f), new Color(.08f, .10f, .13f));
                var lamp = Primitive("Lantern", PrimitiveType.Sphere, new Vector3(side * 5.2f, 3f, z), Vector3.one * .42f, new Color(.2f, .9f, .75f));
                SetEmission(lamp.GetComponent<Renderer>(), new Color(.15f, 1f, .72f), 2.8f);
            }

            AddClue("FRACTURED PROMISE", new Vector3(-8f, .3f, 2f), new Color(1f, .45f, .2f), "A launch date was promised before the system could survive one user.");
            AddClue("BORROWED VOICES", new Vector3(8f, .3f, 13f), new Color(.45f, .72f, 1f), "The creature repeats praise, but none of it came from real interviews.");
            AddClue("HIDDEN BURDEN", new Vector3(-7f, .3f, 25f), new Color(.85f, .35f, 1f), "Every success creates unpaid work for someone outside the pitch.");

            _creature = new GameObject("EscapedIdeaCreature").transform;
            _creature.position = new Vector3(0f, 1.35f, 20f);
            var body = Primitive("CreatureCore", PrimitiveType.Sphere, _creature.position, new Vector3(2.1f, 1.25f, 2.6f), new Color(.12f, .55f, .62f));
            body.transform.SetParent(_creature, true);
            SetEmission(body.GetComponent<Renderer>(), new Color(.08f, .9f, .8f), 1.7f);
            for (var i = 0; i < 3; i++)
            {
                var shard = Primitive("OrbitingClaim", PrimitiveType.Cube, _creature.position, new Vector3(.25f, .8f, .25f), new Color(1f, .58f, .2f));
                shard.transform.SetParent(_creature, true);
                shard.transform.localPosition = Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 2f;
            }
            _creatureTarget = _creature.position;

            _beacon = Primitive("GuidanceBeacon", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.16f, 3f, .16f), new Color(.2f, 1f, .78f)).transform;
            SetEmission(_beacon.GetComponent<Renderer>(), new Color(.15f, 1f, .72f), 3.5f);
            _beacon.gameObject.SetActive(false);

            _releaseGate = BuildGate("RELEASE", new Vector3(-7f, 0f, 37f), new Color(.2f, .85f, .55f));
            _containGate = BuildGate("CONTAIN", new Vector3(7f, 0f, 37f), new Color(1f, .35f, .22f));
            _releaseGate.gameObject.SetActive(false);
            _containGate.gameObject.SetActive(false);
        }

        private void BuildHud()
        {
            var root = new GameObject("RebuiltGameplayHUD");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();

            var panel = UiImage(root.transform, "MissionPanel", new Color(.015f, .025f, .045f, .92f), new Vector2(30f, -30f), new Vector2(570f, 142f), new Vector2(0f, 1f));
            _objective = UiText(panel.transform, "Objective", 29, FontStyle.Bold, new Color(.45f, 1f, .82f), new Vector2(24f, -18f), new Vector2(520f, 44f));
            _detail = UiText(panel.transform, "Detail", 19, FontStyle.Normal, Color.white, new Vector2(24f, -63f), new Vector2(520f, 60f));
            _prompt = UiText(root.transform, "Prompt", 23, FontStyle.Bold, Color.white, new Vector2(0f, 52f), new Vector2(820f, 58f), new Vector2(.5f, 0f), TextAnchor.MiddleCenter);

            var bar = UiImage(root.transform, "CaptureBar", new Color(.02f, .03f, .05f, .9f), new Vector2(0f, 112f), new Vector2(480f, 18f), new Vector2(.5f, 0f));
            _progressFill = UiImage(bar.transform, "Fill", new Color(.2f, 1f, .72f, 1f), Vector2.zero, Vector2.zero, new Vector2(0f, .5f));
            _progressFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _progressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _progressFill.rectTransform.pivot = new Vector2(0f, .5f);
            _progressFill.rectTransform.offsetMin = new Vector2(3f, 3f);
            _progressFill.rectTransform.offsetMax = new Vector2(3f, -3f);
        }

        private void SetMission(Mission mission)
        {
            _mission = mission;
            if (mission == Mission.Track)
            {
                _objective.text = "TRACK THE ESCAPED IDEA";
                _detail.text = "Follow the teal signal into the Lantern Yard.";
            }
            else if (mission == Mission.Investigate)
            {
                _objective.text = "INVESTIGATE THE TRAIL · 0/3";
                _detail.text = "Find the three glowing evidence fractures and inspect them.";
            }
            else if (mission == Mission.Contain)
            {
                _objective.text = "STABILIZE THE CREATURE";
                _detail.text = "Get close. Keep the containment lens trained on it.";
            }
            else if (mission == Mission.Decide)
            {
                _objective.text = "MAKE THE RULING";
                _detail.text = "Release the changed idea—or contain it before it causes harm.";
                _releaseGate.gameObject.SetActive(true);
                _containGate.gameObject.SetActive(true);
            }
        }

        private void Interact()
        {
            if (_mission == Mission.Investigate)
            {
                Clue nearest = null;
                var distance = 3.4f;
                foreach (var clue in _clues)
                {
                    var candidate = Vector3.Distance(_keeper.transform.position, clue.Root.position);
                    if (!clue.Found && candidate < distance) { nearest = clue; distance = candidate; }
                }
                if (nearest == null) return;
                nearest.Found = true;
                nearest.Root.localScale *= .72f;
                var found = _clues.FindAll(item => item.Found).Count;
                _objective.text = "INVESTIGATE THE TRAIL · " + found + "/3";
                _detail.text = nearest.Reveal;
                if (found == 3) SetMission(Mission.Contain);
                return;
            }
            if (_mission != Mission.Decide) return;
            var releaseDistance = Vector3.Distance(_keeper.transform.position, _releaseGate.position);
            var containDistance = Vector3.Distance(_keeper.transform.position, _containGate.position);
            if (Mathf.Min(releaseDistance, containDistance) > 3.5f) return;
            var released = releaseDistance < containDistance;
            _mission = Mission.Complete;
            _objective.text = released ? "RULING: RELEASED WITH CONDITIONS" : "RULING: CONTAINED";
            _detail.text = released ? "The idea leaves smaller, slower, and accountable to the people carrying its burden." : "The idea remains in the Zoo until evidence can justify the risk.";
            _prompt.text = "CASE COMPLETE · KEEP EXPLORING";
            _beacon.gameObject.SetActive(false);
        }

        private void LensChanged(bool active) { _lens = active; }

        private void UpdateCapture()
        {
            var distance = Vector3.Distance(_keeper.transform.position, _creature.position);
            if (_lens && distance < 8f) _capture += Time.deltaTime * Mathf.Lerp(.42f, .2f, distance / 8f);
            else _capture = Mathf.Max(0f, _capture - Time.deltaTime * .12f);
            _capture = Mathf.Clamp01(_capture);
            if (_progressFill != null) _progressFill.rectTransform.anchorMax = new Vector2(_capture, 1f);
            if (_capture >= 1f)
            {
                SetMission(Mission.Decide);
                _creatureTarget = new Vector3(0f, 1.35f, 33f);
            }
        }

        private void UpdateGuidance()
        {
            Transform target = null;
            if (_mission == Mission.Track || _mission == Mission.Contain) target = _creature;
            else if (_mission == Mission.Investigate)
            {
                var best = float.MaxValue;
                foreach (var clue in _clues)
                {
                    if (clue.Found) continue;
                    var distance = Vector3.Distance(_keeper.transform.position, clue.Root.position);
                    if (distance < best) { best = distance; target = clue.Root; }
                }
            }
            if (target != null)
            {
                _beacon.gameObject.SetActive(true);
                _beacon.position = target.position + Vector3.up * (3.2f + Mathf.Sin(Time.time * 3f) * .25f);
            }
            else if (_mission != Mission.Complete) _beacon.gameObject.SetActive(false);

            var nearClue = _mission == Mission.Investigate && _clues.Exists(c => !c.Found && Vector3.Distance(_keeper.transform.position, c.Root.position) < 3.4f);
            var nearCreature = Vector3.Distance(_keeper.transform.position, _creature.position) < 8f;
            var nearGate = _mission == Mission.Decide && (Vector3.Distance(_keeper.transform.position, _releaseGate.position) < 3.5f || Vector3.Distance(_keeper.transform.position, _containGate.position) < 3.5f);
            if (nearClue) _prompt.text = "E · INSPECT EVIDENCE";
            else if (_mission == Mission.Contain && nearCreature) _prompt.text = "HOLD SPACE · CONTAINMENT LENS";
            else if (nearGate) _prompt.text = "E · ISSUE RULING";
            else _prompt.text = "WASD · MOVE     RIGHT-DRAG · LOOK     FOLLOW THE BEACON";
        }

        private void AnimateCreature()
        {
            _creatureClock += Time.deltaTime;
            if (_mission != Mission.Decide && _mission != Mission.Complete && Vector3.Distance(_creature.position, _creatureTarget) < 1.2f)
                _creatureTarget = new Vector3(Mathf.Sin(_creatureClock * .7f) * 7f, 1.35f, 18f + Mathf.Cos(_creatureClock * .43f) * 9f);
            _creature.position = Vector3.MoveTowards(_creature.position, _creatureTarget, Time.deltaTime * (_mission == Mission.Contain ? 2.4f : 1.2f));
            _creature.Rotate(0f, Time.deltaTime * 38f, 0f, Space.World);
            _creature.position += Vector3.up * Mathf.Sin(_creatureClock * 2.4f) * Time.deltaTime * .18f;
        }

        private void AddClue(string name, Vector3 position, Color color, string reveal)
        {
            var root = Primitive(name, PrimitiveType.Sphere, position, new Vector3(1.25f, .55f, 1.25f), color).transform;
            root.rotation = Quaternion.Euler(0f, 0f, 28f);
            SetEmission(root.GetComponent<Renderer>(), color, 2.6f);
            _clues.Add(new Clue(root, reveal));
        }

        private Transform BuildGate(string label, Vector3 position, Color color)
        {
            var root = new GameObject(label + "_GATE").transform;
            root.position = position;
            Primitive(label + "_Left", PrimitiveType.Cube, position + new Vector3(-1.5f, 1.7f, 0f), new Vector3(.35f, 3.4f, .5f), color).transform.SetParent(root, true);
            Primitive(label + "_Right", PrimitiveType.Cube, position + new Vector3(1.5f, 1.7f, 0f), new Vector3(.35f, 3.4f, .5f), color).transform.SetParent(root, true);
            Primitive(label + "_Top", PrimitiveType.Cube, position + new Vector3(0f, 3.4f, 0f), new Vector3(3.4f, .35f, .5f), color).transform.SetParent(root, true);
            return root;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.position = position;
            item.transform.localScale = scale;
            var shader = Resources.Load<Shader>("IdeaZooLit")
                         ?? Shader.Find("IdeaZoo/RuntimeLit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");
            item.GetComponent<Renderer>().material = new Material(shader) { color = color };
            return item;
        }

        private static void SetEmission(Renderer renderer, Color color, float intensity)
        {
            if (renderer == null || renderer.material == null) return;
            if (!renderer.material.HasProperty("_EmissionColor")) return;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", color * intensity);
        }

        private static Image UiImage(Transform parent, string name, Color color, Vector2 position, Vector2 size, Vector2 anchor)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = node.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text UiText(Transform parent, string name, int size, FontStyle style, Color color, Vector2 position, Vector2 dimensions, Vector2? anchor = null, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            var resolved = anchor ?? new Vector2(0f, 1f);
            rect.anchorMin = rect.anchorMax = resolved;
            rect.pivot = resolved;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = node.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private sealed class Clue
        {
            public readonly Transform Root;
            public readonly string Reveal;
            public bool Found;
            public Clue(Transform root, string reveal) { Root = root; Reveal = reveal; }
        }
    }
}
