using HEngine.Rendering.Data;
using Xunit;

namespace HEngine.Rendering.Tests.Data;

public class PrimitiveGeometryCacheTests
{
    [Fact(DisplayName = "Get returns the same cached arrays for repeated calls with the same id")]
    public void Get_ReturnsSameArrays_ForRepeatedCalls()
    {
        var (verticesA, indicesA) = PrimitiveGeometryCache.Get(1);
        var (verticesB, indicesB) = PrimitiveGeometryCache.Get(1);

        Assert.Same(verticesA, verticesB);
        Assert.Same(indicesA, indicesB);
    }

    [Theory(DisplayName = "Get produces the expected geometry for each supported primitive id")]
    [InlineData(1u, 24, 36)]
    [InlineData(2u, 4, 6)]
    public void Get_ProducesExpectedGeometry_ForCubeAndPlane(uint vertexArrayId, int expectedVertexCount, int expectedIndexCount)
    {
        var (vertices, indices) = PrimitiveGeometryCache.Get(vertexArrayId);

        Assert.Equal(expectedVertexCount, vertices.Length);
        Assert.Equal(expectedIndexCount, indices.Length);
    }

    [Fact(DisplayName = "Get produces sphere geometry for id 3")]
    public void Get_ProducesSphereGeometry_ForId3()
    {
        var (vertices, indices) = PrimitiveGeometryCache.Get(3);

        Assert.NotEmpty(vertices);
        Assert.NotEmpty(indices);
    }

    [Fact(DisplayName = "Get throws for an unsupported primitive id instead of silently substituting a cube")]
    public void Get_Throws_ForUnsupportedId()
    {
        Assert.Throws<NotSupportedException>(() => PrimitiveGeometryCache.Get(999));
    }
}
