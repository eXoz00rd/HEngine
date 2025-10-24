using System;
using System.Linq;
using System.Numerics;
using HEngine.Core.Rendering.Data;
using Xunit;

namespace HEngine.Core.Tests.Rendering
{
    public class MeshPrimitivesPlaneTests
    {
        [Fact(DisplayName = "CreatePlane should produce 4 vertices and 6 indices")]
        public void CreatePlane_Counts_Are_Correct()
        {
            var (vertices, indices) = MeshPrimitives.CreatePlane(4.0f, 6.0f);
            Assert.Equal(4, vertices.Length);
            Assert.Equal(6, indices.Length);
            
            foreach (var idx in indices)
                Assert.InRange(idx, 0u, (uint)(vertices.Length - 1));
        }

        [Fact(DisplayName = "CreatePlane normals should be +Y and unit length")]
        public void CreatePlane_Normals_Are_Up_And_Unit()
        {
            var (vertices, _) = MeshPrimitives.CreatePlane(4.0f, 6.0f);
            foreach (var v in vertices)
            {
                Assert.True(Vector3.Distance(v.Normal, Vector3.UnitY) < 1e-5f);
                Assert.InRange(System.MathF.Abs(v.Normal.Length() - 1f), 0f, 1e-5f);
            }
        }

        [Fact(DisplayName = "CreatePlane winding is CCW when viewed from +Y")]
        public void CreatePlane_Winding_Is_CCW_From_Up()
        {
            var (vertices, indices) = MeshPrimitives.CreatePlane(4.0f, 6.0f);
            
            var a = vertices[indices[0]].Position;
            var b = vertices[indices[1]].Position;
            var c = vertices[indices[2]].Position;
            var cross0 = Vector3.Cross(b - a, c - a);
            Assert.True(Vector3.Dot(Vector3.Normalize(cross0), Vector3.UnitY) > 0.99f);
            
            var d = vertices[indices[3]].Position;
            var e = vertices[indices[4]].Position;
            var f = vertices[indices[5]].Position;
            var cross1 = Vector3.Cross(e - d, f - d);
            Assert.True(Vector3.Dot(Vector3.Normalize(cross1), Vector3.UnitY) > 0.99f);
        }
    }
}
