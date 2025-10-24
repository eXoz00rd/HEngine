using HEngine.Core.Components.Rendering;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using Microsoft.Extensions.Logging;
using HEngine.Rendering.Logging;

namespace HEngine.Rendering;

public class RenderPipeline : IRenderPipeline
{
    private readonly ILogger<RenderPipeline> _logger;
    private readonly IRenderingSystem _renderingSystem;
    private readonly IRenderManager _renderManager;
    private readonly WorldManager _world;

    public RenderPipeline(IRenderManager renderManager, IRenderingSystem renderingSystem,
        WorldManager world, ILogger<RenderPipeline> logger)
    {
        _renderManager = renderManager;
        _renderingSystem = renderingSystem;
        _world = world;
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
            _renderManager.BeginRender();
            
            var qb = _world.QueryBuilder.With<Camera>();
            if (qb.TryGetFirst(out var _, out var cam))
            {
                context.ViewMatrix = cam.GetViewMatrix();
                context.ProjectionMatrix = cam.GetProjectionMatrix();
            }
            else if (_renderManager.TryGetActiveCamera(out var camera))
            {
                context.ViewMatrix = camera.ViewMatrix;
                context.ProjectionMatrix = camera.ProjectionMatrix;
            }
            
            context.Renderer.SetViewMatrix(context.ViewMatrix);
            context.Renderer.SetProjectionMatrix(context.ProjectionMatrix);
            
            _renderingSystem.Render(context);

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