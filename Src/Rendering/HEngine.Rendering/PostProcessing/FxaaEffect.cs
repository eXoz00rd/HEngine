namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// FXAA 3.11 anti-aliasing effect (Timothy Lottes, public domain).
/// Order: 900 (applied last, after tone mapping).
/// </summary>
public sealed class FxaaEffect : IPostProcessEffect
{
    private static readonly float[] QualitySubpixValues = [0.50f, 0.75f, 1.00f, 1.00f];
    private static readonly float[] QualityEdgeThresholdValues = [0.166f, 0.125f, 0.063f, 0.031f];
    private static readonly float[] QualityEdgeThresholdMinValues = [0.083f, 0.063f, 0.031f, 0.016f];

    public string Name => "FXAA";
    public bool IsEnabled { get; set; } = true;
    public int Order => 900;

    public FxaaQualityPreset Quality { get; set; } = FxaaQualityPreset.High;

    public float SubpixelBlending => QualitySubpixValues[(int)Quality];
    public float EdgeThreshold => QualityEdgeThresholdValues[(int)Quality];
    public float EdgeThresholdMin => QualityEdgeThresholdMinValues[(int)Quality];

    public void Execute(IPostProcessCommandContext context)
    {
        context.SetConstantFloat("FxaaQualitySubpix", SubpixelBlending);
        context.SetConstantFloat("FxaaQualityEdgeThreshold", EdgeThreshold);
        context.SetConstantFloat("FxaaQualityEdgeThresholdMin", EdgeThresholdMin);
        context.SetConstantFloat4("FxaaRcpFrame",
            1.0f / context.Width,
            1.0f / context.Height,
            0f, 0f);
        context.DrawFullscreenTriangle();
    }
}

