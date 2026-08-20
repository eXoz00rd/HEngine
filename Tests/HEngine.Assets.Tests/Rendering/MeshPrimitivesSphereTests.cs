using System;
using System.Linq;
using System.Numerics;
using HEngine.Core.Rendering.Data;
using Xunit;

namespace HEngine.Core.Tests.Rendering
{
    public class MeshPrimitivesSphereTests
    {
        [Fact(DisplayName = "CreateSphere should produce expected vertex and index counts for segments=8")]
        public void CreateSphere_Counts_Are_Correct()
        {
            int segments = 8;
            float radius = 2.5f;
            var (vertices, indices) = MeshPrimitives.CreateSphere(radius, segments);

            int expectedVerts = (segments + 1) * (segments + 1);
            int expectedIndices = 6 * segments * (segments - 1);

            Assert.Equal(expectedVerts, vertices.Length);
            Assert.Equal(expectedIndices, indices.Length);
            
            foreach (var idx in indices)
                Assert.InRange(idx, 0u, (uint)(vertices.Length - 1));
        }

        [Fact(DisplayName = "CreateSphere normals should be outward (position normalized) and unit length")]
        public void CreateSphere_Normals_Outward_And_Unit()
        {
            int segments = 10;
            float radius = 3.0f;
            var (vertices, _) = MeshPrimitives.CreateSphere(radius, segments);

            foreach (var v in vertices)
            {
                if (v.Position.Length() > 1e-6f)
                {
                    var expectedNormal = Vector3.Normalize(v.Position);
                    Assert.InRange(System.MathF.Abs(v.Normal.Length() - 1f), 0f, 1e-5f);
                    Assert.True(Vector3.Dot(v.Normal, expectedNormal) > 0.999f);
                }
            }
        }

        [Fact(DisplayName = "CreateSphere triangles are non-degenerate and face normals align with vertex normals (up to sign)")]
        public void CreateSphere_Triangles_NonDegenerate_And_Aligned()
        {
            int segments = 6;
            float radius = 1.0f;
            var (vertices, indices) = MeshPrimitives.CreateSphere(radius, segments);

            for (int i = 0; i < indices.Length; i += 3)
            {
                var i0 = (int)indices[i];
                var i1 = (int)indices[i + 1];
                var i2 = (int)indices[i + 2];

                var a = vertices[i0].Position;
                var b = vertices[i1].Position;
                var c = vertices[i2].Position;

                var ab = b - a;
                var ac = c - a;
                var cross = Vector3.Cross(ab, ac);
                
                Assert.True(cross.Length() > 1e-6f);

                var avgNormal = Vector3.Normalize((vertices[i0].Normal + vertices[i1].Normal + vertices[i2].Normal) / 3f);
                var crossDir = Vector3.Normalize(cross);
                
                Assert.True(System.MathF.Abs(Vector3.Dot(crossDir, avgNormal)) > 0.9f);
            }
        }

        [Fact(DisplayName = "CreateSphere should throw for invalid parameters")]
        public void CreateSphere_Invalid_Parameters()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MeshPrimitives.CreateSphere(0f, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => MeshPrimitives.CreateSphere(1f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => MeshPrimitives.CreateSphere(-1f, 2));
        }
    }
}
