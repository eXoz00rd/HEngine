using System.Numerics;
using HEngine.Core.Configuration;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Factories;
using HEngine.Rendering.Logging;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.Managers;

public class RenderManager : IRenderManager
{
    private readonly IRenderContextFactory _contextFactory;
    private readonly ILogger<RenderManager> _logger;
    private readonly IRenderer _renderer;
    private readonly EngineConfiguration _config;
    private bool _disposed;
    private IRenderContext? _renderContext;
    private ICamera? _activeCamera;

    public RenderManager(IRenderer renderer, IRenderContextFactory contextFactory, EngineConfiguration config, ILogger<RenderManager> logger)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool ShouldClose => _renderer?.ShouldClose ?? false;
    public bool CanRender => IsInitialized && !_disposed && _renderer != null;
    public bool IsInitialized { get; private set; }

    public void Initialize(int width, int height, string title)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RenderManager));

        if (IsInitialized)
        {
            _logger.LogWarning("RenderManager already initialized");
            return;
        }

        try
        {
            _logger.LogInformation(RenderLogEvents.InitializeStart,
                "Initializing RenderManager with {Width}x{Height} '{Title}'", width, height, title);

            _renderer.Initialize(width, height, title);

            _renderContext = _contextFactory.Create();

            // Configure initial render context state based on configuration
            var w = width <= 0 ? 1 : width;
            var h = height <= 0 ? 1 : height;
            var aspect = h == 0 ? 1.0f : (float)w / h;

            var renderCfg = _config.Rendering;
            _renderContext.ClearColor = renderCfg.ClearColor;
            _renderContext.ViewMatrix = Matrix4x4.Identity;

            Matrix4x4 projection;
            if (renderCfg.ProjectionMode == ProjectionMode.Orthographic)
            {
                // Screen-space orthographic with top-left origin and Y down
                projection = Matrix4x4.CreateOrthographicOffCenter(0, w, h, 0, renderCfg.NearPlane, renderCfg.FarPlane);
            }
            else
            {
                var fov = Math.Clamp(renderCfg.FieldOfView, 0.01f, MathF.PI - 0.01f);
                var nearP = Math.Max(0.0001f, renderCfg.NearPlane);
                var farP = Math.Max(nearP + 0.001f, renderCfg.FarPlane);
                projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, nearP, farP);
            }

            _renderContext.ProjectionMatrix = projection;

            IsInitialized = true;
            _logger.LogInformation(RenderLogEvents.InitializeSuccess, "RenderManager initialized successfully with Aspect={Aspect} Mode={Mode}", aspect, renderCfg.ProjectionMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(RenderLogEvents.InitializeFailure, ex, "Failed to initialize RenderManager");
            IsInitialized = false;
            throw;
        }
    }

    public void UpdateInput()
    {
        if (!CanRender) return;

        try
        {
            _logger.LogDebug(RenderLogEvents.PollEvents, "Polling events");
            _renderer.PollEvents();
        }
        catch (Exception ex)
        {
            _logger.LogError(RenderLogEvents.PollEvents, ex, "Error during input update");
        }
    }

    public void BeginRender()
    {
        if (!CanRender) return;

        try
        {
            _logger.LogDebug(RenderLogEvents.BeginRender, "BeginRender");

            _logger.LogDebug(RenderLogEvents.BeginFrame, "BeginFrame");
            _renderer.BeginFrame();

            if (_renderContext != null)
            {
                var color = _renderContext.ClearColor;
                _logger.LogDebug(RenderLogEvents.Clear, "Clear with color {Color}", color);
                _renderer.Clear(color);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(RenderLogEvents.BeginRender, ex, "Error during begin render");
        }
    }

    public void EndRender()
    {
        if (!CanRender) return;

        try
        {
            _logger.LogDebug(RenderLogEvents.EndRender, "EndRender");

            _logger.LogDebug(RenderLogEvents.EndFrame, "EndFrame");
            _renderer.EndFrame();

            _logger.LogDebug(RenderLogEvents.Present, "Present");
            _renderer.Present();
        }
        catch (Exception ex)
        {
            _logger.LogError(RenderLogEvents.EndRender, ex, "Error during end render");
        }
    }

    public void Clear(Vector4 clearColor)
    {
        if (!CanRender) return;

        try
        {
            _logger.LogDebug(RenderLogEvents.Clear, "Clear with color {Color}", clearColor);
            _renderer.Clear(clearColor);
        }
        catch (Exception ex)
        {
            _logger.LogError(RenderLogEvents.Clear, ex, "Error during clear");
        }
    }

    public void Present()
    {
        if (!CanRender) return;

        try
        {
            _logger.LogDebug(RenderLogEvents.Present, "Present");
            _renderer.Present();
        }
        catch (Exception ex)
        {
            _logger.LogError(RenderLogEvents.Present, ex, "Error during present");
        }
    }

    public IRenderContext GetRenderContext()
    {
        if (IsInitialized && !_disposed && _renderContext != null) return _renderContext;

        _logger.LogWarning("RenderManager not initialized or disposed - GetRenderContext is unavailable");
        throw new InvalidOperationException("RenderContext is not available. Ensure RenderManager is initialized and not disposed.");
    }

    public bool TryGetRenderContext(out IRenderContext context)
    {
        if (IsInitialized && !_disposed && _renderContext != null)
        {
            context = _renderContext;
            return true;
        }

        context = null!;
        return false;
    }

    public void SetActiveCamera(ICamera camera)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RenderManager));
        ArgumentNullException.ThrowIfNull(camera);
        _activeCamera = camera;
    }

    public bool TryGetActiveCamera(out ICamera camera)
    {
        if (IsInitialized && !_disposed && _activeCamera != null)
        {
            camera = _activeCamera;
            return true;
        }

        camera = null!;
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation(RenderLogEvents.Dispose, "Disposing RenderManager");

        _renderContext = null;
        _renderer?.Dispose();
        IsInitialized = false;
        _disposed = true;
    }
}