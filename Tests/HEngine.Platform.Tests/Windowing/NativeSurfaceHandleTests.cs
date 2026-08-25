using HEngine.Platform.Windowing;

namespace HEngine.Platform.Tests.Windowing;

public class NativeSurfaceHandleTests
{
    [Fact]
    public void Equals_SameHandleValue_ReturnsTrue()
    {
        var first = new NativeSurfaceHandle(1234);
        var second = new NativeSurfaceHandle(1234);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Equals_DifferentHandleValue_ReturnsFalse()
    {
        var first = new NativeSurfaceHandle(1234);
        var second = new NativeSurfaceHandle(5678);

        Assert.NotEqual(first, second);
        Assert.False(first == second);
    }
}
