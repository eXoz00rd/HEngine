using HEngine.Core.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.ECS.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineECS(this IServiceCollection services)
    {
        services.AddSingleton<WorldManager>();
        services.AddSingleton<SystemManager>();

        return services;
    }
}
