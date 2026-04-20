using HEngine.Rendering.PostProcessing;

namespace HEngine.Rendering.Tests.PostProcessing;

public class BloomEffectTests
{
    [Fact(DisplayName = "BloomEffect has Order=600")]
    public void BloomEffect_Order_Is600()
    {
        var effect = new BloomEffect();
        Assert.Equal(600, effect.Order);
    }

    [Fact(DisplayName = "BloomEffect is enabled by default")]
    public void BloomEffect_IsEnabled_ByDefault()
    {
        var effect = new BloomEffect();
        Assert.True(effect.IsEnabled);
    }

    [Fact(DisplayName = "BloomEffect downsample chain has correct level count")]
    public void BloomEffect_DownsampleChain_HasCorrectLevelCount()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1920, 1080, 5);
        Assert.Equal(5, chain.Length);
    }

    [Fact(DisplayName = "BloomEffect downsample chain Level 0 is full resolution")]
    public void BloomEffect_DownsampleChain_Level0_IsFullResolution()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1920, 1080, 5);
        Assert.Equal(1920, chain[0].Width);
        Assert.Equal(1080, chain[0].Height);
    }

    [Fact(DisplayName = "BloomEffect downsample chain Level 1 is half resolution")]
    public void BloomEffect_DownsampleChain_Level1_IsHalfResolution()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1920, 1080, 5);
        Assert.Equal(960, chain[1].Width);
        Assert.Equal(540, chain[1].Height);
    }

    [Fact(DisplayName = "BloomEffect downsample chain Level 2 is quarter resolution")]
    public void BloomEffect_DownsampleChain_Level2_IsQuarterResolution()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1920, 1080, 5);
        Assert.Equal(480, chain[2].Width);
        Assert.Equal(270, chain[2].Height);
    }

    [Fact(DisplayName = "BloomEffect downsample chain Level 3 is eighth resolution")]
    public void BloomEffect_DownsampleChain_Level3_IsEighthResolution()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1920, 1080, 5);
        Assert.Equal(240, chain[3].Width);
        Assert.Equal(135, chain[3].Height);
    }

    [Fact(DisplayName = "BloomEffect downsample chain Level 4 is sixteenth resolution")]
    public void BloomEffect_DownsampleChain_Level4_IsSixteenthResolution()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1920, 1080, 5);
        Assert.Equal(120, chain[4].Width);
        Assert.Equal(67, chain[4].Height);
    }

    [Fact(DisplayName = "BloomEffect downsample chain level indices are sequential")]
    public void BloomEffect_DownsampleChain_LevelIndices_AreSequential()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1920, 1080, 4);
        for (var i = 0; i < chain.Length; i++)
            Assert.Equal(i, chain[i].Level);
    }

    [Fact(DisplayName = "BloomEffect downsample chain minimum dimension is 1")]
    public void BloomEffect_DownsampleChain_MinimumDimension_IsOne()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1, 1, 5);
        foreach (var mip in chain)
        {
            Assert.True(mip.Width >= 1);
            Assert.True(mip.Height >= 1);
        }
    }

    [Fact(DisplayName = "BloomEffect downsample chain caps to MaxMipLevels")]
    public void BloomEffect_DownsampleChain_CapsToMaxMipLevels()
    {
        var chain = BloomEffect.ComputeDownsampleChain(1920, 1080, 100);
        Assert.Equal(BloomEffect.MaxMipLevels, chain.Length);
    }

    [Fact(DisplayName = "BloomEffect.ComputeDownsampleChain throws for zero width")]
    public void BloomEffect_ComputeDownsampleChain_Throws_ForZeroWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BloomEffect.ComputeDownsampleChain(0, 1080, 5));
    }

    [Fact(DisplayName = "BloomEffect.ComputeDownsampleChain throws for zero levels")]
    public void BloomEffect_ComputeDownsampleChain_Throws_ForZeroLevels()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BloomEffect.ComputeDownsampleChain(1920, 1080, 0));
    }

    [Fact(DisplayName = "BloomEffect.Execute draws fullscreen triangle")]
    public void BloomEffect_Execute_DrawsFullscreenTriangle()
    {
        var effect = new BloomEffect();
        var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };

        effect.Execute(ctx);

        Assert.Equal(1, ctx.DrawCallCount);
    }

    [Fact(DisplayName = "BloomEffect.Execute sets Threshold constant")]
    public void BloomEffect_Execute_SetsThreshold()
    {
        var effect = new BloomEffect { Threshold = 0.8f };
        var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };

        effect.Execute(ctx);

        Assert.Contains("BloomThreshold=0.8", ctx.Constants);
    }
}

