using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Batches;
using HEngine.Rendering.Configuration;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.Managers;
using HEngine.Rendering.PostProcessing;
using HEngine.Rendering.Systems;
using HEngine.Rendering.Systems.Contracts;
using HEngine.Rendering.Systems.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Rendering.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineRendering(this IServiceCollection services,
        RenderingSettings rendering, PbrSettings pbr, ShadowSettings shadow, PostProcessingSettings postProcessing)
    {
        services.AddSingleton(rendering);
        services.AddSingleton(pbr);
        services.AddSingleton(shadow);
        services.AddSingleton(postProcessing);

        services.AddSingleton<RenderManager>();
        services.AddSingleton<IRenderManager>(provider => provider.GetRequiredService<RenderManager>());
        services.AddSingleton<IRenderManagerContext>(provider => provider.GetRequiredService<RenderManager>());

        services.AddSingleton<IRenderBatch<SpriteData>, SpriteBatch>();

        services.AddSingleton<ISpriteRenderingSystem, SpriteRenderingSystem>();
        services.AddSingleton<IMeshRenderingSystem, MeshRenderingSystem>();
        services.AddSingleton<IRenderingSystem, RenderingSystem>();

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

        services.AddSingleton<IRenderPipeline, RenderPipeline>();

        return services;
    }
}
