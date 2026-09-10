using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Serialization.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHEngineSerialization(this IServiceCollection services)
    {
        services.AddSingleton<ComponentSerializerRegistry>();
        services.AddSingleton<SceneSerializer>();

        return services;
    }
}
