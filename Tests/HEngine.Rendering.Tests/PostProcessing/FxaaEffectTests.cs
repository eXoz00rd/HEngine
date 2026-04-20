using System.Globalization;
using HEngine.Rendering.PostProcessing;

namespace HEngine.Rendering.Tests.PostProcessing;

public class FxaaEffectTests
{
    [Fact(DisplayName = "FxaaEffect has Order=900")]
    public void FxaaEffect_Order_Is900()
    {
        var effect = new FxaaEffect();
        Assert.Equal(900, effect.Order);
    }

    [Fact(DisplayName = "FxaaEffect is enabled by default")]
    public void FxaaEffect_IsEnabled_ByDefault()
    {
        var effect = new FxaaEffect();
        Assert.True(effect.IsEnabled);
    }

    [Fact(DisplayName = "FxaaEffect defaults to High quality preset")]
    public void FxaaEffect_DefaultQuality_IsHigh()
    {
        var effect = new FxaaEffect();
        Assert.Equal(FxaaQualityPreset.High, effect.Quality);
    }

    [Fact(DisplayName = "FxaaEffect High quality has correct subpixel blending")]
    public void FxaaEffect_HighQuality_SubpixelBlending_IsCorrect()
    {
        var effect = new FxaaEffect { Quality = FxaaQualityPreset.High };
        Assert.Equal(1.0f, effect.SubpixelBlending);
    }

    [Fact(DisplayName = "FxaaEffect Low quality has lower subpixel blending than High")]
    public void FxaaEffect_LowQuality_SubpixelBlending_IsLess_ThanHigh()
    {
        var low = new FxaaEffect { Quality = FxaaQualityPreset.Low };
        var high = new FxaaEffect { Quality = FxaaQualityPreset.High };
        Assert.True(low.SubpixelBlending < high.SubpixelBlending);
    }

    [Fact(DisplayName = "FxaaEffect Low quality has higher edge threshold than High")]
    public void FxaaEffect_LowQuality_EdgeThreshold_IsHigher_ThanHigh()
    {
        var low = new FxaaEffect { Quality = FxaaQualityPreset.Low };
        var high = new FxaaEffect { Quality = FxaaQualityPreset.High };
        Assert.True(low.EdgeThreshold > high.EdgeThreshold);
    }

    [Fact(DisplayName = "FxaaEffect.Execute sets RcpFrame constant based on viewport size")]
    public void FxaaEffect_Execute_SetsRcpFrame_CorrectValues()
    {
        var effect = new FxaaEffect();
        var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };

        effect.Execute(ctx);

        var rcpX = (1.0f / 1920).ToString(CultureInfo.InvariantCulture);
        var rcpY = (1.0f / 1080).ToString(CultureInfo.InvariantCulture);
        var expected = $"FxaaRcpFrame=({rcpX},{rcpY},0,0)";
        Assert.Contains(expected, ctx.Constants);
    }

    [Fact(DisplayName = "FxaaEffect.Execute draws fullscreen triangle")]
    public void FxaaEffect_Execute_DrawsFullscreenTriangle()
    {
        var effect = new FxaaEffect();
        var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };

        effect.Execute(ctx);

        Assert.Equal(1, ctx.DrawCallCount);
    }

    [Fact(DisplayName = "FxaaEffect supports all FxaaQualityPreset enum values")]
    public void FxaaEffect_Supports_AllQualityPresets()
    {
        var presets = Enum.GetValues<FxaaQualityPreset>();
        foreach (var preset in presets)
        {
            var effect = new FxaaEffect { Quality = preset };
            var ctx = new RecordingPostProcessContext { Width = 1920, Height = 1080 };
            var ex = Record.Exception(() => effect.Execute(ctx));
            Assert.Null(ex);
        }
    }

    [Fact(DisplayName = "FxaaEffect.Execute sets FxaaQualityEdgeThreshold constant")]
    public void FxaaEffect_Execute_SetsEdgeThreshold()
    {
        var effect = new FxaaEffect { Quality = FxaaQualityPreset.Medium };
        var ctx = new RecordingPostProcessContext { Width = 1280, Height = 720 };

        effect.Execute(ctx);

        var expectedValue = effect.EdgeThreshold.ToString(CultureInfo.InvariantCulture);
        Assert.Contains($"FxaaQualityEdgeThreshold={expectedValue}", ctx.Constants);
    }
}

