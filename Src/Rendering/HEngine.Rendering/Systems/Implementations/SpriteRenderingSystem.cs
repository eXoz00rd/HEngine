using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.ECS.Queries;
using HEngine.Rendering.Components;
using HEngine.Rendering.Systems.Contracts;

namespace HEngine.Rendering.Systems.Implementations;

public class SpriteRenderingSystem : ISpriteRenderingSystem
{
    private bool _disposed;
    private bool _isInitialized;
    private QueryBuilder _queryBuilder = null!;

    public bool IsInitialized => _isInitialized && !_disposed;

    public void Initialize(WorldManager worldManager)
    {
        _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);
        _isInitialized = true;
    }

    public void Render(IRenderContext renderContext)
    {
        if (_disposed || !_isInitialized)
            return;

        var query = _queryBuilder.With<Transform2D, Sprite>();
        if (query.IsEmpty)
        {
            return;
        }

        foreach (var (entity, transform, sprite) in query)
        {
            renderContext.Renderer.DrawSprite(transform.Position, sprite.Size, sprite.Color);
        }

        renderContext.Renderer.FlushBatch();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}