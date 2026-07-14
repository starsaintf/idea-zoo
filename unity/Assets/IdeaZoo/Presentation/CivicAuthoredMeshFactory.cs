using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    public static class CivicAuthoredMeshFactory
    {
        public static Mesh FoldedPanel(float width, float height, float depth, int folds)
        {
            folds = Mathf.Max(2, folds);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var columns = folds + 1;

            for (var side = 0; side < 2; side++)
            {
                var sign = side == 0 ? 1f : -1f;
                for (var y = 0; y < 2; y++)
                {
                    for (var x = 0; x < columns; x++)
                    {
                        var t = x / (float)folds;
                        var px = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
                        var crease = (x % 2 == 0 ? -1f : 1f) * depth * 0.22f;
                        vertices.Add(new Vector3(px, y == 0 ? 0f : height, sign * depth * 0.5f + crease));
                        uvs.Add(new Vector2(t, y));
                    }
                }

                var offset = side * columns * 2;
                for (var x = 0; x < folds; x++)
                {
                    var a = offset + x;
                    var b = offset + x + 1;
                    var c = offset + columns + x;
                    var d = offset + columns + x + 1;
                    if (side == 0)
                    {
                        AddQuad(triangles, a, c, d, b);
                    }
                    else
                    {
                        AddQuad(triangles, a, b, d, c);
                    }
                }
            }

            for (var x = 0; x < folds; x++)
            {
                var frontBottomA = x;
                var frontBottomB = x + 1;
                var backBottomA = columns * 2 + x;
                var backBottomB = columns * 2 + x + 1;
                AddQuad(triangles, frontBottomA, frontBottomB, backBottomB, backBottomA);

                var frontTopA = columns + x;
                var frontTopB = columns + x + 1;
                var backTopA = columns * 3 + x;
                var backTopB = columns * 3 + x + 1;
                AddQuad(triangles, frontTopA, backTopA, backTopB, frontTopB);
            }

            AddQuad(triangles, 0, columns * 2, columns * 3, columns);
            AddQuad(triangles, columns - 1, columns * 2 - 1, columns * 4 - 1, columns * 3 - 1);
            return Finish("Civic_FoldedPanel", vertices, triangles, uvs);
        }

        public static Mesh ArchRing(float innerRadius, float outerRadius, float depth, int segments)
        {
            segments = Mathf.Max(8, segments);
            innerRadius = Mathf.Max(0.1f, innerRadius);
            outerRadius = Mathf.Max(innerRadius + 0.1f, outerRadius);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            for (var side = 0; side < 2; side++)
            {
                var z = side == 0 ? -depth * 0.5f : depth * 0.5f;
                for (var i = 0; i <= segments; i++)
                {
                    var angle = Mathf.Lerp(0f, Mathf.PI, i / (float)segments);
                    var sin = Mathf.Sin(angle);
                    var cos = Mathf.Cos(angle);
                    vertices.Add(new Vector3(cos * outerRadius, sin * outerRadius, z));
                    vertices.Add(new Vector3(cos * innerRadius, sin * innerRadius, z));
                    uvs.Add(new Vector2(i / (float)segments, 1f));
                    uvs.Add(new Vector2(i / (float)segments, 0f));
                }
            }

            var ringCount = (segments + 1) * 2;
            for (var i = 0; i < segments; i++)
            {
                var a = i * 2;
                var b = a + 1;
                var c = a + 2;
                var d = a + 3;
                AddQuad(triangles, a, c, d, b);

                var back = ringCount;
                AddQuad(triangles, back + a, back + b, back + d, back + c);

                AddQuad(triangles, a, back + a, back + c, c);
                AddQuad(triangles, b, d, back + d, back + b);
            }

            AddQuad(triangles, 0, 1, ringCount + 1, ringCount);
            var last = segments * 2;
            AddQuad(triangles, last, ringCount + last, ringCount + last + 1, last + 1);
            return Finish("Civic_ArchRing", vertices, triangles, uvs);
        }

        public static Mesh TubeArc(float radius, float tubeRadius, float arcDegrees, int arcSegments, int ringSegments)
        {
            arcSegments = Mathf.Max(4, arcSegments);
            ringSegments = Mathf.Max(6, ringSegments);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            for (var a = 0; a <= arcSegments; a++)
            {
                var t = a / (float)arcSegments;
                var angle = Mathf.Deg2Rad * arcDegrees * t;
                var center = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var binormal = Vector3.forward;
                for (var r = 0; r <= ringSegments; r++)
                {
                    var rt = r / (float)ringSegments;
                    var ringAngle = rt * Mathf.PI * 2f;
                    var local = radial * (Mathf.Cos(ringAngle) * tubeRadius) + binormal * (Mathf.Sin(ringAngle) * tubeRadius);
                    vertices.Add(center + local);
                    uvs.Add(new Vector2(t, rt));
                }
            }

            var stride = ringSegments + 1;
            for (var a = 0; a < arcSegments; a++)
            {
                for (var r = 0; r < ringSegments; r++)
                {
                    var i = a * stride + r;
                    AddQuad(triangles, i, i + stride, i + stride + 1, i + 1);
                }
            }
            return Finish("Civic_TubeArc", vertices, triangles, uvs);
        }

        public static Mesh ClassificationSeal(float radius, float depth, int teeth)
        {
            teeth = Mathf.Max(8, teeth);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var points = teeth * 2;

            for (var side = 0; side < 2; side++)
            {
                var z = side == 0 ? -depth * 0.5f : depth * 0.5f;
                var centerIndex = vertices.Count;
                vertices.Add(new Vector3(0f, 0f, z));
                uvs.Add(new Vector2(0.5f, 0.5f));
                for (var i = 0; i < points; i++)
                {
                    var angle = i * Mathf.PI * 2f / points;
                    var r = i % 2 == 0 ? radius : radius * 0.84f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, z));
                    uvs.Add(new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f));
                }

                for (var i = 0; i < points; i++)
                {
                    var next = (i + 1) % points;
                    if (side == 0)
                        triangles.AddRange(new[] { centerIndex, centerIndex + next + 1, centerIndex + i + 1 });
                    else
                        triangles.AddRange(new[] { centerIndex, centerIndex + i + 1, centerIndex + next + 1 });
                }
            }

            var ringOffset = points + 1;
            for (var i = 0; i < points; i++)
            {
                var next = (i + 1) % points;
                var a = i + 1;
                var b = next + 1;
                AddQuad(triangles, a, b, ringOffset + b, ringOffset + a);
            }
            return Finish("Civic_ClassificationSeal", vertices, triangles, uvs);
        }

        public static Mesh StitchedFrame(float width, float height, float depth, int stitches)
        {
            stitches = Mathf.Max(4, stitches);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var halfW = width * 0.5f;
            var halfH = height * 0.5f;
            AddBox(vertices, triangles, uvs, new Vector3(-halfW, 0f, 0f), new Vector3(depth, height, depth));
            AddBox(vertices, triangles, uvs, new Vector3(halfW, 0f, 0f), new Vector3(depth, height, depth));
            AddBox(vertices, triangles, uvs, new Vector3(0f, halfH, 0f), new Vector3(width, depth, depth));
            AddBox(vertices, triangles, uvs, new Vector3(0f, -halfH, 0f), new Vector3(width, depth, depth));

            for (var i = 0; i < stitches; i++)
            {
                var t = (i + 0.5f) / stitches;
                var y = Mathf.Lerp(-halfH * 0.75f, halfH * 0.75f, t);
                var lean = i % 2 == 0 ? -1f : 1f;
                AddBox(vertices, triangles, uvs, new Vector3(lean * depth * 0.8f, y, depth * 0.7f), new Vector3(depth * 0.65f, height / stitches * 0.55f, depth * 0.35f));
            }
            return Finish("Civic_StitchedFrame", vertices, triangles, uvs);
        }

        public static Mesh Ribbon(float length, float width, float amplitude, int segments)
        {
            segments = Mathf.Max(4, segments);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var x = Mathf.Lerp(-length * 0.5f, length * 0.5f, t);
                var y = Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
                var z = Mathf.Cos(t * Mathf.PI * 4f) * amplitude * 0.35f;
                vertices.Add(new Vector3(x, y - width * 0.5f, z));
                vertices.Add(new Vector3(x, y + width * 0.5f, z));
                uvs.Add(new Vector2(t, 0f));
                uvs.Add(new Vector2(t, 1f));
            }
            for (var i = 0; i < segments; i++)
            {
                var a = i * 2;
                AddQuad(triangles, a, a + 2, a + 3, a + 1);
            }
            return Finish("Civic_Ribbon", vertices, triangles, uvs);
        }

        // Compatibility overloads used by the production creature kit.
        public static Mesh Ribbon(float length, float width, int segments, float phase)
        {
            var mesh = Ribbon(length, width, 0.12f, segments);
            mesh.name = "Civic_Ribbon_Phase_" + phase.ToString("0.00");
            return mesh;
        }

        public static Mesh ArchRing(float radius, float tubeRadius, int segments, float arcDegrees)
        {
            return TubeArc(radius, tubeRadius, arcDegrees, segments, 8);
        }

        public static GameObject Create(string objectName, Mesh mesh, Material material, Transform parent)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            var node = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            node.transform.SetParent(parent, false);
            node.GetComponent<MeshFilter>().sharedMesh = mesh;
            node.GetComponent<MeshRenderer>().sharedMaterial = material;
            return node;
        }

        private static void AddBox(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 center, Vector3 size)
        {
            var start = vertices.Count;
            var h = size * 0.5f;
            vertices.AddRange(new[]
            {
                center + new Vector3(-h.x,-h.y,-h.z), center + new Vector3(h.x,-h.y,-h.z),
                center + new Vector3(h.x,h.y,-h.z), center + new Vector3(-h.x,h.y,-h.z),
                center + new Vector3(-h.x,-h.y,h.z), center + new Vector3(h.x,-h.y,h.z),
                center + new Vector3(h.x,h.y,h.z), center + new Vector3(-h.x,h.y,h.z)
            });
            for (var i = 0; i < 8; i++) uvs.Add(new Vector2((i & 1) == 0 ? 0f : 1f, (i & 2) == 0 ? 0f : 1f));
            AddQuad(triangles, start + 0, start + 3, start + 2, start + 1);
            AddQuad(triangles, start + 4, start + 5, start + 6, start + 7);
            AddQuad(triangles, start + 0, start + 4, start + 7, start + 3);
            AddQuad(triangles, start + 1, start + 2, start + 6, start + 5);
            AddQuad(triangles, start + 3, start + 7, start + 6, start + 2);
            AddQuad(triangles, start + 0, start + 1, start + 5, start + 4);
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }

        private static Mesh Finish(string name, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            if (uvs.Count == vertices.Count) mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
