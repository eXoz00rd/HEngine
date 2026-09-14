using HEngine.Platform.Configuration;
using HEngine.Rendering.Configuration;

namespace HEngine.Runtime.Configuration;

public class EngineConfiguration {
    public WindowSettings Window { get; set; } = new();
    public RenderingSettings Rendering { get; set; } = new();
    public PbrSettings PBR { get; set; } = new();
    public ShadowSettings Shadow { get; set; } = new();
    public PostProcessingSettings PostProcessing { get; set; } = new();
}
