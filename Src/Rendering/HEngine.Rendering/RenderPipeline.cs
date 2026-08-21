using HEngine.Core.Components.Rendering;
using HEngine.Core.Configuration;
using HEngine.Core.Managers;
using HEngine.Core.Mathematics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.Logging;
using HEngine.Rendering.PostProcessing;
using HEngine.Rendering.Systems;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering;

public class RenderPipeline : IRenderPipeline {
    private readonly LightingSystem _lightingSystem;
    private readonly ILogger<RenderPipeline> _logger;
    private readonly IRenderingSystem _renderingSystem;
    private readonly IRenderManager _renderManager;
    private readonly IRenderContextProvider _renderContextProvider;
    private readonly ShadowRenderingSystem _shadowRenderingSystem;
    private readonly ShadowSettings _shadowSettings;
    private readonly WorldManager _world;
    private readonly PostProcessStack _postProcessStack;
    private readonly IPostProcessCommandContext _postProcessCommandContext;

    public PostProcessStack PostProcessStack => _postProcessStack;

    public RenderPipeline(
        IRenderManager renderManager,
        IRenderContextProvider renderContextProvider,
        IRenderingSystem renderingSystem,
        WorldManager world,
        LightingSystem lightingSystem,
        ShadowRenderingSystem shadowRenderingSystem,
        ShadowSettings shadowSettings,
        PostProcessStack postProcessStack,
        IPostProcessCommandContext postProcessCommandContext,
        ILogger<RenderPipeline> logger)
    {
        _renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));
        _renderContextProvider = renderContextProvider ?? throw new ArgumentNullException(nameof(renderContextProvider));
        _renderingSystem = renderingSystem ?? throw new ArgumentNullException(nameof(renderingSystem));
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _lightingSystem = lightingSystem ?? throw new ArgumentNullException(nameof(lightingSystem));
        _shadowRenderingSystem = shadowRenderingSystem ?? throw new ArgumentNullException(nameof(shadowRenderingSystem));
        _shadowSettings = shadowSettings ?? throw new ArgumentNullException(nameof(shadowSettings));
        _postProcessStack = postProcessStack ?? throw new ArgumentNullException(nameof(postProcessStack));
        _postProcessCommandContext = postProcessCommandContext ?? throw new ArgumentNullException(nameof(postProcessCommandContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_shadowSettings.Enabled && !_shadowRenderingSystem.HasShadowRenderer)
        {
            throw new InvalidOperationException(
                "ShadowSettings.Enabled is true, but ShadowRenderingSystem has no IShadowRenderer wired (tracks #19). " +
                "Either wire a production IShadowRenderer via ShadowRenderingSystem.SetShadowRenderer before constructing RenderPipeline, or set ShadowSettings.Enabled = false.");
        }
    }

    public void RenderFrame()
    {
        if (!_renderManager.CanRender)
        {
            return;
        }

        if (!_renderContextProvider.TryGetRenderContext(out var context))
        {
            _logger.LogWarning(RenderLogEvents.PipelineContextNullWarn, "RenderContext unavailable; skipping frame");
            return;
        }

        try
        {
            _logger.LogDebug(RenderLogEvents.PipelineStart, "RenderFrame start");
            _renderManager.BeginRender();

            try
            {
                Camera activeCamera = default;
                var hasCamera = false;

                var qb = _world.QueryBuilder.With<Camera>();
                if (qb.TryGetFirst(out _, out var cam))
                {
                    context.ViewMatrix = cam.GetViewMatrix();
                    context.ProjectionMatrix = cam.GetProjectionMatrix();
                    activeCamera = cam;
                    hasCamera = true;
                }
                else if (_renderManager.TryGetActiveCamera(out var camera))
                {
                    context.ViewMatrix = camera.ViewMatrix;
                    context.ProjectionMatrix = camera.ProjectionMatrix;
                }

                context.Renderer.SetViewMatrix(context.ViewMatrix);
                context.Renderer.SetProjectionMatrix(context.ProjectionMatrix);

                _lightingSystem.Update(0f);
                var lights = _lightingSystem.LastLights;
                context.Renderer.SetLights(lights);

                if (_shadowSettings.Enabled && hasCamera && _shadowRenderingSystem.HasShadowRenderer)
                {
                    ExecuteShadowPass(activeCamera, lights);
                }

                _renderingSystem.Render(context);

                ExecutePostProcessPass();
            }
            finally
            {
                _renderManager.EndRender();
            }

            _logger.LogDebug(RenderLogEvents.PipelineEnd, "RenderFrame end");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Critical error in render pipeline");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in render pipeline");
            throw new InvalidOperationException("Critical error in render pipeline", ex);
        }
    }

    private void ExecuteShadowPass(in Camera camera, ReadOnlySpan<LightData> lights)
    {
        for (var i = 0; i < lights.Length; i++)
        {
            if (lights[i].Type != LightType.Directional)
            {
                continue;
            }

            var splits = ShadowUtils.ComputePSSMSplits(
                camera.NearPlane,
                camera.FarPlane,
                _shadowSettings.CascadeCount,
                _shadowSettings.LambdaSplit
            );

            _shadowRenderingSystem.RenderShadows(
                camera,
                lights[i].Direction,
                splits,
                _shadowSettings.Resolution
            );

            break;
        }
    }

    private void ExecutePostProcessPass()
    {
        if (_postProcessStack.EnabledEffectCount == 0)
            return;

        _postProcessCommandContext.PrepareSceneSource();
        _postProcessStack.Execute(_postProcessCommandContext);
        _postProcessCommandContext.ResolveToBackBuffer();
    }
}