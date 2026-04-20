using System.Numerics;

namespace HEngine.Core.Configuration;

public enum ProjectionMode
{
    Orthographic,
    Perspective
}

public class EngineConfiguration {
    public WindowSettings Window { get; set; } = new();
    public RenderingSettings Rendering { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
    public PbrSettings PBR { get; set; } = new();
    public ShadowSettings Shadow { get; set; } = new();
    public PostProcessingSettings PostProcessing { get; set; } = new();
}

public class WindowSettings {
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public string Title { get; set; } = "HEngine";
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
}

public class RenderingSettings {
    public Vector4 ClearColor { get; set; } = new(0.2f, 0.3f, 0.8f, 1.0f);
    public ProjectionMode ProjectionMode { get; set; } = ProjectionMode.Orthographic;
    public float FieldOfView { get; set; } = MathF.PI / 4;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 100.0f;
    public int MaxAnisotropy { get; set; } = 16;
}

public class PerformanceSettings {
    public int TargetFps { get; set; } = 60;
    public bool LimitFrameRate { get; set; } = false;
    public bool ShowFps { get; set; } = true;
}

public class PbrSettings {
    public bool UseHdrRenderTarget { get; set; } = true;
    public float Exposure { get; set; } = 1.0f;
    public Vector3 AmbientColor { get; set; } = new(0.03f, 0.03f, 0.03f);
    public int MaxActiveLights { get; set; } = 8;
    public bool EnableBloom { get; set; } = false;
}

public class ShadowSettings {
    public bool Enabled { get; set; } = true;
    public int Resolution { get; set; } = 2048;
    public int CascadeCount { get; set; } = 4;
    public float LambdaSplit { get; set; } = 0.75f;
    public float DepthBias { get; set; } = 0.001f;
    public float SlopeScaledDepthBias { get; set; } = 2.0f;
}

public class PostProcessingSettings {
    public bool EnableBloom { get; set; } = true;
    public float BloomThreshold { get; set; } = 1.0f;
    public float BloomIntensity { get; set; } = 1.0f;
    public int BloomMipLevels { get; set; } = 5;
    public int ToneMapping { get; set; } = 0;
    public int AntiAliasing { get; set; } = 1;
    public float Exposure { get; set; } = 1.0f;
    public bool EnableGammaCorrection { get; set; } = true;
    public float Gamma { get; set; } = 2.2f;
}
