using System;
using System.Numerics;

namespace HEngine.Core.Rendering.Data
{
    public static class MeshPrimitives
    {
        public static (Vertex3D[] vertices, uint[] indices) CreateCube(float size = 1.0f, Vector4? color = null)
        {
            float h = size * 0.5f;
            Vector4 c = color ?? new Vector4(1, 1, 1, 1);
            
            var verts = new Vertex3D[24];
            int vi = 0;
            
            var nx = new Vector3(1, 0, 0);
            verts[vi++] = new Vertex3D(new Vector3(+h, -h, -h), nx, new Vector2(0, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, -h, +h), nx, new Vector2(1, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, +h, +h), nx, new Vector2(1, 0), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, +h, -h), nx, new Vector2(0, 0), c);

            nx = new Vector3(-1, 0, 0);
            verts[vi++] = new Vertex3D(new Vector3(-h, -h, +h), nx, new Vector2(0, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(-h, -h, -h), nx, new Vector2(1, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(-h, +h, -h), nx, new Vector2(1, 0), c);
            verts[vi++] = new Vertex3D(new Vector3(-h, +h, +h), nx, new Vector2(0, 0), c);

            var ny = new Vector3(0, 1, 0);
            verts[vi++] = new Vertex3D(new Vector3(-h, +h, -h), ny, new Vector2(0, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, +h, -h), ny, new Vector2(1, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, +h, +h), ny, new Vector2(1, 0), c);
            verts[vi++] = new Vertex3D(new Vector3(-h, +h, +h), ny, new Vector2(0, 0), c);

            ny = new Vector3(0, -1, 0);
            verts[vi++] = new Vertex3D(new Vector3(-h, -h, +h), ny, new Vector2(0, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, -h, +h), ny, new Vector2(1, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, -h, -h), ny, new Vector2(1, 0), c);
            verts[vi++] = new Vertex3D(new Vector3(-h, -h, -h), ny, new Vector2(0, 0), c);

            var nz = new Vector3(0, 0, 1);
            verts[vi++] = new Vertex3D(new Vector3(-h, -h, +h), nz, new Vector2(0, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, -h, +h), nz, new Vector2(1, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, +h, +h), nz, new Vector2(1, 0), c);
            verts[vi++] = new Vertex3D(new Vector3(-h, +h, +h), nz, new Vector2(0, 0), c);

            nz = new Vector3(0, 0, -1);
            verts[vi++] = new Vertex3D(new Vector3(+h, -h, -h), nz, new Vector2(0, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(-h, -h, -h), nz, new Vector2(1, 1), c);
            verts[vi++] = new Vertex3D(new Vector3(-h, +h, -h), nz, new Vector2(1, 0), c);
            verts[vi++] = new Vertex3D(new Vector3(+h, +h, -h), nz, new Vector2(0, 0), c);
            
            var indices = new uint[36];
            int ii = 0;
            for (uint face = 0; face < 6; face++)
            {
                uint baseIndex = face * 4u;
                if (face <= 3)
                {
                    indices[ii++] = baseIndex + 0;
                    indices[ii++] = baseIndex + 2;
                    indices[ii++] = baseIndex + 1;

                    indices[ii++] = baseIndex + 0;
                    indices[ii++] = baseIndex + 3;
                    indices[ii++] = baseIndex + 2;
                }
                else
                {
                    indices[ii++] = baseIndex + 0;
                    indices[ii++] = baseIndex + 1;
                    indices[ii++] = baseIndex + 2;

                    indices[ii++] = baseIndex + 0;
                    indices[ii++] = baseIndex + 2;
                    indices[ii++] = baseIndex + 3;
                }
            }

            return (verts, indices);
        }

        public static (Vertex3D[] vertices, uint[] indices) CreatePlane(float width, float depth, Vector4? color = null)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            Vector4 c = color ?? new Vector4(1, 1, 1, 1);
            
            var n = new Vector3(0, 1, 0);
            var verts = new[]
            {
                new Vertex3D(new Vector3(-hw, 0f, -hd), n, new Vector2(0f, 1f), c),
                new Vertex3D(new Vector3(+hw, 0f, -hd), n, new Vector2(1f, 1f), c),
                new Vertex3D(new Vector3(+hw, 0f, +hd), n, new Vector2(1f, 0f), c),
                new Vertex3D(new Vector3(-hw, 0f, +hd), n, new Vector2(0f, 0f), c),
            };
            
            var indices = new uint[] { 0, 2, 1, 0, 3, 2 };
            return (verts, indices);
        }

        public static (Vertex3D[] vertices, uint[] indices) CreateSphere(float radius, int segments, Vector4? color = null)
        {
            if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments));

            int latSegments = segments;
            int lonSegments = segments;
            int vertCount = (latSegments + 1) * (lonSegments + 1);
            int indexCount = 6 * lonSegments * (latSegments - 1);

            var verts = new Vertex3D[vertCount];
            var indices = new uint[indexCount];
            Vector4 c = color ?? new Vector4(1, 1, 1, 1);

            int v = 0;
            for (int y = 0; y <= latSegments; y++)
            {
                float vTex = (float)y / latSegments;
                float phi = vTex * MathF.PI;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                for (int x = 0; x <= lonSegments; x++)
                {
                    float uTex = (float)x / lonSegments;
                    float theta = uTex * MathF.Tau;
                    float sinTheta = MathF.Sin(theta);
                    float cosTheta = MathF.Cos(theta);

                    var normal = new Vector3(cosTheta * sinPhi, cosPhi, sinTheta * sinPhi);
                    var position = normal * radius;
                    var tex = new Vector2(uTex, 1f - vTex);

                    verts[v++] = new Vertex3D(position, Vector3.Normalize(normal), tex, c);
                }
            }

            int i = 0;
            int rowStride = lonSegments + 1;
            for (int y = 0; y < latSegments; y++)
            {
                for (int x = 0; x < lonSegments; x++)
                {
                    int i0 = y * rowStride + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + rowStride;
                    int i3 = i2 + 1;

                    if (y == 0)
                    {
                        indices[i++] = (uint)i0;
                        indices[i++] = (uint)i2;
                        indices[i++] = (uint)i3;
                    }
                    else if (y == latSegments - 1)
                    {
                        indices[i++] = (uint)i0;
                        indices[i++] = (uint)i1;
                        indices[i++] = (uint)i2;
                    }
                    else
                    {
                        indices[i++] = (uint)i0;
                        indices[i++] = (uint)i1;
                        indices[i++] = (uint)i2;

                        indices[i++] = (uint)i1;
                        indices[i++] = (uint)i3;
                        indices[i++] = (uint)i2;
                    }
                }
            }

            return (verts, indices);
        }
    }
}
