using HEngine.Platform.Windows.Time;

namespace HEngine.Platform.Windows.Tests.Time;

public class SystemClockTests
{
    [Fact]
    public void Elapsed_IncreasesMonotonically()
    {
        var clock = new SystemClock();

        var first = clock.Elapsed;
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (clock.Elapsed == first && DateTime.UtcNow < deadline)
        {
            Thread.SpinWait(100);
        }

        Assert.True(clock.Elapsed > first);
    }
}
