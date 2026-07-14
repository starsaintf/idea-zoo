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
            Beam(root, "TopRail", start + Vector3.up * 0.78f, end + Vector3.up * 0.78f, 0.07f, CivicSurface.Brass);
            for (var i = 0; i <= posts; i++)
            {
                var p = Vector3.Lerp(start, end, i / (float)Mathf.Max(1, posts));
                Cylinder(root, "Post_" + i, p + Vector3.up * 0.39f, new Vector3(0.09f, 0.78f, 0.09f), CivicSurface.Brass);
            }
            return root;
        }

        public static Transform PipeRun(Transform parent, string name, Vector3[] points, float width, CivicSurface surface)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            for (var i = 0; i < points.Length - 1; i++) Beam(root, "Segment_" + i, points[i], points[i + 1], width, surface);
            for (var i = 1; i < points.Length - 1; i++) Sphere(root, "Joint_" + i, points[i], Vector3.one * width * 1.7f, surface);
            return root;
        }

        public static Transform Workbench(Transform parent, string name, Vector3 position, Vector3 size, CivicSurface surface)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Box(root, "Top", new Vector3(0f, size.y, 0f), new Vector3(size.x, 0.12f, size.z), surface);
            for (var x = -1; x <= 1; x += 2)
                for (var z = -1; z <= 1; z += 2)
                    Cylinder(root, "Leg_" + x + "_" + z, new Vector3(x * size.x * 0.38f, size.y * 0.5f, z * size.z * 0.36f), new Vector3(0.12f, size.y, 0.12f), CivicSurface.Brass);
            return root;
        }

        public static Transform Vitrine(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            Box(root, "Plinth", new Vector3(0f, 0.18f, 0f), new Vector3(size.x * 1.1f, 0.36f, size.z * 1.1f), CivicSurface.Brass);
            Box(root, "Glass", new Vector3(0f, size.y * 0.5f + 0.32f, 0f), size, CivicSurface.Glass);
            return root;
        }

        public static Transform Banner(Transform parent, string name, Vector3 position, Vector2 size, CivicSurface surface, float curl)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            for (var i = 0; i < 5; i++)
            {
                var y = size.y * (0.5f - (i + 0.5f) / 5f);
                var depth = Mathf.Sin(i / 4f * Mathf.PI) * curl;
                Box(root, "Fold_" + i, new Vector3(0f, y, depth), new Vector3(size.x, size.y / 5f + 0.015f, 0.05f), surface);
            }
            return root;
        }

        public static GameObject Beam(Transform parent, string name, Vector3 start, Vector3 end, float width, CivicSurface surface)
        {
            var midpoint = (start + end) * 0.5f;
            var direction = end - start;
            var beam = Cylinder(parent, name, midpoint, new Vector3(width, direction.magnitude * 0.5f, width), surface);
            if (direction.sqrMagnitude > 0.0001f) beam.transform.up = direction.normalized;
            return beam;
        }
    }
}
