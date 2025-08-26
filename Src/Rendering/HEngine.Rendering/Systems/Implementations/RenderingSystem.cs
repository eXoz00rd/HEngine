using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Systems.Contracts;

namespace HEngine.Rendering.Systems.Implementations;

public class RenderingSystem : IRenderingSystem
{
    private readonly IMeshRenderingSystem _meshSystem;
    private readonly ISpriteRenderingSystem _spriteSystem;
    private bool _disposed;
    private bool _isInitialized;
    private IRenderContext _renderContext = null!;

    public RenderingSystem(ISpriteRenderingSystem spriteSystem, IMeshRenderingSystem meshSystem)
    {
        _spriteSystem = spriteSystem;
        _meshSystem = meshSystem;
    }

    public RenderingSystem()
    {
        _spriteSystem = new SpriteRenderingSystem();
        _meshSystem = new MeshRenderingSystem();
    }

    public bool IsInitialized => _isInitialized && !_disposed;

    public void Update(float deltaTime)
    {
        if (_disposed)
            return;
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
            // Delegate to the context-based path to keep a single authoritative render flow.
            Render(_renderContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RenderingSystem.Render: {ex.Message}");
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
        Console.WriteLine("RenderContext set successfully");
    }
}