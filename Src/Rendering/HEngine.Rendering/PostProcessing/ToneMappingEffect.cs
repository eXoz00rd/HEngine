namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// HDR to LDR tone mapping effect with ACES Filmic, Reinhard, and Uncharted 2 algorithms.
/// Also applies gamma correction (linear → sRGB) and exposure control.
/// Order: 800 (applied before FXAA, after Bloom).
/// </summary>
public sealed class ToneMappingEffect : IPostProcessEffect
{
    public string Name => "ToneMapping";
    public bool IsEnabled { get; set; } = true;
    public int Order => 800;

    public ToneMappingMode Mode { get; set; } = ToneMappingMode.ACESFilmic;
    public float Exposure { get; set; } = 1.0f;
    public bool ApplyGammaCorrection { get; set; } = true;
    public float Gamma { get; set; } = 2.2f;

    public void Execute(IPostProcessCommandContext context)
    {
        context.SetConstantInt("ToneMappingMode", (int)Mode);
        context.SetConstantFloat("Exposure", Exposure);
        context.SetConstantFloat("Gamma", ApplyGammaCorrection ? Gamma : 1.0f);
        context.DrawFullscreenTriangle();
    }
}