using HEngine.Core.Configuration;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Time;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineCore(this IServiceCollection services, EngineConfiguration config)
    {
        // Configuration
        services.AddSingleton(config);

        // Core Services
        services.AddSingleton<GameTime>();
        services.AddSingleton<WorldManager>();
        services.AddSingleton<SystemManager>();

        // Game Loop
        services.AddSingleton<IGameLoop, GameLoop>();

        return services;
    }
}