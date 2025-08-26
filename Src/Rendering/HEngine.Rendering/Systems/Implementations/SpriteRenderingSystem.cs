using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Queries;
using HEngine.Core.Rendering.Contracts;
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
        // Use QueryBuilder so queries reflect entities created after initialization as well
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
            Console.WriteLine("SpriteRenderingSystem: Query is empty, skipping render.");
            return;
        }

        Console.WriteLine($"SpriteRenderingSystem: Rendering {query.Count} sprites.");

        foreach (var (entity, transform, sprite) in query)
        {
            Console.WriteLine($"Drawing sprite at {transform.Position}");
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