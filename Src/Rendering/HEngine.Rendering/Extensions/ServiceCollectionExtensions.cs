using HEngine.Core.Configuration;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Batches;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Factories;
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
        // Direct registration - no adapter needed
        services.AddSingleton<IGraphicsDevice, DirectX12Device>();
        services.AddSingleton<ISpriteRenderer, DirectX12SpriteRenderer>();

        // Other services
        services.AddSingleton<IRenderer, SilkDirectX12Renderer>();
        services.AddSingleton<IRenderManager, RenderManager>();

        // Context factory (per device/window creation via factory, not a singleton context)
        services.AddSingleton<IRenderContextFactory, SilkRenderContextFactory>();

        services.AddSingleton<IRenderBatch<SpriteData>, SpriteBatch>();
        services.AddSingleton<IShaderManager, DirectX12ShaderManager>();

        // ✅ Poprawiona rejestracja - użyj interfejsów z SpriteRenderingSystemImplementation.cs
        services.AddSingleton<ISpriteRenderingSystem, SpriteRenderingSystem>();
        services.AddSingleton<IMeshRenderingSystem, MeshRenderingSystem>();
        services.AddSingleton<IRenderingSystem, RenderingSystem>();

        services.AddSingleton<IRenderPipeline, RenderPipeline>();


        return services;
    }
}