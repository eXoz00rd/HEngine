using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Systems.Contracts;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.Systems.Implementations;

public class RenderingSystem : IRenderingSystem
{
    private readonly IMeshRenderingSystem _meshSystem;
    private readonly ISpriteRenderingSystem _spriteSystem;
    private readonly ILogger<RenderingSystem> _logger;
    private bool _disposed;
    private bool _isInitialized;

    public RenderingSystem(ISpriteRenderingSystem spriteSystem, IMeshRenderingSystem meshSystem,
        ILogger<RenderingSystem> logger)
    {
        _spriteSystem = spriteSystem;
        _meshSystem = meshSystem;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsInitialized => _isInitialized && !_disposed;

    public void Update(float deltaTime)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RenderingSystem));
        }

        if (!_isInitialized)
        {
            throw new InvalidOperationException("RenderingSystem must be initialized before calling Update.");
        }
    }

    public void Render(IRenderContext context)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RenderingSystem));
        }

        if (!_isInitialized)
        {
            throw new InvalidOperationException("RenderingSystem must be initialized before calling Render.");
        }

        ArgumentNullException.ThrowIfNull(context);

        _spriteSystem.Render(context);
        _meshSystem.Render(context);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _spriteSystem.Dispose();
        _meshSystem.Dispose();
        _disposed = true;
    }

    public void Initialize(WorldManager worldManager)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RenderingSystem));
        }

        ArgumentNullException.ThrowIfNull(worldManager);
        if (_isInitialized)
        {
            return;
        }

        _spriteSystem.Initialize(worldManager);
        _meshSystem.Initialize(worldManager);
        _isInitialized = true;
        _logger.LogDebug("RenderingSystem initialized");
    }

    public void Initialize()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RenderingSystem));
        }

        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
    }

    public void SetRenderContext(IRenderContext renderContext)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RenderingSystem));
        }

        ArgumentNullException.ThrowIfNull(renderContext);
    }
}