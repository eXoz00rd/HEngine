using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Systems.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HEngine.Rendering.Systems.Implementations;

public class RenderingSystem : IRenderingSystem
{
    private readonly IMeshRenderingSystem _meshSystem;
    private readonly ISpriteRenderingSystem _spriteSystem;
    private readonly ILogger<RenderingSystem> _logger;
    private bool _disposed;
    private bool _isInitialized;
    private IRenderContext _renderContext = null!;

    public RenderingSystem(ISpriteRenderingSystem spriteSystem, IMeshRenderingSystem meshSystem, ILogger<RenderingSystem> logger)
    {
        _spriteSystem = spriteSystem;
        _meshSystem = meshSystem;
        _logger = logger ?? NullLogger<RenderingSystem>.Instance;
    }

    public RenderingSystem(ISpriteRenderingSystem spriteSystem, IMeshRenderingSystem meshSystem)
    {
        _spriteSystem = spriteSystem;
        _meshSystem = meshSystem;
        _logger = NullLogger<RenderingSystem>.Instance;
    }

    public RenderingSystem()
    {
        _spriteSystem = new SpriteRenderingSystem();
        _meshSystem = new MeshRenderingSystem();
        _logger = NullLogger<RenderingSystem>.Instance;
    }

    public bool IsInitialized => _isInitialized && !_disposed;

    public void Update(float deltaTime)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RenderingSystem));
        if (!_isInitialized)
            throw new InvalidOperationException("RenderingSystem must be initialized before calling Update.");
    }

    public void Render(IRenderContext context)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RenderingSystem));
        if (!_isInitialized)
            throw new InvalidOperationException("RenderingSystem must be initialized before calling Render.");
        ArgumentNullException.ThrowIfNull(context);

        _spriteSystem.Render(context);
        _meshSystem.Render(context);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _spriteSystem.Dispose();
        _meshSystem.Dispose();
        _disposed = true;
    }

    public void Initialize(WorldManager worldManager)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RenderingSystem));
        ArgumentNullException.ThrowIfNull(worldManager);
        if (_isInitialized) return;

        _spriteSystem.Initialize(worldManager);
        _meshSystem.Initialize(worldManager);
        _isInitialized = true;
    }

    [Obsolete("Use Render(IRenderContext) instead. This parameterless method is deprecated and will be removed.")]
    internal void Render()
    {
        if (_disposed || _renderContext == null) return;

        try
        {
            Render(_renderContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RenderingSystem.Render");
            throw;
        }
    }

    public void Initialize()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RenderingSystem));

        if (_isInitialized)
            return;

        _isInitialized = true;
    }

    public void SetRenderContext(IRenderContext renderContext)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RenderingSystem));
        ArgumentNullException.ThrowIfNull(renderContext);
        _renderContext = renderContext;
    }
}