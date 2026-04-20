using HEngine.Rendering.PostProcessing;

namespace HEngine.Rendering.Tests.PostProcessing;

public class PingPongRenderTargetsTests
{
    [Fact(DisplayName = "PingPongRenderTargets starts with A as source, B as destination")]
    public void PingPong_Initial_Source_IsA_Destination_IsB()
    {
        var pp = new PingPongRenderTargets();

        Assert.Equal(PingPongRenderTargets.RenderTargetA, pp.CurrentSource);
        Assert.Equal(PingPongRenderTargets.RenderTargetB, pp.CurrentDestination);
    }

    [Fact(DisplayName = "PingPongRenderTargets.Flip swaps source and destination")]
    public void PingPong_Flip_SwapsSourceAndDestination()
    {
        var pp = new PingPongRenderTargets();
        pp.Flip();

        Assert.Equal(PingPongRenderTargets.RenderTargetB, pp.CurrentSource);
        Assert.Equal(PingPongRenderTargets.RenderTargetA, pp.CurrentDestination);
    }

    [Fact(DisplayName = "PingPongRenderTargets double Flip restores original state")]
    public void PingPong_DoubleFlip_RestoresOriginalState()
    {
        var pp = new PingPongRenderTargets();
        pp.Flip();
        pp.Flip();

        Assert.Equal(PingPongRenderTargets.RenderTargetA, pp.CurrentSource);
        Assert.Equal(PingPongRenderTargets.RenderTargetB, pp.CurrentDestination);
    }

    [Fact(DisplayName = "PingPongRenderTargets.FlipCount increments on each Flip")]
    public void PingPong_FlipCount_Increments()
    {
        var pp = new PingPongRenderTargets();

        pp.Flip();
        pp.Flip();
        pp.Flip();

        Assert.Equal(3, pp.FlipCount);
    }

    [Fact(DisplayName = "PingPongRenderTargets.Reset returns to initial state")]
    public void PingPong_Reset_ReturnsToInitialState()
    {
        var pp = new PingPongRenderTargets();
        pp.Flip();
        pp.Flip();
        pp.Flip();
        pp.Reset();

        Assert.Equal(PingPongRenderTargets.RenderTargetA, pp.CurrentSource);
        Assert.Equal(PingPongRenderTargets.RenderTargetB, pp.CurrentDestination);
        Assert.Equal(0, pp.FlipCount);
    }
}

