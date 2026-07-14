using System;
using UnityEngine;

namespace IdeaZoo.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class IdeaZooKeeperController : MonoBehaviour
    {
        public event Action InteractRequested;
        public event Action<bool> LensChanged;

        public float MoveSpeed = 5.4f;
        public float Acceleration = 18f;
        public float Gravity = 22f;
        public float CameraDistance = 6.6f;
        public float CameraSensitivity = 0.12f;

        public bool ControlsLocked { get; private set; } = true;
        public bool LensActive { get; private set; }
        public Camera PlayerCamera { get; private set; }

        private CharacterController _controller;
        private Transform _visual;
        private Transform _cameraPivot;
        private Vector2 _mobileMove;
        private Vector2 _mobileLook;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;
        private float _yaw;
        private float _pitch = 18f;
        private bool _mobileLensHeld;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _controller.radius = 0.42f;
            _controller.height = 1.75f;
            _controller.center = new Vector3(0f, 0.88f, 0f);
            BuildVisual();
            BuildCamera();
        }

        private void Update()
        {
            if (ControlsLocked)
            {
                _planarVelocity = Vector3.MoveTowards(_planarVelocity, Vector3.zero, Acceleration * Time.deltaTime);
                ApplyGravityAndMove(Vector3.zero);
                return;
            }

            var keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var input = _mobileMove.sqrMagnitude > 0.0025f ? _mobileMove : Vector2.ClampMagnitude(keyboard, 1f);

            if (Input.GetMouseButton(1))
                AddLook(new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y")) * 2.4f);

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
                InteractRequested?.Invoke();

            SetLens(Input.GetKey(KeyCode.Space) || _mobileLensHeld);

            _yaw += _mobileLook.x * CameraSensitivity;
            _pitch = Mathf.Clamp(_pitch - _mobileLook.y * CameraSensitivity, -12f, 48f);
            _mobileLook = Vector2.zero;

            var cameraForward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            var cameraRight = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
            var desiredDirection = Vector3.ClampMagnitude(cameraForward * input.y + cameraRight * input.x, 1f);
            var target = desiredDirection * MoveSpeed;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, target, Acceleration * Time.deltaTime);

            if (desiredDirection.sqrMagnitude > 0.01f && _visual != null)
            {
                var targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
                _visual.rotation = Quaternion.Slerp(_visual.rotation, targetRotation, 12f * Time.deltaTime);
            }

            ApplyGravityAndMove(_planarVelocity);
            UpdateCamera();
        }

        public void SetControlsLocked(bool locked)
        {
            ControlsLocked = locked;
            if (!locked) return;
            _mobileMove = Vector2.zero;
            _mobileLook = Vector2.zero;
            _mobileLensHeld = false;
            SetLens(false);
        }

        public void SetMobileMove(Vector2 input)
        {
            _mobileMove = ControlsLocked ? Vector2.zero : Vector2.ClampMagnitude(input, 1f);
        }

        public void AddLook(Vector2 delta)
        {
            if (ControlsLocked) return;
            var bounded = Vector2.ClampMagnitude(delta, 42f);
            _mobileLook += bounded;
        }

        public void RequestInteract()
        {
            if (!ControlsLocked) InteractRequested?.Invoke();
        }

        public void SetMobileLens(bool active)
        {
            _mobileLensHeld = !ControlsLocked && active;
            SetLens(Input.GetKey(KeyCode.Space) || _mobileLensHeld);
        }

        public void SetLens(bool active)
        {
            if (ControlsLocked) active = false;
            if (LensActive == active) return;
            LensActive = active;
            LensChanged?.Invoke(active);
        }

        public void Teleport(Vector3 position)
        {
            var enabled = _controller.enabled;
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = enabled;
            _planarVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }

        private void ApplyGravityAndMove(Vector3 planar)
        {
            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -1.5f;
            else _verticalVelocity -= Gravity * Time.deltaTime;
            var motion = new Vector3(planar.x, _verticalVelocity, planar.z);
            _controller.Move(motion * Time.deltaTime);
        }

        private void BuildVisual()
        {
            var root = new GameObject("KeeperVisual").transform;
            root.SetParent(transform, false);
            _visual = root;

            var coat = Primitive("CivicFieldCoat", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 1.25f, 0.62f), new Color(0.08f, 0.33f, 0.35f));
            coat.transform.SetParent(root, false);
            var head = Primitive("KeeperHead", PrimitiveType.Sphere, new Vector3(0f, 1.75f, 0f), Vector3.one * 0.58f, new Color(0.40f, 0.27f, 0.21f));
            head.transform.SetParent(root, false);
            var lens = Primitive("ResonanceLens", PrimitiveType.Sphere, new Vector3(0.32f, 1.77f, 0.24f), Vector3.one * 0.20f, new Color(0.28f, 0.88f, 0.82f));
            lens.transform.SetParent(root, false);
            var spool = Primitive("ContainmentThread", PrimitiveType.Cylinder, new Vector3(-0.48f, 0.62f, 0.15f), new Vector3(0.30f, 0.18f, 0.30f), new Color(0.78f, 0.58f, 0.28f));
            spool.transform.SetParent(root, false);
            spool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void BuildCamera()
        {
            _cameraPivot = new GameObject("KeeperCameraPivot").transform;
            _cameraPivot.SetParent(transform, false);
            _cameraPivot.localPosition = new Vector3(0f, 1.45f, 0f);

            var cameraObject = new GameObject("IdeaZooCamera");
            cameraObject.transform.SetParent(_cameraPivot, false);
            PlayerCamera = cameraObject.AddComponent<Camera>();
            PlayerCamera.fieldOfView = 58f;
            PlayerCamera.nearClipPlane = 0.12f;
            PlayerCamera.farClipPlane = 180f;
            cameraObject.AddComponent<AudioListener>();
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            if (_cameraPivot == null || PlayerCamera == null) return;
            _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var desired = _cameraPivot.position - _cameraPivot.forward * CameraDistance;
            RaycastHit hit;
            if (Physics.SphereCast(_cameraPivot.position, 0.28f, -_cameraPivot.forward, out hit, CameraDistance, ~0, QueryTriggerInteraction.Ignore))
                desired = _cameraPivot.position - _cameraPivot.forward * Mathf.Max(1.1f, hit.distance - 0.32f);
            PlayerCamera.transform.position = Vector3.Lerp(PlayerCamera.transform.position, desired, 14f * Time.deltaTime);
            PlayerCamera.transform.rotation = Quaternion.LookRotation(_cameraPivot.position - PlayerCamera.transform.position, Vector3.up);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var node = GameObject.CreatePrimitive(type);
            node.name = name;
            node.transform.localPosition = localPosition;
            node.transform.localScale = localScale;
            var collider = node.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);
            var renderer = node.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            renderer.material.color = color;
            return node;
        }
    }
}
