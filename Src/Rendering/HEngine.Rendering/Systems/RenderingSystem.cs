using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Rendering.Contracts;

namespace HEngine.Rendering.Systems;

public class RenderingSystem : ISystem {
    private readonly IMeshRenderingSystem _meshSystem;
    private readonly ISpriteRenderingSystem _spriteSystem;
    private bool _disposed;
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

    public void Initialize(WorldManager worldManager)
    {
        _spriteSystem.Initialize(worldManager);
        _meshSystem.Initialize(worldManager);
    }

    public void Update(float deltaTime)
    {
        if (_disposed)
            return;

        // Sprawdź czy render context jest ustawiony
        if (_renderContext == null)
        {
            Console.WriteLine("RenderContext is null - skipping render");
            return;
        }

        try
        {
            // Nie wywołuj BeginFrame/EndFrame tutaj - to jest w RenderManager
            _renderContext.Renderer.SetViewMatrix(_renderContext.ViewMatrix);
            _renderContext.Renderer.SetProjectionMatrix(_renderContext.ProjectionMatrix);

            _spriteSystem.Render(_renderContext);
            _meshSystem.Render(_renderContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RenderingSystem.Update: {ex.Message}");
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

    public void SetRenderContext(IRenderContext renderContext)
    {
        _renderContext = renderContext;
        Console.WriteLine("RenderContext set successfully");
    }
}