using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Flat-shaded, vertex-coloured mesh construction kit.  Every prop, machine, vehicle and
    /// character in Void Mart is assembled from these primitives, which is what keeps the whole
    /// game installable from a single button with no imported art.
    ///
    /// Faces never share vertices, so normals stay hard and the low-poly facets read cleanly.
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> m_Vertices = new List<Vector3>(512);
        readonly List<Vector3> m_Normals = new List<Vector3>(512);
        readonly List<Vector2> m_Uvs = new List<Vector2>(512);
        readonly List<Color> m_Colors = new List<Color>(512);
        readonly List<int> m_Triangles = new List<int>(1024);
        readonly Stack<Matrix4x4> m_Stack = new Stack<Matrix4x4>(8);

        Matrix4x4 m_Matrix = Matrix4x4.identity;

        public int VertexCount => m_Vertices.Count;
        public int TriangleCount => m_Triangles.Count / 3;

        // ------------------------------------------------------------- transform

        public void Push(Vector3 position) => Push(Matrix4x4.Translate(position));
        public void Push(Vector3 position, Quaternion rotation) => Push(Matrix4x4.TRS(position, rotation, Vector3.one));
        public void Push(Vector3 position, Quaternion rotation, Vector3 scale) => Push(Matrix4x4.TRS(position, rotation, scale));

        public void Push(Matrix4x4 matrix)
        {
            m_Stack.Push(m_Matrix);
            m_Matrix = m_Matrix * matrix;
        }

        public void Pop()
        {
            if (m_Stack.Count > 0) m_Matrix = m_Stack.Pop();
        }

        Vector3 TransformPoint(Vector3 point) => m_Matrix.MultiplyPoint3x4(point);
        Vector3 TransformDirection(Vector3 direction) => m_Matrix.MultiplyVector(direction).normalized;

        // ------------------------------------------------------------ primitives

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color, Vector3 expectedNormal = default)
        {
            a = TransformPoint(a); b = TransformPoint(b); c = TransformPoint(c);
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-12f) return;
            normal.Normalize();

            if (expectedNormal != default && Vector3.Dot(normal, TransformDirection(expectedNormal)) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }

            int baseIndex = m_Vertices.Count;
            Emit(a, normal, new Vector2(0f, 0f), color);
            Emit(b, normal, new Vector2(1f, 0f), color);
            Emit(c, normal, new Vector2(0.5f, 1f), color);
            m_Triangles.Add(baseIndex);
            m_Triangles.Add(baseIndex + 1);
            m_Triangles.Add(baseIndex + 2);
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 expectedNormal = default)
        {
            Vector3 wa = TransformPoint(a), wb = TransformPoint(b), wc = TransformPoint(c), wd = TransformPoint(d);
            Vector3 normal = Vector3.Cross(wb - wa, wd - wa);
            if (normal.sqrMagnitude < 1e-12f) return;
            normal.Normalize();

            if (expectedNormal != default && Vector3.Dot(normal, TransformDirection(expectedNormal)) < 0f)
            {
                (wb, wd) = (wd, wb);
                normal = -normal;
            }

            int baseIndex = m_Vertices.Count;
            Emit(wa, normal, new Vector2(0f, 0f), color);
            Emit(wb, normal, new Vector2(1f, 0f), color);
            Emit(wc, normal, new Vector2(1f, 1f), color);
            Emit(wd, normal, new Vector2(0f, 1f), color);
            m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 1); m_Triangles.Add(baseIndex + 2);
            m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 2); m_Triangles.Add(baseIndex + 3);
        }

        void Emit(Vector3 position, Vector3 normal, Vector2 uv, Color color)
        {
            m_Vertices.Add(position);
            m_Normals.Add(normal);
            m_Uvs.Add(uv);
            m_Colors.Add(color);
        }

        /// <summary>Axis-aligned box, optionally chamfered for that moulded-plastic silhouette.</summary>
        public void AddBox(Vector3 centre, Vector3 size, Color color, float chamfer = 0f)
        {
            Vector3 h = size * 0.5f;
            float c = Mathf.Clamp(chamfer, 0f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.48f);

            if (c <= 0.0001f)
            {
                AddPlainBox(centre, h, color);
                return;
            }

            Vector3 inner = new Vector3(h.x - c, h.y - c, h.z - c);

            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int b = (axis + 1) % 3;
                    int d = (axis + 2) % 3;
                    Vector3 normal = Axis(axis) * sign;

                    Vector3 v0 = Compose(axis, sign * h[axis], b, -inner[b], d, -inner[d]) + centre;
                    Vector3 v1 = Compose(axis, sign * h[axis], b, inner[b], d, -inner[d]) + centre;
                    Vector3 v2 = Compose(axis, sign * h[axis], b, inner[b], d, inner[d]) + centre;
                    Vector3 v3 = Compose(axis, sign * h[axis], b, -inner[b], d, inner[d]) + centre;
                    AddQuad(v0, v1, v2, v3, color, normal);
                }
            }

            // 12 chamfer strips
            for (int a = 0; a < 3; a++)
            {
                for (int b = a + 1; b < 3; b++)
                {
                    int d = 3 - a - b;
                    for (int sa = -1; sa <= 1; sa += 2)
                    {
                        for (int sb = -1; sb <= 1; sb += 2)
                        {
                            Vector3 normal = (Axis(a) * sa + Axis(b) * sb).normalized;
                            Vector3 p0 = Compose(a, sa * h[a], b, sb * inner[b], d, -inner[d]) + centre;
                            Vector3 p1 = Compose(a, sa * h[a], b, sb * inner[b], d, inner[d]) + centre;
                            Vector3 p2 = Compose(a, sa * inner[a], b, sb * h[b], d, inner[d]) + centre;
                            Vector3 p3 = Compose(a, sa * inner[a], b, sb * h[b], d, -inner[d]) + centre;
                            AddQuad(p0, p1, p2, p3, color, normal);
                        }
                    }
                }
            }

            // 8 corner facets
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        Vector3 normal = new Vector3(sx, sy, sz).normalized;
                        Vector3 p0 = centre + new Vector3(sx * h.x, sy * inner.y, sz * inner.z);
                        Vector3 p1 = centre + new Vector3(sx * inner.x, sy * h.y, sz * inner.z);
                        Vector3 p2 = centre + new Vector3(sx * inner.x, sy * inner.y, sz * h.z);
                        AddTriangle(p0, p1, p2, color, normal);
                    }
                }
            }
        }

        void AddPlainBox(Vector3 centre, Vector3 h, Color color)
        {
            Vector3 p000 = centre + new Vector3(-h.x, -h.y, -h.z);
            Vector3 p100 = centre + new Vector3(h.x, -h.y, -h.z);
            Vector3 p110 = centre + new Vector3(h.x, h.y, -h.z);
            Vector3 p010 = centre + new Vector3(-h.x, h.y, -h.z);
            Vector3 p001 = centre + new Vector3(-h.x, -h.y, h.z);
            Vector3 p101 = centre + new Vector3(h.x, -h.y, h.z);
            Vector3 p111 = centre + new Vector3(h.x, h.y, h.z);
            Vector3 p011 = centre + new Vector3(-h.x, h.y, h.z);

            AddQuad(p001, p101, p111, p011, color, Vector3.forward);
            AddQuad(p100, p000, p010, p110, color, Vector3.back);
            AddQuad(p101, p100, p110, p111, color, Vector3.right);
            AddQuad(p000, p001, p011, p010, color, Vector3.left);
            AddQuad(p010, p011, p111, p110, color, Vector3.up);
            AddQuad(p000, p100, p101, p001, color, Vector3.down);
        }

        static Vector3 Axis(int index) => index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;

        static Vector3 Compose(int a, float va, int b, float vb, int c, float vc)
        {
            var result = Vector3.zero;
            result[a] = va;
            result[b] = vb;
            result[c] = vc;
            return result;
        }

        /// <summary>Cone / cylinder / truncated cone around the Y axis.</summary>
        public void AddCylinder(Vector3 baseCentre, float bottomRadius, float topRadius, float height, int sides, Color color, bool caps = true, Color? topColor = null)
        {
            sides = Mathf.Clamp(sides, 3, 64);
            Color capColor = topColor ?? color;
            Vector3 topCentre = baseCentre + Vector3.up * height;

            for (int i = 0; i < sides; i++)
            {
                float t0 = (float)i / sides * Mathf.PI * 2f;
                float t1 = (float)(i + 1) / sides * Mathf.PI * 2f;
                Vector3 d0 = new Vector3(Mathf.Cos(t0), 0f, Mathf.Sin(t0));
                Vector3 d1 = new Vector3(Mathf.Cos(t1), 0f, Mathf.Sin(t1));

                Vector3 b0 = baseCentre + d0 * bottomRadius;
                Vector3 b1 = baseCentre + d1 * bottomRadius;
                Vector3 t0p = topCentre + d0 * topRadius;
                Vector3 t1p = topCentre + d1 * topRadius;

                Vector3 outward = (d0 + d1).normalized;
                if (topRadius > 0.0001f && bottomRadius > 0.0001f)
                    AddQuad(b0, b1, t1p, t0p, color, outward);
                else if (topRadius <= 0.0001f)
                    AddTriangle(b0, b1, topCentre, color, outward);
                else
                    AddTriangle(t0p, t1p, baseCentre, color, outward);

                if (!caps) continue;
                if (bottomRadius > 0.0001f) AddTriangle(baseCentre, b1, b0, color, Vector3.down);
                if (topRadius > 0.0001f) AddTriangle(topCentre, t0p, t1p, capColor, Vector3.up);
            }
        }

        /// <summary>Faceted sphere (icosphere-lite via lat/long bands).</summary>
        public void AddSphere(Vector3 centre, float radius, int segments, int rings, Color color)
        {
            segments = Mathf.Clamp(segments, 4, 48);
            rings = Mathf.Clamp(rings, 2, 32);

            for (int r = 0; r < rings; r++)
            {
                float phi0 = Mathf.PI * r / rings;
                float phi1 = Mathf.PI * (r + 1) / rings;
                for (int s = 0; s < segments; s++)
                {
                    float theta0 = Mathf.PI * 2f * s / segments;
                    float theta1 = Mathf.PI * 2f * (s + 1) / segments;

                    Vector3 p00 = centre + Spherical(radius, phi0, theta0);
                    Vector3 p01 = centre + Spherical(radius, phi0, theta1);
                    Vector3 p10 = centre + Spherical(radius, phi1, theta0);
                    Vector3 p11 = centre + Spherical(radius, phi1, theta1);

                    Vector3 outward = ((p00 + p01 + p10 + p11) * 0.25f - centre).normalized;
                    if (r == 0) AddTriangle(p00, p11, p10, color, outward);
                    else if (r == rings - 1) AddTriangle(p00, p01, p10, color, outward);
                    else AddQuad(p00, p01, p11, p10, color, outward);
                }
            }
        }

        static Vector3 Spherical(float radius, float phi, float theta) => new Vector3(
            radius * Mathf.Sin(phi) * Mathf.Cos(theta),
            radius * Mathf.Cos(phi),
            radius * Mathf.Sin(phi) * Mathf.Sin(theta));

        /// <summary>Flat ring / disc lying in the XZ plane (used for the event horizon).</summary>
        public void AddRing(Vector3 centre, float innerRadius, float outerRadius, int sides, Color innerColor, Color outerColor)
        {
            sides = Mathf.Clamp(sides, 6, 128);
            for (int i = 0; i < sides; i++)
            {
                float t0 = (float)i / sides * Mathf.PI * 2f;
                float t1 = (float)(i + 1) / sides * Mathf.PI * 2f;
                Vector3 d0 = new Vector3(Mathf.Cos(t0), 0f, Mathf.Sin(t0));
                Vector3 d1 = new Vector3(Mathf.Cos(t1), 0f, Mathf.Sin(t1));

                Vector3 i0 = centre + d0 * innerRadius;
                Vector3 i1 = centre + d1 * innerRadius;
                Vector3 o0 = centre + d0 * outerRadius;
                Vector3 o1 = centre + d1 * outerRadius;

                int baseIndex = m_Vertices.Count;
                Vector3 up = TransformDirection(Vector3.up);
                Emit(TransformPoint(i0), up, new Vector2(0f, 0f), innerColor);
                Emit(TransformPoint(o0), up, new Vector2(1f, 0f), outerColor);
                Emit(TransformPoint(o1), up, new Vector2(1f, 1f), outerColor);
                Emit(TransformPoint(i1), up, new Vector2(0f, 1f), innerColor);
                m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 1); m_Triangles.Add(baseIndex + 2);
                m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 2); m_Triangles.Add(baseIndex + 3);
            }
        }

        /// <summary>Wedge / gable roof along X.</summary>
        public void AddWedge(Vector3 centre, Vector3 size, Color color)
        {
            Vector3 h = size * 0.5f;
            Vector3 a = centre + new Vector3(-h.x, -h.y, -h.z);
            Vector3 b = centre + new Vector3(h.x, -h.y, -h.z);
            Vector3 c = centre + new Vector3(h.x, -h.y, h.z);
            Vector3 d = centre + new Vector3(-h.x, -h.y, h.z);
            Vector3 apexA = centre + new Vector3(-h.x, h.y, 0f);
            Vector3 apexB = centre + new Vector3(h.x, h.y, 0f);

            AddQuad(a, b, c, d, color, Vector3.down);
            AddQuad(d, c, apexB, apexA, color, new Vector3(0f, 1f, 1f).normalized);
            AddQuad(b, a, apexA, apexB, color, new Vector3(0f, 1f, -1f).normalized);
            AddTriangle(a, d, apexA, color, Vector3.left);
            AddTriangle(c, b, apexB, color, Vector3.right);
        }

        /// <summary>Single quad in the XZ plane, for ground planes and decals.</summary>
        public void AddGroundQuad(Vector3 centre, Vector2 size, Color color, float uvTiling = 1f)
        {
            Vector3 h = new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);
            Vector3 a = centre + new Vector3(-h.x, 0f, -h.z);
            Vector3 b = centre + new Vector3(h.x, 0f, -h.z);
            Vector3 c = centre + new Vector3(h.x, 0f, h.z);
            Vector3 d = centre + new Vector3(-h.x, 0f, h.z);

            int baseIndex = m_Vertices.Count;
            Vector3 up = TransformDirection(Vector3.up);
            Emit(TransformPoint(a), up, new Vector2(0f, 0f), color);
            Emit(TransformPoint(b), up, new Vector2(uvTiling, 0f), color);
            Emit(TransformPoint(c), up, new Vector2(uvTiling, uvTiling), color);
            Emit(TransformPoint(d), up, new Vector2(0f, uvTiling), color);
            m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 1); m_Triangles.Add(baseIndex + 2);
            m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 2); m_Triangles.Add(baseIndex + 3);
        }

        /// <summary>Upright quad in the XY plane (billboards, stencil masks).</summary>
        public void AddUprightQuad(Vector3 centre, Vector2 size, Color color)
        {
            Vector3 h = new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
            Vector3 a = centre + new Vector3(-h.x, -h.y, 0f);
            Vector3 b = centre + new Vector3(h.x, -h.y, 0f);
            Vector3 c = centre + new Vector3(h.x, h.y, 0f);
            Vector3 d = centre + new Vector3(-h.x, h.y, 0f);

            int baseIndex = m_Vertices.Count;
            Vector3 normal = TransformDirection(Vector3.back);
            Emit(TransformPoint(a), normal, new Vector2(0f, 0f), color);
            Emit(TransformPoint(b), normal, new Vector2(1f, 0f), color);
            Emit(TransformPoint(c), normal, new Vector2(1f, 1f), color);
            Emit(TransformPoint(d), normal, new Vector2(0f, 1f), color);
            m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 2); m_Triangles.Add(baseIndex + 1);
            m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 3); m_Triangles.Add(baseIndex + 2);
        }

        /// <summary>Hollow tube - used for the pit shaft (front faces culled at render time).</summary>
        public void AddTube(Vector3 topCentre, float radius, float depth, int sides, Color topColor, Color bottomColor)
        {
            sides = Mathf.Clamp(sides, 6, 64);
            Vector3 bottomCentre = topCentre + Vector3.down * depth;
            for (int i = 0; i < sides; i++)
            {
                float t0 = (float)i / sides * Mathf.PI * 2f;
                float t1 = (float)(i + 1) / sides * Mathf.PI * 2f;
                Vector3 d0 = new Vector3(Mathf.Cos(t0), 0f, Mathf.Sin(t0));
                Vector3 d1 = new Vector3(Mathf.Cos(t1), 0f, Mathf.Sin(t1));

                Vector3 tv0 = topCentre + d0 * radius;
                Vector3 tv1 = topCentre + d1 * radius;
                Vector3 bv0 = bottomCentre + d0 * radius * 0.35f;
                Vector3 bv1 = bottomCentre + d1 * radius * 0.35f;

                int baseIndex = m_Vertices.Count;
                Vector3 inward = -(d0 + d1).normalized;
                Emit(TransformPoint(tv0), TransformDirection(inward), new Vector2(0f, 1f), topColor);
                Emit(TransformPoint(tv1), TransformDirection(inward), new Vector2(1f, 1f), topColor);
                Emit(TransformPoint(bv1), TransformDirection(inward), new Vector2(1f, 0f), bottomColor);
                Emit(TransformPoint(bv0), TransformDirection(inward), new Vector2(0f, 0f), bottomColor);
                m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 1); m_Triangles.Add(baseIndex + 2);
                m_Triangles.Add(baseIndex); m_Triangles.Add(baseIndex + 2); m_Triangles.Add(baseIndex + 3);
            }

            // Floor of the shaft so the void is never see-through.
            for (int i = 0; i < sides; i++)
            {
                float t0 = (float)i / sides * Mathf.PI * 2f;
                float t1 = (float)(i + 1) / sides * Mathf.PI * 2f;
                Vector3 d0 = new Vector3(Mathf.Cos(t0), 0f, Mathf.Sin(t0)) * radius * 0.35f;
                Vector3 d1 = new Vector3(Mathf.Cos(t1), 0f, Mathf.Sin(t1)) * radius * 0.35f;
                AddTriangle(bottomCentre, bottomCentre + d0, bottomCentre + d1, bottomColor, Vector3.up);
            }
        }

        // ---------------------------------------------------------------- output

        public void Clear()
        {
            m_Vertices.Clear();
            m_Normals.Clear();
            m_Uvs.Clear();
            m_Colors.Clear();
            m_Triangles.Clear();
            m_Stack.Clear();
            m_Matrix = Matrix4x4.identity;
        }

        public Mesh ToMesh(string meshName, bool recalculateBounds = true)
        {
            var mesh = new Mesh { name = meshName };
            if (m_Vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(m_Vertices);
            mesh.SetNormals(m_Normals);
            mesh.SetUVs(0, m_Uvs);
            mesh.SetColors(m_Colors);
            mesh.SetTriangles(m_Triangles, 0);
            if (recalculateBounds) mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>Uniformly scales and centres the accumulated geometry on its base.</summary>
        public void NormalizeToHeight(float targetHeight, bool sitOnGround = true)
        {
            if (m_Vertices.Count == 0) return;
            var bounds = new Bounds(m_Vertices[0], Vector3.zero);
            for (int i = 1; i < m_Vertices.Count; i++) bounds.Encapsulate(m_Vertices[i]);

            float scale = bounds.size.y > 0.0001f ? targetHeight / bounds.size.y : 1f;
            for (int i = 0; i < m_Vertices.Count; i++)
            {
                Vector3 v = m_Vertices[i];
                v.x = (v.x - bounds.center.x) * scale;
                v.z = (v.z - bounds.center.z) * scale;
                v.y = sitOnGround ? (v.y - bounds.min.y) * scale : (v.y - bounds.center.y) * scale;
                m_Vertices[i] = v;
            }
        }
    }
}
