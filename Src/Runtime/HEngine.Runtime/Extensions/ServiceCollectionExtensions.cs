using HEngine.ECS.Extensions;
using HEngine.Runtime.Configuration;
using HEngine.Runtime.Contracts;
using HEngine.Runtime.Time;
using HEngine.Scene.Extensions;
using HEngine.Serialization.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Runtime.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineRuntime(this IServiceCollection services, EngineConfiguration config)
    {
        services.AddSingleton(config);

        services.AddSingleton<GameTime>();
        services.AddHEngineECS();
        services.AddHEngineScene();
        services.AddHEngineSerialization();

        services.AddSingleton<IGameLoop, GameLoop>();

        return services;
    }
}
