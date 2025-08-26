// Src/Core/HEngine.Core/DI/ServiceCollectionExtensions.cs

using HEngine.Core.Configuration;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.ServiceExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineRendering(this IServiceCollection services, EngineConfiguration config)
    {
        services.AddSingleton(config);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<GameTime>();
        services.AddSingleton<WorldManager>();
        services.AddSingleton<SystemManager>();

        services.AddSingleton<IGameLoop, GameLoop>();

        return services;
    }
}