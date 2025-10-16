using HEngine.Core.Rendering.Contracts;
using Microsoft.Extensions.Logging;
using HEngine.Rendering.Logging;

namespace HEngine.Rendering;

public class RenderPipeline : IRenderPipeline
{
    private readonly ILogger<RenderPipeline> _logger;
    private readonly IRenderingSystem _renderingSystem;
    private readonly IRenderManager _renderManager;

    public RenderPipeline(IRenderManager renderManager, IRenderingSystem renderingSystem,
        ILogger<RenderPipeline> logger)
    {
        _renderManager = renderManager;
        _renderingSystem = renderingSystem;
        _logger = logger;
    }

    public void RenderFrame()
    {
        if (!_renderManager.CanRender) return;

        if (!_renderManager.TryGetRenderContext(out var context))
        {
            _logger.LogWarning(RenderLogEvents.PipelineContextNullWarn, "RenderContext is not available, skipping frame.");
            return;
        }

        try
        {
            _logger.LogDebug(RenderLogEvents.PipelineStart, "RenderFrame start");
            // Step 1: Begin frame (clears, etc.)
            _renderManager.BeginRender();

            // Step 2: If an active camera is available, push its matrices to the context
            if (_renderManager.TryGetActiveCamera(out var camera))
            {
                context.ViewMatrix = camera.ViewMatrix;
                context.ProjectionMatrix = camera.ProjectionMatrix;
            }

            // Step 3: Apply context matrices to renderer
            context.Renderer.SetViewMatrix(context.ViewMatrix);
            context.Renderer.SetProjectionMatrix(context.ProjectionMatrix);

            // Step 4: Perform all drawing operations via the rendering system
            _renderingSystem.Render(context);

            // Step 5: End frame and present
            _renderManager.EndRender();
            _logger.LogDebug(RenderLogEvents.PipelineEnd, "RenderFrame end");
        }
        catch (Exception ex)
        {
            _logger.LogError(RenderLogEvents.PipelineError, ex, "A critical error occurred in the render pipeline.");
            throw;
        }
    }
}