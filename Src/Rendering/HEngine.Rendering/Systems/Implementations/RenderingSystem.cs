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

    public void Render()
    {
        if (_disposed || _renderContext == null) return;

        try
        {
            _renderContext.Renderer.SetViewMatrix(_renderContext.ViewMatrix);
            _renderContext.Renderer.SetProjectionMatrix(_renderContext.ProjectionMatrix);

            _spriteSystem.Render(_renderContext);
            _meshSystem.Render(_renderContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RenderingSystem.Render: {ex.Message}");
            throw;
        }
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
        _spriteSystem.Initialize(worldManager);
        _meshSystem.Initialize(worldManager);
        _isInitialized = true;
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
        _renderContext = renderContext;
        Console.WriteLine("RenderContext set successfully");
    }
}