// RenderPipeline.cs
using HEngine.Core.Rendering.Contracts;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering;

public class RenderPipeline : IRenderPipeline
{
    private readonly IRenderManager _renderManager;
    private readonly IRenderingSystem _renderingSystem;
    private readonly ILogger<RenderPipeline> _logger;

    public RenderPipeline(IRenderManager renderManager, IRenderingSystem renderingSystem, ILogger<RenderPipeline> logger)
    {
        _renderManager = renderManager;
        _renderingSystem = renderingSystem;
        _logger = logger;
    }

    public void RenderFrame()
    {
        if (!_renderManager.CanRender)
        {
            return;
        }

        try
        {
            // Tutaj w przyszłości można dodać logikę synchronizacji z GPU,
            // np. oczekiwanie na zakończenie poprzedniej klatki.
            
            _renderManager.BeginRender();
            _renderingSystem.Render();
            _renderManager.EndRender();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A critical error occurred in the render pipeline.");
            // Można tu dodać logikę próbującą odzyskać urządzenie graficzne.
            throw;
        }
    }
}
