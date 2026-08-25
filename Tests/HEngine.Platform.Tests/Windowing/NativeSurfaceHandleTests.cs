using HEngine.Platform.Windowing;

namespace HEngine.Platform.Tests.Windowing;

public class NativeSurfaceHandleTests
{
    [Theory]
    [InlineData(1234, 1234, true)]
    [InlineData(1234, 5678, false)]
    public void Equals_ComparesByHandleValue(int firstHandle, int secondHandle, bool expectedEqual)
    {
        var first = new NativeSurfaceHandle(firstHandle);
        var second = new NativeSurfaceHandle(secondHandle);

        Assert.Equal(expectedEqual, first.Equals(second));
        Assert.Equal(expectedEqual, first == second);
    }
}
