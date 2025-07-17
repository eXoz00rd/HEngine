using System.Numerics;
using HEngine.Core.Configuration;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.Systems;

namespace HEngine.Rendering.Managers;

public class RenderManager : IDisposable
{
    private readonly EngineConfiguration _config;
    private readonly IRenderer _renderer;
    private bool _disposed;
    private bool _initialized;

    public RenderManager(IRenderer renderer, EngineConfiguration config)
    {
        _renderer = renderer;
        _config = config;
        RenderContext = new SilkRenderContext(_renderer);
        ConfigureRenderContext();
    }

    public bool ShouldClose => _renderer.ShouldClose;
    public bool IsInitialized => _initialized && !_disposed;
    public bool CanRender => IsInitialized && !_renderer.ShouldClose;
    public SilkRenderContext RenderContext { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        Console.WriteLine("Disposing RenderManager");
        _initialized = false;
        _renderer?.Dispose();
        _disposed = true;
    }

    public void Initialize()
    {
        try
        {
            Console.WriteLine("Initializing RenderManager...");
            _renderer.Initialize(
                _config.Window.Width,
                _config.Window.Height,
                _config.Window.Title
            );

            if (_renderer is SilkDirectX12Renderer { IsInitialized: false })
                throw new InvalidOperationException("Renderer failed to initialize");

            _initialized = true;
            Console.WriteLine("RenderManager initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize RenderManager: {ex.Message}");
            _initialized = false;
            throw;
        }
    }

    public void UpdateInput()
    {
        if (!CanRender)
            return;
        _renderer.PollEvents();
    }

    public void BeginRender()
    {
        if (!CanRender)
            return;

        try
        {
            _renderer.BeginFrame();
            _renderer.Clear(RenderContext.ClearColor);
            _renderer.SetViewMatrix(RenderContext.ViewMatrix);
            _renderer.SetProjectionMatrix(RenderContext.ProjectionMatrix);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in BeginRender: {ex.Message}");
            throw;
        }
    }

    public void EndRender()
    {
        if (!CanRender)
            return;

        try
        {
            _renderer.EndFrame();
            _renderer.Present();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in EndRender: {ex.Message}");
            throw;
        }
    }

    private void ConfigureRenderContext()
    {
        RenderContext.ClearColor = _config.Rendering.ClearColor;

        // Dla renderowania 2D używaj macierzy ortogonalnej
        RenderContext.ViewMatrix = Matrix4x4.Identity;
        RenderContext.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            0, // left
            _config.Window.Width, // right
            _config.Window.Height, // bottom
            0, // top
            -1, // near
            1 // far
        );
    }
}