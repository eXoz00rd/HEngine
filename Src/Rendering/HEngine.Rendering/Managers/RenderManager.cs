using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using Microsoft.Extensions.Logging;
using HEngine.Rendering.Logging;

namespace HEngine.Rendering.Managers;

public class RenderManager : IRenderManager
{
    private readonly ILogger<RenderManager> _logger;
    private readonly IRenderer _renderer;
    private bool _disposed;
    private IRenderContext? _renderContext;

    public RenderManager(IRenderer renderer, ILogger<RenderManager> logger)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
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
            _logger.LogInformation(RenderLogEvents.InitializeStart, "Initializing RenderManager with {Width}x{Height} '{Title}'", width, height, title);

            _renderer.Initialize(width, height, title);

            _renderContext = new SilkRenderContext(_renderer);

            // ===================== NAPRAWIONO =====================
            // Tworzymy ortograficzną macierz projekcji, która mapuje współrzędne
            // z przestrzeni ekranu (np. 0-1280) na znormalizowane współrzędne urządzenia (-1 do 1).
            _renderContext.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1.0f, 1.0f);
            // ======================================================

            IsInitialized = true;
            _logger.LogInformation(RenderLogEvents.InitializeSuccess, "RenderManager initialized successfully");
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

    public IRenderContext? GetRenderContext()
    {
        if (IsInitialized && !_disposed) return _renderContext;

        _logger.LogWarning("RenderManager not initialized or disposed - returning null RenderContext");
        return null;
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