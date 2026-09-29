using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.ECS.Contracts;
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

    public static IServiceCollection AddHEngineSystem<T>(this IServiceCollection services, int priority = 0,
        bool enabled = true) where T : class, ISystem
    {
        services.AddSingleton<ISystemRegistration>(provider =>
            new SystemRegistration<T>(ActivatorUtilities.CreateInstance<T>(provider), priority, enabled));

        return services;
    }

    private sealed class SystemRegistration<T> : ISystemRegistration where T : ISystem
    {
        private readonly T _system;
        private readonly int _priority;
        private readonly bool _enabled;

        public SystemRegistration(T system, int priority, bool enabled)
        {
            _system = system;
            _priority = priority;
            _enabled = enabled;
        }

        public void AddTo(WorldManager world)
        {
            world.AddSystem(_system, _priority, _enabled);
        }
    }
}
