using HEngine.Core.Configuration;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Batches;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Factories;
using HEngine.Rendering.Input;
using HEngine.Rendering.Managers;
using HEngine.Rendering.PostProcessing;
using HEngine.Rendering.Renderers;
using HEngine.Rendering.Systems;
using HEngine.Rendering.Systems.Contracts;
using HEngine.Rendering.Systems.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Rendering.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineRendering(this IServiceCollection services, EngineConfiguration config)
    {
        services.AddSingleton<InputState>();
        services.AddSingleton<ICameraInputProvider, SilkCameraInputProvider>();

        services.AddSingleton<IGraphicsDevice, DirectX12Device>();
        services.AddSingleton<ISpriteRenderer, DirectX12SpriteRenderer>();

        services.AddSingleton<IRenderer, SilkDirectX12Renderer>();
        services.AddSingleton<RenderManager>();
        services.AddSingleton<IRenderManager>(provider => provider.GetRequiredService<RenderManager>());
        services.AddSingleton<IRenderManagerContext>(provider => provider.GetRequiredService<RenderManager>());

        services.AddSingleton<IRenderContextFactory, SilkRenderContextFactory>();

        services.AddSingleton<IRenderBatch<SpriteData>, SpriteBatch>();

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

        services.AddSingleton<ISpriteRenderingSystem, SpriteRenderingSystem>();
        services.AddSingleton<IMeshRenderingSystem, MeshRenderingSystem>();
        services.AddSingleton<IRenderingSystem, RenderingSystem>();

        services.AddSingleton(config.Shadow);
        services.AddSingleton(config.PBR);
        services.AddSingleton(config.PostProcessing);

        services.AddSingleton<ShadowMapManager>();
        services.AddSingleton<ShadowPipelineStateManager>();
        services.AddSingleton<IShadowRenderer, DirectX12ShadowRenderer>();

        services.AddSingleton<DescriptorHeapManager>();
        services.AddSingleton<TextureManager>();
        services.AddSingleton<ITextureManager>(provider => provider.GetRequiredService<TextureManager>());
        services.AddSingleton<MaterialManager>();
        services.AddSingleton<MaterialLibrary>();

        services.AddSingleton<LightingSystem>(provider =>
        {
            var lightingSystem = new LightingSystem();
            lightingSystem.Initialize(provider.GetRequiredService<WorldManager>());
            return lightingSystem;
        });

        services.AddSingleton<ShadowRenderingSystem>(provider =>
        {
            var shadowRenderingSystem = new ShadowRenderingSystem();
            shadowRenderingSystem.Initialize(provider.GetRequiredService<WorldManager>());
            shadowRenderingSystem.SetShadowRenderer(provider.GetRequiredService<IShadowRenderer>());
            return shadowRenderingSystem;
        });

        services.AddSingleton<PostProcessStack>();

        services.AddSingleton<RenderTargetManager>();
        services.AddSingleton<DirectX12PostProcessPipelineManager>();
        services.AddSingleton<DirectX12PostProcessCommandContext>();
        services.AddSingleton<IPostProcessCommandContext>(provider => provider.GetRequiredService<DirectX12PostProcessCommandContext>());

        services.AddSingleton<IRenderPipeline, RenderPipeline>();

        return services;
    }
}