using UnityEngine;

namespace IdeaZoo.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ProceduralSpecialist : MonoBehaviour
    {
        public string SpecialistName;
        public string Role;
        public SpecialistTool Tool;
        public Transform Keeper;
        public Transform Creature;
        public float Phase;

        private Transform _body;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _tool;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private float _gestureUntil;
        private float _nextGesture;

        public void Build(string specialistName, string role, SpecialistTool tool, Color coat, float height, float width)
        {
            SpecialistName = specialistName;
            Role = role;
            Tool = tool;
            _startPosition = transform.localPosition;
            _startRotation = transform.localRotation;

            _body = new GameObject("Rig").transform;
            _body.SetParent(transform, false);
            Part(_body, "Coat", PrimitiveType.Capsule, new Vector3(0f, height * 0.48f, 0f), new Vector3(width, height * 0.55f, width * 0.72f), coat);
            Part(_body, "Apron", PrimitiveType.Cube, new Vector3(0f, height * 0.46f, -width * 0.43f), new Vector3(width * 0.72f, height * 0.56f, 0.06f), new Color(0.76f, 0.70f, 0.58f));
            _head = Part(_body, "Head", PrimitiveType.Sphere, new Vector3(0f, height * 1.08f, 0f), Vector3.one * width * 0.58f, new Color(0.38f, 0.25f, 0.19f)).transform;
            Part(_head, "EyeMark", PrimitiveType.Sphere, new Vector3(width * 0.18f, width * 0.06f, -width * 0.48f), Vector3.one * width * 0.10f, new Color(0.22f, 0.84f, 0.76f));

            _leftArm = Limb(_body, "LeftArm", new Vector3(-width * 0.70f, height * 0.78f, 0f), new Vector3(width * 0.22f, height * 0.36f, width * 0.22f), coat);
            _rightArm = Limb(_body, "RightArm", new Vector3(width * 0.70f, height * 0.78f, 0f), new Vector3(width * 0.22f, height * 0.36f, width * 0.22f), coat);
            Limb(_body, "LeftLeg", new Vector3(-width * 0.28f, height * 0.14f, 0f), new Vector3(width * 0.26f, height * 0.34f, width * 0.28f), new Color(0.10f, 0.13f, 0.14f));
            Limb(_body, "RightLeg", new Vector3(width * 0.28f, height * 0.14f, 0f), new Vector3(width * 0.26f, height * 0.34f, width * 0.28f), new Color(0.10f, 0.13f, 0.14f));

            BuildTool(width, height);
            BuildSilhouetteDetails(width, height, coat);
            _nextGesture = Time.time + 1.5f + Phase;
        }

        public void SetTargets(Transform keeper, Transform creature)
        {
            Keeper = keeper;
            Creature = creature;
        }

        public void Gesture(float duration = 1.4f)
        {
            _gestureUntil = Time.time + duration;
            _nextGesture = _gestureUntil + 2.0f + Phase;
        }

        private void Update()
        {
            if (_body == null) return;
            var wave = Mathf.Sin(Time.time * 0.75f + Phase);
            _body.localPosition = Vector3.up * (wave * 0.025f);
            _body.localRotation = Quaternion.Euler(0f, wave * 1.3f, wave * 0.5f);

            var target = Creature != null && Creature.gameObject.activeInHierarchy ? Creature : Keeper;
            if (target != null && _head != null)
            {
                var direction = target.position - _head.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.2f)
                {
                    var local = transform.InverseTransformDirection(direction.normalized);
                    var yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -38f, 38f);
                    _head.localRotation = Quaternion.Slerp(_head.localRotation, Quaternion.Euler(0f, yaw, 0f), Time.deltaTime * 2.2f);
                }
            }

            if (Time.time > _nextGesture) Gesture(1.2f + Mathf.Abs(Mathf.Sin(Phase)) * 0.8f);
            var gesturing = Time.time < _gestureUntil;
            var gestureWave = gesturing ? Mathf.Sin((1f - (_gestureUntil - Time.time)) * Mathf.PI * 2f) : 0f;
            if (_leftArm != null) _leftArm.localRotation = Quaternion.Euler(gesturing ? -28f + gestureWave * 12f : -4f + wave * 2f, 0f, 12f);
            if (_rightArm != null) _rightArm.localRotation = Quaternion.Euler(gesturing ? -52f + gestureWave * 18f : 4f - wave * 2f, 0f, -14f);
            AnimateTool(wave, gesturing ? gestureWave : 0f);
        }

        private void BuildTool(float width, float height)
        {
            _tool = new GameObject(Tool.ToString()).transform;
            _tool.SetParent(_rightArm, false);
            _tool.localPosition = new Vector3(0f, -height * 0.26f, -width * 0.25f);

            if (Tool == SpecialistTool.HatchFork)
            {
                CivicKit.Cylinder(_tool, "Stem", Vector3.zero, new Vector3(0.06f, 0.48f, 0.06f), CivicSurface.Brass);
                CivicKit.Beam(_tool, "Fork", new Vector3(-0.18f, -0.48f, 0f), new Vector3(0.18f, -0.48f, 0f), 0.06f, CivicSurface.TealGlow);
            }
            else if (Tool == SpecialistTool.ReleaseStaff)
            {
                CivicKit.Cylinder(_tool, "ShepherdStaff", new Vector3(0f, -0.22f, 0f), new Vector3(0.07f, 0.86f, 0.07f), CivicSurface.Brass);
                CivicKit.Sphere(_tool, "RecallLight", new Vector3(0f, -1.10f, 0f), Vector3.one * 0.22f, CivicSurface.TealGlow);
            }
            else if (Tool == SpecialistTool.AppetiteLens)
            {
                CivicKit.Cylinder(_tool, "Handle", Vector3.zero, new Vector3(0.06f, 0.32f, 0.06f), CivicSurface.Brass);
                CivicKit.Cylinder(_tool, "LensRing", new Vector3(0f, -0.38f, 0f), new Vector3(0.30f, 0.05f, 0.30f), CivicSurface.Brass).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                CivicKit.Sphere(_tool, "LensGlass", new Vector3(0f, -0.38f, 0f), Vector3.one * 0.24f, CivicSurface.Glass);
            }
            else if (Tool == SpecialistTool.MoltSpool)
            {
                CivicKit.Cylinder(_tool, "Spool", Vector3.zero, new Vector3(0.35f, 0.18f, 0.35f), CivicSurface.Brass).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                CivicKit.Beam(_tool, "Thread", Vector3.zero, new Vector3(0.35f, -0.55f, 0.15f), 0.035f, CivicSurface.TealGlow);
            }
            else if (Tool == SpecialistTool.CounterfactualFrames)
            {
                for (var i = 0; i < 3; i++)
                {
                    var frame = CivicKit.Box(_tool, "FutureFrame_" + i, new Vector3((i - 1) * 0.22f, -0.30f - i * 0.09f, i * 0.08f), new Vector3(0.18f, 0.28f, 0.03f), i == 1 ? CivicSurface.TealGlow : CivicSurface.Paper);
                    frame.transform.localRotation = Quaternion.Euler(0f, i * 12f - 12f, i * 7f - 7f);
                }
            }
            else
            {
                CivicKit.Cylinder(_tool, "BellHandle", Vector3.zero, new Vector3(0.05f, 0.28f, 0.05f), CivicSurface.Brass);
                CivicKit.Sphere(_tool, "MercyBell", new Vector3(0f, -0.36f, 0f), new Vector3(0.26f, 0.22f, 0.26f), CivicSurface.Rust);
            }
        }

        private void BuildSilhouetteDetails(float width, float height, Color coat)
        {
            if (Tool == SpecialistTool.HatchFork)
            {
                CivicKit.Banner(_body, "RookCape", new Vector3(0f, height * 0.70f, width * 0.42f), new Vector2(width * 1.5f, height * 0.75f), CivicSurface.Paper, 0.12f);
            }
            else if (Tool == SpecialistTool.ReleaseStaff)
            {
                CivicKit.Box(_body, "RecallSash", new Vector3(-width * 0.15f, height * 0.62f, -width * 0.43f), new Vector3(width * 0.18f, height * 0.95f, 0.04f), CivicSurface.TealGlow);
            }
            else if (Tool == SpecialistTool.AppetiteLens)
            {
                for (var i = 0; i < 4; i++) CivicKit.Sphere(_body, "AppetiteVial_" + i, new Vector3(-width * 0.55f + i * width * 0.35f, height * 0.50f, -width * 0.48f), Vector3.one * width * 0.13f, i % 2 == 0 ? CivicSurface.Rust : CivicSurface.TealGlow);
            }
            else if (Tool == SpecialistTool.MoltSpool)
            {
                CivicKit.Rail(_body, "SurgicalFrame", new Vector3(-width, height * 0.24f, width * 0.32f), new Vector3(width, height * 0.24f, width * 0.32f), 3);
            }
            else if (Tool == SpecialistTool.CounterfactualFrames)
            {
                CivicKit.Banner(_body, "ForecastCoat", new Vector3(0f, height * 0.60f, width * 0.43f), new Vector2(width * 1.7f, height * 0.95f), CivicSurface.Ink, 0.10f);
            }
            else CivicKit.Box(_body, "WhiteRoomPlate", new Vector3(0f, height * 0.48f, -width * 0.48f), new Vector3(width * 0.70f, height * 0.55f, 0.05f), CivicSurface.Paper);
        }

        private void AnimateTool(float idle, float gesture)
        {
            if (_tool == null) return;
            _tool.localRotation = Quaternion.Euler(gesture * 22f, idle * 4f, gesture * -16f);
            if (Tool == SpecialistTool.MercyBell && Mathf.Abs(gesture) > 0.75f)
                _tool.localRotation *= Quaternion.Euler(0f, 0f, gesture * 18f);
        }

        private static Transform Limb(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Part(root, "Segment", PrimitiveType.Capsule, new Vector3(0f, -scale.y * 0.52f, 0f), scale, color);
            return root;
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var node = GameObject.CreatePrimitive(type);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            node.transform.localScale = scale;
            var collider = node.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Object.Destroy(collider);
                else Object.DestroyImmediate(collider);
            }
            var renderer = node.GetComponent<Renderer>();
            renderer.sharedMaterial = MaterialFor(color);
            return node;
        }

        private static Material MaterialFor(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }
    }
}
