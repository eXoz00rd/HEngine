using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.Batches;

public struct SpriteData
{
    public Vector2 Position;
    public Vector2 Size;
    public Vector4 Color;
}

public class SpriteBatch : IRenderBatch<SpriteData>
{
    private readonly ILogger<SpriteBatch> _logger;
    private readonly List<SpriteData> _sprites;
    private bool _disposed;
    private bool _isInitialized;
    private ISpriteRenderer _spriteRenderer = null!;

    public SpriteBatch(ILogger<SpriteBatch> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sprites = new List<SpriteData>(1024);
    }

    public int Count => _sprites.Count;

    public void Add(SpriteData sprite)
    {
        if (_disposed || !_isInitialized)
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
        if (_disposed || !_isInitialized || _sprites.Count == 0)
            return;

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Rendering {Count} sprites", _sprites.Count);
            
            for (var i = 0; i < _sprites.Count; i++)
            {
                var sprite = _sprites[i];
                _spriteRenderer.DrawSprite(sprite.Position, sprite.Size, sprite.Color);
            }
            
            _spriteRenderer.FlushBatch();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render sprite batch");
            throw;
        }
    }

    public void Initialize(ISpriteRenderer spriteRenderer)
    {
        ArgumentNullException.ThrowIfNull(spriteRenderer);

        if (_disposed)
        {
            _logger.LogWarning("Cannot initialize disposed batch");
            return;
        }

        _spriteRenderer = spriteRenderer;
        _isInitialized = true;

        _logger.LogInformation("SpriteBatch initialized successfully");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing SpriteBatch with {Count} sprites", _sprites.Count);

        _sprites.Clear();
        _spriteRenderer = null!;
        _isInitialized = false;
        _disposed = true;
    }
}