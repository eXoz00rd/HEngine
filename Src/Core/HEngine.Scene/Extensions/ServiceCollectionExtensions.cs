using HEngine.Core.Systems;
using HEngine.ECS.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Scene.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineScene(this IServiceCollection services)
    {
        services.AddHEngineSystem<FreeCameraSystem>(priority: 10);
        services.AddHEngineSystem<TransformHierarchySystem>(priority: 9);
        services.AddHEngineSystem<FrustumCullingSystem>(priority: 8);

        return services;
    }
}
