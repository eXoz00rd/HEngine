namespace HEngine.Platform.Windowing;

public interface IWindow
{
    string Title { get; set; }
    int Width { get; }
    int Height { get; }
    bool ShouldClose { get; }
    NativeSurfaceHandle Surface { get; }

    void PumpEvents();
    void Close();
}
