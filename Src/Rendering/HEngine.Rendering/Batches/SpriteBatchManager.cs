using HEngine.Rendering.Contracts;
using HEngine.Rendering.Renderers;
using System.Numerics;

namespace HEngine.Rendering.Batches;

public struct SpriteData {
    public Vector2 Position;
    public Vector2 Size;
    public Vector4 Color;
}

public class SpriteBatch : IRenderBatch<SpriteData> {
    private readonly List<SpriteData> _sprites = new();
    private bool _disposed;
    private ISpriteRenderer _spriteRenderer = null!;

    public int Count => _sprites.Count;

    public void Add(SpriteData sprite)
    {
        if (_disposed)
            return;
        _sprites.Add(sprite);
    }

    public void Clear()
    {
        if (_disposed)
            return;
        _sprites.Clear();
    }

    public void Render(IRenderCommandList commandList)
    {
        if (_disposed || _sprites.Count == 0)
            return;

        foreach (var sprite in _sprites)
            _spriteRenderer.DrawSprite(sprite.Position, sprite.Size, sprite.Color);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _sprites.Clear();
        _spriteRenderer?.Dispose();
        _disposed = true;
    }

    public void Initialize(ISpriteRenderer spriteRenderer)
        => _spriteRenderer = spriteRenderer;
}