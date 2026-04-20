using HEngine.Rendering.PostProcessing;

namespace HEngine.Rendering.Tests.PostProcessing;

public class ToneMappingEffectTests
{
    [Fact(DisplayName = "ToneMappingEffect has Order=800")]
    public void ToneMappingEffect_Order_Is800()
    {
        var effect = new ToneMappingEffect();
        Assert.Equal(800, effect.Order);
    }

    [Fact(DisplayName = "ToneMappingEffect is enabled by default")]
    public void ToneMappingEffect_IsEnabled_ByDefault()
    {
        var effect = new ToneMappingEffect();
        Assert.True(effect.IsEnabled);
    }

    [Fact(DisplayName = "ToneMappingEffect defaults to ACES Filmic")]
    public void ToneMappingEffect_DefaultMode_IsACES()
    {
        var effect = new ToneMappingEffect();
        Assert.Equal(ToneMappingMode.ACESFilmic, effect.Mode);
    }

    [Fact(DisplayName = "ToneMappingEffect.Execute sets ToneMappingMode constant")]
    public void ToneMappingEffect_Execute_SetsToneMappingMode()
    {
        var effect = new ToneMappingEffect { Mode = ToneMappingMode.Reinhard };
        var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };

        effect.Execute(ctx);

        Assert.Contains($"ToneMappingMode={(int)ToneMappingMode.Reinhard}", ctx.Constants);
    }

    [Fact(DisplayName = "ToneMappingEffect.Execute sets Exposure constant")]
    public void ToneMappingEffect_Execute_SetsExposure()
    {
        var effect = new ToneMappingEffect { Exposure = 1.5f };
        var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };

        effect.Execute(ctx);

        Assert.Contains("Exposure=1.5", ctx.Constants);
    }

    [Fact(DisplayName = "ToneMappingEffect.Execute draws fullscreen triangle")]
    public void ToneMappingEffect_Execute_DrawsFullscreenTriangle()
    {
        var effect = new ToneMappingEffect();
        var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };

        effect.Execute(ctx);

        Assert.Equal(1, ctx.DrawCallCount);
    }

    [Fact(DisplayName = "ToneMappingEffect.Execute sets Gamma=1.0 when gamma correction disabled")]
    public void ToneMappingEffect_Execute_GammaIs1_WhenDisabled()
    {
        var effect = new ToneMappingEffect { ApplyGammaCorrection = false, Gamma = 2.2f };
        var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };

        effect.Execute(ctx);

        Assert.Contains("Gamma=1", ctx.Constants);
    }

    [Fact(DisplayName = "ToneMappingEffect supports all ToneMappingMode enum values")]
    public void ToneMappingEffect_Supports_AllModes()
    {
        var modes = Enum.GetValues<ToneMappingMode>();

        foreach (var mode in modes)
        {
            var effect = new ToneMappingEffect { Mode = mode };
            var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };
            var ex = Record.Exception(() => effect.Execute(ctx));
            Assert.Null(ex);
        }
    }
}

