using HEngine.Platform.Windowing;
using Silk.NET.Maths;
using SilkIWindow = Silk.NET.Windowing.IWindow;
using SilkWindowFactory = Silk.NET.Windowing.Window;
using SilkWindowOptions = Silk.NET.Windowing.WindowOptions;
using SilkGraphicsAPI = Silk.NET.Windowing.GraphicsAPI;

namespace HEngine.Platform.Windows.Windowing;

public sealed class SilkWindow : IWindow
{
    private bool _disposed;

    public SilkWindow(int width, int height, string title)
    {
        var options = SilkWindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        options.API = SilkGraphicsAPI.None;
        options.VSync = false;

        NativeWindow = SilkWindowFactory.Create(options);
        NativeWindow.Initialize();
    }

    internal SilkIWindow NativeWindow { get; }

    public string Title
    {
        get => NativeWindow.Title;
        set => NativeWindow.Title = value;
    }

    public int Width => NativeWindow.Size.X;
    public int Height => NativeWindow.Size.Y;
    public bool ShouldClose => NativeWindow.IsClosing;

    public NativeSurfaceHandle Surface =>
        new(NativeWindow.Native?.Win32?.Hwnd ?? 0);

    public void PumpEvents()
    {
        NativeWindow.DoEvents();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeWindow.Dispose();
        _disposed = true;
    }
}
