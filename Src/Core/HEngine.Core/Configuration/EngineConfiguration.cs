using System.Numerics;

namespace HEngine.Core.Configuration;

public class EngineConfiguration {
    public WindowSettings Window { get; set; } = new();
    public RenderingSettings Rendering { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
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
    public float FieldOfView { get; set; } = MathF.PI / 4;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 100.0f;
}

public class PerformanceSettings {
    public int TargetFPS { get; set; } = 60;
    public bool LimitFrameRate { get; set; } = false;
    public bool ShowFPS { get; set; } = true;
}