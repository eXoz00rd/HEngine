using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Queries;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Components;
using HEngine.Rendering.Data;
using HEngine.Rendering.Systems.Contracts;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.Systems;

public class MeshRenderingSystem : IMeshRenderingSystem
{
    private bool _disposed;
    private bool _isInitialized;
    private QueryBuilder _queryBuilder = null!;
    private WorldManager _world = null!;
    private readonly ILogger<MeshRenderingSystem>? _logger;
    private int _frameCount;

    public MeshRenderingSystem(ILogger<MeshRenderingSystem>? logger = null)
    {
        _logger = logger;
    }

    public void Initialize(WorldManager worldManager)
    {
        _world = worldManager;
        _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);
        _isInitialized = true;
    }

    public void Render(IRenderContext context)
    {
        if (_disposed)
            return;

        if (!_isInitialized)
        {
            throw new InvalidOperationException("MeshRenderingSystem must be initialized before calling Render.");
        }

        var meshQuery = _queryBuilder.With<Transform, Mesh>();
        int meshCount = 0;

        foreach (var (entity, transform, mesh) in meshQuery)
        {
            if (_world.HasComponent<Culled>(entity))
                continue;

            meshCount++;

            var transformMatrix = transform.GetWorldMatrix(_world);

            var (vertices, indices) = PrimitiveGeometryCache.Get(mesh.VertexArrayId);

            var flat = Flatten(vertices, mesh.Color);
            context.Renderer.DrawMesh(transformMatrix, flat, indices);
        }

        _frameCount++;
        if (_frameCount % 60 == 0 && _logger != null && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Frame {Frame}: Rendered {Count} meshes", _frameCount, meshCount);
        }
    }

    private static float[] Flatten(ReadOnlySpan<Vertex3D> vertices, Vector4 color)
    {
        var result = new float[vertices.Length * 12];
        var o = 0;
        for (var i = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];
            result[o++] = v.Position.X;
            result[o++] = v.Position.Y;
            result[o++] = v.Position.Z;
            result[o++] = v.Normal.X;
            result[o++] = v.Normal.Y;
            result[o++] = v.Normal.Z;
            result[o++] = v.TexCoord.X;
            result[o++] = v.TexCoord.Y;
            result[o++] = color.X;
            result[o++] = color.Y;
            result[o++] = color.Z;
            result[o++] = color.W;
        }
        return result;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _queryBuilder = null!;
        _disposed = true;
    }
}