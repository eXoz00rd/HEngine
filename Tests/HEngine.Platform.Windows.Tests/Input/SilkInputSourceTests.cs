using HEngine.Platform.Windowing;
using HEngine.Platform.Windows.Input;

namespace HEngine.Platform.Windows.Tests.Input;

public class SilkInputSourceTests
{
    [Fact]
    public void Constructor_RejectsNonSilkWindow()
    {
        var window = new FakeWindow();

        Assert.Throws<ArgumentException>(() => new SilkInputSource(window));
    }

    private sealed class FakeWindow : IWindow
    {
        public string Title { get; set; } = string.Empty;
        public int Width => 0;
        public int Height => 0;
        public bool ShouldClose => false;
        public NativeSurfaceHandle Surface => default;

        public void PumpEvents()
        {
        }

        public void Dispose()
        {
        }
    }
}
