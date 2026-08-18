using System.Collections.Concurrent;
using HEngine.Core.Rendering.Data;

namespace HEngine.Rendering.Data;

public static class PrimitiveGeometryCache
{
    private const int SphereSegments = 24;
    private const float SphereRadius = 0.5f;

    private static readonly ConcurrentDictionary<uint, (Vertex3D[] Vertices, uint[] Indices)> Cache = new();

    public static (Vertex3D[] Vertices, uint[] Indices) Get(uint vertexArrayId)
    {
        return Cache.GetOrAdd(vertexArrayId, Generate);
    }

    private static (Vertex3D[] Vertices, uint[] Indices) Generate(uint vertexArrayId)
    {
        return vertexArrayId switch
        {
            1 => MeshPrimitives.CreateCube(1.0f),
            2 => MeshPrimitives.CreatePlane(1.0f, 1.0f),
            3 => MeshPrimitives.CreateSphere(SphereRadius, SphereSegments),
            _ => throw new NotSupportedException(
                $"Unsupported primitive VertexArrayId '{vertexArrayId}'. Supported ids: 1 (cube), 2 (plane), 3 (sphere).")
        };
    }
}
