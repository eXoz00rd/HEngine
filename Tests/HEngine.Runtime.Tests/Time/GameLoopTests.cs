using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.ECS.Extensions;
using HEngine.Runtime.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HEngine.Runtime.Tests.Time;

public class GameLoopTests
{
    private static GameLoop CreateLoop(IRenderManager renderManager, IRenderPipeline renderPipeline) =>
        new(new GameTime(), new WorldManager(new SystemManager()), renderPipeline, renderManager, NullLogger<GameLoop>.Instance);

    [Fact(DisplayName = "Run renders no frame when the render manager already wants to close")]
    public void Run_DoesNotRenderFrame_WhenShouldCloseIsAlreadyTrue()
    {
        var renderManager = Substitute.For<IRenderManager>();
        renderManager.ShouldClose.Returns(true);
        var renderPipeline = Substitute.For<IRenderPipeline>();

        CreateLoop(renderManager, renderPipeline).Run();

        renderPipeline.DidNotReceive().RenderFrame();
        renderManager.DidNotReceive().UpdateInput();
    }

    [Fact(DisplayName = "Each frame pumps input before rendering, and Stop ends the loop")]
    public void Run_PumpsInputAndRenders_UntilStopped()
    {
        var renderManager = Substitute.For<IRenderManager>();
        renderManager.ShouldClose.Returns(false);
        var renderPipeline = Substitute.For<IRenderPipeline>();

        GameLoop loop = null!;
        renderPipeline.When(pipeline => pipeline.RenderFrame()).Do(_ => loop.Stop());
        loop = CreateLoop(renderManager, renderPipeline);

        loop.Run();

        Received.InOrder(() =>
        {
            renderManager.UpdateInput();
            renderPipeline.RenderFrame();
        });
        Assert.False(loop.IsRunning);
    }

    [Fact(DisplayName = "A frame that throws stops the loop instead of spinning on the failure")]
    public void Run_StopsLoop_WhenAFrameThrows()
    {
        var renderManager = Substitute.For<IRenderManager>();
        renderManager.ShouldClose.Returns(false);
        var renderPipeline = Substitute.For<IRenderPipeline>();
        renderPipeline.When(pipeline => pipeline.RenderFrame()).Do(_ => throw new InvalidOperationException("device lost"));

        var loop = CreateLoop(renderManager, renderPipeline);

        loop.Run();

        Assert.False(loop.IsRunning);
        renderPipeline.Received(1).RenderFrame();
    }

    [Fact(DisplayName = "Stop before Run leaves the loop stopped")]
    public void Stop_BeforeRun_LeavesLoopStopped()
    {
        var renderManager = Substitute.For<IRenderManager>();
        var renderPipeline = Substitute.For<IRenderPipeline>();
        var loop = CreateLoop(renderManager, renderPipeline);

        loop.Stop();

        Assert.False(loop.IsRunning);
    }

    [Fact(DisplayName = "Resolving GameLoop from DI also constructs WorldManager, so its registered systems are present before the loop ever runs")]
    public void GameLoop_ResolvedFromContainer_HasWorldSystemsRegisteredBeforeRunning()
    {
        var services = new ServiceCollection();
        services.AddHEngineECS();
        services.AddHEngineSystem<TickCountingSystem>();
        services.AddSingleton(new GameTime());
        services.AddSingleton(Substitute.For<IRenderManager>());
        services.AddSingleton(Substitute.For<IRenderPipeline>());
        services.AddSingleton<ILogger<GameLoop>>(NullLogger<GameLoop>.Instance);
        services.AddSingleton<GameLoop>();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<GameLoop>();

        Assert.True(provider.GetRequiredService<WorldManager>().HasSystem<TickCountingSystem>());
    }

    private sealed class TickCountingSystem : ISystem
    {
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
