using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Queries;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Components;
using HEngine.Rendering.Systems.Contracts;

namespace HEngine.Rendering.Systems;

public class MeshRenderingSystem : IMeshRenderingSystem
{
    private bool _disposed;
    private QueryBuilder _queryBuilder = null!;
    private WorldManager _world = null!;

    public void Initialize(WorldManager worldManager)
    {
        _world = worldManager;
        _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);
    }

    public void Render(IRenderContext context)
    {
        if (_disposed)
            return;

        var meshQuery = _queryBuilder.With<Transform, Mesh>();

        foreach (var (entity, transform, mesh) in meshQuery)
        {
            if (_world.HasComponent<Culled>(entity))
                continue;

            var transformMatrix = transform.GetWorldMatrix(_world);
            context.Renderer.DrawMesh(transformMatrix, [], []);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _queryBuilder = null!;
        _disposed = true;
    }
}