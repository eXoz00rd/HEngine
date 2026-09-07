using HEngine.Platform.Windows.Time;

namespace HEngine.Platform.Windows.Tests.Time;

public class SystemClockTests
{
    [Fact]
    public void Elapsed_IncreasesMonotonically()
    {
        var clock = new SystemClock();

        var first = clock.Elapsed;

        SpinWait.SpinUntil(() => clock.Elapsed != first, TimeSpan.FromSeconds(1));

        Assert.True(clock.Elapsed > first);
    }
}
