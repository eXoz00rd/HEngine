using HEngine.Core.Components.Rendering;
using HEngine.Core.Configuration;
using HEngine.Core.Managers;
using HEngine.Core.Mathematics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Data;
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
    private readonly ShadowRenderingSystem _shadowRenderingSystem;
    private readonly ShadowSettings _shadowSettings;
    private readonly WorldManager _world;
    private readonly PostProcessStack _postProcessStack;

    public PostProcessStack PostProcessStack => _postProcessStack;

    public RenderPipeline(
        IRenderManager renderManager,
        IRenderingSystem renderingSystem,
        WorldManager world,
        LightingSystem lightingSystem,
        ShadowRenderingSystem shadowRenderingSystem,
        ShadowSettings shadowSettings,
        PostProcessStack postProcessStack,
        ILogger<RenderPipeline> logger)
    {
        _renderManager = renderManager;
        _renderingSystem = renderingSystem;
        _world = world;
        _lightingSystem = lightingSystem;
        _shadowRenderingSystem = shadowRenderingSystem;
        _shadowSettings = shadowSettings;
        _postProcessStack = postProcessStack;
        _logger = logger;
    }

    public void RenderFrame()
    {
        if (!_renderManager.CanRender)
        {
            return;
        }

        if (!_renderManager.TryGetRenderContext(out var context))
        {
            _logger.LogWarning(RenderLogEvents.PipelineContextNullWarn, "RenderContext unavailable; skipping frame");
            return;
        }

        try
        {
            _logger.LogDebug(RenderLogEvents.PipelineStart, "RenderFrame start");
            _renderManager.BeginRender();

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

            if (_shadowSettings.Enabled && hasCamera && _shadowRenderingSystem.HasShadowRenderer)
            {
                ExecuteShadowPass(activeCamera);
            }

            _renderingSystem.Render(context);

            ExecutePostProcessPass(context);

            _renderManager.EndRender();
            _logger.LogDebug(RenderLogEvents.PipelineEnd, "RenderFrame end");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in render pipeline");
            throw new InvalidOperationException("Critical error in render pipeline", ex);
        }
    }

    private void ExecuteShadowPass(in Camera camera)
    {
        _lightingSystem.Update(0f);
        var lights = _lightingSystem.LastLights;

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

    private void ExecutePostProcessPass(IRenderContext context)
    {
        if (_postProcessStack.EnabledEffectCount == 0)
            return;

        _logger.LogDebug(RenderLogEvents.PipelineStart, "PostProcess pass: {Count} effects", _postProcessStack.EnabledEffectCount);

        var ppContext = new NullPostProcessCommandContext(context);
        _postProcessStack.Execute(ppContext);
    }
}