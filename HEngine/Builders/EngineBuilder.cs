using HEngine.Rendering.Direct3D12.Extensions;
using HEngine.Rendering.Extensions;
using HEngine.Runtime.Configuration;
using HEngine.Runtime.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HEngine.Builders;

public class EngineBuilder
{
    private readonly EngineConfiguration _configuration;
    private readonly ServiceCollection _services;

    public EngineBuilder(EngineConfiguration? configuration = null)
    {
        _services = [];
        _configuration = configuration ?? new EngineConfiguration();
    }

    public EngineBuilder AddRuntime()
    {
        _services.AddHEngineRuntime(_configuration);
        return this;
    }

    public EngineBuilder AddRendering()
    {
        _services.AddHEngineRendering(
            _configuration.Rendering, _configuration.PBR, _configuration.Shadow, _configuration.PostProcessing);
        return this;
    }

    public EngineBuilder AddDirectX12Backend()
    {
        _services.AddHEngineRenderingD3D12();
        return this;
    }

    public EngineBuilder AddLogging(Action<ILoggingBuilder>? configure = null)
    {
        _services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);

            configure?.Invoke(builder);
        });
        return this;
    }

    public EngineBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    public GameEngine Build()
    {
        _services.AddSingleton<GameEngine>();

        var serviceProvider = _services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        return serviceProvider.GetRequiredService<GameEngine>();
    }
}