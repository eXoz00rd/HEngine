using System.Numerics;
using HEngine.Core.Configuration;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Managers;
using HEngine.Rendering.Renderers;

namespace HEngine.Rendering.Tests.Renderers;

file sealed class UninitializedGraphicsDevice : IGraphicsDevice
{
    public bool IsInitialized => false;
    public bool ShouldClose => false;

    public void Initialize(int width, int height, string title)
    {
    }

    public void BeginFrame()
    {
    }

    public void EndFrame()
    {
    }

    public void Clear(Vector4 clearColor)
    {
    }

    public void Present()
    {
    }

    public ICommandQueue GetCommandQueue()
    {
        throw new InvalidOperationException("Device not initialized");
    }

    public void Dispose()
    {
    }
}

public class DirectX12ShadowRendererTests
{
    private static DirectX12ShadowRenderer CreateRenderer()
    {
        var device = new UninitializedGraphicsDevice();
        var shadowMapManager = new ShadowMapManager();
        var shaderFileLoader = new ShaderFileLoader(AppDomain.CurrentDomain.BaseDirectory);
        var pipelineStateManager = new ShadowPipelineStateManager(shaderFileLoader);
        var settings = new ShadowSettings();

        return new DirectX12ShadowRenderer(device, shadowMapManager, pipelineStateManager, settings);
    }

    [Fact(DisplayName = "BeginShadowPass throws when called before the graphics device is initialized")]
    public void BeginShadowPass_Throws_When_Device_Not_Initialized()
    {
        using var renderer = CreateRenderer();

        Assert.Throws<InvalidOperationException>(
            () => renderer.BeginShadowPass(0, Matrix4x4.Identity, 1024));
    }

    [Fact(DisplayName = "RenderDepthOnlyMesh is a no-op when no shadow pass has been started")]
    public void RenderDepthOnlyMesh_Does_Nothing_Before_BeginShadowPass()
    {
        using var renderer = CreateRenderer();

        var vertices = new float[12];
        var indices = new uint[] { 0, 1, 2 };

        renderer.RenderDepthOnlyMesh(Matrix4x4.Identity, vertices, indices);
    }

    [Fact(DisplayName = "BindShadowResources is a no-op when the shadow renderer's GPU resources were never initialized")]
    public void BindShadowResources_Does_Nothing_Before_GpuResourcesInitialized()
    {
        using var renderer = CreateRenderer();

        var lightVPs = new[] { Matrix4x4.Identity };
        var splits = new float[] { 10f };

        renderer.BindShadowResources(lightVPs, splits);
    }

    [Fact(DisplayName = "BindShadowResources leaves ShadowMapManager.HasShadowData false when GPU resources were never initialized")]
    public void BindShadowResources_Does_Not_Populate_ShadowMapManager_Before_GpuResourcesInitialized()
    {
        var device = new UninitializedGraphicsDevice();
        var shadowMapManager = new ShadowMapManager();
        var shaderFileLoader = new ShaderFileLoader(AppDomain.CurrentDomain.BaseDirectory);
        var pipelineStateManager = new ShadowPipelineStateManager(shaderFileLoader);
        var settings = new ShadowSettings();

        using var renderer = new DirectX12ShadowRenderer(device, shadowMapManager, pipelineStateManager, settings);

        renderer.BindShadowResources([Matrix4x4.Identity], [10f]);

        Assert.False(shadowMapManager.HasShadowData);
    }
}
