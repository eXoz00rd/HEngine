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

        var context = _renderManager.GetRenderContext();
        if (context == null)
        {
            _logger.LogWarning(RenderLogEvents.PipelineContextNullWarn, "RenderContext is null, skipping frame.");
            return;
        }

        try
        {
            _logger.LogDebug(RenderLogEvents.PipelineStart, "RenderFrame start");
            // Krok 1: Rozpocznij klatkę (czyści tło itp.)
            _renderManager.BeginRender();

            // Krok 2: Ustaw stan renderera na podstawie kontekstu (macierze)
            // To jest kluczowy, jawny krok, którego brakowało.
            context.Renderer.SetViewMatrix(context.ViewMatrix);
            context.Renderer.SetProjectionMatrix(context.ProjectionMatrix);

            // Krok 3: Wykonaj wszystkie operacje rysowania, przekazując kontekst.
            // Będzie to wymagało małej zmiany w interfejsie IRenderingSystem.
            _renderingSystem.Render(context);

            // Krok 4: Zakończ klatkę i zaprezentuj wynik.
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