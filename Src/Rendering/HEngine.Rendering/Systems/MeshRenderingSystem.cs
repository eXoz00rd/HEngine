using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.ECS.Queries;
using HEngine.Rendering.Components;
using HEngine.Rendering.Data;
using HEngine.Rendering.Enums;
using HEngine.Rendering.Managers;
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
    private readonly MaterialManager? _materialManager;
    private readonly ITextureManager? _textureManager;
    private int _frameCount;

    public MeshRenderingSystem(ILogger<MeshRenderingSystem>? logger = null,
        MaterialManager? materialManager = null, ITextureManager? textureManager = null)
    {
        _logger = logger;
        _materialManager = materialManager;
        _textureManager = textureManager;
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
            var material = ResolveMaterial(entity);
            context.Renderer.DrawMesh(transformMatrix, flat, indices, material);
        }

        _frameCount++;
        if (_frameCount % 60 == 0 && _logger != null && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Frame {Frame}: Rendered {Count} meshes", _frameCount, meshCount);
        }
    }

    private MaterialData? ResolveMaterial(Entity entity)
    {
        if (_materialManager is null || _textureManager is null)
            return null;

        if (!_world.HasComponent<Renderable>(entity))
            return null;

        var renderable = _world.GetComponent<Renderable>(entity);
        if (renderable.MaterialId == 0)
            return null;

        if (!_materialManager.TryGetById(renderable.MaterialId, out var name, out var material) ||
            name is null || material is null)
            return null;

        var diffuseHandle = _materialManager.GetTextureHandleForSlot(name, TextureSlot.DiffuseMap, _textureManager);
        var normalHandle = _materialManager.GetTextureHandleForSlot(name, TextureSlot.NormalMap, _textureManager);
        var metallicRoughnessHandle = _materialManager.GetTextureHandleForSlot(name, TextureSlot.MetallicRoughnessMap, _textureManager);
        var emissiveHandle = _materialManager.GetTextureHandleForSlot(name, TextureSlot.EmissiveMap, _textureManager);
        var aoHandle = _materialManager.GetTextureHandleForSlot(name, TextureSlot.AOMap, _textureManager);

        return new MaterialData
        {
            DiffuseColor = material.DiffuseColor,
            Metallic = material.Metallic,
            Roughness = material.Roughness,
            AO = material.GetFloat("_AO", 1.0f),
            EmissiveColor = material.GetVector4("_EmissiveColor"),
            EmissiveIntensity = material.GetFloat("_EmissiveIntensity", 0.0f),
            DiffuseTextureHandle = diffuseHandle,
            NormalTextureHandle = normalHandle,
            MetallicRoughnessTextureHandle = metallicRoughnessHandle,
            EmissiveTextureHandle = emissiveHandle,
            AOTextureHandle = aoHandle
        };
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