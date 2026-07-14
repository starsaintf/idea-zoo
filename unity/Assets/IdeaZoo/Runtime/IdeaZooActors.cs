using System;
using System.Collections.Generic;
using IdeaZoo.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdeaZoo.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafe;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rect = transform as RectTransform;
            Apply();
        }

        private void Update()
        {
            if (_lastSafe != Screen.safeArea || _lastScreen.x != Screen.width || _lastScreen.y != Screen.height)
                Apply();
        }

        private void Apply()
        {
            if (_rect == null || Screen.width <= 0 || Screen.height <= 0) return;
            var safe = Screen.safeArea;
            _lastSafe = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }

    [DisallowMultipleComponent]
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Vector2 Value { get; private set; }
        public int PointerId { get; private set; } = int.MinValue;

        private RectTransform _rect;
        private RectTransform _knob;
        private float _radius;

        public void Build(Color background, Color knob)
        {
            _rect = transform as RectTransform;
            if (_rect == null) throw new InvalidOperationException("MobileJoystick must be created on a RectTransform.");
            _rect.sizeDelta = new Vector2(144f, 144f);

            var image = GetComponent<Image>();
            if (image == null) image = gameObject.AddComponent<Image>();
            image.color = background;
            image.raycastTarget = true;

            var knobObject = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            knobObject.transform.SetParent(transform, false);
            _knob = knobObject.GetComponent<RectTransform>();
            _knob.anchorMin = _knob.anchorMax = new Vector2(0.5f, 0.5f);
            _knob.pivot = new Vector2(0.5f, 0.5f);
            _knob.sizeDelta = new Vector2(66f, 66f);
            knobObject.GetComponent<Image>().color = knob;
            _radius = 52f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (PointerId != int.MinValue) return;
            PointerId = eventData.pointerId;
            UpdateValue(eventData.position, eventData.pressEventCamera);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != PointerId) return;
            UpdateValue(eventData.position, eventData.pressEventCamera);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == PointerId) ResetInput();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerId == PointerId) ResetInput();
        }

        public void ResetInput()
        {
            PointerId = int.MinValue;
            Value = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
        }

        private void UpdateValue(Vector2 screenPoint, Camera eventCamera)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPoint, eventCamera, out local)) return;
            var normalized = Vector2.ClampMagnitude(local / Mathf.Max(1f, _radius), 1f);
            Value = normalized.magnitude < 0.14f ? Vector2.zero : normalized;
            if (_knob != null) _knob.anchoredPosition = normalized * _radius;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonKeeperController : MonoBehaviour
    {
        public bool ControlsLocked { get; private set; } = true;
        public Vector2 MobileMove;
        public event Action InteractRequested;
        public event Action<bool> LensChanged;

        private CharacterController _controller;
        private Transform _visual;
        private Transform _cameraPivot;
        private Camera _camera;
        private float _yaw;
        private float _pitch = 19f;
        private float _verticalVelocity;
        private bool _lens;
        private int _cameraTouch = int.MinValue;
        private Vector3 _cameraVelocity;

        public void Build(Camera camera)
        {
            _controller = GetComponent<CharacterController>();
            _controller.height = 1.85f;
            _controller.radius = 0.42f;
            _controller.center = new Vector3(0f, 0.92f, 0f);
            _camera = camera;
            BuildKeeperVisual();
            _cameraPivot = new GameObject("KeeperCameraPivot").transform;
            _cameraPivot.SetParent(transform, false);
            _cameraPivot.localPosition = new Vector3(0f, 1.48f, 0f);
        }

        public void SetLocked(bool locked)
        {
            ControlsLocked = locked;
            if (locked) ResetTransientInput();
        }

        public void SetLens(bool active)
        {
            if (ControlsLocked) active = false;
            if (_lens == active) return;
            _lens = active;
            LensChanged?.Invoke(active);
        }

        public void ResetTransientInput()
        {
            MobileMove = Vector2.zero;
            _cameraTouch = int.MinValue;
            SetLens(false);
        }

        private void Update()
        {
            if (_camera == null || _controller == null) return;
            ReadCameraInput();
            if (!ControlsLocked && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
                InteractRequested?.Invoke();
            if (!Application.isMobilePlatform) SetLens(!ControlsLocked && Input.GetKey(KeyCode.Space));
            MoveKeeper();
        }

        private void LateUpdate()
        {
            if (_camera == null || _cameraPivot == null) return;
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var desired = _cameraPivot.position + rotation * new Vector3(0f, 0.5f, -7.2f);
            var direction = desired - _cameraPivot.position;
            var distance = direction.magnitude;
            RaycastHit hit;
            if (distance > 0.01f && Physics.SphereCast(_cameraPivot.position, 0.28f, direction.normalized, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
                desired = _cameraPivot.position + direction.normalized * Mathf.Max(1.1f, hit.distance - 0.25f);
            _camera.transform.position = Vector3.SmoothDamp(_camera.transform.position, desired, ref _cameraVelocity, 0.06f);
            _camera.transform.LookAt(_cameraPivot.position + Vector3.up * 0.25f);
        }

        private void ReadCameraInput()
        {
            if (ControlsLocked)
            {
                _cameraTouch = int.MinValue;
                return;
            }

            if (Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxisRaw("Mouse X") * 2.6f;
                _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * 1.8f, 10f, 42f);
            }

            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                var cameraZone = touch.position.x > Screen.width * 0.42f && touch.position.y > Screen.height * 0.28f;
                if (touch.phase == TouchPhase.Began && cameraZone && _cameraTouch == int.MinValue)
                    _cameraTouch = touch.fingerId;
                if (touch.fingerId != _cameraTouch) continue;
                if (touch.phase == TouchPhase.Moved)
                {
                    var delta = Vector2.ClampMagnitude(touch.deltaPosition, 44f);
                    _yaw += delta.x * 0.095f;
                    _pitch = Mathf.Clamp(_pitch - delta.y * 0.075f, 10f, 42f);
                }
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    _cameraTouch = int.MinValue;
            }
        }

        private void MoveKeeper()
        {
            var input = Vector2.zero;
            if (!ControlsLocked)
            {
                input = new Vector2(
                    (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f),
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f));
                if (MobileMove.sqrMagnitude > 0.02f) input = MobileMove;
            }
            input = Vector2.ClampMagnitude(input, 1f);

            var cameraForward = _camera.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();
            var cameraRight = _camera.transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();
            var direction = cameraForward * input.y + cameraRight * input.x;
            if (direction.sqrMagnitude > 0.001f && _visual != null)
                _visual.rotation = Quaternion.Slerp(_visual.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12f);

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -1f;
            else _verticalVelocity -= 22f * Time.deltaTime;
            var motion = direction * 5.8f;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);
        }

        private void BuildKeeperVisual()
        {
            _visual = new GameObject("KeeperVisual").transform;
            _visual.SetParent(transform, false);
            Part(_visual, "CivicFieldCoat", PrimitiveType.Capsule, new Vector3(0f, 1.0f, 0f), new Vector3(0.82f, 1.42f, 0.64f), new Color(0.30f, 0.10f, 0.15f));
            Part(_visual, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.92f, 0f), Vector3.one * 0.55f, new Color(0.39f, 0.27f, 0.20f));
            Part(_visual, "ShoulderLens", PrimitiveType.Sphere, new Vector3(0.42f, 1.55f, -0.28f), Vector3.one * 0.22f, new Color(0.24f, 0.88f, 0.80f));
            Part(_visual, "ThreadSpool", PrimitiveType.Cylinder, new Vector3(-0.50f, 0.65f, 0.18f), new Vector3(0.34f, 0.18f, 0.34f), new Color(0.76f, 0.55f, 0.28f));
            for (var i = 0; i < 5; i++)
                Part(_visual, "RulingPlate_" + i, PrimitiveType.Cube, new Vector3(-0.32f + i * 0.16f, 0.35f, -0.38f), new Vector3(0.12f, 0.25f, 0.05f), new Color(0.84f, 0.79f, 0.68f));
        }

        private static GameObject Part(Transform parent, string objectName, PrimitiveType type, Vector3 localPosition, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            part.GetComponent<Renderer>().material = ActorMaterial(color);
            return part;
        }

        internal static Material ActorMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader) { color = color };
        }
    }

    [DisallowMultipleComponent]
    public sealed class CreatureAssembler : MonoBehaviour
    {
        public IdeaProfile Profile { get; private set; }
        public float EvidenceLevel { get; private set; }
        public float RiskLevel { get; private set; }
        public float GuardrailLevel { get; private set; }

        private Transform _body;
        private Transform _hiddenBurden;
        private Transform _follow;
        private float _time;
        private float _targetScale = 1f;
        private readonly List<Transform> _orbiters = new List<Transform>();

        public void Configure(IdeaProfile profile)
        {
            Profile = profile;
            if (_body != null) Destroy(_body.gameObject);
            _orbiters.Clear();
            _body = new GameObject("SpecimenBody").transform;
            _body.SetParent(transform, false);
            BuildBody(profile);
            SetStage((float)profile.Metrics.Evidence, 1f - (float)profile.Metrics.Safety, profile.Guardrails.Count / 6f);
        }

        public void SetFollowTarget(Transform target) { _follow = target; }

        public void SetRevealed(bool visible)
        {
            if (_hiddenBurden != null) _hiddenBurden.gameObject.SetActive(visible);
        }

        public void SetStage(float evidence, float risk, float guardrails)
        {
            EvidenceLevel = Mathf.Clamp01(evidence);
            RiskLevel = Mathf.Clamp01(risk);
            GuardrailLevel = Mathf.Clamp01(guardrails);
            _targetScale = 0.78f + EvidenceLevel * 0.5f;
        }

        public void Molt(IdeaProfile profile)
        {
            var evidence = EvidenceLevel;
            Configure(profile);
            SetStage(evidence, Mathf.Max(0f, 1f - (float)profile.Metrics.Safety), profile.Guardrails.Count / 6f);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_body == null) return;
            _body.localPosition = Vector3.up * (0.08f + Mathf.Sin(_time * 2.1f) * 0.08f);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * _targetScale, Time.deltaTime * 3.6f);

            for (var i = 0; i < _orbiters.Count; i++)
            {
                var angle = _time * (0.65f + RiskLevel * 0.7f) + i * Mathf.PI * 2f / Mathf.Max(1, _orbiters.Count);
                _orbiters[i].localPosition = new Vector3(Mathf.Cos(angle) * (1.1f + EvidenceLevel * 0.25f), 1.15f + Mathf.Sin(angle * 2f) * 0.40f, Mathf.Sin(angle) * (1.1f + EvidenceLevel * 0.25f));
            }

            if (_follow != null)
            {
                var desired = _follow.position - _follow.forward * 1.6f + _follow.right * 1.25f;
                desired.y = Mathf.Max(0.15f, desired.y);
                transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * 3f);
            }
        }

        private void BuildBody(IdeaProfile profile)
        {
            var color = ClassColor(profile.Class);
            switch (profile.Class)
            {
                case IdeaClass.Hand:
                    BodyPart("WorkingCore", PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(1.0f, 1.55f, 0.9f), color);
                    BodyPart("LeftCarrier", PrimitiveType.Capsule, new Vector3(-0.78f, 0.95f, 0f), new Vector3(0.28f, 1.15f, 0.28f), Dark(color));
                    BodyPart("RightCarrier", PrimitiveType.Capsule, new Vector3(0.78f, 0.95f, 0f), new Vector3(0.28f, 1.15f, 0.28f), Dark(color));
                    BodyPart("Harness", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0.48f), new Vector3(1.65f, 0.18f, 0.65f), Brass());
                    break;
                case IdeaClass.Mirror:
                    BodyPart("ReflectiveCore", PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f), new Vector3(1.15f, 1.75f, 0.62f), color);
                    for (var i = 0; i < 6; i++)
                    {
                        var angle = Mathf.PI * 2f * i / 6f;
                        BodyPart("MirrorShard_" + i, PrimitiveType.Cube, new Vector3(Mathf.Cos(angle), 1.25f + Mathf.Sin(angle * 2f) * 0.25f, Mathf.Sin(angle)), new Vector3(0.24f, 0.78f, 0.08f), Light(color));
                    }
                    break;
                case IdeaClass.Teeth:
                    BodyPart("PredatorCore", PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(1.15f, 1.75f, 1.15f), color);
                    for (var i = 0; i < 8; i++)
                    {
                        var angle = Mathf.PI * 2f * i / 8f;
                        BodyPart("Tooth_" + i, PrimitiveType.Capsule, new Vector3(Mathf.Cos(angle) * 0.82f, 0.42f, Mathf.Sin(angle) * 0.82f), new Vector3(0.16f, 0.42f, 0.16f), new Color(0.88f, 0.82f, 0.68f));
                    }
                    break;
                case IdeaClass.Swarm:
                    BodyPart("SwarmHeart", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.one * 0.82f, color);
                    BuildOrbiters(color, 12);
                    break;
                case IdeaClass.Weather:
                    BodyPart("WeatherCore", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), new Vector3(1.8f, 1.1f, 1.8f), color);
                    BuildOrbiters(color, 8);
                    break;
                case IdeaClass.Burrower:
                    BodyPart("BurrowerCore", PrimitiveType.Capsule, new Vector3(0f, 0.75f, 0f), new Vector3(1.45f, 1.0f, 0.82f), color);
                    BodyPart("LeftArchiveClaw", PrimitiveType.Cube, new Vector3(-0.62f, 0.30f, 0.62f), new Vector3(0.52f, 0.18f, 0.82f), Dark(color));
                    BodyPart("RightArchiveClaw", PrimitiveType.Cube, new Vector3(0.62f, 0.30f, 0.62f), new Vector3(0.52f, 0.18f, 0.82f), Dark(color));
                    break;
                default:
                    BodyPart("FleckCore", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.one * 0.82f, color);
                    BodyPart("LeftPaperWing", PrimitiveType.Cube, new Vector3(-0.62f, 1.15f, 0f), new Vector3(0.72f, 0.08f, 0.52f), Light(color));
                    BodyPart("RightPaperWing", PrimitiveType.Cube, new Vector3(0.62f, 1.15f, 0f), new Vector3(0.72f, 0.08f, 0.52f), Light(color));
                    break;
            }

            BodyPart("LeftEye", PrimitiveType.Sphere, new Vector3(-0.20f, 1.53f, 0.58f), Vector3.one * 0.14f, new Color(0.02f, 0.04f, 0.05f));
            BodyPart("RightEye", PrimitiveType.Sphere, new Vector3(0.20f, 1.53f, 0.58f), Vector3.one * 0.14f, new Color(0.02f, 0.04f, 0.05f));
            BodyPart("AppetiteMark_" + profile.Appetite, PrimitiveType.Sphere, new Vector3(0f, 0.34f, 0.62f), Vector3.one * 0.28f, Light(color));
            BuildHiddenBurden(profile.HiddenBurden);
        }

        private void BuildOrbiters(Color color, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var mote = BodyPart("Orbiter_" + i, PrimitiveType.Sphere, Vector3.zero, Vector3.one * (0.14f + i % 3 * 0.025f), Light(color));
                _orbiters.Add(mote.transform);
            }
        }

        private void BuildHiddenBurden(string burden)
        {
            _hiddenBurden = new GameObject("HiddenBurden_" + Sanitize(burden)).transform;
            _hiddenBurden.SetParent(_body, false);
            _hiddenBurden.localPosition = new Vector3(0f, 0f, -0.72f);
            for (var i = 0; i < 4; i++)
                Part(_hiddenBurden, "BurdenWeight_" + i, PrimitiveType.Cube, new Vector3(-0.48f + i * 0.32f, 0.15f + i % 2 * 0.24f, 0f), Vector3.one * 0.34f, new Color(0.62f, 0.22f, 0.20f));
            _hiddenBurden.gameObject.SetActive(false);
        }

        private GameObject BodyPart(string objectName, PrimitiveType type, Vector3 localPosition, Vector3 scale, Color color)
        {
            return Part(_body, objectName, type, localPosition, scale, color);
        }

        private static GameObject Part(Transform parent, string objectName, PrimitiveType type, Vector3 localPosition, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            part.GetComponent<Renderer>().material = ThirdPersonKeeperController.ActorMaterial(color);
            return part;
        }

        private static Color ClassColor(IdeaClass value)
        {
            switch (value)
            {
                case IdeaClass.Hand: return new Color(0.32f, 0.76f, 0.68f);
                case IdeaClass.Mirror: return new Color(0.48f, 0.67f, 0.82f);
                case IdeaClass.Teeth: return new Color(0.76f, 0.32f, 0.28f);
                case IdeaClass.Swarm: return new Color(0.82f, 0.58f, 0.27f);
                case IdeaClass.Weather: return new Color(0.54f, 0.48f, 0.78f);
                case IdeaClass.Burrower: return new Color(0.50f, 0.61f, 0.37f);
                default: return new Color(0.90f, 0.70f, 0.32f);
            }
        }

        private static Color Brass() { return new Color(0.79f, 0.57f, 0.27f); }
        private static Color Light(Color color) { return Color.Lerp(color, Color.white, 0.24f); }
        private static Color Dark(Color color) { return Color.Lerp(color, Color.black, 0.18f); }
        private static string Sanitize(string value) { return string.IsNullOrWhiteSpace(value) ? "Unnamed" : value.Replace(" ", "_").Replace("/", "_"); }
    }
}
