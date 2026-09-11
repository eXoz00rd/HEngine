using HEngine.Core.Contracts;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Input;
using HEngine.Rendering.Renderers;
using HEngine.Rendering.Systems;
using HEngine.Rendering.Factories;
using HEngine.Rendering.Managers;
using HEngine.Rendering.PostProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Rendering.Direct3D12.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineRenderingD3D12(this IServiceCollection services)
    {
        services.AddSingleton<InputState>();
        services.AddSingleton<ICameraInputProvider, SilkCameraInputProvider>();

        services.AddSingleton<IGraphicsDevice, DirectX12Device>();
        services.AddSingleton<ISpriteRenderer, DirectX12SpriteRenderer>();
        services.AddSingleton<IRenderer, SilkDirectX12Renderer>();
        services.AddSingleton<IRenderContextFactory, SilkRenderContextFactory>();

        services.AddSingleton<ShaderFileLoader>(provider =>
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var shaderPath = Path.Combine(basePath, "Shaders");
            return new ShaderFileLoader(shaderPath);
        });

        services.AddSingleton<ShaderFileWatcher>(provider =>
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var shaderPath = Path.Combine(basePath, "Shaders");
            return new ShaderFileWatcher(shaderPath);
        });

        services.AddSingleton<ShaderDiskCache>(provider =>
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var cachePath = Path.Combine(basePath, "ShaderCache");
            return new ShaderDiskCache(cachePath);
        });

        services.AddSingleton<IShaderManager>(provider =>
        {
            var fileLoader = provider.GetRequiredService<ShaderFileLoader>();
            var diskCache = provider.GetRequiredService<ShaderDiskCache>();
            var fileWatcher = provider.GetRequiredService<ShaderFileWatcher>();
            return new DirectX12ShaderManager(fileLoader, diskCache, fileWatcher);
        });

        services.AddSingleton<ShadowMapManager>();
        services.AddSingleton<ShadowPipelineStateManager>();
        services.AddSingleton<IShadowRenderer, DirectX12ShadowRenderer>();

        services.AddSingleton<DescriptorHeapManager>();
        services.AddSingleton<TextureManager>();
        services.AddSingleton<ITextureManager>(provider => provider.GetRequiredService<TextureManager>());

        services.AddSingleton<RenderTargetManager>();
        services.AddSingleton<DirectX12PostProcessPipelineManager>();
        services.AddSingleton<DirectX12PostProcessCommandContext>();
        services.AddSingleton<IPostProcessCommandContext>(provider => provider.GetRequiredService<DirectX12PostProcessCommandContext>());

        return services;
    }
}
