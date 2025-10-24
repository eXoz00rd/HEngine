using System.Linq;
using System.Numerics;
using HEngine.Core.Rendering.Data;
using Xunit;

namespace HEngine.Core.Tests.Rendering
{
    public class MeshPrimitivesAdditionalTests
    {
        [Fact(DisplayName = "CreateCube should produce 24 vertices and 36 indices with outward normals")]
        public void CreateCube_BasicCounts_And_Normals()
        {
            var (vertices, indices) = MeshPrimitives.CreateCube(2.0f);
            Assert.Equal(24, vertices.Length);
            Assert.Equal(36, indices.Length);
            
            var uniqueNormals = vertices.Select(v => v.Normal).Distinct().ToArray();
            Assert.Equal(6, uniqueNormals.Length);
            
            foreach (var n in uniqueNormals)
            {
                Assert.True(Vector3.Distance(n, Vector3.UnitX) < 1e-4f
                            || Vector3.Distance(n, -Vector3.UnitX) < 1e-4f
                            || Vector3.Distance(n, Vector3.UnitY) < 1e-4f
                            || Vector3.Distance(n, -Vector3.UnitY) < 1e-4f
                            || Vector3.Distance(n, Vector3.UnitZ) < 1e-4f
                            || Vector3.Distance(n, -Vector3.UnitZ) < 1e-4f);
            }
        }

        [Fact(DisplayName = "CreateCube index winding should be consistent (non-degenerate triangles)")]
        public void CreateCube_Winding_Produces_NonDegenerate_Triangles()
        {
            var (vertices, indices) = MeshPrimitives.CreateCube(2.0f);
            
            for (int i = 0; i < indices.Length; i += 3)
            {
                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];

                Assert.InRange(i0, 0u, (uint)(vertices.Length - 1));
                Assert.InRange(i1, 0u, (uint)(vertices.Length - 1));
                Assert.InRange(i2, 0u, (uint)(vertices.Length - 1));

                Assert.NotEqual(i0, i1);
                Assert.NotEqual(i1, i2);
                Assert.NotEqual(i2, i0);
                
                var a = vertices[i0].Position;
                var b = vertices[i1].Position;
                var c = vertices[i2].Position;

                var ab = b - a;
                var ac = c - a;
                var cross = Vector3.Cross(ab, ac);
                Assert.True(cross.Length() > 1e-6f);
            }
        }
    }
}
