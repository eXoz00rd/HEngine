// Rendering/Systems/SpriteRenderingSystem.cs

using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Queries;
using HEngine.Rendering.Components;
using HEngine.Rendering.Contracts;

namespace HEngine.Rendering.Systems;

public interface ISpriteRenderingSystem : IDisposable {
    void Initialize(WorldManager worldManager);
    void Render(IRenderContext context);
}

public class SpriteRenderingSystem : ISpriteRenderingSystem {
    private bool _disposed;
    private QueryBuilder _queryBuilder = null!;

    public void Initialize(WorldManager worldManager)
        => _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);

    public void Render(IRenderContext context)
    {
        if (_disposed)
            return;

        var spriteQuery = _queryBuilder.With<Transform2D, Sprite>();

        foreach (var (entity, transform, sprite) in spriteQuery)
        {
            var position = transform.Position - sprite.Origin * sprite.Size;
            Console.WriteLine($"Drawing sprite at ({position.X}, {position.Y}) with size ({sprite.Size.X}, {sprite.Size.Y})");
            context.Renderer.DrawSprite(position, sprite.Size, sprite.Color);
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

public interface IMeshRenderingSystem : IDisposable {
    void Initialize(WorldManager worldManager);
    void Render(IRenderContext context);
}

public class MeshRenderingSystem : IMeshRenderingSystem {
    private bool _disposed;
    private QueryBuilder _queryBuilder = null!;

    public void Initialize(WorldManager worldManager)
        => _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);

    public void Render(IRenderContext context)
    {
        if (_disposed)
            return;

        var meshQuery = _queryBuilder.With<Transform, Mesh>();

        foreach (var (entity, transform, mesh) in meshQuery)
        {
            var transformMatrix = transform.ToMatrix();
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