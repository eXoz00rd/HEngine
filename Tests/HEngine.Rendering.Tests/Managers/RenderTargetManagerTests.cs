using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Managers;
using HEngine.Rendering.PostProcessing;

namespace HEngine.Rendering.Tests.Managers;

file sealed class UninitializedGraphicsDevice : IGraphicsDevice
{
    public bool IsInitialized => false;
    public bool ShouldClose => false;

    public void Initialize(int width, int height, string title) { }
    public void BeginFrame() { }
    public void EndFrame() { }
    public void Clear(Vector4 clearColor) { }
    public void Present() { }

    public ICommandQueue GetCommandQueue()
    {
        throw new InvalidOperationException("Device not initialized");
    }

    public void Dispose() { }
}

public class RenderTargetManagerTests
{
    [Fact(DisplayName = "RenderTargetManager constructor throws when DescriptorHeapManager is null")]
    public void RenderTargetManager_Constructor_ThrowsOnNullDescriptorHeapManager()
    {
        Assert.Throws<ArgumentNullException>(() => new RenderTargetManager(null!));
    }

    [Fact(DisplayName = "RenderTargetManager default values before Initialize")]
    public void RenderTargetManager_DefaultValues()
    {
        var manager = new RenderTargetManager(new DescriptorHeapManager());

        Assert.False(manager.IsInitialized);
        Assert.Equal(0, manager.Width);
        Assert.Equal(0, manager.Height);
    }

    [Fact(DisplayName = "RenderTargetManager Initialize with a null device handle throws ArgumentException")]
    public void RenderTargetManager_Initialize_WithNullDeviceHandle_Throws()
    {
        var manager = new RenderTargetManager(new DescriptorHeapManager());

        Assert.Throws<ArgumentException>(() => manager.Initialize(default, width: 1280, height: 720));
    }

    [Fact(DisplayName = "DirectX12PostProcessPipelineManager constructor throws when ShaderFileLoader is null")]
    public void DirectX12PostProcessPipelineManager_Constructor_ThrowsOnNullFileLoader()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectX12PostProcessPipelineManager(null!));
    }

    private static DirectX12PostProcessPipelineManager CreatePipelineManager()
    {
        var shaderFileLoader = new ShaderFileLoader(AppDomain.CurrentDomain.BaseDirectory);
        return new DirectX12PostProcessPipelineManager(shaderFileLoader);
    }

    [Fact(DisplayName = "DirectX12PostProcessCommandContext constructor throws when the pipeline manager is null")]
    public void DirectX12PostProcessCommandContext_Constructor_ThrowsOnNullPipelineManager()
    {
        var device = new UninitializedGraphicsDevice();
        var renderTargetManager = new RenderTargetManager(new DescriptorHeapManager());
        var descriptorHeapManager = new DescriptorHeapManager();

        Assert.Throws<ArgumentNullException>(() =>
            new DirectX12PostProcessCommandContext(null!, device, renderTargetManager, descriptorHeapManager));
    }

    [Fact(DisplayName = "DirectX12PostProcessCommandContext constructor throws when the graphics device is null")]
    public void DirectX12PostProcessCommandContext_Constructor_ThrowsOnNullGraphicsDevice()
    {
        var pipelineManager = CreatePipelineManager();
        var renderTargetManager = new RenderTargetManager(new DescriptorHeapManager());
        var descriptorHeapManager = new DescriptorHeapManager();

        Assert.Throws<ArgumentNullException>(() =>
            new DirectX12PostProcessCommandContext(pipelineManager, null!, renderTargetManager, descriptorHeapManager));
    }

    [Fact(DisplayName = "DirectX12PostProcessCommandContext constructor throws when RenderTargetManager is null")]
    public void DirectX12PostProcessCommandContext_Constructor_ThrowsOnNullRenderTargetManager()
    {
        var pipelineManager = CreatePipelineManager();
        var device = new UninitializedGraphicsDevice();
        var descriptorHeapManager = new DescriptorHeapManager();

        Assert.Throws<ArgumentNullException>(() =>
            new DirectX12PostProcessCommandContext(pipelineManager, device, null!, descriptorHeapManager));
    }

    [Fact(DisplayName = "DirectX12PostProcessCommandContext constructor throws when DescriptorHeapManager is null")]
    public void DirectX12PostProcessCommandContext_Constructor_ThrowsOnNullDescriptorHeapManager()
    {
        var pipelineManager = CreatePipelineManager();
        var device = new UninitializedGraphicsDevice();
        var renderTargetManager = new RenderTargetManager(new DescriptorHeapManager());

        Assert.Throws<ArgumentNullException>(() =>
            new DirectX12PostProcessCommandContext(pipelineManager, device, renderTargetManager, null!));
    }
}
