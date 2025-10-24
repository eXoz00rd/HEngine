using System;
using System.Linq;
using System.Numerics;
using HEngine.Core.Rendering.Data;
using Xunit;

namespace HEngine.Core.Tests.Rendering
{
    public class MeshPrimitivesTests
    {
        [Fact(DisplayName = "Vertex3D stride should be 48 bytes")]        
        public void Vertex3D_Stride_Should_Be_48()
        {
            Assert.Equal((uint)48, Vertex3D.GetStride());
        }

        [Fact(DisplayName = "CreateCube should produce 24 vertices and 36 indices")]
        public void CreateCube_Should_Produce_Correct_Counts()
        {
            var (verts, indices) = MeshPrimitives.CreateCube(2.0f);
            Assert.Equal(24, verts.Length);
            Assert.Equal(36, indices.Length);
        }

        [Fact(DisplayName = "CreateCube normals should be outward and unit length")]
        public void CreateCube_Normals_Should_Be_Outward_And_Unit()
        {
            var (verts, _) = MeshPrimitives.CreateCube(2.0f);

            for (int face = 0; face < 6; face++)
            {
                int baseIdx = face * 4;
                var n0 = verts[baseIdx + 0].Normal;
                var n1 = verts[baseIdx + 1].Normal;
                var n2 = verts[baseIdx + 2].Normal;
                var n3 = verts[baseIdx + 3].Normal;

              
                Assert.Equal(n0, n1);
                Assert.Equal(n0, n2);
                Assert.Equal(n0, n3);

            
                Assert.InRange(System.MathF.Abs(n0.Length() - 1f), 0f, 1e-5f);
                
                var faceCenter = (verts[baseIdx + 0].Position + verts[baseIdx + 1].Position +
                                  verts[baseIdx + 2].Position + verts[baseIdx + 3].Position) / 4f;
                var dir = Vector3.Normalize(faceCenter - Vector3.Zero);
                Assert.True(Vector3.Dot(n0, dir) > 0.99f);
            }
        }

        [Fact(DisplayName = "CreateCube indices should have consistent winding per face (all CCW or all CW)")]
        public void CreateCube_Winding_Should_Be_Consistent_Per_Face()
        {
            var (verts, indices) = MeshPrimitives.CreateCube(2.0f);
            
            int baseIdx0 = 0;
            var fn0 = verts[0].Normal;
            bool expectCCW = IsTriangleCCW(verts[indices[baseIdx0 + 0]].Position, 
                                           verts[indices[baseIdx0 + 1]].Position,
                                           verts[indices[baseIdx0 + 2]].Position, fn0);

            for (int face = 0; face < 6; face++)
            {
                int baseVert = face * 4;
                int baseIdx = face * 6;

                var i0 = (int)indices[baseIdx + 0];
                var i1 = (int)indices[baseIdx + 1];
                var i2 = (int)indices[baseIdx + 2];

                var j0 = (int)indices[baseIdx + 3];
                var j1 = (int)indices[baseIdx + 4];
                var j2 = (int)indices[baseIdx + 5];
                
                var fn = verts[baseVert].Normal;

                bool ccw1 = IsTriangleCCW(verts[i0].Position, verts[i1].Position, verts[i2].Position, fn);
                bool ccw2 = IsTriangleCCW(verts[j0].Position, verts[j1].Position, verts[j2].Position, fn);

                Assert.True(ccw1 == expectCCW, $"Face {face} first triangle winding inconsistent");
                Assert.True(ccw2 == expectCCW, $"Face {face} second triangle winding inconsistent");
            }
        }

        private static bool IsTriangleCCW(in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 expectedNormal)
        {
            var ab = b - a;
            var ac = c - a;
            var cross = Vector3.Cross(ab, ac);
            return Vector3.Dot(Vector3.Normalize(cross), Vector3.Normalize(expectedNormal)) > 0.99f;
        }
    }
}
