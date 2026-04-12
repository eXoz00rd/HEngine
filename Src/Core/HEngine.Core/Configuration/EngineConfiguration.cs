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
    public PBRSettings PBR { get; set; } = new();
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
    public int TargetFPS { get; set; } = 60;
    public bool LimitFrameRate { get; set; } = false;
    public bool ShowFPS { get; set; } = true;
}

public class PBRSettings {
    public bool UseHdrRenderTarget { get; set; } = true;
    public float Exposure { get; set; } = 1.0f;
    public Vector3 AmbientColor { get; set; } = new(0.03f, 0.03f, 0.03f);
    public int MaxActiveLights { get; set; } = 8;
    public bool EnableBloom { get; set; } = false;
}
