using HEngine.Core.Configuration;
using HEngine.Core.Contracts;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Batches;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Factories;
using HEngine.Rendering.Input;
using HEngine.Rendering.Managers;
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
        services.AddSingleton<IRenderManager, RenderManager>();
        
        services.AddSingleton<IRenderContextFactory, SilkRenderContextFactory>();

        services.AddSingleton<IRenderBatch<SpriteData>, SpriteBatch>();
        services.AddSingleton<IShaderManager, DirectX12ShaderManager>();

        services.AddSingleton<ISpriteRenderingSystem, SpriteRenderingSystem>();
        services.AddSingleton<IMeshRenderingSystem, MeshRenderingSystem>();
        services.AddSingleton<IRenderingSystem, RenderingSystem>();

        services.AddSingleton<IRenderPipeline, RenderPipeline>();

        return services;
    }
}