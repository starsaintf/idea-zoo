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
            if (_lastSafe != Screen.safeArea || _lastScreen.x != Screen.width || _lastScreen.y != Screen.height) Apply();
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
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value { get; private set; }
        public int PointerId { get; private set; } = int.MinValue;

        private RectTransform _rect;
        private RectTransform _knob;
        private float _radius;

        public void Build(Color background, Color knob)
        {
            _rect = transform as RectTransform;
            if (_rect == null) throw new InvalidOperationException("MobileJoystick requires a RectTransform.");
            _rect.sizeDelta = new Vector2(144f, 144f);
            var image = gameObject.AddComponent<Image>();
            image.color = background;
            image.raycastTarget = true;

            var knobObject = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            knobObject.transform.SetParent(transform, false);
            _knob = knobObject.GetComponent<RectTransform>();
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
            if (eventData.pointerId != PointerId) return;
            ResetInput();
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
            var normalized = Vector2.ClampMagnitude(local / _radius, 1f);
            Value = normalized.magnitude < 0.14f ? Vector2.zero : normalized;
            _knob.anchoredPosition = normalized * _radius;
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
            if (locked)
            {
                MobileMove = Vector2.zero;
                _cameraTouch = int.MinValue;
                SetLens(false);
            }
        }

        public void SetLens(bool active)
        {
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
            if (!ControlsLocked && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))) InteractRequested?.Invoke();
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
            if (Physics.SphereCast(_cameraPivot.position, 0.28f, direction.normalized, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
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
                if (touch.phase == TouchPhase.Began && cameraZone && _cameraTouch == int.MinValue) _cameraTouch = touch.fingerId;
                if (touch.fingerId != _cameraTouch) continue;
                if (touch.phase == TouchPhase.Moved)
                {
                    var delta = Vector2.ClampMagnitude(touch.deltaPosition, 44f);
                    _yaw += delta.x * 0.095f;
                    _pitch = Mathf.Clamp(_pitch - delta.y * 0.075f, 10f, 42f);
                }
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) _cameraTouch = int.MinValue;
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

            if (_controller.isGrounded) _verticalVelocity = -1f;
            else _verticalVelocity -= 22f * Time.deltaTime;
            var motion = direction * 5.8f;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);
        }

        private void BuildKeeperVisual()
        {
            _visual = new GameObject("KeeperVisual").transform;
            _visual.SetParent(transform, false);
            var coat = Part(_visual, "CivicFieldCoat", PrimitiveType.Capsule, new Vector3(0f, 1.0f, 0f), new Vector3(0.82f, 1.42f, 0.64f), new Color(0.30f, 0.10f, 0.15f));
            coat.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
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
            var risk = 1f - (float)profile.Metrics.Safety;
            SetStage((float)profile.Metrics.Evidence, risk, profile.Guardrails.Count / 6f);
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
            _body.localPosition = Vector3.up * (0.10f + Mathf.Sin(_time * 2.1f) * 0.08f);
            _body.Rotate(Vector3.up, Time.deltaTime * (8f + RiskLevel * 18f), Space.Self);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * _targetScale, Time.deltaTime * 3f);

            for (var i = 0; i < _orbiters.Count; i++)
            {
                var a = _time * (0.55f + RiskLevel * 0.8f) + i * Mathf.PI * 2f / Mathf.Max(1, _orbiters.Count);
                var radius = 1.0f + EvidenceLevel * 0.45f;
                _orbiters[i].localPosition = new Vector3(Mathf.Cos(a) * radius, 1.2f + Mathf.Sin(a * 2f) * 0.45f, Mathf.Sin(a) * radius);
            }

            if (_follow != null)
            {
                var desired = _follow.position - _follow.forward * 1.7f + _follow.right * 1.4f;
                transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * 2.6f);
            }
        }

        private void BuildBody(IdeaProfile profile)
        {
            var color = ClassColor(profile.Class);
            if (profile.Class == IdeaClass.Hand) BuildHand(color);
            else if (profile.Class == IdeaClass.Mirror) BuildMirror(color);
            else if (profile.Class == IdeaClass.Teeth) BuildTeeth(color);
            else if (profile.Class == IdeaClass.Swarm) BuildSwarm(color);
            else if (profile.Class == IdeaClass.Weather) BuildWeather(color);
            else if (profile.Class == IdeaClass.Burrower) BuildBurrower(color);
            else BuildFleck(color);

            BuildFace(color, profile.Class);
            BuildAppetiteMark(profile.Appetite, color);
            BuildHiddenBurden(profile.HiddenBurden);
            Label(profile.CreatureName, new Vector3(0f, 3.25f, 0f), 34, new Color(0.90f, 0.84f, 0.72f));
        }

        private void BuildHand(Color color)
        {
            Part("Core", PrimitiveType.Capsule, new Vector3(0f, 1.25f, 0f), new Vector3(1.2f, 2.2f, 1.0f), color);
            for (var side = -1; side <= 1; side += 2)
            {
                var arm = Part("WorkingLimb", PrimitiveType.Capsule, new Vector3(side * 0.9f, 1.15f, 0f), new Vector3(0.28f, 1.55f, 0.28f), color * 0.8f);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, side * 20f);
            }
            Part("Harness", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0.48f), new Vector3(1.9f, 0.18f, 0.70f), new Color(0.76f, 0.55f, 0.28f));
        }

        private void BuildMirror(Color color)
        {
            Part("Core", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(1.2f, 2.0f, 0.72f), color);
            for (var i = 0; i < 8; i++)
            {
                var a = i * Mathf.PI * 2f / 8f;
                var shard = Part("ReflectiveShard", PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 1.0f, 1.3f + Mathf.Sin(a * 2f) * 0.2f, Mathf.Sin(a) * 1.0f), new Vector3(0.24f, 0.85f, 0.08f), color + new Color(0.18f, 0.18f, 0.18f));
                shard.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
        }

        private void BuildTeeth(Color color)
        {
            Part("Core", PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0f), new Vector3(1.15f, 1.75f, 1.15f), color);
            for (var i = 0; i < 10; i++)
            {
                var a = i * Mathf.PI * 2f / 10f;
                var tooth = Part("Tooth", PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 0.88f, 0.55f, Mathf.Sin(a) * 0.88f), new Vector3(0.12f, 0.48f, 0.12f), new Color(0.90f, 0.84f, 0.72f));
                tooth.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            }
        }

        private void BuildSwarm(Color color)
        {
            Part("SwarmHeart", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), Vector3.one * 0.72f, color);
            for (var i = 0; i < 13; i++)
            {
                var mote = Part("SwarmBody", PrimitiveType.Sphere, Vector3.zero, Vector3.one * (0.20f + (i % 3) * 0.04f), color);
                _orbiters.Add(mote.transform);
            }
        }

        private void BuildWeather(Color color)
        {
            for (var i = 0; i < 6; i++)
            {
                var cloud = Part("WeatherCloud", PrimitiveType.Sphere, new Vector3(-1.2f + i * 0.48f, 1.1f + (i % 3) * 0.36f, Mathf.Sin(i) * 0.5f), new Vector3(1.2f, 0.72f, 0.95f), color);
                cloud.transform.localRotation = Quaternion.Euler(i * 8f, i * 21f, 0f);
            }
            for (var i = 0; i < 5; i++)
                Part("PhraseRain", PrimitiveType.Cube, new Vector3(-0.9f + i * 0.45f, 0.25f, -0.20f + (i % 2) * 0.35f), new Vector3(0.06f, 0.75f, 0.06f), color + new Color(0.15f, 0.15f, 0.15f));
        }

        private void BuildBurrower(Color color)
        {
            var core = Part("Core", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(1.3f, 1.35f, 1.0f), color);
            core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (var side = -1; side <= 1; side += 2)
            {
                var claw = Part("ArchiveClaw", PrimitiveType.Cube, new Vector3(side * 0.62f, 0.45f, -0.52f), new Vector3(0.52f, 0.18f, 0.82f), color * 0.72f);
                claw.transform.localRotation = Quaternion.Euler(0f, side * 12f, 0f);
            }
        }

        private void BuildFleck(Color color)
        {
            Part("Core", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), Vector3.one * 0.72f, color);
            for (var side = -1; side <= 1; side += 2)
            {
                var wing = Part("PaperWing", PrimitiveType.Cube, new Vector3(side * 0.62f, 1.25f, 0f), new Vector3(0.78f, 0.06f, 0.58f), color + new Color(0.16f, 0.16f, 0.16f));
                wing.transform.localRotation = Quaternion.Euler(0f, 0f, side * 24f);
            }
        }

        private void BuildFace(Color color, IdeaClass ideaClass)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                Part("Eye", PrimitiveType.Sphere, new Vector3(side * 0.20f, 1.55f, -0.62f), Vector3.one * 0.13f, new Color(0.02f, 0.04f, 0.05f));
                if (ideaClass == IdeaClass.Mirror || Profile.Appetite == Appetite.Attention)
                    Part("AttentionHalo", PrimitiveType.Sphere, new Vector3(side * 0.20f, 1.55f, -0.66f), Vector3.one * 0.19f, color + new Color(0.20f, 0.20f, 0.20f));
            }
        }

        private void BuildAppetiteMark(Appetite appetite, Color color)
        {
            Label(appetite.ToString().ToUpperInvariant(), new Vector3(0f, 0.18f, -0.82f), 22, color + new Color(0.18f, 0.18f, 0.18f));
        }

        private void BuildHiddenBurden(string burden)
        {
            _hiddenBurden = new GameObject("HiddenBurden").transform;
            _hiddenBurden.SetParent(_body, false);
            _hiddenBurden.gameObject.SetActive(false);
            for (var i = 0; i < 4; i++)
                PartUnder(_hiddenBurden, "BurdenWeight", PrimitiveType.Cube, new Vector3(-0.55f + i * 0.36f, 0.10f + (i % 2) * 0.25f, 0.66f), Vector3.one * 0.36f, new Color(0.65f, 0.24f, 0.20f));
            LabelUnder(_hiddenBurden, burden.ToUpperInvariant(), new Vector3(0f, 0.96f, 0.64f), 18, new Color(0.95f, 0.48f, 0.40f));
        }

        private GameObject Part(string objectName, PrimitiveType type, Vector3 localPosition, Vector3 scale, Color color)
        {
            return PartUnder(_body, objectName, type, localPosition, scale, color);
        }

        private static GameObject PartUnder(Transform parent, string objectName, PrimitiveType type, Vector3 localPosition, Vector3 scale, Color color)
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

        private void Label(string text, Vector3 localPosition, int fontSize, Color color)
        {
            LabelUnder(_body, text, localPosition, fontSize, color);
        }

        private static void LabelUnder(Transform parent, string text, Vector3 localPosition, int fontSize, Color color)
        {
            var item = new GameObject("Label_" + text.Replace(' ', '_'));
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var mesh = item.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = fontSize;
            mesh.characterSize = 0.06f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
        }

        private static Color ClassColor(IdeaClass ideaClass)
        {
            if (ideaClass == IdeaClass.Fleck) return new Color(0.93f, 0.68f, 0.28f);
            if (ideaClass == IdeaClass.Hand) return new Color(0.29f, 0.72f, 0.64f);
            if (ideaClass == IdeaClass.Mirror) return new Color(0.45f, 0.65f, 0.82f);
            if (ideaClass == IdeaClass.Teeth) return new Color(0.72f, 0.28f, 0.23f);
            if (ideaClass == IdeaClass.Swarm) return new Color(0.80f, 0.56f, 0.27f);
            if (ideaClass == IdeaClass.Weather) return new Color(0.56f, 0.48f, 0.78f);
            return new Color(0.48f, 0.62f, 0.38f);
        }
    }
}
