using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.ECS.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.ECS.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure, List<string>? tickLog = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(tickLog ?? []);
        services.AddHEngineECS();
        configure(services);

        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact(DisplayName = "A system registered with AddHEngineSystem is added to the WorldManager resolved from the container")]
    public void RegisteredSystem_IsAddedToResolvedWorld()
    {
        using var provider = BuildProvider(services => services.AddHEngineSystem<FirstSystem>());

        var world = provider.GetRequiredService<WorldManager>();

        Assert.True(world.HasSystem<FirstSystem>());
        Assert.NotNull(world.GetSystem<FirstSystem>());
    }

    [Fact(DisplayName = "A system registered with AddHEngineSystem is not independently resolvable from the container, so only the world disposes it")]
    public void RegisteredSystem_IsNotIndependentlyResolvable()
    {
        using var provider = BuildProvider(services => services.AddHEngineSystem<FirstSystem>());

        Assert.Null(provider.GetService<FirstSystem>());
    }

    [Fact(DisplayName = "A system registered with AddHEngineSystem is disposed exactly once when the provider is disposed")]
    public void RegisteredSystem_IsDisposedExactlyOnce()
    {
        var services = new ServiceCollection();
        services.AddHEngineECS();
        services.AddHEngineSystem<CountingDisposeSystem>();

        var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var world = provider.GetRequiredService<WorldManager>();
        var system = (CountingDisposeSystem)world.GetSystem<CountingDisposeSystem>()!;

        provider.Dispose();

        Assert.Equal(1, system.DisposeCount);
    }

    [Fact(DisplayName = "Systems registered with AddHEngineSystem tick in descending priority order regardless of registration order")]
    public void RegisteredSystems_TickInPriorityOrder()
    {
        var tickLog = new List<string>();
        using var provider = BuildProvider(services =>
        {
            services.AddHEngineSystem<SecondSystem>(priority: 1);
            services.AddHEngineSystem<FirstSystem>(priority: 5);
        }, tickLog);

        provider.GetRequiredService<WorldManager>();
        provider.GetRequiredService<SystemManager>().Update(0.016f);

        Assert.Equal([nameof(FirstSystem), nameof(SecondSystem)], tickLog);
    }

    [Fact(DisplayName = "A system registered as disabled is added to the world but does not tick")]
    public void DisabledSystem_IsAddedButDoesNotTick()
    {
        var tickLog = new List<string>();
        using var provider = BuildProvider(services => services.AddHEngineSystem<FirstSystem>(enabled: false), tickLog);

        var world = provider.GetRequiredService<WorldManager>();
        provider.GetRequiredService<SystemManager>().Update(0.016f);

        Assert.True(world.HasSystem<FirstSystem>());
        Assert.Equal(0, world.GetActiveSystemCount());
        Assert.Empty(tickLog);
    }

    [Fact(DisplayName = "A registered system with an unregistered dependency fails as soon as the world resolves it")]
    public void RegisteredSystem_WithMissingDependency_FailsOnWorldResolution()
    {
        using var provider = BuildProvider(services => services.AddHEngineSystem<SystemWithMissingDependency>());

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<WorldManager>());
    }

    private abstract class LoggingSystem : ISystem
    {
        private readonly List<string> _tickLog;
        private readonly string _name;

        protected LoggingSystem(List<string> tickLog, string name)
        {
            _tickLog = tickLog;
            _name = name;
        }

        public void Initialize(WorldManager worldManager)
        {
        }

        public void Update(float deltaTime)
        {
            _tickLog.Add(_name);
        }

        public void Dispose()
        {
        }
    }

    private sealed class FirstSystem : LoggingSystem
    {
        public FirstSystem(List<string> tickLog) : base(tickLog, nameof(FirstSystem))
        {
        }
    }

    private sealed class SecondSystem : LoggingSystem
    {
        public SecondSystem(List<string> tickLog) : base(tickLog, nameof(SecondSystem))
        {
        }
    }

    private sealed class CountingDisposeSystem : ISystem
    {
        public int DisposeCount { get; private set; }

        public void Initialize(WorldManager worldManager)
        {
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private interface IMissingDependency;

    private sealed class SystemWithMissingDependency : ISystem
    {
        public SystemWithMissingDependency(IMissingDependency dependency)
        {
        }

        public void Initialize(WorldManager worldManager)
        {
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
        }
    }
}
