using HEngine.Platform.Windows.Time;

namespace HEngine.Platform.Windows.Tests.Time;

public class SystemClockTests
{
    [Fact]
    public void Elapsed_IncreasesMonotonically()
    {
        var clock = new SystemClock();

        var first = clock.Elapsed;
        Thread.Sleep(10);
        var second = clock.Elapsed;

        Assert.True(second > first);
    }
}
