namespace HEngine.Platform.Configuration;

public class WindowSettings {
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public string Title { get; set; } = "HEngine";
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
}
