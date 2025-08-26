// Src/Core/HEngine.Core/DI/ServiceCollectionExtensions.cs

using HEngine.Core.Configuration;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HEngine.Core.ServiceExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineCore(this IServiceCollection services, EngineConfiguration config)
    {
        // Configuration
        services.AddSingleton(config);
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Core Services
        services.AddSingleton<GameTime>();
        services.AddSingleton<WorldManager>();
        services.AddSingleton<SystemManager>();
        
        // Game Loop
        services.AddSingleton<IGameLoop, GameLoop>();
        
        return services;
    }
}
