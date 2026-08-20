using HEngine.Core.Configuration;
using HEngine.Core.Contracts;
using HEngine.Core.Time;
using HEngine.ECS.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineCore(this IServiceCollection services, EngineConfiguration config)
    {
        services.AddSingleton(config);

        services.AddSingleton<GameTime>();
        services.AddHEngineECS();

        services.AddSingleton<IGameLoop, GameLoop>();

        return services;
    }
}