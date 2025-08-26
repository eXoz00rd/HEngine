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
    private QueryBuilder _queryBuilder = null!;

    public void Initialize(WorldManager worldManager)
    {
        _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);
    }

    public void Render(IRenderContext context)
    {
        if (_disposed)
            return;

        var spriteQuery = _queryBuilder.With<Transform2D, Sprite>();

        foreach (var (entity, transform, sprite) in spriteQuery)
        {
            var position = transform.Position - sprite.Origin * sprite.Size;
            Console.WriteLine(
                $"Drawing sprite at ({position.X}, {position.Y}) with size ({sprite.Size.X}, {sprite.Size.Y})");
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

    public void Update(float deltaTime)
    {
        throw new NotImplementedException();
    }
}