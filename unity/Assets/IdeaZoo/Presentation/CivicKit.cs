using UnityEngine;

namespace IdeaZoo.Presentation
{
    public static class CivicKit
    {
        public static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, CivicSurface surface, bool collider = false)
        {
            return Primitive(parent, name, PrimitiveType.Cube, position, scale, surface, collider);
        }

        public static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, CivicSurface surface, bool collider = false)
        {
            return Primitive(parent, name, PrimitiveType.Cylinder, position, scale, surface, collider);
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 position, Vector3 scale, CivicSurface surface, bool collider = false)
        {
            return Primitive(parent, name, PrimitiveType.Sphere, position, scale, surface, collider);
        }

        public static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, CivicSurface surface, bool collider)
        {
            var node = GameObject.CreatePrimitive(type);
            node.name = name;
            node.transform.SetParent(parent, false);
            node.transform.localPosition = position;
            node.transform.localScale = scale;
            var renderer = node.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = CivicMaterialLibrary.Get(surface);
            var existing = node.GetComponent<Collider>();
            if (!collider && existing != null)
            {
                if (Application.isPlaying) Object.Destroy(existing);
                else Object.DestroyImmediate(existing);
            }
            return node;
        }

        public static Transform LayeredFacade(Transform parent, string name, Vector3 position, Vector2 size, int layers, CivicSurface surface)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            for (var i = 0; i < layers; i++)
            {
                var inset = i * 0.18f;
                var panel = Box(root, "Layer_" + i, new Vector3(0f, i * 0.08f, i * 0.09f),
                    new Vector3(Mathf.Max(0.4f, size.x - inset), Mathf.Max(0.4f, size.y - inset), 0.10f), surface);
                panel.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * (0.7f + i * 0.18f));
            }
            return root;
        }

        public static Transform Rail(Transform parent, string name, Vector3 start, Vector3 end, int posts)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            for (var i = 0; i <= posts; i++)
            {
                var t = posts == 0 ? 0f : i / (float)posts;
                var position = Vector3.Lerp(start, end, t);
                Cylinder(root, "Post_" + i, position + Vector3.up * 0.65f, new Vector3(0.06f, 0.65f, 0.06f), CivicSurface.Brass);
            }
            Beam(root, "TopRail", start + Vector3.up * 1.25f, end + Vector3.up * 1.25f, 0.09f, CivicSurface.Brass);
            Beam(root, "MidRail", start + Vector3.up * 0.63f, end + Vector3.up * 0.63f, 0.045f, CivicSurface.Brass);
            return root;
        }

        public static GameObject Beam(Transform parent, string name, Vector3 start, Vector3 end, float width, CivicSurface surface)
        {
            var direction = end - start;
            var beam = Box(parent, name, (start + end) * 0.5f, new Vector3(width, width, direction.magnitude), surface);
            if (direction.sqrMagnitude > 0.0001f) beam.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            return beam;
        }

        public static Transform PipeRun(Transform parent, string name, Vector3[] points, float radius, CivicSurface surface)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            for (var i = 0; i < points.Length - 1; i++)
            {
                var start = points[i];
                var end = points[i + 1];
                var direction = end - start;
                var segment = Cylinder(root, "Pipe_" + i, (start + end) * 0.5f,
                    new Vector3(radius, direction.magnitude * 0.5f, radius), surface);
                if (direction.sqrMagnitude > 0.0001f) segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                Sphere(root, "Joint_" + i, start, Vector3.one * radius * 2.3f, surface);
            }
            if (points.Length > 0) Sphere(root, "Joint_End", points[points.Length - 1], Vector3.one * radius * 2.3f, surface);
            return root;
        }

        public static Transform Workbench(Transform parent, string name, Vector3 position, Vector3 scale, CivicSurface surface)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Box(root, "Top", new Vector3(0f, 0.85f, 0f), new Vector3(scale.x, 0.16f, scale.z), surface, true);
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sz = -1; sz <= 1; sz += 2)
                    Box(root, "Leg_" + sx + "_" + sz,
                        new Vector3(sx * scale.x * 0.38f, 0.42f, sz * scale.z * 0.35f),
                        new Vector3(0.13f, 0.84f, 0.13f), CivicSurface.Brass, true);
            return root;
        }

        public static Transform Vitrine(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Box(root, "Plinth", new Vector3(0f, size.y * 0.12f, 0f), new Vector3(size.x * 1.08f, size.y * 0.24f, size.z * 1.08f), CivicSurface.Brass, true);
            Box(root, "Glass", new Vector3(0f, size.y * 0.65f, 0f), size, CivicSurface.Glass);
            Sphere(root, "SpecimenGlow", new Vector3(0f, size.y * 0.66f, 0f), Vector3.one * Mathf.Min(size.x, size.z) * 0.30f, CivicSurface.TealGlow);
            return root;
        }

        public static Transform Banner(Transform parent, string name, Vector3 position, Vector2 size, CivicSurface surface, float curl)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            const int columns = 8;
            var vertices = new Vector3[(columns + 1) * 2];
            var triangles = new int[columns * 6];
            var uvs = new Vector2[vertices.Length];
            for (var i = 0; i <= columns; i++)
            {
                var t = i / (float)columns;
                var x = Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, t);
                var z = Mathf.Sin(t * Mathf.PI) * curl;
                vertices[i * 2] = new Vector3(x, size.y * 0.5f, z);
                vertices[i * 2 + 1] = new Vector3(x, -size.y * 0.5f, z + Mathf.Sin(t * Mathf.PI * 2f) * curl * 0.22f);
                uvs[i * 2] = new Vector2(t, 1f);
                uvs[i * 2 + 1] = new Vector2(t, 0f);
                if (i == columns) continue;
                var index = i * 6;
                var v = i * 2;
                triangles[index] = v;
                triangles[index + 1] = v + 2;
                triangles[index + 2] = v + 1;
                triangles[index + 3] = v + 2;
                triangles[index + 4] = v + 3;
                triangles[index + 5] = v + 1;
            }
            var mesh = new Mesh { name = name + "_Mesh", vertices = vertices, triangles = triangles, uv = uvs };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = CivicMaterialLibrary.Get(surface);
            return root;
        }
    }

    public sealed class CivicAmbientMotion : MonoBehaviour
    {
        public Vector3 Axis = Vector3.up;
        public float Degrees = 5f;
        public float Speed = 0.5f;
        public float Bob = 0.08f;
        public float Phase;
        private Quaternion _startRotation;
        private Vector3 _startPosition;

        private void Awake()
        {
            _startRotation = transform.localRotation;
            _startPosition = transform.localPosition;
        }

        private void Update()
        {
            var wave = Mathf.Sin(Time.time * Speed + Phase);
            transform.localRotation = _startRotation * Quaternion.AngleAxis(wave * Degrees, Axis.normalized);
            transform.localPosition = _startPosition + Vector3.up * (wave * Bob);
        }
    }
}
