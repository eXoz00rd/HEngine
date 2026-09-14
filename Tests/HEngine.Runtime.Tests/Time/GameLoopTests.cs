using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Runtime.Time;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HEngine.Runtime.Tests.Time;

public class GameLoopTests
{
    private static GameLoop CreateLoop(IRenderManager renderManager, IRenderPipeline renderPipeline) =>
        new(new GameTime(), new SystemManager(), renderPipeline, renderManager, NullLogger<GameLoop>.Instance);

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

        renderManager.Received(1).UpdateInput();
        renderPipeline.Received(1).RenderFrame();
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
}
